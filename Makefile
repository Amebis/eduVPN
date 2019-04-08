#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.0.26

TAPWINPRE_VERSION=1.0.1
EDUVPN_TAPWINPRE_VERSION_GUID={D2ED47C4-82BE-481F-B0B8-81F98FFFC578}
LETSCONNECT_TAPWINPRE_VERSION_GUID={2AD4C4A1-A29E-4316-98DC-EE5A00F99120}

OPENVPN_VERSION=2.4.6.1
EDUVPN_OPENVPN_VERSION_GUID={C2C12548-4F1B-4E20-B376-39A54C5F371F}
LETSCONNECT_OPENVPN_VERSION_GUID={1E0B0573-624D-4B43-A2F7-ABB3A9E5BEE4}

CORE_VERSION=1.0.26
EDUVPN_CORE_VERSION_GUID={8C3E9E9C-CC76-4F8D-A6EB-B2355451F938}
LETSCONNECT_CORE_VERSION_GUID={E83D9B53-73E8-47BB-B384-FB6985094730}

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
WIX_LIGHT_FLAGS=-nologo -dcl:high -spdb -sice:ICE03 -sice:ICE60 -sice:ICE82 $(WIX_EXTENSIONS)
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
