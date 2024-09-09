/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"fmt"
	"net/http"

	"golang.org/x/sys/windows"
)

var userAgent string

func init() {
	maj, min, rev := windows.RtlGetNtVersionNumbers()
	userAgent = fmt.Sprintf("eduvpn-windows/4.1.6 Windows/%d.%d.%d", maj, min, rev)
}

func makeClient() *http.Client {
	tr := &http.Transport{}
	tr.RegisterProtocol("file", http.NewFileTransport(http.Dir("/")))
	return &http.Client{
		Transport: tr,
		CheckRedirect: func(req *http.Request, via []*http.Request) error {
			req.Header.Set("User-Agent", userAgent)
			return nil
		}}
}
