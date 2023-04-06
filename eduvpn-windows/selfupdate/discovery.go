/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"

	"github.com/Amebis/eduVPN/eduvpn-windows/progress"
)

// Available self-update description
type Package struct {
	Arguments    string   `json:"arguments"`     // Update file command line arguments
	Uris         []string `json:"uri"`           // List of update file download URIs
	Version      *Version `json:"version"`       // Available product version
	ChangelogUri string   `json:"changelog_uri"` // Product changelog URI
	Hash         Hash     `json:"hash-sha256"`   // Update file SHA-256 hash
}

// discoverAvailable securely loads available self-update description.
func discoverAvailable(client *http.Client, url string, allowedSigners []TrustedSigner, ctx context.Context, progress progress.ProgressIndicator) (pkg *Package, err error) {
	progress.SetProgress(0)
	req, err := http.NewRequestWithContext(ctx, "GET", url, nil)
	if err != nil {
		return nil, fmt.Errorf("error creating '%s' request: %w", url, err)
	}
	resp, err := client.Do(req)
	if err != nil {
		return nil, fmt.Errorf("error downloading '%s': %w", url, err)
	}
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("error downloading '%s', status: %v", url, resp.StatusCode)
	}
	content, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("error reading '%s': %w", url, err)
	}
	progress.SetProgress(0.4)

	if allowedSigners != nil {
		reqSig, err := http.NewRequestWithContext(ctx, "GET", url+".minisig", nil)
		if err != nil {
			return nil, fmt.Errorf("error creating '%s' signature request: %w", url, err)
		}
		respSig, err := client.Do(reqSig)
		if err != nil {
			return nil, fmt.Errorf("error downloading '%s' signature: %w", url, err)
		}
		defer respSig.Body.Close()
		if respSig.StatusCode != http.StatusOK {
			return nil, fmt.Errorf("error downloading '%s' signature, status: %v", url, respSig.StatusCode)
		}
		bodyBytes, err := io.ReadAll(respSig.Body)
		if err != nil {
			return nil, fmt.Errorf("error reading '%s': %w", url, err)
		}
		contentSig := string(bodyBytes)
		if err = verifySignature(content, contentSig, allowedSigners); err != nil {
			return nil, fmt.Errorf("failed to verify '%s' signature: %w", url, err)
		}
	}
	progress.SetProgress(0.8)

	err = json.Unmarshal(content, &pkg)
	if err != nil {
		return nil, fmt.Errorf("error parsing '%s': %w", url, err)
	}
	progress.SetProgress(1.0)
	return pkg, nil
}
