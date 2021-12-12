using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Xml;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace twController
{
    class envClass
    {
        static private envClass _this = null;
        static public envClass getInstance()
        {
            if (_this==null)
            {
                _this = new envClass();
            }
            return _this;
        }

        private string appName = string.Empty;
        public string AppName
        {
            get { return appName; }
        }
        private string exePath = string.Empty;
        public string ExePath
        {
            get { return exePath; }
        }
        private string runtimePath = string.Empty;
        public string RuntimePath
        {
            get { return runtimePath; }
        }
        private string serverUrl=string.Empty;
        public string ServerUrl
        {
            get { return serverUrl ; }
        }
        private System.Configuration.Install.InstallContext commandLine = null;
        public System.Configuration.Install.InstallContext CommandLine
        {
            get { return commandLine; }
        }
        envClass()
        {
            // exePath, for example "c:\program files\futuredial\apst"
            exePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            // url
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString("config", "server", "http://camarodev.futuredial.com/websync", sb, (uint)sb.Capacity, System.IO.Path.Combine(exePath, "config.ini"));
            serverUrl = sb.ToString();
            // runtime path, for exmaple, "c:\pregamdata\Futuredial\tetherwing"
            String appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            sb.Clear();
            win32API.GetPrivateProfileString("config", "appname", "TetherWing", sb, (uint)sb.Capacity, System.IO.Path.Combine(exePath, "config.ini"));
            appName = sb.ToString();
            runtimePath = System.IO.Path.Combine(appDataPath, "FutureDial", appName);
            System.IO.Directory.CreateDirectory(runtimePath);
        }
        public void setParameters(System.Configuration.Install.InstallContext args) 
        {
            commandLine = args;
            // prepare the product name
            if (!commandLine.Parameters.ContainsKey("product"))
            {
                if (commandLine.IsParameterTrue("sts"))
                {
                    commandLine.Parameters.Add("product", "sts");
                }
                else if (commandLine.IsParameterTrue("mobileq"))
                {
                    commandLine.Parameters.Add("product", "mobileq");
                }
                else
                {
                    commandLine.Parameters.Add("product", envClass.getInstance().GetConfigValueByKey("config", "product", "sts"));
                }
            }
            if (!commandLine.Parameters.ContainsKey("ui"))
            {
                commandLine.Parameters.Add("ui", System.IO.Path.Combine(exePath, "GreenT.exe"));
            }
            // dump args
            LogIt("Dump parameters:");
            LogIt(string.Format("{0,-16} : {1}", "key", "value"));
            foreach (string k in args.Parameters.Keys)
            {
                LogIt(string.Format("{0,-16} : {1}", k, args.Parameters[k]));
            }
        }
        public void setParameter(string key, string value)
        {
            if (commandLine.Parameters.ContainsKey(key))
                commandLine.Parameters[key] = value;
            else
                commandLine.Parameters.Add(key, value);
        }
        public void LogIt(string s)
        {            
            //System.Diagnostics.Trace.WriteLine("[twController]:"+s);
            if(commandLine.IsParameterTrue("verbose"))
                System.Console.Out.WriteLine(s);
            string fn = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("apsthome"), "twController.txt");
            if (System.IO.File.Exists(fn))
            {
                lock (this)
                {
                    System.IO.File.AppendAllText(fn, $"[{DateTime.Now.ToString("O")}]: {s}\n");
                    FileInfo fi = new FileInfo(fn);
                    if (fi.Length > 32 * 1024 * 1024)
                    {
                        System.IO.File.Move(fn, System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("apsthome"), $"twController_{DateTime.Now.ToString("yyyyMMddThhmmss")}.txt"));
                        using (File.Create(fn)) ;
                    }
                }
            }
        }
        public string[] readCalibrationIni(string label, string calibrationFile)
        {
            ArrayList ret = new ArrayList();
            StringBuilder sb = new StringBuilder(512);
            uint size = win32API.GetPrivateProfileString("label", label, "", sb, (uint)sb.Capacity, calibrationFile);
            if (size > 0)
            {
                bool found = false;
                foreach (string s in ret)
                {
                    if (string.Compare(s, sb.ToString(), true) == 0)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    ret.Add(sb.ToString());
            }
            size = win32API.GetPrivateProfileString("label_1.1", label, "", sb, (uint)sb.Capacity, calibrationFile);
            if (size > 0)
            {
                bool found = false;
                foreach (string s in ret)
                {
                    if (string.Compare(s, sb.ToString(), true) == 0)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    ret.Add(sb.ToString());
            }
            size = win32API.GetPrivateProfileString("label_2.0", label, "", sb, (uint)sb.Capacity, calibrationFile);
            if (size > 0)
            {
                bool found = false;
                foreach (string s in ret)
                {
                    if (string.Compare(s, sb.ToString(), true) == 0)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    ret.Add(sb.ToString());
            }
            return (string[])ret.ToArray(typeof(string));
        }
        public bool saveXml(XmlDocument doc, string fileToSave)
        {
            bool bSuccess = false;
            while (!bSuccess)
            {
                try
                {
                    if (doc != null && !string.IsNullOrEmpty(fileToSave))
                    {
                        doc.Save(fileToSave);
                        bSuccess = true;
                        break;
                    }


                }
                catch (System.Exception ex)
                {
                    LogIt("saveXml exception " + ex.ToString());
                    bSuccess = false;
                }

                if (!bSuccess)
                {
                    // need retry
                    System.Threading.Thread.Sleep(1000);
                }

            }
            return bSuccess;
        }
        public int getSupportedLabels()
        {
            int ret = 12;
            //StringBuilder sb = new StringBuilder(100);
            //win32API.GetPrivateProfileString("config", "labels", "24", sb, (uint)sb.Capacity, System.IO.Path.Combine(envClass.getInstance().ExePath, "tetherwing.ini"));
            //try
            //{
            //    if (Int32.TryParse(sb.ToString(), out ret))
            //    {
            //    }
            //}
            //catch (System.Exception ex)
            //{
            	
            //}
            string s = GetConfigValueByKey("config", "labels", "12");
            if (Int32.TryParse(s, out ret))
            {
            }
            return ret;
        }
        public int getLicensedPanels()
        {
            int ret = 0;
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString("amount", "labels", "0", sb, (uint)sb.Capacity, System.IO.Path.Combine(exePath, "calibration.ini"));
            string s = sb.ToString();
            //string s = GetConfigValueByKey("config", "labels", "12");
            if (Int32.TryParse(s, out ret))
            {
            }
            return ret;
        }
        public bool pauseDetection(int nPort)
        {
            bool bRet = false;
            string sEventName = string.Format("event_label_{0}", nPort.ToString());

            try
            {
                if (string.IsNullOrEmpty(sEventName) != true)
                {
                    IntPtr handle = win32API.OpenEvent((uint)(win32API.EVENT_ALL_ACCESS | win32API.EVENT_MODIFY_STATE), false, sEventName);
                    if (handle != IntPtr.Zero)
                    {
                        bRet = win32API.SetEvent(handle);
                        win32API.CloseHandle(handle);
                    }
                }
            }
            catch (Exception e)
            {
                bRet = false;
                Trace.WriteLine(e.Message);
            }
            return bRet;
        }
        public bool resumeDetection(int nPort)
        {
            bool bRet = false;
            string sEventName = string.Format("event_label_{0}", nPort.ToString());

            try
            {
                if (string.IsNullOrEmpty(sEventName) != true)
                {
                    IntPtr handle = win32API.OpenEvent((uint)(win32API.EVENT_ALL_ACCESS | win32API.EVENT_MODIFY_STATE), false, sEventName);
                    if (handle != IntPtr.Zero)
                    {
                        bRet = win32API.ResetEvent(handle);
                        win32API.CloseHandle(handle);
                    }
                    else
                    {
                        bRet = false;
                        LogIt(string.Format("Event = {0} is not exist", sEventName));
                    }
                }
            }
            catch (Exception e)
            {
                bRet = false;
                Trace.WriteLine(e.Message);
            }
            return bRet;
        }
        public string findLastestPrlFile(string prlFile)
        {
            string ret = prlFile;
            string prlPath = System.IO.Path.GetDirectoryName(prlFile);
            string prlVersion = System.IO.Path.GetFileNameWithoutExtension(prlFile);
            prlVersion = prlVersion.Substring(0, 2);
            string[] versions = System.IO.Directory.GetFiles(prlPath, string.Format("{0}*.prl", prlVersion));
            int maxVersion = 0;
            foreach (string v in versions)
            {
                string s = System.IO.Path.GetFileNameWithoutExtension(v);
                int i = Convert.ToInt32(s);
                if (i > maxVersion)
                {
                    maxVersion = i;
                }
            }
            ret = System.IO.Path.Combine(prlPath, string.Format("{0}.prl", maxVersion));
            return ret;
        }
        public int readHub(string hubname, int hubport, ref int vid, ref int pid)
        {
            int ret = win32API.NO_ERROR;
            vid = -1;
            pid = -1;
            string devicePath = @"\\?\" + hubname;
            IntPtr handel2 = IntPtr.Zero;
            handel2 = win32API.CreateFile(devicePath, win32API.GENERIC_WRITE, win32API.FILE_SHARE_WRITE, IntPtr.Zero, win32API.OPEN_EXISTING, 0, IntPtr.Zero);
            if (handel2.ToInt32() != win32API.INVALID_HANDLE_VALUE)
            {
                win32API.USB_NODE_INFORMATION NodeInfo = new win32API.USB_NODE_INFORMATION();
                NodeInfo.NodeType = win32API.USB_HUB_NODE.UsbHub;
                int nBytes = Marshal.SizeOf(NodeInfo);
                IntPtr ptrNodeInfo = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(NodeInfo, ptrNodeInfo, true);
                int nBytesReturned = -1;
                // Get the hub information.
                if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_NODE_INFORMATION, ptrNodeInfo, nBytes, ptrNodeInfo, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    NodeInfo = (win32API.USB_NODE_INFORMATION)Marshal.PtrToStructure(ptrNodeInfo, typeof(win32API.USB_NODE_INFORMATION));
                }
                Marshal.FreeHGlobal(ptrNodeInfo);

                if (hubport > 0 && hubport <= NodeInfo.HubInformation.HubDescriptor.bNumberOfPorts)
                {
                    nBytes = Marshal.SizeOf(typeof(win32API.USB_NODE_CONNECTION_INFORMATION_EX));
                    IntPtr ptrNodeConnection = Marshal.AllocHGlobal(nBytes);
                    win32API.USB_NODE_CONNECTION_INFORMATION_EX nodeConnection = new win32API.USB_NODE_CONNECTION_INFORMATION_EX();
                    nodeConnection.ConnectionIndex = hubport;
                    Marshal.StructureToPtr(nodeConnection, ptrNodeConnection, true);
                    if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, ptrNodeConnection, nBytes, ptrNodeConnection, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        nodeConnection = (win32API.USB_NODE_CONNECTION_INFORMATION_EX)Marshal.PtrToStructure(ptrNodeConnection, typeof(win32API.USB_NODE_CONNECTION_INFORMATION_EX));
                        if ((nodeConnection.ConnectionStatus == (win32API.USB_CONNECTION_STATUS)win32API.USB_CONNECTION_STATUS.DeviceConnected))
                        {
                            if (nodeConnection.DeviceDescriptor.bDeviceClass == win32API.UsbDeviceClass.HubDevice)
                            {
                                //LogIt(String.Format("There is a hub connect on {0}@{1}.", hubport, hubname));
                                vid = nodeConnection.DeviceDescriptor.idVendor;
                                pid = nodeConnection.DeviceDescriptor.idProduct;
                            }
                            else
                            {
                                vid = nodeConnection.DeviceDescriptor.idVendor;
                                pid = nodeConnection.DeviceDescriptor.idProduct;
                            }
                        }
                        else
                        {
                            //logIt(String.Format("No device connect on {0}@{1}.", _hubport, _hubname));
                        }
                    }
                    Marshal.FreeHGlobal(ptrNodeConnection);
                }
                win32API.CloseHandle(handel2);
            }
            else
                ret = Marshal.GetLastWin32Error();
            return ret;
        }

        public int readHubV2(string hubname, int hubport, ref int vid, ref int pid, ref string serialnumber, ref string driverKey, bool read_more)
        {
            int ret = -1;
            vid = -1;
            pid = -1;
            string devicePath = @"\\?\" + hubname;
            IntPtr handel2 = IntPtr.Zero;
            handel2 = win32API.CreateFile(devicePath, win32API.GENERIC_WRITE, win32API.FILE_SHARE_WRITE, IntPtr.Zero, win32API.OPEN_EXISTING, 0, IntPtr.Zero);
            if (handel2.ToInt32() != win32API.INVALID_HANDLE_VALUE)
            {
                win32API.USB_NODE_INFORMATION NodeInfo = new win32API.USB_NODE_INFORMATION();
                NodeInfo.NodeType = win32API.USB_HUB_NODE.UsbHub;
                int nBytes = Marshal.SizeOf(NodeInfo);
                IntPtr ptrNodeInfo = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(NodeInfo, ptrNodeInfo, true);
                int nBytesReturned = -1;
                // Get the hub information.
                if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_NODE_INFORMATION, ptrNodeInfo, nBytes, ptrNodeInfo, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    NodeInfo = (win32API.USB_NODE_INFORMATION)Marshal.PtrToStructure(ptrNodeInfo, typeof(win32API.USB_NODE_INFORMATION));
                }
                Marshal.FreeHGlobal(ptrNodeInfo);

                if (hubport > 0 && hubport <= NodeInfo.HubInformation.HubDescriptor.bNumberOfPorts)
                {
                    nBytes = Marshal.SizeOf(typeof(win32API.USB_NODE_CONNECTION_INFORMATION_EX));
                    IntPtr ptrNodeConnection = Marshal.AllocHGlobal(nBytes);
                    win32API.USB_NODE_CONNECTION_INFORMATION_EX nodeConnection = new win32API.USB_NODE_CONNECTION_INFORMATION_EX();
                    nodeConnection.ConnectionIndex = hubport;
                    Marshal.StructureToPtr(nodeConnection, ptrNodeConnection, true);
                    if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, ptrNodeConnection, nBytes, ptrNodeConnection, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        nodeConnection = (win32API.USB_NODE_CONNECTION_INFORMATION_EX)Marshal.PtrToStructure(ptrNodeConnection, typeof(win32API.USB_NODE_CONNECTION_INFORMATION_EX));
                        if ((nodeConnection.ConnectionStatus == (win32API.USB_CONNECTION_STATUS)win32API.USB_CONNECTION_STATUS.DeviceConnected))
                        {
                            if (nodeConnection.DeviceDescriptor.bDeviceClass == win32API.UsbDeviceClass.HubDevice)
                            {
                                LogIt(String.Format("hub device connect on {0}@{1}.", hubport, hubname));
                                nBytes = Marshal.SizeOf(typeof(win32API.USB_NODE_CONNECTION_NAME));
                                IntPtr ptrNodeConnectionName = Marshal.AllocHGlobal(nBytes);
                                win32API.USB_NODE_CONNECTION_NAME NodeConnectionName = new win32API.USB_NODE_CONNECTION_NAME();
                                NodeConnectionName.ConnectionIndex = hubport;
                                Marshal.StructureToPtr(NodeConnectionName, ptrNodeConnectionName, true);
                                if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_NODE_CONNECTION_NAME, ptrNodeConnectionName, nBytes, ptrNodeConnectionName, nBytes, out nBytesReturned, IntPtr.Zero))
                                {
                                    NodeConnectionName = (win32API.USB_NODE_CONNECTION_NAME)Marshal.PtrToStructure(ptrNodeConnectionName, typeof(win32API.USB_NODE_CONNECTION_NAME));
                                    devicePath = @"\\?\" + NodeConnectionName.NodeName;
                                    IntPtr handel3 = IntPtr.Zero;
                                    handel3 = win32API.CreateFile(devicePath, win32API.GENERIC_WRITE, win32API.FILE_SHARE_WRITE, IntPtr.Zero, win32API.OPEN_EXISTING, 0, IntPtr.Zero);
                                    if (handel3.ToInt32() != win32API.INVALID_HANDLE_VALUE)
                                    {
                                        win32API.USB_NODE_INFORMATION NodeInfo_child = new win32API.USB_NODE_INFORMATION();
                                        NodeInfo_child.NodeType = win32API.USB_HUB_NODE.UsbHub;
                                        nBytes = Marshal.SizeOf(NodeInfo_child);
                                        IntPtr ptrNodeInfo_child = Marshal.AllocHGlobal(nBytes);
                                        Marshal.StructureToPtr(NodeInfo_child, ptrNodeInfo_child, true);
                                        if (win32API.DeviceIoControl(handel3, win32API.IOCTL_USB_GET_NODE_INFORMATION, ptrNodeInfo_child, nBytes, ptrNodeInfo_child, nBytes, out nBytesReturned, IntPtr.Zero))
                                        {
                                            NodeInfo_child = (win32API.USB_NODE_INFORMATION)Marshal.PtrToStructure(ptrNodeInfo_child, typeof(win32API.USB_NODE_INFORMATION));
                                        }
                                        Marshal.FreeHGlobal(ptrNodeInfo_child);

                                        for (int nIndexPort = 1; nIndexPort <= NodeInfo_child.HubInformation.HubDescriptor.bNumberOfPorts; nIndexPort++)
                                        {
                                            nBytes = Marshal.SizeOf(typeof(win32API.USB_NODE_CONNECTION_INFORMATION_EX));
                                            IntPtr ptrNodeConnection_child = Marshal.AllocHGlobal(nBytes);
                                            win32API.USB_NODE_CONNECTION_INFORMATION_EX NodeConnection_child = new win32API.USB_NODE_CONNECTION_INFORMATION_EX();
                                            NodeConnection_child.ConnectionIndex = nIndexPort;
                                            Marshal.StructureToPtr(NodeConnection_child, ptrNodeConnection_child, true);
                                            if (win32API.DeviceIoControl(handel3, win32API.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, ptrNodeConnection_child, nBytes, ptrNodeConnection_child, nBytes, out nBytesReturned, IntPtr.Zero))
                                            {
                                                NodeConnection_child = (win32API.USB_NODE_CONNECTION_INFORMATION_EX)Marshal.PtrToStructure(ptrNodeConnection_child, typeof(win32API.USB_NODE_CONNECTION_INFORMATION_EX));
                                                if ((NodeConnection_child.ConnectionStatus == (win32API.USB_CONNECTION_STATUS)win32API.USB_CONNECTION_STATUS.DeviceConnected))
                                                {
                                                    if (NodeConnection_child.DeviceDescriptor.bDeviceClass == win32API.UsbDeviceClass.HubDevice)
                                                    {
                                                        //if child device also is a hub, stop detect
                                                        
                                                    }
                                                    else
                                                    {
                                                                                                               
                                                        vid = NodeConnection_child.DeviceDescriptor.idVendor;
                                                        pid = NodeConnection_child.DeviceDescriptor.idProduct;
                                                        if (vid !=0 && pid !=0)
                                                        {
                                                            ret = 0;
                                                        }
                                                        Trace.WriteLine(String.Format("There is a device (0x{0:X4}:0x{1:X4}) connect on {2}@{3}.", vid, pid, nIndexPort, NodeConnectionName.NodeName));
                                                        string manufacturer = "";
                                                        string product = "";
                                                        readUsbInformationFromHandle(handel3, NodeConnection_child, nIndexPort, ref driverKey, ref manufacturer, ref product, ref serialnumber);
                                                    }
                                                    Marshal.FreeHGlobal(ptrNodeConnection_child);
                                                    break;
                                                }
                                            }
                                            Marshal.FreeHGlobal(ptrNodeConnection_child);
                                        }
                                        win32API.CloseHandle(handel3);
                                    }
                                    else
                                        ret = Marshal.GetLastWin32Error();
                                }
                                Marshal.FreeHGlobal(ptrNodeConnectionName);
                            }
                            else
                            {
                                vid = nodeConnection.DeviceDescriptor.idVendor;
                                pid = nodeConnection.DeviceDescriptor.idProduct;
                                if (vid != 0 && pid != 0)
                                {
                                    ret = 0;
                                }
                                Trace.WriteLine(String.Format("There is a device (0x{0:X4}:0x{1:X4}) connect on {2}@{3}.", vid, pid, hubport, hubname));
                                string manufacturer = "";
                                string product = "";
                                if (read_more)
                                    readUsbInformationFromHandle(handel2, nodeConnection, hubport, ref driverKey, ref manufacturer, ref product, ref serialnumber);
                            }

                            
                        }
                        else
                        {
                            ret = 0;
                            vid = 1;
                            pid = 1;
                            Trace.WriteLine(String.Format("No device connect on {0}@{1}.", hubport, hubname));
                        }
                    }
                    Marshal.FreeHGlobal(ptrNodeConnection);
                }
                win32API.CloseHandle(handel2);
            }
            else
            {
                ret = Marshal.GetLastWin32Error();
                Trace.WriteLine(string.Format("CreateFile: error {0}", ret));

            }              
            return ret;
        }

        int readUsbInformationFromHandle(IntPtr handel2, win32API.USB_NODE_CONNECTION_INFORMATION_EX nodeConnection, int port, ref string driverKey, ref string manufacturer, ref string product, ref string serialNumber)
        {
            int ret = win32API.NO_ERROR;
            if (handel2.ToInt32() != win32API.INVALID_HANDLE_VALUE)
            {
                // Get the Driver Key Name (usefull in locating a device)
                win32API.USB_NODE_CONNECTION_DRIVERKEY_NAME DriverKey = new win32API.USB_NODE_CONNECTION_DRIVERKEY_NAME();
                DriverKey.ConnectionIndex = port;
                int nBytes = Marshal.SizeOf(DriverKey);
                int nBytesReturned = -1;
                IntPtr ptrDriverKey = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(DriverKey, ptrDriverKey, true);
                // Use an IOCTL call to request the Driver Key Name
                if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME, ptrDriverKey, nBytes, ptrDriverKey, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    DriverKey = (win32API.USB_NODE_CONNECTION_DRIVERKEY_NAME)Marshal.PtrToStructure(ptrDriverKey, typeof(win32API.USB_NODE_CONNECTION_DRIVERKEY_NAME));
                    driverKey = DriverKey.DriverKeyName;
                }
                Marshal.FreeHGlobal(ptrDriverKey);
                // get usb descriptor string
                // The iManufacturer, iProduct and iSerialNumber entries in the
                // device descriptor are really just indexes.  So, we have to 
                // request a string descriptor to get the values for those strings.
                string NullString = new string((char)0, win32API.MAX_BUFFER_SIZE / Marshal.SystemDefaultCharSize);
                if (nodeConnection.DeviceDescriptor != null && nodeConnection.DeviceDescriptor.iManufacturer > 0)
                {
                    // Build a request for string descriptor.
                    win32API.USB_DESCRIPTOR_REQUEST Request = new win32API.USB_DESCRIPTOR_REQUEST();
                    Request.ConnectionIndex = port;
                    Request.SetupPacket.wValue = (short)((win32API.USB_STRING_DESCRIPTOR_TYPE << 8) + nodeConnection.DeviceDescriptor.iManufacturer);
                    Request.SetupPacket.wLength = (short)(nBytes - Marshal.SizeOf(Request));
                    Request.SetupPacket.wIndex = 0x409; // The language code.

                    // Geez, I wish C# had a Marshal.MemSet() method.
                    IntPtr ptrRequest = Marshal.StringToHGlobalAuto(NullString);
                    Marshal.StructureToPtr(Request, ptrRequest, true);

                    // Use an IOCTL call to request the string descriptor.
                    if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, ptrRequest, nBytes, ptrRequest, nBytes, out nBytesReturned, IntPtr.Zero))
                    {

                        // The location of the string descriptor is immediately after
                        // the Request structure.  Because this location is not "covered"
                        // by the structure allocation, we're forced to zero out this
                        // chunk of memory by using the StringToHGlobalAuto() hack above
                        IntPtr ptrStringDesc = new IntPtr(ptrRequest.ToInt32() + Marshal.SizeOf(Request));
                        win32API.USB_STRING_DESCRIPTOR StringDesc = (win32API.USB_STRING_DESCRIPTOR)Marshal.PtrToStructure(ptrStringDesc, typeof(win32API.USB_STRING_DESCRIPTOR));
                        manufacturer = StringDesc.bString;
                    }
                    Marshal.FreeHGlobal(ptrRequest);
                }

                if (nodeConnection.DeviceDescriptor != null && nodeConnection.DeviceDescriptor.iSerialNumber > 0)
                {
                    // Build a request for string descriptor.
                    win32API.USB_DESCRIPTOR_REQUEST Request = new win32API.USB_DESCRIPTOR_REQUEST();
                    Request.ConnectionIndex = port;
                    Request.SetupPacket.wValue = (short)((win32API.USB_STRING_DESCRIPTOR_TYPE << 8) + nodeConnection.DeviceDescriptor.iSerialNumber);
                    Request.SetupPacket.wLength = (short)(nBytes - Marshal.SizeOf(Request));
                    Request.SetupPacket.wIndex = 0x409; // The language code.

                    // Geez, I wish C# had a Marshal.MemSet() method.
                    IntPtr ptrRequest = Marshal.StringToHGlobalAuto(NullString);
                    Marshal.StructureToPtr(Request, ptrRequest, true);

                    // Use an IOCTL call to request the string descriptor
                    if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, ptrRequest, nBytes, ptrRequest, nBytes, out nBytesReturned, IntPtr.Zero))
                    {

                        // The location of the string descriptor is immediately after the request structure.
                        IntPtr ptrStringDesc = new IntPtr(ptrRequest.ToInt32() + Marshal.SizeOf(Request));
                        win32API.USB_STRING_DESCRIPTOR StringDesc = (win32API.USB_STRING_DESCRIPTOR)Marshal.PtrToStructure(ptrStringDesc, typeof(win32API.USB_STRING_DESCRIPTOR));
                        serialNumber = StringDesc.bString;
                    }
                    Marshal.FreeHGlobal(ptrRequest);
                }

                if (nodeConnection.DeviceDescriptor != null && nodeConnection.DeviceDescriptor.iProduct > 0)
                {
                    // Build a request for endpoint descriptor.
                    win32API.USB_DESCRIPTOR_REQUEST Request = new win32API.USB_DESCRIPTOR_REQUEST();
                    Request.ConnectionIndex = port;
                    Request.SetupPacket.wValue = (short)((win32API.USB_STRING_DESCRIPTOR_TYPE << 8) + nodeConnection.DeviceDescriptor.iProduct);
                    Request.SetupPacket.wLength = (short)(nBytes - Marshal.SizeOf(Request));
                    Request.SetupPacket.wIndex = 0x409; // The language code.

                    // Geez, I wish C# had a Marshal.MemSet() method.
                    IntPtr ptrRequest = Marshal.StringToHGlobalAuto(NullString);
                    Marshal.StructureToPtr(Request, ptrRequest, true);

                    // Use an IOCTL call to request the string descriptor.
                    if (win32API.DeviceIoControl(handel2, win32API.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, ptrRequest, nBytes, ptrRequest, nBytes, out nBytesReturned, IntPtr.Zero))
                    {

                        // the location of the string descriptor is immediately after the Request structure
                        IntPtr ptrStringDesc = new IntPtr(ptrRequest.ToInt32() + Marshal.SizeOf(Request));
                        win32API.USB_STRING_DESCRIPTOR StringDesc = (win32API.USB_STRING_DESCRIPTOR)Marshal.PtrToStructure(ptrStringDesc, typeof(win32API.USB_STRING_DESCRIPTOR));
                        product = StringDesc.bString;
                    }
                    Marshal.FreeHGlobal(ptrRequest);
                }
            }
            return ret;
        }

        public string getOperationId()
        {
            string ret = string.Empty;
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString("login", "operator", "", sb, (uint)sb.Capacity, System.IO.Path.Combine(runtimePath, "config.ini"));
            ret = sb.ToString();
            return ret;
        }
        public string GetMACAddress()
        {
            string ret = string.Empty;

            System.Management.ManagementClass mc = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration");
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementObject mo in moc)
            {
                if (mo["MacAddress"] != null)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        ret = mo["MacAddress"].ToString();
                    }
                }
            }
            return ret;
        }
        public string getStoreId()
        {
            string ret = string.Empty;
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString("login", "store", "", sb, (uint)sb.Capacity, System.IO.Path.Combine(runtimePath, "config.ini"));
            ret = sb.ToString();
            return ret;
        }
        public string getStatusString(int status)
        {
            string ret = string.Empty;
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString("status", status.ToString(), status.ToString(), sb, (uint)sb.Capacity, System.IO.Path.Combine(exePath, "config.ini"));
            ret = sb.ToString();
            return ret;
        }
        public int getTaskDelay()
        {
            int ret = 30;
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString("config", "delay", "30", sb, (uint)sb.Capacity, System.IO.Path.Combine(exePath, "config.ini"));
            Int32.TryParse(sb.ToString(), out ret);
            return ret;
        }
        public string getUserName()
        {
            string ret = string.Empty;
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString("login", "username", "", sb, (uint)sb.Capacity, System.IO.Path.Combine(runtimePath, "config.ini"));
            ret = sb.ToString();
            return ret;
        }
        public string GetConfigValueByKey(string section, string key, string value, string configfile="")
        {
            string ret = string.Empty;
            StringBuilder sb = new StringBuilder(512);
            string config_filename = string.Empty;
            if (string.IsNullOrEmpty(configfile))
                config_filename = System.IO.Path.Combine(exePath, appName) + ".ini";
            else
                config_filename = System.IO.Path.Combine(exePath, configfile);
            win32API.GetPrivateProfileString(section, key, value, sb, (uint)sb.Capacity, config_filename);
            ret = sb.ToString();
            return ret;
        }
        public string getRmsDir()
        {
            string ret = "";
            if (!string.IsNullOrEmpty(exePath) && System.IO.Directory.Exists(exePath))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exePath), "RMS Web_Client");
            }
            return ret;
        }
        public string getProfilePoolFolder()
        {
            string ret = string.Empty;
            if (commandLine.Parameters.ContainsKey("profilepool"))
            {
                ret = commandLine.Parameters["profilepool"];
            }
            else
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exePath), "Profile Pool");
                //ret = System.IO.Path.Combine(exePath, "Profile Pool");
            }
            return ret;
        }
        public profileClass getProfileClass()
        {
            profileClass ret = null;
            if (commandLine.Parameters.ContainsKey("selected_profile"))
            {
                ret = profileClass.CreateFormProfileName(commandLine.Parameters["selected_profile"]);
            }
            else if (!string.IsNullOrEmpty(GetConfigValueByKey("config", "profile", string.Empty)))
            {
                ret = profileClass.CreateFormProfileName(GetConfigValueByKey("config", "profile", string.Empty));
            }
            return ret;
        }
        public bool uiOperating()
        {
            bool ret = false;
            if (commandLine.Parameters.ContainsKey("product") && string.Compare(commandLine.Parameters["product"], "mobileq", true) == 0)
            {
                if (commandLine.Parameters.ContainsKey("operate_started"))
                {
                    ret = string.Compare(commandLine.Parameters["operate_started"], bool.TrueString, true) == 0;
                }
            }
            else
                ret = true;
            return ret;
        }
        public string getParametersByKey(string key)
        {
            string ret = string.Empty;
            if (commandLine.Parameters.ContainsKey(key))
            {
                ret = commandLine.Parameters[key];
            }
            return ret;
        }
        public void prepareLog()
        {
            // logd will start from preparation.xml
            //try
            //{
            //    if (!commandLine.Parameters.ContainsKey("logdir"))
            //    {
            //        // create a temp log folder
            //        string logdir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fd_logs");
            //        commandLine.Parameters.Add("logdir", logdir);
            //    }
            //    string log_exe = System.IO.Path.Combine(exePath, "logd.exe");
            //    if (System.IO.File.Exists(log_exe))
            //    {
            //        // stop all dbgview
                    //System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcessesByName("dbgview");
                    //foreach (System.Diagnostics.Process p in procs)
                    //{
                    //    p.Kill();
                    //}
            //        procs = System.Diagnostics.Process.GetProcessesByName("logd");
            //        foreach (System.Diagnostics.Process p in procs)
            //        {
            //            p.Kill();
            //        }
            //        //System.Diagnostics.Process.Start(log_exe, string.Format("-ppid {0} -filepath \"{1}\" -filelimit 4", System.Diagnostics.Process.GetCurrentProcess().Id, commandLine.Parameters["logdir"]));
            //        StringBuilder sb = new StringBuilder(512);
            //        if (!commandLine.Parameters.ContainsKey("memfile"))
            //        {
            //            sb.Clear();
            //            win32API.GetPrivateProfileString("config", "memfile", "stslog", sb, (uint)sb.Capacity, System.IO.Path.Combine(exePath, "config.ini"));
            //            commandLine.Parameters.Add("memfile", sb.ToString());
            //        }
            //        if (!commandLine.Parameters.ContainsKey("filelimit"))
            //        {
            //            sb.Clear();
            //            win32API.GetPrivateProfileString("config", "filelimit", "4", sb, (uint)sb.Capacity, System.IO.Path.Combine(exePath, "config.ini"));
            //            commandLine.Parameters.Add("filelimit", sb.ToString());
            //        }
            //        System.Diagnostics.Process.Start(log_exe, string.Format("-ppid {0} -memfile \"{1}\" -filelimit {2}", System.Diagnostics.Process.GetCurrentProcess().Id,
            //            commandLine.Parameters["memfile"], commandLine.Parameters["filelimit"]));
            //    }
            //}
            //catch (System.Exception ex)
            //{
            //    LogIt(string.Format("Exception: during prepareLog: {0}", ex.Message));
            //}
        }
    }
}
