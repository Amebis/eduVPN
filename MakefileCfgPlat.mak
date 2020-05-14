#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

MSVC_VERSION = \
!INCLUDE "$(VCINSTALLDIR)Auxiliary\Build\Microsoft.VCRedistVersion.default.txt"

SETUP_TARGET=$(PLAT)
!IF "$(CFG)" == "Debug"
SETUP_TARGET=$(SETUP_TARGET)D
!ENDIF

!IF "$(PLAT)" == "x64"
OPENSSL_PLAT=-$(PLAT)
!ELSE
OPENSSL_PLAT=
!ENDIF

# WiX parameters
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG) \
	-arch $(PLAT) \
	-dTargetDir="bin\$(CFG)\$(PLAT)\\" \
	-dTAPWinPre.VersionInformational="$(TAPWINPRE_VERSION) $(SETUP_TARGET)" \
	-dOpenVPN.VersionInformational="$(OPENVPN_VERSION) $(SETUP_TARGET)" \
	-dOpenSSL.Platform="$(OPENSSL_PLAT)" \
	-dCore.VersionInformational="$(CORE_VERSION) $(SETUP_TARGET)"
!IF "$(PLAT)" == "x64"
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFiles64Folder"
!ELSE
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFilesFolder"
!ENDIF

!IF "$(CFG)" == "Debug"
VC142REDIST_MSM=Microsoft_VC142_DebugCRT_$(PLAT).msm
!ELSE
VC142REDIST_MSM=Microsoft_VC142_CRT_$(PLAT).msm
!ENDIF


######################################################################
# Setup
######################################################################

!IF "$(CFG)" == "Release"
SetupBuild :: \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\tap0901.cer" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe"

SetupBuild ::
	bin\nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)
!ENDIF


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)" : "$(OUTPUT_DIR)\$(CFG)"
	if not exist $@ md $@

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\tap0901.cer" : "$(OUTPUT_DIR)\Setup\tap0901.cer"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll" \
"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll" ::
	bin\nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Clean ::
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\liblzo2-2.dll"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\libpkcs11-helper-1.dll"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\openvpn.exe"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\openvpnserv.exe"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\config" : "$(OUTPUT_DIR)\$(CFG)\$(PLAT)"
	if not exist $@ md $@

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\tap0901.cer"                      del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\tap0901.cer"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll"    del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll"                    del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll"           del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe"                      del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe"                  del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\config"                           rd  /s /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\config"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)" : "$(VCINSTALLDIR)Redist\MSVC\$(MSVC_VERSION)\MergeModules\$(VC142REDIST_MSM)"
	copy /y $** $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj" : "OpenVPN.Resources\OpenVPN.Resources.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj" : "eduEd25519\eduEd25519\eduEd25519.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEx.dll.wixobj" : "eduEx\eduEx\eduEx.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj" : "eduJSON\eduJSON\eduJSON.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj" : "eduOAuth\eduOAuth\eduOAuth.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj" : "eduOpenVPN\eduOpenVPN\eduOpenVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj" : "eduVPN\eduVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj" : "eduVPN.Views\eduVPN.Views.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj" : "eduVPN.Resources\eduVPN.Resources.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj"       del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj"              del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEx.dll.wixobj"                   del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEx.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj"                 del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj"                del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj"              del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj"                  del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj"            del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj"        del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj"


######################################################################
# Client-specific rules
######################################################################

!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgPlatClient.mak"

!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
