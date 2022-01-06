#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

# WiX parameters
WIX_CANDLE_FLAGS_CFG=$(WIX_CANDLE_FLAGS)


######################################################################
# Building
######################################################################

"bin\$(CFG)" :
	if not exist $@ md $@


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

!INCLUDE "eduVPN.mak"
!INCLUDE "MakefileCfgClient.mak"

!INCLUDE "LetsConnect.mak"
!INCLUDE "MakefileCfgClient.mak"
