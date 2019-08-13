#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
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

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_$(LANG).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_$(LANG).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\OpenVPN.Resources.dll.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -sice:ICE61 -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_$(LANG).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Views.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Resources.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET).Client.exe.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_*_$(SETUP_TARGET)_$(LANG).msi" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_*_$(SETUP_TARGET)_$(LANG).msi"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).msi"   del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).msi"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).msi"      del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).msi"

!IF "$(LANG)" == "en-US"
# The en-US localization serves as the base. Therefore, it does not produce a diff MST.
!ELSE

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_$(LANG).mst" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_en-US.msi" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_$(TAPWINPRE_VERSION)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_$(LANG).mst" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_en-US.msi" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_$(OPENVPN_VERSION)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_$(LANG).mst" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_en-US.msi" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_$(CORE_VERSION)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_*_$(SETUP_TARGET)_$(LANG).mst" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)TAPWinPre_*_$(SETUP_TARGET)_$(LANG).mst"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).mst"   del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)OpenVPN_*_$(SETUP_TARGET)_$(LANG).mst"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).mst"      del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Core_*_$(SETUP_TARGET)_$(LANG).mst"

!ENDIF
