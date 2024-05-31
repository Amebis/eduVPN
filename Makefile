#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2024 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

VERSION=3.255.22
PRODUCT_GUID_TEMPLATE={0BC3DA63-3CCE-4884-80__-3587FE1DA2FC}

!IFNDEF BUILD_EDUVPN
BUILD_EDUVPN=1
!ENDIF
!IFNDEF BUILD_LETSCONNECT
BUILD_LETSCONNECT=1
!ENDIF
!IFNDEF BUILD_GOVVPN
BUILD_GOVVPN=0
!ENDIF

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
	BuildOpenVPN \
	BuildWireGuard \
	BuildeduVPNCommon \
	BuildeduVPNWindows \
	SignDeps

BuildWireGuard ::
	cd "wireguard-windows\embeddable-dll-service"
	build.bat
	cd "$(MAKEDIR)"

"bin\llvm-mingw-20220906-msvcrt-x86_64\bin\gcc.exe" :
	cd "bin"
	curl.exe --location --output "llvm-mingw-msvcrt.zip" "https://github.com/mstorsjo/llvm-mingw/releases/download/20220906/llvm-mingw-20220906-msvcrt-x86_64.zip"
	for /f %%a in ('CertUtil.exe -hashfile "llvm-mingw-msvcrt.zip" SHA256 ^| findstr /r "^[0-9a-f]*$$"') do if not "%%a"=="1b63120c346ff78a4e3dba77101a535434a62122d3b44021438a77bdf1b4679a" exit /b 1
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
	cscript.exe $(CSCRIPT_FLAGS) bin\VirusTotal.wsf //Job:Upload $** $@

.dll.vtanalysis :
	cscript.exe $(CSCRIPT_FLAGS) bin\VirusTotal.wsf //Job:Upload $** $@

.msi.vtanalysis :
	cscript.exe $(CSCRIPT_FLAGS) bin\VirusTotal.wsf //Job:Upload $** $@


######################################################################
# Setup
######################################################################

Setup :: \
	SetupBuild \
	SetupSign \
	SetupMSI \
	SetupSignMSI \
	SetupBoot \
	SetupSignBoot \
	SetupExe \
	SetupSignExe \
	SetupPDB


######################################################################
# Configuration specific rules
######################################################################

CFG=Debug
!INCLUDE "MakefileCfg.mak"

CFG=Release
!INCLUDE "MakefileCfg.mak"


######################################################################
# Signing
######################################################################

SignDeps \
SignOpenVPN \
SignWireGuard \
SigneduVPNCommon \
SigneduVPNWindows \
Sign \
SetupSign \
SetupSignMSI \
SetupSignBoot \
SetupSignExe :
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 $**
!ENDIF

BuildOpenVPN :: SignOpenVPN

BuildWireGuard :: SignWireGuard

BuildeduVPNCommon :: SigneduVPNCommon

BuildeduVPNWindows :: SigneduVPNWindows

Build :: Sign


######################################################################
# Publishing
######################################################################

Publish :: \
	PublishVTUpload \
	PublishVTJoin


######################################################################
# Transifex
######################################################################

TRANSIFEX_ORG=amebis
TRANSIFEX_PROJ=eduvpn

RESOURCE_DIR=$(MAKEDIR)\eduOpenVPN\Resources
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
