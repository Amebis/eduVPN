#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

VERSION=1.0.39
PRODUCT_GUID_TEMPLATE={E937E37F-9296-4607-BD__-4EB4BB8A78E4}

TAPWIN_VERSION=9.24.5.2
TAPWIN_PRODUCT_GUID_TEMPLATE={AE0F588E-FFBC-4475-8F__-364A70E90CF9}

OPENVPN_VERSION=2.5.2.2
OPENVPN_PRODUCT_GUID_TEMPLATE={E0C452C0-C5F2-4C4E-AB__-A72270FFB427}

# Default testing configuration and platform
TEST_CFG=Debug
!IF "$(PROCESSOR_ARCHITECTURE)" == "AMD64"
TEST_PLAT=x64
!ELSE
TEST_PLAT=x86
!ENDIF

# Utility default flags
REG_FLAGS=/f
NUGET_FLAGS=-Verbosity quiet
MSBUILD_FLAGS=/m /v:minimal /nologo
CSCRIPT_FLAGS=//Nologo
WIX_EXTENSIONS=-ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension
WIX_WIXCOP_FLAGS=-nologo "-set1$(MAKEDIR)\wixcop.xml"
WIX_CANDLE_FLAGS=-nologo \
	-dTAPWin.Version="$(TAPWIN_VERSION)" \
	-dOpenVPN.Version="$(OPENVPN_VERSION)" \
	-dCore.Version="$(VERSION)" \
	-dVersion="$(VERSION)" \
	$(WIX_EXTENSIONS)
WIX_LIGHT_FLAGS=-nologo -dcl:high -spdb -sice:ICE03 -sice:ICE60 -sice:ICE61 -sice:ICE82 $(WIX_EXTENSIONS)
WIX_INSIGNIA_FLAGS=-nologo


######################################################################
# Default target
######################################################################

All :: \
	Setup


######################################################################
# Registration
######################################################################

Register :: \
	NuGetRestore \
	RegisterOpenVPNInteractiveService \
	RegisterShortcuts

Unregister :: \
	UnregisterShortcuts \
	UnregisterOpenVPNInteractiveService

NuGetRestore ::
	bin\nuget.exe restore $(NUGET_FLAGS)


######################################################################
# Setup
######################################################################

Setup :: \
	NuGetRestore \
	SetupBuild \
	SetupMSI \
	SetupExe

"bin\Setup\eduVPN.windows.json.minisig" \
"bin\Setup\LetsConnect.windows.json.minisig" : \
	"bin\Setup\eduVPN.windows.json" \
	"bin\Setup\LetsConnect.windows.json"
	echo Signing $**
	minisign.exe -Sm $**


######################################################################
# Configuration specific rules
######################################################################

CFG=Debug
!INCLUDE "MakefileCfg.mak"

CFG=Release
!INCLUDE "MakefileCfg.mak"


######################################################################
# Platform specific rules
######################################################################

PLAT=x86
!INCLUDE "MakefilePlat.mak"

PLAT=x64
!INCLUDE "MakefilePlat.mak"
