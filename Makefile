#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.255.0

TAPWIN_VERSION=9.24.5.1
EDUVPN_TAPWIN_VERSION_GUID={9A7F021A-EB4C-401C-9196-60589CE51C70}
LETSCONNECT_TAPWIN_VERSION_GUID={4F24A76F-3164-468E-BCE0-AB0D81C7867B}

OPENVPN_VERSION=2.5.0.10
EDUVPN_OPENVPN_VERSION_GUID={88A64758-C09D-4CFF-ADD8-2D02BF9262B7}
LETSCONNECT_OPENVPN_VERSION_GUID={526F6F8D-CB02-4DF6-A9C7-2510EFF0DA1A}

CORE_VERSION=1.255.0
EDUVPN_CORE_VERSION_GUID={08EFF53C-15CB-4CE5-BA6E-8C93274B795E}
LETSCONNECT_CORE_VERSION_GUID={AE73690F-0781-4D6C-8FED-B64B7F121165}

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
