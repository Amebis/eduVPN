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

const WTS_CURRENT_SERVER windows.Handle = windows.Handle(0)

const (
	NOTIFY_FOR_ALL_SESSIONS uint32 = 1
	NOTIFY_FOR_THIS_SESSION uint32 = 0
)

const WM_WTSSESSION_CHANGE uint32 = 0x02b1

const (
	WTS_CONSOLE_CONNECT        = 0x1
	WTS_CONSOLE_DISCONNECT     = 0x2
	WTS_REMOTE_CONNECT         = 0x3
	WTS_REMOTE_DISCONNECT      = 0x4
	WTS_SESSION_LOGON          = 0x5
	WTS_SESSION_LOGOFF         = 0x6
	WTS_SESSION_LOCK           = 0x7
	WTS_SESSION_UNLOCK         = 0x8
	WTS_SESSION_REMOTE_CONTROL = 0x9
	WTS_SESSION_CREATE         = 0xa
	WTS_SESSION_TERMINATE      = 0xb
)

//sys RegisterSessionNotification(server windows.Handle, hwnd windows.HWND, flags uint32) (err error) = wtsapi32.WTSRegisterSessionNotificationEx
//sys UnregisterSessionNotification(server windows.Handle, hwnd windows.HWND) (err error) = wtsapi32.WTSUnRegisterSessionNotificationEx

type WTS_INFO_CLASS uint32

const (
	WTSInitialProgram WTS_INFO_CLASS = iota
	WTSApplicationName
	WTSWorkingDirectory
	WTSOEMId
	WTSSessionId
	WTSUserName
	WTSWinStationName
	WTSDomainName
	WTSConnectState
	WTSClientBuildNumber
	WTSClientName
	WTSClientDirectory
	WTSClientProductId
	WTSClientHardwareId
	WTSClientAddress
	WTSClientDisplay
	WTSClientProtocolType
	WTSIdleTime
	WTSLogonTime
	WTSIncomingBytes
	WTSOutgoingBytes
	WTSIncomingFrames
	WTSOutgoingFrames
	WTSClientInfo
	WTSSessionInfo
	WTSSessionInfoEx
	WTSConfigInfo
	WTSValidationInfo // Info Class value used to fetch Validation Information through the WTSQuerySessionInformation
	WTSSessionAddressV4
	WTSIsRemoteSession
)

const WTS_CURRENT_SESSION uint32 = 0xffffffff

//sys wtsFreeMemory(pMemory uintptr) = wtsapi32.WTSFreeMemory
//sys wtsQuerySessionInformation(hServer windows.Handle, SessionId uint32, WTSInfoClass WTS_INFO_CLASS, ppBuffer *uintptr, pBytesReturned *uint32) (err error) = wtsapi32.WTSQuerySessionInformationW

func querySessionInformationULONG(server windows.Handle, sessionId uint32, infoClass WTS_INFO_CLASS) (uint32, error) {
	var addr uintptr
	var size uint32
	err := wtsQuerySessionInformation(server, sessionId, infoClass, &addr, &size)
	if err != nil {
		return 0, err
	}
	defer wtsFreeMemory(addr)
	return *(*uint32)(unsafe.Pointer(addr)), nil
}

func SessionId() (uint32, error) {
	id, err := querySessionInformationULONG(WTS_CURRENT_SERVER, WTS_CURRENT_SESSION, WTSSessionId)
	if err == nil {
		return id, nil
	}
	err = windows.ProcessIdToSessionId(windows.GetCurrentProcessId(), &id)
	if err != nil {
		return 0, err
	}
	return id, nil
}
