#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy
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

!IF "$(CFG)" == "$(SETUP_CFG)"
SetupExe :: \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe"

"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" : \
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" \
	"bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ab "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" "bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" -o "$(@:"=).tmp"
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL
!ELSE
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe"
	copy /y $** $@ > NUL
!ENDIF

Clean ::
	-if exist "bin\Setup\$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\Setup\$(CLIENT_TARGET)Client_*.exe"
	-if exist "bin\Setup\$(CLIENT_TARGET)Client_*.msi" del /f /q "bin\Setup\$(CLIENT_TARGET)Client_*.msi"
!ENDIF

!IF "$(CFG)" == "Release"
Publish :: \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" \
!IFDEF VIRUSTOTALAPIKEY
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).vtanalysis" \
!ENDIF
	"bin\Setup\$(CLIENT_TARGET).windows.json" \
	"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)" \
	"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)\SURF.$(CLIENT_TARGET)Client.installer.yaml" \
	"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)\SURF.$(CLIENT_TARGET)Client.locale.en-US.yaml" \
	"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)\SURF.$(CLIENT_TARGET)Client.yaml"

"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).vtanalysis" : "bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe"

Clean ::
	-if exist "bin\Setup\$(CLIENT_TARGET)Client_*$(CFG_TARGET).vtanalysis" del /f /q "bin\Setup\$(CLIENT_TARGET)Client_*$(CFG_TARGET).vtanalysis"

"bin\Setup\$(CLIENT_TARGET).windows.json" : "bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe"
	move /y << "$(@:"=).tmp" > NUL
{"arguments": "/install",
"uri": [ "https://github.com/Amebis/eduVPN/releases/download/$(VERSION)/$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" ],
"version": "$(VERSION)",
"changelog_uri": "https://github.com/Amebis/eduVPN/blob/master/CHANGES.md",
<<NOKEEP
	for /f %%a in ('CertUtil.exe -hashfile $** SHA256 ^| findstr /r "^[0-9a-f]*$$"') do @echo "hash-sha256": "%%a"}>> "$(@:"=).tmp"
	move /y "$(@:"=).tmp" $@ > NUL

"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client" : "bin\Setup\winget-manifests\s\SURF"
	if not exist $@ md $@

"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)" : "bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client"
	if not exist $@ md $@

"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)\SURF.$(CLIENT_TARGET)Client.installer.yaml" : \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_ARM64.msi" \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x64.msi" \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x86.msi"
	move /y << "$(@:"=).tmp" > NUL
# Created using eduVPN build system
# yaml-language-server: $$schema=https://aka.ms/winget-manifest.installer.1.1.0.schema.json

PackageIdentifier: SURF.$(CLIENT_TARGET)Client
PackageVersion: $(VERSION)
Installers:
- InstallerLocale: en-US
  Architecture: arm64
  InstallerType: wix
  InstallerUrl: https://github.com/Amebis/eduVPN/releases/download/$(VERSION)/$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_ARM64.msi
<<NOKEEP
	for /f %%a in ('CertUtil.exe -hashfile "bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_ARM64.msi" SHA256 ^| findstr /r "^[0-9a-f]*$$"') do @echo ^ ^ InstallerSha256: %%a>> "$(@:"=).tmp"
	type << >> "$(@:"=).tmp"
  ProductCode: '$(CLIENT_PRODUCT_CODE)'
- InstallerLocale: en-US
  Architecture: x64
  InstallerType: wix
  InstallerUrl: https://github.com/Amebis/eduVPN/releases/download/$(VERSION)/$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x64.msi
<<NOKEEP
	for /f %%a in ('CertUtil.exe -hashfile "bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x64.msi" SHA256 ^| findstr /r "^[0-9a-f]*$$"') do @echo ^ ^ InstallerSha256: %%a>> "$(@:"=).tmp"
	type << >> "$(@:"=).tmp"
  ProductCode: '$(CLIENT_PRODUCT_CODE)'
- InstallerLocale: en-US
  Architecture: x86
  InstallerType: wix
  InstallerUrl: https://github.com/Amebis/eduVPN/releases/download/$(VERSION)/$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x86.msi
<<NOKEEP
	for /f %%a in ('CertUtil.exe -hashfile "bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x86.msi" SHA256 ^| findstr /r "^[0-9a-f]*$$"') do @echo ^ ^ InstallerSha256: %%a>> "$(@:"=).tmp"
	type << >> "$(@:"=).tmp"
  ProductCode: '$(CLIENT_PRODUCT_CODE)'
ManifestType: installer
ManifestVersion: 1.1.0

<<NOKEEP
	move /y "$(@:"=).tmp" $@ > NUL

"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)\SURF.$(CLIENT_TARGET)Client.locale.en-US.yaml" :
	move /y << "$(@:"=).tmp" > NUL
# Created using eduVPN build system
# yaml-language-server: $$schema=https://aka.ms/winget-manifest.defaultLocale.1.1.0.schema.json

PackageIdentifier: SURF.$(CLIENT_TARGET)Client
PackageVersion: $(VERSION)
PackageLocale: en-US
Publisher: SURF
PublisherUrl: $(CLIENT_ABOUT_URI)
PackageName: $(CLIENT_TITLE) Client
License: GPL-3.0
ShortDescription: Access your institute's network or the Internet using an encrypted connection.
Tags:
- vpn
- eduvpn
ReleaseNotesUrl: https://github.com/Amebis/eduVPN/releases/tag/$(VERSION)
ManifestType: defaultLocale
ManifestVersion: 1.1.0

<<NOKEEP
	move /y "$(@:"=).tmp" $@ > NUL

"bin\Setup\winget-manifests\s\SURF\$(CLIENT_TARGET)Client\$(VERSION)\SURF.$(CLIENT_TARGET)Client.yaml" :
	move /y << "$(@:"=).tmp" > NUL
# Created using eduVPN build system
# yaml-language-server: $$schema=https://aka.ms/winget-manifest.version.1.1.0.schema.json

PackageIdentifier: SURF.$(CLIENT_TARGET)Client
PackageVersion: $(VERSION)
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.1.0

<<NOKEEP
	move /y "$(@:"=).tmp" $@ > NUL
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(CLIENT_TARGET).wixobj" : "eduVPN.wxs"
	"$(WIX)bin\wixcop.exe" $(WIX_WIXCOP_FLAGS) $**
	"$(WIX)bin\candle.exe" $(WIX_CANDLE_FLAGS_CFG_CLIENT) -out $@ $**

Clean ::
	-if exist "bin\$(CFG)\$(CLIENT_TARGET).wixobj" del /f /q "bin\$(CFG)\$(CLIENT_TARGET).wixobj"

"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" : \
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
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x86.msi" \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_x64.msi" \
	"bin\Setup\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET)_ARM64.msi"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:en -loc "Install\eduVPN.wxl" -out $@ "bin\$(CFG)\$(CLIENT_TARGET).wixobj"

Clean ::
	-if exist "bin\$(CFG)\$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\$(CFG)\$(CLIENT_TARGET)Client_*.exe"

"bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe" : \
	"bin\$(CFG)\$(CLIENT_TARGET)Client_$(VERSION)$(CFG_TARGET).exe"
	"$(WIX)bin\insignia.exe" $(WIX_INSIGNIA_FLAGS) -ib $** -o "$(@:"=).tmp"
!IFDEF MANIFESTCERTIFICATETHUMBPRINT
	signtool.exe sign /sha1 "$(MANIFESTCERTIFICATETHUMBPRINT)" /fd sha256 /as /tr "$(MANIFESTTIMESTAMPRFC3161URL)" /td sha256 /d "$(CLIENT_TITLE) Client" /q "$(@:"=).tmp"
!ENDIF
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe" del /f /q "bin\$(CFG)\x86\Engine_$(CLIENT_TARGET)Client_*.exe"
