/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package main

import (
	"fmt"

	"github.com/Amebis/eduVPN/eduvpn-windows/sessioncheck/wtsapi32"
)

func main() {
	sessionId, err := wtsapi32.SessionId()
	if err != nil {
		fmt.Printf("failed to get session ID: %v\n", err)
		return
	}
	fmt.Printf("sessionId = %v\n", sessionId)
	sessions, err := wtsapi32.EnumerateSessions(wtsapi32.WTS_CURRENT_SERVER)
	if err != nil {
		fmt.Printf("failed to enumerate sessions: %v\n", err)
		return
	}
	for i, session := range sessions {
		fmt.Printf("session[%d] = %+v\n", i, session)
	}
}
