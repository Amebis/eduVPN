/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package progress

// ProgressIndicator is a prototype for long running tasks to report progress
type ProgressIndicator interface {
	// SetProgress sets current progress. Value is in the [0.0f...1.0f] range.
	SetProgress(value float32)
}

// noopProgressIndicator is a no-op progress indicator. It is not struct{}, since vars of this type must have distinct addresses.
type noopProgressIndicator int

func (*noopProgressIndicator) SetProgress(value float32) {
}

var noop = new(noopProgressIndicator)

// Noop returns a non-nil, no-op ProgressIndicator.
func Noop() ProgressIndicator {
	return noop
}
