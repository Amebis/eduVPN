#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.0.19

TAPWINPRE_VERSION=1.0.0
EDUVPN_TAPWINPRE_VERSION_GUID={405BF01E-F159-4E55-8C22-FFA136CA9F95}
LETSCONNECT_TAPWINPRE_VERSION_GUID={2C231F83-74E1-4EB1-A3AA-A4139211B098}

OPENVPN_VERSION=2.4.4.3
EDUVPN_OPENVPN_VERSION_GUID={EAA1D8B1-192B-4CF9-A94F-3565F33E0F7F}
LETSCONNECT_OPENVPN_VERSION_GUID={FA39751B-0E5A-434B-A667-F459BC0FF31C}

CORE_VERSION=1.0.19
EDUVPN_CORE_VERSION_GUID={78738805-E2DC-407B-89E7-A26D789BAF9E}
LETSCONNECT_CORE_VERSION_GUID={062F3398-EA38-4BFF-B440-4A1D918B5D82}

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
