/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"context"
	"crypto/sha256"
	"crypto/subtle"
	"errors"
	"fmt"
	"hash"
	"io"
	"log"
	"mime"
	"net/http"
	"net/url"
	"os"
	"path"
	"syscall"
	"unsafe"

	"github.com/Amebis/eduVPN/eduvpn-windows/progress"
)

// Check locates latest version available from url discovery resource and version of the product installed.
func Check(url string, allowedSigners []TrustedSigner, productId string, ctx context.Context) (pkg *Package, ver *Version, err error) {
	type resultDiscovery struct {
		pkg *Package
		err error
	}
	discovery := make(chan resultDiscovery)
	go func() {
		pkg, err := discoverAvailable(makeClient(), url, allowedSigners, ctx, progress.Noop())
		discovery <- resultDiscovery{pkg, err}
	}()

	type resultEvaluation struct {
		ver *Version
		err error
	}
	evaluation := make(chan resultEvaluation)
	go func() {
		ver, err := evaluateInstalled(productId, ctx, progress.Noop())
		evaluation <- resultEvaluation{ver, err}
	}()

	discovered := <-discovery
	if discovered.err != nil {
		return nil, nil, discovered.err
	}
	evaluated := <-evaluation
	if evaluated.err != nil {
		return nil, nil, evaluated.err
	}

	return discovered.pkg, evaluated.ver, nil
}

type mywriter struct {
	offset, total int64
	hash          hash.Hash
	ctx           context.Context
	progress      progress.ProgressIndicator
}

func (w *mywriter) Write(p []byte) (int, error) {
	select {
	case <-w.ctx.Done():
		return 0, w.ctx.Err()
	default:
	}
	n := len(p)
	w.hash.Write(p)
	w.offset += int64(n)
	if w.total > 0 {
		w.progress.SetProgress(float32(w.offset) / float32(w.total))
	}
	return n, nil
}

// downloadInstaller downloads a file from first available urls, checks its SHA256 hash, and saves it into specified folder.
// The file returned is kept open for writing and should be closed by caller.
func downloadInstaller(folder string, urls []string, hash *Hash, ctx context.Context, progress progress.ProgressIndicator) (filename string, file *os.File, err error) {
	client := makeClient()
	for _, u := range urls {
		select {
		case <-ctx.Done():
			return "", nil, ctx.Err()
		default:
		}

		log.Printf("Downloading installer file %s\n", u)

		var req *http.Request
		req, err = http.NewRequestWithContext(ctx, "GET", u, nil)
		if err != nil {
			continue
		}
		req.Header.Set("User-Agent", userAgent)
		var resp *http.Response
		resp, err = client.Do(req)
		if err != nil {
			continue
		}
		defer resp.Body.Close()
		if resp.StatusCode != http.StatusOK {
			continue
		}
		filename = "Setup.exe"
		if u2, err2 := url.Parse(u); err2 == nil {
			_, filename = path.Split(u2.Path)
		}
		if val, ok := resp.Header["Content-Disposition"]; ok && len(val) > 0 {
			if _, params, err2 := mime.ParseMediaType(val[0]); err2 == nil {
				_, filename = path.Split(params["filename"])
			}
		}
		absoluteFilename := path.Join(folder, filename)

		var fileW *os.File
		fileW, err = os.OpenFile(absoluteFilename, os.O_WRONLY|os.O_CREATE|os.O_TRUNC, 0777)
		if err != nil {
			continue
		}
		counter := &mywriter{total: resp.ContentLength, hash: sha256.New(), ctx: ctx, progress: progress}
		if counter.total > 0 {
			// Preallocate file to keep NTFS fragmentation low.
			fileW.Truncate(counter.total)
		}
		if _, err = io.Copy(fileW, io.TeeReader(resp.Body, counter)); err != nil {
			fileW.Close()
			os.Remove(absoluteFilename)
			continue
		}
		fileW.Truncate(counter.offset)
		if subtle.ConstantTimeCompare(counter.hash.Sum(nil), unsafe.Slice(&hash[0], 32)) == 0 {
			fileW.Close()
			os.Remove(absoluteFilename)
			err = fmt.Errorf("file content different than expected: %s", u)
			continue
		}

		// Reopen file as read-only to allow execution of the file, but keep the file locked for writing.
		file, err = os.OpenFile(absoluteFilename, os.O_RDONLY, 0777)
		if err != nil {
			continue
		}
		fileW.Close()
		return filename, file, nil
	}
	if err == nil {
		err = errors.New("resource not available")
	}
	return "", nil, err
}

// DownloadAndInstall securely downloads installer file, prepares and launches installer
func DownloadAndInstall(urls []string, hash *Hash, installerArguments string, ctx context.Context, progress progress.ProgressIndicator) error {
	workingFolder, err := os.MkdirTemp(os.TempDir(), "SURF")
	if err != nil {
		return fmt.Errorf("failed to create temporary folder: %w", err)
	}
	defer os.RemoveAll(workingFolder)

	installerFilename, installerFile, err := downloadInstaller(workingFolder, urls, hash, ctx, progress)
	if err != nil {
		return fmt.Errorf("error downloading installer: %w", err)
	}
	absoluteInstallerFilename := path.Join(workingFolder, installerFilename)
	defer os.Remove(absoluteInstallerFilename)
	defer installerFile.Close()

	workingFolderU16, err := syscall.UTF16PtrFromString(workingFolder)
	if err != nil {
		return fmt.Errorf("failed to convert working folder to UTF-16: %w", err)
	}
	installerFilenameU16, err := syscall.UTF16PtrFromString(absoluteInstallerFilename)
	if err != nil {
		return fmt.Errorf("failed to convert installer filename to UTF-16: %w", err)
	}
	commandLineU16, err := syscall.UTF16PtrFromString("\"" + installerFilename + "\" " + installerArguments)
	if err != nil {
		return fmt.Errorf("failed to convert command line to UTF-16: %w", err)
	}
	si := syscall.StartupInfo{
		Cb: uint32(unsafe.Sizeof(syscall.StartupInfo{})),
	}
	pi := syscall.ProcessInformation{}
	err = syscall.CreateProcess(installerFilenameU16, commandLineU16, nil, nil, false, 0, nil, workingFolderU16, &si, &pi)
	if err != nil {
		return fmt.Errorf("error executing installer: %w", err)
	}
	defer syscall.CloseHandle(pi.Process)
	defer syscall.CloseHandle(pi.Thread)
	return nil
}
