#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT=$(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) \
	-dClientTarget="$(CLIENT_TARGET)" \
	-dClientTitle="$(CLIENT_TITLE)" \
	-dClientUpgradeCode="$(CLIENT_UPGRADE_CODE)" \
	-dClientAboutUri="$(CLIENT_ABOUT_URI)" \
	-dClientUrn="$(CLIENT_URN)" \
	-dClientId="$(CLIENT_ID)" \
	-dIDS_CLIENT_PREFIX="$(IDS_CLIENT_PREFIX)"


######################################################################
# Setup
######################################################################

!IF "$(CFG)" == "$(SETUP_CFG)"
SetupMSI :: \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT).msi"
!ENDIF


######################################################################
# Registration
######################################################################

!IF "$(CFG)" == "$(TEST_CFG)"
!IF "$(PLAT)" == "$(TEST_PLAT)"
Register :: \
	UnregisterServices \
	Build-$(CFG)-$(PLAT) \
	"bin\$(CFG)\$(PLAT)\config"
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /ve                   /t REG_SZ /d "$(MAKEDIR)\bin\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "exe_path"         /t REG_SZ /d "$(MAKEDIR)\bin\$(CFG)\$(PLAT)\openvpn.exe" $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_dir"       /t REG_SZ /d "$(MAKEDIR)\bin\$(CFG)\$(PLAT)\config"      $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_ext"       /t REG_SZ /d "conf"                                      $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_dir"          /t REG_SZ /d "$(MAKEDIR)\bin\$(CFG)\$(PLAT)\config"      $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_append"       /t REG_SZ /d "0"                                         $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "priority"         /t REG_SZ /d "NORMAL_PRIORITY_CLASS"                     $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "ovpn_admin_group" /t REG_SZ /d "Users"                                     $(REG_FLAGS)
	sc.exe create "OpenVPNServiceInteractive$$$(CLIENT_TARGET)" \
		binpath= "\"$(MAKEDIR)\bin\$(CFG)\$(PLAT)\openvpnserv.exe\" -instance interactive $$$(CLIENT_TARGET)" \
		DisplayName= "@$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-$(IDS_CLIENT_PREFIX)5" \
		type= own \
		start= auto \
		depend= "Dhcp"
	sc.exe description "OpenVPNServiceInteractive$$$(CLIENT_TARGET)" "@$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-$(IDS_CLIENT_PREFIX)6"
	net.exe start "OpenVPNServiceInteractive$$$(CLIENT_TARGET)"
	reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Eventlog\Application\eduWGSvcHost$$$(CLIENT_TARGET)" /v "EventMessageFile" /t REG_EXPAND_SZ /d "$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduWGSvcHost.exe" $(REG_FLAGS)
	reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Eventlog\Application\eduWGSvcHost$$$(CLIENT_TARGET)" /v "TypesSupported"   /t REG_DWORD     /d 7                                                $(REG_FLAGS)
	sc.exe create "eduWGManager$$$(CLIENT_TARGET)" \
		binpath= "\"$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduWGSvcHost.exe\" $(CLIENT_TARGET) Manager" \
		DisplayName= "@$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduWGSvcHost.exe,-$(IDS_CLIENT_PREFIX)1" \
		type= own \
		start= auto
	sc.exe description "eduWGManager$$$(CLIENT_TARGET)" "@$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduWGSvcHost.exe,-$(IDS_CLIENT_PREFIX)2"
	net.exe start "eduWGManager$$$(CLIENT_TARGET)"
	cscript.exe "bin\MkLnk.wsf" //Nologo "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(CLIENT_TITLE) Client.lnk" "$(MAKEDIR)\bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe" \
		/F:"$(MAKEDIR)\bin\$(CFG)\$(PLAT)" \
		/LN:"@$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-$(IDS_CLIENT_PREFIX)1" \
		/C:"@$(MAKEDIR)\bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-$(IDS_CLIENT_PREFIX)2"

Unregister ::
	-if exist "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(CLIENT_TITLE) Client.lnk" del /f /q "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(CLIENT_TITLE) Client.lnk"

Unregister :: \
	UnregisterServices
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /ve                   $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "exe_path"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_dir"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_ext"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_dir"          $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_append"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "priority"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "ovpn_admin_group" $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\SYSTEM\CurrentControlSet\Services\Eventlog\Application\eduWGSvcHost$$$(CLIENT_TARGET)" /va $(REG_FLAGS) > NUL 2>&1

UnregisterServices ::
	-net.exe stop "OpenVPNServiceInteractive$$$(CLIENT_TARGET)"  > NUL 2>&1
	-sc.exe delete "OpenVPNServiceInteractive$$$(CLIENT_TARGET)" > NUL 2>&1
	-net.exe stop "eduWGManager$$$(CLIENT_TARGET)"  > NUL 2>&1
	-sc.exe delete "eduWGManager$$$(CLIENT_TARGET)" > NUL 2>&1
!ENDIF
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client.wixobj" : \
	"eduVPNClient.wxs" \
	"bin\$(CFG)\$(PLAT)" \
	"bin\$(CFG)\$(PLAT)\$(VCREDIST_MSM)"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) "eduVPNClient.wxs"
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) -dVCRedistMSM="$(VCREDIST_MSM)" -out $@ "eduVPNClient.wxs"

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client.wixobj" del /f /q "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client.wixobj"

"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT).msi" : \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_ar.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_de.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_fr.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_nl.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_sl.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_tr.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_uk.mst" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_en.msi"
	copy /y "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_en.msi" "$(@:"=).tmp" > NUL
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_ar.mst" 1025  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_de.mst" 1031  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 22538 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 11274 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 16394 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 13322 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 9226  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 5130  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 23562 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 7178  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 12298 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 4106  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 18442 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 2058  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 19466 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 6154  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 10250 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 20490 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 15370 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 17418 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 21514 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 14346 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_es.mst" 8202  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_fr.mst" 1036  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_nl.mst" 1043  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_sl.mst" 1060  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_tr.mst" 1055  /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_$(PLAT)_uk.mst" 1058  /L
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
!ENDIF
	attrib.exe +r "$(@:"=).tmp"
	if exist $@ attrib.exe -r $@
	move /y "$(@:"=).tmp" $@ > NUL


######################################################################
# Locale-specific rules
######################################################################

LANG=ar
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=en
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=de
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=es
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=es-ES
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=fr
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=nl
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=pt-PT
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=sl
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=tr
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=uk
!INCLUDE "MakefileCfgPlatClientLang.mak"
