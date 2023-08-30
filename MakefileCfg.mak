#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2023 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

!IF "$(CFG)" == "Debug"
CFG_TARGET=D
CFG_VCPKG=debug\\
CFG_GOFLAGS=$(GOFLAGS)
!ELSE
CFG_TARGET=
CFG_VCPKG=
CFG_GOFLAGS=$(GOFLAGS) -tags=release -ldflags "-s -w"
!ENDIF

# WiX parameters
WIX_CANDLE_FLAGS_CFG=$(WIX_CANDLE_FLAGS) \
	-dCfgTarget="$(CFG_TARGET)"


######################################################################
# Building
######################################################################

"bin\$(CFG)" :
	if not exist $@ md $@

!IF "$(CFG)" == "$(SETUP_CFG)"
SetupPDB :: \
	"bin\Setup\PDB_$(VERSION)$(CFG_TARGET).zip"

"bin\Setup\PDB_$(VERSION)$(CFG_TARGET).zip" :
	tar.exe -caf $@ $**
!ENDIF


######################################################################
# Platform specific rules
######################################################################

PLAT=x86
!INCLUDE "MakefileCfgPlat.mak"

PLAT=x64
!INCLUDE "MakefileCfgPlat.mak"

PLAT=ARM64
!INCLUDE "MakefileCfgPlat.mak"


######################################################################
# Client-specific rules
######################################################################

!IF $(BUILD_EDUVPN)
!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgClient.mak"
!ENDIF

!IF $(BUILD_LETSCONNECT)
!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgClient.mak"
!ENDIF

!IF $(BUILD_GOVVPN)
!INCLUDE "govVPN.mak"
!INCLUDE "MakefileCfgClient.mak"
!ENDIF


######################################################################
# Signing
######################################################################

MINISIGN_SETUP_FILES=
MINISIGN_SELFUPDATE_FILES=
!IF $(BUILD_EDUVPN)
MINISIGN_SETUP_FILES=$(MINISIGN_SETUP_FILES) \
	"bin\Setup\eduVPNClient_$(VERSION)$(CFG_TARGET).exe" \
	"bin\Setup\eduVPNClient_$(VERSION)$(CFG_TARGET)_ARM64.msi" \
	"bin\Setup\eduVPNClient_$(VERSION)$(CFG_TARGET)_x64.msi" \
	"bin\Setup\eduVPNClient_$(VERSION)$(CFG_TARGET)_x86.msi"
MINISIGN_SELFUPDATE_FILES=$(MINISIGN_SELFUPDATE_FILES) \
	"bin\Setup\eduVPN.windows.json"
!ENDIF
!IF $(BUILD_LETSCONNECT)
MINISIGN_SETUP_FILES=$(MINISIGN_SETUP_FILES) \
	"bin\Setup\LetsConnectClient_$(VERSION)$(CFG_TARGET).exe" \
	"bin\Setup\LetsConnectClient_$(VERSION)$(CFG_TARGET)_ARM64.msi" \
	"bin\Setup\LetsConnectClient_$(VERSION)$(CFG_TARGET)_x64.msi" \
	"bin\Setup\LetsConnectClient_$(VERSION)$(CFG_TARGET)_x86.msi"
MINISIGN_SELFUPDATE_FILES=$(MINISIGN_SELFUPDATE_FILES) \
	"bin\Setup\LetsConnect.windows.json"
!ENDIF
!IF $(BUILD_GOVVPN)
MINISIGN_SETUP_FILES=$(MINISIGN_SETUP_FILES) \
	"bin\Setup\govVPNClient_$(VERSION)$(CFG_TARGET).exe" \
	"bin\Setup\govVPNClient_$(VERSION)$(CFG_TARGET)_ARM64.msi" \
	"bin\Setup\govVPNClient_$(VERSION)$(CFG_TARGET)_x64.msi" \
	"bin\Setup\govVPNClient_$(VERSION)$(CFG_TARGET)_x86.msi"
MINISIGN_SELFUPDATE_FILES=$(MINISIGN_SELFUPDATE_FILES) \
	"bin\Setup\govVPN.windows.json"
!ENDIF

!IF "$(CFG)" == "$(SETUP_CFG)"
Setup ::
!IFDEF MINISIGN_KEY_AVAILABLE
	@echo Signing setup files
	minisign.exe -Sm $(MINISIGN_SETUP_FILES)
!ENDIF

Clean ::
	-if exist "bin\Setup\*Client_*.exe.minisig" del /f /q "bin\Setup\*Client_*.exe.minisig"
	-if exist "bin\Setup\*Client_*.msi.minisig" del /f /q "bin\Setup\*Client_*.msi.minisig"
!ENDIF

!IF "$(CFG)" == "Release"
Publish ::
!IFDEF MINISIGN_KEY_AVAILABLE
	@echo Signing self-update discovery files
	minisign.exe -Sm $(MINISIGN_SELFUPDATE_FILES)
!ENDIF
!ENDIF
