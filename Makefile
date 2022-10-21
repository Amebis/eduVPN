#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

VERSION=3.3.3
PRODUCT_GUID_TEMPLATE={A3E6AB0E-B083-4969-88__-5DF7FDF8F96E}

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
# Default target
######################################################################

Build ::


######################################################################
# Build
######################################################################

Build \
SetupBuild :: \
	"bin\$(TEST_CFG)\$(TEST_PLAT)"
	bin\nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /t:PrepopulateResponseCache /p:Configuration="$(TEST_CFG)" /p:Platform="$(TEST_PLAT)" $(MSBUILD_FLAGS)
	"bin\$(TEST_CFG)\$(TEST_PLAT)\PrepopulateResponseCache.exe" "eduVPN.Client\app.config"

BuildDeps :: \
	BuildLibsodium \
	BuildOpenVPN \
	BuildWireGuard

BuildWireGuard ::
	cd "wireguard-windows\embeddable-dll-service"
	build.bat
	cd "$(MAKEDIR)"

CleanDeps :: \
	CleanLibsodium \
	CleanOpenVPN \
	CleanWireGuard

CleanWireGuard ::
	-if exist "wireguard-windows\.deps"                        rd /q /s "wireguard-windows\.deps"
	-if exist "wireguard-windows\amd64"                        rd /q /s "wireguard-windows\amd64"
	-if exist "wireguard-windows\arm64"                        rd /q /s "wireguard-windows\arm64"
	-if exist "wireguard-windows\x86"                          rd /q /s "wireguard-windows\x86"
	-if exist "wireguard-windows\resources_*.syso"             del /f /q "wireguard-windows\resources_*.syso"
	-if exist "wireguard-windows\embeddable-dll-service\amd64" rd /q /s "wireguard-windows\embeddable-dll-service\amd64"
	-if exist "wireguard-windows\embeddable-dll-service\arm64" rd /q /s "wireguard-windows\embeddable-dll-service\arm64"
	-if exist "wireguard-windows\embeddable-dll-service\x86"   rd /q /s "wireguard-windows\embeddable-dll-service\x86"

Clean ::
	-if exist "bin\Setup\PDB_*.zip" del /f /q "bin\Setup\PDB_*.zip"

.SUFFIXES : .exe .dll .msi

.exe.vtanalysis :
	curl.exe --request POST --url "https://www.virustotal.com/api/v3/files" --header "Accept: application/json" --header "Content-Type: multipart/form-data" --header "x-apikey: $(VIRUSTOTALAPIKEY)" --form "file=@$**" --output "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL

.dll.vtanalysis :
	curl.exe --request POST --url "https://www.virustotal.com/api/v3/files" --header "Accept: application/json" --header "Content-Type: multipart/form-data" --header "x-apikey: $(VIRUSTOTALAPIKEY)" --form "file=@$**" --output "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL

.msi.vtanalysis :
	curl.exe --request POST --url "https://www.virustotal.com/api/v3/files" --header "Accept: application/json" --header "Content-Type: multipart/form-data" --header "x-apikey: $(VIRUSTOTALAPIKEY)" --form "file=@$**" --output "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL


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

RESOURCE_DIR=$(MAKEDIR)\eduWireGuard\eduWireGuard\Resources
TRANSIFEX_RES=eduwireguard
!INCLUDE "MakefileTransifex.mak"
