#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

VERSION=2.255.0
PRODUCT_GUID_TEMPLATE={0BD2B793-650B-410A-9B__-F2116A3CB0BA}

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
	-dVersion="$(VERSION)" \
	$(WIX_EXTENSIONS) \
	-sw1118
WIX_LIGHT_FLAGS=-nologo -dcl:high -spdb -sice:ICE03 -sice:ICE60 -sice:ICE61 -sice:ICE82 $(WIX_EXTENSIONS)
WIX_INSIGNIA_FLAGS=-nologo


######################################################################
# Setup
######################################################################

Setup :: \
	Build \
	SetupMSI \
	SetupExe


######################################################################
# Configuration specific rules
######################################################################

CFG=Debug
!INCLUDE "MakefileCfg.mak"

CFG=Release
!INCLUDE "MakefileCfg.mak"


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


######################################################################
# Signing
######################################################################

Setup ::
	echo Signing setup files
	minisign.exe -Sm \
		"bin\Setup\eduVPNClient_$(VERSION).exe" \
		"bin\Setup\LetsConnectClient_$(VERSION).exe" \
		"bin\Setup\eduVPNClient_$(VERSION)_ARM64.msi" \
		"bin\Setup\eduVPNClient_$(VERSION)_x64.msi" \
		"bin\Setup\eduVPNClient_$(VERSION)_x86.msi" \
		"bin\Setup\LetsConnectClient_$(VERSION)_ARM64.msi" \
		"bin\Setup\LetsConnectClient_$(VERSION)_x64.msi" \
		"bin\Setup\LetsConnectClient_$(VERSION)_x86.msi"

Publish ::
	echo Signing self-update discovery files
	minisign.exe -Sm \
		"bin\Setup\eduVPN.windows.json" \
		"bin\Setup\LetsConnect.windows.json"

Clean ::
	-if exist "bin\Setup\*Client_*.exe.minisig" del /f /q "bin\Setup\*Client_*.exe.minisig"
	-if exist "bin\Setup\*Client_*.msi.minisig" del /f /q "bin\Setup\*Client_*.msi.minisig"
