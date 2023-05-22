/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package sessioncheck

import "context"

type Notify func(string, string)

// Monitor detects and reports local user sessions.
func Monitor(ctx context.Context) error {

	return nil
}
