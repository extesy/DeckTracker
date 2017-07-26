#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <shlwapi.h>
#include <shlobj.h>

typedef unsigned __int64 QWORD, *PQWORD;
typedef int(__cdecl *MYPROC)(LPWSTR);
typedef DWORD(*mono_security_get_mode)();
typedef void(*mono_security_set_mode)(DWORD mode);
typedef void*(*mono_domain_get)();
typedef void*(*mono_domain_assembly_open)(PVOID domain, PCHAR file);
typedef void*(*mono_assembly_get_image)(PVOID assembly);
typedef void*(*mono_method_desc_new)(PCHAR method, BOOLEAN flag);
typedef void*(*mono_method_desc_search_in_image)(PVOID assembly, PVOID image);
typedef void*(*mono_class_from_name)(PVOID image, PCHAR namespacee, PCHAR name);
typedef void*(*mono_class_get_method_from_name)(PVOID classs, PCHAR name, DWORD param_count);
typedef void*(*mono_runtime_invoke)(PVOID method, PVOID instance, PVOID *params, PVOID exc);
typedef void*(*mono_thread_attach)(PVOID domain);
typedef void*(*mono_thread_detach)(PVOID domain);
typedef void*(*mono_get_root_domain)();
typedef void*(*mono_domain_create)();
typedef void*(*mono_domain_create_appdomain)(PCHAR friendly_name, PCHAR configuration_file);
typedef void*(*mono_domain_unload)(PVOID domain);
typedef void*(*mono_domain_set)(PVOID domain, BOOLEAN force);
typedef void*(*mono_thread_push_appdomain_ref)(PVOID domain);
typedef void*(*mono_thread_pop_appdomain_ref)();
typedef void(*mono_set_commandline_arguments)(int, const char* argv[], const char*);
typedef void(*mono_jit_parse_options)(int argc, char* argv[]);
typedef void(*mono_debug_init)(int format);

mono_security_get_mode do_mono_security_get_mode;
mono_security_set_mode do_mono_security_set_mode;
mono_domain_get do_mono_domain_get;
mono_domain_assembly_open do_mono_domain_assembly_open;
mono_method_desc_new do_mono_method_desc_new;
mono_method_desc_search_in_image do_mono_method_desc_search_in_image;
mono_assembly_get_image do_mono_assembly_get_image;
mono_class_from_name do_mono_class_from_name;
mono_class_get_method_from_name do_mono_class_get_method_from_name;
mono_runtime_invoke do_mono_runtime_invoke;
mono_thread_attach do_mono_thread_attach;
mono_thread_detach do_mono_thread_detach;
mono_get_root_domain do_mono_get_root_domain;
mono_domain_create do_mono_domain_create;
mono_domain_create_appdomain do_mono_domain_create_appdomain;
mono_domain_unload do_mono_domain_unload;
mono_domain_set do_mono_domain_set;
mono_thread_push_appdomain_ref do_mono_thread_push_appdomain_ref;
mono_thread_pop_appdomain_ref do_mono_thread_pop_appdomain_ref;
mono_set_commandline_arguments do_mono_set_commandline_arguments;
mono_jit_parse_options do_mono_jit_parse_options;
mono_debug_init do_mono_debug_init;

VOID Inject(HMODULE hModule);

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call) {
		case DLL_PROCESS_ATTACH:
			CreateThread(nullptr, 0, (LPTHREAD_START_ROUTINE)Inject, hModule, 0, nullptr);
			break;
		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
		case DLL_PROCESS_DETACH:
		default:
			break;
	}
	return TRUE;
}

VOID LogError(LPTSTR message)
{
	TCHAR szPath[MAX_PATH] = {0};
	if (SUCCEEDED(SHGetSpecialFolderPath(nullptr, szPath, CSIDL_APPDATA, false)))
		PathAppend(szPath, L"\\UniversalDeckTracker\\error.log");

	auto hLogFile = CreateFile(szPath, FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
	if (hLogFile != INVALID_HANDLE_VALUE) {
		char sMessage[1024];
		WideCharToMultiByte(CP_UTF8, 0, message, wcslen(message) + 1, sMessage, sizeof sMessage, nullptr, nullptr);
		DWORD dwBytesWritten;
		WriteFile(hLogFile, sMessage, strlen(sMessage) + 1, &dwBytesWritten, nullptr);
		CloseHandle(hLogFile);
	}
}

VOID Inject(HMODULE hModule)
{
	HMODULE hMono;
	while ((hMono = GetModuleHandle(L"mono.dll")) == nullptr) Sleep(10);

	do_mono_security_get_mode = (mono_security_get_mode)GetProcAddress(hMono, "mono_security_get_mode");
	do_mono_security_set_mode = (mono_security_set_mode)GetProcAddress(hMono, "mono_security_set_mode");
	do_mono_domain_get = (mono_domain_get)GetProcAddress(hMono, "mono_domain_get");
	do_mono_domain_assembly_open = (mono_domain_assembly_open)GetProcAddress(hMono, "mono_domain_assembly_open");
	do_mono_assembly_get_image = (mono_assembly_get_image)GetProcAddress(hMono, "mono_assembly_get_image");
	do_mono_method_desc_new = (mono_method_desc_new)GetProcAddress(hMono, "mono_method_desc_new");
	do_mono_method_desc_search_in_image = (mono_method_desc_search_in_image)GetProcAddress(hMono, "mono_method_desc_search_in_image");
	do_mono_class_from_name = (mono_class_from_name)GetProcAddress(hMono, "mono_class_from_name");
	do_mono_class_get_method_from_name = (mono_class_get_method_from_name)GetProcAddress(hMono, "mono_class_get_method_from_name");
	do_mono_runtime_invoke = (mono_runtime_invoke)GetProcAddress(hMono, "mono_runtime_invoke");
	do_mono_thread_attach = (mono_thread_attach)GetProcAddress(hMono, "mono_thread_attach");
	do_mono_thread_detach = (mono_thread_detach)GetProcAddress(hMono, "mono_thread_detach");
	do_mono_get_root_domain = (mono_get_root_domain)GetProcAddress(hMono, "mono_get_root_domain");
	do_mono_domain_create = (mono_domain_create)GetProcAddress(hMono, "mono_domain_create");
	do_mono_domain_create_appdomain = (mono_domain_create_appdomain)GetProcAddress(hMono, "mono_domain_create_appdomain");
	do_mono_domain_set = (mono_domain_set)GetProcAddress(hMono, "mono_domain_set");
	do_mono_thread_push_appdomain_ref = (mono_thread_push_appdomain_ref)GetProcAddress(hMono, "mono_thread_push_appdomain_ref");
	do_mono_thread_pop_appdomain_ref = (mono_thread_pop_appdomain_ref)GetProcAddress(hMono, "mono_thread_pop_appdomain_ref");
	do_mono_set_commandline_arguments = (mono_set_commandline_arguments)GetProcAddress(hMono, "mono_set_commandline_arguments");
	do_mono_jit_parse_options = (mono_jit_parse_options)GetProcAddress(hMono, "mono_jit_parse_options");
	do_mono_debug_init = (mono_debug_init)GetProcAddress(hMono, "mono_debug_init");

	TCHAR path[MAX_PATH];
	GetModuleFileName(hModule, path, MAX_PATH);
	PathRemoveFileSpec(path);
	PathAppend(path, L"DeckTracker.InGame.dll");

	PVOID monodomain = nullptr;
	for (auto attempt = 0; attempt < 5; attempt++) {
		__try {
			Sleep(500);
			monodomain = do_mono_get_root_domain();
			do_mono_thread_attach(monodomain);
			break;
		} __except (EXCEPTION_EXECUTE_HANDLER) {
			LogError(L"Exception happened while attaching to mono thread\r\n");
			monodomain = nullptr;
		}
	}

	if (monodomain == nullptr) return;

	__try {
		char sPath[MAX_PATH];
		WideCharToMultiByte(CP_UTF8, 0, path, wcslen(path) + 1, sPath, sizeof sPath, nullptr, nullptr);
		auto assembly = do_mono_domain_assembly_open(monodomain, sPath);
		if (assembly == nullptr) LogError(L"Error happened while calling mono_domain_assembly_open\r\n");
		auto image = do_mono_assembly_get_image(assembly);
		if (image == nullptr) LogError(L"Error happened while calling mono_assembly_get_image\r\n");
		auto clazz = do_mono_class_from_name(image, "DeckTracker", "Hook");
		if (clazz == nullptr) LogError(L"Error happened while calling mono_class_from_name\r\n");
		auto method = do_mono_class_get_method_from_name(clazz, "Initialize", 0);
		if (method == nullptr) LogError(L"Error happened while calling mono_class_get_method_from_name\r\n");
		auto result = do_mono_runtime_invoke(method, nullptr, nullptr, nullptr);
		if (result == nullptr) LogError(L"Error happened while calling mono_runtime_invoke\r\n");
	}
	__except (EXCEPTION_EXECUTE_HANDLER) {
		LogError(L"Exception happened while invoking DeckTracker.InGame.dll\r\n");
	}
}
