/*
	eduVPN - VPN for education and research

	Copyright: 2022 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

#define _WINSOCKAPI_ // Prevent inclusion of winsock.h in windows.h.
#include "driver.h"

winstd::library wg::driver::dll;
WIREGUARD_CREATE_ADAPTER_FUNC* wg::driver::WireGuardCreateAdapter;
WIREGUARD_OPEN_ADAPTER_FUNC* wg::driver::WireGuardOpenAdapter;
WIREGUARD_CLOSE_ADAPTER_FUNC* wg::driver::WireGuardCloseAdapter;
WIREGUARD_GET_ADAPTER_LUID_FUNC* wg::driver::WireGuardGetAdapterLUID;
WIREGUARD_GET_RUNNING_DRIVER_VERSION_FUNC* wg::driver::WireGuardGetRunningDriverVersion;
WIREGUARD_DELETE_DRIVER_FUNC* wg::driver::WireGuardDeleteDriver;
WIREGUARD_SET_LOGGER_FUNC* wg::driver::WireGuardSetLogger;
WIREGUARD_SET_ADAPTER_LOGGING_FUNC* wg::driver::WireGuardSetAdapterLogging;
WIREGUARD_GET_ADAPTER_STATE_FUNC* wg::driver::WireGuardGetAdapterState;
WIREGUARD_SET_ADAPTER_STATE_FUNC* wg::driver::WireGuardSetAdapterState;
WIREGUARD_GET_CONFIGURATION_FUNC* wg::driver::WireGuardGetConfiguration;
WIREGUARD_SET_CONFIGURATION_FUNC* wg::driver::WireGuardSetConfiguration;
