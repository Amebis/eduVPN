/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"context"
	"fmt"
	"net/http"
	"testing"

	"github.com/Amebis/eduVPN/eduvpn-windows/progress"
	"github.com/jedisct1/go-minisign"
)

func getUpdate(ctx context.Context, progress progress.ProgressIndicator) (pkg *Package, err error) {
	k, err := minisign.NewPublicKey("RWRPrtnepBgoU86pKtSnHJXBtmtJjv6T5wN2Q+P7vPgHPdr3v8kGme13")
	if err != nil {
		return nil, err
	}
	signers := []TrustedSigner{{
		PublicKey:     k,
		AlgorithmMask: PrehashedAlgorithm,
	}}
	tr := http.DefaultTransport.(*http.Transport).Clone()
	tr.RegisterProtocol("file", http.NewFileTransport(http.Dir("..\\..\\bin\\Setup")))
	client := &http.Client{Transport: tr}
	pkg, err = discoverAvailable(client, "file:///eduVPN.windows.json", signers, ctx, progress)
	if err != nil {
		return nil, fmt.Errorf("failed to load available update data: %w", err)
	}
	return pkg, nil
}

func TestCheck(t *testing.T) {
	k, err := minisign.NewPublicKey("RWRPrtnepBgoU86pKtSnHJXBtmtJjv6T5wN2Q+P7vPgHPdr3v8kGme13")
	if err != nil {
		t.Errorf("Error parsing key: %#v", err)
	}
	Check(
		"https://raw.githubusercontent.com/Amebis/eduVPN/master/bin/Setup/eduVPN.windows.json",
		[]TrustedSigner{{
			PublicKey:     k,
			AlgorithmMask: PrehashedAlgorithm,
		}},
		"{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}",
		context.Background())
}

func TestSelfUpdate(t *testing.T) {
	ctx := context.Background()
	progress := progress.Noop()
	pkg, err := getUpdate(ctx, progress)
	if err != nil {
		t.Errorf("failed to get update: %#v", err)
	}

	err = DownloadAndInstall(pkg.Uris, &pkg.Hash, pkg.Arguments, ctx, progress)
	if err != nil {
		t.Errorf("failed to download and launch installer: %#v", err)
	}
}
