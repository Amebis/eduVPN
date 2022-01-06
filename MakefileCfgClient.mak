#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
WIX_CANDLE_FLAGS_CFG_CLIENT=$(WIX_CANDLE_FLAGS_CFG) \
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

!IF "$(CFG)" == "Release"
SetupExe :: \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION).exe"

Publish :: \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION).exe" \
	"bin\Setup\$(CLIENT_TARGET).windows.json" \
	"bin\Setup\$(CLIENT_TARGET).windows.json.minisig"

"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION).exe" : \
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION).exe" \
	"bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ab "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(VERSION).exe" "bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION).exe" -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL
!ELSE
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION).exe"
	copy /y $** $@ > NUL
!ENDIF

"bin\Setup\$(CLIENT_TARGET).windows.json" : "bin\Setup\$(CLIENT_TARGET)Client_$(VERSION).exe"
	move /y << "$(@:"=).tmp" > NUL
{"arguments": "/install",
"uri": [ "https://github.com/Amebis/eduVPN/releases/download/$(VERSION)/$(CLIENT_TARGET)Client_$(VERSION).exe" ],
"version": "$(VERSION)",
"changelog_uri": "https://github.com/Amebis/eduVPN/blob/master/CHANGES.md",
<<NOKEEP
	for /f %%a in ('CertUtil.exe -hashfile $** SHA256 ^| findstr /r "^[0-9a-f]*$$"') do @echo "hash-sha256": "%%a"}>> "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "bin\Setup\$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\Setup\$(CLIENT_TARGET)Client_*.exe"
	-if exist "bin\Setup\$(CLIENT_TARGET)Client_*.msi" del /f /q "bin\Setup\$(CLIENT_TARGET)Client_*.msi"
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(CLIENT_TARGET).wixobj" : "eduVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_CLIENT) -out $@ $**

Clean ::
	-if exist "bin\$(CFG)\$(CLIENT_TARGET).wixobj" del /f /q "bin\$(CFG)\$(CLIENT_TARGET).wixobj"

"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION).exe" : \
	"Install\eduVPN.wxl" \
	"Install\thm.wxl" \
	"Install\de\thm.wxl" \
	"Install\es\thm.wxl" \
	"Install\fr\thm.wxl" \
	"Install\nl\thm.wxl" \
	"Install\sl\thm.wxl" \
	"Install\tr\thm.wxl" \
	"Install\uk\thm.wxl" \
	"Install\thm.xml" \
	"Install\$(CLIENT_TARGET)\logo.png" \
	"bin\$(CFG)\$(CLIENT_TARGET).wixobj" \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)_x86.msi" \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)_x64.msi" \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)_ARM64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en -loc "Install\eduVPN.wxl" -out $@ "bin\$(CFG)\$(CLIENT_TARGET).wixobj"

Clean ::
	-if exist "bin\$(CFG)\$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\$(CFG)\$(CLIENT_TARGET)Client_*.exe"

"bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(VERSION).exe" : \
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ib $** -o "$(@:"=).tmp"
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
!ENDIF
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe"
