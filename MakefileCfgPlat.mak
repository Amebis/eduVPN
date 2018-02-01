#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

!IF "$(PLAT)" == "x86"
PLAT_NATIVE=Win32
!ELSE
PLAT_NATIVE=$(PLAT)
!ENDIF

SETUP_TARGET=$(PLAT)
!IF "$(CFG)" == "Debug"
SETUP_TARGET=$(SETUP_TARGET)D
!ENDIF

# WiX parameters
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG) \
	-arch $(PLAT) \
	-dTargetDir="bin\$(CFG)\$(PLAT)\\" \
	-dTAPWinPre.VersionInformational="$(TAPWINPRE_VERSION) $(SETUP_TARGET)" \
	-dOpenVPN.VersionInformational="$(OPENVPN_VERSION) $(SETUP_TARGET)" \
	-dCore.VersionInformational="$(CORE_VERSION) $(SETUP_TARGET)"
!IF "$(PLAT)" == "x64"
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFiles64Folder"
!ELSE
WIX_CANDLE_FLAGS_CFG_PLAT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dProgramFilesFolder="ProgramFilesFolder"
!ENDIF

!IF "$(CFG)" == "Debug"
VC141REDIST_MSM=Microsoft_VC141_DebugCRT_$(PLAT).msm
!ELSE
VC141REDIST_MSM=Microsoft_VC141_CRT_$(PLAT).msm
!ENDIF


######################################################################
# Setup
######################################################################

!IF "$(CFG)" == "Release"
SetupBuild :: \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\tap0901.cer" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libeay32.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\ssleay32.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe"

SetupBuild ::
	nuget.exe restore $(NUGET_FLAGS)
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
	nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Clean ::
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

"OpenVPN\$(PLAT_NATIVE)-Output\$(CFG)\openvpnserv.exe" ::
	msbuild.exe "OpenVPN\openvpn.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT_NATIVE)" $(MSBUILD_FLAGS)

Clean ::
	-msbuild.exe "OpenVPN\openvpn.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT_NATIVE)" $(MSBUILD_FLAGS)

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libeay32.dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\libeay32.dll"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\liblzo2-2.dll"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\libpkcs11-helper-1.dll"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\openvpn.exe"
	copy /y $** $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe" : "OpenVPN\$(PLAT_NATIVE)-Output\$(CFG)\openvpnserv.exe"
	copy /y $** "$(@:"=).tmp" > NUL
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
!IF "$(CFG)" == "Release"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /d "OpenVPN Interactive Service" /q "$(@:"=).tmp"
!ENDIF
!ENDIF
	move /y "$(@:"=).tmp" $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\ssleay32.dll" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\ssleay32.dll"
	copy /y $** $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\tap0901.cer"            del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\tap0901.cer"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libeay32.dll"           del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libeay32.dll"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll"          del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe"            del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe"        del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\ssleay32.dll"           del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\ssleay32.dll"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC141REDIST_MSM)" : "$(VCINSTALLDIR)Redist\MSVC\$(MSVC_VERSION)\MergeModules\$(VC141REDIST_MSM)"
	copy /y $** $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC141REDIST_MSM)" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC141REDIST_MSM)"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj" : "OpenVPN.Resources\OpenVPN.Resources.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj" : "eduEd25519\eduEd25519\eduEd25519.wxs"
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
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj"                 del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj"                del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj"              del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj"                  del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj"            del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj"        del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj"


######################################################################
# Client-specific rules
######################################################################

CLIENT_TARGET=eduVPN
CLIENT_TITLE=eduVPN
!INCLUDE "MakefileCfgPlatClient.mak"

CLIENT_TARGET=LetsConnect
CLIENT_TITLE=Let's Connect!
!INCLUDE "MakefileCfgPlatClient.mak"
