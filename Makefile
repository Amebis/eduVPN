#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

VERSION=1.255.6
OPENVPN_VERSION=2.5.1.15

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


######################################################################
# Transifex
######################################################################

TRANSIFEX_ORG=amebis
TRANSIFEX_PROJ=eduvpn

RESOURCE_DIR=$(MAKEDIR)\eduEd25519\eduEd25519
TRANSIFEX_RES=edued25519
!INCLUDE "MakefileTransifex.mak"

RESOURCE_DIR=$(MAKEDIR)\eduJSON\eduJSON\Resources
TRANSIFEX_RES=edujson
!INCLUDE "MakefileTransifex.mak"

RESOURCE_DIR=$(MAKEDIR)\eduOAuth\eduOAuth\Resources
TRANSIFEX_RES=eduoauth
!INCLUDE "MakefileTransifex.mak"

RESOURCE_DIR=$(MAKEDIR)\eduOpenVPN\eduOpenVPN\Resources
TRANSIFEX_RES=eduopenvpn
!INCLUDE "MakefileTransifex.mak"

RESOURCE_DIR=$(MAKEDIR)\eduVPN\Resources
TRANSIFEX_RES=eduvpn
!INCLUDE "MakefileTransifex.mak"

RESOURCE_DIR=$(MAKEDIR)\eduVPN.Views\Resources
TRANSIFEX_RES=eduvpnviews
!INCLUDE "MakefileTransifex.mak"
