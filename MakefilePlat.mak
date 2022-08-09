#
#   eduVPN - VPN for education and research
#
#   Copyright: 2017-2022 The Commons Conservancy
#   SPDX-License-Identifier: GPL-3.0+
#

!IF "$(PLAT)" == "x64"
PLAT_VCPKG=x64
!ELSEIF "$(PLAT)" == "ARM64"
PLAT_VCPKG=arm64
!ELSE
PLAT_VCPKG=x86
!ENDIF


######################################################################
# Building
######################################################################

BuildVcpkg \
BuildVcpkg-$(PLAT) ::
!IFNDEF APPVEYOR
	if not exist "$(VCPKG_ROOT)\vcpkg.exe" "$(VCPKG_ROOT)\bootstrap-vcpkg.bat" -disableMetrics
	"$(VCPKG_ROOT)\vcpkg.exe" install --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(PLAT_VCPKG)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun
!ENDIF

CleanVcpkg \
CleanVcpkg-$(PLAT) ::
!IFNDEF APPVEYOR
	-if exist "$(VCPKG_ROOT)\vcpkg.exe" "$(VCPKG_ROOT)\vcpkg.exe" remove --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(PLAT_VCPKG)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun
!ENDIF
