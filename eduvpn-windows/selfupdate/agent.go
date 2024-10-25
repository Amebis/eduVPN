/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"net/http"

	"golang.org/x/sys/windows"
)

var userAgent string

func init() {
	userAgent = "eduvpn-windows/4.2.2"

	maj, min, rev := windows.RtlGetNtVersionNumbers()
	var winVer string
	if maj > 10 || maj == 10 && min >= 0 && rev >= 22000 {
		winVer = "11"
	} else if maj > 6 {
		winVer = "10"
	} else if maj == 6 && min >= 2 {
		winVer = "8"
	} else if maj == 6 && min >= 1 {
		winVer = "7"
	}
	if len(winVer) > 0 {
		userAgent += " Windows/" + winVer
	}
}

func makeClient() *http.Client {
	tr := http.DefaultTransport.(*http.Transport).Clone()
	return &http.Client{
		Transport: tr,
		CheckRedirect: func(req *http.Request, via []*http.Request) error {
			req.Header.Set("User-Agent", userAgent)
			return nil
		}}
}
