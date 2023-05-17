#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2023 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

VERSION=3.255.2
PRODUCT_GUID_TEMPLATE={99D534BE-0970-4842-92__-E935D5ADD79D}

# Default testing configuration and platform
TEST_CFG=Debug
!IF "$(PROCESSOR_ARCHITECTURE)" == "AMD64"
TEST_PLAT=x64
!ELSE
TEST_PLAT=x86
!ENDIF
SETUP_CFG=Release

# Go and CGo building
PATH=$(MAKEDIR)\bin\llvm-mingw-20220906-msvcrt-x86_64\bin;$(PATH)

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

!IF [minisign.exe -v > NUL] == 0
MINISIGN_AVAILABLE=1
!IF EXISTS("$(USERPROFILE)\.minisign\minisign.key")
MINISIGN_KEY_AVAILABLE=1
!ENDIF
!ENDIF

CGO_CFLAGS=-O3 -Wall -Wno-unused-function -Wno-switch -std=gnu11 -DWINVER=0x0601
CGO_LDFLAGS=-Wl,--dynamicbase -Wl,--nxcompat -Wl,--export-all-symbols


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

BuildDeps :: \
	BuildLibsodium \
	BuildOpenVPN \
	BuildWireGuard \
	BuildeduVPNCommon \
	BuildeduVPNWindows

BuildWireGuard ::
	cd "wireguard-windows\embeddable-dll-service"
	build.bat
	cd "$(MAKEDIR)"

"bin\llvm-mingw-20220906-msvcrt-x86_64\bin\gcc.exe" :
	cd "bin"
	curl.exe --location --output "llvm-mingw-msvcrt.zip" "https://github.com/mstorsjo/llvm-mingw/releases/download/20220906/llvm-mingw-20220906-msvcrt-x86_64.zip"
	for /f %%a in ('CertUtil -hashfile "llvm-mingw-msvcrt.zip" SHA256 ^| findstr /r "^[0-9a-f]*$$"') do if not "%%a"=="1b63120c346ff78a4e3dba77101a535434a62122d3b44021438a77bdf1b4679a" exit /b 1
	tar -xf "llvm-mingw-msvcrt.zip"
	del /f /q "llvm-mingw-msvcrt.zip"
	cd "$(MAKEDIR)"

"eduvpn-common\internal\discovery\server_list.json" ::
	curl.exe --location --output $@ "https://disco.eduvpn.org/v2/server_list.json"
!IFDEF MINISIGN_AVAILABLE
	curl.exe --location --output "$(@:"=).minisig" "https://disco.eduvpn.org/v2/server_list.json.minisig"
	minisign.exe -Vm $@ -P RWRtBSX1alxyGX+Xn3LuZnWUT0w//B6EmTJvgaAxBMYzlQeI+jdrO6KF || minisign.exe -Vm $@ -P "RWQKqtqvd0R7rUDp0rWzbtYPA3towPWcLDCl7eY9pBMMI/ohCmrS0WiM"
!ENDIF

"eduvpn-common\internal\discovery\organization_list.json" ::
	curl.exe --location --output $@ "https://disco.eduvpn.org/v2/organization_list.json"
!IFDEF MINISIGN_AVAILABLE
	curl.exe --location --output "$(@:"=).minisig" "https://disco.eduvpn.org/v2/organization_list.json.minisig"
	minisign.exe -Vm $@ -P RWRtBSX1alxyGX+Xn3LuZnWUT0w//B6EmTJvgaAxBMYzlQeI+jdrO6KF || minisign.exe -Vm $@ -P "RWQKqtqvd0R7rUDp0rWzbtYPA3towPWcLDCl7eY9pBMMI/ohCmrS0WiM"
!ENDIF

CleanDeps :: \
	CleanLibsodium \
	CleanOpenVPN \
	CleanWireGuard \
	CleaneduVPNCommon

CleanWireGuard ::
	-if exist "wireguard-windows\.deps"                        rd /q /s "wireguard-windows\.deps"
	-if exist "wireguard-windows\amd64"                        rd /q /s "wireguard-windows\amd64"
	-if exist "wireguard-windows\arm64"                        rd /q /s "wireguard-windows\arm64"
	-if exist "wireguard-windows\x86"                          rd /q /s "wireguard-windows\x86"
	-if exist "wireguard-windows\resources_*.syso"             del /f /q "wireguard-windows\resources_*.syso"
	-if exist "wireguard-windows\embeddable-dll-service\amd64" rd /q /s "wireguard-windows\embeddable-dll-service\amd64"
	-if exist "wireguard-windows\embeddable-dll-service\arm64" rd /q /s "wireguard-windows\embeddable-dll-service\arm64"
	-if exist "wireguard-windows\embeddable-dll-service\x86"   rd /q /s "wireguard-windows\embeddable-dll-service\x86"

CleaneduVPNCommon ::
	-if exist "bin\llvm-mingw-20220906-msvcrt-x86_64"                           rd /q /s "bin\llvm-mingw-20220906-msvcrt-x86_64"
	-if exist "eduvpn-common\internal\discovery\server_list.json"               del /f /q "eduvpn-common\internal\discovery\server_list.json"
	-if exist "eduvpn-common\internal\discovery\server_list.json.minisig"       del /f /q "eduvpn-common\internal\discovery\server_list.json.minisig"
	-if exist "eduvpn-common\internal\discovery\organization_list.json"         del /f /q "eduvpn-common\internal\discovery\organization_list.json"
	-if exist "eduvpn-common\internal\discovery\organization_list.json.minisig" del /f /q "eduvpn-common\internal\discovery\organization_list.json.minisig"

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
