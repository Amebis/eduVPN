#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)" :
	if not exist $@ md $@

"$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj" : "$(CLIENT_TARGET).wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS) -out $@ $**

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj"

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
