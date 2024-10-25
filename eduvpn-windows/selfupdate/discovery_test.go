/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"context"
	"net/http"
	"testing"

	"github.com/Amebis/eduVPN/eduvpn-windows/progress"
	"github.com/jedisct1/go-minisign"
)

func TestDiscovery(t *testing.T) {
	var err error
	var signers []TrustedSigner = make([]TrustedSigner, 0, 4)
	for _, keyStr := range []string{
		"RWTbIHtCWd57+tcyjPSn30I7xhPGow35NR7wBzj3qDm13TE6YFk2L2M8",
		"RWQHk3PWKr6pfbb7MSTJrhHrPgz3/BYk8uvwFoScHK5LYZhC2oNXnW16",
		"RWQ68Y5/b8DED0TJ41B1LE7yAvkmavZWjDwCBUuC+Z2pP9HaSawzpEDA",
	} {
		k, err := minisign.NewPublicKey(keyStr)
		if err != nil {
			t.Errorf("Error parsing key: %#v", err)
		}
		signers = append(signers, TrustedSigner{
			PublicKey:     k,
			AlgorithmMask: AnyAlgorithm,
		})
	}

	tr := http.DefaultTransport.(*http.Transport).Clone()
	tr.RegisterProtocol("file", http.NewFileTransport(http.Dir("..\\..\\bin\\Setup")))
	client := &http.Client{Transport: tr}
	ctx := context.Background()
	progress := progress.Noop()

	_, err = discoverAvailable(client, "file:///foo.bar/autodiscover.json", nil, ctx, progress)
	if err == nil {
		t.Error("Error was expected")
	}
	_, err = discoverAvailable(client, "file:///foo.bar/autodiscover.json", signers, ctx, progress)
	if err == nil {
		t.Error("Error was expected")
	}
	_, err = discoverAvailable(client, "file:///eduVPN.windows.json", signers, ctx, progress)
	if err == nil {
		t.Error("Error was expected")
	}
	k, err := minisign.NewPublicKey("RWRPrtnepBgoU86pKtSnHJXBtmtJjv6T5wN2Q+P7vPgHPdr3v8kGme13")
	if err != nil {
		t.Errorf("Error parsing key: %#v", err)
	}
	signers = append(signers, TrustedSigner{
		PublicKey:     k,
		AlgorithmMask: PrehashedAlgorithm,
	})
	_, err = discoverAvailable(client, "file:///eduVPN.windows.json", signers, ctx, progress)
	if err != nil {
		t.Errorf("Error loading available update data: %#v", err)
	}
}
