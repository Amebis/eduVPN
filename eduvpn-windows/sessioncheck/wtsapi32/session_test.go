/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package wtsapi32

import (
	"testing"
)

func TestSession(t *testing.T) {
	array, err := EnumerateSessions(WTS_CURRENT_SERVER_HANDLE)
	if err != nil {
		t.Errorf("Error enumerating sessions: %#v", err)
	}
	for _, session := range array {
		if session.SessionName == "" {
			t.Errorf("Invalid session name: %v", session.SessionName)
		}
	}
}
