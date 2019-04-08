#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT=$(WIX_CANDLE_FLAGS_CFG_PLAT) \
	-dClientTarget="$(CLIENT_TARGET)"

!IF "$(CLIENT_TARGET)" == "eduVPN"

WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT=$(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) \
	-dTAPWinPre.ProductGUID="$(EDUVPN_TAPWINPRE_VERSION_GUID)" \
	-dOpenVPN.ProductGUID="$(EDUVPN_OPENVPN_VERSION_GUID)" \
	-dCore.ProductGUID="$(EDUVPN_CORE_VERSION_GUID)" \
!IF "$(PLAT)" == "x64"
	-dTAPWinPre.UpgradeGUID="{D6F9001D-05D8-4107-BCDD-41FB5520691E}" \
	-dOpenVPN.UpgradeGUID="{75C79E9E-5486-4568-814D-80C56E113FB8}" \
	-dCore.UpgradeGUID="{02EBD828-2565-4BCD-ABFF-E3F48C3F9A23}"
!ELSE
	-dTAPWinPre.UpgradeGUID="{FE30D203-C056-42D5-AF56-273F65A7709A}" \
	-dOpenVPN.UpgradeGUID="{258634EA-316E-434E-9AE9-13926FB26B12}" \
	-dCore.UpgradeGUID="{E3746042-5041-4E2F-83E8-0240EF3C60CA}"
!ENDIF
IDS_CLIENT_TITLE=1
IDS_CLIENT_DESCRIPTION=2
IDS_CLIENT_PUBLISHER=3
IDS_CORE_TITLE=4

!ELSEIF "$(CLIENT_TARGET)" == "LetsConnect"

WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT=$(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) \
	-dTAPWinPre.ProductGUID="$(LETSCONNECT_TAPWINPRE_VERSION_GUID)" \
	-dOpenVPN.ProductGUID="$(LETSCONNECT_OPENVPN_VERSION_GUID)" \
	-dCore.ProductGUID="$(LETSCONNECT_CORE_VERSION_GUID)" \
!IF "$(PLAT)" == "x64"
	-dTAPWinPre.UpgradeGUID="{CF82B6AD-EA1D-4DBA-8328-84E164701793}" \
	-dOpenVPN.UpgradeGUID="{4EB92ECE-8B6E-4FFF-9DD2-8626FB90BCD3}" \
	-dCore.UpgradeGUID="{090C24F6-1F11-4DB8-9338-68E3526139F5}"
!ELSE
	-dTAPWinPre.UpgradeGUID="{BC858264-4A39-4917-AB32-6A8DA8C12C72}" \
	-dOpenVPN.UpgradeGUID="{973B9D4D-16BD-4CEC-8B6C-7729D58B0BF9}" \
	-dCore.UpgradeGUID="{76E93AAB-1F90-4AA3-B7EE-F697A4B1B479}"
!ENDIF
IDS_CLIENT_TITLE=101
IDS_CLIENT_DESCRIPTION=102
IDS_CLIENT_PUBLISHER=103
IDS_CORE_TITLE=104

!ELSE
!ERROR Unknown CLIENT_TARGET
!ENDIF


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
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(VC141REDIST_MSM)"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) "$(CLIENT_TARGET)Core.wxs"
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) -dVC150RedistMSM="$(VC141REDIST_MSM)" -out $@ "$(CLIENT_TARGET)Core.wxs"

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj" : "$(CLIENT_TARGET).Client\$(CLIENT_TARGET).Client.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_PLAT_CLIENT) -out $@ $**

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre.wixobj"   del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN.wixobj"     del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core.wixobj"        del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core.wixobj"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj"

"$(SETUP_DIR)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET).msi" : \
#	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_nl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_sl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_en-US.msi"
	copy /y "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_en-US.msi" "$(@:"=).tmp" > NUL
#	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_nl.mst" 1043 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_sl.mst" 1060 /L
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client TAP-Windows Prerequisites" /q "$(@:"=).tmp"
!ENDIF
	attrib.exe +r "$(@:"=).tmp"
	if exist $@ attrib.exe -r $@
	move /y "$(@:"=).tmp" $@ > NUL

"$(SETUP_DIR)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET).msi" : \
#	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_nl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_sl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_en-US.msi"
	copy /y "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_en-US.msi" "$(@:"=).tmp" > NUL
#	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_nl.mst" 1043 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_sl.mst" 1060 /L
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client OpenVPN Components" /q "$(@:"=).tmp"
!ENDIF
	attrib.exe +r "$(@:"=).tmp"
	if exist $@ attrib.exe -r $@
	move /y "$(@:"=).tmp" $@ > NUL

"$(SETUP_DIR)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET).msi" : \
#	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_nl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_sl.mst" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_en-US.msi"
	copy /y "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_en-US.msi" "$(@:"=).tmp" > NUL
#	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_nl.mst" 1043 /L
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:AddStorage "$(@:"=).tmp" "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_sl.mst" 1060 /L
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
