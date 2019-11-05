#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

BUNDLE_VERSION=1.0.28

TAPWINPRE_VERSION=1.0.2
EDUVPN_TAPWINPRE_VERSION_GUID={29C3BFAB-EDC3-45F1-A5A7-B93E4216176F}
LETSCONNECT_TAPWINPRE_VERSION_GUID={102FC18E-FBEF-4AF8-8189-EA32CFDFC89C}

OPENVPN_VERSION=2.4.8
EDUVPN_OPENVPN_VERSION_GUID={F598BA53-BECF-4114-9154-7E8415D7F00D}
LETSCONNECT_OPENVPN_VERSION_GUID={3DF09BC8-AF7D-4CAE-BF42-2518BF4FBE95}

CORE_VERSION=1.0.28
EDUVPN_CORE_VERSION_GUID={5232255B-AEFF-48EB-B7D2-D9DDE5EB9239}
LETSCONNECT_CORE_VERSION_GUID={FDBF4850-F78E-4B4C-8D07-8085983537EB}

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
