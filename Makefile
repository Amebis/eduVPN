#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

PRODUCT_NAME=eduVPN Client
PRODUCT_VERSION_MAJ=1
PRODUCT_VERSION_MIN=0
PRODUCT_VERSION_REV=9
PRODUCT_VERSION=$(PRODUCT_VERSION_MAJ).$(PRODUCT_VERSION_MIN).$(PRODUCT_VERSION_REV)
PRODUCT_VERSION_STR=$(PRODUCT_VERSION_MAJ).$(PRODUCT_VERSION_MIN)-alpha7

OUTPUT_DIR=bin
SETUP_DIR=$(OUTPUT_DIR)\Setup
SETUP_NAME=eduVPN-Client-Win

# Default testing configuration and platform
CFG=Debug
!IF "$(PROCESSOR_ARCHITECTURE)" == "AMD64"
PLAT=x64
!ELSE
PLAT=x86
!ENDIF

# Utility default flags
REG_FLAGS=/f
MSBUILD_FLAGS=/m /v:minimal /nologo
CSCRIPT_FLAGS=//Nologo
WIX_WIXCOP_FLAGS=-nologo "-set1$(MAKEDIR)\wixcop.xml"
WIX_CANDLE_FLAGS=-nologo -deduVPN.Version="$(PRODUCT_VERSION)" -ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension
WIX_LIGHT_FLAGS=-nologo -dcl:high -spdb -sice:ICE03 -sice:ICE60 -sice:ICE61 -sice:ICE69 -sice:ICE82 -ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension
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
	"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(PRODUCT_NAME).lnk"

UnregisterShortcuts ::
	-if exist "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(PRODUCT_NAME).lnk" del /f /q "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(PRODUCT_NAME).lnk"

RegisterOpenVPNInteractiveService :: \
	UnregisterOpenVPNInteractiveServiceSCM \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libeay32.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\ssleay32.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll"
	reg.exe add "HKLM\Software\eduVPN" /v "exe_path"         /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" $(REG_FLAGS)
	reg.exe add "HKLM\Software\eduVPN" /v "config_dir"       /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\eduVPN" /v "config_ext"       /t REG_SZ /d "conf"                                                $(REG_FLAGS)
	reg.exe add "HKLM\Software\eduVPN" /v "log_dir"          /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\eduVPN" /v "log_append"       /t REG_SZ /d "0"                                                   $(REG_FLAGS)
	reg.exe add "HKLM\Software\eduVPN" /v "priority"         /t REG_SZ /d "NORMAL_PRIORITY_CLASS"                               $(REG_FLAGS)
	reg.exe add "HKLM\Software\eduVPN" /v "ovpn_admin_group" /t REG_SZ /d "Users"                                               $(REG_FLAGS)
	sc.exe create eduVPNServiceInteractive \
		binpath= "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe" \
		DisplayName= "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-4" \
		type= share \
		start= auto \
		depend= "tap0901/Dhcp"
	sc.exe description eduVPNServiceInteractive "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-5"
	net.exe start eduVPNServiceInteractive

UnregisterOpenVPNInteractiveService :: \
	UnregisterOpenVPNInteractiveServiceSCM
	-reg.exe delete "HKLM\Software\eduVPN" /v "exe_path"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\eduVPN" /v "config_dir"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\eduVPN" /v "config_ext"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\eduVPN" /v "log_dir"          $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\eduVPN" /v "log_append"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\eduVPN" /v "priority"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\eduVPN" /v "ovpn_admin_group" $(REG_FLAGS) > NUL 2>&1

UnregisterOpenVPNInteractiveServiceSCM ::
	-net.exe stop eduVPNServiceInteractive > NUL 2>&1
	-sc.exe delete eduVPNServiceInteractive > NUL 2>&1


######################################################################
# Setup
######################################################################

Setup :: \
	SetupBuild \
	SetupMSI \
	SetupExe

SetupExe :: \
	"$(SETUP_DIR)\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe"


######################################################################
# Shortcut creation
######################################################################

"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(PRODUCT_NAME).lnk" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll"
	cscript.exe "bin\MkLnk.wsf" //Nologo $@ "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe" \
		/F:"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
		/LN:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-1" \
		/C:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-2"


######################################################################
# Building
######################################################################

"OpenVPN\config-msvc-local.h" : "Makefile"
	copy /y << $@ > NUL
/* This file is auto-generated. */

#undef PACKAGE_NAME
#define PACKAGE_NAME "eduVPN"

#undef PACKAGE_STRING
#define PACKAGE_STRING "$(PRODUCT_NAME) $(PRODUCT_VERSION_STR)"

#undef PACKAGE_TARNAME
#define PACKAGE_TARNAME "eduvpn"

#undef PACKAGE
#define PACKAGE "eduvpn"

#undef PRODUCT_VERSION_MAJOR
#define PRODUCT_VERSION_MAJOR "$(PRODUCT_VERSION_MAJ)"

#undef PRODUCT_VERSION_MINOR
#define PRODUCT_VERSION_MINOR "$(PRODUCT_VERSION_MIN)"

#undef PRODUCT_VERSION_PATCH
#define PRODUCT_VERSION_PATCH "$(PRODUCT_VERSION_REV)"

#undef PACKAGE_VERSION
#define PACKAGE_VERSION "$(PRODUCT_VERSION_STR)"

#undef PRODUCT_VERSION
#define PRODUCT_VERSION "$(PRODUCT_VERSION_STR)"

#undef PRODUCT_BUGREPORT
#define PRODUCT_BUGREPORT "eduvpn@eduvpn.org"

#undef OPENVPN_VERSION_RESOURCE
#define OPENVPN_VERSION_RESOURCE $(PRODUCT_VERSION_MAJ),$(PRODUCT_VERSION_MIN),$(PRODUCT_VERSION_REV),0
<<NOKEEP

Clean ::
	-if exist "OpenVPN\config-msvc-local.h" del /f /q "OpenVPN\config-msvc-local.h"

"$(OUTPUT_DIR)\Release\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" : \
	"eduVPN.wxl" \
	"eduVPN.Install\eduVPN.thm.wxl" \
	"eduVPN.Install\eduVPN.thm.sl.wxl" \
	"eduVPN.Install\eduVPN.thm.xml" \
	"eduVPN.Install\eduVPN.logo.png" \
	"$(OUTPUT_DIR)\Release\eduVPN.wixobj" \
	"$(OUTPUT_DIR)\Release\TAP-Windows.wixobj" \
	"$(SETUP_DIR)\$(SETUP_NAME)_$(PRODUCT_VERSION_STR)_x86.msi" \
	"$(SETUP_DIR)\$(SETUP_NAME)_$(PRODUCT_VERSION_STR)_x64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en-US -loc "eduVPN.wxl" -out $@ "$(OUTPUT_DIR)\Release\eduVPN.wixobj" "$(OUTPUT_DIR)\Release\TAP-Windows.wixobj"

Clean ::
	-if exist "$(OUTPUT_DIR)\Release\$(SETUP_NAME)_*.exe" del /f /q "$(OUTPUT_DIR)\Release\$(SETUP_NAME)_*.exe"

!IFDEF MANIFESTCERTIFICATETHUMBPRINT
"$(OUTPUT_DIR)\Release\x86\Engine_$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" : "$(OUTPUT_DIR)\Release\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ib $** -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /d "$(PRODUCT_NAME)" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\Release\x86\Engine_$(SETUP_NAME)_*.exe" del /f /q "$(OUTPUT_DIR)\Release\x86\Engine_$(SETUP_NAME)_*.exe"

"$(SETUP_DIR)\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" : \
	"$(OUTPUT_DIR)\Release\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" \
	"$(OUTPUT_DIR)\Release\x86\Engine_$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ab "$(OUTPUT_DIR)\Release\x86\Engine_$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" "$(OUTPUT_DIR)\Release\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /d "$(PRODUCT_NAME)" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL
!ELSE
"$(SETUP_DIR)\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" : "$(OUTPUT_DIR)\Release\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe"
	copy /y $** $@ > NUL
!ENDIF

Clean ::
	-if exist "$(SETUP_DIR)\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe" del /f /q "$(SETUP_DIR)\$(SETUP_NAME)_$(PRODUCT_VERSION_STR).exe"


######################################################################
# Configuration specific rules
######################################################################

CFG=Debug
!INCLUDE "MakefileCfg.mak"

CFG=Release
!INCLUDE "MakefileCfg.mak"
