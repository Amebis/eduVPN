#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
WIX_CANDLE_FLAGS_CFG_CLIENT=$(WIX_CANDLE_FLAGS_CFG) \
	-dClientTarget="$(CLIENT_TARGET)" \
	-dClientUpgradeCode="$(CLIENT_UPGRADE_CODE)" \
	-dClientAboutUrl="$(CLIENT_ABOUT_URL)"


######################################################################
# Setup
######################################################################

!IF "$(CFG)" == "Release"
SetupExe :: \
	"$(SETUP_DIR)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"

"$(SETUP_DIR)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" : \
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	"$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" \
	"$(OUTPUT_DIR)\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ab "$(OUTPUT_DIR)\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL
!ELSE
	"$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"
	copy /y $** $@ > NUL
!ENDIF

Clean ::
	-if exist "$(SETUP_DIR)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" del /f /q "$(SETUP_DIR)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"
!ENDIF


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj" : "eduVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_CLIENT) -out $@ $**

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj"

"$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" : \
	"eduVPN.wxl" \
	"Install\thm.wxl" \
	"Install\thm.de.wxl" \
	"Install\thm.fr.wxl" \
	"Install\thm.nl.wxl" \
	"Install\thm.sl.wxl" \
	"Install\thm.uk.wxl" \
	"Install\thm.xml" \
	"Install\$(CLIENT_TARGET)\logo.png" \
	"$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\TAP-Windows.wixobj" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_x86.msi" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_x64.msi" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_x86.msi" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_x64.msi" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_x86.msi" \
	"$(SETUP_DIR)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_x64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en-US -loc "eduVPN.wxl" -out $@ "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET).wixobj" "$(OUTPUT_DIR)\$(CFG)\TAP-Windows.wixobj"

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET)Client_*.exe" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET)Client_*.exe"

"$(OUTPUT_DIR)\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" : \
	"$(OUTPUT_DIR)\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ib $** -o "$(@:"=).tmp"
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
!ENDIF
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe" del /f /q "$(OUTPUT_DIR)\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe"
