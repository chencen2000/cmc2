using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace twController
{
    class ctrlClass
    {
        public const string TetherWingQuitEvent = "TetherWingQuitEvent";
        public static string productV = string.Empty;
        public static bool hasVerify = false;
        private string strVerify = string.Empty;

        public class transactionLogData
        {
            public string sRptFile;
            public string sUser;
            public string sLogZip;
            public string uuid;
            public transactionLogData(string sRptFile, string sUser, string sLogZip)
            {
                this.sRptFile = sRptFile;
                this.sUser = sUser;
                this.sLogZip = sLogZip;
            }
            public transactionLogData(string sRptFile, string sUser, string sLogZip, string suuid)
            {
                this.sRptFile = sRptFile;
                this.sUser = sUser;
                this.sLogZip = sLogZip;
                this.uuid = suuid;
            }
        };

        static private ctrlClass _this = null;
        static public ctrlClass getInstance()
        {
            if (_this==null)
            {
                _this = new ctrlClass();
            }
            return _this;
        }
        private List<monitor> monitors = new List<monitor>();
        private List<transactionLogData> tDataList = new List<transactionLogData>();
        private System.Threading.EventWaitHandle canQuit = null;
        private bool _quit = false;
        private System.Collections.Generic.Dictionary<string, System.Threading.Mutex> taskLock = new System.Collections.Generic.Dictionary<string, System.Threading.Mutex>();
        private System.Collections.Generic.Dictionary<string, System.Threading.Semaphore> taskSemaphore = new System.Collections.Generic.Dictionary<string, System.Threading.Semaphore>();
        public System.Diagnostics.Process _ENV_Check_Process = null;
        private System.Net.HttpListener _listener = null;

        ctrlClass()
        {

        }
        public System.Threading.Semaphore getSemaphoreByKey(string key, int max)
        {
            System.Threading.Semaphore ret = null;
            lock (taskSemaphore)
            {
                if (taskSemaphore.ContainsKey(key))
                {
                    ret = taskSemaphore[key];
                }
                else
                {
                    ret = new System.Threading.Semaphore(max, max);
                    taskSemaphore.Add(key, ret);
                }
            }
            return ret;
        }
        public System.Threading.Mutex getMutexByKey(string key)
        {
            System.Threading.Mutex ret = null;
            lock (taskLock)
            {
                if (taskLock.ContainsKey(key))
                {
                    ret = taskLock[key];
                }
                else
                {
                    ret = new System.Threading.Mutex();
                    taskLock.Add(key, ret);
                }
            }
            return ret;
        }
        public bool prepareEnv()
        {
            bool ret = true;
            // TO-DO: start twPreparation
            ret = twPreParation();
            // prepare info folder
            prepareInfoFolder();
            // Chris: add 08/13/2012
            // prepare logs
            envClass.getInstance().prepareLog();
            return ret;
        }
        public bool start()
        {
            bool ret = true;
            envClass.getInstance().LogIt("Controller: start.");
            try
            {
                // 1. prepare environment
                if (prepareEnv())
                {
                    // Chris: add http listener to open a interface to other modules
                    string proini = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "ProjectVer.ini");
                    IniFile fileINI = new IniFile(proini);
                    string ver = fileINI.GetString("config", "version", "");
                    productV = ver;

                    string configPath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "config.ini");
                    IniFile objini = new IniFile(configPath);
                    strVerify = objini.GetString("Verification", "verify", "false");
                    if (string.Compare(strVerify, "true", true) == 0)
                    {
                        hasVerify = true;
                    }
                    if (_listener == null)
                    {
                        _listener = new System.Net.HttpListener();
                        try
                        {
                            _listener.Prefixes.Add(@"http://+:1210/");
                            _listener.Start();
                            //_listener.BeginGetContext(new AsyncCallback(ctrlListenerCallback), _listener);
                            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(httpListenThread), _listener);
                        }
                        catch (System.Exception ex)
                        {
                            _listener.Close();
                            _listener = null;
                        }
                    }
                    detectionClass.getInstance().start(envClass.getInstance().ExePath);
                    detectionClass.getInstance().detectionEvent += new detectionClass.detectionEventHandler(ctrlClass_detectionEvent);

                    canQuit = new System.Threading.EventWaitHandle(true, System.Threading.EventResetMode.ManualReset, envClass.getInstance().GetConfigValueByKey("config", "quitevent", TetherWingQuitEvent));
                    canQuit.Set();

                    // start send transaction report thread
                    System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(sendTransactionLog), null);
                    //System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(killAdb), null);

                    // start monitor labels.
                    string strTriggerAllLabelDetect = objini.GetString("userinput", "triggerAllLabelDetect", "false");
                    string inipath = Path.Combine(Environment.GetEnvironmentVariable("APSTHOME"), "fdEnableDetect.ini");
                    IniFile iniobj = new IniFile(inipath);
                    
                    for (int i = 0; i < envClass.getInstance().getLicensedPanels(); i++) // start detection only for licensed ports
                    {
                        if (string.Compare(strTriggerAllLabelDetect, "true", true) == 0)
                        {
                            iniobj.WriteValue("detecting", string.Format("Label_{0}", i+1), "false");
                        }
                        monitor m = new monitor(i + 1);
                        m.monitorEvent += new monitor.MonitorEventHandler(m_monitorEvent);
                        monitors.Add(m);
                        m.start();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ret = false;
            }
            return ret;
        }

        void m_monitorEvent(object sender, EventArgs e)
        {
            bool idle = true;
            foreach (monitor m in monitors)
            {
                if (m.Status==8)
                {
                    idle = false;
                    break;
                }
            }
            if (idle) canQuit.Set();
            else canQuit.Reset();
        }

        void ctrlClass_detectionEvent(object sender, detectionEventArgs e)
        {
            if (e!=null && !string.IsNullOrEmpty(e.Result))
            {
                //envClass.getInstance().LogIt("changed");
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(onDetection), e.Result);
                //envClass.getInstance().LogIt(e.Result);
            }
        }
        public bool stop()
        {
            bool ret = true;
            envClass.getInstance().LogIt("Controller: stop.");
            _quit = true;
            try
            {
                if (_listener != null)
                {
                    if (_listener.IsListening)
                    {
                        _listener.Abort();
                    }
                    _listener.Close();
                }
                string exePath = envClass.getInstance().ExePath;
                string exeName = System.IO.Path.Combine(exePath, "twPreparation.exe");
                if (System.IO.File.Exists(exeName))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = exeName;
                    p.StartInfo.Arguments = "-event=onctrlterminate";
                    p.StartInfo.WorkingDirectory = exePath;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    p.Start();
                }
            }
            catch (System.Exception ex)
            {
                ret = false;
            }
            detectionClass.getInstance().stop();
            foreach (monitor m in monitors)
            {
                m.stop();
            }
            return ret;
        }
        private monitor getMonitorByLabel(int label)
        {
            monitor ret = null;
            foreach (monitor m in monitors)
            {
                if (m.Label==label)
                {
                    ret = m;
                    break;
                }
            }
            return ret;
        }
        private bool twPreParation()
        {
            bool ret = true;
            try
            {
                string exePath = envClass.getInstance().ExePath;
                string exeName = System.IO.Path.Combine(exePath, "twPreparation.exe");
                if (System.IO.File.Exists(exeName))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = exeName;
                    p.StartInfo.Arguments = "-event=onctrlstartup";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WorkingDirectory = exePath;
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    p.Start();
                    p.WaitForExit();
                }
            }
            catch (System.Exception ex)
            {
                ret = false;
            }
            return ret;
        }
        private bool ENV_Check()
        {
            bool ret = true;
            try
            {
                string exePath = envClass.getInstance().ExePath;
                string exeName = System.IO.Path.Combine(exePath, "twPreparation.exe");
                if (System.IO.File.Exists(exeName))
                {
                    _ENV_Check_Process = new System.Diagnostics.Process();
                    _ENV_Check_Process.StartInfo.FileName = exeName;
                    _ENV_Check_Process.StartInfo.Arguments = "-event=onctrlstartup";
                    _ENV_Check_Process.StartInfo.CreateNoWindow = true;
                    _ENV_Check_Process.StartInfo.WorkingDirectory = exePath;
                    _ENV_Check_Process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    _ENV_Check_Process.Start();
                }
            }
            catch (System.Exception ex)
            {
                ret = false;
            }
            return ret;
        }
        private void prepareInfoFolder()
        {
            try
            {
                string runtimePath = envClass.getInstance().RuntimePath;
                string infoPath = System.IO.Path.Combine(runtimePath, "info");
                System.IO.Directory.CreateDirectory(infoPath);
                foreach (string s in System.IO.Directory.GetFiles(infoPath, "label_*.xml"))
                {
                    System.IO.File.Delete(s);
                }
            }
            catch (System.Exception ex)
            {
                envClass.getInstance().LogIt("prepareInfoFolder exception " + ex.ToString());
            }
           

        }
        private void onDetection(object obj)
        {
            string result = (string)obj;
            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    XmlDocument dom = new XmlDocument();
                    dom.LoadXml(result);
                    if (dom.DocumentElement!=null)
                    {
                        XmlNodeList nodes = dom.SelectNodes("/detection/label");
                        foreach (XmlNode n in nodes)
                        {
                            try
                            {
                                string id = n.Attributes["id"].Value;
                                int i;
                                if (Int32.TryParse(id,out i))
                                {
                                    monitor m = getMonitorByLabel(i);
                                    if (m!=null)
                                    {
                                        m.onDetection(n.OuterXml);
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                            	
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                	
                }               
            }
        }
        public void sendTransactionLog(transactionLogData _data)
        {
            lock (tDataList)
            {
                tDataList.Add(_data);
            }
        }
        public void sendTransactionLog(object obj)
        {
            DateTime lasttimeSendOffline = DateTime.Now;
            while (!_quit)
            {
                if (tDataList.Count > 0)
                {
                    while (tDataList.Count > 0)
                    {
                        envClass.getInstance().LogIt("There is new data need to transaction");
                        transactionLogData data = null;
                        lock (tDataList)
                        {
                            data = (transactionLogData)tDataList[tDataList.Count - 1];
                            tDataList.RemoveAt(tDataList.Count - 1);
                        }
                        if (data != null)
                        {
                            // Chris: To-Do: here we hard code the url path for now
                            string app = System.IO.Path.Combine(envClass.getInstance().ExePath, "HydraTransaction.exe");
                            if (System.IO.File.Exists(app))
                            {
                                Process parentProcess = Process.GetCurrentProcess();
                                
                                Process p = new Process();
                                p.StartInfo.FileName = app;
                                string uuidTmp = data.uuid;
                                if (!string.IsNullOrEmpty(uuidTmp))
                                {
                                    p.StartInfo.Arguments = string.Format("-add -config=\"{0}\" -xml=\"{1}\" -zip=\"{2}\" -uuid={3}",
                                    System.IO.Path.Combine(envClass.getInstance().ExePath, "config.ini"), data.sRptFile, data.sLogZip, uuidTmp);
                                }
                                else
                                {
                                    p.StartInfo.Arguments = string.Format("-add -config=\"{0}\" -xml=\"{1}\" -zip=\"{2}\"",
                                    System.IO.Path.Combine(envClass.getInstance().ExePath, "config.ini"), data.sRptFile, data.sLogZip);

                                }
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                p.StartInfo.WorkingDirectory = envClass.getInstance().ExePath;
                                envClass.getInstance().LogIt(string.Format("{0}: parentId twController is: {1} run arguments: {2}",p.StartInfo.FileName, parentProcess.Id, p.StartInfo.Arguments));
                                p.Start();
                                envClass.getInstance().LogIt(string.Format("{0}: processid is: {1} ", p.StartInfo.FileName, p.Id));
                                p.WaitForExit();
                                try
                                {
                                    System.IO.File.Delete(data.sRptFile);
                                    System.IO.File.Delete(data.sLogZip);
                                }
                                catch (System.Exception ex)
                                {

                                }
                            }
                            else
                            {
                                if (ClassNetService.SendReport(envClass.getInstance().ServerUrl + "/FDContactService", data.sRptFile, data.sUser, data.sLogZip) == 1)
                                {
                                    try
                                    {
                                        System.IO.File.Delete(data.sRptFile);
                                        System.IO.File.Delete(data.sLogZip);
                                    }
                                    catch (System.Exception ex)
                                    {

                                    }
                                }
                                else
                                {
                                    // Chris: To-Do; how to handle transaction log failure?
                                    //tDataList.Add(data);
                                }
                            }
                        }
                    }
                }
                else
                    System.Threading.Thread.Sleep(1000);
                // send any offline transaction
                DateTime now = DateTime.Now;
                TimeSpan t = now - lasttimeSendOffline;
                if (t.TotalSeconds>60)
                {
                    lasttimeSendOffline = now;
                    ClassNetService.SendOfflineReport(envClass.getInstance().ServerUrl + "/FDContactService", envClass.getInstance().getUserName());
                }
            }
        }
        public int getOfflineTransactionNumber()
        {
            int ret = 0;
            if (!string.IsNullOrEmpty(envClass.getInstance().getUserName()))
            {
                ret = ClassNetService.getOfflineRecordNum(envClass.getInstance().getUserName());
            }
            return ret;
        }
        string readRequestData(System.Net.HttpListenerRequest req)
        {
            string ret = string.Empty;
            if (req != null)
            {
                if (req.HttpMethod == "POST")
                {
                    //int req_size = (int)req.ContentLength64;
                    System.IO.Stream body = req.InputStream;
                    System.Text.Encoding encoding = Encoding.UTF8;
                    System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
                    //System.IO.StreamReader reader = new System.IO.StreamReader(body);
                    //if (reader.Peek() == -1)
                    //    System.Threading.Thread.Sleep(1000);
                    ret = reader.ReadToEnd();
                    // envClass.getInstance().LogIt("Post method request data: " + ret);
                    body.Close();
                    reader.Close();
                }
                else if (req.HttpMethod == "GET")
                {
                    // ?label=1&xpath=/labelinfo/runtime/message&value=this is a message
                    int pos = req.Url.Query.IndexOf('?');
                    ret = req.Url.Query.Substring(pos+1);
                    envClass.getInstance().LogIt("Get method request data: " + ret);
                }
            }
            return ret;
        }
        System.Collections.Specialized.StringDictionary readRequestParam(string reqData)
        {
            System.Collections.Specialized.StringDictionary ret = new System.Collections.Specialized.StringDictionary();
            if (!string.IsNullOrEmpty(reqData))
            {
                foreach (string vp in System.Text.RegularExpressions.Regex.Split(reqData, "&"))
                {
                    string decode = Uri.UnescapeDataString(vp);
                    int pos = decode.IndexOf('=');
                    if (pos > 0)
                    {
                        string k = decode.Substring(0, pos);
                        string v = decode.Substring(pos + 1);
                        if (ret.ContainsKey(k)) ret[k] = v;
                        else ret.Add(k, v);
                    }
                }
            }
            return ret;
        }
        bool handle_PostModifyInfo(string postString)
        {
            bool bRet = true;
            try
            {
                Dictionary<string, string> InfoDic = new Dictionary<string, string>();
                Dictionary<string, object> objDic = new Dictionary<string, object>();
                Dictionary<string, object> AttribDict = new Dictionary<string, object>();
                JObject objStr = (JObject)JsonConvert.DeserializeObject(postString);
                string label = (string)objStr["label"];
                if (!string.IsNullOrEmpty(label))
                {
                    int i = 0;
                    if (Int32.TryParse(label, out i))
                    {
                        if (objStr["data"] != null)
                        {
                            JObject jdata = (JObject)objStr["data"];
                            InfoDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(jdata.ToString());
                        }
                        if (objStr["dataobj"] != null)
                        {
                            JObject jdataobj = (JObject)objStr["dataobj"];
                            objDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(jdataobj.ToString());
                        }
                        if (objStr["attribute"] != null)
                        {
                            JObject jattribute = (JObject)objStr["attribute"];
                            AttribDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jattribute.ToString());
                        }
                        if (InfoDic != null && InfoDic.Count != 0 || (objDic != null && objDic.Count != 0))
                        {
                            if (objDic != null && objDic.Count != 0)
                            {
                                foreach (string key in objDic.Keys)
                                {

                                    object ValueOfKey = objDic[key];
                                    string jsons = JsonConvert.SerializeObject(ValueOfKey);
                                    if (InfoDic.ContainsKey(key))
                                    {
                                        InfoDic[key] = jsons;
                                    }
                                    else
                                    {
                                        InfoDic.Add(key, jsons);

                                    }
                                }

                            }

                            foreach (monitor m in monitors)
                            {
                                if (m.Label == i)
                                {
                                    // found object
                                    bRet = m.updateInfoXmlFromPost(InfoDic, AttribDict);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            bRet = false;
                            envClass.getInstance().LogIt("post data dictionary is empty");
                        }
                    }
                }
                else
                {
                    bRet = false;
                    envClass.getInstance().LogIt("label is empty");
                }

            }
            catch (System.Exception ex)
            {
                bRet = false;
                envClass.getInstance().LogIt("handle_PostModifyInfo exception " + ex.ToString());
            }
            return bRet;
        }
        bool handle_ModifiInfo(System.Collections.Specialized.StringDictionary args)
        {
            // modify info.xml request must have 3 parameters:
            // lable=1
            // xpath=/labelinfo/runtime/message
            // value=any string.
            bool bRet = true;
            if (args.ContainsKey("label") && args.ContainsKey("xpath") && args.ContainsKey("value"))
            {
                int i = 0;
                if (Int32.TryParse(args["label"], out i))
                {
                    foreach (monitor m in monitors)
                    {
                        if (m.Label == i)
                        {
                            // found object
                            bRet = m.updateInfoXmlFromRequest(args);
                            break;
                        }
                    }
                }
            }
            return bRet;
        }
        void ctrlListenerCallback_2(object obj)
        {
            System.Net.HttpListenerContext context = (System.Net.HttpListenerContext)obj;
            try
            {
                bool bModify = false;
                //if (listener.IsListening)
                {
                    //System.Net.HttpListenerContext context = listener.EndGetContext(result);
                    System.Net.HttpListenerRequest request = context.Request;
                    StringBuilder sbForOutput = new StringBuilder();

                    // dump input parameters
                    envClass.getInstance().LogIt(string.Format("ctrlListenerCallback: url={0}", request.Url.ToString()));
                    if (string.Compare("/modifyinfo", request.Url.LocalPath, true) == 0)
                    {
                        // request modify info, reqString is ?label=1&xpath=/labelinfo/runtime/message&value=this is a message
                        string reqString = readRequestData(request);
                        //envClass.getInstance().LogIt(string.Format("ctrlListenerCallback: request={0}", reqString));
                        if (request.HttpMethod == "GET")
                        {
                            System.Collections.Specialized.StringDictionary args = readRequestParam(reqString);
                            if (args.ContainsKey("label") && args.ContainsKey("xpath") && args.ContainsKey("value"))
                            {
                                bModify = handle_ModifiInfo(args);
                            }
                            else
                            {
                                envClass.getInstance().LogIt("label or xpath or value parameter is missing");
                            }
                        }
                        else if(request.HttpMethod== "POST")
                        {
                            bModify = handle_PostModifyInfo(reqString);
                        }
                    }
                    // close 
                    string err = string.Empty;
                    if (bModify)
                    {
                        err = "0";
                    }
                    else
                    {
                        err = "-1";
                    }
                    System.Net.HttpListenerResponse response = context.Response;
                    string responseString = string.Format("error={0}",err);
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                    response.Close();
                }
            }
            catch (System.Exception ex)
            {

            }
        }
        void httpListenThread(object obj)
        {
            System.Net.HttpListener listener = (System.Net.HttpListener)obj;
            if (listener != null)
            {
                while (!_quit)
                {
                    try
                    {
                        System.Net.HttpListenerContext context = _listener.GetContext();
                        System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(ctrlListenerCallback_2), context);
                    }
                    catch (Exception) { }
                }
            }
        }
        void ctrlListenerCallback(IAsyncResult result)
        {
            System.Net.HttpListener listener = (System.Net.HttpListener)result.AsyncState;
            try
            {
                if (listener.IsListening)
                {
                    System.Net.HttpListenerContext context = listener.EndGetContext(result);
                    listener.BeginGetContext(new AsyncCallback(ctrlListenerCallback), listener);
                    System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(ctrlListenerCallback_2), context);
                }
            }
            catch (Exception) { }
        }
        void ctrlListenerCallback_1(IAsyncResult result)
        {
            System.Net.HttpListener listener = (System.Net.HttpListener)result.AsyncState;
            try
            {
                //if (listener.IsListening)
                //{
                //    listener.BeginGetContext(new AsyncCallback(ctrlListenerCallback), listener);
                //}
                if (listener.IsListening)
                {
                    System.Net.HttpListenerContext context = listener.EndGetContext(result);
                    System.Net.HttpListenerRequest request = context.Request;
                    StringBuilder sbForOutput = new StringBuilder();

                    // dump input parameters
                    envClass.getInstance().LogIt(string.Format("ctrlListenerCallback: url={0}", request.Url.ToString()));
                    if (string.Compare("/modifyinfo", request.Url.LocalPath, true) == 0)
                    {
                        // request modify info, reqString is ?label=1&xpath=/labelinfo/runtime/message&value=this is a message
                        string reqString = readRequestData(request);
                        envClass.getInstance().LogIt(string.Format("ctrlListenerCallback: request={0}", reqString));
                        System.Collections.Specialized.StringDictionary args = readRequestParam(reqString);
                        if (args.ContainsKey("label") && args.ContainsKey("xpath") && args.ContainsKey("value"))
                        {
                            handle_ModifiInfo(args);
                        }
                        else
                        {
                            // parameters are missing
                        }
                    }
                    // close 
                    System.Net.HttpListenerResponse response = context.Response;
                    string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();

                    
                }
            }
            catch (System.Exception ex)
            {

            }
            finally
            {
                try
                {
                    if (listener.IsListening)
                    {
                        listener.BeginGetContext(new AsyncCallback(ctrlListenerCallback), listener);
                    }
                }
                catch (Exception) { }
            }
        }
        void killAdb(object o)
        {
            DateTime last_time = DateTime.Now;
            while (!_quit)
            {
                System.Threading.Thread.Sleep(1000);
                TimeSpan t = DateTime.Now - last_time;
                if (t.TotalMinutes > 30)
                {
                    last_time = DateTime.Now;
                    canQuit.WaitOne();
                    // adb.exe kill
                    // adb.exe start
                    try
                    {
                        string exePath = envClass.getInstance().ExePath;
                        string exeName = System.IO.Path.Combine(exePath, "adb.exe");
                        if (System.IO.File.Exists(exeName))
                        {
                            {
                                System.Diagnostics.Process p = new System.Diagnostics.Process();
                                p.StartInfo.FileName = exeName;
                                p.StartInfo.Arguments = "kill-server";
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.WorkingDirectory = exePath;
                                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                p.Start();
                                p.WaitForExit();
                            }
                            exeName = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Windows), "system32", "taskkill.exe");
                            if(System.IO.File.Exists(exeName))
                            {                                
                                System.Diagnostics.Process p = new System.Diagnostics.Process();
                                p.StartInfo.FileName = exeName;
                                p.StartInfo.Arguments = "/F /IM adb.exe /T";
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.WorkingDirectory = exePath;
                                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                p.Start();
                                p.WaitForExit();
                            }
                            {
                                exeName = System.IO.Path.Combine(exePath, "adb.exe");
                                System.Diagnostics.Process p = new System.Diagnostics.Process();
                                p.StartInfo.FileName = exeName;
                                p.StartInfo.Arguments = "start-server";
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.WorkingDirectory = exePath;
                                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                p.Start();
                                p.WaitForExit();
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                    }
                }
            }
        }
    }
}
