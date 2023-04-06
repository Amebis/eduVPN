/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"context"
	"fmt"
	"io"
	"log"
	"math"
	"strings"

	"github.com/Amebis/eduVPN/eduvpn-windows/progress"
	"golang.org/x/sys/windows/registry"
)

// evaluateInstalled enumerates installed products looking for productId and returns productVersion of productId; or nil, if productId is not installed.
func evaluateInstalled(productId string, ctx context.Context, progress progress.ProgressIndicator) (productVersion *Version, err error) {
	log.Println("Evaluating installed products")
	productId = strings.ToUpper(productId)
	uninstallKey, err := registry.OpenKey(registry.LOCAL_MACHINE, `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`, registry.ENUMERATE_SUB_KEYS|registry.WOW64_32KEY)
	if err != nil {
		return nil, fmt.Errorf("failed to open registry key: %w", err)
	}
	defer uninstallKey.Close()
	subkeys, err := uninstallKey.ReadSubKeyNames(math.MaxInt32)
	if err != io.EOF && err != nil {
		return nil, fmt.Errorf("failed to evaluate registry subkeys: %w", err)
	}
	for i := range subkeys {
		select {
		case <-ctx.Done():
			return nil, ctx.Err()
		default:
		}
		progress.SetProgress(float32(i) / float32(len(subkeys)))
		productKey, err := registry.OpenKey(uninstallKey, subkeys[i], registry.QUERY_VALUE)
		if err != nil {
			continue
		}
		defer productKey.Close()
		match := false
		bundleUpgradeCode, typ, err := productKey.GetStringValue("BundleUpgradeCode")
		if err == nil && strings.ToUpper(bundleUpgradeCode) == productId {
			match = true
		} else if err == registry.ErrUnexpectedType && typ == registry.MULTI_SZ {
			if bundleUpgradeCodes, _, err := productKey.GetStringsValue("BundleUpgradeCode"); err == nil {
				for _, bundleUpgradeCode := range bundleUpgradeCodes {
					if strings.ToUpper(bundleUpgradeCode) == productId {
						match = true
						break
					}
				}
			}
		}
		if !match {
			continue
		}
		if bundleVersionStr, _, err := productKey.GetStringValue("BundleVersion"); err == nil {
			if bundleVersion, err := ParseVersion(bundleVersionStr); err == nil {
				productVersion = bundleVersion
				if displayVersionStr, _, err := productKey.GetStringValue("DisplayVersion"); err == nil {
					if displayVersion, err := ParseVersion(displayVersionStr); err == nil {
						productVersion = displayVersion
					}
				}
				log.Printf("Installed version: %v", productVersion)
				break
			}
		}
	}
	return productVersion, nil
}
