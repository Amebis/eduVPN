/*
	eduVPN - VPN for education and research

	Copyright: 2023-2024 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"context"
	"errors"
	"testing"

	"github.com/Amebis/eduVPN/eduvpn-windows/progress"
)

func TestEvaluateInstalled(t *testing.T) {
	ctx := context.Background()
	progress := progress.Noop()

	_, err := evaluateInstalled("{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}", ctx, progress)
	if err != nil {
		t.Errorf("Error evaluating installed programs: %#v", err)
	}
	_, err = evaluateInstalled("{f899bad3-98ed-308e-a905-56b5338963ff}", ctx, progress)
	if err != nil {
		t.Errorf("Error evaluating installed programs: %#v", err)
	}

	ctx, cancel := context.WithCancel(ctx)
	cancel()
	_, err = evaluateInstalled("{f899bad3-98ed-308e-a905-56b5338963ff}", ctx, progress)
	if !errors.Is(err, context.Canceled) {
		t.Errorf("Cancelled error was expected: %#v", err)
	}
}
