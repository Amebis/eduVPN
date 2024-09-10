/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package sessioncheck

import (
	"fmt"
	"log"
	"runtime"
	"strings"
	"syscall"
	"unsafe"

	"github.com/Amebis/eduVPN/eduvpn-windows/sessioncheck/wtsapi32"
	"github.com/lxn/win"
	"golang.org/x/sys/windows"
)

// Event is an enum of session change notifications types
type Event int

const (
	ConsoleConnect       Event = wtsapi32.WTS_CONSOLE_CONNECT
	ConsoleDisconnect    Event = wtsapi32.WTS_CONSOLE_DISCONNECT
	RemoteConnect        Event = wtsapi32.WTS_REMOTE_CONNECT
	RemoteDisconnect     Event = wtsapi32.WTS_REMOTE_DISCONNECT
	SessionReportLogon   Event = wtsapi32.WTS_SESSION_LOGON | 0x8000000
	SessionLogon         Event = wtsapi32.WTS_SESSION_LOGON
	SessionLogoff        Event = wtsapi32.WTS_SESSION_LOGOFF
	SessionLock          Event = wtsapi32.WTS_SESSION_LOCK
	SessionUnlock        Event = wtsapi32.WTS_SESSION_UNLOCK
	SessionRemoteControl Event = wtsapi32.WTS_SESSION_REMOTE_CONTROL
	SessionCreate        Event = wtsapi32.WTS_SESSION_CREATE
	SessionTerminate     Event = wtsapi32.WTS_SESSION_TERMINATE
)

// Notify is a prototype of session change notification callback
type Notify func(event Event, sessionId uint32, data any)

// Monitor to intercept session changes
type Monitor struct {
	hwnd            win.HWND // Window handle
	notify          Notify   // Notification callback
	data            any      // Additional user-provided data for the callback
	sessionId       uint32   // Current session ID
	sessionUsername string   // Current session user name
	sessionDomain   string   // Current session user domain
}

const (
	wndClassName        = "SESSION_MONITOR"
	servicesSessionName = "Services"
	consoleSessionName  = "Console"
)

var wndClassAtom win.ATOM

// InitMonitor creates a monitor to report all local foreign user sessions changes. The notify(event, sessionId, data) is called
// for all session events where: session ID is not 0 (Services), session ID & domain\user are different than running process'.
func InitMonitor(notify Notify, data any) (monitor *Monitor, err error) {
	sessionId, err2 := wtsapi32.SessionId()
	if err2 != nil {
		log.Printf("Failed to get session ID: %v\n", err2)
		err = fmt.Errorf("failed to get session ID: %w", err2)
		return
	}
	sessionUsername, sessionDomain, err2 := wtsapi32.SessionUsername(wtsapi32.WTS_CURRENT_SERVER, sessionId)
	if err2 != nil {
		log.Printf("Failed to get session domain\\username: %v\n", err2)
		err = fmt.Errorf("failed to get session domain\\username: %w", err2)
		return
	}

	log.Println("Starting session monitor")
	ch := make(chan bool)
	go func() {
		defer func() {
			ch <- false
		}()

		// Window creation and its message pump needs to run in the same thread.
		runtime.LockOSThread()

		classNameU16, err2 := syscall.UTF16PtrFromString(wndClassName)
		if err2 != nil {
			log.Printf("Failed to convert class name to UTF-16: %v\n", err2)
			err = fmt.Errorf("failed to convert class name to UTF-16: %w", err2)
			return
		}
		hInstance := win.GetModuleHandle(nil)
		if wndClassAtom == 0 {
			wc := win.WNDCLASSEX{}
			wc.CbSize = uint32(unsafe.Sizeof(wc))
			wc.LpfnWndProc = syscall.NewCallback(func(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr {
				switch msg {
				case win.WM_CREATE:
					log.Println("Creating session monitor notification window")
					cs := (*win.CREATESTRUCT)(unsafe.Pointer(lp))
					m := (*Monitor)(unsafe.Pointer(cs.CreateParams))
					win.SetWindowLongPtr(hwnd, win.GWLP_USERDATA, uintptr(unsafe.Pointer(m)))
				case win.WM_DESTROY:
					log.Println("Destroying session monitor notification window")
					m := (*Monitor)(unsafe.Pointer(win.GetWindowLongPtr(hwnd, win.GWLP_USERDATA)))
					win.GlobalFree(win.HGLOBAL(unsafe.Pointer(m)))
				case wtsapi32.WM_WTSSESSION_CHANGE:
					log.Printf("Session change event received: sessionId=%v, event=%v\n", uint32(lp), Event(wp))
					m := (*Monitor)(unsafe.Pointer(win.GetWindowLongPtr(hwnd, win.GWLP_USERDATA)))
					sessionId := uint32(lp)
					if sessionId == m.sessionId {
						log.Println("Ignoring own session by ID")
						return 0
					}
					sessionName, err := wtsapi32.SessionName(wtsapi32.WTS_CURRENT_SERVER, sessionId)
					if err == nil && (strings.EqualFold(sessionName, servicesSessionName) || strings.EqualFold(sessionName, consoleSessionName)) {
						log.Println("Ignoring system session")
						return 0
					}
					sessionUsername, sessionDomain, err := wtsapi32.SessionUsername(wtsapi32.WTS_CURRENT_SERVER, sessionId)
					if err == nil && strings.EqualFold(sessionUsername, m.sessionUsername) && strings.EqualFold(sessionDomain, m.sessionDomain) {
						log.Println("Ignoring own session by domain\\username")
						return 0
					}
					log.Println("Notifying listener")
					m.notify(Event(wp), sessionId, m.data)
					return 0
				}
				return win.DefWindowProc(hwnd, msg, wp, lp)
			})
			wc.HInstance = hInstance
			wc.LpszClassName = classNameU16
			if wndClassAtom = win.RegisterClassEx(&wc); wndClassAtom == 0 {
				err2 := windows.Errno(win.GetLastError())
				log.Printf("Failed to register window class: %v\n", err2)
				err = fmt.Errorf("failed to register window class: %w", err2)
				return
			}
		}
		monitor = (*Monitor)(unsafe.Pointer(uintptr(win.GlobalAlloc(win.GPTR, unsafe.Sizeof(Monitor{})))))
		monitor.notify = notify
		monitor.data = data
		monitor.sessionId = sessionId
		monitor.sessionUsername = sessionUsername
		monitor.sessionDomain = sessionDomain
		hwnd := win.CreateWindowEx(
			0, // dwExStyle
			(*uint16)(unsafe.Pointer(uintptr(wndClassAtom))), // lpClassName
			classNameU16, // lpWindowName
			0,            // dwStyle
			0, 0, 0, 0,   // X, Y, nWidth, nHeight
			win.HWND_MESSAGE,        // hWndParent
			win.HMENU(0),            // hMenu
			hInstance,               // hInstance
			unsafe.Pointer(monitor)) // lpParam
		if hwnd == win.HWND(0) {
			err = windows.Errno(win.GetLastError())
			log.Printf("Failed to create window: %v\n", err)
			win.GlobalFree(win.HGLOBAL(unsafe.Pointer(monitor)))
			monitor = nil
			err = fmt.Errorf("failed to create window: %w", err)
			return
		}
		defer func() {
			if err != nil {
				win.DestroyWindow(hwnd)
				monitor = nil
			}
		}()
		monitor.hwnd = hwnd

		err2 = wtsapi32.RegisterSessionNotification(wtsapi32.WTS_CURRENT_SERVER, windows.HWND(hwnd), wtsapi32.NOTIFY_FOR_ALL_SESSIONS)
		if err2 != nil {
			log.Printf("Failed to register session notification: %v\n", err2)
			monitor = nil
			err = fmt.Errorf("failed to register session notification: %w", err2)
			return
		}
		defer func() {
			if err != nil {
				wtsapi32.UnregisterSessionNotification(wtsapi32.WTS_CURRENT_SERVER, windows.HWND(hwnd))
				monitor = nil
			}
		}()

		log.Println("Enumerating existing sessions")
		sessions, err2 := wtsapi32.EnumerateSessions(wtsapi32.WTS_CURRENT_SERVER)
		if err2 != nil {
			log.Printf("Failed to enumerate sessions: %v\n", err2)
			err = fmt.Errorf("failed to enumerate sessions: %w", err2)
			return
		}
		currentSession := sessions[sessionId]
		for _, session := range sessions {
			log.Printf("Session: %+v\n", session)
			if session.SessionId == sessionId {
				log.Println("Ignoring own session by ID")
				continue
			}
			if strings.EqualFold(session.SessionName, servicesSessionName) || strings.EqualFold(session.SessionName, consoleSessionName) {
				log.Println("Ignoring system session")
				continue
			}
			if strings.EqualFold(session.UserName, currentSession.UserName) && strings.EqualFold(session.DomainName, currentSession.DomainName) {
				log.Println("Ignoring own session by domain\\username")
				continue
			}
			log.Println("Notifying listener")
			notify(SessionReportLogon, session.SessionId, data)
		}

		ch <- true
		var m win.MSG
		for win.GetMessage(&m, 0, 0, 0) != 0 {
			win.TranslateMessage(&m)
			win.DispatchMessage(&m)
		}
	}()

	<-ch
	return
}

// Close destroys monitor
func (m *Monitor) Close() {
	log.Println("Closing session monitor")
	wtsapi32.UnregisterSessionNotification(wtsapi32.WTS_CURRENT_SERVER, windows.HWND(m.hwnd))
	win.DestroyWindow(m.hwnd)
}
