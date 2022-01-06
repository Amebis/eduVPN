#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
!IF "$(LANG)" == "en"
WIX_LOC_FILE=Install\eduVPN.wxl
!ELSE
WIX_LOC_FILE=Install\$(LANG)\eduVPN.wxl
!ENDIF


######################################################################
# Building
######################################################################

"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)_$(SETUP_TARGET)_$(LANG).msi" : "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client.wixobj"
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) -cultures:$(LANG) -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_*_$(SETUP_TARGET)_$(LANG).msi" del /f /q "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_*_$(SETUP_TARGET)_$(LANG).msi"

!IF "$(LANG)" == "en"
# The English localization serves as the base. Therefore, it does not produce a diff MST.
!ELSE

"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)_$(SETUP_TARGET)_$(LANG).mst" : \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)_$(SETUP_TARGET)_en.msi" \
	"bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_$(VERSION)_$(SETUP_TARGET)_$(LANG).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@

Clean ::
	-if exist "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_*_$(SETUP_TARGET)_$(LANG).mst" del /f /q "bin\$(CFG)\$(PLAT)\$(CLIENT_TARGET)Client_*_$(SETUP_TARGET)_$(LANG).mst"

!ENDIF
