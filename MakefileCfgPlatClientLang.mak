#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
!IF "$(LANG)" == "en-US"
WIX_LOC_FILE=eduVPN.wxl
!ELSE
WIX_LOC_FILE=eduVPN.$(LANG).wxl
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_$(LANG).msi" : \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN.wixobj" \
	"bin\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(VERSION)_$(SETUP_TARGET)_$(LANG).msi" : \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduEx.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduJSON.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduVPN.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).msi" del /f /q "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).msi"
	-if exist "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).msi"    del /f /q "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).msi"

!IF "$(LANG)" == "en-US"
# The en-US localization serves as the base. Therefore, it does not produce a diff MST.
!ELSE

"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_$(LANG).mst" : \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_en-US.msi" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(VERSION)_$(SETUP_TARGET)_$(LANG).mst" : \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(VERSION)_$(SETUP_TARGET)_en-US.msi" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(VERSION)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).mst" del /f /q "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).mst"
	-if exist "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).mst"    del /f /q "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).mst"

!ENDIF
