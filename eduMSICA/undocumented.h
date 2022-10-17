#pragma once

#include <Windows.h>

EXTERN_C const IID IID_ICMLuaUtil;
extern "C++"
{
	MIDL_INTERFACE("6EDD6D74-C007-4E75-B76A-E5740995E24C")
	ICMLuaUtil: public IUnknown{
	public:
		virtual HRESULT STDMETHODCALLTYPE SetRasCredentials() = 0;
		virtual HRESULT STDMETHODCALLTYPE SetRasEntryProperties() = 0;
		virtual HRESULT STDMETHODCALLTYPE DeleteRasEntry() = 0;
		virtual HRESULT STDMETHODCALLTYPE LaunchInfSection() = 0;
		virtual HRESULT STDMETHODCALLTYPE LaunchInfSectionEx() = 0;
		virtual HRESULT STDMETHODCALLTYPE CreateLayerDirectory() = 0;
		virtual HRESULT STDMETHODCALLTYPE ShellExec(_In_z_ LPCWSTR lpFile, _In_opt_z_ LPCWSTR lpParameters, _In_opt_z_ LPCWSTR lpDirectory, _In_ ULONG fMask, _In_ ULONG nShow) = 0;
		virtual HRESULT STDMETHODCALLTYPE SetRegistryStringValue(_In_ HKEY hKey, _In_opt_z_ LPCWSTR lpSubKey, _In_opt_z_ LPCWSTR lpValueName, _In_z_ LPCWSTR lpValueString) = 0;
		virtual HRESULT STDMETHODCALLTYPE DeleteRegistryStringValue(_In_ HKEY hKey, _In_z_ LPCWSTR lpSubKey, _In_z_ LPCWSTR lpValueName) = 0;
		virtual HRESULT STDMETHODCALLTYPE DeleteRegKeysWithoutSubKeys() = 0;
		virtual HRESULT STDMETHODCALLTYPE DeleteRegTree() = 0;
		virtual HRESULT STDMETHODCALLTYPE ExitWindowsFunc() = 0;
		virtual HRESULT STDMETHODCALLTYPE AllowAccessToTheWorld() = 0;
		virtual HRESULT STDMETHODCALLTYPE CreateFileAndClose() = 0;
		virtual HRESULT STDMETHODCALLTYPE DeleteHiddenCmProfileFiles() = 0;
		virtual HRESULT STDMETHODCALLTYPE CallCustomActionDll() = 0;
		virtual HRESULT STDMETHODCALLTYPE RunCustomActionExe(_In_z_ LPCWSTR lpFile, _In_opt_z_ LPCWSTR lpParameters, _COM_Outptr_ LPCWSTR* pszHandleAsHexString) = 0;
		virtual HRESULT STDMETHODCALLTYPE SetRasSubEntryProperties() = 0;
		virtual HRESULT STDMETHODCALLTYPE DeleteRasSubEntry() = 0;
		virtual HRESULT STDMETHODCALLTYPE SetCustomAuthData() = 0;
	};
}