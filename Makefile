#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.0.19

TAPWINPRE_VERSION=1.0.0
TAPWINPRE_VERSION_GUID={405BF01E-F159-4E55-8C22-FFA136CA9F95}

OPENVPN_VERSION=2.4.4.3
OPENVPN_VERSION_GUID={EAA1D8B1-192B-4CF9-A94F-3565F33E0F7F}

CORE_VERSION=1.0.19
EDUVPN_CORE_VERSION_GUID={78738805-E2DC-407B-89E7-A26D789BAF9E}
LETSCONNECT_CORE_VERSION_GUID={062F3398-EA38-4BFF-B440-4A1D918B5D82}

MSVC_VERSION=14.12.25810

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
WIX_EXTENSIONS=-ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension -ext WixIIsExtension
WIX_WIXCOP_FLAGS=-nologo "-set1$(MAKEDIR)\wixcop.xml"
WIX_CANDLE_FLAGS=-nologo \
	-deduVPN.TAPWinPre.Version="$(TAPWINPRE_VERSION)" -deduVPN.TAPWinPre.ProductGUID="$(TAPWINPRE_VERSION_GUID)" \
	-deduVPN.OpenVPN.Version="$(OPENVPN_VERSION)" -deduVPN.OpenVPN.ProductGUID="$(OPENVPN_VERSION_GUID)" \
	-deduVPN.Core.Version="$(CORE_VERSION)" -deduVPN.Core.ProductGUID="$(EDUVPN_CORE_VERSION_GUID)" \
	-dLetsConnect.Core.Version="$(CORE_VERSION)" -dLetsConnect.Core.ProductGUID="$(LETSCONNECT_CORE_VERSION_GUID)" \
	-deduVPN.Version="$(BUNDLE_VERSION)" \
	$(WIX_EXTENSIONS)
WIX_LIGHT_FLAGS=-nologo -dcl:high -spdb -sice:ICE03 -sice:ICE60 -sice:ICE61 -sice:ICE82 $(WIX_EXTENSIONS)
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
	RegisterShortcuts

Unregister :: \
	UnregisterShortcuts \
	UnregisterOpenVPNInteractiveService

RegisterShortcuts :: \
	"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\eduVPN Client.lnk" \
	"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\Lets Connect Client.lnk"

UnregisterShortcuts ::
	-if exist "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\eduVPN Client.lnk"       del /f /q "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\eduVPN Client.lnk"
	-if exist "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\Lets Connect Client.lnk" del /f /q "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\Lets Connect Client.lnk"

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
	reg.exe add "HKLM\Software\OpenVPN$$LetsConnect" /v "exe_path"         /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$LetsConnect" /v "config_dir"       /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$LetsConnect" /v "config_ext"       /t REG_SZ /d "conf"                                                $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$LetsConnect" /v "log_dir"          /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$LetsConnect" /v "log_append"       /t REG_SZ /d "0"                                                   $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$LetsConnect" /v "priority"         /t REG_SZ /d "NORMAL_PRIORITY_CLASS"                               $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$LetsConnect" /v "ovpn_admin_group" /t REG_SZ /d "Users"                                               $(REG_FLAGS)
	sc.exe create OpenVPNServiceInteractive$$LetsConnect \
		binpath= "\"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe\" -instance interactive $$LetsConnect" \
		DisplayName= "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll,-103" \
		type= own \
		start= auto \
		depend= "tap0901/Dhcp"
	sc.exe description OpenVPNServiceInteractive$$LetsConnect "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll,-104"
	net.exe start OpenVPNServiceInteractive$$LetsConnect

UnregisterOpenVPNInteractiveService :: \
	UnregisterOpenVPNInteractiveServiceSCM
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN"      /v "exe_path"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN"      /v "config_dir"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN"      /v "config_ext"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN"      /v "log_dir"          $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN"      /v "log_append"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN"      /v "priority"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$eduVPN"      /v "ovpn_admin_group" $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$LetsConnect" /v "exe_path"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$LetsConnect" /v "config_dir"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$LetsConnect" /v "config_ext"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$LetsConnect" /v "log_dir"          $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$LetsConnect" /v "log_append"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$LetsConnect" /v "priority"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$LetsConnect" /v "ovpn_admin_group" $(REG_FLAGS) > NUL 2>&1

UnregisterOpenVPNInteractiveServiceSCM ::
	-net.exe stop OpenVPNServiceInteractive$$eduVPN       > NUL 2>&1
	-sc.exe delete OpenVPNServiceInteractive$$eduVPN      > NUL 2>&1
	-net.exe stop OpenVPNServiceInteractive$$LetsConnect  > NUL 2>&1
	-sc.exe delete OpenVPNServiceInteractive$$LetsConnect > NUL 2>&1


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

"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\Lets Connect Client.lnk" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\LetsConnect.Client.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll"
	cscript.exe "bin\MkLnk.wsf" //Nologo $@ "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\LetsConnect.Client.exe" \
		/F:"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
		/LN:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-101" \
		/C:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-102"


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\Release\eduVPNClient_$(BUNDLE_VERSION).exe" : \
	"eduVPN.wxl" \
	"eduVPN.Install\eduVPN.thm.wxl" \
	"eduVPN.Install\eduVPN.thm.nl.wxl" \
	"eduVPN.Install\eduVPN.thm.sl.wxl" \
	"eduVPN.Install\eduVPN.thm.xml" \
	"eduVPN.Install\eduVPN.logo.png" \
	"$(OUTPUT_DIR)\Release\eduVPN.wixobj" \
	"$(OUTPUT_DIR)\Release\TAP-Windows.wixobj" \
	"$(SETUP_DIR)\eduVPNTAPWinPre_$(TAPWINPRE_VERSION)_x86.msi" \
	"$(SETUP_DIR)\eduVPNTAPWinPre_$(TAPWINPRE_VERSION)_x64.msi" \
	"$(SETUP_DIR)\eduVPNOpenVPN_$(OPENVPN_VERSION)_x86.msi" \
	"$(SETUP_DIR)\eduVPNOpenVPN_$(OPENVPN_VERSION)_x64.msi" \
	"$(SETUP_DIR)\eduVPNCore_$(CORE_VERSION)_x86.msi" \
	"$(SETUP_DIR)\eduVPNCore_$(CORE_VERSION)_x64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en-US -loc "eduVPN.wxl" -out $@ "$(OUTPUT_DIR)\Release\eduVPN.wixobj" "$(OUTPUT_DIR)\Release\TAP-Windows.wixobj"

"$(OUTPUT_DIR)\Release\LetsConnectClient_$(BUNDLE_VERSION).exe" : \
	"eduVPN.wxl" \
	"eduVPN.Install\eduVPN.thm.wxl" \
	"eduVPN.Install\eduVPN.thm.nl.wxl" \
	"eduVPN.Install\eduVPN.thm.sl.wxl" \
	"eduVPN.Install\eduVPN.thm.xml" \
	"eduVPN.Install\eduVPN.logo.png" \
	"$(OUTPUT_DIR)\Release\LetsConnect.wixobj" \
	"$(OUTPUT_DIR)\Release\TAP-Windows.wixobj" \
	"$(SETUP_DIR)\eduVPNTAPWinPre_$(TAPWINPRE_VERSION)_x86.msi" \
	"$(SETUP_DIR)\eduVPNTAPWinPre_$(TAPWINPRE_VERSION)_x64.msi" \
	"$(SETUP_DIR)\eduVPNOpenVPN_$(OPENVPN_VERSION)_x86.msi" \
	"$(SETUP_DIR)\eduVPNOpenVPN_$(OPENVPN_VERSION)_x64.msi" \
	"$(SETUP_DIR)\LetsConnectCore_$(CORE_VERSION)_x86.msi" \
	"$(SETUP_DIR)\LetsConnectCore_$(CORE_VERSION)_x64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en-US -loc "eduVPN.wxl" -out $@ "$(OUTPUT_DIR)\Release\LetsConnect.wixobj" "$(OUTPUT_DIR)\Release\TAP-Windows.wixobj"

Clean ::
	-if exist "$(OUTPUT_DIR)\Release\eduVPNClient_*.exe"      del /f /q "$(OUTPUT_DIR)\Release\eduVPNClient_*.exe"
	-if exist "$(OUTPUT_DIR)\Release\LetsConnectClient_*.exe" del /f /q "$(OUTPUT_DIR)\Release\LetsConnectClient_*.exe"

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

"$(SETUP_DIR)\LetsConnectClient_$(BUNDLE_VERSION).exe" : \
	"$(OUTPUT_DIR)\Release\LetsConnectClient_$(BUNDLE_VERSION).exe" \
	"$(OUTPUT_DIR)\Release\x86\Engine_eduVPNClient_$(BUNDLE_VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ab "$(OUTPUT_DIR)\Release\x86\Engine_LetsConnectClient_$(BUNDLE_VERSION).exe" "$(OUTPUT_DIR)\Release\eduVPNClient_$(BUNDLE_VERSION).exe" -o "$(@:"=).tmp"
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

CLIENT_TARGET=eduVPN

CFG=Debug
!INCLUDE "MakefileCfg.mak"

CFG=Release
!INCLUDE "MakefileCfg.mak"
