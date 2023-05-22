/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package wtsapi32

import (
	"unsafe"

	"golang.org/x/sys/windows"
)

type WTS_TYPE_CLASS int32

const (
	WTSTypeProcessInfoLevel0 WTS_TYPE_CLASS = iota
	WTSTypeProcessInfoLevel1
	WTSTypeSessionInfoLevel1
)

//sys wtsFreeMemoryExW(WTSTypeClass WTS_TYPE_CLASS, pMemory uintptr, NumberOfEntries uint32) (err error) = wtsapi32.WTSFreeMemoryExW

type WTS_CONNECTSTATE_CLASS int32

const (
	WTSActive       WTS_CONNECTSTATE_CLASS = iota // User logged on to WinStation
	WTSConnected                                  // WinStation connected to client
	WTSConnectQuery                               // In the process of connecting to client
	WTSShadow                                     // Shadowing another WinStation
	WTSDisconnected                               // WinStation logged on without client
	WTSIdle                                       // Waiting for client to connect
	WTSListen                                     // WinStation is listening for connection
	WTSReset                                      // WinStation is being reset
	WTSDown                                       // WinStation is down due to error
	WTSInit                                       // WinStation in initialization
)

const WTS_CURRENT_SERVER_HANDLE windows.Handle = 0

type WTS_SESSION_INFO_1W struct {
	ExecEnvId    uint32
	State        WTS_CONNECTSTATE_CLASS
	SessionId    uint32
	pSessionName *uint16
	pHostName    *uint16
	pUserName    *uint16
	pDomainName  *uint16
	pFarmName    *uint16
}

type Session struct {
	ExecEnvId   uint32
	State       WTS_CONNECTSTATE_CLASS
	SessionId   uint32
	SessionName string
	HostName    string
	UserName    string
	DomainName  string
	FarmName    string
}

//sys	wtsEnumerateSessionsExW(hServer windows.Handle, pLevel *uint32, Filter uint32, ppSessionInfo **WTS_SESSION_INFO_1W, pCount *uint32) (err error) = wtsapi32.WTSEnumerateSessionsExW

func EnumerateSessions(server windows.Handle) ([]Session, error) {
	pLevel := uint32(1)
	var sessionsPointer *WTS_SESSION_INFO_1W
	var count uint32
	err := wtsEnumerateSessionsExW(server, &pLevel, 0, &sessionsPointer, &count)
	if err != nil {
		return nil, err
	}
	defer wtsFreeMemoryExW(WTSTypeSessionInfoLevel1, uintptr(unsafe.Pointer(sessionsPointer)), count)
	result := make([]Session, 0, count)
	for _, session := range unsafe.Slice(sessionsPointer, count) {
		result = append(result, Session{
			ExecEnvId:   session.ExecEnvId,
			State:       session.State,
			SessionId:   session.SessionId,
			SessionName: windows.UTF16PtrToString(session.pSessionName),
			HostName:    windows.UTF16PtrToString(session.pHostName),
			UserName:    windows.UTF16PtrToString(session.pUserName),
			DomainName:  windows.UTF16PtrToString(session.pDomainName),
			FarmName:    windows.UTF16PtrToString(session.pFarmName),
		})
	}
	return result, nil
}
