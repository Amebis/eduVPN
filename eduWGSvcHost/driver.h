/*
	eduVPN - VPN for education and research

	Copyright: 2022 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

#pragma once

#include <Windows.h>
#include <WinStd/Common.h>
#include <WinStd/Win.h>
#include <wireguard.h>
#include <vector>

// TODO: Remove once this lands in the upstream wireguard.h.
DEFINE_ENUM_FLAG_OPERATORS(WIREGUARD_PEER_FLAG)
DEFINE_ENUM_FLAG_OPERATORS(WIREGUARD_INTERFACE_FLAG)

namespace wg
{
	class driver
	{
	private:
		static winstd::library dll;

	public:
		static WIREGUARD_CREATE_ADAPTER_FUNC* WireGuardCreateAdapter;
		static WIREGUARD_OPEN_ADAPTER_FUNC* WireGuardOpenAdapter;
		static WIREGUARD_CLOSE_ADAPTER_FUNC* WireGuardCloseAdapter;
		static WIREGUARD_GET_ADAPTER_LUID_FUNC* WireGuardGetAdapterLUID;
		static WIREGUARD_GET_RUNNING_DRIVER_VERSION_FUNC* WireGuardGetRunningDriverVersion;
		static WIREGUARD_DELETE_DRIVER_FUNC* WireGuardDeleteDriver;
		static WIREGUARD_SET_LOGGER_FUNC* WireGuardSetLogger;
		static WIREGUARD_SET_ADAPTER_LOGGING_FUNC* WireGuardSetAdapterLogging;
		static WIREGUARD_GET_ADAPTER_STATE_FUNC* WireGuardGetAdapterState;
		static WIREGUARD_SET_ADAPTER_STATE_FUNC* WireGuardSetAdapterState;
		static WIREGUARD_GET_CONFIGURATION_FUNC* WireGuardGetConfiguration;
		static WIREGUARD_SET_CONFIGURATION_FUNC* WireGuardSetConfiguration;

		static void init()
		{
			dll = LoadLibraryExW(L"wireguard.dll", NULL, LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
			if (!dll)
				throw winstd::win_runtime_error("Failed to load wireguard.dll");
#define X(Name) ((*(FARPROC *)&Name = GetProcAddress(dll, #Name)) == NULL)
			if (X(WireGuardCreateAdapter) || X(WireGuardOpenAdapter) || X(WireGuardCloseAdapter) ||
				X(WireGuardGetAdapterLUID) || X(WireGuardGetRunningDriverVersion) || X(WireGuardDeleteDriver) ||
				X(WireGuardSetLogger) || X(WireGuardSetAdapterLogging) || X(WireGuardGetAdapterState) ||
				X(WireGuardSetAdapterState) || X(WireGuardGetConfiguration) || X(WireGuardSetConfiguration))
#undef X
				throw winstd::win_runtime_error("Failed to load all wireguard.dll entries");
		}

		class adapter : public winstd::handle<WIREGUARD_ADAPTER_HANDLE, NULL>
		{
			WINSTD_HANDLE_IMPL(adapter, NULL)

		public:
			virtual ~adapter()
			{
				if (m_h != invalid)
					free_internal();
			}

		protected:
			void free_internal() noexcept override
			{
				WireGuardCloseAdapter(m_h);
			}

		public:
			void get_configuration(_Inout_ std::vector<unsigned char, winstd::sanitizing_allocator<unsigned char>>& data)
			{
				DWORD bytes = (DWORD)data.size();
				for (;;)
				{
					if (WireGuardGetConfiguration(m_h, reinterpret_cast<WIREGUARD_INTERFACE*>(data.data()), &bytes))
					{
						data.erase(data.cbegin() + bytes, data.cend());
						break;
					}
					DWORD err = GetLastError();
					if (err != ERROR_MORE_DATA)
						throw winstd::win_runtime_error(err, "WireGuardGetConfiguration failed");
					data.insert(data.cend(), bytes - data.size(), 0);
				}
			}
		};
	};
}
