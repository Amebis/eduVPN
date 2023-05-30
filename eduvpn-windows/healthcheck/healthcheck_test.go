/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package healthcheck

import (
	"context"
	"testing"
	"unsafe"

	"github.com/lxn/win"
)

func TestHealthCheck(t *testing.T) {
	hr := win.CoInitializeEx(nil, win.COINIT_APARTMENTTHREADED|win.COINIT_SPEED_OVER_MEMORY)
	if win.FAILED(hr) {
		t.Errorf("Error initializing COM: %#v", hr)
	}
	defer win.CoUninitialize()
	var session3 *win.IUpdateSession3
	hr = win.CoCreateInstance(&win.CLSID_UpdateSession, nil, win.CLSCTX_INPROC_SERVER, &win.IID_IUpdateSession3, (*unsafe.Pointer)(unsafe.Pointer(&session3)))
	if win.FAILED(hr) {
		t.Errorf("Failed to create UpdateSession: %#v", hr)
	}
	defer session3.Release()

	ctx := context.Background()
	_, err := MostRecentUpdateTimestamp(session3, ctx)
	if err != nil {
		t.Errorf("Failed to get last update timestamp: %#v", err)
	}

	history, err := UpdateHistory(session3, ctx)
	if err != nil {
		t.Errorf("Failed to enumerate update history: %#v", err)
	}
	defer ReleaseIUpdateHistoryEntries(history)
}
