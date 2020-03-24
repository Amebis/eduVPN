#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
WIX_CANDLE_FLAGS_CFG=$(WIX_CANDLE_FLAGS)


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)" :
	if not exist $@ md $@

"$(OUTPUT_DIR)\$(CFG)\TAP-Windows.wixobj" : "TAP-Windows.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS) -out $@ $**

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\TAP-Windows.wixobj"     del /f /q "$(OUTPUT_DIR)\$(CFG)\TAP-Windows.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\tap-windows-9.21.2.exe" del /f /q "$(OUTPUT_DIR)\$(CFG)\tap-windows-9.21.2.exe"


######################################################################
# Platform specific rules
######################################################################

PLAT=x86
!INCLUDE "MakefileCfgPlat.mak"

PLAT=x64
!INCLUDE "MakefileCfgPlat.mak"


######################################################################
# Client-specific rules
######################################################################

!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgClient.mak"

!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgClient.mak"
