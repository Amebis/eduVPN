/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package wtsapi32

import (
	"testing"
)

func TestEnumerateSessions(t *testing.T) {
	array, err := EnumerateSessions(WTS_CURRENT_SERVER_HANDLE)
	if err != nil {
		t.Errorf("Error enumerating sessions: %#v", err)
	}
	for _, session := range array {
		if session.SessionName == "" && session.UserName == "" {
			t.Errorf("Invalid session/user name: %v/%v", session.SessionName, session.UserName)
		}
	}
}

func TestQuerySessionInfo(t *testing.T) {
	sessionId, err := SessionId()
	if err != nil {
		t.Errorf("Error querying session ID: %#v", err)
	}
	_, err = SessionName(WTS_CURRENT_SERVER, sessionId)
	if err != nil {
		t.Errorf("Error querying session name: %#v", err)
	}
	_, _, err = SessionUsername(WTS_CURRENT_SERVER, sessionId)
	if err != nil {
		t.Errorf("Error querying session domain\\username: %#v", err)
	}
}
