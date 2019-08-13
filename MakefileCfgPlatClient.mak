#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT=$(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) \
	-dClientTarget="$(CLIENT_TARGET)" \
	-dClientTitle="$(CLIENT_TITLE)" \
	-dClientAboutUrl="$(CLIENT_ABOUT_URL)"

######################################################################
# Registration
######################################################################

!IF "$(CFG)" == "$(TEST_CFG)"
!IF "$(PLAT)" == "$(TEST_PLAT)"
RegisterShortcuts :: \
	"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(CLIENT_TITLE) Client.lnk"

UnregisterShortcuts ::
	-if exist "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(CLIENT_TITLE) Client.lnk" del /f /q "$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(CLIENT_TITLE) Client.lnk"

"$(PROGRAMDATA)\Microsoft\Windows\Start Menu\Programs\$(CLIENT_TITLE) Client.lnk" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll"
	cscript.exe "bin\MkLnk.wsf" //Nologo $@ "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe" \
		/F:"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
		/LN:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-$(IDS_CLIENT_TITLE)" \
		/C:"@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll,-$(IDS_CLIENT_DESCRIPTION)"

RegisterOpenVPNInteractiveService :: \
	UnregisterOpenVPNInteractiveServiceSCM \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libcrypto-1_1$(OPENSSL_PLAT).dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libssl-1_1$(OPENSSL_PLAT).dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\liblzo2-2.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\libpkcs11-helper-1.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\config"
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /ve                   /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "exe_path"         /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpn.exe" $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_dir"       /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\config"      $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_ext"       /t REG_SZ /d "conf"                                                $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_dir"          /t REG_SZ /d "$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)"             $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_append"       /t REG_SZ /d "0"                                                   $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "priority"         /t REG_SZ /d "NORMAL_PRIORITY_CLASS"                               $(REG_FLAGS)
	reg.exe add "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "ovpn_admin_group" /t REG_SZ /d "Users"                                               $(REG_FLAGS)
	sc.exe create "OpenVPNServiceInteractive$$$(CLIENT_TARGET)" \
		binpath= "\"$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\openvpnserv.exe\" -instance interactive $$$(CLIENT_TARGET)" \
		DisplayName= "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll,-$(IDS_CLIENT_PUBLISHER)" \
		type= own \
		start= auto \
		depend= "tap0901/Dhcp"
	sc.exe description "OpenVPNServiceInteractive$$$(CLIENT_TARGET)" "@$(MAKEDIR)\$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll,-$(IDS_CORE_TITLE)"
	net.exe start "OpenVPNServiceInteractive$$$(CLIENT_TARGET)"

UnregisterOpenVPNInteractiveService :: \
	UnregisterOpenVPNInteractiveServiceSCM
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /ve                   $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "exe_path"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_dir"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "config_ext"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_dir"          $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "log_append"       $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "priority"         $(REG_FLAGS) > NUL 2>&1
	-reg.exe delete "HKLM\Software\OpenVPN$$$(CLIENT_TARGET)" /v "ovpn_admin_group" $(REG_FLAGS) > NUL 2>&1

UnregisterOpenVPNInteractiveServiceSCM ::
	-net.exe stop "OpenVPNServiceInteractive$$$(CLIENT_TARGET)"  > NUL 2>&1
	-sc.exe delete "OpenVPNServiceInteractive$$$(CLIENT_TARGET)" > NUL 2>&1
!ENDIF
!ENDIF


######################################################################
# Setup
######################################################################

!IF "$(CFG)" == "Release"
SetupMSI :: \
	"$(SETUP_DIR)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET).msi" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET).msi" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET).msi"
!ENDIF


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe" ::
	nuget.exe restore $(NUGET_FLAGS)
	msbuild.exe "eduVPN.sln" /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

Clean ::
	-msbuild.exe "eduVPN.sln" /t:Clean /p:Configuration="$(CFG)" /p:Platform="$(PLAT)" $(MSBUILD_FLAGS)

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre.wixobj" : \
	"$(CLIENT_TARGET)TAPWinPre.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) "$(CLIENT_TARGET)TAPWinPre.wxs"
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) -out $@ "$(CLIENT_TARGET)TAPWinPre.wxs"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN.wixobj" : \
	"$(CLIENT_TARGET)OpenVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) "$(CLIENT_TARGET)OpenVPN.wxs"
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) -out $@ "$(CLIENT_TARGET)OpenVPN.wxs"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core.wixobj" : \
	"$(CLIENT_TARGET)Core.wxs" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC142REDIST_MSM)"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) "$(CLIENT_TARGET)Core.wxs"
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) -dVC150RedistMSM="$(VC142REDIST_MSM)" -out $@ "$(CLIENT_TARGET)Core.wxs"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj" : "$(CLIENT_TARGET).Client\$(CLIENT_TARGET).Client.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) -out $@ $**

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre.wixobj"   del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN.wixobj"     del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core.wixobj"        del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj"

"$(SETUP_DIR)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_nl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_sl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_uk.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_en-US.msi"
	copy /y "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_en-US.msi" "$(@:"=).tmp" > NUL
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_nl.mst" 1043 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_sl.mst" 1060 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_uk.mst" 1058 /L
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client TAP-Windows Prerequisites" /q "$(@:"=).tmp"
!ENDIF
	attrib.exe +r "$(@:"=).tmp"
	if exist $@ attrib.exe -r $@
	move /y "$(@:"=).tmp" $@ > NUL

"$(SETUP_DIR)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_nl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_sl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_uk.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_en-US.msi"
	copy /y "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_en-US.msi" "$(@:"=).tmp" > NUL
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_nl.mst" 1043 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_sl.mst" 1060 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_uk.mst" 1058 /L
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client OpenVPN Components" /q "$(@:"=).tmp"
!ENDIF
	attrib.exe +r "$(@:"=).tmp"
	if exist $@ attrib.exe -r $@
	move /y "$(@:"=).tmp" $@ > NUL

"$(SETUP_DIR)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_nl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_sl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_uk.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_en-US.msi"
	copy /y "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_en-US.msi" "$(@:"=).tmp" > NUL
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_nl.mst" 1043 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_sl.mst" 1060 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_uk.mst" 1058 /L
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client Core" /q "$(@:"=).tmp"
!ENDIF
	attrib.exe +r "$(@:"=).tmp"
	if exist $@ attrib.exe -r $@
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "$(SETUP_DIR)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET).msi" del /f /q "$(SETUP_DIR)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET).msi"
	-if exist "$(SETUP_DIR)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET).msi"     del /f /q "$(SETUP_DIR)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET).msi"
	-if exist "$(SETUP_DIR)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET).msi"           del /f /q "$(SETUP_DIR)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET).msi"


######################################################################
# Locale-specific rules
######################################################################

LANG=en-US
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=nl
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=sl
!INCLUDE "MakefileCfgPlatClientLang.mak"

LANG=uk
!INCLUDE "MakefileCfgPlatClientLang.mak"
