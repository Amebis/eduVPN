#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

VERSION=2.255.1
PRODUCT_GUID_TEMPLATE={4DCF2B73-6D3B-46CF-A3__-24C449CFE194}

# Default testing configuration and platform
TEST_CFG=Debug
!IF "$(PROCESSOR_ARCHITECTURE)" == "AMD64"
TEST_PLAT=x64
!ELSE
TEST_PLAT=x86
!ENDIF
SETUP_CFG=Release

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
# Build
######################################################################

BuildWireGuard ::
	cd "wireguard-windows\embeddable-dll-service"
	build.bat
	cd "$(MAKEDIR)"

Clean ::
	-if exist "wireguard-windows\.deps"                        rd /q /s "wireguard-windows\.deps"
	-if exist "wireguard-windows\amd64"                        rd /q /s "wireguard-windows\amd64"
	-if exist "wireguard-windows\arm64"                        rd /q /s "wireguard-windows\arm64"
	-if exist "wireguard-windows\x86"                          rd /q /s "wireguard-windows\x86"
	-if exist "wireguard-windows\resources_*.syso"             del /f /q "wireguard-windows\resources_*.syso"
	-if exist "wireguard-windows\embeddable-dll-service\amd64" rd /q /s "wireguard-windows\embeddable-dll-service\amd64"
	-if exist "wireguard-windows\embeddable-dll-service\arm64" rd /q /s "wireguard-windows\embeddable-dll-service\arm64"
	-if exist "wireguard-windows\embeddable-dll-service\x86"   rd /q /s "wireguard-windows\embeddable-dll-service\x86"


######################################################################
# Setup
######################################################################

Setup :: \
	SetupBuild \
	SetupMSI \
	SetupExe \
	SetupPDB


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

RESOURCE_DIR=$(MAKEDIR)\eduLibsodium\eduLibsodium
TRANSIFEX_RES=edulibsodium
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
