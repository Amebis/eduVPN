#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
	-dPlatform="$(PLAT)" \
	-dTargetDir="bin\$(CFG)\$(PLAT)\\" \
	-dTAPWin.VersionInformational="$(TAPWIN_VERSION) $(SETUP_TARGET)" \
	-dOpenVPN.Dir="bin\OpenVPN\$(PLAT)\\" \
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
SetupBuild ::
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(PLAT)" : "bin\$(CFG)"
	if not exist $@ md $@

"bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll" \
"bin\$(CFG)\$(PLAT)\OpenVPN.Resources.dll" ::
	bin\nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Clean ::
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

"bin\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)" : "$(VCINSTALLDIR)Redist\MSVC\$(MSVC_VERSION)\MergeModules\$(VC142REDIST_MSM)"
	copy /y $** $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)" del /f /q "bin\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)"

"bin\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj" : "OpenVPN.Resources\OpenVPN.Resources.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj" : "eduEd25519\eduEd25519\eduEd25519.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduEx.dll.wixobj" : "eduEx\eduEx\eduEx.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduJSON.dll.wixobj" : "eduJSON\eduJSON\eduJSON.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj" : "eduOAuth\eduOAuth\eduOAuth.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj" : "eduOpenVPN\eduOpenVPN\eduOpenVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduVPN.dll.wixobj" : "eduVPN\eduVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj" : "eduVPN.Views\eduVPN.Views.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

"bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj" : "eduVPN.Resources\eduVPN.Resources.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT) -out $@ $**

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj" del /f /q "bin\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj"        del /f /q "bin\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduEx.dll.wixobj"             del /f /q "bin\$(CFG)\$(PLAT)\eduEx.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduJSON.dll.wixobj"           del /f /q "bin\$(CFG)\$(PLAT)\eduJSON.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj"          del /f /q "bin\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj"        del /f /q "bin\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduVPN.dll.wixobj"            del /f /q "bin\$(CFG)\$(PLAT)\eduVPN.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj"      del /f /q "bin\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj"
	-if exist "bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj"  del /f /q "bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj"


######################################################################
# Client-specific rules
######################################################################

!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgPlatClient.mak"

!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgPlatClient.mak"
