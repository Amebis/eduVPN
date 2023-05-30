/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package sessioncheck

import (
	"fmt"
	"runtime"
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
	hwnd   win.HWND // Window handle
	notify Notify   // Notification callback
	data   any      // Additional user-provided data for the callback
}

const wndClassName = "SESSION_MONITOR"

var wndClassAtom win.ATOM

// InitMonitor creates a monitor to reports local user sessions changes.
func InitMonitor(notify Notify, data any) (monitor *Monitor, err error) {
	ch := make(chan bool)
	go func() {
		defer func() {
			ch <- false
		}()

		// Window creation and its message pump needs to run in the same thread.
		runtime.LockOSThread()

		classNameU16, err2 := syscall.UTF16PtrFromString(wndClassName)
		if err2 != nil {
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
					cs := (*win.CREATESTRUCT)(unsafe.Pointer(lp))
					m := (*Monitor)(unsafe.Pointer(cs.CreateParams))
					win.SetWindowLongPtr(hwnd, win.GWLP_USERDATA, uintptr(unsafe.Pointer(m)))
				case win.WM_DESTROY:
					m := (*Monitor)(unsafe.Pointer(win.GetWindowLongPtr(hwnd, win.GWLP_USERDATA)))
					win.GlobalFree(win.HGLOBAL(unsafe.Pointer(m)))
				case wtsapi32.WM_WTSSESSION_CHANGE:
					m := (*Monitor)(unsafe.Pointer(win.GetWindowLongPtr(hwnd, win.GWLP_USERDATA)))
					m.notify(Event(wp), uint32(lp), m.data)
					return 0
				}
				return win.DefWindowProc(hwnd, msg, wp, lp)
			})
			wc.HInstance = hInstance
			wc.LpszClassName = classNameU16
			if wndClassAtom = win.RegisterClassEx(&wc); wndClassAtom == 0 {
				err = fmt.Errorf("failed to register window class: %w", windows.Errno(win.GetLastError()))
				return
			}
		}
		monitor = (*Monitor)(unsafe.Pointer(uintptr(win.GlobalAlloc(win.GPTR, unsafe.Sizeof(Monitor{})))))
		monitor.notify = notify
		monitor.data = data
		hwnd := win.CreateWindowEx(
			0, // dwExStyle
			(*uint16)(unsafe.Pointer(uintptr(wndClassAtom))), // lpClassName
			classNameU16, // lpWindowName
			0,            // dwStyle
			0, 0, 0, 0,   // X, Y, nWidth, nHeight,
			win.HWND_MESSAGE,        // hWndParent
			win.HMENU(0),            // hMenu
			hInstance,               // hInstance
			unsafe.Pointer(monitor)) // lpParam
		if hwnd == win.HWND(0) {
			win.GlobalFree(win.HGLOBAL(unsafe.Pointer(monitor)))
			monitor = nil
			err = fmt.Errorf("failed to create window: %w", windows.Errno(win.GetLastError()))
			return
		}
		defer func() {
			if err != nil {
				win.DestroyWindow(hwnd)
				monitor = nil
			}
		}()
		monitor.hwnd = hwnd

		err = wtsapi32.RegisterSessionNotification(wtsapi32.WTS_CURRENT_SERVER, windows.HWND(hwnd), wtsapi32.NOTIFY_FOR_ALL_SESSIONS)
		if err != nil {
			monitor = nil
			err = fmt.Errorf("failed to register session notification: %w", err)
			return
		}
		defer func() {
			if err != nil {
				wtsapi32.UnregisterSessionNotification(wtsapi32.WTS_CURRENT_SERVER, windows.HWND(hwnd))
				monitor = nil
			}
		}()

		sessions, err2 := wtsapi32.EnumerateSessions(wtsapi32.WTS_CURRENT_SERVER)
		if err2 != nil {
			err = fmt.Errorf("failed to enumerate sessions: %w", err2)
			return
		}
		sessionId, err2 := wtsapi32.SessionId()
		if err2 != nil {
			err = fmt.Errorf("failed to get session ID: %w", err2)
			return
		}
		for _, session := range sessions {
			if session.SessionId != 0 && session.SessionId != sessionId {
				notify(SessionReportLogon, session.SessionId, data)
			}
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
func (m *Monitor) Close() error {
	wtsapi32.UnregisterSessionNotification(wtsapi32.WTS_CURRENT_SERVER, windows.HWND(m.hwnd))
	if !win.DestroyWindow(m.hwnd) {
		return fmt.Errorf("failed to destroy window: %w", windows.Errno(win.GetLastError()))
	}
	return nil
}