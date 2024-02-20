/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"encoding/json"
	"errors"
	"strconv"
	"testing"
)

func TestVersionJSON(t *testing.T) {
	ver := Version{1, 2, 3}
	_, err := ver.MarshalJSON()
	if err != nil {
		t.Errorf("Error marshalling version to JSON: %#v", err)
	}
	ver2 := Version{}
	err = ver2.UnmarshalJSON(([]byte)(`"1.2.3"`))
	if err != nil {
		t.Errorf("Error unmarshalling version from JSON: %#v", err)
	}
	if ver != ver2 {
		t.Errorf("Version mismatch")
	}
	if serial, err := (&Version{0, 0}).MarshalJSON(); err != nil || string(serial) != `"0.0"` {
		t.Errorf("Error marshalling 0.0 version to JSON: %#v", err)
	}
	if serial, err := (&Version{1, 0, 1}).MarshalJSON(); err != nil || string(serial) != `"1.0.1"` {
		t.Errorf("Error marshalling 1.0.1 version to JSON: %#v", err)
	}
	if serial, err := (&Version{1, 0, 0, 1}).MarshalJSON(); err != nil || string(serial) != `"1.0.0.1"` {
		t.Errorf("Error marshalling 1.0.0.1 version to JSON: %#v", err)
	}

	ver3 := Version{}
	err = ver3.UnmarshalJSON(([]byte)(`""`))
	if err != nil {
		t.Errorf("Error unmarshalling empty version from JSON: %#v", err)
	}
	err = ver3.UnmarshalJSON(([]byte)(`{}`))
	var jsonErr *json.UnmarshalTypeError
	if !errors.As(err, &jsonErr) {
		t.Errorf("UnmarshalTypeError error was expected: %#v", err)
	}
	err = ver3.UnmarshalJSON(([]byte)(`"1.a.3"`))
	var numErr *strconv.NumError
	if !errors.As(err, &numErr) {
		t.Errorf("NumError error was expected: %#v", err)
	}
}

func TestVersionOrdering(t *testing.T) {
	verA := Version{1, 2, 3}
	verB := Version{1, 2, 3, 4}
	if !verB.IsNewer(&verA) {
		t.Error("Invalid version ordering 1")
	}
	if verB.IsNewer(&verB) {
		t.Error("Invalid version ordering 2")
	}
	if verB.IsOlderOrEqual(&verA) {
		t.Error("Invalid version ordering 3")
	}
}
