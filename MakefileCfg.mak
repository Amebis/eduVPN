#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
WIX_CANDLE_FLAGS_CFG=$(WIX_CANDLE_FLAGS)


######################################################################
# Setup
######################################################################

!IF "$(CFG)" == "Release"
"$(SETUP_DIR)\eduVPN.windows.json.minisig" \
"$(SETUP_DIR)\LetsConnect.windows.json.minisig" : \
	"$(SETUP_DIR)\eduVPN.windows.json" \
	"$(SETUP_DIR)\LetsConnect.windows.json"
	echo Signing $**
	minisign.exe -Sm $**

Clean ::
	-if exist "$(SETUP_DIR)\eduVPN.windows.json.minisig"       del /f /q "$(SETUP_DIR)\eduVPN.windows.json.minisig"
	-if exist "$(SETUP_DIR)\LetsConnect.windows.json.minisig"  del /f /q "$(SETUP_DIR)\LetsConnect.windows.json.minisig"
!ENDIF


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)" :
	if not exist $@ md $@


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
