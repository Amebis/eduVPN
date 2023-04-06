/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"encoding/json"
	"fmt"
	"strconv"
	"strings"
)

// Product/file version
type Version [4]uint

// ParseVersion parses n.n.n.n version string into components.
func ParseVersion(s string) (version *Version, err error) {
	v := strings.Split(s, ".")
	version = &Version{}
	for i := 0; i < 4 && i < len(v); i++ {
		val, err := strconv.ParseUint(v[i], 10, 0)
		if err != nil {
			return nil, err
		}
		version[i] = uint(val)
	}
	return version, nil
}

// UnmarshalJSON decodes "n.n.n.n" version.
func (v *Version) UnmarshalJSON(data []byte) error {
	if string(data) == "null" || string(data) == `""` {
		return nil
	}
	var realV string
	err := json.Unmarshal(data, &realV)
	if err != nil {
		return err
	}
	ver, err := ParseVersion(realV)
	if err != nil {
		return err
	}
	*v = *ver
	return nil
}

// MarshalJSON encodes "n.n.n.n" version.
func (v *Version) MarshalJSON() ([]byte, error) {
	if v[3] != 0 {
		return json.Marshal(fmt.Sprintf("%d.%d.%d.%d", v[0], v[1], v[2], v[3]))
	}
	if v[2] != 0 {
		return json.Marshal(fmt.Sprintf("%d.%d.%d", v[0], v[1], v[2]))
	}
	return json.Marshal(fmt.Sprintf("%d.%d", v[0], v[1]))
}

// IsNewer returns true if v > other
func (v *Version) IsNewer(other *Version) bool {
	for i := 0; i < 4; i++ {
		if v[i] > other[i] {
			return true
		}
	}
	return false
}

// IsOlderOrEqual returns true if v <= other
func (v *Version) IsOlderOrEqual(other *Version) bool {
	return !v.IsNewer(other)
}
