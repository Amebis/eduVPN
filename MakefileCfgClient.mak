#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
	"bin\Setup\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"

"bin\Setup\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" : \
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" \
	"bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ab "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" "bin\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL
!ELSE
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"
	copy /y $** $@ > NUL
!ENDIF

Clean ::
	-if exist "bin\Setup\$(CLIENT_TARGET)Client_*.exe"  del /f /q "bin\Setup\$(CLIENT_TARGET)Client_*.exe"
	-if exist "bin\Setup\$(CLIENT_TARGET)TAPWin_*.msi"  del /f /q "bin\Setup\$(CLIENT_TARGET)TAPWin_*.msi"
	-if exist "bin\Setup\$(CLIENT_TARGET)OpenVPN_*.msi" del /f /q "bin\Setup\$(CLIENT_TARGET)OpenVPN_*.msi"
	-if exist "bin\Setup\$(CLIENT_TARGET)Core_*.msi"    del /f /q "bin\Setup\$(CLIENT_TARGET)Core_*.msi"
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(CLIENT_TARGET).wixobj" : "eduVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_CLIENT) -out $@ $**

Clean ::
	-if exist "bin\$(CFG)\$(CLIENT_TARGET).wixobj" del /f /q "bin\$(CFG)\$(CLIENT_TARGET).wixobj"

"bin\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" : \
	"eduVPN.wxl" \
	"Install\thm.wxl" \
#	"Install\thm.ar.wxl" \
	"Install\thm.de.wxl" \
	"Install\thm.fr.wxl" \
	"Install\thm.nl.wxl" \
	"Install\thm.sl.wxl" \
	"Install\thm.uk.wxl" \
	"Install\thm.xml" \
	"Install\$(CLIENT_TARGET)\logo.png" \
	"bin\$(CFG)\$(CLIENT_TARGET).wixobj" \
	"bin\Setup\$(CLIENT_TARGET)TAPWin_$(TAPWIN_VERSION)_x86.msi" \
	"bin\Setup\$(CLIENT_TARGET)TAPWin_$(TAPWIN_VERSION)_x64.msi" \
	"bin\Setup\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_x86.msi" \
	"bin\Setup\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_x64.msi" \
	"bin\Setup\$(CLIENT_TARGET)Core_$(CORE_VERSION)_x86.msi" \
	"bin\Setup\$(CLIENT_TARGET)Core_$(CORE_VERSION)_x64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en-US -loc "eduVPN.wxl" -out $@ "bin\$(CFG)\$(CLIENT_TARGET).wixobj"

Clean ::
	-if exist "bin\$(CFG)\$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\$(CFG)\$(CLIENT_TARGET)Client_*.exe"

"bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe" : \
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(BUNDLE_VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ib $** -o "$(@:"=).tmp"
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
!ENDIF
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe"
