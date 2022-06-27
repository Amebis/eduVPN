#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

ImportTranslations ::
!IF EXISTS("$(USERPROFILE)\Downloads\$(TRANSIFEX_ORG)_$(TRANSIFEX_PROJ)_$(TRANSIFEX_RES).zip")
	cd "$(RESOURCE_DIR)"
	tar.exe -xf "$(USERPROFILE)\Downloads\$(TRANSIFEX_ORG)_$(TRANSIFEX_PROJ)_$(TRANSIFEX_RES).zip"
	move /y "$(TRANSIFEX_RES)_en_US.resx"  "Strings.resx"        > NUL
	move /y "$(TRANSIFEX_RES)_ar.resx"     "Strings.ar.resx"     > NUL
	move /y "$(TRANSIFEX_RES)_de.resx"     "Strings.de.resx"     > NUL
	move /y "$(TRANSIFEX_RES)_es_419.resx" "Strings.es.resx"     > NUL
	copy /y "Strings.resx"                 "Strings.es-ES.resx"  > NUL
	move /y "$(TRANSIFEX_RES)_fr.resx"     "Strings.fr.resx"     > NUL
	move /y "$(TRANSIFEX_RES)_nb.resx"     "Strings.nb.resx"     > NUL
	move /y "$(TRANSIFEX_RES)_nl.resx"     "Strings.nl.resx"     > NUL
	move /y "$(TRANSIFEX_RES)_pt_PT.resx"  "Strings.pt-PT.resx"  > NUL
	move /y "$(TRANSIFEX_RES)_sl.resx"     "Strings.sl.resx"     > NUL
	move /y "$(TRANSIFEX_RES)_tr.resx"     "Strings.tr.resx"     > NUL
	move /y "$(TRANSIFEX_RES)_uk.resx"     "Strings.uk.resx"     > NUL
	cd "$(MAKEDIR)"
!ELSE
	@echo $(USERPROFILE)\Downloads\$(TRANSIFEX_ORG)_$(TRANSIFEX_PROJ)_$(TRANSIFEX_RES).zip : Warning: File does not exist. Skipping...
!ENDIF
