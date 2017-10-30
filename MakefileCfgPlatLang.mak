#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

!IF "$(LANG)" == "en-US"
WIX_LOC_FILE=eduVPN.wxl
!ELSE
WIX_LOC_FILE=eduVPN.$(LANG).wxl
!ENDIF


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_$(OPENVPN_VERSION_STR)_$(SETUP_TARGET)_$(LANG).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_$(PRODUCT_VERSION_STR)_$(SETUP_TARGET)_$(LANG).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_*_$(SETUP_TARGET)_$(LANG).msi" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_*_$(SETUP_TARGET)_$(LANG).msi"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_*_$(SETUP_TARGET)_$(LANG).msi"    del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_*_$(SETUP_TARGET)_$(LANG).msi"

!IF "$(LANG)" == "en-US"
# The en-US localization serves as the base. Therefore, it does not produce a diff MST.
!ELSE

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_$(OPENVPN_VERSION_STR)_$(SETUP_TARGET)_$(LANG).mst" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_$(OPENVPN_VERSION_STR)_$(SETUP_TARGET)_en-US.msi" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_$(OPENVPN_VERSION_STR)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_$(PRODUCT_VERSION_STR)_$(SETUP_TARGET)_$(LANG).mst" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_$(PRODUCT_VERSION_STR)_$(SETUP_TARGET)_en-US.msi" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_$(PRODUCT_VERSION_STR)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_*_$(SETUP_TARGET)_$(LANG).mst" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNOpenVPN_*_$(SETUP_TARGET)_$(LANG).mst"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_*_$(SETUP_TARGET)_$(LANG).mst"    del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPNCore_*_$(SETUP_TARGET)_$(LANG).mst"

!ENDIF
