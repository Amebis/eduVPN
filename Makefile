#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.0.20

TAPWINPRE_VERSION=1.0.0
EDUVPN_TAPWINPRE_VERSION_GUID={405BF01E-F159-4E55-8C22-FFA136CA9F95}
LETSCONNECT_TAPWINPRE_VERSION_GUID={2C231F83-74E1-4EB1-A3AA-A4139211B098}

OPENVPN_VERSION=2.4.4.4
EDUVPN_OPENVPN_VERSION_GUID={5CDB1F77-0AC9-41DD-8B5D-6B3E2A97C898}
LETSCONNECT_OPENVPN_VERSION_GUID={C524F93F-60A7-44B3-963F-EF50FA376BEC}

CORE_VERSION=1.0.20
EDUVPN_CORE_VERSION_GUID={803E3649-24C4-4F8B-AB5E-59EE7B3736BF}
LETSCONNECT_CORE_VERSION_GUID={951A4B8E-6347-47CC-BF72-002DE45F1234}

MSVC_VERSION=14.12.25810

OUTPUT_DIR=bin
SETUP_DIR=$(OUTPUT_DIR)\Setup

# Default testing configuration and platform
TEST_CFG=Debug
!IF "$(PROCESSOR_ARCHITECTURE)" == "AMD64"
TEST_PLAT=x64
!ELSE
TEST_PLAT=x86
!ENDIF

# Utility default flags
REG_FLAGS=/f
NUGET_FLAGS=-Verbosity quiet
MSBUILD_FLAGS=/m /v:minimal /nologo
CSCRIPT_FLAGS=//Nologo
WIX_EXTENSIONS=-ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension -ext WixIIsExtension
WIX_WIXCOP_FLAGS=-nologo "-set1$(MAKEDIR)\wixcop.xml"
WIX_CANDLE_FLAGS=-nologo \
	-dTAPWinPre.Version="$(TAPWINPRE_VERSION)" \
	-dOpenVPN.Version="$(OPENVPN_VERSION)" \
	-dCore.Version="$(CORE_VERSION)" \
	-dVersion="$(BUNDLE_VERSION)" \
	$(WIX_EXTENSIONS)
WIX_LIGHT_FLAGS=-nologo -dcl:high -spdb -sice:ICE03 -sice:ICE60 -sice:ICE61 -sice:ICE82 $(WIX_EXTENSIONS)
WIX_INSIGNIA_FLAGS=-nologo


######################################################################
# Default target
######################################################################

All :: \
	Setup


######################################################################
# Registration
######################################################################

Register :: \
	RegisterOpenVPNInteractiveService \
	RegisterShortcuts

Unregister :: \
	UnregisterShortcuts \
	UnregisterOpenVPNInteractiveService


######################################################################
# Setup
######################################################################

Setup :: \
	SetupBuild \
	SetupMSI \
	SetupExe


######################################################################
# Configuration specific rules
######################################################################

CFG=Debug
!INCLUDE "MakefileCfg.mak"

CFG=Release
!INCLUDE "MakefileCfg.mak"
