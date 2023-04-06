/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package healthcheck

import (
	"context"
	"fmt"
	"math"
	"time"

	"github.com/lxn/win"
)

const (
	secondsPerMinute = 60
	secondsPerHour   = 60 * secondsPerMinute
	secondsPerDay    = 24 * secondsPerHour
)

var variantDATEToUnixDiff = time.Date(1970, 1, 1, 0, 0, 0, 0, time.UTC).Sub(time.Date(1899, 12, 30, 0, 0, 0, 0, time.UTC)).Seconds()

func MostRecentUpdateTimestamp(session3 *win.IUpdateSession3, ctx context.Context) (t time.Time, err error) {
	criteria := win.SysAllocString("")
	if criteria == nil {
		panic("SysAllocString failed")
	}
	defer win.SysFreeString(criteria)
	col, hr := session3.QueryHistory(criteria, 0, 0xffff)
	if win.FAILED(hr) {
		return time.Time{}, fmt.Errorf("failed to query update history: %x", hr)
	}
	defer col.Release()
	count, hr := col.Count()
	if win.FAILED(hr) {
		return time.Time{}, fmt.Errorf("failed to query update history count: %x", hr)
	}
	for i := int32(0); i < count; i++ {
		select {
		case <-ctx.Done():
			return time.Time{}, ctx.Err()
		default:
		}
		entry, hr := col.Item(i)
		if win.FAILED(hr) {
			return time.Time{}, fmt.Errorf("failed to query update: %x", hr)
		}
		defer entry.Release()
		if op, hr := entry.Operation(); win.FAILED(hr) || op != win.UOInstallation {
			continue
		}
		if rc, hr := entry.ResultCode(); win.FAILED(hr) || rc != win.ORCSucceeded && rc != win.ORCSucceededWithErrors {
			continue
		}
		date, hr := entry.Date()
		if win.FAILED(hr) {
			continue
		}
		date *= secondsPerDay
		date -= variantDATEToUnixDiff
		sec, subsec := math.Modf(date)
		nsec := math.Trunc(subsec * 1e+9)
		t2 := time.Unix(int64(sec), int64(nsec))
		if t2.Unix() > t.Unix() {
			t = t2
		}
	}
	return t, nil
}

func UpdateHistory(session3 *win.IUpdateSession3, ctx context.Context) (history []*win.IUpdateHistoryEntry, err error) {
	criteria := win.SysAllocString("")
	if criteria == nil {
		panic("SysAllocString failed")
	}
	defer win.SysFreeString(criteria)
	col, hr := session3.QueryHistory(criteria, 0, 0xffff)
	if win.FAILED(hr) {
		return nil, fmt.Errorf("failed to query update history: %x", hr)
	}
	defer col.Release()
	count, hr := col.Count()
	if win.FAILED(hr) {
		return nil, fmt.Errorf("failed to query update history count: %x", hr)
	}
	history = make([]*win.IUpdateHistoryEntry, 0, count)
	for i := int32(0); i < count; i++ {
		select {
		case <-ctx.Done():
			ReleaseIUpdateHistoryEntries(history)
			return nil, ctx.Err()
		default:
		}
		entry, hr := col.Item(i)
		if win.FAILED(hr) {
			continue
		}
		history = append(history, entry)
	}
	return history, nil
}

func ReleaseIUpdateHistoryEntries(history []*win.IUpdateHistoryEntry) {
	for i := range history {
		history[i].Release()
	}
}
