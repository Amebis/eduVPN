/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package main

/*
#include <stdint.h>
#include <stdlib.h>

typedef void (*set_progress)(float current);
static void call_set_progress(set_progress callback, float value) { callback(value); }
*/
import "C"

import (
	"context"
	"encoding/json"
	"fmt"
	"runtime/cgo"
	"strconv"
	"strings"
	"unsafe"

	"github.com/Amebis/eduVPN/eduvpn-windows/healthcheck"
	"github.com/Amebis/eduVPN/eduvpn-windows/selfupdate"
	"github.com/jedisct1/go-minisign"
	"github.com/lxn/win"
)

type mycontext struct {
	ctx    context.Context
	cancel context.CancelFunc
}

//export make_context
func make_context() C.uintptr_t {
	ctx := context.Background()
	c := &mycontext{}
	c.ctx, c.cancel = context.WithCancel(ctx)
	return C.uintptr_t(cgo.NewHandle(c))
}

//export free_context
func free_context(ctx C.uintptr_t) {
	if ctx != 0 {
		cgo.Handle(ctx).Delete()
	}
}

func goContext(ctx C.uintptr_t) context.Context {
	if ctx != 0 {
		return cgo.Handle(ctx).Value().(*mycontext).ctx
	}
	return context.Background()
}

//export cancel_context
func cancel_context(ctx C.uintptr_t) {
	if ctx != 0 {
		cgo.Handle(ctx).Value().(mycontext).cancel()
	}
}

//export free_string
func free_string(addr *C.char) {
	C.free(unsafe.Pointer(addr))
}

func goStringZ(strz *C.char) []string {
	tab := make([]string, 0, 16)
	for *strz != 0 {
		tab = append(tab, C.GoString(strz))
		for *strz != 0 {
			strz = (*C.char)(unsafe.Add(unsafe.Pointer(strz), 1))
		}
		strz = (*C.char)(unsafe.Add(unsafe.Pointer(strz), 1))
	}
	return tab
}

func cError(err error) *C.char {
	return C.CString(err.Error())
}

//export check_selfupdate
func check_selfupdate(url *C.char, allowedSigners *C.char, productId *C.char, ctx C.uintptr_t) (pkg *C.char, err *C.char) {
	_allowedSigners := goStringZ(allowedSigners)
	signers := make([]selfupdate.TrustedSigner, 0, len(_allowedSigners))
	for _, str := range _allowedSigners {
		v := strings.Split(str, "|")
		k, err := minisign.NewPublicKey(v[0])
		if err != nil {
			return nil, cError(err)
		}
		s := selfupdate.TrustedSigner{PublicKey: k}
		if len(v) > 1 {
			x, err := strconv.Atoi(v[1])
			if err != nil {
				return nil, cError(err)
			}
			s.AlgorithmMask = selfupdate.AlgorithmMask(x)
		} else {
			s.AlgorithmMask = selfupdate.AnyAlgorithm
		}
		signers = append(signers, s)
	}
	p, err2 := selfupdate.Check(C.GoString(url), signers, C.GoString(productId), goContext(ctx))
	if err2 != nil {
		return nil, cError(err2)
	}
	pStr, err2 := json.Marshal(p)
	if err2 != nil {
		return nil, cError(fmt.Errorf("failed converting to JSON: %w", err2))
	}
	return C.CString(string(pStr)), nil
}

type progressIndicator struct {
	setProgress C.set_progress
}

// SetProgress sets current progress
func (p progressIndicator) SetProgress(value float32) {
	if p.setProgress != nil {
		C.call_set_progress(p.setProgress, C.float(value))
	}
}

//export download_and_install_selfupdate
func download_and_install_selfupdate(
	urls *C.char,
	hash *byte,
	installerArguments *C.char,
	ctx C.uintptr_t,
	setProgress C.set_progress) (err *C.char) {
	err2 := selfupdate.DownloadAndInstall(goStringZ(urls), (*selfupdate.Hash)(unsafe.Pointer(hash)), C.GoString(installerArguments), goContext(ctx), progressIndicator{setProgress: setProgress})
	if err2 != nil {
		return cError(err2)
	}
	return nil
}

//export get_last_update_timestamp
func get_last_update_timestamp(ctx C.uintptr_t) (timestamp C.int64_t, err *C.char) {
	var session3 *win.IUpdateSession3
	hr := win.CoCreateInstance(&win.CLSID_UpdateSession, nil, win.CLSCTX_INPROC_SERVER, &win.IID_IUpdateSession3, (*unsafe.Pointer)(unsafe.Pointer(&session3)))
	if win.FAILED(hr) {
		return 0, cError(fmt.Errorf("failed to create UpdateSession: %#v", hr))
	}
	defer session3.Release()
	t, err2 := healthcheck.MostRecentUpdateTimestamp(session3, goContext(ctx))
	if err2 != nil {
		return 0, cError(err2)
	}
	return C.int64_t(t.Unix()), nil
}
