#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION_MAJ=1
BUNDLE_VERSION_MIN=0
BUNDLE_VERSION_REV=13
BUNDLE_VERSION=$(BUNDLE_VERSION_MAJ).$(BUNDLE_VERSION_MIN).$(BUNDLE_VERSION_REV)

CORE_VERSION_MAJ=1
CORE_VERSION_MIN=0
CORE_VERSION_REV=13
CORE_VERSION=$(CORE_VERSION_MAJ).$(CORE_VERSION_MIN).$(CORE_VERSION_REV)
CORE_VERSION_GUID={8C8D0D69-B3BB-444C-BD32-2941AA86A3BF}

OPENVPN_VERSION_MAJ=2
OPENVPN_VERSION_MIN=4
OPENVPN_VERSION_REV=4
OPENVPN_VERSION_BUILD=1
OPENVPN_VERSION=$(OPENVPN_VERSION_MAJ).$(OPENVPN_VERSION_MIN).$(OPENVPN_VERSION_REV).$(OPENVPN_VERSION_BUILD)
OPENVPN_VERSION_GUID={723B8EBF-346D-4D57-8CF2-775CE54C6E17}

OUTPUT_DIR=bin
SETUP_DIR=$(OUTPUT_DIR)\Setup

# Default testing configuration and platform
CFG=Debug
!IF "$(PROCESSOR_ARCHITECTURE)" == "AMD64"
PLAT=x64
!ELSE
PLAT=x86
!ENDIF

# Utility default flags
REG_FLAGS=/f
NUGET_FLAGS=-Verbosity quiet
MSBUILD_FLAGS=/m /v:minimal /nologo
CSCRIPT_FLAGS=//Nologo
WIX_WIXCOP_FLAGS=-nologo "-set1$(MAKEDIR)\wixcop.xml"
WIX_CANDLE_FLAGS=-nologo \
	-deduVPN.OpenVPN.Version="$(OPENVPN_VERSION)" -deduVPN.OpenVPN.ProductGUID="$(OPENVPN_VERSION_GUID)" \
	-deduVPN.Core.Version="$(CORE_VERSION)" -deduVPN.Core.ProductGUID="$(CORE_VERSION_GUID)" \
	-deduVPN.Version="$(BUNDLE_VERSION)" \
	-ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension
WIX_LIGHT_FLAGS=-nologo -dcl:high -spdb -sice:ICE03 -sice:ICE60 -sice:ICE61 -sice:ICE82 -ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension
WIX_INSIGNIA_FLAGS=-nologo


######################################################################
# Default target
######################################################################

All :: \
	Setup


######################################################################
# Registration
######################################################################

Register :: \
	RegisterOpenVPNInteractiveService \
	RegisterSettings \
	RegisterShortcuts

Unregister :: \
	UnregisterShortcuts \
	UnregisterSettings \
	UnregisterOpenVPNInteractiveService

RegisterSettings :: \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe"
	reg.exe add "HKCR\org.eduvpn.app"                    /v "URL Protocol" /t REG_SZ /d ""                                                                     $(REG_FLAGS)
	reg.exe add "HKCR\org.eduvpn.app\DefaultIcon"        /ve               /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe,1"          $(REG_FLAGS)
	reg.exe add "HKCR\org.eduvpn.app\shell\open\command" /ve               /t REG_SZ /d "\"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe\" \"%1\"" $(REG_FLAGS)

UnregisterSettings ::
	-reg.exe delete "HKCR\org.eduvpn.app"                    /v "URL Protocol" $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKCR\org.eduvpn.app\DefaultIcon"        /ve               $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKCR\org.eduvpn.app\shell\open\command" /ve               $(REG_FLAGS) > NUL 2>&1

RegisterShortcuts :: \
	"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\eduVPN Client.lnk"

UnregisterShortcuts ::
	-if exist "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\eduVPN Client.lnk" del /f /q "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\eduVPN Client.lnk"

RegisterOpenVPNInteractiveService :: \
	UnregisterOpenVPNInteractiveServiceSCM \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libeay32.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\ssleay32.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll"
	reg.exe add "HKLM\Software\OpenVPN$$eduVPN" /v "exe_path"         /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$eduVPN" /v "config_dir"       /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$eduVPN" /v "config_ext"       /t REG_SZ /d "conf"                                                $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$eduVPN" /v "log_dir"          /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$eduVPN" /v "log_append"       /t REG_SZ /d "0"                                                   $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$eduVPN" /v "priority"         /t REG_SZ /d "NORMAL_PRIORITY_CLASS"                               $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$eduVPN" /v "ovpn_admin_group" /t REG_SZ /d "Users"                                               $(REG_FLAGS)
	sc.exe create OpenVPNServiceInteractive$$eduVPN \
		binpath= "\"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe\" -instance interactive $$eduVPN" \
		DisplayName= "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll,-3" \
		type= own \
		start= auto \
		depend= "tap0901/Dhcp"
	sc.exe description OpenVPNServiceInteractive$$eduVPN "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll,-4"
	net.exe start OpenVPNServiceInteractive$$eduVPN

UnregisterOpenVPNInteractiveService :: \
	UnregisterOpenVPNInteractiveServiceSCM
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN" /v "exe_path"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN" /v "config_dir"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN" /v "config_ext"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN" /v "log_dir"          $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN" /v "log_append"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN" /v "priority"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN" /v "ovpn_admin_group" $(REG_FLAGS) > NUL 2>&1

UnregisterOpenVPNInteractiveServiceSCM ::
	-net.exe stop OpenVPNServiceInteractive$$eduVPN > NUL 2>&1
	-sc.exe delete OpenVPNServiceInteractive$$eduVPN > NUL 2>&1


######################################################################
# Setup
######################################################################

Setup :: \
	SetupBuild \
	SetupMSI \
	SetupExe

SetupExe :: \
	"$(SETUP_DIR)\eduVPNClient_$(BUNDLE_VERSION).exe"


######################################################################
# Shortcut creation
######################################################################

"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\eduVPN Client.lnk" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll"
	cscript.exe "bin\MkLnk.wsf" //Nologo $@ "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe" \
		/F:"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
		/LN:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-1" \
		/C:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-2"


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\Release\eduVPNClient_$(BUNDLE_VERSION).exe" : \
	"eduVPN.wxl" \
	"eduVPN.Install\eduVPN.thm.wxl" \
	"eduVPN.Install\eduVPN.thm.sl.wxl" \
	"eduVPN.Install\eduVPN.thm.xml" \
	"eduVPN.Install\eduVPN.logo.png" \
	"$(OUTPUT_DIR)\Release\eduVPN.wixobj" \
	"$(OUTPUT_DIR)\Release\TAP-Windows.wixobj" \
	"$(SETUP_DIR)\eduVPNOpenVPN_$(OPENVPN_VERSION)_x86.msi" \
	"$(SETUP_DIR)\eduVPNOpenVPN_$(OPENVPN_VERSION)_x64.msi" \
	"$(SETUP_DIR)\eduVPNCore_$(CORE_VERSION)_x86.msi" \
	"$(SETUP_DIR)\eduVPNCore_$(CORE_VERSION)_x64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en-US -loc "eduVPN.wxl" -out $@ "$(OUTPUT_DIR)\Release\eduVPN.wixobj" "$(OUTPUT_DIR)\Release\TAP-Windows.wixobj"

Clean ::
	-if exist "$(OUTPUT_DIR)\Release\eduVPNClient_*.exe" del /f /q "$(OUTPUT_DIR)\Release\eduVPNClient_*.exe"

!IFDEF MANIFESTCERTIFICATETHUMBPRINT

"$(OUTPUT_DIR)\Release\x86\Engine_eduVPNClient_$(BUNDLE_VERSION).exe" : "$(OUTPUT_DIR)\Release\eduVPNClient_$(BUNDLE_VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ib $** -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /d "eduVPN Client" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\Release\x86\Engine_eduVPNClient_*.exe" del /f /q "$(OUTPUT_DIR)\Release\x86\Engine_eduVPNClient_*.exe"

"$(SETUP_DIR)\eduVPNClient_$(BUNDLE_VERSION).exe" : \
	"$(OUTPUT_DIR)\Release\eduVPNClient_$(BUNDLE_VERSION).exe" \
	"$(OUTPUT_DIR)\Release\x86\Engine_eduVPNClient_$(BUNDLE_VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ab "$(OUTPUT_DIR)\Release\x86\Engine_eduVPNClient_$(BUNDLE_VERSION).exe" "$(OUTPUT_DIR)\Release\eduVPNClient_$(BUNDLE_VERSION).exe" -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /d "eduVPN Client" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL

!ELSE

"$(SETUP_DIR)\eduVPNClient_$(BUNDLE_VERSION).exe" : "$(OUTPUT_DIR)\Release\eduVPNClient_$(BUNDLE_VERSION).exe"
	copy /y $** $@ > NUL

!ENDIF

Clean ::
	-if exist "$(SETUP_DIR)\eduVPNClient_$(BUNDLE_VERSION).exe" del /f /q "$(SETUP_DIR)\eduVPNClient_$(BUNDLE_VERSION).exe"


######################################################################
# Configuration specific rules
######################################################################

CFG=Debug
!INCLUDE "MakefileCfg.mak"

CFG=Release
!INCLUDE "MakefileCfg.mak"
