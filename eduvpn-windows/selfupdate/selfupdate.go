/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"context"
	"crypto/sha256"
	"crypto/subtle"
	"encoding/json"
	"encoding/xml"
	"fmt"
	"hash"
	"io"
	"log"
	"mime"
	"net/http"
	"net/url"
	"os"
	"path"
	"strings"
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
		tr := &http.Transport{}
		tr.RegisterProtocol("file", http.NewFileTransport(http.Dir("/")))
		client := &http.Client{Transport: tr}
		pkg, err := discoverAvailable(client, url, allowedSigners, ctx, progress.Noop())
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
	tr := &http.Transport{}
	tr.RegisterProtocol("file", http.NewFileTransport(http.Dir("/")))
	client := &http.Client{Transport: tr}
	for _, u := range urls {
		select {
		case <-ctx.Done():
			return "", nil, ctx.Err()
		default:
		}

		log.Printf("Downloading installer file %s", u)

		var req *http.Request
		req, err = http.NewRequestWithContext(ctx, "GET", u, nil)
		if err != nil {
			continue
		}
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
		filename = path.Join(folder, filename)

		var fileW *os.File
		fileW, err = os.OpenFile(filename, os.O_WRONLY|os.O_CREATE, 0777)
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
			os.Remove(filename)
			continue
		}
		fileW.Truncate(counter.offset)
		if subtle.ConstantTimeCompare(counter.hash.Sum(nil), unsafe.Slice(&hash[0], 32)) == 0 {
			fileW.Close()
			os.Remove(filename)
			err = fmt.Errorf("file content different than expected: %s", u)
			continue
		}

		// Reopen file as read-only to allow execution of the file, but keep the file locked for writing.
		file, err = os.OpenFile(filename, os.O_RDONLY, 0777)
		if err != nil {
			continue
		}
		fileW.Close()
		return filename, file, nil
	}
	if err == nil {
		err = fmt.Errorf("resource not available")
	}
	return "", nil, err
}

// generateLauncherFile creates installer launch&cleanup script
// After installer process is spawned, our process will end - to release files in use. But who is going to cleanup installer files then?
// To launch the installer and do the cleanup, we use WScript, because:
// 1. Less flicker: WScript as opposed to .bat or CScript does not open excessive terminal window.
// 2. Cleanup: A separate script process can launch the installer, wait for it to finish and cleanup after - without keeping any relevant files in use.
func generateLauncherFile(workingFolder, installerFilename, installerArguments string) (filename string, file *os.File, err error) {
	filename = path.Join(workingFolder, "Setup.wsf")

	type Reference struct {
		XMLName xml.Name `xml:"reference"`
		Object  string   `xml:"object,attr,omitempty"`
	}
	type Script struct {
		XMLName  xml.Name `xml:"script"`
		Language string   `xml:"language,attr,omitempty"`
		Value    string   `xml:",chardata"`
	}
	type Job struct {
		XMLName    xml.Name `xml:"job"`
		References []Reference
		Scripts    []Script
	}
	type Package struct {
		XMLName xml.Name `xml:"package"`
		Jobs    []Job
	}

	script := strings.Builder{}
	script.WriteString("var wsh = WScript.CreateObject(\"WScript.Shell\");\n")
	commandLine := "\"" + installerFilename + "\""
	if installerArguments != "" {
		commandLine = commandLine + " " + installerArguments
	}
	esc, err := json.Marshal(commandLine)
	if err != nil {
		return "", nil, fmt.Errorf("failed to escape command line: %w", err)
	}
	script.WriteString("wsh.Run(" + string(esc) + ", 0, true);\n")
	script.WriteString("var fso = WScript.CreateObject(\"Scripting.FileSystemObject\");\n")
	esc, err = json.Marshal(installerFilename)
	if err != nil {
		return "", nil, fmt.Errorf("failed to escape installer filename: %w", err)
	}
	script.WriteString("try { fso.DeleteFile(" + string(esc) + ", true); } catch (err) {}\n")
	esc, err = json.Marshal(filename)
	if err != nil {
		return "", nil, fmt.Errorf("failed to escape updater filename: %w", err)
	}
	script.WriteString("try { fso.DeleteFile(" + string(esc) + ", true); } catch (err) {}\n")
	script.WriteString("try { fso.DeleteFile(" + string(esc) + ", true); } catch (err) {}\n")
	esc, err = json.Marshal(workingFolder)
	if err != nil {
		return "", nil, fmt.Errorf("failed to escape working folder name: %w", err)
	}
	script.WriteString("try { fso.DeleteFolder(" + string(esc) + ", true); } catch (err) {}\n")
	p := &Package{
		Jobs: []Job{
			{
				References: []Reference{
					{Object: "WScript.Shell"},
					{Object: "Scripting.FileSystemObject"},
				},
				Scripts: []Script{
					{
						Language: "JScript",
						Value:    script.String(),
					},
				},
			},
		},
	}
	xmlstring, err := xml.MarshalIndent(p, "", "\t")
	if err != nil {
		return "", nil, fmt.Errorf("failed to generate launcher XML: %w", err)
	}
	xmlstring = []byte(xml.Header + string(xmlstring))

	fileW, err := os.OpenFile(filename, os.O_WRONLY|os.O_CREATE, 0777)
	if err != nil {
		return "", nil, fmt.Errorf("failed to create launcher: %w", err)
	}
	_, err = fileW.Write(xmlstring)
	if err != nil {
		fileW.Close()
		os.Remove(filename)
		return "", nil, fmt.Errorf("failed to write launcher: %w", err)
	}
	// Reopen file as read-only to allow execution of the file, but keep the file locked for writing.
	file, err = os.OpenFile(filename, os.O_RDONLY, 0777)
	if err != nil {
		fileW.Close()
		os.Remove(filename)
		return "", nil, fmt.Errorf("failed to reopen launcher: %w", err)
	}
	fileW.Close()
	return filename, file, nil
}

// DownloadAndInstall securely downloads installer file, prepares and launches installer
func DownloadAndInstall(urls []string, hash *Hash, installerArguments string, ctx context.Context, progress progress.ProgressIndicator) error {
	workingFolder, err := os.MkdirTemp(os.TempDir(), "SURF")
	if err != nil {
		return fmt.Errorf("failed to create temporary folder: %w", err)
	}

	installerFilename, installerFile, err := downloadInstaller(workingFolder, urls, hash, ctx, progress)
	if err != nil {
		return fmt.Errorf("error downloading installer: %w", err)
	}
	defer installerFile.Close()
	updaterFilename, updaterFile, err := generateLauncherFile(workingFolder, installerFilename, installerArguments)
	if err != nil {
		os.Remove(installerFilename)
		return fmt.Errorf("error generating launcher: %w", err)
	}
	defer updaterFile.Close()

	proc, err := os.StartProcess(
		os.ExpandEnv("$WINDIR\\System32\\wscript.exe"),
		[]string{"wscript.exe", updaterFilename},
		&os.ProcAttr{
			Dir:   workingFolder,
			Files: []*os.File{nil, nil, nil},
		})
	if err != nil {
		return fmt.Errorf("error executing updater: %w", err)
	}
	proc.Release()
	return nil
}
