/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package main

import (
	"log"

	"github.com/Amebis/eduVPN/eduvpn-windows/sessioncheck/wtsapi32"
)

func main() {
	sessionId, err := wtsapi32.SessionId()
	if err != nil {
		log.Fatalf("failed to get session ID: %v\n", err)
	}
	log.Printf("sessionId = %v\n", sessionId)
	sessions, err := wtsapi32.EnumerateSessions(wtsapi32.WTS_CURRENT_SERVER)
	if err != nil {
		log.Fatalf("failed to enumerate sessions: %v\n", err)
	}
	for i, session := range sessions {
		log.Printf("session[%d] = %+v\n", i, session)
	}
}
