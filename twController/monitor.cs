using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace twController
{
    class monitor
    {
        public delegate void MonitorEventHandler(object sender, EventArgs e);
        public event MonitorEventHandler monitorEvent;
        public int onlyphbk = 0;
        string temp_kitting_data = string.Empty;
        public bool hasRMS = false;
        public bool hasKitting = false;
        public int g_vid = 70000;
        public int g_pid = 70000;
        public const int MAX_NUM = 70000;
        public int nOpenHubFail = 0;
        private Thread _monitorThread = null;
        private AutoResetEvent _quit = new AutoResetEvent(false);
        private int _label = 0;
        public int Label
        {
            get { return _label; }
        }
        int _time = 0;
        int _status;
        int _sub_status;
        public int Status
        {
            get { return _status; }
        }
        private string _filename = string.Empty;
        private string _detectionResult = "";
        private bool _detectionResultChanged = false;
        string _previous_device_id = string.Empty;
        string _previous_special_id = string.Empty;
        XmlNode _previous_device_node = null;
        string _previous_device_gone_time = string.Empty ;
        bool _previous_isSuccess = true;
        List<string> _mobileq_rms_report = null;

        public monitor(int label)
        {
            _label = label;
        }
        public void start()
        {
            _monitorThread = new Thread(new ThreadStart(startMonitor));
            _monitorThread.Name = string.Format("Monitor_{0}", _label);
            _monitorThread.IsBackground = true;
            _monitorThread.Start();
        }
        public void stop()
        {
            if (_monitorThread != null)
            {
                _quit.Set();
                _monitorThread.Join(1000);
            }
        }
        public void onDetection(string s)
        {
            int h1 = s.GetHashCode();
            int h2 = _detectionResult.GetHashCode();
            if (h1 != h2)
            {
                _detectionResult = s;
                _detectionResultChanged = true;
            }
        }
        private void LogIt(string s)
        {
            string ss = string.Format("[{0}]: {1}", System.Threading.Thread.CurrentThread.Name, s);
            envClass.getInstance().LogIt(ss);
        }
        //check credit
        private bool hasCheckcredit = false;
        private bool hasIgnoreRecord = false;
        private bool hasSpecialerror = false;
        private bool bReDedect = false;
        private bool bResume = false;
        private bool hasCheckEnv = false;
        //response click start button feature
        private bool hasclickStartbutton = false;
        private string strNextBXpath = string.Empty;
        private string strclickStartbutton = string.Empty;

        private bool hasprintbutton = false;
        private string strprintbutton = string.Empty;

        private bool bClickbutton = false;

        // Handle Go button
        private bool hastrigerAllLabelDetect = false;
        //Feature to Show Port Locked Instead of Previous Transaction 
        private bool hasLockPreviousTrasaction = false;

        private void startMonitor()
        {
            LogIt("Thread is started.");

            //CurrentUICulture needs to be set for every thread.
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                // prepare 
                {
                    _filename = System.IO.Path.Combine(envClass.getInstance().RuntimePath, "info", string.Format("label_{0}.xml", _label));
                    string[] hubs = envClass.getInstance().readCalibrationIni(_label.ToString(), System.IO.Path.Combine(envClass.getInstance().ExePath, "calibration.ini"));
                    XmlDocument dom = new XmlDocument();
                    XmlDeclaration xmlDeclaration;
                    //XmlNode dec = dom.CreateNode(XmlNodeType.XmlDeclaration, "", "");
                    xmlDeclaration = dom.CreateNode(XmlNodeType.XmlDeclaration, "", "") as XmlDeclaration;
                    xmlDeclaration.Encoding = "UTF-8";
                    dom.AppendChild(xmlDeclaration);
                    //dom.AppendChild(dec);
                    XmlElement labelinfoNode = dom.CreateElement("", "labelinfo", "");
                    dom.AppendChild(labelinfoNode);
                    XmlElement label = dom.CreateElement("", "label", "");
                    label.SetAttribute("id", _label.ToString());
                    labelinfoNode.AppendChild(label);
                    XmlElement device = dom.CreateElement("", "device", "");
                    labelinfoNode.AppendChild(device);
                    XmlElement runtime = dom.CreateElement("", "runtime", "");
                    runtime.SetAttribute("id", "1");
                    labelinfoNode.AppendChild(runtime);
                    foreach (string s in hubs)
                    {
                        string[] ss = s.Split(new char[] { '@' });
                        if (ss.Length == 2)
                        {
                            XmlElement hub = dom.CreateElement("", "usbhub", "");
                            hub.SetAttribute("name", ss[1]);
                            hub.SetAttribute("port", ss[0]);
                            label.AppendChild(hub);
                        }
                    }
                    //dom.Save(_filename);

                    envClass.getInstance().saveXml(dom, _filename);

                    string configPath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "config.ini");
                    IniFile iniObj = new IniFile(configPath);
                    string strcheck = iniObj.GetString("credit", "checkcredit","");
                    if (string.Compare(strcheck, "true", true) == 0)
                    {
                        hasCheckcredit = true;
                    }
                    string strRecord = iniObj.GetString("refurbishRecord", "IgnoreID", "false");
                    if (string.Compare(strRecord, "true", true) == 0)
                    {
                        hasIgnoreRecord = true;
                    }

                    strNextBXpath = iniObj.GetString("finalUI", "trigger_controller", "");
                    strclickStartbutton = iniObj.GetString("finalUI", "Click2start","");

                    if (string.Compare(strclickStartbutton, "true", true) == 0)
                    {
                        hasclickStartbutton = true;
                    }

                    strprintbutton = iniObj.GetString("finalUI", "Print2start", "");

                    if (string.Compare(strprintbutton, "true", true) == 0)
                    {
                        hasprintbutton = true;
                    }
                    if (hasclickStartbutton || hasprintbutton)
                    {
                        bClickbutton = true;
                    }
                    string strtrigerAllLabelDetect = iniObj.GetString("userinput", "triggerAllLabelDetect", "false");
                    if (string.Compare(strtrigerAllLabelDetect, "true", true) == 0)
                    {
                        hastrigerAllLabelDetect = true;
                    }
                    string strLockPreviousTran = iniObj.GetString("finalUI", "showLock4PreviousTransaction", "false");
                    if (string.Compare(strLockPreviousTran,"true",true)==0)
                    {
                        hasLockPreviousTrasaction = true;
                    }

                    string strCheckEnv = iniObj.GetString("environment", "stopOP", "false");
                    if (string.Compare(strCheckEnv,"true",true)==0)
                    {
                        hasCheckEnv = true;
                    }


                }
                loop();
            }
            catch (System.Exception ex)
            {
                LogIt("there is exception happen start monitor "+ex.ToString());
            }
           
            // terminate
            LogIt("Thread is going to terminated.");
        }
        bool bAvailableCredit()
        {
            bool bAvai = false;
            int ret = -1;
            try
            {
                string exePath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "checkcmcCredit.exe");
                if (File.Exists(exePath))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = exePath;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.Arguments = "-checkcredit";
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    p.Start();
                    p.WaitForExit();
                    ret = p.ExitCode;
                    if (ret == 0)
                    {
                        bAvai = true;
                    }

                }

            }
            catch (System.Exception ex)
            {

            }
            return bAvai;

        }

        bool bcorrectEnv()
        {
            bool bAvai = true;
            int ret = -1;
            try
            {
                string exePath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "envProtecter.exe");
                if (File.Exists(exePath))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = exePath;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.Arguments = "-check";
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    p.Start();
                    p.WaitForExit();
                    ret = p.ExitCode;
                    if (ret == 0)
                    {
                        bAvai = true;
                    }
                    else
                    {
                        bAvai = false;
                    }

                }
               

            }
            catch (System.Exception ex)
            {

            }
            return bAvai;

        }
        
        private void loop()
        {
            
            if (hastrigerAllLabelDetect)
            {
                _status = 10000;
            }
            else
            {
                _status = 1;
            }
            uiSameDeviceWarning _uiSameDeviceWarning = null;
            while (!_quit.WaitOne(1000))
            {
                try
                {
                    switch (_status)
                    {
                        case 9999:
                            // Chris: temp fix
                            if (this._status == 46)
                                _status = 46;
                            break;
                        case 0: // 3.14 failure
                            _status = handleAfterTaskDone_2();
                            break;
                        case 1: // 3.7.1 idle
                            // Chris: 
                            LogIt("status 1 begin");
                            if (hasCheckcredit)
                            {

                                if (bReDedect)
                                {
                                    envClass.getInstance().resumeDetection(_label);
                                    bReDedect = false;
                                }
                                else
                                {
                                    LogIt(string.Format("can't resume detection credit run out {0}", _label));
                                }
                            }
                            else if (hasCheckEnv)
                            {
                                if (bResume)
                                {
                                    if (envClass.getInstance().resumeDetection(_label))
                                    {
                                        bResume = false;
                                    }
                                }
                            }
                            else
                            {
                                envClass.getInstance().resumeDetection(_label);

                            }

                            _sub_status = 0;
                            _status = checkDevice();

                            LogIt(string.Format("status 1 end: {0} ", _status));

                            if (_uiSameDeviceWarning != null)
                            {
                                _uiSameDeviceWarning.closeDialog();
                                _uiSameDeviceWarning = null;
                            }
                            break;
                        case 2: // 3.7.2 not detect
                            _status = checkDevice();
                            if (_uiSameDeviceWarning != null)
                            {
                                _uiSameDeviceWarning.closeDialog();
                                _uiSameDeviceWarning = null;
                            }
                            break;
                        case 3: // 3.7.3 mis-detect
                            if (hastrigerAllLabelDetect)
                            {
                                envClass.getInstance().pauseDetection(_label);
                                string inipath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "fdEnableDetect.ini");
           
                                IniFile iniobj = new IniFile(inipath);
                                iniobj.WriteValue("detecting", string.Format("Label_{0}", _label), "false");
                                updateRuntimeCurrentStatus(3);
                                _status = 10000;
                                
                            }
                            else
                            {
                                _status = checkDevice();
                            }
                            
                            if (_uiSameDeviceWarning != null)
                            {
                                _uiSameDeviceWarning.closeDialog();
                                _uiSameDeviceWarning = null;
                            }
                            break;
                        case 4: // 3.7.5 detected
                            if (_detectionResultChanged)
                            {
                                _status = checkDevice();
                            }
                            else
                            {
                                // if click2start feature is on, don't need to compare current device id to previous device id
                                if (bClickbutton)
                                {
                                    try
                                    {
                                        lock (_filename)
                                        {
                                            XmlDocument dom = new XmlDocument();
                                            dom.Load(_filename);
                                            XmlNode deviceNode = dom.SelectSingleNode("/labelinfo/device");
                                            XmlNode mslNode = dom.CreateNode(XmlNodeType.Element, "msl", "");
                                            mslNode.InnerText = "NO MSL";
                                            deviceNode.AppendChild(mslNode);
                                            XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime");
                                            n1.RemoveAll();
                                            XmlElement e1 = (XmlElement)n1;
                                            e1.SetAttribute("id", envClass.getInstance().getStatusString(_status));
                                            envClass.getInstance().saveXml(dom, _filename);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                    _status = 5;
                                }
                                else
                                {
                                    string s = getDeviceId();
                                    string specialid = getSpecialId();
                                    LogIt(string.Format("{0} vs {1}", s, _previous_device_id));
                                    LogIt(string.Format("current special_id {0} vs previous={1}", specialid, _previous_special_id));
                                    if (!string.IsNullOrEmpty(_previous_device_id) && string.Compare(s, _previous_device_id, true) == 0||
                                        (!string.IsNullOrEmpty(_previous_special_id) && string.Compare(specialid,_previous_special_id,true)==0))
                                    {


                                        recoveryPreviousDeviceNode();
                                        //if ((!string.IsNullOrEmpty(_previous_special_id) && string.Compare(specialid, _previous_special_id, true) == 0))
                                        //{
                                            
                                        //}

                                        if (_previous_isSuccess)
                                        {
                                            _status = 9;
                                        }
                                        else
                                        {
                                            if (hasLockPreviousTrasaction)
                                            {
                                                _status = 90; // Feature to Show Port Locked Instead of Previous Transaction 
                                            }
                                            else
                                            {
                                                _status = 0;
                                            }
                                        }



                                        if (ctrlClass.hasVerify)
                                        {
                                            updateRuntimeCurrentStatus(289);
                                        }
                                        else
                                        {
                                            updateRuntimeCurrentStatus(_status);

                                        }
                                        //Steven: Must pause detection and reset g_vid g_pid
                                        envClass.getInstance().pauseDetection(_label);
                                        
                                        g_vid = MAX_NUM;
                                        g_pid = MAX_NUM;
                                        if (hastrigerAllLabelDetect)
                                        {
                                        }
                                        else
                                        {
                                            this._status = 0;//very important,"_status" need optimize!!!

                                        }
                                        

                                        lock (_filename)
                                        {
                                            XmlDocument doc = new XmlDocument();
                                            doc.Load(_filename);
                                            cleanXMLnode(doc);
                                        }
                                    }
                                    else
                                    {
                                        // if skip msl look up
                                        try
                                        {
                                            lock (_filename)
                                            {
                                                XmlDocument dom = new XmlDocument();
                                                dom.Load(_filename);
                                                XmlNode deviceNode = dom.SelectSingleNode("/labelinfo/device");
                                                XmlNode mslNode = dom.CreateNode(XmlNodeType.Element, "msl", "");
                                                mslNode.InnerText = "NO MSL";
                                                deviceNode.AppendChild(mslNode);
                                                XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime");
                                                n1.RemoveAll();
                                                XmlElement e1 = (XmlElement)n1;
                                                e1.SetAttribute("id", envClass.getInstance().getStatusString(_status));
                                                envClass.getInstance().saveXml(dom, _filename);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        if (skipLookupMsl())
                                        {
                                            _status = 5;
                                        }
                                        else
                                            _status = lookupMSL();
                                    }

                                }
                                
                            }
                            if (hastrigerAllLabelDetect)
                            {
                                string inipath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "fdEnableDetect.ini");
                                IniFile iniobj = new IniFile(inipath);
                                iniobj.WriteValue("detecting", string.Format("Label_{0}", _label), "false");

                            }
                            break;
                        case 5: // 3.8 has MSL
                            if (_detectionResultChanged)
                            {
                                _status = checkDevice();
                            }
                            else
                            {
                                // has msl do pst
                                //_status = doPstTask(null);
                                string s = envClass.getInstance().GetConfigValueByKey("userinput", "cStatus", string.Empty, "config.ini");
                                string strbeforetask = envClass.getInstance().GetConfigValueByKey("userinput", "beforetask", "false", "config.ini");
                                if (string.Compare(strbeforetask,"true",true)==0 && checkSkipstatus()==1)
                                {
                                    if (handleUserInput(5))
                                    {
                                        _status = doTask();
                                    }
                                    else
                                    {
                                        int i;
                                        if (Int32.TryParse(s, out i))
                                            _status = i;
                                        else
                                            _status = 0;
                                        updateRuntimeCurrentStatus(_status);

                                    }
                                    
                                }
                                else
                                {
                                    _status = doTask();
                                }
                            }
                            break;
                        case 6: // 3.9 not has MSL
                            if (_detectionResultChanged)
                            {
                                _status = checkDevice();
                            }
                            else
                                _status = lookupMSLLocal();
                            break;
                        case 7: // 3.10 connect and ready
                            break;
                        case 8: // 3.11 operation in progress
                            break;
                        case 9: // 3.12 success
                        case 108:
                        case 109:
                        case 110:
                            _status = handleAfterTaskDone_2();
                            break;
                        case 111:
                            _status = handleAfterTaskDone_2();
                            break;
                        case 112:
                            _status = handleAfterTaskDone_2();
                            break;
                        case 10: // 3.7.1 idle
                            _sub_status = 0;
                            _status = checkDevice();
                            if (_uiSameDeviceWarning != null)
                            {
                                _uiSameDeviceWarning.closeDialog();
                                _uiSameDeviceWarning = null;
                            }
                            break;
                        case 12: // wait for user input before task
                            if (_detectionResultChanged)
                            {
                                _status = checkDevice();
                            }
                            else
                            {
                                // check user input done?
                                if (handleUserInput(_status))
                                {
                                    _status = 5;
                                    updateRuntimeCurrentStatus(_status);
                                }
                            }
                            break;

                        case 46:
                            //click to start feature when task was done
                            if (bClickbutton)
                            {
                                LogIt(string.Format("handle click next feature begin {0} ", _status));
                                try
                                {
                                    lock (_filename)
                                    {
                                        //1,remove device node value
                                        XmlDocument dom = new XmlDocument();
                                        dom.Load(_filename);
                                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/device");
                                        n1.RemoveAll();
                                        //2,set label.xml runtime status 1;
                                        XmlNode runtimeNode = dom.SelectSingleNode("/labelinfo/runtime");
                                        runtimeNode.Attributes["id"].Value = "1";
                                        envClass.getInstance().saveXml(dom, _filename);

                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    LogIt("handle state machine status 46 exception " + ex.ToString());
                                }
                                //3,set state machine status 1
                                _status = 1;
                                LogIt(string.Format("handle click next feature End {0} ", _status));
                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (System.Exception ex)
                {
                    LogIt(string.Format("loop: exception: {0}", ex.Message));
                }
            }
            // finally
            if (_uiSameDeviceWarning != null)
            {
                _uiSameDeviceWarning.closeDialog();
                _uiSameDeviceWarning = null;
            }
        }

       
        int _pretime = 0;
               
        int checkSkipstatus()
        {
            int ret = 1;
            try
            {
                string home = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string exePath = Path.Combine(home, "userinputControl.exe");
                string config = Path.Combine(home, "config.ini");
                IniFile iniobj = new IniFile(config);
                string specialShowItems = iniobj.GetString("userinput", "specialShowItems", "");
                if (string.Compare(specialShowItems, "true", true) == 0)
                {
                    string parm = string.Format("-label={0} -checkstatus={1}", _label, true);
                    if (File.Exists(exePath))
                    {
                        ret = run_app_sync(exePath, parm);
                    }

                }

            }
            catch (System.Exception ex)
            {

            }
            return ret;

        }
        int run_app_sync(string appPath, string parameter, int iTimeOut = 5*60*1000)
        {
            int iRet = -1;
            try
            {
                LogIt(string.Format("will run: {0} {1}", appPath, parameter));
                if (File.Exists(appPath))
                {
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = appPath;
                    proc.StartInfo.Arguments = parameter;
                    proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    LogIt(string.Format("exe ID is - [{0}]", proc.Id));
                    System.Console.WriteLine(proc.StandardOutput.ReadToEnd());
                    proc.WaitForExit(iTimeOut);
                    iRet = proc.ExitCode;
                    LogIt(string.Format("exe return - [{0}]", iRet));
                }
                else
                    LogIt("exe is not exist");
            }
            catch (System.Exception ex)
            {
                iRet = -1;
                LogIt(string.Format("Run App exception - {0}", ex.ToString()));
            }
            LogIt(string.Format("will run: {0} exit: {1}", appPath, iRet));
            return iRet;
        }
        bool skipLookupMsl()
        {
            bool ret = false;
            string handsetId = getHandsetId();
            try
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(System.IO.Path.Combine(envClass.getInstance().ExePath, "icss.xml"));
                if (dom.DocumentElement != null && !string.IsNullOrEmpty(handsetId))
                {
                    XmlNode handsetNode = dom.SelectSingleNode(string.Format("//handset[@id='{0}']", handsetId));
                    if (handsetNode != null)
                    {
                        string dll_type = envClass.getInstance().GetConfigValueByKey("config", "dlltype", Program.dllType);
                        if (string.Compare(dll_type, "CSS", true) != 0)
                        {
                            XmlNode dllNode = handsetNode.SelectSingleNode(string.Format("DLL[@type='{0}']", envClass.getInstance().GetConfigValueByKey("config", "dlltype", Program.dllType)));
                            if (dllNode != null)
                            {
                                string v = dllNode.SelectSingleNode("checkMSL") == null ? "true" : dllNode.SelectSingleNode("checkMSL").InnerText;
                                if (string.Compare(v, "false", true) == 0)
                                {
                                    ret = true;
                                }
                            }
                        }
                        else
                            ret = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ret = false;
            }
            return ret;
        }
        private int checkDevice()
        {
            int status = 1;
            // for mobile, select profile first
            if (envClass.getInstance().uiOperating())
            {
                if (_detectionResultChanged && !string.IsNullOrEmpty(_detectionResult))
                {
                    try
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.LoadXml(_detectionResult);
                        if (dom.DocumentElement != null)
                        {
                            XmlNode device = dom.SelectSingleNode("/label/device");
                            if (device != null)
                            {
                                if (device.HasChildNodes)
                                {
                                    status = updateDetectedDevice(device);
                                }
                                else
                                    status = updateDetectedDevice(null);
                            }
                            else
                                status = updateDetectedDevice(null);
                        }
                        else
                            status = updateDetectedDevice(null);
                        _detectionResultChanged = false;
                    }
                    catch (System.Exception ex)
                    {
                        LogIt(string.Format("checkDevice: exception: {0}", ex.Message));
                    }
                }
            }
            else
                _previous_device_id = string.Empty;
            return status;
        }
        bool checkDeviceOnPort(XmlDocument dom)
        {
            bool ret = true;
            string blacklist_filename = System.IO.Path.Combine(envClass.getInstance().ExePath, "stsPortLimitation.XML");
            if (System.IO.File.Exists(blacklist_filename))
            {
                try
                {
                    XmlDocument bl = new XmlDocument();
                    bl.Load(blacklist_filename);
                    if (bl.DocumentElement != null)
                    {
                        bool found = false;
                        // read label id
                        XmlNode label_info_label = dom.SelectSingleNode("/labelinfo/label");
                        XmlNode label_info_device = dom.SelectSingleNode("/labelinfo/device");
                        XmlNode label_info_runtime = dom.SelectSingleNode("/labelinfo/runtime");
                        string label_id = string.Empty;
                        string device_id = string.Empty;
                        if (label_info_label != null && ((XmlElement)label_info_label).HasAttribute("id"))
                        {
                            label_id = label_info_label.Attributes["id"].Value;
                        }
                        if (label_info_device != null && ((XmlElement)label_info_device).HasAttribute("id"))
                        {
                            device_id = label_info_device.Attributes["id"].Value;
                        }
                        if (!string.IsNullOrEmpty(label_id) && !string.IsNullOrEmpty(device_id))
                        {
                            XmlNode bl_label = bl.SelectSingleNode(string.Format("/ports/port[@label=\'{0}\']", label_id));
                            if (bl_label != null)
                            {
                                XmlNodeList h = bl_label.SelectNodes(string.Format("id[.=\'{0}\']", device_id));
                                if (h.Count > 0)
                                {
                                    found = true;
                                }
                            }
                        }
                        if (found)
                        {
                            ret = false;
                            XmlNode bl_message = bl.SelectSingleNode("/ports/message");
                            string msg = (bl_message == null) ? "Please connect to right port" : bl_message.InnerText;
                            XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/message");
                            if (n1 == null)
                            {
                                XmlElement e = dom.CreateElement("message");
                                e.InnerText = msg;
                                XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                                rtNode.AppendChild(e);
                            }
                            else
                                n1.InnerText = msg;
                        }
                    }
                }
                catch (System.Exception ex)
                {

                }
            }
            else
            {
                LogIt(string.Format("The blacklist file doesn't exist, {0}", blacklist_filename));
            }
            return ret;
        }
        string[] getDeviceIDFromProfile()
        {
            List<string> ret = new List<string>();
            profileClass pc = envClass.getInstance().getProfileClass();
            if (pc != null)
            {
                string id = pc.getPropertyByName("deviceid");
                if (!string.IsNullOrEmpty(id))
                {
                    ret.Add(id);
                }
            }
            return ret.ToArray();
        }
        bool getPhoneInfoFromIcss(XmlDocument dom, string icssXml)
        {
            bool ret = false;
            try
            {
                if (dom != null && dom.DocumentElement != null && System.IO.File.Exists(icssXml))
                {
                    XmlNode deviceNode = dom.SelectSingleNode("/labelinfo/device");
                    string phoneID = (deviceNode.Attributes["id"] != null) ? deviceNode.Attributes["id"].Value : "";
                    string serialnumber = (deviceNode.SelectSingleNode("deviceid") == null) ? string.Empty : deviceNode.SelectSingleNode("deviceid").InnerText;
                    // TO-DO: Chris add to support profile
                    // check if the device is in the profile
                    string[] deviceInProfile = getDeviceIDFromProfile();
                    bool found = true; // set to false, no phone detected if no profile select, set to true, all phone can be detected if no profile select.
                    if (deviceInProfile.Length > 0)
                    {
                        found = false;
                        foreach (string s in deviceInProfile)
                        {
                            if (string.Compare(s, phoneID, true) == 0)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found && !string.IsNullOrEmpty(phoneID) && !string.IsNullOrEmpty(serialnumber))
                    {
                        XmlDocument icssDom = new XmlDocument();
                        icssDom.Load(icssXml);
                        XmlNode handsetNode = icssDom.SelectSingleNode(string.Format("//handset[@id='{0}']", phoneID));
                        if (handsetNode != null)
                        {
                            ret = true;
                            // retrieve 4 nodes, <maker> <phoneDesp>, <carrier>, <firmwareFile>, <prlFile>
                            // <manufacturer name=""> to <maker>
                            {
                                XmlNode n = handsetNode.ParentNode;
                                if (n != null)
                                {
                                    string s = n.Attributes["name"].Value;
                                    XmlElement e = dom.CreateElement("maker");
                                    e.InnerText = s;
                                    deviceNode.AppendChild(e);
                                }
                            }
                            // <phoneDesp> to <model>
                            {
                                XmlNode n = handsetNode.SelectSingleNode("phoneDesp");
                                if (n != null)
                                {
                                    string s = n.InnerText;
                                    XmlElement e = dom.CreateElement("model");
                                    e.InnerText = s;
                                    deviceNode.AppendChild(e);
                                }
                            }
                            // <carrier> to <carrier>
                            {
                                XmlNode n = handsetNode.SelectSingleNode("carrier");
                                if (n != null)
                                {
                                    string s = n.InnerText;
                                    XmlElement e = dom.CreateElement("carrier");
                                    e.InnerText = s;
                                    deviceNode.AppendChild(e);
                                }
                            }
                            // <firmwareFile> to <firmwareFile>
                            {
                                XmlNode n = handsetNode.SelectSingleNode("firmwareFile");
                                if (n != null)
                                {
                                    string s = n.InnerText;
                                    XmlElement e = dom.CreateElement("firmwareFile");
                                    e.InnerText = System.IO.Path.GetFileName(s);
                                    deviceNode.AppendChild(e);
                                }
                            }
                            // <prlFile> to <prlFile>
                            {
                                XmlNode n = handsetNode.SelectSingleNode("prlFile");
                                if (n != null)
                                {
                                    string s = n.InnerText;
                                    XmlElement e = dom.CreateElement("prlFile");
                                    e.InnerText = System.IO.Path.GetFileName(s);
                                    deviceNode.AppendChild(e);
                                }
                            }
                            // <DLL><name> to <dllname>
                            {
                                XmlNode n = handsetNode.SelectSingleNode(string.Format("DLL[@type='{0}']/name", envClass.getInstance().GetConfigValueByKey("config", "dlltype", Program.dllType)));
                                if (n != null)
                                {
                                    string s = n.InnerText;
                                    XmlElement e = dom.CreateElement("dllname");
                                    e.InnerText = System.IO.Path.GetFileName(s);
                                    deviceNode.AppendChild(e);
                                }
                            }
                            // <DLL><profile> to <profile>
                            {
                                XmlNode n = handsetNode.SelectSingleNode(string.Format("DLL[@type='{0}']/profile", envClass.getInstance().GetConfigValueByKey("config", "dlltype", Program.dllType)));
                                if (n != null)
                                {
                                    string s = n.InnerText;
                                    XmlElement e = dom.CreateElement("profile");
                                    e.InnerText = System.IO.Path.GetFileName(s);
                                    deviceNode.AppendChild(e);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {

            }
            return ret;
        }
        bool check_deviceid_for_msl_lookup(string device_id)
        {
            bool ret = false;
            // Chris: due to Sprint service only 14-byte length MEID HEX (must start with A0) and 8-byte length ESN HEX is accepted.
            if (!string.IsNullOrEmpty(device_id))
            {
                if (device_id.Length == 14)
                {
                    if ((device_id[0] >= 'a' && device_id[0] <= 'f') ||
                        (device_id[0] >= 'A' && device_id[0] <= 'F') ||
                        device_id.StartsWith("99") ||
                        device_id.StartsWith("98") ||
                        device_id.StartsWith("97") ||
                        device_id.StartsWith("96"))
                    {
                        ret = true;
                    }
                }
                else if (device_id.Length == 8)
                {
                    ret = true;
                }
            }
            if (!ret)
            {
                LogIt(string.Format("check_deviceid_for_msl_lookup: {0} return {1}", device_id, ret));
            }
            return ret;
        }
        int lookupMSL()
        {
            int _status = 1;
            string exePath = envClass.getInstance().ExePath;
            LogIt(string.Format("lookupMSL: {0}", exePath));
            string deviceid = string.Empty;
            string msl = string.Empty;
            // 0. retrieve the deviceid from xml
            try
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(_filename);
                XmlNode n = dom.SelectSingleNode("/labelinfo/device/meid_imei");//meid_imei
                if (n==null)
                {
                    n = dom.SelectSingleNode("/labelinfo/device/deviceid");
                }      
                deviceid = (n == null) ? string.Empty : n.InnerText;
                LogIt(string.Format("lookupMSL: {0}", deviceid));
                {
                    // 1. look for the msl_backup.ini file
                    if (!string.IsNullOrEmpty(deviceid) && string.IsNullOrEmpty(msl))
                    {
                        string msl_backup = System.IO.Path.Combine(exePath, "msl_backup.ini");
                        if (System.IO.File.Exists(msl_backup))
                        {
                            StringBuilder sb = new StringBuilder(512);
                            uint size = win32API.GetPrivateProfileString("msl", deviceid, "", sb, (uint)sb.Capacity, msl_backup);
                            if (size > 0)
                            {
                                msl = sb.ToString();
                            }
                        }
                    }
                    // 1.5 use new fdMslLookup.exe
                    if (check_deviceid_for_msl_lookup(deviceid) && string.IsNullOrEmpty(msl))
                    {
                        string mslAgent = System.IO.Path.Combine(exePath, "fdMslLookup.exe");
                        if (System.IO.File.Exists(mslAgent))
                        {
                            string url = envClass.getInstance().GetConfigValueByKey("msl", "server1", "");
                            if (!string.IsNullOrEmpty(url))
                            {
                                List<string> ret = new List<string>();
                                System.Diagnostics.Process p = new System.Diagnostics.Process();
                                p.StartInfo.FileName = mslAgent;
                                /*p.StartInfo.Arguments = string.Format("-url={0} -esn={1}", @"http://10.1.1.47/msl/querymsl/", deviceid);*/
                                p.StartInfo.Arguments = string.Format("-url={0} -esn={1}", url, deviceid);
                                p.StartInfo.WorkingDirectory = System.IO.Path.GetTempPath();
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                p.StartInfo.UseShellExecute = false;
                                p.StartInfo.RedirectStandardOutput = true;
                                p.Start();
                                while (true)
                                {
                                    string s = p.StandardOutput.ReadLine();
                                    if (s != null)
                                    {
                                        ret.Add(s);
                                    }
                                    else
                                        break;
                                }
                                p.WaitForExit();
                                if (ret.Count > 0)
                                {
                                    msl = ret[0];
                                }
                            }
                        }
                    }
                    // 2. look up msl from Sprint server
                    if (check_deviceid_for_msl_lookup(deviceid) && string.IsNullOrEmpty(msl))
                    {
                        // TODO: add code to look up msl online.
                        string mslAgent = System.IO.Path.Combine(exePath, "MSLAgent.exe");
                        if (System.IO.File.Exists(mslAgent))
                        {
                            System.Diagnostics.Process p = new System.Diagnostics.Process();
                            p.StartInfo.FileName = mslAgent;
                            p.StartInfo.Arguments = deviceid.ToUpper();
                            p.StartInfo.WorkingDirectory = System.IO.Path.GetTempPath();
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            p.Start();
                            p.WaitForExit();
                            string mslFile = System.IO.Path.Combine(exePath, string.Format("{0}_msl.xml", deviceid));
                            if (System.IO.File.Exists(mslFile))
                            {
                                try
                                {
                                    XmlDocument dm = new XmlDocument();
                                    dm.Load(mslFile);
                                    XmlNode mslNode = dm.SelectSingleNode("/sprint/msl");
                                    if (mslNode != null)
                                    {
                                        msl = mslNode.InnerText;
                                    }
                                    dm = null;
                                }
                                catch (System.Exception ex)
                                {

                                }
                                System.IO.File.Delete(mslFile);
                            }
                        }
                    }
                    // 3. final write msl to label_n.xml
                    if (!string.IsNullOrEmpty(msl))
                    {
                        lock (_filename)
                        {
                            XmlNode deviceNode = dom.SelectSingleNode("/labelinfo/device");
                            XmlNode mslNode = deviceNode.SelectSingleNode("./msl");
                            if (mslNode == null)
                            {
                                mslNode = dom.CreateNode(XmlNodeType.Element, "msl", "");
                                mslNode.InnerText = msl;
                                deviceNode.AppendChild(mslNode);
                            }
                            else
                                mslNode.InnerText = msl;
                            _status = 5;
                            XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime");
                            n1.Attributes["id"].Value = envClass.getInstance().getStatusString(_status);//_status.ToString();
                            envClass.getInstance().saveXml(dom, _filename);
                        }
                    }
                    else
                    {
                        lock (_filename)
                        {
                            _status = 5;
                            XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime");
                            n1.Attributes["id"].Value = envClass.getInstance().getStatusString(_status);
                            envClass.getInstance().saveXml(dom, _filename);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogIt(string.Format("lookupMSL: exception: {0}", ex.Message));
            }
            LogIt(string.Format("lookupMSL: return {0}", msl));
            return _status;
        }
        int lookupMSLLocal()
        {
            string deviceid = string.Empty;
            int status = 6;
            try
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(_filename);
                XmlNode n = dom.SelectSingleNode("/labelinfo/device/deviceid");
                deviceid = (n == null) ? string.Empty : n.InnerText;
                // 1. look for the msl_backup.ini file
                if (!string.IsNullOrEmpty(deviceid))
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(envClass.getInstance().RuntimePath, "config.ini")))
                    {
                        StringBuilder sb = new StringBuilder(512);
                        uint size = win32API.GetPrivateProfileString("msl", _label.ToString(), "", sb, (uint)sb.Capacity, System.IO.Path.Combine(envClass.getInstance().RuntimePath, "config.ini"));
                        if (size > 0)
                        {
                            string msl = sb.ToString();
                            if (!string.IsNullOrEmpty(msl))
                            {
                                // clean msl 
                                win32API.WritePrivateProfileString("msl", _label.ToString(), "", System.IO.Path.Combine(envClass.getInstance().RuntimePath, "config.ini"));
                                // save msl in xml
                                lock (_filename)
                                {
                                    XmlNode deviceNode = dom.SelectSingleNode("/labelinfo/device");
                                    XmlNode mslNode = dom.CreateNode(XmlNodeType.Element, "msl", "");
                                    mslNode.InnerText = msl;
                                    deviceNode.AppendChild(mslNode);
                                    status = 5;
                                    XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime");
                                    n1.Attributes["id"].Value = envClass.getInstance().getStatusString(_status);
                                    envClass.getInstance().saveXml(dom, _filename);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogIt(string.Format("lookupMSLLocal: exception: {0}", ex.Message));
            }
            return status;
        }
        System.Threading.Semaphore getSemaphoreForTask(XmlNode handsetNode)
        {
            System.Threading.Semaphore ret = null;
            try
            {
                XmlNode singleNode = handsetNode.SelectSingleNode("single");
                if (singleNode != null)
                {
                    string v = singleNode.InnerText;
                    if (string.Compare(v, "true", true) == 0)
                    {
                        int limit = 1;
                        // single
                        XmlElement e = (XmlElement)singleNode;
                        if (e.HasAttribute("id"))
                        {
                            v = e.Attributes["id"].Value;
                        }
                        else
                        {
                            v = "default";
                        }
                        if (e.HasAttribute("max"))
                        {
                            int i;
                            if (Int32.TryParse(e.Attributes["max"].Value, out i))
                            {
                                limit = i;
                            }
                            else
                                limit = 1;
                        }
                        ret = ctrlClass.getInstance().getSemaphoreByKey(v, limit);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ret = null;
            }
            return ret;
        }
        System.Threading.Mutex getMutexForTask(XmlNode handsetNode)
        {
            // get single element
            System.Threading.Mutex mutex = null;
            try
            {
                XmlNode singleNode = handsetNode.SelectSingleNode("single");
                if (singleNode != null)
                {
                    string v = singleNode.InnerText;
                    if (string.Compare(v, "true", true) == 0)
                    {
                        // single
                        XmlElement e = (XmlElement)singleNode;
                        if (e.HasAttribute("id"))
                            v = e.Attributes["id"].Value;
                        else
                            v = "default";
                        mutex = ctrlClass.getInstance().getMutexByKey(v);
                    }
                }
            }
            catch (System.Exception ex)
            {
                mutex = null;
            }
            return mutex;
        }
        int doPstTask(object o)
        {
            LogIt("do PST task");
            {
                envClass.getInstance().pauseDetection(_label);
                //_status = 4;
                //updateRuntimeCurrentStatus(_status);
                bool _pstTaskDone = false;
                int prlErrorCode = 1;
                int refurbishErrorCode = 1;
                int firmwareErrorCode = 1;
                string maxPrlFile = string.Empty;
                //System.Threading.Mutex m = null;
                System.Threading.Semaphore sem = null;
                System.Timers.Timer timer = new System.Timers.Timer(1000);

                if (monitorEvent != null)
                {
                    monitorEvent(this, new EventArgs());
                }

                try
                {
                    string exePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    if (System.IO.File.Exists(System.IO.Path.Combine(exePath, "icss.xml")))
                    {
                        XmlDocument icssDom = new XmlDocument();
                        icssDom.Load(System.IO.Path.Combine(exePath, "icss.xml"));
                        string handsetId = getHandsetId();
                        XmlNode handsetNode = icssDom.SelectSingleNode(string.Format("//handset[@id='{0}']", handsetId));
                        if (handsetNode != null)
                        {
                            sem = getSemaphoreForTask(handsetNode);
                            if (sem != null)
                            {
                                sem.WaitOne();
                            }
                            // are you ready?
                            {
                                _status = 8;
                                updateRuntimeCurrentStatus(_status);
                                if (monitorEvent != null)
                                {
                                    monitorEvent(this, new EventArgs());
                                }
                                _time = 0;
                                timer.AutoReset = true;
                                timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                                timer.Start();
                            }

                            string dlltype = envClass.getInstance().GetConfigValueByKey("config", "dlltype", Program.dllType);
                            XmlNode bptNode = handsetNode.SelectSingleNode(string.Format("DLL[@type='{0}']", dlltype));
                            if (bptNode != null)
                            {
                                int errorCode = 1;
                                string prlFile = string.Empty;
                                string binFile = string.Empty;
                                string dllName = (bptNode.SelectSingleNode("name") == null) ? string.Empty : bptNode.SelectSingleNode("name").InnerText;
                                if (!string.IsNullOrEmpty(dllName))
                                {
                                    string op = (bptNode.SelectSingleNode("Operations") == null) ? "PRL Update,Refurbish,Software Download" : bptNode.SelectSingleNode("Operations").InnerText;
                                    string[] ops = op.Split(new char[] { ',' });

                                    foreach (string operation in ops)
                                    {
                                        if (string.Compare(operation, "PRL Update", true) == 0)
                                        {
                                            // do prl update
                                            prlFile = (handsetNode.SelectSingleNode("prlFile") == null) ? string.Empty : handsetNode.SelectSingleNode("prlFile").InnerText;
                                            int steps = 0;
                                            bool b = false;
                                            getPackageFeatures(bptNode.SelectSingleNode("functions"), operation, ref steps, ref b);
                                            updateRuntimeCurrentTask(getResourceString("PRL_UPDATE"));
                                            maxPrlFile = envClass.getInstance().findLastestPrlFile(System.IO.Path.Combine(exePath, System.IO.Path.GetDirectoryName(prlFile), System.IO.Path.GetFileName(prlFile)));
                                            LogIt(string.Format("Write the latest PRL {0} to device.", maxPrlFile));
                                            errorCode = doPrlUpdate(dllName, maxPrlFile, steps);
                                            prlErrorCode = errorCode;
                                        }
                                        else if (string.Compare(operation, "Refurbish", true) == 0)
                                        {
                                            // do refurbish
                                            int postDelay = 0;
                                            int steps = 100;
                                            bool b = false;
                                            XmlNode packageNode = bptNode.SelectSingleNode(string.Format("functions/package[text()='{0}']", operation));
                                            if (packageNode != null)
                                            {
                                                string s = ((XmlElement)packageNode).HasAttribute("steps") ? packageNode.Attributes["steps"].Value : string.Empty;
                                                if (!string.IsNullOrEmpty(s))
                                                    Int32.TryParse(s, out steps);
                                                s = ((XmlElement)packageNode).HasAttribute("postdelay") ? packageNode.Attributes["postdelay"].Value : string.Empty;
                                                if (!string.IsNullOrEmpty(s))
                                                    Int32.TryParse(s, out postDelay);
                                            }
                                            else
                                                getPackageFeatures(bptNode.SelectSingleNode("functions"), operation, ref steps, ref b);
                                            //updateRuntimeCurrentTask(getResourceString("REFURBISH"));
                                            updateRuntimeCurrentTask("fddll_preparation");
                                            updateRuntimeTotalSteps(steps);
                                            errorCode = doRefurbish(dllName, steps, postDelay);
                                            refurbishErrorCode = errorCode;
                                            envClass.getInstance().LogIt(string.Format("Get do refurbish return code: {0}",errorCode));
                                        }
                                        else if (string.Compare(operation, "Software Download", true) == 0)
                                        {
                                            // do software download
                                            int steps = 0;
                                            bool b = false;
                                            getPackageFeatures(bptNode.SelectSingleNode("functions"), operation, ref steps, ref b);
                                            binFile = (handsetNode.SelectSingleNode("firmwareFile") == null) ? string.Empty : handsetNode.SelectSingleNode("firmwareFile").InnerText;
                                            updateRuntimeCurrentTask(getResourceString("SW_DWNLD"));
                                            if (b)
                                            {
                                                // launch fwMemShare.EXE to share memory
                                                shareFirmwareFile(System.IO.Path.Combine(exePath, System.IO.Path.GetDirectoryName(binFile), System.IO.Path.GetFileName(binFile)));
                                            }
                                            // Chris: testing delay 15 seconds to start software download.
                                            //lock (_pranet)
                                            //{
                                            //    bool ok = false;
                                            //    while (!ok)
                                            //    {
                                            //        DateTime _now = DateTime.Now;
                                            //        TimeSpan ts = _now - _lastFirmwareFlashStarted;
                                            //        if (ts.TotalSeconds > 15)
                                            //        {
                                            //            ok = true;
                                            //            _lastFirmwareFlashStarted = _now;
                                            //        }
                                            //        if (!ok)
                                            //        {
                                            //            System.Threading.Thread.Sleep(5000);
                                            //        }
                                            //    }
                                            //}
                                            errorCode = doSoftwareDownload(dllName, System.IO.Path.Combine(exePath, System.IO.Path.GetDirectoryName(binFile), System.IO.Path.GetFileName(binFile)), steps);
                                            firmwareErrorCode = errorCode;
                                        }
                                        else
                                        {
                                            // not support.
                                        }
                                        if (errorCode != 1)
                                        {
                                            envClass.getInstance().LogIt(string.Format("operation break because error code {0}", errorCode));
                                            break;
                                        }
                                    }

                                    //Steven: if pstloader return 86 or 8004, don't send any report to server
                                    if (prlErrorCode != 86 && firmwareErrorCode != 86 && refurbishErrorCode != 86
                                        /*&& prlErrorCode != 8004 && firmwareErrorCode != 8004 && refurbishErrorCode != 8004*/)
                                    {
                                        // send report
                                        string log_file = string.Empty;
                                        // Chris: handle log
                                        try
                                        {
                                            StringBuilder sb = new StringBuilder(512);
                                            string log_ini = System.IO.Path.Combine(envClass.getInstance().ExePath, "log.ini");
                                            win32API.GetPrivateProfileString("config", "log_enable", "true", sb, (uint)sb.Capacity, log_ini);
                                            string log_enable = sb.ToString();
                                            if (string.Compare("true", log_enable, true) == 0)
                                            {
                                                bool force_log = false;
                                                sb.Clear();
                                                win32API.GetPrivateProfileString("config", "log_longop", "", sb, (uint)sb.Capacity, log_ini);
                                                string log_long_op = sb.ToString();
                                                if (!string.IsNullOrEmpty(log_long_op))
                                                {
                                                    int long_op;
                                                    if (Int32.TryParse(log_long_op, out long_op))
                                                    {
                                                        if (_time > long_op * 60)
                                                        {
                                                            envClass.getInstance().LogIt("will force log upload according to log_longop value");
                                                            force_log = true;
                                                        }
                                                    }
                                                }
                                                sb.Clear();
                                                win32API.GetPrivateProfileString("config", "log_force", "false", sb, (uint)sb.Capacity, log_ini);
                                                if (string.Compare(sb.ToString(), System.Boolean.TrueString, true) == 0)
                                                {
                                                    envClass.getInstance().LogIt("will force log upload according to log_force value");
                                                    force_log = true;
                                                }
                                                if (prlErrorCode != 1 || firmwareErrorCode != 1 || refurbishErrorCode != 1 || force_log)
                                                {
                                                    //If any task has a failure, zip up logs to send to server:
                                                    //if (System.IO.Directory.Exists(envClass.getInstance().getLogDir()))
                                                    {
                                                        // copy log dir to temp folder
                                                        envClass.getInstance().LogIt(string.Format("prepare log to transaction with prl errorcode:{0} firmware errorcode:{1},refurbish errorcode:{2},force_log:{3}",prlErrorCode,firmwareErrorCode,refurbishErrorCode,force_log));
                                                        string log_dir = System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables("%APSTHOME%"), "logs");
                                                        string temp_log_dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), string.Format("log_label_{0}", _label));
                                                        if (System.IO.Directory.Exists(log_dir))
                                                        {
                                                            System.IO.Directory.CreateDirectory(temp_log_dir);
                                                            foreach (string file in System.IO.Directory.GetFiles(log_dir))
                                                            {
                                                                System.IO.File.Copy(file, System.IO.Path.Combine(temp_log_dir, System.IO.Path.GetFileName(file)), true);
                                                            }
                                                        }
                                                        if (System.IO.Directory.Exists(temp_log_dir))
                                                        {
                                                            string zip_exe = System.IO.Path.Combine(envClass.getInstance().ExePath, @"hydra\7z.exe");
                                                            if (System.IO.File.Exists(zip_exe))
                                                            {
                                                                string zip_file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), string.Format("log_{0}.zip", _label));
                                                                if (System.IO.File.Exists(zip_file))
                                                                {
                                                                    System.IO.File.Delete(zip_file);
                                                                }

                                                                System.Diagnostics.ProcessStartInfo startInfo_zip = new System.Diagnostics.ProcessStartInfo();
                                                                startInfo_zip.FileName = zip_exe;
                                                                startInfo_zip.Arguments = string.Format("a -rtzip \"{0}\" \"{1}\\*\"", zip_file, temp_log_dir);
                                                                startInfo_zip.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                                                startInfo_zip.CreateNoWindow = true;
                                                                System.Diagnostics.Process p = System.Diagnostics.Process.Start(startInfo_zip);

                                                                if (p != null)
                                                                {
                                                                    p.WaitForExit();
                                                                    if (System.IO.File.Exists(zip_file))
                                                                    {
                                                                        log_file = zip_file;
                                                                    }
                                                                }
                                                            }
                                                            try
                                                            {
                                                                System.IO.Directory.Delete(temp_log_dir, true);
                                                            }
                                                            catch (System.Exception)
                                                            {
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            // Chris: add port information, re-use prl file field
                                            if (string.IsNullOrEmpty(maxPrlFile))
                                            {
                                                maxPrlFile = _label.ToString();
                                            }
                                        }
                                        catch (Exception) { }
                                        //TO-DO: add transaction. 
                                        if (System.IO.File.Exists(System.IO.Path.Combine(envClass.getInstance().ExePath, "HydraTransaction.exe")))
                                        {
                                            envClass.getInstance().LogIt(string.Format("will call HydraTransaction.exe to transaction ===> errorcode:{0}, logfile:{1}",errorCode,log_file));
                                            sendTransLog_V2(errorCode, maxPrlFile, binFile, log_file);
                                        }
                                        else
                                            sendTransLog(errorCode, maxPrlFile, binFile, log_file);
                                        
                                    }
                                }
                            }
                        }
                        icssDom = null;
                    }
                }
                catch (System.Exception ex)
                {
                    LogIt(string.Format("doPstTask: exception: {0}", ex.Message));
                }
                finally
                {
                    if (sem != null)
                    {
                        sem.Release();
                    }
                    timer.Stop();

                    //Steven: if pstloader return 86 or 22002, show password unlock UI
                    if (prlErrorCode == 86 || firmwareErrorCode == 86 || refurbishErrorCode == 86)
                    {
                        _status = 9;
                        updateRuntimeCurrentStatus(10);
                        updateRuntimeMessage("fdtw_info", "fdtc_removePW");
                    }
                    else if (prlErrorCode == 22002 || firmwareErrorCode == 22002 || refurbishErrorCode == 22002)
                    {
                        _status = 9;
                        updateRuntimeCurrentStatus(10);
                        updateRuntimeMessage("fdtw_info", "fdtc_removeIT");
                    }
                    else
                    {
                        if (prlErrorCode == 1 && firmwareErrorCode == 1 && (refurbishErrorCode == 1 || refurbishErrorCode == 50000))
                        {
                            _status = 9;
                            //_previous_device_id = getDeviceId();
                        }
                        else
                        {
                            _previous_device_id = string.Empty;
                            _status = 0;
                        }
                        if (ctrlClass.hasVerify)
                        {
                            updateRuntimeCurrentStatus(289);
                        }
                        else
                        {
                            updateRuntimeCurrentStatus(_status);
                        }

                        if (hasCheckcredit)
                        {
                            if (bAvailableCredit())
                            {
                                bReDedect = true;
                            }
                            else
                            {
                                LogIt(string.Format("credit run out {0}", _label));
                            }
                        }
                        if (hasCheckEnv)
                        {
                            if (bcorrectEnv())
                            {
                                bResume = true;
                            }
                            else
                            {
                                LogIt(string.Format("{0} Environment is not correct!!!!!  ", _label));
                            }
                        }

                    }
                    
                    if (monitorEvent != null)
                    {
                        monitorEvent(this, new EventArgs());
                    }
                      updateRuntimeTimer(0);
                   //updateRuntimeProgressBar(0);
                }
                _pstTaskDone = true;
                if (hasIgnoreRecord && refurbishErrorCode == 50000)
                {
                    _previous_device_id = string.Empty;
                    hasSpecialerror = true;
                    LogIt(string.Format("refurbishErrorCode is  {0}, so don't record ios device id, {1}", refurbishErrorCode, _label));
                }
                else
                {
                    hasSpecialerror = false;
                    _previous_device_id = getDeviceId();

                }
                if (hasSpecialerror)
                {
                    envClass.getInstance().resumeDetection(_label);
                }
                
            }
            if (monitorEvent != null)
            {
                monitorEvent(this, new EventArgs());
            }

            LogIt("PST task done");
            return _status;
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_status == 8)
                _time++;
                //updateRuntimeTimer(_time++);
        }

        void recoveryPreviousDeviceNode()
        {
            try
            {
                lock (_filename)
                {
                    XmlDocument dom = new XmlDocument();
                    dom.Load(_filename);
                    XmlNode rootNode = dom.SelectSingleNode("/labelinfo");
                    XmlNode idNode = rootNode.SelectSingleNode("device");
                    if (_previous_device_node != null)
                    {
                        //recovery previous device node
                        XmlNode newNode = dom.ImportNode(_previous_device_node, true);
                        rootNode.ReplaceChild(newNode, idNode);                        
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                   
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        void updateRuntimeCurrentStatus(int _status)
        {
            // Chris: enhance the update runtime status
            bool ok = false;
            while (!ok)
            {
                try
                {
                    lock (_filename)
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime");
                        n1.Attributes["id"].Value = envClass.getInstance().getStatusString(_status); 
                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
                catch (System.Exception ex)
                {
                }

                // read back and verify
                try
                {
                    XmlDocument dom = new XmlDocument();
                    dom.Load(_filename);
                    XmlElement n1 = (XmlElement)dom.SelectSingleNode("/labelinfo/runtime");
                    if (n1.HasAttribute("id"))
                    {
                        string v = n1.Attributes["id"].Value;
                        if(string.Compare(v, envClass.getInstance().getStatusString(_status))==0)
                        {
                                ok = true;
                        }
                    }
                }
                catch (System.Exception ex)
                {

                }

                if (!ok)
                {
                    // try again;
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
        string getHandsetId()
        {
            string ret = string.Empty;
            try
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(_filename);
                XmlNode n1 = dom.SelectSingleNode("/labelinfo/device");
                ret = n1.Attributes["id"].Value;
            }
            catch (System.Exception ex)
            {
            }
            return ret;
        }
        string getDeviceId()
        {
            // Chris: Due to iPhone case,
            // if iPhone is locked by user, we can't read the IMEI from device, we are using iPhone's UniqueChipID.
            // we will return UniqueChipID instead of IMEI
            string ret = string.Empty;
            try
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(_filename);
                XmlNode d = dom.SelectSingleNode("/labelinfo/device");
                if (d != null && ((XmlElement)d).HasAttribute("vid"))
                {
                    string vid = d.Attributes["vid"].Value;
                    if (string.Compare(vid, "05ac") == 0)
                    {
                        XmlNodeList n = dom.SelectNodes("/labelinfo/device/uniquechipid");
                        ret = (n != null && n.Count > 0) ? n[n.Count - 1].InnerText : string.Empty;
                    }
                    else
                    {
                        XmlNodeList n = dom.SelectNodes("/labelinfo/device/deviceid");
                        ret = (n != null && n.Count > 0) ? n[n.Count - 1].InnerText : string.Empty;
                    }
                }
            }
            catch (Exception) { }
            return ret;
        }

        string getSpecialId()
        {
          
            string ret = string.Empty;
            try
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(_filename);
                XmlNode d = dom.SelectSingleNode("/labelinfo/device");
                if (d != null && ((XmlElement)d).HasAttribute("vid"))
                {
                     XmlNodeList n = dom.SelectNodes("/labelinfo/device/specialId");
                     ret = (n == null && n.Count > 0) ? string.Empty : n[n.Count - 1].InnerText;  
                    if (!string.IsNullOrEmpty(ret)&& string.Compare(ret,_previous_special_id,true)==0)
                    {
                        LogIt("AbNormal device shake detected!!!!!!!");
                        string currentFindTime = DateTime.Now.ToString("yyyy/MM/dd  HH:mm:ss");
                        string fdshakefile = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "deviceshake.ini");
                        IniFile iniobj = new IniFile(fdshakefile);
                        iniobj.WriteValue(currentFindTime, "deviceGone", _previous_device_gone_time);
                        iniobj.WriteValue(currentFindTime, "deviceBack", currentFindTime);
                        iniobj.WriteValue(currentFindTime, "deviceid", _previous_device_id);
                        zipFileToFTP(fdshakefile);
                    }
                }
            }
            catch (Exception) { }
            return ret;
        }

        void zipFileToFTP(string sFile)
        {
            try
            {
                if (File.Exists(sFile))
                {
                    string zip_exe = System.IO.Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), @"hydra\7z.exe");
                    string dFolder = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "logs\\backups");
                    if (Directory.Exists(dFolder))
                    {
                        string zipfileName = string.Format("AbNormal_device_shake_{0}.zip", DateTime.Now.ToString("yyyy_MM_dd_HH_mm"));
                        string zipfile = Path.Combine(dFolder, zipfileName);
                        if (System.IO.File.Exists(zipfile))
                        {
                            System.IO.File.Delete(zipfile);
                        }

                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = zip_exe;
                        p.StartInfo.Arguments = string.Format("a -rtzip \"{0}\" \"{1}\"", zipfile, sFile); ;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        p.Start();
                        p.WaitForExit();


                    }
                    else
                    {
                        LogIt("zipFileToFTP fold is not exist: " + dFolder);
                    }

                }



            }
            catch (System.Exception ex)
            {
                LogIt("zipFileToFTP exception " + ex.ToString());

            }
        }
        void getPackageFeatures(XmlNode functionNode, string name, ref int steps, ref bool shareable)
        {
            steps = 0;
            shareable = false;
            if (functionNode != null)
            {
                try
                {
                    foreach (XmlNode n in functionNode.ChildNodes)
                    {
                        if (string.Compare(n.InnerText, name, true) == 0)
                        {
                            steps = Convert.ToInt32(n.Attributes["steps"].Value);
                            shareable = (string.Compare(n.Attributes["shareable"].Value, "true", true) == 0) ? true : false;
                            break;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                }
            }
        }
        int doPrlUpdate(string dllName, string prlFile, int steps)
        {
            int ret = 0;
            string exePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            LogIt(string.Format("doPrlUpdate: ++ {0}, steps={1}", System.IO.Path.Combine(exePath, System.IO.Path.GetDirectoryName(prlFile), System.IO.Path.GetFileName(prlFile)), steps));
            DateTime _in = DateTime.Now;
            // make pst task command file
            // 
            int position = 0;
            float stepPerSecond = 0;
            float currentSteps = 0;
            if (steps > 0)
            {
                stepPerSecond = (float)100 / steps;
                currentSteps = 0;
            }
            string pstCommand = makePstCommandXmlFile(dllName, "PRL Update", prlFile);
            if (System.IO.File.Exists(pstCommand))
            {
                System.Diagnostics.Process p = doPstLoader(pstCommand);

                uint nCurSubID = (uint)p.Id;
                string datePatt = @"MMddHHmmss";
                string sTime = System.DateTime.Now.ToString(datePatt);
                //string logTxt = string.Format("{0}\\{1}_{2}_PRLUpdate.log", sLogFolder, nCurSubID.ToString(), sTime);

                if (p != null)
                {
                    while (!p.HasExited)
                    {
                        currentSteps += stepPerSecond;
                        if (currentSteps > 1.0)
                        {
                            int _step = Convert.ToInt32(currentSteps);
                            if (_step > position)
                            {
                                updateRuntimeProgressBar(_step);
                                position = _step;
                            }
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                    ret = p.ExitCode;
                    //
                    // delay 3 second to show the status
                    updateRuntimeProgressBar(100);
                    System.Threading.Thread.Sleep(3000);
                }
                System.IO.File.Delete(pstCommand);
            }
            else
            {
                LogIt(string.Format("doPrlUpdate: error, can't create pst command file"));
            }
            bool quit = false;
            while (!quit)
            {
                DateTime _out = DateTime.Now;
                TimeSpan ts = _out - _in;
                if (ts.TotalSeconds > 30)
                {
                    quit = true;
                }
                if (!quit)
                {
                    System.Threading.Thread.Sleep(5000);
                }
            }
            LogIt(string.Format("doPrlUpdate: -- ret={0} ", ret));
            return ret;
        }
        string makePstCommandXmlFile(string dllName, string pstTask, string paramters)
        {
            string ret = string.Empty;
            LogIt(string.Format("makePstCommandXmlFile: ++ dll={0}, task={1}", dllName, pstTask));
            try
            {
                XmlDocument dom = new XmlDocument();
                XmlNode dec = dom.CreateNode(XmlNodeType.XmlDeclaration, "", "");
                dom.AppendChild(dec);
                XmlElement psttaskElement = dom.CreateElement("psttask");
                dom.AppendChild(psttaskElement);
                XmlElement hwndEl = dom.CreateElement("hwnd");
                hwndEl.InnerText = "0";
                psttaskElement.AppendChild(hwndEl);
                XmlElement taskidEl = dom.CreateElement("taskid");
                taskidEl.InnerText = _label.ToString();
                psttaskElement.AppendChild(taskidEl);
                XmlElement processidEl = dom.CreateElement("processid");
                processidEl.InnerText = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
                psttaskElement.AppendChild(processidEl);
                XmlElement cpEl = dom.CreateElement("currentpath");
                cpEl.InnerText = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                psttaskElement.AppendChild(cpEl);
                XmlElement dllEl = dom.CreateElement("dllname");
                dllEl.InnerText = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), dllName);
                psttaskElement.AppendChild(dllEl);
                XmlElement orgDllEl = dom.CreateElement("originalDLL");
                psttaskElement.AppendChild(orgDllEl);
                XmlElement mslEl = dom.CreateElement("msl");
                XmlElement comportEl = dom.CreateElement("comport");
                {
                    XmlDocument d = new XmlDocument();
                    d.Load(_filename);
                    XmlNode mslNode = d.SelectSingleNode("/labelinfo/device/msl");
                    if (mslNode != null)
                    {
                        mslEl.InnerText = mslNode.InnerText;
                    }
                    else
                    {
                        mslEl.InnerText = "NO MSL";
                    }
                    XmlNode portNode = d.SelectSingleNode("/labelinfo/device/comport");
                    if (portNode != null)
                    {
                        comportEl.InnerText = portNode.InnerText;
                    }
                    d = null;
                }
                psttaskElement.AppendChild(mslEl);
                psttaskElement.AppendChild(comportEl);
                XmlElement taskEl = dom.CreateElement("task");
                taskEl.SetAttribute("id", pstTask);
                taskEl.SetAttribute("filename", (string.IsNullOrEmpty(paramters)) ? "" : paramters);
                taskEl.SetAttribute("refurbish_flag", "true");
                taskEl.SetAttribute("end", "false");
                psttaskElement.AppendChild(taskEl);
                ret = System.IO.Path.Combine(System.IO.Path.GetTempPath(), string.Format("pst_task_label_{0}_{1}.xml", _label, pstTask));
                dom.Save(ret);
                // dump xml
                {
                    StringWriter sw = new StringWriter();
                    dom.Save(sw);
                    LogIt(sw.ToString());
                }
            }
            catch (System.Exception ex)
            {
                LogIt(string.Format("makePstCommandXmlFile: exception {0}", ex.Message));
            }
            LogIt(string.Format("makePstCommandXmlFile: -- ret={0}", ret));
            return ret;
        }
        System.Diagnostics.Process doPstLoader(string args)
        {
            LogIt(string.Format("doPstLoader: ++ {0}", args));
            System.Diagnostics.Process _pstProcess = null;
            string exePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "PSTLoader.EXE");
            if (System.IO.File.Exists(exePath))
            {
                _pstProcess = new System.Diagnostics.Process();
                _pstProcess.StartInfo.FileName = exePath;
                _pstProcess.StartInfo.Arguments = string.Format("\"{0}\"", args);
                _pstProcess.StartInfo.CreateNoWindow = true;
                _pstProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _pstProcess.Start();
            }
            LogIt(string.Format("doPstLoader: --"));
            return _pstProcess;
        }
        void updateRuntimeTotalSteps(int steps)
        {
            try
            {
                lock (_filename)
                {
                    if (steps >= 0)
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/steps");
                        if (n1 == null)
                        {
                            XmlElement e = dom.CreateElement("steps");
                            e.InnerText = steps.ToString();
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n1.InnerText = steps.ToString();
                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        void updateRuntimeTaskTag(string strflag)
        {
            try
            {
                lock (_filename)
                {

                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/tasktag");
                        if (n1 == null)
                        {
                            XmlElement e = dom.CreateElement("tasktag");
                            e.InnerText = strflag;
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n1.InnerText = strflag;
                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        void updateRuntimeProgressBar(int steps)
        {
            try
            {
                lock (_filename)
                {
                    if (steps >= 0 && steps <= 100)
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/progress");
                        if (n1 == null)
                        {
                            XmlElement e = dom.CreateElement("progress");
                            e.InnerText = steps.ToString();
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n1.InnerText = steps.ToString();
                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        int doRefurbish(string dllName, int steps, int postDelay)
        {
            int ret = 0;
            string exePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            LogIt(string.Format("doRefurbish: ++　{0}, steps={1}", exePath, steps));
            DateTime _in = DateTime.Now;
            updateRuntimeTaskTag("begin");

            int position = 0;
            float stepPerSecond = 0;
            float currentSteps = 0;
            if (steps > 0)
            {
                stepPerSecond = (float)100 / steps;
                currentSteps = 0;
            }
            string pstCommand = makePstCommandXmlFile(dllName, "Refurbish", string.Empty);
            if (System.IO.File.Exists(pstCommand))
            {
                System.Diagnostics.Process p = doPstLoader(pstCommand);

                uint nCurSubID = (uint)p.Id;
                string datePatt = @"MMddHHmmss";
                string sTime = System.DateTime.Now.ToString(datePatt);

                if (p != null)
                {
                    bool done = false;
                    while (!done)
                    {
                        if (p.HasExited)
                        {
                            if (postDelay > 0)
                            {
                                postDelay--;
                            }
                            else
                                done = true;
                        }
                        currentSteps += stepPerSecond;
                        if (currentSteps > 1.0)
                        {
                            int _step = Convert.ToInt32(currentSteps);
                            if (_step > position)
                            {
                                //updateRuntimeProgressBar(_step);
                                position = _step;
                            }
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                    ret = p.ExitCode;
                    //
                    // delay 4 second to show the status
                    //updateRuntimeProgressBar(100);
                    updateRuntimeTaskTag("stop");
                    //System.Threading.Thread.Sleep(4000);
                }
                System.IO.File.Delete(pstCommand);
            }
            else
            {
                LogIt(string.Format("doRefurbish: error, can't create pst command file"));
            }
            //bool quit = false;
            //while (!quit)
            //{
            //    DateTime _out = DateTime.Now;
            //    TimeSpan ts = _out - _in;
            //    if (ts.TotalSeconds > envClass.getInstance().getTaskDelay())
            //    {
            //        quit = true;
            //    }
            //    if (!quit)
            //    {
            //        System.Threading.Thread.Sleep(5000);
            //    }
            //}
            _pretime = _time;
            updateRuntimeTimer(_time);
            LogIt(string.Format("doRefurbish: -- ret={0}", ret));
            return ret;
        }
        int doSoftwareDownload(string dllName, string binFile, int steps)
        {
            int ret = 0;
            string exePath = envClass.getInstance().ExePath;
            string runtimePath = envClass.getInstance().RuntimePath;
            //string _binFile = System.IO.Path.Combine(exePath, binFile);
            LogIt(string.Format("doSoftwareDownload: ++ {0}", binFile));
            DateTime _in = DateTime.Now;
            // make pst task command file
            // 
            if (System.IO.File.Exists(binFile))
            {
                FileInfo fi = new FileInfo(binFile);
                if (fi.Length > 1024 * 1024)
                {
                    int position = 0;
                    float stepPerSecond = 0;
                    float currentSteps = 0;
                    if (steps > 0)
                    {
                        stepPerSecond = (float)100 / steps;
                        currentSteps = 0;
                    }
                    string pstCommand = makePstCommandXmlFile(dllName, "Software Download", binFile);
                    if (System.IO.File.Exists(pstCommand))
                    {
                        System.Diagnostics.Process p = doPstLoader(pstCommand);

                        uint nCurSubID = (uint)p.Id;
                        string datePatt = @"MMddHHmmss";
                        string sTime = System.DateTime.Now.ToString(datePatt);

                        if (p != null)
                        {
                            StringBuilder sb = new StringBuilder(512);
                            while (!p.HasExited)
                            {
                                // check interaction 
                                win32API.GetPrivateProfileString(string.Format("label_{0}", _label), "interaction", "", sb, (uint)sb.Capacity, System.IO.Path.Combine(runtimePath, "config.ini"));
                                if (string.Compare(sb.ToString(), "start", true) == 0)
                                {
                                    if (_status != 7)
                                    {
                                        updateRuntimeCurrentStatus_7();
                                    }
                                }
                                else if (string.Compare(sb.ToString(), "end", true) == 0)
                                {
                                    _status = 8;
                                    updateRuntimeCurrentStatus(_status);
                                    win32API.WritePrivateProfileString(string.Format("label_{0}", _label), "interaction", "done", System.IO.Path.Combine(runtimePath, "config.ini"));
                                }
                                currentSteps += stepPerSecond;
                                if (currentSteps > 1.0)
                                {
                                    int _step = Convert.ToInt32(currentSteps);
                                    if (_step > position)
                                    {
                                        updateRuntimeProgressBar(_step);
                                        position = _step;
                                    }
                                }
                                System.Threading.Thread.Sleep(1000);
                            }
                            ret = p.ExitCode;

                            //
                            // delay 3 second to show the status
                            updateRuntimeProgressBar(100);
                            System.Threading.Thread.Sleep(3000);
                        }
                        System.IO.File.Delete(pstCommand);
                    }
                    else
                    {
                        LogIt(string.Format("doSoftwareDownload: error, can't create pst command file."));
                    }
                }
                else
                {
                    LogIt(string.Format("doSoftwareDownload: error, {0} in-correct size {1}", binFile, fi.Length));
                }
            }
            else
            {
                LogIt(string.Format("doSoftwareDownload: error, {0} doesn't exist!!", binFile));
            }
            bool quit = false;
            while (!quit)
            {
                DateTime _out = DateTime.Now;
                TimeSpan ts = _out - _in;
                if (ts.TotalSeconds > 100)
                {
                    quit = true;
                }
                if (!quit)
                {
                    System.Threading.Thread.Sleep(5000);
                }
            }
            LogIt(string.Format("doSoftwareDownload: -- ret={0}", ret));
            return ret;
        }
        void shareFirmwareFile(string binFile)
        {
            if (System.IO.File.Exists(binFile))
            {
                string exePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string memShare = System.IO.Path.Combine(exePath, "fwMemShare.exe");
                if (System.IO.File.Exists(memShare))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = memShare;
                    p.StartInfo.Arguments = string.Format("-bin \"{0}\" -ppid {1}", binFile, System.Diagnostics.Process.GetCurrentProcess().Id);
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                }
            }
        }
        int handleAfterTaskDone_2()
        {
            // after task done, (success or failure)
            // controller will keep detect devices,
            // if the device detected is same device as last time (by MEDI/IMEI)
            //    then status no change
            // if the device detected is not same device, then _status change to 4.

            if (string.Compare(envClass.getInstance().getStatusString(9), "89", true) == 0)
            {
                return handleAfterTaskDone();
            }

            // chris: add

            if (bClickbutton)
            {
               // do nothing
                _status = 9999;
            }
            else
            {
                if (hasSpecialerror)
                {

                }
                else
                {
                    return handleAfterTaskDone_3();

                }
                if (!string.IsNullOrEmpty(_detectionResult))
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(_detectionResult);
                        if (doc.DocumentElement != null)
                        {
                            XmlNode deviceNode = doc.SelectSingleNode("/label/device");
                            if (deviceNode != null && deviceNode.HasChildNodes && ((XmlElement)deviceNode).HasAttribute("id"))
                            {
                                string id = string.Empty;
                                if (((XmlElement)deviceNode).HasAttribute("vid"))
                                {
                                    string vid = deviceNode.Attributes["vid"].Value;
                                    if (string.Compare(vid, "05ac") == 0)
                                        id = deviceNode.SelectSingleNode("iPhoneSerialNumber") == null ? string.Empty : deviceNode.SelectSingleNode("iPhoneSerialNumber").InnerText;
                                    else
                                        id = deviceNode.SelectSingleNode("deviceid") == null ? string.Empty : deviceNode.SelectSingleNode("deviceid").InnerText;

                                }
                                //string id = deviceNode.SelectSingleNode("deviceid") == null ? string.Empty : deviceNode.SelectSingleNode("deviceid").InnerText;                            
                                if (!string.IsNullOrEmpty(id) && string.Compare(_previous_device_id, id, true) != 0)
                                {
                                    LogIt(string.Format("{0} vs {1}", id, _previous_device_id));
                                    // different device detected
                                    _status = 4;
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {

                    }
                }
            

            }
            

            
            return _status;
        }
        int handleAfterTaskDone()
        {
            try
            {
                bool deviceGone = true;
                XmlDocument dom = new XmlDocument();
                dom.Load(_filename);
                //XmlNodeList hubs = dom.SelectNodes("/labelinfo/label/usbhub");
                XmlNode labelNode = dom.SelectSingleNode("/labelinfo/label");
                if (labelNode!=null)
                {
                    string labelId = labelNode.Attributes["id"].Value;
                    string calibration = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"),"calibration.ini");
                    operateINIFile iniObj = new operateINIFile(calibration);
                    string hubnames = iniObj.IniReadValue("label", labelId);
                    int pos = hubnames.IndexOf('@');
                    string hubport = hubnames.Substring(0, pos);
                    string hubname = hubnames.Substring(pos + 1);
                    int vid = -1, pid = -1;
                    envClass.getInstance().readHub(hubname, Convert.ToInt32(hubport), ref vid, ref pid);
                    if (vid != -1 && pid != -1)
                    {
                        deviceGone = false;
                    }
                }
                
                //foreach (XmlNode n in hubs)
                //{
                //    string hubname = n.Attributes["name"].Value;
                //    string hubport = n.Attributes["port"].Value;
                //    int vid = -1, pid = -1;
                //    envClass.getInstance().readHub(hubname, Convert.ToInt32(hubport), ref vid, ref pid);
                //    if (vid != -1 && pid != -1)
                //    {
                //        deviceGone = false;
                //        break;
                //    }
                //}
                if (deviceGone)
                {
                    LogIt(string.Format("handleAfterTaskDone: device gone!"));
                    // reset the status
                    lock (_filename)
                    {
                        if (bClickbutton)
                        {
                            _status = 9999;// if it has click2Start feature,we can't resume detect
                        }
                        else
                        {
                            if (hastrigerAllLabelDetect)
                            {
                                _status = 10000;
                            }
                            else
                            {
                                _status = 1;

                            }
                           
                            XmlNode n1 = dom.SelectSingleNode("/labelinfo/device");
                            n1.RemoveAll();
                            XmlNode n2 = dom.SelectSingleNode("/labelinfo/runtime");
                            n2.Attributes["id"].Value = envClass.getInstance().getStatusString(1);
                            envClass.getInstance().saveXml(dom, _filename);
                        }
                        
                       
                    }
                }
            }
            catch (System.Exception ex)
            {

            }
            finally
            {
                //if (_status!=9 &&_status!=0)
                //{
                //    envClass.getInstance().resumeDetection(_label);
                //}
            }
            return _status;
        }
        string createTransLogXml()
        {
            string ret = System.IO.Path.GetTempFileName();
            ret = ret.Replace(".tmp", ".xml");
            XmlTextWriter tw = new XmlTextWriter(ret, null);
            tw.Formatting = System.Xml.Formatting.Indented;
            tw.WriteStartDocument();
            tw.WriteStartElement("TransLog");
            tw.WriteStartElement("FDEMT_TransactionRecord");
            tw.WriteEndElement();
            tw.WriteEndElement();
            tw.Close();
            return ret;
        }
        void cleanXMLnode(XmlDocument doc)
        {
            try
            {

                string configPath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "config.ini");
                IniFile iniObj = new IniFile(configPath);
                string[] keyList = null;
                keyList = iniObj.GetKeyNames("cleanNode");
                if (keyList != null && keyList.Length != 0)
                {
                    lock (_filename)
                    {
                        foreach (string key in keyList)
                        {
                            string keyXPath = iniObj.GetString("cleanNode", key, "");
                            XmlNode keyNode = doc.SelectSingleNode(keyXPath);
                            if (keyNode != null)
                            {
                                keyNode.ParentNode.RemoveChild(keyNode);
                            }
                        }
                        envClass.getInstance().saveXml(doc, _filename);
                    }

                }


            }
            catch (System.Exception ex)
            {
                LogIt("cleanXMLnode exception " + ex.ToString());
            }

        }
        private void sendTransLog_V2(int errorCode, string prlFile, string binFile, string logfile)
        {
            try
            {
                envClass.getInstance().LogIt("sendTransLog_V2 ++");
                string translogXML = createTransLogXml();
                XmlDocument info = new XmlDocument();
                XmlDocument doc = new XmlDocument();
                doc.Load(translogXML);
                envClass.getInstance().LogIt("sendTransLog_V2: translogXML is: " + translogXML);
                envClass.getInstance().LogIt("sendTransLog_V2: label xml is: " + _filename);
                info.Load(_filename);
                string uuid = string.Empty;
                if (doc.DocumentElement!=null && info.DocumentElement!=null)
                {
                    XmlNode transactionRecord = doc.SelectSingleNode("/TransLog/FDEMT_TransactionRecord");
                    // uuid will be inserted by hydraTransaction;
                    // transaction must have user id, store id and site it
                    string _operator = envClass.getInstance().getParametersByKey("user");
                    string _company = envClass.getInstance().getParametersByKey("company");
                    string _site = envClass.getInstance().getParametersByKey("site");
                    string productid = string.Empty;
                    string configPath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "config.ini");
                    operateINIFile opini = new operateINIFile(configPath);
                    productid = opini.IniReadValue("config", "productid");
                    if (!string.IsNullOrEmpty(_operator) && !string.IsNullOrEmpty(_site) && !string.IsNullOrEmpty(_company)) 
                    {
                        XmlNode n1;
                        // insert operator;
                        XmlNode n = doc.CreateNode(XmlNodeType.Element, "operator", null);
                        n.InnerText = _operator;
                        transactionRecord.AppendChild(n);
                        // insert company;
                        n = doc.CreateNode(XmlNodeType.Element, "company", null);
                        n.InnerText = _company;
                        transactionRecord.AppendChild(n);
                        // insert site;
                        n = doc.CreateNode(XmlNodeType.Element, "site", null);
                        n.InnerText = _site;
                        transactionRecord.AppendChild(n);
                        // insert pc name
                        n = doc.CreateNode(XmlNodeType.Element, "workstationName", null);
                        n.InnerText = System.Environment.MachineName;
                        transactionRecord.AppendChild(n);
                        //insert productid
                        n = doc.CreateNode(XmlNodeType.Element, "productid", null);
                        n.InnerText = productid;
                        transactionRecord.AppendChild(n);

                        // insert source device id
                        n = doc.CreateNode(XmlNodeType.Element, "sourcePhoneID", null);
                        n1 = info.SelectSingleNode("/labelinfo/device");
                        if (n1 != null)
                        {
                            if (((XmlElement)n1).HasAttribute("id"))
                                n.InnerText = ((XmlElement)n1).Attributes["id"].Value;
                        }
                        transactionRecord.AppendChild(n);
                        // insert source device maker
                        n = doc.CreateNode(XmlNodeType.Element, "sourceMake", null);
                        n.InnerText = info.SelectSingleNode("/labelinfo/device/manufacturer") == null ? string.Empty : info.SelectSingleNode("/labelinfo/device/manufacturer").InnerText;
                        transactionRecord.AppendChild(n);
                        // insert source device model
                        n = doc.CreateNode(XmlNodeType.Element, "sourceModel", null);
                        n1 = info.SelectSingleNode("/labelinfo/runtime/modeltype");
                        if (n1 == null)
                        {
                            n1 = info.SelectSingleNode("/labelinfo/device/model");
                        }
                        n.InnerText = (n1 == null) ? string.Empty : n1.InnerText;
                        transactionRecord.AppendChild(n);
                        // insert source device carrier
                        n = doc.CreateNode(XmlNodeType.Element, "sourceCarrier", null);
                        n1 = info.SelectSingleNode("/labelinfo/device/carrier[@addbyrequest='True']");
                        if (n1 == null)
                        {
                            n1 = info.SelectSingleNode("/labelinfo/runtime/carrier[@addbyrequest='True']");
                            if(n1==null)
                                n1 = info.SelectSingleNode("/labelinfo/device/carrier");
                        }
                        n.InnerText = (n1 == null) ? string.Empty : n1.InnerText;
                        transactionRecord.AppendChild(n);
                        // insert source device esnNumber
                        if (info.SelectSingleNode("/labelinfo/runtime/meid") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "esnNumber", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/meid") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/meid").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        else
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "esnNumber", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/device/meid_imei") == null ? string.Empty : info.SelectSingleNode("/labelinfo/device/meid_imei").InnerText;
                            if (string.IsNullOrEmpty(n.InnerText))
                            {
                               n.InnerText = info.SelectSingleNode("/labelinfo/device/deviceid") == null ? string.Empty : info.SelectSingleNode("/labelinfo/device/deviceid").InnerText;
                            }
                            transactionRecord.AppendChild(n);
                        }
                        // insert source device serialnumber
                        if (info.SelectSingleNode("/labelinfo/runtime/serialnumber") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "serialnumber", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/serialnumber") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/serialnumber").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        // insert source device devicememorysize
                        if (info.SelectSingleNode("/labelinfo/runtime/memory") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "devicememorysize", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/memory") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/memory").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        // insert source device modelnumber
                        if (info.SelectSingleNode("/labelinfo/runtime/modelnumber") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "modelnumber", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/modelnumber") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/modelnumber").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        // insert source device iosVersion
                        if (info.SelectSingleNode("/labelinfo/runtime/iosVersion") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "iosVersion", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/iosVersion") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/iosVersion").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        else
                        {
                            if (info.SelectSingleNode("/labelinfo/device/productversion") != null)
                            {
                                n = doc.CreateNode(XmlNodeType.Element, "iosVersion", null);
                                n.InnerText = info.SelectSingleNode("/labelinfo/device/productversion") == null ? string.Empty : info.SelectSingleNode("/labelinfo/device/productversion").InnerText;
                                transactionRecord.AppendChild(n);
                            }

                        }
                        //phone os version
                        if (info.SelectSingleNode("/labelinfo/runtime/androidVer") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "AndroidVersion", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/androidVer") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/androidVer").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        if (info.SelectSingleNode("/labelinfo/runtime/rimVer") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "RimVersion", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/rimVer") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/rimVer").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //startTime
                        if (info.SelectSingleNode("/labelinfo/runtime/startTime") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "StartTime", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/startTime") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/startTime").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        // insert errorCode
                        XmlNode errorcodenode = doc.CreateNode(XmlNodeType.Element, "errorCode", null);
                        transactionRecord.AppendChild(errorcodenode);
                        XmlNode detailNode = doc.CreateNode(XmlNodeType.Element, "Error_code_detail", null);
                        transactionRecord.AppendChild(detailNode);
                        if (errorCode == 50000)
                        {
                            errorcodenode.InnerText = "1";
                            detailNode.InnerText = errorCode.ToString();
                        }
                        else if (errorCode == 1)
                        {
                            errorcodenode.InnerText = errorCode.ToString();
                            detailNode.InnerText = errorCode.ToString();
                        }
                        else
                        {
                            errorcodenode.InnerText = errorCode.ToString();
                        }
                        // insert timeCreated, need iso8601 format
                        n = doc.CreateNode(XmlNodeType.Element, "timeCreated", null);
                        n.InnerText = DateTime.UtcNow.ToString("o");
                        transactionRecord.AppendChild(n);
                        // insert timetaken
                        n = doc.CreateNode(XmlNodeType.Element, "timetaken", null);
                        string sTemp = (info.SelectSingleNode("/labelinfo/runtime/time") == null) ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/time").InnerText;
                        int iTemp;
                        if (Int32.TryParse(sTemp, out iTemp))
                        {
                            n.InnerText = string.Format("{0:00}:{1:00}:{2:00}", iTemp / 3600, (iTemp / 60) % 60, iTemp % 60);
                        }                        
                        transactionRecord.AppendChild(n);
                        // appraisall id;

                        if (info.SelectSingleNode("/labelinfo/device/appraisalid") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "appraisalID", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/device/appraisalid") == null ? string.Empty : info.SelectSingleNode("/labelinfo/device/appraisalid").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        // for rooted
                        if (info.SelectSingleNode("/labelinfo/runtime/rooted") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "rooted", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/rooted") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/rooted").InnerText;
                            transactionRecord.AppendChild(n);
                        }

                        //jailbroken status
                        if (info.SelectSingleNode("/labelinfo/runtime/jailbroken") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "jailbroken", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/jailbroken") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/jailbroken").InnerText;
                            transactionRecord.AppendChild(n);
                        }

                        //lock status
                        if (info.SelectSingleNode("/labelinfo/runtime/lockstatus") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "lockstatus", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/lockstatus") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/lockstatus").InnerText;
                            transactionRecord.AppendChild(n);
                        }

                        //FindmyIphone
                        if (info.SelectSingleNode("/labelinfo/runtime/FindMyiPhone") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "FindMyiPhone", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/FindMyiPhone") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/FindMyiPhone").InnerText;
                            transactionRecord.AppendChild(n);
                        }

                        //iCloud
                        XmlNode iCloudNode = info.SelectSingleNode("/labelinfo/runtime/iCloud");
                        if (iCloudNode != null)
                        {

                            n = doc.CreateNode(XmlNodeType.Element, "iCloud", null);
                            transactionRecord.AppendChild(n);
                            string strMessa = iCloudNode.InnerText;

                            if (File.Exists(strMessa))
                            {
                                string iclMessage = "iCloud locked, please remove it";
                                try
                                {
                                    StreamReader reader = new StreamReader(strMessa);
                                    string data = reader.ReadToEnd();
                                    reader.Close();
                                    string strdata = Encoding.UTF8.GetString(Convert.FromBase64String(data));

                                    XmlDocument xmldoc = new XmlDocument();
                                    xmldoc.InnerXml = strdata;
                                    XmlNode messageNode = xmldoc.SelectSingleNode("/section/p");

                                    if (messageNode != null)
                                    {
                                        iclMessage = messageNode.InnerText;
                                    }
                                    else
                                    {
                                        envClass.getInstance().LogIt("can't find /section/p");
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    envClass.getInstance().LogIt("parse /labelinfo/runtime/iCloud value exception " + ex.ToString());
                                }

                                n.InnerText = iclMessage;

                            }
                            else
                            {
                                n.InnerText = iCloudNode.InnerText;
                            }


                        }

                        //fd_meid
                        if (info.SelectSingleNode("/labelinfo/runtime/fd_meid") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "FD_MEID", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/fd_meid") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/fd_meid").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //fd_imei
                        if (info.SelectSingleNode("/labelinfo/runtime/fd_imei") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "FD_IMEI", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/fd_imei") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/fd_imei").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //macaddr
                        if (info.SelectSingleNode("/labelinfo/runtime/macaddr") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "MacAddress", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/macaddr") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/macaddr").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //battery
                        if (info.SelectSingleNode("/labelinfo/runtime/battery") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "Battery", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/battery") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/battery").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //batteryRatio
                        if (info.SelectSingleNode("/labelinfo/runtime/batteryRatio") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "BatteryRatio", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/batteryRatio") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/batteryRatio").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //doRefurbish
                        if (info.SelectSingleNode("/labelinfo/runtime/doRefurbish") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "doRefurbish", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/doRefurbish") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/doRefurbish").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //firmwareVersion
                        if (info.SelectSingleNode("/labelinfo/runtime/FirmwareVer") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "firmwareVersion", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/FirmwareVer") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/FirmwareVer").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //baseband
                        if (info.SelectSingleNode("/labelinfo/runtime/baseband") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "Baseband", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/baseband") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/baseband").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //buildnumber
                        if (info.SelectSingleNode("/labelinfo/runtime/buildnumber") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "Buildnumber", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/buildnumber") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/buildnumber").InnerText;
                            transactionRecord.AppendChild(n);
                        }

                        //ErrList
                        if (info.SelectSingleNode("/labelinfo/runtime/refErrList") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "ErrList", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/refErrList") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/refErrList").InnerText;
                            transactionRecord.AppendChild(n);
                        }

                        //mediaTest
                        if (info.SelectSingleNode("/labelinfo/runtime/mediaTest") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "MediaTest", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/mediaTest") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/mediaTest").InnerText;
                            transactionRecord.AppendChild(n);
                        }

                        //ro_serialno
                        if (info.SelectSingleNode("/labelinfo/runtime/ro_serialno") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "Ro_serialno", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/ro_serialno") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/ro_serialno").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //ril_serialnumber
                        if (info.SelectSingleNode("/labelinfo/runtime/ril_serialnumber") != null)
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "Ril_serialnumber", null);
                            n.InnerText = info.SelectSingleNode("/labelinfo/runtime/ril_serialnumber") == null ? string.Empty : info.SelectSingleNode("/labelinfo/runtime/ril_serialnumber").InnerText;
                            transactionRecord.AppendChild(n);
                        }
                        //product Version:
                        string ver = ctrlClass.productV;
                        if (!string.IsNullOrEmpty(ver))
                        {
                            n = doc.CreateNode(XmlNodeType.Element, "ProductVer", null);
                            n.InnerText = ver;
                            transactionRecord.AppendChild(n);
                        }

                        //wsid and wsgroupid
                        string wsconfigPath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "workstationInfo.ini");
                        if (File.Exists(wsconfigPath))
                        {
                            IniFile wsiniobj = new IniFile(wsconfigPath);
                            string wsid = wsiniobj.GetString("workstationInfo", "wsid", "");
                            string wsgid = wsiniobj.GetString("workstationInfo", "wsgroupid", "");
                            if (!string.IsNullOrEmpty(wsid))
                            {
                                n = doc.CreateNode(XmlNodeType.Element, "wsid", null);
                                n.InnerText = wsid;
                                transactionRecord.AppendChild(n);
                            }
                            if (!string.IsNullOrEmpty(wsgid))
                            {
                                n = doc.CreateNode(XmlNodeType.Element, "wsgroupid", null);
                                n.InnerText = wsgid;
                                transactionRecord.AppendChild(n);
                            }
                        }

                        try
                        {
                            IniFile iniObj = new IniFile(configPath);
                            string[] keyList = null;
                            keyList =iniObj.GetKeyNames("transaction");
                            foreach (string key in keyList)
                            {
                                string keyXPath = iniObj.GetString("transaction", key, "");
                                XmlNode keyNode = info.SelectSingleNode(keyXPath);
                                if (keyNode !=null)
                                {
                                    n = doc.CreateNode(XmlNodeType.Element, key, null);
                                    n.InnerText = keyNode.InnerText;
                                    transactionRecord.AppendChild(n);
                                }
                            }
                            //S3Level
                            string strS3level = iniObj.GetString("S3Level", "Level","0");
                            n = doc.CreateNode(XmlNodeType.Element, "S3Level", null);
                            n.InnerText = strS3level;
                            transactionRecord.AppendChild(n);
                        }
                        catch (System.Exception ex)
                        {
                            LogIt("get transaction node exception " + ex.ToString());
                        }
                        //dynamic get ft_*

                        XmlNode nodeRuntime = info.SelectSingleNode("/labelinfo/runtime");
                        if (nodeRuntime.HasChildNodes)
                        {

                            for (int i = 0; i < nodeRuntime.ChildNodes.Count; i++)
                            {
                                string ft_start = nodeRuntime.ChildNodes[i].Name;
                                if (ft_start.ToUpper().StartsWith("FT") && !ft_start.ToUpper().EndsWith("_TESTTIME"))
                                {
                                    XmlNode ft_temp = nodeRuntime.SelectSingleNode(ft_start);
                                    n = doc.CreateNode(XmlNodeType.Element, ft_start, null);
                                    n.InnerText = ft_temp.InnerText;
                                    transactionRecord.AppendChild(n);
                                }

                            }
                        }

                        // insert portNumber
                        n = doc.CreateNode(XmlNodeType.Element, "portNumber", null);
                        n.InnerText = _label.ToString();
                        transactionRecord.AppendChild(n);

                        doc.Save(translogXML);

                        StringWriter sw = new StringWriter();
                        doc.Save(sw);

                        //get uuid from label.xml
                        XmlNode uuidNode = info.SelectSingleNode("/labelinfo/runtime/uuid");

                        if (uuidNode != null)
                        {
                            uuid = uuidNode.InnerText;
                            envClass.getInstance().LogIt("get uuid from label.xml is " + uuid);
                        }

                        envClass.getInstance().LogIt(string.Format("transaction xml is: {0}", sw.ToString()));
                    }                    
                }
                sendCustomerTrasaction(translogXML);
                ctrlClass.getInstance().sendTransactionLog(new ctrlClass.transactionLogData(translogXML, string.Empty, logfile,uuid));
                cleanXMLnode(info);
            }
            catch (System.Exception ex)
            {
                envClass.getInstance().LogIt("sendTransLog_V2 exception "+ex.ToString());
            }
            envClass.getInstance().LogIt("sendTransLog_V2 --");
        }

        private void sendCustomerTrasaction(string transXml)
        {
            string exePath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "PrepareData4Customer.exe");
            if (System.IO.File.Exists(exePath)&& File.Exists(transXml))
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = exePath;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Arguments = string.Format("-xml={0}", transXml);
                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                p.Start();
                p.WaitForExit(2 * 6 * 1000);

            }
            else
            {
                LogIt("call sendCustomerTrasaction exe or xml is not exist");
            }
        }
        private void sendTransLog(int errorCode, string prlFile, string binFile, string logfile)
        {
            string timeNow = DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss");
            string sOpName = string.Empty;
            string sStoreName = string.Empty;
            string sWorkStationName = string.Empty;
            string sSoModel = string.Empty;
            string sSoMake = string.Empty;
            string sSoCarrier = string.Empty;
            string sOpStat = string.Empty;
            string strOpTask = string.Empty;
            string sErrCode = string.Empty;
            string sSoConType = string.Empty;
            string sTimeCreate = string.Empty;
            string sPRL = string.Empty;
            string sSoftVer = string.Empty;
            string strSec = string.Empty;
            string sWipeTime = string.Empty;
            string sRefTime = string.Empty;
            string sRefStatus = string.Empty;
            string sESN = string.Empty;
            string sMAC = string.Empty; 
            string report = string.Empty;

            // chris: add
            string memory_size = string.Empty;
            string serial_number = string.Empty;
            string model_number = string.Empty;

            try
            {
                // data from login xml
                XmlDocument loginDom = new XmlDocument();
                // must need op, company and site

                //sOpName = envClass.getInstance().getOperationId();
                sOpName = Program.User;
                //sStoreName = envClass.getInstance().getStoreId();
                sStoreName = Program.Store;

                sMAC = envClass.getInstance().GetMACAddress();

                if (File.Exists(temp_kitting_data))
                {
                    XmlDocument kittingrpt = new XmlDocument();
                    kittingrpt.Load(temp_kitting_data);
                    report = kittingrpt.SelectSingleNode("/Settings").OuterXml;
                }

                // data from label_n.xml
                XmlDocument labelDom = new XmlDocument();
                labelDom.Load(_filename);
                if (labelDom.DocumentElement != null)
                {
                    sSoModel = (labelDom.SelectSingleNode("/labelinfo/device") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/device").Attributes["id"].Value;
                    sSoMake = (labelDom.SelectSingleNode("/labelinfo/device/maker") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/device/maker").InnerText;
                    sSoCarrier = (labelDom.SelectSingleNode("/labelinfo/device/carrier") == null) ? string.Empty : labelDom.SelectSingleNode("labelinfo/device/carrier").InnerText;
                    strSec = (labelDom.SelectSingleNode("/labelinfo/runtime/time") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/runtime/time").InnerText;
                    sESN = (labelDom.SelectSingleNode("/labelinfo/device/meid_imei") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/device/meid_imei").InnerText;
                    if(string.IsNullOrEmpty(sESN))
                        sESN = (labelDom.SelectSingleNode("/labelinfo/device/deviceid") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/device/deviceid").InnerText;
                    memory_size = (labelDom.SelectSingleNode("/labelinfo/runtime/memory") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/runtime/memory").InnerText;
                    serial_number = (labelDom.SelectSingleNode("/labelinfo/runtime/serialnumber") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/runtime/serialnumber").InnerText; ;
                    model_number = (labelDom.SelectSingleNode("/labelinfo/runtime/modelnumber") == null) ? string.Empty : labelDom.SelectSingleNode("/labelinfo/runtime/modelnumber").InnerText; ;
                }

              
                // sOpStat: =0 success, =1 fail
                //if (errorCode == 1)
                //sOpStat = "0";
                //else
                //sOpStat = "1";

                sOpStat = errorCode.ToString();
                strOpTask = "9"; // refurbish

                // strOpTask = 12 = CUSTOM Profile
                // strOpTask = 11 = DEFAULT Profile
                // strOpTask = 0 = NONE

                // sErrCode
                // sErrCode: =0 success, =1 fail
                if (errorCode == 1)
                    sOpStat = "0";
                else
                    sOpStat = "1";

                sErrCode = errorCode.ToString();
                sSoConType = "cable";
                sTimeCreate = timeNow;
                sPRL = System.IO.Path.GetFileNameWithoutExtension(prlFile);
                sSoftVer = (labelDom.SelectSingleNode("/labelinfo/device/firmwareFile") == null) ? System.IO.Path.GetFileNameWithoutExtension(binFile) : System.IO.Path.GetFileNameWithoutExtension(labelDom.SelectSingleNode("/labelinfo/device/firmwareFile").InnerText); 
                sWipeTime = timeNow;
                sRefTime = timeNow;
                sRefStatus = "50";

                sWorkStationName = Environment.MachineName;

                int intSec = Convert.ToInt32(strSec);

                strSec = string.Format("{0:00}:{1:00}:{2:00}", intSec / 3600, (intSec / 60) % 60, intSec % 60);

                string output = WriteRMSXMLTrans(sOpName, sWorkStationName, sSoModel, sSoMake, sSoCarrier, "null", "null", "null", sOpStat, strOpTask,
                    sStoreName, sErrCode, sSoConType, "null", sTimeCreate, sPRL, sSoftVer, strSec,
                    sWipeTime, sRefTime, sRefStatus, sESN, sMAC, report, string.Empty, model_number, serial_number, memory_size);

                if (System.IO.File.Exists(output))
                {
                    // Chris: save local transaction for debug
                    StringBuilder sb = new StringBuilder(512);
                    string log_ini = System.IO.Path.Combine(envClass.getInstance().ExePath, "log.ini");
                    win32API.GetPrivateProfileString("config", "savelocal", "false", sb, (uint)sb.Capacity, log_ini);
                    if (string.Compare(sb.ToString(), System.Boolean.TrueString, true) == 0 && System.IO.File.Exists(logfile))
                    {
                        DateTime now = DateTime.Now;
                        string log_dir = System.IO.Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), @"Futuredial\TetherWing\log", now.ToString("yyyy-MM-dd"), now.ToString("hhmmss.fff"));
                        try
                        {
                            System.IO.Directory.CreateDirectory(log_dir);
                            System.IO.File.Copy(output, System.IO.Path.Combine(log_dir, System.IO.Path.GetFileName(output)));
                            System.IO.File.Copy(logfile, System.IO.Path.Combine(log_dir, System.IO.Path.GetFileName(logfile)));
                        }
                        catch (System.Exception)
                        {

                        }
                    }
                    ctrlClass.getInstance().sendTransactionLog(new ctrlClass.transactionLogData(output, sOpName, logfile));
                }
            }
            catch (System.Exception ex)
            {

            }
        }
        private string WriteRMSXMLTrans(string sOpName, string sWorkStationName,
            string sSoModel, string sSoMake, string sSoCarrier,
            string sTaModel, string sTaMake, string sTaCarrier,
            string sOpStat, string strOpTask, string sStoreName,
            string sErrCode, string sSoConType, string sTaConType,
            string sTimeCreate, string sPRL, string sSoftVer, string strSec,
            string sWipeTime, string sRefTime, string sRefStatus, string sESN, string sMAC, string kittingreport, string profile,
            string sModelNumber, string sSerialNumber, string sMemorySizes)
        {
            string ret = System.IO.Path.GetTempFileName();
            ret = ret.Replace(".tmp", ".xml");
            XmlTextWriter tw = new XmlTextWriter(ret, null);
            tw.Formatting = System.Xml.Formatting.Indented;
            tw.WriteStartDocument();
            tw.WriteStartElement("TransLog");
            tw.WriteStartElement("FDEMT_TransactionRecord");
            tw.WriteElementString("operatorName", sOpName);
            tw.WriteElementString("workstationName", sWorkStationName);
            tw.WriteElementString("sourceModel", sSoModel);
            tw.WriteElementString("sourceMake", sSoMake);
            tw.WriteElementString("sourceCarrier", sSoCarrier);
            tw.WriteElementString("targetModel", sTaModel);
            tw.WriteElementString("targetMake", sTaMake);
            tw.WriteElementString("targetCarrier", sTaCarrier);
            tw.WriteElementString("operationStatus", sOpStat);
            tw.WriteElementString("operationType", strOpTask);
            tw.WriteElementString("storeName", sStoreName);
            tw.WriteElementString("errorCode", sErrCode);
            tw.WriteElementString("sourceConnectionType", sSoConType);
            tw.WriteElementString("targetConnectionType", sTaConType);
            tw.WriteElementString("timeCreated", sTimeCreate);
            tw.WriteElementString("membershipCode", "");
            tw.WriteElementString("prlversion", sPRL);
            tw.WriteElementString("firmversion", sSoftVer);
            tw.WriteElementString("timetaken", strSec);
            // if profile is empty, set computer name
            if (!string.IsNullOrEmpty(profile))
                tw.WriteElementString("custName", profile);
            else
                tw.WriteElementString("custName", sWorkStationName);
            //new items
            tw.WriteElementString("contentwipetime", sWipeTime);
            tw.WriteElementString("refurbishedtime", sRefTime);
            tw.WriteElementString("refurbishedstatus", sRefStatus);
            tw.WriteElementString("esnNumber", sESN);
            tw.WriteElementString("mac", sMAC);
            if(!string.IsNullOrEmpty(sModelNumber))
                tw.WriteElementString("modelnumber", sModelNumber);
            if(!string.IsNullOrEmpty(sSerialNumber))
                tw.WriteElementString("serialnumber", sSerialNumber);
            if (!string.IsNullOrEmpty(sMemorySizes))
                tw.WriteElementString("devicememorysize", sMemorySizes);
            // new items add by Chris:
            tw.WriteElementString("portNumber", _label.ToString());
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(_filename);
                if (doc.DocumentElement != null)
                {
                    string xpath;
                    // appraisall id;
                    {
                        xpath = "/labelinfo/device/appraisalid";
                        XmlNode n = doc.SelectSingleNode(xpath);
                        if (n != null)
                        {
                            tw.WriteElementString("appraisalID", n.InnerText);
                        }
                    }
                    // po
                    {
                        xpath = "/labelinfo/device/po";
                        XmlNode n = doc.SelectSingleNode(xpath);
                        if (n != null)
                        {
                            tw.WriteElementString("po", n.InnerText);
                        }
                    }
                    // so
                    {
                        xpath = "/labelinfo/device/so";
                        XmlNode n = doc.SelectSingleNode(xpath);
                        if (n != null)
                        {
                            tw.WriteElementString("so", n.InnerText);
                        }
                    }
                    // rma
                    {
                        xpath = "/labelinfo/device/rma";
                        XmlNode n = doc.SelectSingleNode(xpath);
                        if (n != null)
                        {
                            tw.WriteElementString("rma", n.InnerText);
                        }
                    }
                    // iosVersion
                    {
                        xpath = "/labelinfo/runtime/iosVersion";
                        XmlNode n = doc.SelectSingleNode(xpath);
                        if (n != null)
                        {
                            tw.WriteElementString("iosVersion", n.InnerText);
                        }
                    }
                    // for rooted
                    {
                        xpath = "/labelinfo/device/rooted";
                        XmlNode n = doc.SelectSingleNode(xpath);
                        if (n != null)
                        {
                            tw.WriteElementString("rooted", n.InnerText);
                        }
                    }

                }
            }
            catch (System.Exception) { }
            tw.WriteEndElement();
            
            //tw.WriteStartElement("FDEMT_kittingRecord");
            //tw.WriteRaw(kittingreport);
            //tw.WriteEndElement();

            tw.WriteEndElement();

            tw.Flush();
            tw.Close();
            return ret;
        }
        int doTask()
        {
            _status = 8;
            updateRuntimeCurrentStatus(_status);
            LogIt("doTask: ++");
            envClass.getInstance().pauseDetection(_label);
                         
            doPstTask(null);
            
            if (_status == 0)
            {
                _previous_isSuccess = false;
            }
            else
            {
                _previous_isSuccess = true;
            }

            g_vid = MAX_NUM;
            g_pid = MAX_NUM;
           
            
                // record id 
           _previous_special_id = recordSpecialId();
           lock (_filename)
           {
               XmlDocument dom = new XmlDocument();
               dom.Load(_filename);
               XmlNode idNode = dom.SelectSingleNode("/labelinfo/device");
            
               if ( idNode!= null)
               {
                   _previous_device_node = idNode;
                   
               }
           }
            
            LogIt(string.Format("doTask: -- status={0}", _status));
            return _status;
        }
        #region do RMS task

        int doRmsTask(object o)
        {
            profileClass pc = (profileClass)o;
            LogIt(string.Format("do RMS task: ++ {0}", pc.ToString()));
            int result = 1;
            try
            {
                if (monitorEvent != null)
                {
                    monitorEvent(this, new EventArgs());
                }
                //_time = 0;
                // 1. prepare profile                
                if (pc == null)
                {
                    LogIt("doRmsTask: load profile exception!");
                }
                else
                {
                    _mobileq_rms_report = new List<string>();

                    // get phone info
                    string commandFile;
                    updateRuntimeCurrentTask(getResourceString("PHONE_INFO"));
                    commandFile = makeRmsCommandFile("all", "GetDLLInfor", System.IO.Path.Combine(envClass.getInstance().RuntimePath, "temp"));
                    result = runMobileQDllLoader(commandFile);

                    //prepare for Content Transfer: Make separate copies of profile folder to avoid interferences while writing to device
                    string tempPath = System.IO.Path.GetTempPath();
                    string profilepath = System.IO.Path.GetDirectoryName(pc.getProfileFullPath());
                    string profilename = System.IO.Path.GetFileNameWithoutExtension(profilepath);
                    tempPath = tempPath + profilename + _label.ToString();
                    DirectoryCopy(profilepath, tempPath, true);

                    //start content transfer
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("contacts")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_PHONEBOOK"));
                        commandFile = makeRmsCommandFile("phonebook", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("calendars")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_CAL"));
                        commandFile = makeRmsCommandFile("calendar", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("images")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_IMG"));
                        commandFile = makeRmsCommandFile("image", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("video")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_VID"));
                        commandFile = makeRmsCommandFile("video", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("applications")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_APPS"));
                        commandFile = makeRmsCommandFile("application", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("ringtones")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_RINGTONES"));
                        commandFile = makeRmsCommandFile("ringtones", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("music")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_MUSIC"));
                        commandFile = makeRmsCommandFile("music", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("theme")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_THEMES"));
                        commandFile = makeRmsCommandFile("theme", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("wallpaper")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_WPAPERS"));
                        commandFile = makeRmsCommandFile("wallpaper", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }
                    if (result == 0 && !string.IsNullOrEmpty(pc.getPropertyByName("documents")))
                    {
                        updateRuntimeCurrentTask(getResourceString("CUST_DOCS"));
                        commandFile = makeRmsCommandFile("documents", "restore", tempPath);
                        result = runMobileQDllLoader(commandFile);
                    }

                    // delete temp folder at the end
                    System.IO.Directory.Delete(tempPath, true);
                }
            }
            catch (System.Exception ex)
            {

            }
            LogIt(string.Format("do RMS task: -- ret={0}", result));
            return result;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }


        int AddRMSReport()
        {
            int ret = 1;
            if (_mobileq_rms_report != null)
            {
                string mobileQ_XMLParser = System.IO.Path.Combine(envClass.getInstance().ExePath, "MobileQXmlParser.EXE");
                string inputfile = System.IO.Path.Combine(envClass.getInstance().ExePath, "input_") + _label.ToString() + ".txt";
                System.IO.File.WriteAllLines(inputfile, _mobileq_rms_report.ToArray());
                string outputfile = System.IO.Path.Combine(envClass.getInstance().ExePath, "output_") + _label.ToString() + ".xml";

                if (System.IO.File.Exists(mobileQ_XMLParser) && System.IO.File.Exists(inputfile))
                {
                    try
                    {
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = mobileQ_XMLParser;
                        p.StartInfo.Arguments = string.Format("/input=\"{0}\" /output=\"{1}\"", inputfile, outputfile);
                        p.StartInfo.WorkingDirectory = envClass.getInstance().ExePath;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        p.Start();

                        p.WaitForExit();
                        if (p.ExitCode == 0)
                            ret = 0;

                        File.Delete(inputfile);

                        saveRMSResultInfo(outputfile, temp_kitting_data);

                        File.Delete(outputfile);

                    }
                    catch (System.Exception ex)
                    {

                    }
                }
            }
            return ret;
        }

        static public void saveRMSResultInfo(string rmsReport, string resfile)
        {
            if (System.IO.File.Exists(rmsReport))
            {
                try
                {
                    XmlDocument report = new XmlDocument();
                    report.Load(rmsReport);
                    XmlDocument result = new XmlDocument();
                    result.Load(resfile);

                    if (report.DocumentElement != null)
                    {
                        XmlNode resnode = result.SelectSingleNode("/Settings/Layout");
                        if (resnode != null)
                        {
                            //1. Wallpaper result:
                            XmlNode wpaper = report.SelectSingleNode("/rms_report/item[@id='19']");
                            if (wpaper != null)
                            {
                                string successwallpaper = report.SelectSingleNode("/rms_report/item[@id='19']/success_transferred").InnerText;
                                string skippedwpaper = report.SelectSingleNode("/rms_report/item[@id='19']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Wallpapers");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successwallpaper, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successwallpaper);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedwpaper, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedwpaper);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");

                                    if (string.Compare(successwallpaper, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }

                            }

                            //2. Themes result: Don't know id!
                            //XmlNode totalthemes = report.SelectSingleNode("/transDetails/transDetail[@id='13']/processed");
                            //if (totalthemes != null)
                            //{
                            //    int skippedthemes = 0;
                            //    if (report.SelectNodes("/transDetails/exception/skipitem[@contentid = '13']") != null)
                            //    {
                            //        skippedthemes = report.SelectNodes("/transDetails/exception/skipitem[@contentid = '13']").Count;
                            //    }
                            //    // create a <Group> 
                            //    XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                            //    ((XmlElement)groupEl).SetAttribute("id", "");
                            //    ((XmlElement)groupEl).SetAttribute("display", "Themes");
                            //    ((XmlElement)groupEl).SetAttribute("description", "");
                            //    resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                            //    if (string.Compare(totalthemes.InnerText, "0", 0) != 0)
                            //    {
                            //        XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                            //        ((XmlElement)itemEl1).SetAttribute("id", "");
                            //        ((XmlElement)itemEl1).SetAttribute("display", totalthemes.InnerText);
                            //        ((XmlElement)itemEl1).SetAttribute("description", "");
                            //        ((XmlElement)itemEl1).SetAttribute("appearance", "");
                            //        ((XmlElement)itemEl1).SetAttribute("result", "success");
                            //        XmlNode n1 = groupEl.AppendChild(itemEl1);
                            //    }

                            //    if (skippedthemes != 0)
                            //    {
                            //        XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                            //        ((XmlElement)itemEl2).SetAttribute("id", "");
                            //        ((XmlElement)itemEl2).SetAttribute("display", skippedthemes.ToString());
                            //        ((XmlElement)itemEl2).SetAttribute("description", "");
                            //        ((XmlElement)itemEl2).SetAttribute("appearance", "");
                            //        ((XmlElement)itemEl2).SetAttribute("result", "failure");

                            //        if (string.Compare(totalthemes.InnerText, "0", 0) == 0)  // all configured items failed in this category 
                            //        {
                            //            ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                            //        }

                            //        XmlNode n2 = groupEl.AppendChild(itemEl2);
                            //    }
                            //}

                            //3. Video result:
                            XmlNode video = report.SelectSingleNode("/rms_report/item[@id='11']");
                            if (video != null)
                            {
                                string successvideo = report.SelectSingleNode("/rms_report/item[@id='11']/success_transferred").InnerText;
                                string skippedvideo = report.SelectSingleNode("/rms_report/item[@id='11']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Video");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successvideo, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successvideo);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedvideo, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedvideo);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");

                                    if (string.Compare(successvideo, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }
                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }
                            }

                            //4. Audio result:
                            XmlNode audio = report.SelectSingleNode("/rms_report/item[@id='25']");
                            if (audio != null)
                            {
                                string successaudio = report.SelectSingleNode("/rms_report/item[@id='25']/success_transferred").InnerText;
                                string skippedaudio = report.SelectSingleNode("/rms_report/item[@id='25']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Music");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successaudio, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successaudio);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedaudio, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedaudio);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");

                                    if (string.Compare(successaudio, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }
                            }

                            //5. Ringtone result:
                            XmlNode ringtones = report.SelectSingleNode("/rms_report/item[@id='23']");
                            if (ringtones != null)
                            {
                                string successringtones= report.SelectSingleNode("/rms_report/item[@id='23']/success_transferred").InnerText;
                                string skippedringtones = report.SelectSingleNode("/rms_report/item[@id='23']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Ringtones");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successringtones, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successringtones);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedringtones, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedringtones);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");

                                    if (string.Compare(successringtones, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }
                            }

                            //6. Applications result:
                            XmlNode apps = report.SelectSingleNode("/rms_report/item[@id='18']");
                            if (apps != null)
                            {
                                string successapps = report.SelectSingleNode("/rms_report/item[@id='18']/success_transferred").InnerText;
                                string skippedapps = report.SelectSingleNode("/rms_report/item[@id='18']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Applications");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successapps, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successapps);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedapps, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedapps);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");

                                    if (string.Compare(successapps, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }
                            }

                            //7. Images result:
                            XmlNode images = report.SelectSingleNode("/rms_report/item[@id='5']");
                            if (images != null)
                            {
                                string successimgs = report.SelectSingleNode("/rms_report/item[@id='5']/success_transferred").InnerText;
                                string skippedimgs = report.SelectSingleNode("/rms_report/item[@id='5']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Images");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successimgs, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successimgs);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedimgs, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedimgs);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");

                                    if (string.Compare(successimgs, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }

                            }

                            //8. Documents result:
                            XmlNode docs = report.SelectSingleNode("/rms_report/item[@id='13']");
                            if (docs != null)
                            {
                                string successdocs = report.SelectSingleNode("/rms_report/item[@id='13']/success_transferred").InnerText;
                                string skippeddocs = report.SelectSingleNode("/rms_report/item[@id='13']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Documents");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successdocs, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successdocs);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippeddocs, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippeddocs);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");

                                    if (string.Compare(successdocs, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }
                            }

                            //9. Calendar result:
                            XmlNode calendar = report.SelectSingleNode("/rms_report/item[@id='8']");
                            if (calendar != null)
                            {
                                string successcal = report.SelectSingleNode("/rms_report/item[@id='8']/success_transferred").InnerText;
                                string skippedcal = report.SelectSingleNode("/rms_report/item[@id='8']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Calendar");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successcal, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successcal);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedcal, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedcal);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");


                                    if (string.Compare(successcal, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }
                            }

                            //10. Phonebook result:
                            XmlNode contacts = report.SelectSingleNode("/rms_report/item[@id='2']");
                            if (contacts != null)
                            {
                                string successcont = report.SelectSingleNode("/rms_report/item[@id='2']/success_transferred").InnerText;
                                string skippedcont = report.SelectSingleNode("/rms_report/item[@id='2']/skipped").InnerText;

                                // create a <Group> 
                                XmlNode groupEl = result.CreateNode(XmlNodeType.Element, "Group", "");
                                ((XmlElement)groupEl).SetAttribute("id", "");
                                ((XmlElement)groupEl).SetAttribute("display", "Contacts");
                                ((XmlElement)groupEl).SetAttribute("description", "");
                                resnode.InsertBefore(groupEl, resnode.ChildNodes[1]);

                                if (string.Compare(successcont, "0", 0) != 0)
                                {
                                    XmlNode itemEl1 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl1).SetAttribute("id", "");
                                    ((XmlElement)itemEl1).SetAttribute("display", successcont);
                                    ((XmlElement)itemEl1).SetAttribute("description", "");
                                    ((XmlElement)itemEl1).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl1).SetAttribute("result", "success");
                                    XmlNode n1 = groupEl.AppendChild(itemEl1);
                                }

                                if (string.Compare(skippedcont, "0", 0) != 0)
                                {
                                    XmlNode itemEl2 = result.CreateNode(XmlNodeType.Element, "Item", "");
                                    ((XmlElement)itemEl2).SetAttribute("id", "");
                                    ((XmlElement)itemEl2).SetAttribute("display", skippedcont);
                                    ((XmlElement)itemEl2).SetAttribute("description", "");
                                    ((XmlElement)itemEl2).SetAttribute("appearance", "");
                                    ((XmlElement)itemEl2).SetAttribute("result", "failure");


                                    if (string.Compare(successcont, "0", 0) == 0)  // all configured items failed in this category 
                                    {
                                        ((XmlElement)itemEl2).SetAttribute("allfail", "true");
                                    }

                                    XmlNode n2 = groupEl.AppendChild(itemEl2);
                                }
                            }

                        }
                        result.Save(resfile);

                    }
                }
                catch (System.Exception ex)
                {

                }
            }
        }

        int runMobileQDllLoader(string commandFile)
        {
            int ret = 0;
            string mobileQ_Dllloader = System.IO.Path.Combine(envClass.getInstance().ExePath, "MobileQDllLoader.EXE");
            if (System.IO.File.Exists(mobileQ_Dllloader) && System.IO.File.Exists(commandFile))
            {
                Process p = new Process();
                p.StartInfo.FileName = mobileQ_Dllloader;
                p.StartInfo.Arguments = string.Format("0,\"{0}\"", commandFile);
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                if (!p.StartInfo.EnvironmentVariables.ContainsKey("ANDROID_ADB_SERVER_PORT"))
                {
                    p.StartInfo.EnvironmentVariables.Add("ANDROID_ADB_SERVER_PORT", (5050 + (_label * 5)).ToString());
                }
                p.StartInfo.WorkingDirectory = envClass.getInstance().ExePath;
                p.OutputDataReceived += new DataReceivedEventHandler(mobileQ_DllLoader_OutputDataReceived);
                p.Start();
                p.BeginOutputReadLine();
                while (!p.HasExited)
                {
                    System.Threading.Thread.Sleep(1000);
                    updateRuntimeTimer(_time++);
                }
                if (!p.HasExited)
                {
                    p.WaitForExit();
                }
                ret = p.ExitCode;
            }
            return ret;
        }
        void mobileQ_DllLoader_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Data))
            {
                //LogIt(e.Data);
                //WriteTextFile(e.Data, _label); 
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _mobileq_rms_report.Add(e.Data);
                    System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(mobileQDllLoader_IncomingDataHandler), e.Data);
                }
            }
        }
        static void WriteTextFile(string logstring, int label)
        {
            string logfile = System.IO.Path.Combine(envClass.getInstance().ExePath, "input_") + label.ToString() + ".txt";

            if (File.Exists(logfile))
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logfile, true))
                {
                    file.WriteLine(logstring);
                }
            }
            else
            {
                System.IO.File.WriteAllText(logfile, logstring);
            }
        }

        void mobileQDllLoader_IncomingDataHandler(object o)
        {
            string line = (string)o;
            if (!string.IsNullOrEmpty(line))
            {
                LogIt(line);
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(line);
                    if (doc.DocumentElement != null)
                    {
                        XmlNode n = doc.SelectSingleNode("/mobileqdllloader/consolemsg/message");
                        if (n != null)
                        {
                            string key = (n.SelectSingleNode("name") == null) ? string.Empty : n.SelectSingleNode("name").InnerText;
                            string value = (n.SelectSingleNode("value") == null) ? string.Empty : n.SelectSingleNode("value").InnerText;
                            if (string.Compare(key, "progress", true) == 0)
                            {
                                int i;
                                if (Int32.TryParse(value, out i))
                                {
                                    updateRuntimeProgressBar(i);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                }
            }
        }
        string makeRmsCommandFile(string content, string operation, string targetpath)
        {
            string ret = string.Empty;
            try
            {
                string commandXml = System.IO.Path.Combine(envClass.getInstance().RuntimePath, string.Format("label_{0}_rms_command.xml", _label));
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                settings.Encoding = System.Text.Encoding.UTF8;
                XmlWriter xw = XmlWriter.Create(commandXml, settings);
                xw.WriteStartDocument();
                xw.WriteStartElement("task");
                xw.WriteAttributeString("id", _label.ToString());
                xw.WriteStartElement("common");
                xw.WriteStartElement("timeout");
                xw.WriteAttributeString("value", "3000000");
                xw.WriteEndElement();
                xw.WriteStartElement("heartbeat2ui");
                xw.WriteAttributeString("value", "1");
                xw.WriteEndElement();
                //xw.WriteElementString("logpath","")            
                xw.WriteEndElement();
                xw.WriteStartElement("info");
                xw.WriteStartElement("sourcephone");
                xw.WriteElementString("hwnd", "0");
                xw.WriteElementString("manufacturer", getHandsetPropertyByName("manufacturer"));
                xw.WriteElementString("phoneid", getHandsetId());
                xw.WriteElementString("phonemodel", getHandsetPropertyByName("model"));
                xw.WriteElementString("phonedll", System.IO.Path.GetFileNameWithoutExtension(getHandsetPropertyByName("dllname")));
                xw.WriteElementString("commport", getDeviceCommport());
                xw.WriteElementString("bluetooth", "");
                xw.WriteElementString("rootfolder", "FutureDialRMS");
                xw.WriteElementString("resettimer", "0");
                xw.WriteEndElement();
                xw.WriteStartElement("targetphone");
                xw.WriteElementString("hwnd", "");
                xw.WriteElementString("manufacturer", "");
                xw.WriteElementString("phoneid", "");
                xw.WriteElementString("phonemodel", "");
                xw.WriteElementString("phonedll", "");
                xw.WriteElementString("commport", "");
                xw.WriteElementString("bluetooth", "");
                xw.WriteElementString("rootfolder", "");
                xw.WriteElementString("resettimer", "");
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteStartElement("job");
                xw.WriteAttributeString("id", "1");
                xw.WriteElementString("content", content);
                xw.WriteElementString("operation", operation);
                xw.WriteStartElement("parameter");
                xw.WriteElementString("temppath", System.IO.Path.Combine(envClass.getInstance().RuntimePath, "temp"));
                xw.WriteElementString("targetpath", targetpath);
                xw.WriteEndElement();
                xw.WriteElementString("simcardoption", "0");
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndDocument();
                xw.Flush();
                xw.Close();
                ret = commandXml;
            }
            catch (System.Exception ex)
            {

            }
            return ret;
        }
        #endregion
        #region do Kitting Task
        int doKittingTask(object o)
        {
            profileClass pc = (profileClass)o;
            LogIt(string.Format("do Kitting task: ++ {0}", pc.ToString()));
            int result = 1;
            try
            {
                if (monitorEvent != null)
                {
                    monitorEvent(this, new EventArgs());
                }
                // 1. prepare command xml
                string dllPath = System.IO.Path.Combine(envClass.getInstance().ExePath, "device", getHandsetId(), getHandsetPropertyByName("dllname"));
                string commandXml = System.IO.Path.Combine(envClass.getInstance().ExePath, string.Format("label_{0}_kitting_command.xml", _label));
                string temp;

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                settings.Encoding = System.Text.Encoding.UTF8;
                XmlWriter xw = XmlWriter.Create(commandXml, settings);
                xw.WriteStartDocument();
                xw.WriteStartElement("psttask");
                xw.WriteElementString("hwnd", "0");
                xw.WriteElementString("taskid", _label.ToString());
                xw.WriteElementString("processid", Process.GetCurrentProcess().Id.ToString());
                xw.WriteElementString("currentpath", envClass.getInstance().ExePath);
                xw.WriteElementString("dllname", dllPath);
                xw.WriteElementString("haskitting", hasKitting.ToString());
                xw.WriteElementString("originalDLL", "");
                xw.WriteElementString("msl", "");
                xw.WriteElementString("comport", string.Format("COM{0}", _label));
                xw.WriteStartElement("task");
                xw.WriteAttributeString("id", "kitting");
                xw.WriteAttributeString("filename", temp_kitting_data); // kitting xml file full path
                xw.WriteAttributeString("refurbish_flag", "");
                xw.WriteAttributeString("end", "false");
                xw.WriteAttributeString("kittingxml", pc.getKittingXmlFile()); // original kitting file.
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndDocument();
                xw.Flush();
                xw.Close();
                // 2. call uisettingloader.exe
                temp = System.IO.Path.Combine(envClass.getInstance().ExePath, "uisettingloader.exe");
                if (System.IO.File.Exists(temp))
                {
                    Process p = new Process();
                    p.StartInfo.FileName = temp;
                    p.StartInfo.Arguments = string.Format("-command=\"{0}\"", commandXml);
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.OutputDataReceived += new DataReceivedEventHandler(kittingProcess_OutputDataReceived);
                    LogIt(string.Format("invoke \"{0}\" {1}", p.StartInfo.FileName, p.StartInfo.Arguments));
                    p.Start();
                    updateRuntimeCurrentTask(getResourceString("KITTING"), temp_kitting_data);
                    p.BeginOutputReadLine();
                    //_time = 0;
                    while (!p.HasExited)
                    {
                        System.Threading.Thread.Sleep(1000);
                        _time++;
                        updateRuntimeTimer(_time);
                        if (_time % 5 == 0)
                        {
                            updateRuntimeProgressBar(((_time / 5) > 100) ? 95 : (_time / 5));
                        }
                    }
                    if (!p.HasExited)
                    {
                        p.WaitForExit();
                    }
                    result = p.ExitCode;
                    updateRuntimeProgressBar(100);
                }
            }
            catch (System.Exception ex)
            {

            }

            LogIt(string.Format("do Kitting task: -- ret={0}", result));
            return result;
        }

        int[] CheckResultStatus(int RMSRes, int KITRes)
        {
            List<int> ret = new List<int>();

            // ret[0] = UI error code
            // ret[1] = server error code

            if (KITRes == 1167)
            {
                // incomplete... maybe user disconnected device in between task 
                ret.Add(111);
                ret.Add(995);
            }
            else if (KITRes == 50)
            {
                // Android version not supported
                ret.Add(112);
                ret.Add(1);
            }
            else if (KITRes == 0) // Kitting is ok... go on further
            {
                AddRMSReport();

                int skipped = ParseResultXML(temp_kitting_data);

                // check if any items skipped or failed
                if (skipped > 0)
                {
                    // success with skipped
                    ret.Add(110);
                    ret.Add(1001);
                }
                else if (skipped == -1) 
                {
                    // all configured items are failed/self check
                    ret.Add(0);
                    ret.Add(1);
                }
                else if (skipped == 0) 
                {
                    // success
                    ret.Add(109);
                    ret.Add(0);
                }
            }
            else
            {
                //any other code don't know
                ret.Add(0);
                ret.Add(1);
            }

            return ret.ToArray();
        }

        void kittingProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Data))
            {
                LogIt(e.Data);
            }
        }
        #endregion

        #region Update info XML
        int updateDetectedDevice(XmlNode ddNode)
        {
            int _status = 1;
            try
            {
                lock (_filename)
                {
                    XmlDocument dom = new XmlDocument();
                    dom.Load(_filename);
                    if (ddNode == null)
                    {
                        XmlNode idNode = dom.SelectSingleNode("/labelinfo/device");
                        idNode.RemoveAll();
                        XmlNode n = dom.SelectSingleNode("/labelinfo/runtime");
                        _status = 1;
                        if (hastrigerAllLabelDetect)
                        {
                        }
                        else
                        {
                            n.Attributes["id"].Value = envClass.getInstance().getStatusString(_status);

                        }
                        
                    }
                    else
                    {
                        XmlNode newNode = dom.ImportNode(ddNode, true);
                        XmlNode rootNode = dom.SelectSingleNode("/labelinfo");
                        XmlNode idNode = rootNode.SelectSingleNode("device");
                        rootNode.ReplaceChild(newNode, idNode);
                        XmlNode n = dom.SelectSingleNode("/labelinfo/runtime");
                        XmlNode specialNode = dom.SelectSingleNode("/labelinfo/device/specialId");
                        string strSpecialId = string.Empty;
                        if (specialNode != null)
                        {
                            strSpecialId = specialNode.InnerText;
                        }

                        if (!string.IsNullOrEmpty(strSpecialId))
                        {
                            if (string.Compare(strSpecialId, _previous_special_id, true) == 0)
                            {
                                _status = 4;
                            }
                            else
                            {
                                LogIt("unexpected report, so pauseDetection");
                                envClass.getInstance().pauseDetection(_label);
                            }                    
                        }
                        else
                        {
                            // retrieve phone info from icss.xml by phoneID                        
                            if (getPhoneInfoFromIcss(dom, System.IO.Path.Combine(envClass.getInstance().ExePath, "icss.xml")))
                            {
                                // Chris: before start, check if the device connected on right port
                                // if the device is not connect on the right port, set _status=10 and put message
                                if (!checkDeviceOnPort(dom))
                                    _status = 10;
                                else
                                {
                                    _status = 4;
                                    //_previous_device_node = newNode;
                                }
                                //n.Attributes["id"].Value = envClass.getInstance().getStatusString(_status);
                            }
                            else
                            {
                                if (ddNode.Attributes.Count > 0)
                                {
                                    if (hastrigerAllLabelDetect)
                                    {
                                        _status = 303;
                                        n.Attributes["id"].Value = "303";
                                        if (n.SelectSingleNode("detectfailinfo") != null)
                                            n.RemoveChild(n.SelectSingleNode("detectfailinfo"));
                                        XmlNode n1 = newNode.SelectSingleNode("detectfailinfo");
                                        if (n1 != null)
                                            n.AppendChild(n1.CloneNode(true)); ;// handle Go button
                                    }
                                    else
                                    {
                                        _status = 3;
                                        n.Attributes["id"].Value = envClass.getInstance().getStatusString(_status);
                                        if (n.SelectSingleNode("detectfailinfo") != null)
                                            n.RemoveChild(n.SelectSingleNode("detectfailinfo"));
                                        XmlNode n1 = newNode.SelectSingleNode("detectfailinfo");
                                        if (n1 != null)
                                            n.AppendChild(n1.CloneNode(true));
                                    }

                                }
                                else
                                {
                                    _status = 1;
                                    if (hastrigerAllLabelDetect)
                                    {
                                    }
                                    else
                                    {
                                        n.Attributes["id"].Value = envClass.getInstance().getStatusString(_status);
                                    }

                                }
                            }

                        }
                        
                    }
                    //dom.Save(_filename);
                    envClass.getInstance().saveXml(dom, _filename);
                }
            }
            catch (System.Exception ex)
            {

            }
            return _status;
        }
        void updateRuntimeCurrentStatus_7()
        {
            string runtimePath = envClass.getInstance().RuntimePath;
            StringBuilder sb = new StringBuilder(512);
            win32API.GetPrivateProfileString(string.Format("label_{0}", _label), "interaction", "", sb, (uint)sb.Capacity, System.IO.Path.Combine(runtimePath, "config.ini"));
            if (sb.Length > 0 && string.Compare(sb.ToString(), "start", true) == 0)
            {
                sb.Clear();
                win32API.GetPrivateProfileString(string.Format("label_{0}", _label), "usermessage", "", sb, (uint)sb.Capacity, System.IO.Path.Combine(runtimePath, "config.ini"));
                try
                {
                    lock (_filename)
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        if (dom.DocumentElement != null)
                        {
                            XmlNode runtimeNode = dom.SelectSingleNode("/labelinfo/runtime");
                            if (runtimeNode != null)
                            {
                                runtimeNode.Attributes["id"].Value = "7";
                                XmlNode messageNode = dom.SelectSingleNode("/labelinfo/runtime/usermessage");
                                if (messageNode == null)
                                {
                                    messageNode = dom.CreateNode(XmlNodeType.Element, "usermessage", "");
                                    runtimeNode.AppendChild(messageNode);
                                }
                                if (messageNode != null)
                                {
                                    messageNode.InnerText = sb.ToString();
                                }
                            }
                            envClass.getInstance().saveXml(dom, _filename);
                            _status = 7;
                        }
                    }
                }
                catch (System.Exception ex)
                {

                }
            }
        }
        void updateRuntimeTimer(int time)
        {
            try
            {
                if (time >= 0)
                {
                    lock (_filename)
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/time");
                        if (n1 == null)
                        {
                            XmlElement e = dom.CreateElement("time");
                            e.InnerText = time.ToString();
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n1.InnerText = time.ToString();
                        //dom.Save(_filename);
                        //saveXml(dom, _filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        void updateRuntimeCurrentTask(string task, string parameter = "")
        {
            try
            {
                lock (_filename)
                {
                    if (!string.IsNullOrEmpty(task) && !string.IsNullOrWhiteSpace(task))
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/message");
                        if (n1 == null)
                        {
                            XmlElement e = dom.CreateElement("message");
                            e.InnerText = task;
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n1.InnerText = task;
                        // add new node named message2
                        XmlNode n4 = dom.SelectSingleNode("/labelinfo/runtime/message2");
                        if (n4 == null)
                        {
                            XmlElement e = dom.CreateElement("message2");
                            e.InnerText = "";
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n4.InnerText = "";
                        XmlNode n2 = dom.SelectSingleNode("/labelinfo/runtime/progress");
                        if (n2 == null)
                        {
                            XmlElement e = dom.CreateElement("progress");
                            e.InnerText = "0";
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n2.InnerText = "0";
                        if (!string.IsNullOrEmpty(parameter))
                        {
                            XmlNode n3 = dom.SelectSingleNode("/labelinfo/runtime/parameter");
                            if (n3 == null)
                            {
                                XmlElement e = dom.CreateElement("parameter");
                                e.InnerText = parameter;
                                XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                                rtNode.AppendChild(e);
                            }
                            else
                                n3.InnerText = parameter;
                        }
                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        void updateRuntimeMessage(string message, string message2)
        {
            bool ok = false;
            int retry = 0;
            while (!ok && retry++ < 3)
            {
                LogIt(string.Format("updateRuntimeMessage begin: retry={0}", retry));
                try
                {
                    lock (_filename)
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);

                        if (!string.IsNullOrEmpty(message) && !string.IsNullOrWhiteSpace(message))
                        {
                            XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/message");
                            if (n1 == null)
                            {
                                XmlElement e = dom.CreateElement("message");
                                e.InnerText = message;
                                XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                                rtNode.AppendChild(e);
                            }
                            else
                                n1.InnerText = message;
                            LogIt(string.Format("updateRuntimeMessage success: {0}", message));
                        }

                        if (!string.IsNullOrEmpty(message2) && !string.IsNullOrWhiteSpace(message2))
                        {
                            XmlNode n2 = dom.SelectSingleNode("/labelinfo/runtime/message2");
                            if (n2 == null)
                            {
                                XmlElement e = dom.CreateElement("message2");
                                e.InnerText = message2;
                                XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                                rtNode.AppendChild(e);
                            }
                            else
                                n2.InnerText = message2;
                            LogIt(string.Format("updateRuntimeMessage success: {0}", message2));
                        }

                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
                catch (System.Exception ex)
                {
                    LogIt(string.Format("updateRuntimeMessage: exception: {0}", ex.Message));
                }

                // read back and verify
                try
                {
                    bool ok_message  = false;
                    bool ok_message2 = false;
                    XmlDocument dom = new XmlDocument();
                    dom.Load(_filename);

                    XmlElement n1 = (XmlElement)dom.SelectSingleNode("/labelinfo/runtime/message");
                    if (n1 != null)
                    {
                        string v = n1.InnerText;
                        if (string.Compare(v, message) == 0)
                        {
                            ok_message = true;
                        }
                    }

                    XmlElement n2 = (XmlElement)dom.SelectSingleNode("/labelinfo/runtime/message2");
                    if (n2 != null)
                    {
                        string v = n2.InnerText;
                        if (string.Compare(v, message2) == 0)
                        {
                            ok_message2 = true;
                        }
                    }

                    if (ok_message && ok_message2)
                    {
                        ok = true;
                        LogIt(string.Format("updateRuntimeMessage check message success"));
                    }
                }
                catch (System.Exception ex)
                {
                    LogIt(string.Format("updateRuntimeMessage: exception: {0}", ex.Message));
                }

                if (!ok)
                {
                    // try again;
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
        void updateRuntimeErrorCode(string errorCode)
        {
            try
            {
                lock (_filename)
                {
                    if (!string.IsNullOrEmpty(errorCode) && !string.IsNullOrWhiteSpace(errorCode))
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/error");
                        if (n1 == null)
                        {
                            XmlElement e = dom.CreateElement("error");
                            e.InnerText = errorCode;
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n1.InnerText = errorCode;
                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        string getResourceString(string name)
        {
            string ret = string.Empty;

            int retry = 0;
            while (retry++ < 3)
            {
                try
                {
                    ret = Program.RM.GetString(name);
                    if (!string.IsNullOrEmpty(ret))
                        break;
                }
                catch (System.Exception ex)
                {
                    LogIt(string.Format("Exception: getResourceString: {0}", ex.Message));
                }
                System.Threading.Thread.Sleep(500);
            }

            return ret;
        }
        void updateRuntimeCurrentTask(string task)
        {
            try
            {
                lock (_filename)
                {
                    if (!string.IsNullOrEmpty(task) && !string.IsNullOrWhiteSpace(task))
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode("/labelinfo/runtime/message");
                        if (n1 == null)
                        {
                            XmlElement e = dom.CreateElement("message");
                            e.InnerText = task;
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n1.InnerText = task;
                        // add new node named message2
                        XmlNode n3 = dom.SelectSingleNode("/labelinfo/runtime/message2");
                        if (n3 == null)
                        {
                            XmlElement e = dom.CreateElement("message2");
                            e.InnerText = "";
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n3.InnerText = "";
                        XmlNode n2 = dom.SelectSingleNode("/labelinfo/runtime/progress");
                        if (n2 == null)
                        {
                            XmlElement e = dom.CreateElement("progress");
                            e.InnerText = "0";
                            XmlNode rtNode = dom.SelectSingleNode("/labelinfo/runtime");
                            rtNode.AppendChild(e);
                        }
                        else
                            n2.InnerText = "0";

                        //dom.Save(_filename);
                        envClass.getInstance().saveXml(dom, _filename);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }

        #endregion
        string getHandsetPropertyByName(string propertyname)
        {
            string ret = string.Empty;
            try
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(_filename);
                XmlNode n = dom.SelectSingleNode(string.Format("/labelinfo/device/{0}", propertyname));
                ret = (n == null) ? string.Empty : n.InnerText;
            }
            catch (System.Exception ex)
            {
            }
            return ret;
        }
        string getDeviceCommport()
        {
            string ret = string.Empty;
            try
            {
                string s = getHandsetPropertyByName("profile");
                if (string.Compare(s, "9") == 0)
                {
                    s = getHandsetPropertyByName("driverkey");
                    string[] ss = s.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Length > 1)
                    {
                        s = ss[1];
                        int i = -1;
                        if (Int32.TryParse(s, out i))
                        {
                            ret = string.Format("COM{0}", i);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {

            }
            return ret;
        }

        int ParseResultXML(string resultfile)
        {
            int ret = 0;
            int total = 0;
            int phoneinfo = 0;

            if (System.IO.File.Exists(resultfile))
            {
                if (CheckIfSettingsAllFail(resultfile))
                {
                    return -1;
                }
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(resultfile);
                    if (doc.DocumentElement != null)
                    {
                        XmlNodeList items = doc.SelectNodes("//Item");
                        foreach (XmlNode item in items)
                        {
                            if (((XmlElement)item).HasAttribute("allfail"))
                            {
                                return -1;
                            }
                            if (((XmlElement)item).HasAttribute("result"))
                            {
                                total++;
                                string v = item.Attributes["result"].Value;
                                if (string.Compare(v, "success", true) == 0)
                                {
                                    // success item
                                }
                                else if (string.Compare(v, "", true) == 0) //blank "result" attribute is for Phone Info parameters
                                {
                                    phoneinfo++;
                                }
                                else if (string.Compare(v, "someskipped", true) == 0) // content phonebook has a special status "Complete with Some Items Skipped"
                                {
                                    onlyphbk = 1;
                                }
                                else
                                    ret++;
                            }
                        }

                        if ((ret == (total - phoneinfo)) || ((ret == (total - phoneinfo - 1) && (onlyphbk == 1) && (ret > 0))))
                        {
                            ret = -1; //if all configured items are failed/self check then claim result as fail
                        }
                        else if ((ret == 0) && (onlyphbk == 1)) // Profile has only phonebook configured and the result of phonebook is "Complete with items skipped", then overall result should be "Complete with some items skipped" (! for whatever reason !)
                        {
                            ret = 1;
                        }

                    }
                }
                catch (System.Exception ex)
                {

                }
            }
            return ret;
        }

        private bool CheckIfSettingsAllFail(string resultfile)
        {
            bool ret = false;
            int total = 0;
            int failed = 0;

            if (System.IO.File.Exists(resultfile))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(resultfile);
                    if (doc.DocumentElement != null)
                    {
                        XmlNodeList items = doc.SelectNodes("//Item");
                        foreach (XmlNode item in items)
                        {
                            if (((XmlElement)item).HasAttribute("setting"))
                            {
                                total++;
                                string v = item.Attributes["result"].Value;
                                if (string.Compare(v, "failure", true) == 0)
                                {
                                    failed++;
                                }
                            }
                        }

                        if ((total > 0) && (total == failed))
                        {
                            ret = true;
                        }

                    }
                }
                catch (System.Exception ex)
                {

                }
            }
            return ret;
        }
        string captureLogForTransaction()
        {
            string ret = string.Empty;
            string log_tool = System.IO.Path.Combine(envClass.getInstance().ExePath, "CapLogFile.exe");
            if (System.IO.File.Exists(log_tool))
            {
                // prepare a folder to save logs
                string log_dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), string.Format("label_{0}_log",_label));
                try { System.IO.Directory.CreateDirectory(log_dir); }
                catch (Exception) { }
                // call CapLogFile.exe to save logs into label_{0}_log folder
                // final zip
            }
            return ret;
        }
        public bool updateInfoXmlFromPost(Dictionary<string, string> InfoDic, Dictionary<string, object> attributeDict)
        {
            LogIt("updateInfoXmlFromPost: ++");
            bool bSucess = true;
            try
            {
                lock (_filename)
                {
                    XmlDocument dom = new XmlDocument();
                    dom.Load(_filename);
                    foreach(string key in InfoDic.Keys)
                    {
                        try
                        {
                            string ValueOfKey = InfoDic[key];
                            LogIt(string.Format("updateInfoXmlFromPost: {0} = {1} ", key, ValueOfKey));
                            XmlNode nodeKey = dom.SelectSingleNode(key);
                            if (nodeKey == null)
                            {
                                appendInfoXmlNode(dom, key);
                                nodeKey = dom.SelectSingleNode(key);

                            }
                            if (nodeKey != null)
                            {
                                XmlElement e1 = (XmlElement)nodeKey;
                                e1.SetAttribute("addbypost", System.Boolean.TrueString);
                                nodeKey.InnerText = ValueOfKey;
                                if (attributeDict != null)
                                {
                                    if (attributeDict.ContainsKey(key))
                                    {
                                        object tempobj = attributeDict[key];
                                        Dictionary<string, string> Attribkeymap = JsonConvert.DeserializeObject<Dictionary<string, string>>(tempobj.ToString());
                                        foreach (string attribkey in Attribkeymap.Keys)
                                        {
                                            LogIt(string.Format("updateInfoXmlFromPost:set attribute {0} = {1} ", attribkey, Attribkeymap[attribkey]));
                                            e1.SetAttribute(attribkey, Attribkeymap[attribkey]);
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            LogIt(string.Format("updateInfoXmlFromPost:{0}  exception {1}",key,ex.Message));
                        }
                        
                    }
                    bSucess = envClass.getInstance().saveXml(dom, _filename);
                }
            }
            catch (System.Exception ex)
            {
                bSucess = false;
                LogIt("updateInfoXmlFromPost: exception "+ex.ToString());
            }
            LogIt("updateInfoXmlFromPost: --");
            return bSucess;
        }
        public bool updateInfoXmlFromRequest(System.Collections.Specialized.StringDictionary args)
        {
            bool bSucess = true;
            LogIt("updateInfoXmlFromRequest: ++");
            try
            {
                if (args.ContainsKey("xpath") && args.ContainsKey("value"))
                {
                    LogIt(string.Format("updateInfoXmlFromRequest: xpath={0}, value={1}", args["xpath"], args["value"]));
                    LogIt(string.Format("updateInfoXmlFromRequest: bClickbutton={0}, strNextBXpath={1}", bClickbutton, strNextBXpath));
                    if (bClickbutton)
                    {
                        if (string.Compare(strNextBXpath, args["xpath"], true) == 0)
                        {
                            _status = 46;
                            LogIt(string.Format("updateInfoXmlFromRequest: _status={0}", _status));
                        }
                    }

                    if (hastrigerAllLabelDetect)
                    {
                        if (string.Compare("/labelinfo/runtime/EnableDetect", args["xpath"],true) == 0)
                        {
                            updateRuntimeCurrentStatus(13);
                            _status = 1;
                        }
                        if (string.Compare("/labelinfo/runtime/DetectFailManualEntry",args["xpath"],true) == 0)
                        {
                            _status = 3;
                        }
                        if (string.Compare("/labelinfo/runtime/RetryDetect", args["xpath"], true) == 0)
                        {
                            envClass.getInstance().pauseDetection(_label);
                            updateRuntimeCurrentStatus(13);
                            Thread.Sleep(2 * 1000);
                            _status = 1;
                        }
                    }
                    
                    lock (_filename)
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(_filename);
                        XmlNode n1 = dom.SelectSingleNode(args["xpath"]);
                        if (n1 == null)
                        {
                            // Chris: need create node
                            appendInfoXmlNode(dom, args["xpath"]);
                            n1 = dom.SelectSingleNode(args["xpath"]);
                        }
                        if (n1 != null)
                        {
                            // test
                            XmlElement e1 = (XmlElement)n1;
                            e1.SetAttribute("addbyrequest", System.Boolean.TrueString);
                            //
                            if (args.ContainsKey("attrib"))
                            {
                                if (args.ContainsKey("attribvalue"))
                                {
                                    e1.SetAttribute(args["attrib"], args["attribvalue"]);
                                }
                                else
                                {
                                    LogIt("updateInfoXmlFromRequest:there is no attribvalue parameter, will use value parameter");
                                    e1.SetAttribute(args["attrib"], args["value"]);
                                }
                            }
                            else
                            {
                                n1.InnerText = args["value"];
                            }
                            bSucess = envClass.getInstance().saveXml(dom, _filename);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogIt("updateInfoXmlFromRequest: exception " + ex.ToString());
                bSucess = false;
            }
            LogIt("updateInfoXmlFromRequest: --");
            return bSucess;
        }
        void appendInfoXmlNode(XmlDocument doc, string xpath)
        {
            try
            {
                if (doc != null && doc.DocumentElement != null && !string.IsNullOrEmpty(xpath))
                {
                    string[] paths = xpath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    string parent_path = "/";
                    XmlNode parent_node = doc.DocumentElement;
                    foreach (string path in paths)
                    {
                        string s = string.Format("{0}{1}", parent_path, path);
                        XmlNode n1 = doc.SelectSingleNode(s);
                        if (n1 == null)
                        {
                            // need add
                            XmlNode mslNode = doc.CreateNode(XmlNodeType.Element, path, "");
                            mslNode.InnerText = "";
                            n1 = parent_node.AppendChild(mslNode);
                        }
                        // reset parent information
                        parent_path = string.Format("{0}/",s);
                        parent_node = n1;
                    }
                }
            }
            catch (System.Exception )
            {
           	
            }
        }
        bool handleUserInput(int currentStatus)
        {
            bool ret = false;
            // get xpath from config
            string xpath = envClass.getInstance().GetConfigValueByKey("userinput", "cXpath", string.Empty, "config.ini");
            if (!string.IsNullOrEmpty(xpath))
            {
                try
                {
                    XmlDocument dom=new XmlDocument();
                    dom.Load(_filename);
                    if(dom.DocumentElement!=null)
                    {
                        XmlNode n = dom.DocumentElement.SelectSingleNode(xpath);
                        if (n != null)
                        {
                            string s = n.InnerText;
                            if (!string.IsNullOrEmpty(s))
                            {
                                ret = true;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                	
                }
            }
            return ret;
        }

        int run_app_sync(string appPath, string parameter, out string csOutput, int iTimeOut = 5*60*1000)
        {
            int iRet = -1;
            csOutput = "";
            try
            {
                LogIt(string.Format("will run: {0} {1}", appPath, parameter));
                if (File.Exists(appPath))
                {
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = appPath;
                    proc.StartInfo.Arguments = parameter;
                    proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    LogIt(string.Format("exe ID is - [{0}]", proc.Id));
                    csOutput = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(iTimeOut);
                    iRet = proc.ExitCode;
                    LogIt(string.Format("exe return content - [{0}]", csOutput));
                    LogIt(string.Format("exe return id - [{0}]", iRet));
                }
                else
                    LogIt("exe is not exist");
            }
            catch (System.Exception ex)
            {
                iRet = -1;
                LogIt(string.Format("Run Sync App exception - {0}", ex.ToString()));
            }
            LogIt(string.Format("Run Sync App-- return: {0}", iRet));
            return iRet;
        }

        string recordSpecialId()
        {
            string id = string.Empty;
            Dictionary<string, string> info = readLabelxmlInfo();
            string vid = string.Empty;
            if (info.ContainsKey("vid"))
            {
                vid = info["vid"];
            }
            if (string.Compare(vid, "05ac",true)!=0)
            {
                LogIt("recordSpecialId ++");
                if (info.ContainsKey("hubname") && info.ContainsKey("hubport"))
                {
                    string exePath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "AndroidDeviceCICheck.exe");
                    string para = string.Format("-label={0} -hubname={1} -hubport={2} -recordusb", _label, info["hubname"], Convert.ToInt32(info["hubport"]) );
                    if (File.Exists(exePath))
                    {
                        int nRetry = 3;
                        string temp = string.Empty;
                        while (nRetry > 0)
                        {
                            LogIt(string.Format("recordSpecialId begin nRetry={0}", nRetry));
                            int ret = run_app_sync(exePath, para, out temp);
                            LogIt(string.Format("recordSpecialId end ret={0}", ret));
                            if (ret == 0)
                            {
                                string[] idlist = temp.Split('\n');
                                foreach (string strid in idlist)
                                {
                                    if (strid.IndexOf("uuid=") != -1)
                                    {
                                        id = strid.Substring(5);
                                        LogIt("recordSpecialId " + id);
                                        break;
                                    }

                                }

                                break;
                            }
                            else
                            {
                                nRetry--;
                                Thread.Sleep(2 * 1000);
                            }

                        }

                    }
                }

                LogIt("recordSpecialId --");
                
            }
            

            return id;
        }

        System.Collections.Generic.Dictionary<string, string> readLabelxmlInfo()
        {
            System.Collections.Generic.Dictionary<string, string> info = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(_filename);
                XmlNode deviceNode = doc.SelectSingleNode("/labelinfo/device");

                if (deviceNode != null && deviceNode.HasChildNodes)
                {
                    XmlElement e = (XmlElement)deviceNode;
                    if (e.HasAttribute("vid"))
                    {
                        info.Add("vid", e.Attributes["vid"].Value);
                    }
                    if (e.HasAttribute("pid"))
                    {
                        info.Add("pid", e.Attributes["pid"].Value);
                    }
                    XmlNode bbpinNode = deviceNode.SelectSingleNode("pin");
                    if (bbpinNode != null)
                    {
                        info.Add("bbpin", bbpinNode.InnerText);
                    }
                    XmlNode serialnumber = deviceNode.SelectSingleNode("serialnumber");
                    if (serialnumber != null)
                    {
                        info.Add("serialnumber", serialnumber.InnerText);
                    }
                    XmlNode serialnumber2 = deviceNode.SelectSingleNode("serialnumber2");
                    if (serialnumber2 != null)
                    {
                        info.Add("serialnumber2", serialnumber2.InnerText);
                    }
                }
                
                XmlNode labelNode = doc.SelectSingleNode("/labelinfo/label");
                if (labelNode != null)
                {
                    string labelId = labelNode.Attributes["id"].Value;
                    string calibration = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "calibration.ini");
                    operateINIFile iniObj = new operateINIFile(calibration);
                    string hubnames = iniObj.IniReadValue("label", labelId);
                    int pos = hubnames.IndexOf('@');
                    string hubport = hubnames.Substring(0, pos);
                    string hubname = hubnames.Substring(pos + 1);
                    if (!string.IsNullOrEmpty(hubname) && !string.IsNullOrEmpty(hubport))
                    {
                        info.Add("hubname", hubname);
                        info.Add("hubport", hubport);
                        LogIt(string.Format("hubname={0}, hubport={1}", hubname, hubport));
                    }

                }
            }
            catch (System.Exception ex)
            {
                LogIt(string.Format("Exception: {0}", ex.Message));
            }

            return info;
        }

        int handleAfterTaskDone_3()
        {
            int ret = _status;
            LogIt(string.Format("handleAfterTaskDone_3: {0}++ ",_status));
            // Chris: 
            // if the current device is Android, 
            //    if see the vid/pid, do nothing;
            //    else vid/pid gone, return _status=1;
            // if the current device is BB
            //    if see the vid/pid is not BB; then return _status=1;
            //    else if read BB PIN success and BB PIN != saved BB PIN, then return _status=1;
            //    else do nothing.
            //
            // 1. get information from label_n.xml
            string strHubname = string.Empty;
            int nHubport = 0;
            System.Collections.Generic.Dictionary<string, string> info = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(_filename);
                XmlNode deviceNode = doc.SelectSingleNode("/labelinfo/device");
                
                if (deviceNode!=null && deviceNode.HasChildNodes)
                {
                    XmlElement e = (XmlElement)deviceNode;
                    if (e.HasAttribute("vid"))
                    {
                        info.Add("vid", e.Attributes["vid"].Value);
                    }
                    if (e.HasAttribute("pid"))
                    {
                        info.Add("pid", e.Attributes["pid"].Value);
                    }
                    XmlNode bbpinNode = deviceNode.SelectSingleNode("pin");
                    if (bbpinNode!=null)
                    {
                        info.Add("bbpin", bbpinNode.InnerText);
                    }
                    XmlNode serialnumber = deviceNode.SelectSingleNode("serialnumber");
                    if (serialnumber != null)
                    {
                        info.Add("serialnumber", serialnumber.InnerText);
                    }
                    XmlNode serialnumber2 = deviceNode.SelectSingleNode("serialnumber2");
                    if (serialnumber2 != null)
                    {
                        info.Add("serialnumber2", serialnumber2.InnerText);
                    }
                }
                //XmlNode hubNode = doc.SelectSingleNode("/labelinfo/label/usbhub");
                //if (hubNode!=null)
                //{
                //    XmlElement e = (XmlElement)hubNode;
                //    if (e.HasAttribute("name"))
                //    {
                //        info.Add("hubname", e.Attributes["name"].Value);
                //    }
                //    if (e.HasAttribute("port"))
                //    {
                //        info.Add("hubport", e.Attributes["port"].Value);
                //    }
                //}

                XmlNode labelNode = doc.SelectSingleNode("/labelinfo/label");             
                if (labelNode != null)
                {
                    string labelId = labelNode.Attributes["id"].Value;
                    string calibration = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "calibration.ini");
                    operateINIFile iniObj = new operateINIFile(calibration);
                    string hubnames = iniObj.IniReadValue("label", labelId);
                    int pos = hubnames.IndexOf('@');
                    string hubport = hubnames.Substring(0, pos);
                    string hubname = hubnames.Substring(pos + 1);
                    if (!string.IsNullOrEmpty(hubname) && !string.IsNullOrEmpty(hubport))
                    {
                        info.Add("hubname", hubname);
                        info.Add("hubport", hubport);
                        strHubname = hubname;
                        nHubport = Convert.ToInt32(hubport);
                        LogIt(string.Format("hubname={0}, hubport={1}",hubname,hubport));
                    }

                }
            }
            catch (System.Exception ex)
            {
                LogIt(string.Format("Exception: {0}", ex.Message));
            }
            // 1.5 dump info
            //foreach (KeyValuePair<string, string> kvp in info)
            //{
            //    //LogIt(string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value));
            //}
            try
            {
                // 2. check usb connection
                int vid = -1, pid = -1;
                string serialnumber = "";
                string driverKey = "";
                if (info.ContainsKey("hubname") && info.ContainsKey("hubport"))
                {

                    int retcode =  envClass.getInstance().readHubV2(info["hubname"], Convert.ToInt32(info["hubport"]), ref vid, ref pid,ref serialnumber,ref driverKey, false);
                    if (retcode == 0)
                    {
                        if (vid != -1 && pid != -1)
                        {

                        }
                    }
                    else 
                    {
                        nOpenHubFail++;
                        LogIt(string.Format("read Hubinfo fail = {0}", nOpenHubFail));

                        if (nOpenHubFail < 10)
                        {
                            vid = g_vid;
                            pid = g_pid;
                        }
                        else
                        {
                            if (retcode == 2)
                            {
                                LogIt(string.Format("twController think hub is change because open fail = total {0} times", nOpenHubFail));
                            }
                           
                        }
                    }
                             
                }
                 
                LogIt(string.Format("vid=0x{0:X4}, pid=0x{1:X4}", vid, pid));

                // 3.1 if previous device is BB
                if (info.ContainsKey("vid") && string.Compare(info["vid"], "0fca", true) == 0)
                {
                    if (vid == -1 || vid ==0)
                    {
                        // no device see on usb port;
                        // do nothing,
                    }
                    else if (vid == 0x0fca)
                    {
                        // BB device, let's check the bb pin 
                        // call rim utility to get bbpin
                        string exePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                        if (System.IO.File.Exists(System.IO.Path.Combine(exePath, "RimUtility.exe")))
                        {
                            System.Diagnostics.Process p = new System.Diagnostics.Process();
                            p.StartInfo.FileName = System.IO.Path.Combine(exePath, "RimUtility.exe");
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            p.StartInfo.Arguments = string.Format("-getpin -hubname \"{0}\" -hubport {1}", info["hubname"], info["hubport"]);
                            p.Start();
                            p.WaitForExit();
                            if (p.ExitCode != 0)
                            {
                                string pin = p.ExitCode.ToString();
                                LogIt("bbpin is :" + pin);
                                if (pin.Length>5)
                                {
                                    if (info.ContainsKey("bbpin") && string.Compare(info["bbpin"], pin, true) == 0)
                                    {
                                        // do nothing
                                    }
                                    else
                                    {
                                        // LogIt("1,set status value 1");
                                        _status = 1;
                                    }
                                }
                                
                            }
                        }
                    }
                    else
                    {
                        // different device connectd;
                        //LogIt("2,set status value 1");
                        _status = 1;
                    }
                }
                
                // 3.2 if previous device is android (not BB)
                else if (info.ContainsKey("vid") && string.Compare(info["vid"], "0fca", true) != 0 && string.Compare(info["vid"], "0bb4", true)!= 0)
                {
                    //first time we just record vid and pid to g_vid and g_pid
                    LogIt(string.Format("g_vid=0x{0:X4}, g_pid=0x{1:X4}", g_vid, g_pid));
                    if (vid == 0)
                    {
                        // read wrong vid;
                        // do nothing,
                    }
                    else if (g_vid == MAX_NUM && g_pid == MAX_NUM)
                    {
                        g_vid = vid;
                        g_pid = pid;
                    }
                    else
                    {
                        //compare record vid and pid with current vid and pid, if it change, set status
                        if (vid == g_vid && pid == g_pid)
                        {
                        }
                        else
                        {
                            _status = 1;
                            _previous_device_gone_time = DateTime.Now.ToString("yyyy/MM/dd  HH:mm:ss");
                        }
                    }

                    //if (vid != -1 && pid != -1)
                    //{
                    //    // device is connected
                    //}
                    //else
                    //{
                    //    //LogIt("3,set status value 1");
                    //    _status = 1;
                    //}
                }
                //htc 
                else if (info.ContainsKey("vid") && string.Compare(info["vid"], "0bb4", true) == 0)
                {
                    envClass.getInstance().readHubV2(info["hubname"], Convert.ToInt32(info["hubport"]), ref vid, ref pid, ref serialnumber, ref driverKey, true);
                    if (vid == 0 || vid == -1)
                    {
                    }
                    else if (vid == 0x0bb4)
                    {
                        LogIt(string.Format("Serial number:{0}", serialnumber));
                        if (!string.IsNullOrEmpty(serialnumber))
                        {
                            if ((info.ContainsKey("serialnumber") && string.Compare(serialnumber, info["serialnumber"], true) == 0) || (info.ContainsKey("serialnumber2") && string.Compare(serialnumber, info["serialnumber2"], true) == 0))
                            {
                                //do nothing
                            }
                            else
                            {
                                _status = 1;
                            }
                        }
                    }
                    else
                    {
                        _status = 1;
                    }
                }

                // 3.3 other
                else
                {
                    // 
                    //LogIt("4,set status value 1");
                    _status = 1;
                }
            }
            catch (System.Exception ex)
            {
                LogIt(string.Format("Exception: {0}", ex.Message));
            }

            int nret = -1;
            if (hastrigerAllLabelDetect)
            {
                nret = 10000;
            }
            else
            {
                nret = _status;
            }

            LogIt(string.Format("handleAfterTaskDone_3: -- ret={0}", nret));

            
            return nret;
        }
    }
}
