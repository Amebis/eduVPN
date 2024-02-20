/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"encoding/hex"
	"encoding/json"
	"errors"
	"testing"
)

func TestHash(t *testing.T) {
	hash := Hash{0xf7, 0xbc, 0xbe, 0xc8, 0x43, 0xd9, 0x70, 0x6b, 0x6d, 0xe7, 0x43, 0x0b, 0xf3, 0x66, 0x24, 0x52, 0xfc, 0x64, 0x88, 0xaa, 0x73, 0xdd, 0x19, 0x04, 0x7b, 0x2d, 0x7e, 0x00, 0x58, 0xe1, 0x70, 0x01}
	_, err := hash.MarshalJSON()
	if err != nil {
		t.Errorf("Error marshalling SHA-256 hash to JSON: %#v", err)
	}
	hash2 := Hash{}
	err = hash2.UnmarshalJSON(([]byte)(`"f7bcbec843d9706b6de7430bf3662452fc6488aa73dd19047b2d7e0058e17001"`))
	if err != nil {
		t.Errorf("Error unmarshalling SHA-256 hash from JSON: %#v", err)
	}
	if hash != hash2 {
		t.Errorf("SHA-256 hash mismatch")
	}

	hash3 := Hash{}
	err = hash3.UnmarshalJSON(([]byte)(`""`))
	if err != nil {
		t.Errorf("Error unmarshalling empty hash from JSON: %#v", err)
	}
	err = hash3.UnmarshalJSON(([]byte)(`{}`))
	var jsonErr *json.UnmarshalTypeError
	if !errors.As(err, &jsonErr) {
		t.Errorf("UnmarshalTypeError error was expected: %#v", err)
	}
	err = hash3.UnmarshalJSON(([]byte)(`"deadbeef"`))
	if err == nil {
		t.Error("Error was expected")
	}
	err = hash3.UnmarshalJSON(([]byte)(`"f7bcbec843d9706b6de7430xf3662452fc6488aa73dd19047b2d7e0058e17001"`))
	var hexErr hex.InvalidByteError
	if !errors.As(err, &hexErr) {
		t.Errorf("InvalidByteError error was expected: %#v", err)
	}
}
