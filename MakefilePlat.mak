#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\OpenVPN\$(PLAT)\config" : "$(OUTPUT_DIR)\OpenVPN\$(PLAT)"
	if not exist $@ md $@

Clean ::
	-if exist "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\config" rd  /s /q "$(OUTPUT_DIR)\OpenVPN\$(PLAT)\config"
