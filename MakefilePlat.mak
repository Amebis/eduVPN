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
	if not exist vcpkg\vcpkg.exe vcpkg\bootstrap-vcpkg.bat -disableMetrics
	vcpkg\vcpkg.exe install --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(PLAT_VCPKG)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun

CleanVcpkg \
CleanVcpkg-$(PLAT) ::
	-if exist vcpkg\vcpkg.exe vcpkg\vcpkg.exe remove --overlay-ports=openvpn\contrib\vcpkg-ports --overlay-triplets=openvpn\contrib\vcpkg-triplets --triplet "$(PLAT_VCPKG)-windows-ovpn" openssl lz4 lzo pkcs11-helper tap-windows6 wintun
