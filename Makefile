#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.0.34

TAPWIN_VERSION=9.24.4.2
EDUVPN_TAPWIN_VERSION_GUID={739A267F-5B41-4828-AC35-41E7B7683856}
LETSCONNECT_TAPWIN_VERSION_GUID={3C594C0B-A2A8-4CE5-BF86-759E9E7B7C25}

OPENVPN_VERSION=2.5.0.7
EDUVPN_OPENVPN_VERSION_GUID={B142EC9C-D49D-45D8-A6F6-23B635F55BF5}
LETSCONNECT_OPENVPN_VERSION_GUID={DAB10C94-809C-4FB3-9FBB-56039D0003C1}

CORE_VERSION=1.0.34
EDUVPN_CORE_VERSION_GUID={476E2607-8BA4-42B0-8B30-B6AC07158419}
LETSCONNECT_CORE_VERSION_GUID={B0216147-E14D-49AB-A4D2-9ACA3FA0A8AC}

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
WIX_EXTENSIONS=-ext WixNetFxExtension -ext WixUtilExtension -ext WixBalExtension
WIX_WIXCOP_FLAGS=-nologo "-set1$(MAKEDIR)\wixcop.xml"
WIX_CANDLE_FLAGS=-nologo \
    -dTAPWin.Version="$(TAPWIN_VERSION)" \
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
