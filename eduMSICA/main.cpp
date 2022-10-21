/*
	eduMSICA - MSI Custom Actions

	Copyright: 2021-2022 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

#define _WINSOCKAPI_ // Prevent inclusion of winsock.h in windows.h.
#include <Windows.h>
#include "undocumented.h"
#include <AccCtrl.h>
#include <AclAPI.h>
#include <Msi.h>
#include <MsiQuery.h>
#include <Shlwapi.h>
#include <stdarg.h>
#include <TlHelp32.h>
#include <wchar.h>
#include <WinStd/COM.h>
#include <WinStd/MSI.h>
#include <WinStd/Shell.h>
#include <WinStd/Win.h>
#include <winternl.h>
#include <wintun.h>

#include <fstream>
#include <vector>

#pragma comment(lib, "ntdll.lib")
#pragma comment(lib, "shlwapi.lib")

using namespace std;
using namespace winstd;

#define WINTUN_COMPONENT  TEXT("wintun.dll")
#define WINTUN_FILE_NAME  TEXT("wintun.dll")
#define WINTUN_DIRECTORY  TEXT("OPENVPNDIR")
#define ERROR_EDUMSICA_ERRNO 2550L

static MSIHANDLE s_hInstall; // Handle to the installation session
static wofstream s_log;      // Log file

static int MsiProcessMessage(INSTALLMESSAGE eMessageType, MSIHANDLE hRecord)
{
	if (s_hInstall)
		return MsiProcessMessage(s_hInstall, eMessageType, hRecord);

	if (!s_log.bad()) {
		switch (eMessageType) {
		case INSTALLMESSAGE_ERROR: s_log << L"[E] "; break;
		case INSTALLMESSAGE_WARNING: s_log << L"[W] "; break;
		case INSTALLMESSAGE_INFO: s_log << L"[I] "; break;
		default: return IDOK;
		}

		wstring sLine;
		MsiFormatRecordW(NULL, hRecord, sLine);
		s_log << sLine << endl;
		return IDOK;
	}

	return IDOK;
}

static void
LogErrorNumV(_In_ DWORD dwError, _In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, _In_ va_list args)
{
	LPTSTR szSystemMessage = NULL;
	FormatMessage(
		FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_MAX_WIDTH_MASK,
		NULL,
		HRESULT_FROM_WIN32(dwError),
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&szSystemMessage,
		0,
		NULL);
	PMSIHANDLE hRecord = MsiCreateRecord(5);
	if (!(MSIHANDLE)hRecord)
		return;
	MsiRecordSetInteger(hRecord, 1, ERROR_EDUMSICA_ERRNO);
	MsiRecordSetString(hRecord, 2, szFunction);
	tstring str;
	vsprintf(str, szFormat, args);
	MsiRecordSetString(hRecord, 3, str.c_str());
	MsiRecordSetInteger(hRecord, 4, dwError);
	MsiRecordSetString(hRecord, 5, szSystemMessage ? szSystemMessage : TEXT(""));
	MsiProcessMessage(INSTALLMESSAGE_ERROR, hRecord);
	LocalFree(szSystemMessage);
}

static void
LogErrorNum(_In_ DWORD dwError, _In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, ...)
{
	va_list args;
	va_start(args, szFormat);
	LogErrorNumV(dwError, szFunction, szFormat, args);
	va_end(args);
}

#define LOG_ERROR_NUM(dwError, szFormat, ...) LogErrorNum(dwError, TEXT(__FUNCTION__), szFormat, __VA_ARGS__)

static void
LogErrorV(_In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, _In_ va_list args)
{
	PMSIHANDLE hRecord = MsiCreateRecord(3);
	if (!(MSIHANDLE)hRecord)
		return;
	MsiRecordSetInteger(hRecord, 1, ERROR_EDUMSICA_ERRNO);
	MsiRecordSetString(hRecord, 2, szFunction);
	tstring str;
	vsprintf(str, szFormat, args);
	MsiRecordSetString(hRecord, 3, str.c_str());
	MsiProcessMessage(INSTALLMESSAGE_ERROR, hRecord);
}

static void
LogError(_In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, ...)
{
	va_list args;
	va_start(args, szFormat);
	LogErrorV(szFunction, szFormat, args);
	va_end(args);
}

#define LOG_ERROR(szFormat, ...) LogError(TEXT(__FUNCTION__), szFormat, __VA_ARGS__)

static void CALLBACK
WintunLogger(_In_ WINTUN_LOGGER_LEVEL Level, _In_ DWORD64 Timestamp, _In_z_ LPCWSTR Message)
{
	UNREFERENCED_PARAMETER(Timestamp);
	PMSIHANDLE hRecord = MsiCreateRecord(2);
	if (!(MSIHANDLE)hRecord)
		return;
	LPCTSTR szTemplate;
	INSTALLMESSAGE eType;
	switch (Level)
	{
	case WINTUN_LOG_INFO:
		szTemplate = TEXT("Wintun: [1]");
		eType = INSTALLMESSAGE_INFO;
		break;
	case WINTUN_LOG_WARN:
		szTemplate = TEXT("Wintun warning: [1]");
		eType = INSTALLMESSAGE_INFO;
		break;
	case WINTUN_LOG_ERR:
		szTemplate = TEXT("Wintun error: [1]");
		eType = INSTALLMESSAGE_ERROR;
		break;
	default:
		return;
	}
	MsiRecordSetString(hRecord, 0, szTemplate);
	MsiRecordSetStringW(hRecord, 1, Message);
	MsiProcessMessage(eType, hRecord);
}

static UINT
SetFormattedProperty(_In_ MSIHANDLE hInstall, _In_z_ LPCTSTR szPropertyName, _In_z_ LPCTSTR szFormat)
{
	PMSIHANDLE hRecord = MsiCreateRecord(1);
	if (!(MSIHANDLE)hRecord) {
		LOG_ERROR(TEXT("MsiCreateRecord failed"));
		return ERROR_OUTOFMEMORY;
	}
	UINT uiResult = MsiRecordSetString(hRecord, 0, szFormat);
	if (uiResult != ERROR_SUCCESS) {
		LOG_ERROR_NUM(uiResult, TEXT("MsiRecordSetString failed"));
		return uiResult;
	}
	tstring sValue;
	uiResult = MsiFormatRecord(hInstall, hRecord, sValue);
	if (uiResult != ERROR_SUCCESS) {
		LOG_ERROR_NUM(uiResult, TEXT("MsiFormatRecord failed"));
		return uiResult;
	}
	uiResult = MsiSetProperty(hInstall, szPropertyName, sValue.c_str());
	if (uiResult != ERROR_SUCCESS) {
		LOG_ERROR_NUM(uiResult, TEXT("MsiSetProperty(\"%s\") failed"), szPropertyName);
		return uiResult;
	}
	return ERROR_SUCCESS;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
EvaluateComponents(_In_ MSIHANDLE hInstall)
{
	com_initializer com_init(NULL);
	s_hInstall = hInstall;

	// Get the wintun.dll component state.
	INSTALLSTATE iInstalled, iAction;
	UINT uiResult = MsiGetComponentState(hInstall, WINTUN_COMPONENT, &iInstalled, &iAction);
	if (uiResult == ERROR_SUCCESS) {
		if (iInstalled >= INSTALLSTATE_LOCAL && iAction > INSTALLSTATE_BROKEN && iAction < INSTALLSTATE_LOCAL)
		{
			// Wintun is installed, but should be degraded to advertised/removed.
			// Schedule Wintun driver deletition.
			SetFormattedProperty(hInstall, TEXT("RemoveWintunDriver"), TEXT("[") WINTUN_DIRECTORY TEXT("]") WINTUN_FILE_NAME);
		}
	}
	else
		LOG_ERROR_NUM(uiResult, TEXT("MsiGetComponentState(\"%s\") failed"), WINTUN_COMPONENT);

	static const LPCTSTR szClientFilenames[] = {
		TEXT("eduVPN.Client.exe"),
		TEXT("LetsConnect.Client.exe"),
	};
	for (size_t i = 0; i < _countof(szClientFilenames); ++i) {
		// Get the client component state.
		uiResult = MsiGetComponentState(hInstall, szClientFilenames[i], &iInstalled, &iAction);
		if (uiResult == ERROR_SUCCESS) {
			if (iInstalled >= INSTALLSTATE_LOCAL && iAction >= INSTALLSTATE_REMOVED) {
				// Client component is installed and shall be removed/upgraded/reinstalled.
				// Schedule client termination.
				SetFormattedProperty(hInstall, TEXT("KillExecutableProcesses"), tstring_printf(TEXT("[COREDIR]%s"), szClientFilenames[i]).c_str());
			}
		}
		else if (uiResult != ERROR_UNKNOWN_COMPONENT)
			LOG_ERROR_NUM(uiResult, TEXT("MsiGetComponentState(\"%s\") failed"), szClientFilenames[i]);
	}

	return ERROR_SUCCESS;
}

static void
SetTokenPrivilege(_In_ HANDLE hToken, _In_z_ LPCTSTR szPrivilege, _In_ BOOL bEnablePrivilege)
{
	LUID luid;
	if (!LookupPrivilegeValue(NULL, szPrivilege, &luid))
		throw win_runtime_error("LookupPrivilegeValue failed");
	TOKEN_PRIVILEGES tp = { 1, {{ luid, bEnablePrivilege ? (DWORD)SE_PRIVILEGE_ENABLED : 0 }}};
	if (!AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof(TOKEN_PRIVILEGES), NULL, NULL))
		throw win_runtime_error("AdjustTokenPrivileges failed");
	if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)
		throw runtime_error("The token does not have the specified privilege");
}

static void
ResetFileACL(_In_z_ LPCTSTR szPath, _In_ PACL pACL, _In_ PSID pOwner)
{
	try {
		// Try to modify the object's DACL.
		DWORD dwResult = SetNamedSecurityInfo((LPTSTR)szPath, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, pACL, NULL);
		if (dwResult == ERROR_SUCCESS) {
			PMSIHANDLE hRecord = MsiCreateRecord(2);
			if ((MSIHANDLE)hRecord) {
				MsiRecordSetString(hRecord, 0, TEXT("Reset \"[1]\" ACL"));
				MsiRecordSetString(hRecord, 1, szPath);
				MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
			}
			return;
		}
		if (dwResult != ERROR_ACCESS_DENIED)
			throw win_runtime_error(dwResult, "SetNamedSecurityInfo(DACL_SECURITY_INFORMATION) failed");

		// If the preceding call failed because access was denied, 
		// enable the SE_TAKE_OWNERSHIP_NAME privilege, create a SID for 
		// the Administrators group, take ownership of the object, and 
		// disable the privilege. Then try again to set the object's DACL.

		// Open a handle to the access token for the calling process.
		HANDLE hToken;
		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES, &hToken))
			throw win_runtime_error("OpenProcessToken(GetCurrentProcess()) failed");
		win_handle<INVALID_HANDLE_VALUE> process_token(hToken);

		// Enable the SE_TAKE_OWNERSHIP_NAME privilege.
		SetTokenPrivilege(process_token, SE_TAKE_OWNERSHIP_NAME, TRUE);

		// Set the owner in the object's security descriptor.
		dwResult = SetNamedSecurityInfo((LPTSTR)szPath, SE_FILE_OBJECT, OWNER_SECURITY_INFORMATION, pOwner, NULL, NULL, NULL);
		if (dwResult != ERROR_SUCCESS)
			throw win_runtime_error(dwResult, "SetNamedSecurityInfo(OWNER_SECURITY_INFORMATION) failed");

		// Disable the SE_TAKE_OWNERSHIP_NAME privilege.
		SetTokenPrivilege(process_token, SE_TAKE_OWNERSHIP_NAME, FALSE);

		// Try again to modify the object's DACL, now that we are the owner.
		dwResult = SetNamedSecurityInfo((LPTSTR)szPath, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, pACL, NULL);
		if (dwResult != ERROR_SUCCESS)
			throw win_runtime_error(dwResult, "SetNamedSecurityInfo(DACL_SECURITY_INFORMATION) failed");

		PMSIHANDLE hRecord = MsiCreateRecord(2);
		if ((MSIHANDLE)hRecord) {
			MsiRecordSetString(hRecord, 0, TEXT("Reset \"[1]\" ACL"));
			MsiRecordSetString(hRecord, 1, szPath);
			MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
		}
	}
	catch (win_runtime_error& e) {
		PMSIHANDLE hRecord = MsiCreateRecord(5);
		if ((MSIHANDLE)hRecord) {
			MsiRecordSetString(hRecord, 0, TEXT("Failed to reset \"[1]\" ACL: [2]: ([3]): [4]"));
			MsiRecordSetString(hRecord, 1, szPath);
			MsiRecordSetStringA(hRecord, 2, e.what());
			MsiRecordSetInteger(hRecord, 3, e.number());
			MsiRecordSetString(hRecord, 4, e.msg().c_str());
			MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
		}
	}
	catch (exception& e) {
		PMSIHANDLE hRecord = MsiCreateRecord(3);
		if ((MSIHANDLE)hRecord) {
			MsiRecordSetString(hRecord, 0, TEXT("Failed to reset \"[1]\" ACL: [2]"));
			MsiRecordSetString(hRecord, 1, szPath);
			MsiRecordSetStringA(hRecord, 2, e.what());
			MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
		}
	}
}

static void
ResetACLAndPurgeFolder(_In_z_ LPCTSTR szPath)
{
	tstring szPath2(szPath);
	PathRemoveBackslash(szPath2);

	vector<EXPLICIT_ACCESS> eas;
	eas.reserve(4);

	// Full control for SYSTEM user.
	SID_IDENTIFIER_AUTHORITY SIDAuthNT = SECURITY_NT_AUTHORITY;
	security_id pSIDSystem;
	if (AllocateAndInitializeSid(&SIDAuthNT, 1, SECURITY_LOCAL_SYSTEM_RID, 0, 0, 0, 0, 0, 0, 0, pSIDSystem))
		eas.push_back(EXPLICIT_ACCESS{ GENERIC_ALL, SET_ACCESS, SUB_CONTAINERS_AND_OBJECTS_INHERIT, { NULL, NO_MULTIPLE_TRUSTEE, TRUSTEE_IS_SID, TRUSTEE_IS_WELL_KNOWN_GROUP, (LPTSTR)(PSID)pSIDSystem } });

	// Full control for BUILTIN\Administrators group.
	security_id pSIDAdmins;
	if (AllocateAndInitializeSid(&SIDAuthNT, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, pSIDAdmins))
		eas.push_back(EXPLICIT_ACCESS{ GENERIC_ALL, SET_ACCESS, SUB_CONTAINERS_AND_OBJECTS_INHERIT, { NULL, NO_MULTIPLE_TRUSTEE, TRUSTEE_IS_SID, TRUSTEE_IS_GROUP, (LPTSTR)(PSID)pSIDAdmins } });

	// List&create for BUILTIN\Users group.
	security_id pSIDUsers;
	if (AllocateAndInitializeSid(&SIDAuthNT, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_USERS, 0, 0, 0, 0, 0, 0, pSIDUsers))
		eas.push_back(EXPLICIT_ACCESS{ FILE_LIST_DIRECTORY | FILE_ADD_FILE | SYNCHRONIZE, SET_ACCESS, NO_INHERITANCE, { NULL, NO_MULTIPLE_TRUSTEE, TRUSTEE_IS_SID, TRUSTEE_IS_WELL_KNOWN_GROUP, (LPTSTR)(PSID)pSIDUsers } });

	// Full control for CREATOR OWNER user.
	SID_IDENTIFIER_AUTHORITY SIDAuthCreator = SECURITY_CREATOR_SID_AUTHORITY;
	security_id pSIDCreatorOwner;
	if (AllocateAndInitializeSid(&SIDAuthCreator, 1, SECURITY_CREATOR_OWNER_RID, 0, 0, 0, 0, 0, 0, 0, pSIDCreatorOwner))
		eas.push_back(EXPLICIT_ACCESS{ GENERIC_ALL, SET_ACCESS, SUB_OBJECTS_ONLY_INHERIT | INHERIT_ONLY, { NULL, NO_MULTIPLE_TRUSTEE, TRUSTEE_IS_SID, TRUSTEE_IS_WELL_KNOWN_GROUP, (LPTSTR)(PSID)pSIDCreatorOwner } });

	unique_ptr<ACL, LocalFree_delete<ACL>> acl;
	SetEntriesInAcl((ULONG)eas.size(), eas.data(), NULL, acl);
	if (acl)
		ResetFileACL(szPath2.c_str(), acl.get(), pSIDAdmins);

	PMSIHANDLE hRecordSuccess = MsiCreateRecord(2);
	if ((MSIHANDLE)hRecordSuccess)
		MsiRecordSetString(hRecordSuccess, 0, TEXT("Deleted \"[1]\""));
	PMSIHANDLE hRecordFailure = MsiCreateRecord(3);
	if ((MSIHANDLE)hRecordFailure)
		MsiRecordSetString(hRecordFailure, 0, TEXT("Failed to delete \"[1]\": [2]"));
	szPath2 += TEXT('\\');
	size_t offset = szPath2.length();
	szPath2 += TEXT("*.*");
	WIN32_FIND_DATA data;
	find_file hHandle(FindFirstFile(szPath2.c_str(), &data));
	if (!hHandle)
		return;
	do {
		if (data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
			continue;
		szPath2.replace(offset, (size_t)-1, data.cFileName);
		if (DeleteFile(szPath2.c_str())) {
			if ((MSIHANDLE)hRecordSuccess) {
				MsiRecordSetString(hRecordSuccess, 1, szPath2.c_str());
				MsiProcessMessage(INSTALLMESSAGE_INFO, hRecordSuccess);
			}
		}
		else {
			DWORD dwError = GetLastError();
			if ((MSIHANDLE)hRecordFailure) {
				MsiRecordSetString(hRecordFailure, 1, szPath2.c_str());
				MsiRecordSetInteger(hRecordFailure, 2, dwError);
				MsiProcessMessage(INSTALLMESSAGE_INFO, hRecordFailure);
			}
		}
	} while (FindNextFile(hHandle, &data));
}

void __stdcall
ResetACLAndPurgeFolder(HWND hwnd, HINSTANCE hinst, LPCSTR lpszCmdLine, int nCmdShow)
{
	UNREFERENCED_PARAMETER(hwnd);
	UNREFERENCED_PARAMETER(hinst);
	UNREFERENCED_PARAMETER(nCmdShow);

	int argc;
	unique_ptr<LPWSTR[], LocalFree_delete<LPWSTR[]> > argv(CommandLineToArgvW(wstring_printf(_L(__FUNCTION__) L" %hs", lpszCmdLine).c_str(), &argc));
	if (argc < 3)
		return;

	s_log.open(argv[2], ios_base::out | ios_base::ate | ios_base::trunc);
	com_initializer com_init(NULL);

	system_impersonator system_impersonator;
	if (!system_impersonator) {
		DWORD dwError = GetLastError();
		PMSIHANDLE hRecordFailure = MsiCreateRecord(2);
		if ((MSIHANDLE)hRecordFailure) {
			MsiRecordSetString(hRecordFailure, 0, TEXT("Failed to impersonate SYSTEM. Error: [1]"));
			MsiRecordSetInteger(hRecordFailure, 1, dwError);
			MsiProcessMessage(INSTALLMESSAGE_INFO, hRecordFailure);
		}
	}

	ResetACLAndPurgeFolder(argv[1]);
}

static BOOL
IsMemberOfAdministrators(_In_ HANDLE hToken)
{
	win_handle<NULL> token;
	if (!DuplicateTokenEx(hToken, TOKEN_QUERY | TOKEN_IMPERSONATE, NULL, SecurityIdentification, TokenImpersonation, &token))
		throw win_runtime_error("DuplicateTokenEx failed");
	unique_ptr<SID> sid;
	if (!CreateWellKnownSid(WinBuiltinAdministratorsSid, NULL, sid))
		throw win_runtime_error("CreateWellKnownSid failed");
	BOOL bIsMember = FALSE;
	if (!CheckTokenMembership(token, sid.get(), &bIsMember))
		throw win_runtime_error("CheckTokenMembership failed");
	return bIsMember;
}

static WCHAR sExplorerPath[MAX_PATH];

static void
ShellExecuteElevated(_In_ LPCWSTR lpFile, _In_opt_ LPCWSTR lpParameters, _In_opt_ LPCWSTR lpDirectory, _In_ INT nShowCmd)
{
	// Pretend we are an explorer.exe process.
	class data_table_entry_mockup {
	protected:
		LDR_DATA_TABLE_ENTRY* m_data_table_entry;
		UNICODE_STRING m_original_path;

		static LDR_DATA_TABLE_ENTRY* FindCurrentDataTableEntry()
		{
			library ntdll(LoadLibrary(TEXT("ntdll.dll")));
			if (!ntdll)
				throw win_runtime_error("LoadLibrary(ntdll.dll) failed");
			_Ret_maybenull_ PEB* (WINAPI * RtlGetCurrentPeb)(VOID);
			*(FARPROC*)&RtlGetCurrentPeb = GetProcAddress(ntdll, "RtlGetCurrentPeb");
			if (!RtlGetCurrentPeb)
				throw win_runtime_error("GetProcAddress(ntdll.dll, RtlGetCurrentPeb) failed");

			PEB* peb = RtlGetCurrentPeb();
			if (peb == NULL || peb->Ldr == NULL)
				throw win_runtime_error("RtlGetCurrentPeb failed");

			for (LIST_ENTRY* cur = peb->Ldr->InMemoryOrderModuleList.Flink; cur != &peb->Ldr->InMemoryOrderModuleList; cur = cur->Flink) {
				auto entry = CONTAINING_RECORD(cur, LDR_DATA_TABLE_ENTRY, InMemoryOrderLinks);
				if (entry->DllBase == peb->Reserved3[1])
					return entry;
			}
			return NULL;
		}

	public:
		data_table_entry_mockup() {
			m_data_table_entry = FindCurrentDataTableEntry();
			if (!m_data_table_entry)
				throw runtime_error("Data table entry not found");
			m_original_path = m_data_table_entry->FullDllName;
			RtlInitUnicodeString(&m_data_table_entry->FullDllName, sExplorerPath);
		}

		~data_table_entry_mockup() {
			if (m_data_table_entry)
				m_data_table_entry->FullDllName = m_original_path;
		}
	} data_table_entry_mockup;

	// Ask CMLuaUtil to execute our command elevated.
	BIND_OPTS3 bind_opts;
	ZeroMemory(&bind_opts, sizeof(bind_opts));
	bind_opts.cbStruct = sizeof(bind_opts);
	bind_opts.dwClassContext = CLSCTX_LOCAL_SERVER;
	com_obj<ICMLuaUtil> cm_lua_util;
	HRESULT hr = CoGetObject(L"Elevation:Administrator!new:{3E5FC7F9-9A51-4367-9063-A120244FBEC7}", &bind_opts, IID_ICMLuaUtil, cm_lua_util);
	if (FAILED(hr))
		throw com_runtime_error(hr, "CoGetObject(Elevation:Administrator!new:{3E5FC7F9-9A51-4367-9063-A120244FBEC7})");
	hr = cm_lua_util->ShellExec(lpFile, lpParameters, lpDirectory, SEE_MASK_DEFAULT, nShowCmd);
	if (FAILED(hr))
		throw com_runtime_error(hr, "ICMLuaUtil::ShellExec failed");
}

static TCHAR sDllPath[MAX_PATH];

_Return_type_success_(return == ERROR_SUCCESS) static UINT
PurgeFolder(_In_z_ LPCTSTR szPath)
{
	WCHAR sSystem32Path[MAX_PATH], sRunDll32Path[MAX_PATH];
	GetSystemWindowsDirectoryW(sSystem32Path, _countof(sSystem32Path));
	PathCombineW(sSystem32Path, sSystem32Path, L"system32");
	PathCombineW(sRunDll32Path, sSystem32Path, L"rundll32.exe");
	try {
		// Check if proces is elevated already.
		win_handle<NULL> process_token;
		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY | TOKEN_DUPLICATE, process_token))
			throw win_runtime_error("OpenProcessToken(GetCurrentProcess()) failed");
		TOKEN_ELEVATION elevation_data;
		DWORD dwLength;
		if (!GetTokenInformation(process_token, TokenElevation, &elevation_data, sizeof(elevation_data), &dwLength))
			throw win_runtime_error("GetTokenInformation(TokenElevation) of process token failed");
		if (elevation_data.TokenIsElevated) {
			ResetACLAndPurgeFolder(szPath);
			return ERROR_SUCCESS;
		}

		{
			// Check if process is elevateable at least.
			TOKEN_LINKED_TOKEN linked_token_data;
			if (!GetTokenInformation(process_token, TokenLinkedToken, &linked_token_data, sizeof(linked_token_data), &dwLength)) {
				DWORD dwResult = GetLastError();
				throw dwResult == ERROR_NO_SUCH_LOGON_SESSION ?
					win_runtime_error(ERROR_ACCESS_DENIED, "Not elevateable - process token has no linked token") :
					win_runtime_error(dwResult, "GetTokenInformation(TokenLinkedToken) of process token failed");
			}
			win_handle<NULL> linked_token(linked_token_data.LinkedToken);
			if (!GetTokenInformation(linked_token, TokenElevation, &elevation_data, sizeof(elevation_data), &dwLength))
				throw win_runtime_error("GetTokenInformation(TokenElevation) of linked token failed");
			if (!elevation_data.TokenIsElevated || !IsMemberOfAdministrators(linked_token))
				throw win_runtime_error(ERROR_ACCESS_DENIED, "Not elevateable - user is not a member of Administrators group");
		}

		{
			// Is UAC policy set to prompt for consent?
			reg_key key;
			LSTATUS lResult = RegOpenKeyEx(HKEY_LOCAL_MACHINE, TEXT("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System"), 0, KEY_QUERY_VALUE, key);
			if (lResult != ERROR_SUCCESS)
				throw win_runtime_error("Failed to open HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
			DWORD dwPromptBehavior, dwRegType;
			dwLength = sizeof(dwPromptBehavior);
			lResult = RegQueryValueEx(key, TEXT("ConsentPromptBehaviorAdmin"), NULL, &dwRegType, (LPBYTE)&dwPromptBehavior, &dwLength);
			if (lResult != ERROR_SUCCESS)
				throw win_runtime_error("Failed to query HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\ConsentPromptBehaviorAdmin");
			if (dwRegType != REG_DWORD || dwLength != sizeof(dwPromptBehavior))
				throw win_runtime_error(ERROR_UNSUPPORTED_TYPE, "Not a REG_DWORD: HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\ConsentPromptBehaviorAdmin");
			if (dwPromptBehavior == 0) {
				ResetACLAndPurgeFolder(szPath);
				return ERROR_SUCCESS;
			}
			if (dwPromptBehavior != 5)
				throw win_runtime_error(ERROR_ACCESS_DENIED, "UAC policy is set to prompt for consent");
		}

		{
			// Is CMLuaUtil auto-approved?
			reg_key key;
			LSTATUS lResult = RegOpenKeyEx(HKEY_LOCAL_MACHINE, TEXT("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\UAC\\COMAutoApprovalList"), 0, KEY_QUERY_VALUE, key);
			if (lResult == ERROR_SUCCESS) {
				DWORD dwAutoApproved, dwRegType;
				dwLength = sizeof(dwAutoApproved);
				lResult = RegQueryValueEx(key, TEXT("{3E5FC7F9-9A51-4367-9063-A120244FBEC7}"), NULL, &dwRegType, (LPBYTE)&dwAutoApproved, &dwLength);
				if (lResult != ERROR_SUCCESS)
					throw win_runtime_error("Failed to query HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\UAC\\COMAutoApprovalList\\{3E5FC7F9-9A51-4367-9063-A120244FBEC7}");
				if (dwRegType != REG_DWORD || dwLength != sizeof(dwAutoApproved))
					throw win_runtime_error(ERROR_UNSUPPORTED_TYPE, "Not a REG_DWORD: HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\UAC\\COMAutoApprovalList\\{3E5FC7F9-9A51-4367-9063-A120244FBEC7}");
				if (dwAutoApproved == 0)
					throw win_runtime_error(ERROR_ACCESS_DENIED, "CMLuaUtil auto-approval disabled");
			}
		}

		TCHAR sTempFilename[MAX_PATH];
		GetTempPath(_countof(sTempFilename), sTempFilename);
		GetTempFileName(sTempFilename, TEXT("eduVPN"), 0, sTempFilename);
		sTempFilename[_countof(sTempFilename) - 1] = 0;
		try {
			// Spawn elevated ResetACLAndPurgeFolder process.
			ShellExecuteElevated(sRunDll32Path, tstring_printf(TEXT("\"%s\",ResetACLAndPurgeFolder \"%s\" \"%s\""), sDllPath, szPath, sTempFilename).c_str(), sSystem32Path, SW_HIDE);

			{
				// Unfortunately, I am not aware of a way to make ICMLuaUtil::ShellExec() sync with the child process.
				// Hence, wait for the child process result: config folder gets clean.
				PMSIHANDLE hRecord = MsiCreateRecord(5);
				MsiRecordSetInteger(hRecord, 1, 1);
				MsiRecordSetInteger(hRecord, 2, 0);
				MsiRecordSetInteger(hRecord, 3, 0);
				MsiRecordSetInteger(hRecord, 4, 0);
				tstring szPath2(szPath);
				szPath2 += TEXT("\\*.*");
				WIN32_FIND_DATA data;
				for (;;) {
					if (MsiProcessMessage(INSTALLMESSAGE_PROGRESS, hRecord) == IDCANCEL)
						return ERROR_INSTALL_USEREXIT;
					find_file hHandle(FindFirstFile(szPath2.c_str(), &data));
					if (!hHandle)
						break;
					BOOL bFilesFound = FALSE;
					do {
						if (data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
							continue;
						bFilesFound = TRUE;
						break;
					} while (FindNextFile(hHandle, &data));
					if (!bFilesFound)
						break;
					Sleep(200);
				}
			}

			{
				// Copy the log from child process to MSI log.
				wifstream log(sTempFilename);
				PMSIHANDLE hRecord = MsiCreateRecord(2);
				if ((MSIHANDLE)hRecord) {
					MsiRecordSetString(hRecord, 0, TEXT("[1]"));
					for (wstring line; getline(log, line); ) {
						MsiRecordSetStringW(hRecord, 1, line.c_str());
						MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
					}
				}
			}
			DeleteFile(sTempFilename);
		}
		catch (exception &e) {
			DeleteFile(sTempFilename);
			throw e;
		}
		return ERROR_SUCCESS;
	}
	catch (win_runtime_error& e) {
		PMSIHANDLE hRecord = MsiCreateRecord(4);
		if ((MSIHANDLE)hRecord) {
			MsiRecordSetString(hRecord, 0, TEXT("Failed to spawn RunDll32 ResetACLAndPurgeFolder: [1]: ([2]): [3]"));
			MsiRecordSetStringA(hRecord, 1, e.what());
			MsiRecordSetInteger(hRecord, 2, e.number());
			MsiRecordSetString(hRecord, 3, e.msg().c_str());
			MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
		}
	}
	catch (com_runtime_error& e) {
		PMSIHANDLE hRecord = MsiCreateRecord(3);
		if ((MSIHANDLE)hRecord) {
			MsiRecordSetString(hRecord, 0, TEXT("Failed to spawn RunDll32 ResetACLAndPurgeFolder: [1]: ([2])"));
			MsiRecordSetStringA(hRecord, 1, e.what());
			MsiRecordSetInteger(hRecord, 2, e.number());
			MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
		}
	}
	catch (exception& e) {
		PMSIHANDLE hRecord = MsiCreateRecord(2);
		if ((MSIHANDLE)hRecord) {
			MsiRecordSetString(hRecord, 0, TEXT("Failed to spawn RunDll32 ResetACLAndPurgeFolder: [1]"));
			MsiRecordSetStringA(hRecord, 1, e.what());
			MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
		}
	}

	// Failed to do the elevated purge. Purge as much as we can unelevated.
	ResetACLAndPurgeFolder(szPath);
	return ERROR_SUCCESS;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
PurgeConfigFolders(_In_ MSIHANDLE hInstall)
{
	com_initializer com_init(NULL);
	s_hInstall = hInstall;

	tstring szValue;
	UINT uiResult = MsiGetProperty(hInstall, TEXT("ISSUE205VERSIONSINSTALLED"), szValue);
	if (uiResult != ERROR_SUCCESS || szValue.empty())
		return ERROR_SUCCESS;
	
	static LPCTSTR szConfigFolderProperty[] = {
		TEXT("OPENVPNCONFIGDIR"),
		TEXT("WIREGUARDCONFIGDIR")
	};
	tstring szPath;
	for (size_t i = 0; i < _countof(szConfigFolderProperty); ++i) {
		uiResult = MsiGetProperty(hInstall, szConfigFolderProperty[i], szValue);
		if (uiResult == ERROR_SUCCESS && !szValue.empty()) {
			PathRemoveBackslash(szValue);
			if (PurgeFolder(szValue.c_str()) == ERROR_INSTALL_USEREXIT)
				return ERROR_INSTALL_USEREXIT;
		}
		else
			LOG_ERROR_NUM(uiResult, TEXT("MsiGetProperty(\"%s\") failed"), szConfigFolderProperty[i]);
	}
	return ERROR_SUCCESS;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
RemoveWintunDriver(_In_ MSIHANDLE hInstall)
{
	com_initializer com_init(NULL);
	s_hInstall = hInstall;

	tstring sWintunPath;
	UINT uiResult = MsiGetProperty(hInstall, TEXT("CustomActionData"), sWintunPath);
	if (uiResult != ERROR_SUCCESS) {
		LOG_ERROR_NUM(uiResult, TEXT("MsiGetProperty(\"CustomActionData\") failed"));
		return ERROR_SUCCESS;
	}
	if (sWintunPath.empty())
		return ERROR_SUCCESS;

	library hWintun(LoadLibraryExW(sWintunPath.c_str(), NULL, LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32));
	if (!hWintun)
	{
		LOG_ERROR_NUM(GetLastError(), TEXT("LoadLibraryExW(\"%ls\") failed"), sWintunPath.c_str());
		return ERROR_SUCCESS;
	}
	WINTUN_SET_LOGGER_FUNC* WintunSetLogger;
	WINTUN_DELETE_DRIVER_FUNC* WintunDeleteDriver;
#define X(Name) ((*(FARPROC *)&Name = GetProcAddress(hWintun, #Name)) == NULL)
	if (X(WintunSetLogger) ||
		X(WintunDeleteDriver))
#undef X
	{
		LOG_ERROR_NUM(GetLastError(), TEXT("GetProcAddress failed"));
		return ERROR_SUCCESS;
	}
	WintunSetLogger(WintunLogger);
	if (!WintunDeleteDriver())
		LOG_ERROR_NUM(GetLastError(), TEXT("WintunDeleteDriver failed"));
	return ERROR_SUCCESS;
}

typedef struct tagFILEID {
	DWORD dwVolumeSerialNumber;
	DWORD nFileIndexHigh;
	DWORD nFileIndexLow;
} FILEID;
typedef FILEID* LPFILEID;

_Return_type_success_(return != FALSE) static BOOL
CalculateFileId(_In_z_ LPCTSTR szPath, _Out_ LPFILEID id)
{
	file hFile(CreateFile(szPath, 0, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL));
	if (!hFile)
		return FALSE;
	BY_HANDLE_FILE_INFORMATION info = { 0 };
	if (!GetFileInformationByHandle(hFile, &info))
		return FALSE;
	id->dwVolumeSerialNumber = info.dwVolumeSerialNumber;
	id->nFileIndexHigh = info.nFileIndexHigh;
	id->nFileIndexLow = info.nFileIndexLow;
	return TRUE;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
KillExecutableProcesses(_In_ MSIHANDLE hInstall)
{
	com_initializer com_init(NULL);
	s_hInstall = hInstall;

	tstring szExecutable;
	UINT uiResult = MsiGetProperty(hInstall, TEXT("CustomActionData"), szExecutable);
	if (uiResult != ERROR_SUCCESS) {
		LOG_ERROR_NUM(uiResult, TEXT("MsiGetProperty(\"CustomActionData\") failed"));
		return ERROR_SUCCESS;
	}
	if (szExecutable.empty())
		return ERROR_SUCCESS;
	FILEID idExecutable;
	if (!CalculateFileId(szExecutable.c_str(), &idExecutable))
		return ERROR_SUCCESS;

	process_snapshot hSnapshot(CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0));
	if (!hSnapshot)
		return ERROR_SUCCESS;

	PROCESSENTRY32 pe32 = { sizeof(PROCESSENTRY32) };
	for (BOOL fSuccess = Process32First(hSnapshot, &pe32); fSuccess; fSuccess = Process32Next(hSnapshot, &pe32)) {
		process hProcess(OpenProcess(PROCESS_TERMINATE | PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pe32.th32ProcessID));
		FILEID id;
		if (hProcess &&
			QueryFullProcessImageName(hProcess, 0, szExecutable) &&
			CalculateFileId(szExecutable.c_str(), &id) &&
			idExecutable.dwVolumeSerialNumber == id.dwVolumeSerialNumber &&
			idExecutable.nFileIndexHigh == id.nFileIndexHigh &&
			idExecutable.nFileIndexLow == id.nFileIndexLow &&
			TerminateProcess(hProcess, 0xC000026BL /*STATUS_DLL_INIT_FAILED_LOGOFF*/))
		{
			WaitForSingleObject(hProcess, INFINITE);

			PMSIHANDLE hRecord = MsiCreateRecord(3);
			if ((MSIHANDLE)hRecord) {
				MsiRecordSetString(hRecord, 0, TEXT("Killed \"[1]\" (pid [2])"));
				MsiRecordSetString(hRecord, 1, szExecutable.c_str());
				MsiRecordSetInteger(hRecord, 2, pe32.th32ProcessID);
				MsiProcessMessage(INSTALLMESSAGE_INFO, hRecord);
			}
		}
	}
	return ERROR_SUCCESS;
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved)
{
	UNREFERENCED_PARAMETER(hinstDLL);
	UNREFERENCED_PARAMETER(lpReserved);

	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
		GetSystemWindowsDirectoryW(sExplorerPath, _countof(sExplorerPath));
		PathCombineW(sExplorerPath, sExplorerPath, L"explorer.exe");
		GetModuleFileName(hinstDLL, sDllPath, _countof(sDllPath));
		sDllPath[_countof(sDllPath) - 1] = 0;
		break;
	}
	return TRUE;
}
