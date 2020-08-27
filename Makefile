#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.0.30

TAPWINPRE_VERSION=1.0.2
EDUVPN_TAPWINPRE_VERSION_GUID={29C3BFAB-EDC3-45F1-A5A7-B93E4216176F}
LETSCONNECT_TAPWINPRE_VERSION_GUID={102FC18E-FBEF-4AF8-8189-EA32CFDFC89C}

OPENVPN_VERSION=2.6.0.2
EDUVPN_OPENVPN_VERSION_GUID={909004D7-4D8A-4AFE-A1D9-D66E35502BF0}
LETSCONNECT_OPENVPN_VERSION_GUID={A3BCA196-869C-4D18-B522-BB2C15A30349}

CORE_VERSION=1.0.30
EDUVPN_CORE_VERSION_GUID={41FE8D9A-4F4E-4C9D-8FA0-2C084D8AA2D2}
LETSCONNECT_CORE_VERSION_GUID={3B2CFCC2-382D-48EA-AC10-AA495ADDF660}

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
