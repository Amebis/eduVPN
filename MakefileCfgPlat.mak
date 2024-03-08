#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2024 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

MSVC_REDIST_VERSION = \
!INCLUDE "$(VCINSTALLDIR)Auxiliary\Build\Microsoft.VCRedistVersion.default.txt"

!IF "$(PLAT)" == "x64"
PLAT_MSVC=x64
PLAT_VCPKG=x64
PLAT_CLIENT=x64
PLAT_PROCESSOR_ARCHITECTURE=amd64
GOARCH=amd64
CC=x86_64-w64-mingw32-gcc.exe
WINDRES=x86_64-w64-mingw32-windres.exe
!ELSEIF "$(PLAT)" == "ARM64"
PLAT_MSVC=ARM64
PLAT_VCPKG=arm64
PLAT_CLIENT=ARM64
PLAT_PROCESSOR_ARCHITECTURE=arm64
GOARCH=arm64
CC=aarch64-w64-mingw32-gcc.exe
WINDRES=aarch64-w64-mingw32-windres.exe
!ELSE
PLAT_MSVC=Win32
PLAT_VCPKG=x86
PLAT_CLIENT=x86
PLAT_PROCESSOR_ARCHITECTURE=x86
GOARCH=386
CC=i686-w64-mingw32-gcc.exe
WINDRES=i686-w64-mingw32-windres.exe
!ENDIF

# WiX parameters
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG) \
	-arch $(PLAT) \
	-dPlatform="$(PLAT)" \
	-dTargetDir="bin\$(CFG)\$(PLAT)\\" \
	-dTargetDirClient="bin\$(CFG)\$(PLAT_CLIENT)\\" \
	-dVersionInformational="$(VERSION)$(CFG_TARGET) $(PLAT)"
!IF "$(PLAT)" == "x64" || "$(PLAT)" == "ARM64"
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFiles64Folder"
!ELSE
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFilesFolder"
!ENDIF

!IF "$(CFG)" == "Debug"
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dVCRedistDir="$(VCINSTALLDIR)Redist\MSVC\$(MSVC_REDIST_VERSION)\debug_nonredist\$(PLAT_CLIENT)\Microsoft.VC143.DebugCRT\\" \
	-dVCRedistSuffix="d"
!ELSE
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dVCRedistDir="$(VCINSTALLDIR)Redist\MSVC\$(MSVC_REDIST_VERSION)\$(PLAT_CLIENT)\Microsoft.VC143.CRT\\" \
	-dVCRedistSuffix=""
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(PLAT)" : "bin\$(CFG)"
	if not exist $@ md $@

"bin\$(CFG)\$(PLAT)\config" : "bin\$(CFG)\$(PLAT)"
	if not exist $@ md $@

!IF "$(CFG)" == "$(SETUP_CFG)"
SetupBuild :: \
	Build-$(CFG)-$(PLAT)
!ENDIF

BuildOpenVPN \
BuildOpenVPN-$(CFG)-$(PLAT) ::
	msbuild.exe "openvpn\openvpn.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT_MSVC)" $(MSBUILD_FLAGS)

BuildOpenVPN \
BuildOpenVPN-$(CFG)-$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)" \
	"bin\$(CFG)\$(PLAT)\wintun.dll" \
	"bin\$(CFG)\$(PLAT)\openvpn.exe" \
	"bin\$(CFG)\$(PLAT)\openvpnserv.exe"

"bin\$(CFG)\$(PLAT)\wintun.dll" : "openvpn\src\openvpn\vcpkg_installed\$(PLAT_VCPKG)-windows-ovpn\$(PLAT_VCPKG)-windows-ovpn\$(CFG_VCPKG)bin\wintun.dll"
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpn.exe" : "openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpn.exe"
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpnserv.exe" : "openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpnserv.exe"
	copy /y $** $@ > NUL

SignDeps \
SignOpenVPN : \
	"bin\$(CFG)\$(PLAT)\openvpn.exe" \
	"bin\$(CFG)\$(PLAT)\openvpnserv.exe"

BuildOpenVPN-$(CFG)-$(PLAT) ::
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 \
		"bin\$(CFG)\$(PLAT)\openvpn.exe" \
		"bin\$(CFG)\$(PLAT)\openvpnserv.exe"
!ENDIF

CleanOpenVPN \
CleanOpenVPN-$(CFG)-$(PLAT) ::
	-msbuild.exe "openvpn\openvpn.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT_MSVC)" $(MSBUILD_FLAGS)
	-if exist "bin\$(CFG)\$(PLAT)\wintun.dll"      del /f /q "bin\$(CFG)\$(PLAT)\wintun.dll"
	-if exist "bin\$(CFG)\$(PLAT)\openvpn.exe"     del /f /q "bin\$(CFG)\$(PLAT)\openvpn.exe"
	-if exist "bin\$(CFG)\$(PLAT)\openvpnserv.exe" del /f /q "bin\$(CFG)\$(PLAT)\openvpnserv.exe"

BuildWireGuard :: \
	"bin\$(CFG)\$(PLAT)" \
	"bin\$(CFG)\$(PLAT)\wireguard.dll" \
	"bin\$(CFG)\$(PLAT)\tunnel.dll"

"bin\$(CFG)\$(PLAT)\wireguard.dll" : "wireguard-windows\.deps\wireguard-nt\bin\$(PLAT_PROCESSOR_ARCHITECTURE)\wireguard.dll"
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\tunnel.dll" : "wireguard-windows\embeddable-dll-service\$(PLAT_PROCESSOR_ARCHITECTURE)\tunnel.dll"
	copy /y $** $@ > NUL

SignDeps \
SignWireGuard : \
	"bin\$(CFG)\$(PLAT)\wireguard.dll" \
	"bin\$(CFG)\$(PLAT)\tunnel.dll"

CleanWireGuard ::
	-if exist "bin\$(CFG)\$(PLAT)\wireguard.dll" del /f /q "bin\$(CFG)\$(PLAT)\wireguard.dll"
	-if exist "bin\$(CFG)\$(PLAT)\tunnel.dll"    del /f /q "bin\$(CFG)\$(PLAT)\tunnel.dll"

BuildeduVPNCommon \
BuildeduVPNCommon-$(CFG)-$(PLAT) :: \
	"bin\llvm-mingw-20220906-msvcrt-x86_64\bin\gcc.exe" \
	"eduvpn-common\internal\discovery\server_list.json" \
	"eduvpn-common\internal\discovery\organization_list.json" \
	"eduvpn-common\exports\resources_$(GOARCH).syso" \
	"bin\$(CFG)\$(PLAT)"
	cd "eduvpn-common\exports"
	set GOARCH=$(GOARCH)
	set GOARM=7
	set CGO_ENABLED=1
	set CGO_CFLAGS=$(CGO_CFLAGS)
	set CGO_LDFLAGS=$(CGO_LDFLAGS)
	set CC=$(CC)
	go.exe build $(CFG_GOFLAGS) -o "..\..\bin\$(CFG)\$(PLAT)\eduvpn_common.dll" -buildmode=c-shared .
	cd "$(MAKEDIR)"

SignDeps \
SigneduVPNCommon : "bin\$(CFG)\$(PLAT)\eduvpn_common.dll"

BuildeduVPNCommon-$(CFG)-$(PLAT) ::
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 "bin\$(CFG)\$(PLAT)\eduvpn_common.dll"
!ENDIF

CleaneduVPNCommon ::
	-if exist "bin\$(CFG)\$(PLAT)\eduvpn_common.dll" del /f /q "bin\$(CFG)\$(PLAT)\eduvpn_common.dll"
	-if exist "bin\$(CFG)\$(PLAT)\eduvpn_common.h"   del /f /q "bin\$(CFG)\$(PLAT)\eduvpn_common.h"

!IF "$(CFG)" == "$(SETUP_CFG)"
"eduvpn-common\exports\resources_$(GOARCH).syso" : "eduvpn-common\exports\resources.rc"
	$(WINDRES) -DVERSION_ARRAY=$(VERSION:.=,) -DVERSION=$(VERSION) -i $** -o $@ -O coff -c 65001

CleaneduVPNCommon ::
	-if exist "eduvpn-common\exports\resources_$(GOARCH).syso" del /f /q "eduvpn-common\exports\resources_$(GOARCH).syso"
!ENDIF

BuildeduVPNWindows \
BuildeduVPNWindows-$(CFG)-$(PLAT) :: \
	"bin\llvm-mingw-20220906-msvcrt-x86_64\bin\gcc.exe" \
	"eduvpn-windows\resources_$(GOARCH).syso" \
	"bin\$(CFG)\$(PLAT)"
	cd "eduvpn-windows"
	set GOARCH=$(GOARCH)
	set GOARM=7
	set CGO_ENABLED=1
	set CGO_CFLAGS=$(CGO_CFLAGS)
	set CGO_LDFLAGS=$(CGO_LDFLAGS)
	set CC=$(CC)
	go.exe build $(CFG_GOFLAGS) -o "..\bin\$(CFG)\$(PLAT)\eduvpn_windows.dll" -buildmode=c-shared .
	cd "$(MAKEDIR)"

SignDeps \
SigneduVPNWindows : "bin\$(CFG)\$(PLAT)\eduvpn_windows.dll"

BuildeduVPNWindows-$(CFG)-$(PLAT) ::
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 "bin\$(CFG)\$(PLAT)\eduvpn_windows.dll"
!ENDIF

CleaneduVPNWindows ::
	-if exist "bin\$(CFG)\$(PLAT)\eduvpn_windows.dll" del /f /q "bin\$(CFG)\$(PLAT)\eduvpn_windows.dll"
	-if exist "bin\$(CFG)\$(PLAT)\eduvpn_windows.h"   del /f /q "bin\$(CFG)\$(PLAT)\eduvpn_windows.h"

!IF "$(CFG)" == "$(SETUP_CFG)"
"eduvpn-windows\resources_$(GOARCH).syso" : "eduvpn-windows\resources.rc"
	$(WINDRES) -DVERSION_ARRAY=$(VERSION:.=,) -DVERSION=$(VERSION) -i $** -o $@ -O coff -c 65001

CleaneduVPNWindows ::
	-if exist "eduvpn-windows\resources_$(GOARCH).syso" del /f /q "eduvpn-windows\resources_$(GOARCH).syso"
!ENDIF

Build-$(CFG)-$(PLAT) ::
	bin\nuget.exe restore $(NUGET_FLAGS)

Build \
Build-$(CFG)-$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)"
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

!IF "$(CFG)" == "$(SETUP_CFG)"
SetupSign \
!ENDIF
Sign : \
	"bin\$(CFG)\$(PLAT)\eduEx.dll" \
	"bin\$(CFG)\$(PLAT)\eduJSON.dll" \
	"bin\$(CFG)\$(PLAT)\eduOAuth.dll" \
	"bin\$(CFG)\$(PLAT)\eduOpenVPN.dll" \
	"bin\$(CFG)\$(PLAT)\eduVPN.dll" \
	"bin\$(CFG)\$(PLAT)\eduVPN.Client.exe" \
	"bin\$(CFG)\$(PLAT)\eduVPN.Views.dll" \
	"bin\$(CFG)\$(PLAT)\eduWireGuard.dll" \
	"bin\$(CFG)\$(PLAT)\govVPN.Client.exe" \
	"bin\$(CFG)\$(PLAT)\LetsConnect.Client.exe" \
	"bin\$(CFG)\$(PLAT)\eduMSICA.dll" \
	"bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll" \
	"bin\$(CFG)\$(PLAT)\eduWGSvcHost.exe"

Clean ::
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

!IF "$(CFG)" == "$(SETUP_CFG)"
"bin\Setup\PDB_$(VERSION)$(CFG_TARGET).zip" : \
	bin\$(CFG)\$(PLAT)\*.pdb \
!IF "$(CFG)" != "Debug" && "$(PLAT)" != "ARM64"
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\compat.pdb" \
!ENDIF
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpn.pdb" \
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpnserv.pdb"
!ENDIF


######################################################################
# Client-specific rules
######################################################################

!IF $(BUILD_EDUVPN)
!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
!ENDIF

!IF $(BUILD_LETSCONNECT)
!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
!ENDIF

!IF $(BUILD_GOVVPN)
!INCLUDE "govVPN.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
!ENDIF


######################################################################
# Locale-specific rules
######################################################################

LANG=ar
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=en
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=de
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=es
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=es-ES
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=fr
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=nl
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=pt-PT
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=sl
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=tr
!INCLUDE "MakefileCfgPlatLang.mak"

LANG=uk
!INCLUDE "MakefileCfgPlatLang.mak"
