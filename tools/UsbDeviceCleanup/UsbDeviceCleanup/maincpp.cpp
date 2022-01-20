#include "pch.h"
#include <atlstr.h>
using namespace System;

char* TAG = "FDUSBDEVICECLEANUP";
CStringA logFilename;
long long last_USB_Device_arrival = 0;

void LogIt(String^ msg) {
    System::DateTime^ t0 = System::DateTime::Now;
    String^ stag = gcnew String(TAG);
    String^ s = String::Format("[{0}]: [{1}]: {2}", stag, t0->ToString("o"), msg);
    System::Diagnostics::Trace::WriteLine(s);
	if (!logFilename.IsEmpty()) {
		String^ slog = gcnew String(logFilename);
		System::IO::File::AppendAllText(slog, s + "\n");
	}
}

void LogItW(WCHAR* msg) {
    String^ smsg = gcnew String(msg);
    LogIt(smsg);
}

void LogItA(char* msg) {
    String^ smsg = gcnew String(msg);
    LogIt(smsg);
}

void prepare_log() {
	DateTime t = DateTime::Now;
	System::String^ s = System::String::Format("UsbDeviceCleanup_{0}{1}{2}T{3}{4}{5}.log", t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);
	logFilename = System::IO::Path::Combine(System::Environment::GetEnvironmentVariable("APSTHOME"), s);
	// remove old log files
	//array<System::String^>^ files = System::IO::Directory::GetFiles(System::Environment::GetEnvironmentVariable("APSTHOME"), "UsbDeviceCleanup_*.log");
	//for each(System::String^ f in files) {
	//	System::IO::FileInfo^ info = gcnew System::IO::FileInfo(f);
	//	if ((t - info->CreationTime).TotalDays > 1) {
	//		s = System::IO::Path::Combine(System::Environment::GetEnvironmentVariable("APSTHOME"), "Logs", info->Name);
	//		System::IO::File::Move(f, s);
	//	}
	//}
}
void upload_log() {
	array<System::String^>^ files = System::IO::Directory::GetFiles(System::Environment::GetEnvironmentVariable("APSTHOME"), "UsbDeviceCleanup_*.log");
	for each(System::String^ f in files) {
		System::String^ s = System::IO::Path::Combine(System::Environment::GetEnvironmentVariable("APSTHOME"), "Logs", System::IO::Path::GetFileName(f));
		System::IO::File::Move(f, s);
	}
}
void on_device_changed(System::Object^ sender, System::Management::EventArrivedEventArgs^ arg) {
	try {
		System::String^ s = arg->NewEvent->ClassPath->RelativePath;
		System::Management::ManagementBaseObject^ target = (System::Management::ManagementBaseObject^)arg->NewEvent->Properties["TargetInstance"]->Value;
		System::DateTime t0 = System::DateTime(System::Convert::ToInt64(arg->NewEvent->Properties["TIME_CREATED"]->Value));
		if (System::String::Compare(s, "__InstanceCreationEvent") == 0) {
			LogIt(System::String::Format("ARRIVAL ({0}) at {1}", target->Properties["Dependent"]->Value, t0.ToString("o")));
		}
		else if (System::String::Compare(s, "__InstanceDeletionEvent") == 0) {
			LogIt(System::String::Format("REMOVAL ({0}) at {1}", target->Properties["Dependent"]->Value, t0.ToString("o")));
		}
		else {
			LogIt(System::String::Format("Unknown path: {0}", s));
		}
	}
	catch (System::Exception^) {}
}
void MonitorUsbDevice(System::Object^ arg) {
	LogIt(System::String::Format("MonitorUsbDevice: ++"));
	System::Threading::EventWaitHandle^ e = (System::Threading::EventWaitHandle^)arg;
	System::Management::WqlEventQuery^ q1 = gcnew System::Management::WqlEventQuery("__InstanceCreationEvent", System::TimeSpan(0, 0, 1), "TargetInstance isa 'Win32_USBControllerDevice'");
	System::Management::ManagementEventWatcher^ w1 = gcnew System::Management::ManagementEventWatcher();
	w1->Query = q1;
	w1->Options->Timeout = System::TimeSpan(0, 0, 1);
	w1->EventArrived += gcnew System::Management::EventArrivedEventHandler(on_device_changed);
	w1->Start();
	System::Management::WqlEventQuery^ q2 = gcnew System::Management::WqlEventQuery("__InstanceDeletionEvent", System::TimeSpan(0, 0, 1), "TargetInstance isa 'Win32_USBControllerDevice'");
	System::Management::ManagementEventWatcher^ w2 = gcnew System::Management::ManagementEventWatcher();
	w2->Query = q2;
	w2->Options->Timeout = System::TimeSpan(0, 0, 1);
	w2->EventArrived += gcnew System::Management::EventArrivedEventHandler(on_device_changed);
	w2->Start();
	while (!e->WaitOne(5000))
	{
		//try {
		//	System::Management::ManagementBaseObject^ obj = w1->WaitForNextEvent();
		//	LogIt(obj->ClassPath->ClassName);
		//	System::Management::ManagementBaseObject^ target = (System::Management::ManagementBaseObject^)obj->Properties["TargetInstance"]->Value;
		//	System::DateTime t0 = System::DateTime(System::Convert::ToInt64(obj->Properties["TIME_CREATED"]->Value));
		//	last_USB_Device_arrival = System::Convert::ToInt64(obj->Properties["TIME_CREATED"]->Value);
		//	LogIt(System::String::Format("ARRIVAL ({0}) at {1}",target->Properties["Dependent"]->Value, t0.ToString("o")));
		//}
		//catch (System::Management::ManagementException^ ex) {

		//}
		//try {
		//	System::Management::ManagementBaseObject^ obj = w2->WaitForNextEvent();
		//	LogIt(obj->ClassPath->ClassName);
		//	System::Management::ManagementBaseObject^ target = (System::Management::ManagementBaseObject^)obj->Properties["TargetInstance"]->Value;
		//	System::DateTime t0 = System::DateTime(System::Convert::ToInt64(obj->Properties["TIME_CREATED"]->Value));
		//	//last_USB_Device_arrival = System::Convert::ToInt64(obj->Properties["TIME_CREATED"]->Value);
		//	LogIt(System::String::Format("REMOVAL ({0}) at {1}", target->Properties["Dependent"]->Value, t0.ToString("o")));
		//}
		//catch (::Management::ManagementException^ ex) {

		//}
	}
	w1->Stop();
	w2->Stop();
	//	
	//)
	//WqlEventQuery query =
	//	new WqlEventQuery("__InstanceCreationEvent",
	//		new TimeSpan(0, 0, 1),
	//		"TargetInstance isa \"Win32_Process\"");
	LogIt(System::String::Format("MonitorUsbDevice: --"));
}

System::Tuple<int, int, System::String^>^ run_exe(System::String^ exe, System::String^ cmd, int timeout) {
	int ret = ERROR_INVALID_PARAMETER;
	int exit_code = 0;
	System::String^ s = "";
	LogIt(System::String::Format("run_exe: ++ exe={0}, args={1}", exe, cmd));
	if (System::IO::File::Exists(exe)) {
		System::Diagnostics::Process^ p = gcnew System::Diagnostics::Process();
		p->StartInfo->FileName = exe;
		p->StartInfo->Arguments = cmd;
		p->StartInfo->CreateNoWindow = true;
		p->StartInfo->UseShellExecute = false;
		p->StartInfo->RedirectStandardOutput = true;
		p->Start();
		if (p->WaitForExit(timeout)) {
			ret = NO_ERROR;
			// process has terminated within timeout
			s = p->StandardOutput->ReadToEnd();
			exit_code = p->ExitCode;
			LogIt(System::String::Format("run_exe: {0} has been ternimated and exit code is {1}", exe, exit_code));
			LogIt(System::String::Format("run_exe: Process stdout is\n{0}", s));
		}
		else {
			//
			p->Kill();
			ret = ERROR_TIMEOUT;
		}

	}
	LogIt(System::String::Format("run_exe: -- ret={0}", ret));
	return gcnew System::Tuple<int, int, System::String^>(ret, exit_code, s);
}

int removeDeviceByInstanceIdv3(System::String^ diid, int timeout) {
	int ret = ERROR_INVALID_PARAMETER;
	LogIt(System::String::Format("removeDeviceByInstanceIdv3: [{0}] ++ diid={0}", diid));
	System::String^ exe = System::IO::Path::Combine(System::Environment::GetEnvironmentVariable("SystemRoot"), "System32", "pnputil.exe");
	System::String^ arg = System::String::Format("/remove-device \"{0}\"", diid);
	System::Tuple<int, int, System::String^>^ r = run_exe(exe, arg, timeout);
	if (r->Item1 == NO_ERROR) {
		if (r->Item2 == NO_ERROR)
			ret = NO_ERROR;
		else
			ret = r->Item2;
	}
	else
		ret = r->Item1;
	LogIt(System::String::Format("removeDeviceByInstanceIdv3: [{0}] -- ret={1}", diid, ret));
	return ret;
}

int start_service(System::Collections::Specialized::StringDictionary^ args, System::Threading::EventWaitHandle^ quitEvent) {
    int ret = 0;
    LogIt("start_service: ++");
    System::Collections::ArrayList^ devices = listAllDevices();
    DateTime t_list = System::DateTime::Now;
    for (int i = 0; i < devices->Count; i++) {
        LogIt(System::String::Format("{0:D5}: {1}", i, devices[i]));
    }
    System::Diagnostics::PerformanceCounter^ pc = gcnew System::Diagnostics::PerformanceCounter("Processor", "% Processor Time", "_Total");
    float cpu_usage = pc->NextValue();
    bool done = FALSE;
	System::String^ cal;
	if (args->ContainsKey("cal")) {
		cal = args["cal"];
	}
	else {
		cal = System::Environment::GetEnvironmentVariable("APSTHOME");
		if (!System::String::IsNullOrEmpty(cal)) {
			cal = System::IO::Path::Combine(cal, "calibration.ini");
		}
	}
	int timeout = 5000;
	if (args->ContainsKey("timeout")) {
		if (!System::Int32::TryParse(args["timeout"], timeout))
			timeout = 5000;
	}
	int method = 3;
    if (args->ContainsKey("method")) {
        if (!System::Int32::TryParse(args["method"], method))
            method = 3;
    }
    int interval = 1000;
    if (args->ContainsKey("interval"))
    {
        if (!System::Int32::TryParse(args["interval"], interval))
            interval = 1000;
    }
    int list_device_interval = 60;
    if (args->ContainsKey("listinterval"))
    {
        if (!System::Int32::TryParse(args["listinterval"], list_device_interval))
            list_device_interval = 60;
    }
    int cpu_busy_interval = 5000;
    if (args->ContainsKey("cpubusyinterval"))
    {
        if (!System::Int32::TryParse(args["cpubusyinterval"], cpu_busy_interval))
            cpu_busy_interval = 5000;
    }
    int cpu_threshold = 35;
    if (args->ContainsKey("cpuhreshold"))
    {
        if (!System::Int32::TryParse(args["cpuhreshold"], cpu_threshold))
            cpu_threshold = 35;
    }
    int delete_device_threshold = 500;
    if (args->ContainsKey("deletethreshold"))
    {
        if (!System::Int32::TryParse(args["deletethreshold"], delete_device_threshold))
            delete_device_threshold = 500;
    }
    LogIt("Dump paramers:");
    LogIt(System::String::Format("method={0}", method));
	LogIt(System::String::Format("timeout={0}", timeout));
    LogIt(System::String::Format("interval={0}ms", interval));
    LogIt(System::String::Format("listinterval={0}seconds", list_device_interval));
    LogIt(System::String::Format("cpubusyinterval={0}ms", cpu_busy_interval));
    LogIt(System::String::Format("cpuhreshold={0}%", cpu_threshold));
    LogIt(System::String::Format("deletethreshold={0}ms", delete_device_threshold));
	LogIt(System::String::Format("calibration={0}", cal));
	int _interval = interval;
    while (!done) {
        cpu_usage = pc->NextValue();
        LogIt(System::String::Format("CPU usage is {0:F2}%", cpu_usage));
        if (System::Console::KeyAvailable)
        {
            LogIt("Program is going to terminate by keyboard click.");
            done = TRUE;
            continue;
        }
        if (quitEvent->WaitOne(_interval))
        {
            LogIt("Program is going to terminate by event set.");
            done = TRUE;
            continue;
        }
        if (cpu_usage > cpu_threshold) {
            LogIt(System::String::Format("CPU usage ({0:F2}%) is too high.", cpu_usage));
            if (interval < cpu_busy_interval)
				_interval = cpu_busy_interval;
            continue;
        }
		if (doesUSBDeviceExist(cal)) {
			LogIt(System::String::Format("Detected USB device connected."));
			if (interval < cpu_busy_interval)
				_interval = cpu_busy_interval;
			continue;
		}
        if (devices->Count > 0) {
            String^ d = (String^)devices[0];
            System::DateTime t0 = System::DateTime::Now;
            if (method == 1) {
                if (removeDeviceByInstanceId(d) != NOERROR) {
					LogIt(System::String::Format("ERROR Remove deivce {0}.", d));
                }
				devices->Remove(d);
            }
            else if (method == 2) {
                if (removeDeviceByInstanceId2(d) != NOERROR) {
					LogIt(System::String::Format("ERROR Remove deivce {0}.", d));
                }
				devices->Remove(d);
            }
			else if (method == 3) {
				if (removeDeviceByInstanceIdv3(d, timeout) != NOERROR) 
					LogIt(System::String::Format("ERROR Remove deivce {0}.", d));
				devices->Remove(d);
			}
            System::TimeSpan ts = System::DateTime::Now - t0;
            LogIt(System::String::Format("It took {0} ms to remove the device.", ts.TotalMilliseconds));
            if (ts.TotalMilliseconds < delete_device_threshold) {
				_interval = interval;
            }
            else {
				_interval = cpu_busy_interval;
                LogIt(System::String::Format("Delete device took too long. {0} > {1}. Set interval higher {2}", ts.TotalMilliseconds, delete_device_threshold, cpu_busy_interval));
            }
        }
        else {            
            if ((System::DateTime::Now - t_list).TotalSeconds > list_device_interval) {
                devices = listAllDevices();
                for (int i = 0; i < devices->Count; i++) {
                    LogIt(System::String::Format("{0:D5}: {1}", i, devices[i]));
                }
                t_list = System::DateTime::Now;
            }
        }
		// upload log if log file size > 10M
		if (System::IO::File::Exists(gcnew System::String(logFilename))) {
			System::IO::FileInfo ^ ff = gcnew System::IO::FileInfo(gcnew System::String(logFilename));
			if (ff->Length > 10 * 1024 * 1024) {
				upload_log();
				prepare_log();
			}
		}
    }
    LogIt("start_service: --");
    return ret;
}

int main(array<System::String^>^ args)
{
    int ret = 0;
    System::Configuration::Install::InstallContext^ _args = gcnew System::Configuration::Install::InstallContext(nullptr, args);
    String^ stag = gcnew String(TAG);
    if (_args->IsParameterTrue("debug")) {
        System::Console::WriteLine("Wait for debugger, press any key to contibue...");
        System::Console::ReadKey();
    }
    if (_args->IsParameterTrue("start-service")) {
        bool own;
        System::Threading::EventWaitHandle^ e = gcnew System::Threading::EventWaitHandle(false, System::Threading::EventResetMode::ManualReset, stag, own);
        if (own) {
			prepare_log();
            ret = start_service(_args->Parameters, e);
            e->Close();
			upload_log();
        }
        else {
            LogIt("Service already running.");
            e->Close();
            ret = 2;
        }
    }
    else if (_args->IsParameterTrue("kill-service")) {    
        try
        {
            System::Threading::EventWaitHandle^ e = System::Threading::EventWaitHandle::OpenExisting(stag);
            if (e != nullptr) {
                e->Set();
            }
        }
        catch (System::Exception^ e)
        {

        }
    }
    else {
        // test
		//removeDeviceByInstanceIdv3("USB\\VID_05AC&PID_12A8&MI_00\\b&316a4ee2&0&0000", 3000);
		//prepare_log(nullptr);
		//System::Threading::EventWaitHandle^ e = gcnew System::Threading::EventWaitHandle(false, System::Threading::EventResetMode::AutoReset);
		//System::Threading::Thread^ t = gcnew System::Threading::Thread(gcnew System::Threading::ParameterizedThreadStart(MonitorUsbDevice));
		//t->IsBackground = true;
		//t->Start(e);
		//System::Console::ReadKey();
		//e->Set();
		//t->Join();
		//LogIt("Main: --");
		System::String^ s = "C:\\Users\\qa\\OneDrive\\Documents\\ConnectWiseControl\\Files\\calibration.ini";
		doesUSBDeviceExist(s);
	}
    return ret;
}
