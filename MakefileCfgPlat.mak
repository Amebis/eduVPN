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
!ELSEIF "$(PLAT)" == "ARM64"
PLAT_MSVC=ARM64
PLAT_VCPKG=arm64
PLAT_CLIENT=x86
!ELSE
PLAT_MSVC=Win32
PLAT_VCPKG=x86
PLAT_CLIENT=x86
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
# Building
######################################################################

"bin\$(CFG)\$(PLAT)" : "bin\$(CFG)"
	if not exist $@ md $@

"bin\$(CFG)\$(PLAT)\config" : "bin\$(CFG)\$(PLAT)"
	if not exist $@ md $@

!IF "$(CFG)" == "$(SETUP_CFG)"
SetupBuild :: \
	Build$(CFG)$(PLAT)
!ENDIF

Build$(CFG)$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)"
	if not exist vcpkg\vcpkg.exe vcpkg\bootstrap-vcpkg.bat -disableMetrics
	vcpkg\vcpkg.exe install --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(PLAT_VCPKG)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun
	msbuild.exe "openvpn\openvpn.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT_MSVC)" $(MSBUILD_FLAGS)
	bin\nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Clean ::
	-if exist vcpkg\vcpkg.exe vcpkg\vcpkg.exe remove --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(PLAT_VCPKG)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun
	-msbuild.exe "openvpn\openvpn.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT_MSVC)" $(MSBUILD_FLAGS)
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Build$(CFG)$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)\wintun.dll" \
	"bin\$(CFG)\$(PLAT)\openvpn.exe" \
	"bin\$(CFG)\$(PLAT)\openvpnserv.exe"

"bin\$(CFG)\$(PLAT)\wintun.dll" : "vcpkg\installed\$(PLAT_VCPKG)-windows-ovpn\$(CFG_VCPKG)bin\wintun.dll"
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpn.exe" : "openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpn.exe"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "OpenVPN" /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpnserv.exe" : "openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpnserv.exe"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "OpenVPN Interactive Service" /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\$(VCREDIST_MSM)" : "$(VCINSTALLDIR)Redist\MSVC\$(MSVC_VERSION)\MergeModules\$(VCREDIST_MSM)"
	copy /y $** $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\wintun.dll"      del /f /q "bin\$(CFG)\$(PLAT)\wintun.dll"
	-if exist "bin\$(CFG)\$(PLAT)\openvpn.exe"     del /f /q "bin\$(CFG)\$(PLAT)\openvpn.exe"
	-if exist "bin\$(CFG)\$(PLAT)\openvpnserv.exe" del /f /q "bin\$(CFG)\$(PLAT)\openvpnserv.exe"
	-if exist "bin\$(CFG)\$(PLAT)\$(VCREDIST_MSM)" del /f /q "bin\$(CFG)\$(PLAT)\$(VCREDIST_MSM)"

!IF "$(CFG)" == "$(SETUP_CFG)"
"bin\Setup\PDB_$(VERSION)$(CFG_TARGET).zip" : \
	bin\$(CFG)\$(PLAT)\*.pdb \
	"eduLibsodium\libsodium\$(CFG)\$(PLAT_MSVC)\libsodium.pdb" \
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\compat.pdb" \
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpn.pdb" \
	"openvpn\$(PLAT_MSVC)-Output\$(CFG)\openvpnserv.pdb" \
	"vcpkg\installed\$(PLAT_VCPKG)-windows-ovpn\$(CFG_VCPKG)lib\ossl_static.pdb"
!ENDIF


######################################################################
# Client-specific rules
######################################################################

!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgPlatClient.mak"

!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
