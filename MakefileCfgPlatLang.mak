!IF "$(LANG)" == "en"
# The English localization serves as the base. Therefore, it does not produce resource DLLs.
!ELSE
!IF "$(CFG)" == "$(SETUP_CFG)"
SetupSign \
!ENDIF
Sign : \
	"bin\$(CFG)\$(PLAT)\$(LANG)\eduOpenVPN.resources.dll" \
	"bin\$(CFG)\$(PLAT)\$(LANG)\eduVPN.resources.dll" \
	"bin\$(CFG)\$(PLAT)\$(LANG)\eduVPN.Views.resources.dll" \
	"bin\$(CFG)\$(PLAT)\$(LANG)\eduWireGuard.resources.dll"
!ENDIF
