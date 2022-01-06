#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

MSVC_VERSION = \
!INCLUDE "$(VCINSTALLDIR)Auxiliary\Build\Microsoft.VCRedistVersion.default.txt"

SETUP_TARGET=$(PLAT)
!IF "$(CFG)" == "Debug"
SETUP_TARGET=$(SETUP_TARGET)D
VCPKG_BIN=debug\bin
!ELSE
VCPKG_BIN=bin
!ENDIF

!IF "$(PLAT)" == "x64"
OPENSSL_PLAT=-x64
OPENVPN_PLAT=x64
VCPKG_PLAT=x64
CLIENT_PLAT=x64
!ELSEIF "$(PLAT)" == "ARM64"
OPENSSL_PLAT=-arm64
OPENVPN_PLAT=ARM64
VCPKG_PLAT=arm64
CLIENT_PLAT=x86
!ELSE
OPENSSL_PLAT=
OPENVPN_PLAT=Win32
VCPKG_PLAT=x86
CLIENT_PLAT=x86
!ENDIF

# WiX parameters
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG) \
	-arch $(PLAT) \
	-dPlatform="$(PLAT)" \
	-dTargetDir="bin\$(CFG)\$(PLAT)\\" \
	-dTargetDirClient="bin\$(CFG)\$(CLIENT_PLAT)\\" \
	-dOpenSSL.Platform="$(OPENSSL_PLAT)" \
	-dVersionInformational="$(VERSION) $(SETUP_TARGET)"
!IF "$(PLAT)" == "x64" || "$(PLAT)" == "ARM64"
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFiles64Folder"
!ELSE
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFilesFolder"
!ENDIF

!IF "$(CFG)" == "Debug"
VC142REDIST_MSM=Microsoft_VC142_DebugCRT_$(CLIENT_PLAT).msm
!ELSE
VC142REDIST_MSM=Microsoft_VC142_CRT_$(CLIENT_PLAT).msm
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(PLAT)" : "bin\$(CFG)"
	if not exist $@ md $@

"bin\$(CFG)\$(PLAT)\config" : "bin\$(CFG)\$(PLAT)"
	if not exist $@ md $@

!IF "$(CFG)" == "Release"
Build :: \
	Build$(CFG)$(PLAT)
!ENDIF

Build$(CFG)$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)"
	if not exist vcpkg\vcpkg.exe vcpkg\bootstrap-vcpkg.bat -disableMetrics
	vcpkg\vcpkg.exe install --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(VCPKG_PLAT)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun
	msbuild.exe "openvpn\openvpn.sln" /p:Configuration="$(CFG)" /p:Platform="$(OPENVPN_PLAT)" $(MSBUILD_FLAGS)
	bin\nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Clean ::
	-if exist vcpkg\vcpkg.exe vcpkg\vcpkg.exe remove --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(VCPKG_PLAT)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun
	-msbuild.exe "openvpn\openvpn.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(OPENVPN_PLAT)" $(MSBUILD_FLAGS)
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Build$(CFG)$(PLAT) :: \
	"bin\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll" \
	"bin\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll" \
	"bin\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" \
	"bin\$(CFG)\$(PLAT)\lzo2.dll" \
	"bin\$(CFG)\$(PLAT)\wintun.dll" \
	"bin\$(CFG)\$(PLAT)\openvpn.exe" \
	"bin\$(CFG)\$(PLAT)\openvpnserv.exe"

"bin\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll" : "vcpkg\installed\$(VCPKG_PLAT)-windows-ovpn\$(VCPKG_BIN)\libcrypto-1_1$(OPENSSL_PLAT).dll"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll" : "vcpkg\installed\$(VCPKG_PLAT)-windows-ovpn\$(VCPKG_BIN)\libssl-1_1$(OPENSSL_PLAT).dll"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" : "vcpkg\installed\$(VCPKG_PLAT)-windows-ovpn\$(VCPKG_BIN)\libpkcs11-helper-1.dll"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\lzo2.dll" : "vcpkg\installed\$(VCPKG_PLAT)-windows-ovpn\$(VCPKG_BIN)\lzo2.dll"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\wintun.dll" : "vcpkg\installed\$(VCPKG_PLAT)-windows-ovpn\$(VCPKG_BIN)\wintun.dll"
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpn.exe" : "openvpn\$(OPENVPN_PLAT)-Output\$(CFG)\openvpn.exe"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "OpenVPN" /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\openvpnserv.exe" : "openvpn\$(OPENVPN_PLAT)-Output\$(CFG)\openvpnserv.exe"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "OpenVPN Interactive Service" /q $**
	copy /y $** $@ > NUL

"bin\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)" : "$(VCINSTALLDIR)Redist\MSVC\$(MSVC_VERSION)\MergeModules\$(VC142REDIST_MSM)"
	copy /y $** $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll" del /f /q "bin\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll"
	-if exist "bin\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll"    del /f /q "bin\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll"
	-if exist "bin\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll"           del /f /q "bin\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll"
	-if exist "bin\$(CFG)\$(PLAT)\lzo2.dll"                         del /f /q "bin\$(CFG)\$(PLAT)\lzo2.dll"
	-if exist "bin\$(CFG)\$(PLAT)\wintun.dll"                       del /f /q "bin\$(CFG)\$(PLAT)\wintun.dll"
	-if exist "bin\$(CFG)\$(PLAT)\openvpn.exe"                      del /f /q "bin\$(CFG)\$(PLAT)\openvpn.exe"
	-if exist "bin\$(CFG)\$(PLAT)\openvpnserv.exe"                  del /f /q "bin\$(CFG)\$(PLAT)\openvpnserv.exe"
	-if exist "bin\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)"               del /f /q "bin\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)"


######################################################################
# Client-specific rules
######################################################################

!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgPlatClient.mak"

!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
