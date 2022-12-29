#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2023 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

CLIENT_TARGET=eduVPN
CLIENT_TITLE=eduVPN
CLIENT_UPGRADE_CODE={EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}
CLIENT_PRODUCT_CODE=$(PRODUCT_GUID_TEMPLATE:__=00)
CLIENT_UPGRADE_GUID_64={02EBD828-2565-4BCD-ABFF-E3F48C3F9A23}
CLIENT_UPGRADE_GUID_32={E3746042-5041-4E2F-83E8-0240EF3C60CA}
CLIENT_ABOUT_URI=https://www.eduvpn.org/
CLIENT_URN=org.eduvpn.app

CLIENT_ID=00

IDS_CLIENT_PREFIX=

WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProductGUID="$(CLIENT_PRODUCT_CODE)" \
!IF "$(PLAT)" == "x64" || "$(PLAT)" == "ARM64"
	-dTAPWin.UpgradeGUID="{D6F9001D-05D8-4107-BCDD-41FB5520691E}" \
	-dOpenVPN.UpgradeGUID="{75C79E9E-5486-4568-814D-80C56E113FB8}" \
	-dUpgradeGUID="$(CLIENT_UPGRADE_GUID_64)"
!ELSE
	-dTAPWin.UpgradeGUID="{FE30D203-C056-42D5-AF56-273F65A7709A}" \
	-dOpenVPN.UpgradeGUID="{258634EA-316E-434E-9AE9-13926FB26B12}" \
	-dUpgradeGUID="$(CLIENT_UPGRADE_GUID_32)"
!ENDIF
