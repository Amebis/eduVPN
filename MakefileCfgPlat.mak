#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

MSVC_VERSION = \
!INCLUDE "$(VCINSTALLDIR)Auxiliary\Build\Microsoft.VCRedistVersion.default.txt"

!IF "$(PLAT)" == "x64"
PLAT_MSVC=x64
PLAT_VCPKG=x64
PLAT_CLIENT=x64
PLAT_PROCESSOR_ARCHITECTURE=amd64
!ELSEIF "$(PLAT)" == "ARM64"
PLAT_MSVC=ARM64
PLAT_VCPKG=arm64
PLAT_CLIENT=x86
PLAT_PROCESSOR_ARCHITECTURE=arm64
!ELSE
PLAT_MSVC=Win32
PLAT_VCPKG=x86
PLAT_CLIENT=x86
PLAT_PROCESSOR_ARCHITECTURE=x86
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
VCREDIST_MSM=Microsoft_VC142_DebugCRT_$(PLAT_CLIENT).msm
!ELSE
VCREDIST_MSM=Microsoft_VC142_CRT_$(PLAT_CLIENT).msm
!ENDIF


######################################################################
# Setup
######################################################################

!IF "$(CFG)" == "Release"
!IFDEF VIRUSTOTALAPIKEY
Publish :: \
	"bin\$(CFG)\$(PLAT)\eduMSICA.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduVPN.Resources.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduWGSvcHost.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\openvpn.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\openvpnserv.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\tunnel.vtanalysis" \
!IF "$(PLAT)" != "ARM64"
	"bin\$(CFG)\$(PLAT)\eduEx.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduJSON.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduLibsodium.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduOAuth.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduOpenVPN.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduVPN.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduVPN.Views.vtanalysis" \
	"bin\$(CFG)\$(PLAT)\eduWireGuard.vtanalysis"
!ENDIF
!ENDIF

"bin\$(CFG)\$(PLAT)\eduEx.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduEx.dll"

"bin\$(CFG)\$(PLAT)\eduJSON.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduJSON.dll"

"bin\$(CFG)\$(PLAT)\eduLibsodium.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduLibsodium.dll"

"bin\$(CFG)\$(PLAT)\eduMSICA.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduMSICA.dll"

"bin\$(CFG)\$(PLAT)\eduOAuth.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduOAuth.dll"

"bin\$(CFG)\$(PLAT)\eduOpenVPN.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduOpenVPN.dll"

"bin\$(CFG)\$(PLAT)\eduVPN.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduVPN.dll"

"bin\$(CFG)\$(PLAT)\eduVPN.Resources.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll"

"bin\$(CFG)\$(PLAT)\eduVPN.Views.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduVPN.Views.dll"

"bin\$(CFG)\$(PLAT)\eduWGSvcHost.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduWGSvcHost.exe"

"bin\$(CFG)\$(PLAT)\eduWireGuard.vtanalysis" : "bin\$(CFG)\$(PLAT)\eduWireGuard.dll"

"bin\$(CFG)\$(PLAT)\openvpn.vtanalysis" : "bin\$(CFG)\$(PLAT)\openvpn.exe"

"bin\$(CFG)\$(PLAT)\openvpnserv.vtanalysis" : "bin\$(CFG)\$(PLAT)\openvpnserv.exe"

"bin\$(CFG)\$(PLAT)\tunnel.vtanalysis" : "bin\$(CFG)\$(PLAT)\tunnel.dll"

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\eduMSICA.vtanalysis"         del /f /q "bin\$(CFG)\$(PLAT)\eduMSICA.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduVPN.Resources.vtanalysis" del /f /q "bin\$(CFG)\$(PLAT)\eduVPN.Resources.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduWGSvcHost.vtanalysis"     del /f /q "bin\$(CFG)\$(PLAT)\eduWGSvcHost.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\openvpn.vtanalysis"          del /f /q "bin\$(CFG)\$(PLAT)\openvpn.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\openvpnserv.vtanalysis"      del /f /q "bin\$(CFG)\$(PLAT)\openvpnserv.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\tunnel.vtanalysis"           del /f /q "bin\$(CFG)\$(PLAT)\tunnel.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduEx.vtanalysis"            del /f /q "bin\$(CFG)\$(PLAT)\eduEx.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduJSON.vtanalysis"          del /f /q "bin\$(CFG)\$(PLAT)\eduJSON.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduLibsodium.vtanalysis"     del /f /q "bin\$(CFG)\$(PLAT)\eduLibsodium.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduOAuth.vtanalysis"         del /f /q "bin\$(CFG)\$(PLAT)\eduOAuth.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduOpenVPN.vtanalysis"       del /f /q "bin\$(CFG)\$(PLAT)\eduOpenVPN.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduVPN.vtanalysis"           del /f /q "bin\$(CFG)\$(PLAT)\eduVPN.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduVPN.Views.vtanalysis"     del /f /q "bin\$(CFG)\$(PLAT)\eduVPN.Views.vtanalysis"
	-if exist "bin\$(CFG)\$(PLAT)\eduWireGuard.vtanalysis"     del /f /q "bin\$(CFG)\$(PLAT)\eduWireGuard.vtanalysis"
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

BuildLibsodium \
BuildLibsodium-$(CFG)-$(PLAT) ::
	msbuild.exe "eduLibsodium\libsodium\libsodium.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT_MSVC)" $(MSBUILD_FLAGS)

CleanLibsodium \
CleanLibsodium-$(CFG)-$(PLAT) ::
	-msbuild.exe "eduLibsodium\libsodium\libsodium.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT_MSVC)" $(MSBUILD_FLAGS)

BuildOpenVPN \
BuildOpenVPN-$(CFG)-$(PLAT) ::
	msbuild.exe "openvpn\openvpn.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT_MSVC)" $(MSBUILD_FLAGS)

BuildOpenVPN \
BuildOpenVPN-$(CFG)-$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)" \
	"bin\$(CFG)\$(PLAT)\wintun.dll" \
	"bin\$(CFG)\$(PLAT)\openvpn.exe" \
	"bin\$(CFG)\$(PLAT)\openvpnserv.exe"

"bin\$(CFG)\$(PLAT)\wintun.dll" : "$(VCPKG_ROOT)\packages\wintun_$(PLAT_VCPKG)-windows-ovpn\$(CFG_VCPKG)bin\wintun.dll"
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpn.exe" : "openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpn.exe"
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "OpenVPN" /q $**
!ENDIF
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpnserv.exe" : "openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpnserv.exe"
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "OpenVPN Interactive Service" /q $**
!ENDIF
	copy /y $** $@ > NUL

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

CleanWireGuard ::
	-if exist "bin\$(CFG)\$(PLAT)\wireguard.dll" del /f /q "bin\$(CFG)\$(PLAT)\wireguard.dll"
	-if exist "bin\$(CFG)\$(PLAT)\tunnel.dll"    del /f /q "bin\$(CFG)\$(PLAT)\tunnel.dll"

Build \
Build-$(CFG)-$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)"
	bin\nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Clean ::
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

"bin\$(CFG)\$(PLAT)\$(VCREDIST_MSM)" : "$(VCINSTALLDIR)Redist\MSVC\$(MSVC_VERSION)\MergeModules\$(VCREDIST_MSM)"
	copy /y $** $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\$(VCREDIST_MSM)" del /f /q "bin\$(CFG)\$(PLAT)\$(VCREDIST_MSM)"

!IF "$(CFG)" == "$(SETUP_CFG)"
"bin\Setup\PDB_$(VERSION)$(CFG_TARGET).zip" : \
	bin\$(CFG)\$(PLAT)\*.pdb \
	"eduLibsodium\libsodium\Build\$(CFG)\$(PLAT_MSVC)\libsodium.pdb" \
!IF "$(CFG)" != "Debug" && "$(PLAT)" != "ARM64"
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\compat.pdb" \
!ENDIF
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpn.pdb" \
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpnserv.pdb"
!ENDIF


######################################################################
# Client-specific rules
######################################################################

!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgPlatClient.mak"

!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
