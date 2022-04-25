/*
	eduVPN - VPN for education and research

	Copyright: 2022 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

#define _WINSOCKAPI_ // Prevent inclusion of winsock.h in windows.h.
#include <Windows.h>
#include "driver.h"
#include "resource.h"
#include "ringlogger.h"
#include <Messages.h>
#include <Shlwapi.h>
#include <WinStd/SDDL.h>
#include <WinStd/Win.h>
#include <vector>

#pragma warning(disable: 4200) // Nonstandard extensions: This is MSVC-only source code.
#pragma warning(disable: 4815) // Some stack structs will have zero-sized members.

using namespace std;
using namespace winstd;
using namespace wg;

static HINSTANCE hInstance;
static WCHAR module_file_path[MAX_PATH];
static WCHAR config_folder_path[MAX_PATH];
static LPCWSTR client_id;
enum class client_type_t { eduvpn, letsconnect };
static client_type_t client_type;
static event quit;
static SERVICE_STATUS_HANDLE service_handle;
static SERVICE_STATUS service_status = { SERVICE_WIN32_OWN_PROCESS, SERVICE_START_PENDING, 0, NO_ERROR, 0, 0, 1000 };

static event_log service_log;

static void log(_In_ const exception& e)
{
	if (!service_log)
		return;

	wstring msg;
	MultiByteToWideChar(CP_ACP, 0, e.what(), -1, msg);

	auto e_win = dynamic_cast<const win_runtime_error*>(&e);
	if (e_win) {
		wstring
			number = wstring_printf(L"%u", e_win->number()),
			win_msg = e_win->msg();
		LPCWSTR strings[] = { msg.c_str(), number.c_str(), win_msg.c_str() };
		ReportEventW(service_log, EVENTLOG_ERROR_TYPE, 0, ERROR_WIN_RUNTIME_ERROR, NULL, _countof(strings), 0, strings, NULL);
	}
	else
	{
		LPCWSTR strings[] = { msg.c_str() };
		ReportEventW(service_log, EVENTLOG_ERROR_TYPE, 0, ERROR_EXCEPTION, NULL, _countof(strings), 0, strings, NULL);
	}
}

typedef unsigned short version_t[4];

static void module_version(_Out_ version_t& version)
{
	HRSRC hResInfo = FindResource(hInstance, MAKEINTRESOURCE(VS_VERSION_INFO), RT_VERSION);
	if (!hResInfo)
		throw win_runtime_error("Failed to find VS_VERSION_INFO resource");
	DWORD dwSize = SizeofResource(hInstance, hResInfo);
	HGLOBAL hResData = LoadResource(hInstance, hResInfo);
	if (!hResData)
		throw win_runtime_error("Failed to load VS_VERSION_INFO resource");
	LPVOID pRes = LockResource(hResData);
	if (!pRes)
		throw win_runtime_error("Failed to lock VS_VERSION_INFO resource");
	unique_ptr<unsigned char> pResCopy(new unsigned char[dwSize]);
	memcpy(pResCopy.get(), pRes, dwSize);
	VS_FIXEDFILEINFO* lpFfi;
	UINT uLen;
	VerQueryValueW(pResCopy.get(), L"\\", (LPVOID*)&lpFfi, &uLen);
	version[0] = HIWORD(lpFfi->dwFileVersionMS);
	version[1] = LOWORD(lpFfi->dwFileVersionMS);
	version[2] = HIWORD(lpFfi->dwFileVersionLS);
	version[3] = LOWORD(lpFfi->dwFileVersionLS);
}

static DWORD WINAPI manager_handler(_In_ DWORD dwControl, _In_opt_ DWORD dwEventType, _In_opt_ LPVOID lpEventData, _In_opt_ LPVOID lpContext)
{
	UNREFERENCED_PARAMETER(dwEventType);
	UNREFERENCED_PARAMETER(lpEventData);
	UNREFERENCED_PARAMETER(lpContext);

	switch (dwControl)
	{
	case SERVICE_CONTROL_INTERROGATE:
		SetServiceStatus(service_handle, &service_status);
		return NO_ERROR;

	case SERVICE_CONTROL_STOP:
	case SERVICE_CONTROL_SHUTDOWN:
		service_status.dwCurrentState = SERVICE_STOP_PENDING;
		service_status.dwControlsAccepted &= ~(SERVICE_ACCEPT_SHUTDOWN | SERVICE_ACCEPT_STOP);
		SetServiceStatus(service_handle, &service_status);
		SetEvent(quit);
		return NO_ERROR;
	}

	return ERROR_CALL_NOT_IMPLEMENTED;
}

static void deactivate_tunnel(_In_z_ const wchar_t* tunnel_name, _In_ bool wait_for_stop)
{
	sc_handle scm(OpenSCManagerW(NULL, NULL, SC_MANAGER_ALL_ACCESS));
	if (!scm)
		throw win_runtime_error("Failed to open SCM");

	wstring short_name;
	sprintf(short_name, L"eduWGTunnel$%s$%s", client_id, tunnel_name);
	sc_handle service(OpenServiceW(scm, short_name.c_str(), SERVICE_ALL_ACCESS));
	if (service)
	{
		// Stop the tunnel service (and wait).
		SERVICE_STATUS tunnel_service_status;
		ControlService(service, SERVICE_CONTROL_STOP, &tunnel_service_status);
		for (int i = 0; wait_for_stop && i < 180 && QueryServiceStatus(service, &tunnel_service_status) && tunnel_service_status.dwCurrentState != SERVICE_STOPPED; ++i)
			if (WaitForSingleObject(quit, 1000) == WAIT_OBJECT_0)
				break;

		if (!DeleteService(service) && GetLastError() != ERROR_SERVICE_MARKED_FOR_DELETE)
			throw win_runtime_error("Failed to delete tunnel service");
	}

	WCHAR config_file_path[MAX_PATH];
	PathCombineW(config_file_path, config_folder_path, wstring_printf(L"%s.conf.dpapi", tunnel_name).c_str());
	DeleteFileW(config_file_path);
}

static void activate_tunnel(_In_z_ const wchar_t* tunnel_name, _In_count_(config_len) const char* config, _In_ unsigned int config_len, _In_ bool wait_for_start)
{
	sc_handle scm(OpenSCManagerW(NULL, NULL, SC_MANAGER_ALL_ACCESS));
	if (!scm)
		throw win_runtime_error("Failed to open SCM");

	wstring short_name;
	sprintf(short_name, L"eduWGTunnel$%s$%s", client_id, tunnel_name);
	sc_handle service(OpenServiceW(scm, short_name.c_str(), SERVICE_ALL_ACCESS));
	if (!!service)
	{
		// Deactivate existing tunnel with this name.
		service.free();
		deactivate_tunnel(tunnel_name, true);
	}

	WCHAR config_file_path[MAX_PATH];
	{
		// Prepare tunnel config file.
		DATA_BLOB data_in = {
			config_len,
			(BYTE*)config
		}, data_out;
		if (!CryptProtectData(&data_in, tunnel_name, NULL, NULL, NULL, CRYPTPROTECT_UI_FORBIDDEN, &data_out))
			throw win_runtime_error("Failed to encrypt tunnel config");
		unique_ptr<unsigned char, LocalFree_delete<unsigned char>> encrypted_config(data_out.pbData);
		PathCombineW(config_file_path, config_folder_path, wstring_printf(L"%s.conf.dpapi", tunnel_name).c_str());
		winstd::security_attributes sa;
		if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
			SDDL_OWNER SDDL_DELIMINATOR SDDL_LOCAL_SYSTEM
			SDDL_GROUP SDDL_DELIMINATOR SDDL_LOCAL_SYSTEM
			SDDL_DACL SDDL_DELIMINATOR SDDL_PROTECTED SDDL_AUTO_INHERITED
			SDDL_ACE_BEGIN SDDL_ACCESS_ALLOWED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_FILE_ALL SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_LOCAL_SYSTEM SDDL_ACE_END
			SDDL_ACE_BEGIN SDDL_ACCESS_ALLOWED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_STANDARD_DELETE SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_BUILTIN_ADMINISTRATORS SDDL_ACE_END,
			SDDL_REVISION_1, sa, NULL))
			throw win_runtime_error("ConvertStringSecurityDescriptorToSecurityDescriptor failed");
		file config_file(CreateFileW(config_file_path, GENERIC_WRITE | DELETE, FILE_SHARE_READ, &sa, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
		if (!config_file)
			throw win_runtime_error("Failed to create config file");
		DWORD bytes_written;
		if (!WriteFile(config_file, data_out.pbData, data_out.cbData, &bytes_written, NULL))
			throw win_runtime_error("Failed to write config file");
		if (bytes_written != data_out.cbData)
			throw runtime_error("Incomplete write of config file");
	}

	try
	{
		// Create the tunnel service.
		wstring fmt;
		LoadStringW(NULL,
			client_type == client_type_t::eduvpn ? IDS_EDUVPN_TUN_SERVICE_TITLE :
			client_type == client_type_t::letsconnect ? IDS_LETSCONNECT_TUN_SERVICE_TITLE :
			throw invalid_argument("Unknown client"),
			fmt);
		wstring mgr_short_name;
		sprintf(mgr_short_name, L"eduWGManager$%s", client_id);
		const LPCWSTR deps[] = {
			L"Nsi",
			L"TcpIp",
			mgr_short_name.c_str()
		};
		vector<WCHAR> dependencies;
		for (size_t i = 0; i < _countof(deps); ++i)
			dependencies.insert(dependencies.cend(), deps[i], deps[i] + wcslen(deps[i]) + 1);
		dependencies.push_back(L'\0');
		service = CreateServiceW(scm,
			short_name.c_str(),
			wstring_printf(fmt.c_str(), tunnel_name).c_str(),
			SERVICE_ALL_ACCESS,
			SERVICE_WIN32_OWN_PROCESS,
			SERVICE_AUTO_START,
			SERVICE_ERROR_NORMAL,
			wstring_printf(L"\"%.*s\" \"%s\" Tunnel \"%s\"", MAX_PATH, module_file_path, client_id, tunnel_name).c_str(),
			NULL,
			NULL,
			dependencies.data(),
			NULL,
			NULL);
		if (!service)
			throw win_runtime_error("Failed to create tunnel service");

		try {
			// Configure the tunnel service.
			SERVICE_SID_INFO sid_type = { SERVICE_SID_TYPE_UNRESTRICTED };
			if (!ChangeServiceConfig2W(service, SERVICE_CONFIG_SERVICE_SID_INFO, &sid_type))
				throw win_runtime_error("Failed to set tunnel service SID_INFO");
			wstring desc;
			sprintf(desc, L"@%.*s,-%u",
				MAX_PATH, module_file_path,
				client_type == client_type_t::eduvpn ? IDS_EDUVPN_TUN_SERVICE_DESCRIPTION :
				client_type == client_type_t::letsconnect ? IDS_LETSCONNECT_TUN_SERVICE_DESCRIPTION :
				throw invalid_argument("Unknown client"));
			SERVICE_DESCRIPTIONW description = { const_cast<LPWSTR>(desc.c_str()) };
			ChangeServiceConfig2W(service, SERVICE_CONFIG_DESCRIPTION, &description);

			// Start the tunnel service.
			if (!StartServiceW(service, 0, NULL))
				throw win_runtime_error("Failed to start tunnel service");
			SERVICE_STATUS tunnel_service_status;
			for (int i = 0; wait_for_start && i < 180 && QueryServiceStatus(service, &tunnel_service_status) && tunnel_service_status.dwCurrentState == SERVICE_START_PENDING; ++i)
				if (WaitForSingleObject(quit, 1000) == WAIT_OBJECT_0)
					break;
		}
		catch (const exception& e)
		{
			DeleteService(service);
			throw e;
		}
	}
	catch (const exception& e)
	{
		DeleteFileW(config_file_path);
		throw e;
	}
}

enum class message_code
{
	status,
	activate_tunnel,
	deactivate_tunnel,
	get_tunnel_config,
	tunnel_config,
};

struct message {
	message_code code;
};

struct message_status : message {
	bool success;
	DWORD win32_error;
	unsigned int message_len;
	char message[];
};

#define MAX_WG_TUNNEL_NAME 32

struct message_activate_tunnel : message {
	char tunnel_name[MAX_WG_TUNNEL_NAME];
	unsigned int config_len;
	char config[];
};

struct message_config : message {
	unsigned int config_len;
	char config[];
};

#define PIPE_MSG_BUFFER 0x10000

static DWORD WINAPI client_thread(_In_ LPVOID lpThreadParameter)
{
	DWORD ret;
	wstring tunnel_name;
	driver::adapter tunnel_adapter;
	try {
		file pipe(lpThreadParameter);
		event read_complete(CreateEventW(NULL, TRUE, FALSE, NULL));
		OVERLAPPED overlapped = { 0 };
		overlapped.hEvent = read_complete;
		const HANDLE event_handles[] = { read_complete, quit };
		vector<unsigned char, sanitizing_allocator<unsigned char>> msg_in(PIPE_MSG_BUFFER, 0);
		vector<unsigned char, sanitizing_allocator<unsigned char>> tunnel_config(1024, 0);
		vector<unsigned char> msg_out;
		message_status msg_status;
		msg_status.code = message_code::status;

		for (;;)
		{
			DWORD err, bytes_read;

			// Read client request.
			auto msg_in_cursor = msg_in.begin();
		read_one_block:
			if (ReadFile(pipe, &*msg_in_cursor, (DWORD)(msg_in.cend() - msg_in_cursor), &bytes_read, &overlapped))
				msg_in.erase(msg_in_cursor + bytes_read, msg_in.cend());
			else
			{
				err = GetLastError();
				if (err == ERROR_IO_PENDING)
				{
					err = WaitForMultipleObjects(_countof(event_handles), event_handles, FALSE, INFINITE);
					if (err == WAIT_OBJECT_0)
					{
						if (GetOverlappedResult(pipe, &overlapped, &bytes_read, FALSE))
							msg_in.erase(msg_in_cursor + bytes_read, msg_in.cend());
						else
						{
							err = GetLastError();
							if (err == ERROR_MORE_DATA) // There is more data to the message.
							{
								msg_in.insert(msg_in.cend(), PIPE_MSG_BUFFER, 0);
								msg_in_cursor = msg_in.end() - PIPE_MSG_BUFFER;
								goto read_one_block;
							}
							else if (err == ERROR_BROKEN_PIPE) // Client disconnected.
								goto out;
							else
								throw win_runtime_error(err, "Failed to read from pipe");
						}
					}
					else if (err == WAIT_OBJECT_0 + 1) // Service is stopping.
						goto out;
					else
						throw win_runtime_error(err, "WaitForMultipleObjects returned unexpectedly");
				}
				else if (err == ERROR_MORE_DATA) // There is more data to the message.
				{
					msg_in.insert(msg_in.cend(), PIPE_MSG_BUFFER, 0);
					msg_in_cursor = msg_in.end() - PIPE_MSG_BUFFER;
					goto read_one_block;
				}
				else if (err == ERROR_BROKEN_PIPE) // Client disconnected.
					goto out;
				else
					throw win_runtime_error(err, "Failed to read from pipe");
			}

			if (msg_in.size() < sizeof(message))
				throw invalid_argument("Invalid request");
			try
			{
				switch (reinterpret_cast<const message*>(msg_in.data())->code)
				{
				case message_code::activate_tunnel: {
					if (!tunnel_name.empty())
						throw logic_error("Tunnel is already active");
					auto* _msg_in = reinterpret_cast<const message_activate_tunnel*>(msg_in.data());
					if (msg_in.size() < sizeof(message_activate_tunnel) ||
						msg_in.size() < sizeof(message_activate_tunnel) + _msg_in->config_len)
						throw invalid_argument("Invalid request");
					MultiByteToWideChar(CP_UTF8, 0, _msg_in->tunnel_name, MAX_WG_TUNNEL_NAME, tunnel_name);
					activate_tunnel(tunnel_name.c_str(), _msg_in->config, _msg_in->config_len, false);
					break;
				}

				case message_code::deactivate_tunnel: {
					if (tunnel_name.empty())
						throw logic_error("Tunnel is not active");
					tunnel_adapter.free();
					deactivate_tunnel(tunnel_name.c_str(), true);
					tunnel_name.clear();
					break;
				}

				case message_code::get_tunnel_config: {
					if (tunnel_name.empty())
						throw logic_error("Tunnel is not active");
					if (!tunnel_adapter) {
						tunnel_adapter = driver::WireGuardOpenAdapter(tunnel_name.c_str());
						if (!tunnel_adapter)
							throw winstd::win_runtime_error("WireGuardOpenAdapter failed");
					}

					tunnel_adapter.get_configuration(tunnel_config);
					auto* cfg = reinterpret_cast<WIREGUARD_INTERFACE*>(tunnel_config.data());
					if (cfg->Flags & WIREGUARD_INTERFACE_HAS_PRIVATE_KEY)
					{
						SecureZeroMemory(&cfg->PrivateKey, sizeof(cfg->PrivateKey));
						cfg->Flags &= ~WIREGUARD_INTERFACE_HAS_PRIVATE_KEY;
					}

					message_config msg_cfg;
					msg_cfg.code = message_code::tunnel_config;
					msg_cfg.config_len = (DWORD)tunnel_config.size();
					tunnel_config.insert(tunnel_config.cbegin(), reinterpret_cast<unsigned char*>(&msg_cfg), reinterpret_cast<unsigned char*>(&msg_cfg + 1));

					if (!WriteFile(pipe, tunnel_config.data(), (DWORD)tunnel_config.size(), NULL, &overlapped) && (err = GetLastError()) != ERROR_IO_PENDING)
						throw win_runtime_error(err, "Failed to write to pipe");
					err = WaitForMultipleObjects(_countof(event_handles), event_handles, FALSE, INFINITE);
					if (err == WAIT_OBJECT_0 + 1)
						goto out;
					else if (err != WAIT_OBJECT_0)
						throw win_runtime_error(err, "WaitForMultipleObjects returned unexpectedly");
					continue;
				}

				default:
					throw invalid_argument("Unknown message");
				}

				msg_status.success = true;
				msg_status.win32_error = ERROR_SUCCESS;
				msg_status.message_len = 0;
				msg_out.assign(reinterpret_cast<unsigned char*>(&msg_status), reinterpret_cast<unsigned char*>(&msg_status + 1));
			}
			catch (const win_runtime_error& e)
			{
				msg_status.success = false;
				msg_status.win32_error = e.number();
				msg_status.message_len = (unsigned int)strlen(e.what());
				msg_out.assign(reinterpret_cast<unsigned char*>(&msg_status), reinterpret_cast<unsigned char*>(&msg_status + 1));
				msg_out.insert(msg_out.cend(), reinterpret_cast<const unsigned char*>(e.what()), reinterpret_cast<const unsigned char*>(e.what() + msg_status.message_len));
			}
			catch (const exception& e)
			{
				msg_status.success = false;
				msg_status.win32_error = 0;
				msg_status.message_len = (unsigned int)strlen(e.what());
				msg_out.assign(reinterpret_cast<unsigned char*>(&msg_status), reinterpret_cast<unsigned char*>(&msg_status + 1));
				msg_out.insert(msg_out.cend(), reinterpret_cast<const unsigned char*>(e.what()), reinterpret_cast<const unsigned char*>(e.what() + msg_status.message_len));
			}

			// Respond to client.
			if (!WriteFile(pipe, msg_out.data(), (DWORD)msg_out.size(), NULL, &overlapped) && (err = GetLastError()) != ERROR_IO_PENDING)
				throw win_runtime_error(err, "Failed to write to pipe");
			err = WaitForMultipleObjects(_countof(event_handles), event_handles, FALSE, INFINITE);
			if (err == WAIT_OBJECT_0 + 1)
				goto out;
			else if (err != WAIT_OBJECT_0)
				throw win_runtime_error(err, "WaitForMultipleObjects returned unexpectedly");
		}
	out:

		ret = 0;
	}
	catch (const exception& e)
	{
		log(e);
		ret = 1;
	}

	tunnel_adapter.free();
	if (!tunnel_name.empty())
		deactivate_tunnel(tunnel_name.c_str(), false);

	return ret;
}

static VOID WINAPI manager_service(_In_ DWORD dwNumServicesArgs, _In_opt_count_(dwNumServicesArgs) LPWSTR* lpServiceArgVectors)
{
	try
	{
		if (dwNumServicesArgs < 1 || !lpServiceArgVectors)
			throw invalid_argument("Invalid ServiceMain call");
		service_handle = RegisterServiceCtrlHandlerExW(lpServiceArgVectors[0], manager_handler, NULL);
		if (!service_handle)
			throw win_runtime_error("RegisterServiceCtrlHandlerEx failed");
		SetServiceStatus(service_handle, &service_status);
	}
	catch (const exception& e)
	{
		log(e);
		return;
	}

	try
	{
		driver::init();

		winstd::security_attributes sa;
		if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
			SDDL_OWNER SDDL_DELIMINATOR SDDL_LOCAL_SYSTEM
			SDDL_GROUP SDDL_DELIMINATOR SDDL_LOCAL_SYSTEM
			SDDL_DACL SDDL_DELIMINATOR SDDL_PROTECTED SDDL_AUTO_INHERITED
			SDDL_ACE_BEGIN SDDL_ACCESS_ALLOWED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_FILE_ALL SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_LOCAL_SYSTEM SDDL_ACE_END
			SDDL_ACE_BEGIN SDDL_ACCESS_ALLOWED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_FILE_READ SDDL_FILE_WRITE SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_EVERYONE SDDL_ACE_END
			SDDL_ACE_BEGIN SDDL_ACCESS_DENIED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_FILE_ALL SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_ANONYMOUS SDDL_ACE_END,
			SDDL_REVISION_1, sa, NULL))
			throw win_runtime_error("ConvertStringSecurityDescriptorToSecurityDescriptor failed");
		wstring pipe_name;
		sprintf(pipe_name, L"\\\\.\\pipe\\eduWGManager$%s", client_id);
		DWORD open_mode = PIPE_ACCESS_DUPLEX | WRITE_DAC | FILE_FLAG_FIRST_PIPE_INSTANCE | FILE_FLAG_OVERLAPPED;
		event client_connected(CreateEventW(NULL, TRUE, FALSE, NULL));
		const HANDLE event_handles[] = { client_connected, quit };
		for (;;)
		{
			// Create named pipe and schedule client connection accept.
			DWORD err;
			file pipe(CreateNamedPipeW(
				pipe_name.c_str(),
				open_mode,
				PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT | PIPE_REJECT_REMOTE_CLIENTS,
				PIPE_UNLIMITED_INSTANCES,
				PIPE_MSG_BUFFER, PIPE_MSG_BUFFER,
				0,
				&sa));
			if (!pipe)
				throw win_runtime_error("Failed to create pipe");
			OVERLAPPED overlapped = { 0 };
			overlapped.hEvent = client_connected;
			if (!ConnectNamedPipe(pipe, &overlapped))
			{
				err = GetLastError();
				if (err != ERROR_PIPE_CONNECTED && err != ERROR_IO_PENDING)
					throw win_runtime_error(err, "Failed to connect pipe");
			}

			// Report the service is running. Even if we already did so.
			service_status.dwCurrentState = SERVICE_RUNNING;
			service_status.dwControlsAccepted |= SERVICE_ACCEPT_SHUTDOWN | SERVICE_ACCEPT_STOP;
			SetServiceStatus(service_handle, &service_status);

			err = WaitForMultipleObjects(_countof(event_handles), event_handles, FALSE, INFINITE);
			if (err == WAIT_OBJECT_0)
			{
				// Client connected.
				thread t(CreateThread(NULL, 0, client_thread, pipe, 0, NULL));
				if (!!t)
					pipe.detach();
				else
					log(win_runtime_error("CreateThread failed"));
			}
			else if (err == WAIT_OBJECT_0 + 1)
				break;
			else
				throw win_runtime_error(err, "WaitForMultipleObjects returned unexpectedly");

			ResetEvent(client_connected); // ConnectNamedPipe is not documented to reset the overlapped I/O event automatically.
			open_mode &= ~FILE_FLAG_FIRST_PIPE_INSTANCE;
		}
	}
	catch (const win_runtime_error& e)
	{
		log(e);
		service_status.dwWin32ExitCode = e.number();
	}
	catch (const exception& e)
	{
		log(e);
		service_status.dwWin32ExitCode = ERROR_PROCESS_ABORTED;
	}
	service_status.dwCurrentState = SERVICE_STOPPED;
	service_status.dwControlsAccepted = 0;
	SetServiceStatus(service_handle, &service_status);
}

static int manager()
{
	static const SERVICE_TABLE_ENTRYW services[] =
	{
		{ L"", manager_service },
		{ NULL, NULL }
	};
	if (!StartServiceCtrlDispatcherW(services))
		throw win_runtime_error("StartServiceCtrlDispatcher failed");
	return 0;
}

static unique_ptr<ringlogger> wg_log;
static file tunnel_log;

static DWORD WINAPI wg_log_monitor(_In_opt_ LPVOID lpThreadParameter)
{
	UNREFERENCED_PARAMETER(lpThreadParameter);

	auto cursor = ringlogger::cursor_all;
	for (;;)
	{
		wg_log->follow_from_cursor(cursor, tunnel_log);
		switch (WaitForSingleObject(quit, 300))
		{
		case WAIT_TIMEOUT: break;
		case WAIT_OBJECT_0: return 0;
		case WAIT_ABANDONED: return 1;
		default:
			log(win_runtime_error("WaitForSingleObject failed"));
			return 2;
		}
	}
}

static int tunnel(_In_z_ const wchar_t* tunnel_name)
{
	{
		// Open WireGuard ringlog.
		// There is only one global WireGuard log. It is named "log.bin" and resides in the same folder than tunnel configuration.
		WCHAR wg_log_file_path[MAX_PATH];
		PathCombineW(wg_log_file_path, config_folder_path, L"log.bin");
		wg_log.reset(new ringlogger(wg_log_file_path, "Tunnel"));
	}

	{
		// Open/create a text file for user readable log.
		WCHAR tunnel_log_file_path[MAX_PATH];
		PathCombineW(tunnel_log_file_path, config_folder_path, wstring_printf(L"%s.txt", tunnel_name).c_str());
		winstd::security_attributes sa;
		if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
			SDDL_OWNER SDDL_DELIMINATOR SDDL_LOCAL_SYSTEM
			SDDL_GROUP SDDL_DELIMINATOR SDDL_LOCAL_SYSTEM
			SDDL_DACL SDDL_DELIMINATOR SDDL_PROTECTED SDDL_AUTO_INHERITED
			SDDL_ACE_BEGIN SDDL_ACCESS_ALLOWED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_FILE_ALL SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_LOCAL_SYSTEM SDDL_ACE_END
			SDDL_ACE_BEGIN SDDL_ACCESS_ALLOWED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_FILE_ALL SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_BUILTIN_ADMINISTRATORS SDDL_ACE_END
			SDDL_ACE_BEGIN SDDL_ACCESS_ALLOWED SDDL_SEPERATOR SDDL_SEPERATOR SDDL_FILE_READ SDDL_STANDARD_DELETE SDDL_SEPERATOR SDDL_SEPERATOR SDDL_SEPERATOR SDDL_BUILTIN_USERS SDDL_ACE_END,
			SDDL_REVISION_1, sa, NULL))
			throw win_runtime_error("ConvertStringSecurityDescriptorToSecurityDescriptor failed");
		tunnel_log = CreateFileW(tunnel_log_file_path, GENERIC_WRITE, FILE_SHARE_DELETE | FILE_SHARE_READ, &sa, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		if (!tunnel_log)
			throw win_runtime_error("Creating log file failed");
		SetEndOfFile(tunnel_log);
		DWORD written;
		static const char utf8_bom[] = { '\xef', '\xbb', '\xbf' };
		if (!WriteFile(tunnel_log, utf8_bom, sizeof(utf8_bom), &written, NULL))
			throw win_runtime_error("Failed to write to log file");
	}

	// Spawn WireGuard ringlog monitor thread.
	thread wg_log_monitor_thread(CreateThread(NULL, 0, wg_log_monitor, NULL, 0, NULL));
	if (!wg_log_monitor_thread)
		log(win_runtime_error("CreateThread failed. The tunnel log will remain empty."));

	version_t ver;
	module_version(ver);
	wg_log->write(string_printf("eduWGSvcHost v%u.%u.%u.%u, Copyright \xc2\xa9 2022 The Commons Conservancy", ver[0], ver[1], ver[2], ver[3]).c_str());

	// Start the tunnel.
	library tunnel_lib(LoadLibraryW(L"tunnel.dll"));
	if (!tunnel_lib)
		throw win_runtime_error("Failed to load tunnel.dll");
	BOOL(_cdecl * WireGuardTunnelService)(_In_ const wchar_t* conf_file);
	*(FARPROC*)&WireGuardTunnelService = GetProcAddress(tunnel_lib, "WireGuardTunnelService");
	if (!WireGuardTunnelService)
		throw win_runtime_error("Failed to load tunnel.dll entries");
	WCHAR config_file_path[MAX_PATH];
	PathCombineW(config_file_path, config_folder_path, wstring_printf(L"%s.conf.dpapi", tunnel_name).c_str());
	int ret = WireGuardTunnelService(config_file_path) ? 0 : 1;
	SetEvent(quit);
	return ret;
}

_Use_decl_annotations_
int APIENTRY wWinMain(HINSTANCE hInst, HINSTANCE hInstPrev, PWSTR cmdline, int cmdshow)
{
	UNREFERENCED_PARAMETER(hInstPrev);
	UNREFERENCED_PARAMETER(cmdline);
	UNREFERENCED_PARAMETER(cmdshow);

	try
	{
		hInstance = hInst;

		// Parse command line.
		int wargc;
		unique_ptr<LPWSTR[], LocalFree_delete<LPWSTR[]>> wargv(CommandLineToArgvW(GetCommandLineW(), &wargc));
		if (wargv == NULL)
			throw win_runtime_error("CommandLineToArgvW failed");
		if (wargc < 3)
			throw invalid_argument("Usage: eduWGSvcHost.exe <client> <Manager|Tunnel> ...");
		if (_wcsicmp(wargv[1], L"eduVPN") == 0)
		{
			client_id = L"eduVPN";
			client_type = client_type_t::eduvpn;
		}
		else if (_wcsicmp(wargv[1], L"LetsConnect") == 0)
		{
			client_id = L"LetsConnect";
			client_type = client_type_t::letsconnect;
		}
		else
			throw invalid_argument("Unknown client");

		// Prepare for logging to Event Log.
		service_log = RegisterEventSourceW(NULL, wstring_printf(L"eduWGSvcHost$%s", client_id).c_str());

		// Get config folder path.
		switch (GetModuleFileNameW(hInstance, module_file_path, _countof(module_file_path))) {
		case 0:
		case _countof(module_file_path):
			throw win_runtime_error("GetModuleFileName failed");
		}
		memcpy_s(config_folder_path, sizeof(config_folder_path), module_file_path, sizeof(module_file_path));
		PathRemoveFileSpecW(config_folder_path);
		PathCombineW(config_folder_path, config_folder_path, L"config");

		quit = CreateEventW(NULL, TRUE, FALSE, NULL);
		if (!quit)
			throw win_runtime_error("CreateEvent failed");

		if (_wcsicmp(wargv[2], L"Manager") == 0)
			return manager();
		else if (_wcsicmp(wargv[2], L"Tunnel") == 0)
		{
			if (wargc < 4)
				throw invalid_argument("Usage: eduWGSvcHost.exe <client> Tunnel <tunnel name>");
			return tunnel(wargv[3]);
		}
		else
			throw invalid_argument("Unknown service");
	}
	catch (const exception& e)
	{
		log(e);
		return 1;
	}
}
