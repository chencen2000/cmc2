#include "pch.h"
#include <SetupAPI.h>
#include <strsafe.h>
#include <cfgmgr32.h>
#include <vcclr.h>
#include <atlstr.h>
#include <usbioctl.h>

int removeDeviceByInstanceId2(System::String^ deviceInstanceId) {
	int ret = ERROR_INVALID_PARAMETER;
	LogIt(System::String::Format("removeDeviceByInstanceId2: [{0}] ++ ", deviceInstanceId));
	char* diid = (char*)(void*)System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(deviceInstanceId);
	if (diid != NULL) {
		HANDLE h = SetupDiCreateDeviceInfoList(NULL, NULL);
		if (h == INVALID_HANDLE_VALUE) {
			ret = GetLastError();
		}
		else {
			SP_DEVINFO_DATA devInfoData;
			ZeroMemory(&devInfoData, sizeof(SP_DEVINFO_DATA));
			devInfoData.cbSize = sizeof(SP_DEVINFO_DATA);
			if (SetupDiOpenDeviceInfoA(h, diid, NULL, 0, &devInfoData)) {
				ULONG status = 0;
				ULONG problem = 0;
				ret = CM_Get_DevNode_Status(&status, &problem, devInfoData.DevInst, 0);
				if (ret == CR_NO_SUCH_DEVNODE) {
					if (SetupDiRemoveDevice(h, &devInfoData)) {
						ret = NO_ERROR;
					}
					else {
						ret = GetLastError();
					}
				}
			}
			else
				ret = GetLastError();
			SetupDiDestroyDeviceInfoList(h);
		}
		System::Runtime::InteropServices::Marshal::FreeHGlobal((System::IntPtr)diid);
	}
	LogIt(System::String::Format("removeDeviceByInstanceId2: [{0}] -- ret={1}", deviceInstanceId, ret));
	return ret;
}
int removeDeviceByInstanceId(System::String^ deviceInstanceId) {
	int ret = ERROR_INVALID_PARAMETER;	
	LogIt(System::String::Format("removeDeviceByInstanceId: [{0}] ++ ", deviceInstanceId));
	char* diid = (char*)(void*)System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(deviceInstanceId);
	if (diid != NULL) {
		HANDLE h = SetupDiCreateDeviceInfoList(NULL, NULL);
		if (h == INVALID_HANDLE_VALUE) {
			ret = GetLastError();
		}
		else {
			SP_DEVINFO_DATA devInfoData;
			ZeroMemory(&devInfoData, sizeof(SP_DEVINFO_DATA));
			devInfoData.cbSize = sizeof(SP_DEVINFO_DATA);
			if (SetupDiOpenDeviceInfoA(h, diid, NULL, 0, &devInfoData)) {
				ULONG status = 0;
				ULONG problem = 0;
				ret = CM_Get_DevNode_Status(&status, &problem, devInfoData.DevInst, 0);
				if (ret == CR_NO_SUCH_DEVNODE) {
					SP_REMOVEDEVICE_PARAMS rdp;
					ZeroMemory(&rdp, sizeof(SP_REMOVEDEVICE_PARAMS));
					rdp.ClassInstallHeader.cbSize = sizeof(SP_CLASSINSTALL_HEADER);
					rdp.ClassInstallHeader.InstallFunction = DIF_REMOVE;
					rdp.Scope = DI_REMOVEDEVICE_GLOBAL;
					if (SetupDiSetClassInstallParams(h, &devInfoData, &rdp.ClassInstallHeader, sizeof(SP_REMOVEDEVICE_PARAMS))) {
						if (SetupDiCallClassInstaller(DIF_REMOVE, h, &devInfoData)) {
							SP_DEVINSTALL_PARAMS params;
							params.cbSize = sizeof(SP_DEVINSTALL_PARAMS);
							if (SetupDiGetDeviceInstallParams(h, &devInfoData, &params)) {
								ret = NO_ERROR;
							}
							else
								ret = GetLastError();
						}
						else
							ret = GetLastError();
					}
					else
						ret = GetLastError();
				}
			}
			else {
				ret = GetLastError();
			}
			SetupDiDestroyDeviceInfoList(h);
		}
		System::Runtime::InteropServices::Marshal::FreeHGlobal((System::IntPtr)diid);
	}
	LogIt(System::String::Format("removeDeviceByInstanceId: [{0}] -- ret={1}", deviceInstanceId, ret));
	return ret;
}

System::Collections::ArrayList^ listAllDevices() {
	DWORD ret = ERROR_INVALID_PARAMETER;
	BYTE buffer[1024];
	LogItW(L"listAllDevices: ++ ");
	System::Collections::ArrayList^ list = gcnew System::Collections::ArrayList();
	HANDLE h = SetupDiGetClassDevs(NULL, NULL, NULL, DIGCF_ALLCLASSES);
	if (h != INVALID_HANDLE_VALUE) {
		SP_DEVINFO_DATA devInfoData;
		devInfoData.cbSize = sizeof(SP_DEVINFO_DATA);
		DWORD idx = 0;
		DWORD sz = 0;
		CONFIGRET cr = 0;
		ULONG status = 0;
		ULONG problem = 0;
		while (SetupDiEnumDeviceInfo(h, idx++, &devInfoData)) {
			cr = CM_Get_DevNode_Status(&status, &problem, devInfoData.DevInst, 0);
			if (SetupDiGetDeviceInstanceIdA(h, &devInfoData, (char*)buffer, MAX_PATH, &sz)) {
				//LogItW((PWSTR)buffer);
				if (cr == CR_NO_SUCH_DEVINST) {
					System::String^ s = System::Runtime::InteropServices::Marshal::PtrToStringAnsi(static_cast<System::IntPtr>((char*)buffer));
					list->Add(s);
				}
			}
		}
		SetupDiDestroyDeviceInfoList(h);
		ret = NO_ERROR;
	}
	else
		ret = GetLastError();
	StringCchPrintfW((WCHAR*)buffer, 500, L"listAllDevices: -- ret=%d", ret);
	LogItW((WCHAR*)buffer);
	return list;
}

bool doesUSBDeviceExist(System::String^ calFilename) {
	bool ret = false;
	LogIt(System::String::Format("doesUSBDeviceExist: ++ calibration={0}", calFilename));
	CStringA inifilename = calFilename;
	int labels = GetPrivateProfileIntA("amount", "labels", 0, inifilename);
	char *keys[] = {
		"label",
		"label_2.0",
		"label_3.0",
	};
	int key_count = sizeof(keys) / sizeof(keys[0]);
	char buffer[1024];
	for (int i = 1; i <= labels && !ret; i++) {
		for (int j = 0; j < key_count && !ret; j++) {
			try {
				CStringA s;
				s.Format("%d", i);
				ZeroMemory(buffer, sizeof(buffer));
				GetPrivateProfileStringA(keys[j], s, "", buffer, MAX_PATH, inifilename);
				PSTR p = StrChrA(buffer, '@');
				if (p) {
					int port = atoi(buffer);
					p++;
					s.Format("\\\\?\\%s", p);
					if (port > 0) {
						HANDLE h = CreateFileA(s, GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, NULL, NULL);
						if (h != INVALID_HANDLE_VALUE) {
							DWORD sz = sizeof(buffer);
							ZeroMemory(buffer, sz);
							if (DeviceIoControl(h, IOCTL_USB_GET_NODE_INFORMATION, buffer, 1024, buffer, 1024, &sz, NULL)) {
								PUSB_NODE_INFORMATION hub_info = (PUSB_NODE_INFORMATION)buffer;
								if (hub_info->NodeType == UsbHub) {
									if (port > 0 && port <= hub_info->u.HubInformation.HubDescriptor.bNumberOfPorts) {
										PUSB_NODE_CONNECTION_INFORMATION_EX c_info = (PUSB_NODE_CONNECTION_INFORMATION_EX)buffer;
										sz = sizeof(USB_NODE_CONNECTION_INFORMATION_EX);
										ZeroMemory(c_info, sizeof(USB_NODE_CONNECTION_INFORMATION_EX));
										c_info->ConnectionIndex = port;
										if (DeviceIoControl(h, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, c_info, 1024, c_info, 1024, &sz, NULL)) {
											if (c_info->ConnectionStatus != NoDeviceConnected)
												ret = true;
										}
									}
								}
							}
							CloseHandle(h);
						}
						else {
							DWORD d = GetLastError();
							printf("%d\n", d);
						}
					}
				}
			}
			catch (System::Exception^) {}
		}
	}
	LogIt(System::String::Format("doesUSBDeviceExist: -- return {0}", ret));
	return ret;
}