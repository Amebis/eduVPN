#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#


######################################################################
# Building
######################################################################

"bin\OpenVPN\$(PLAT)\config" : "bin\OpenVPN\$(PLAT)"
	if not exist $@ md $@

Clean ::
	-if exist "bin\OpenVPN\$(PLAT)\config" rd  /s /q "bin\OpenVPN\$(PLAT)\config"
