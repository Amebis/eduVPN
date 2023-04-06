/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"encoding/hex"
	"encoding/json"
	"fmt"
	"unsafe"
)

// SHA-256 hash
type Hash [32]byte

// UnmarshalJSON hexadecimal-decodes SHA-256 hash value.
func (h *Hash) UnmarshalJSON(data []byte) error {
	if string(data) == "null" || string(data) == `""` {
		return nil
	}
	var realV string
	err := json.Unmarshal(data, &realV)
	if err != nil {
		return err
	}
	if hex.DecodedLen(len(realV)) != 32 {
		return fmt.Errorf("invalid SHA256 hash")
	}
	_, err = hex.Decode(unsafe.Slice(&h[0], 32), []byte(realV))
	if err != nil {
		return err
	}
	return nil
}

// MarshalJSON hexadecimal-encodes SHA-256 hash value.
func (h *Hash) MarshalJSON() ([]byte, error) {
	return json.Marshal(hex.EncodeToString(unsafe.Slice(&h[0], 32)))
}
