using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace twController
{
    class detectionEventArgs : EventArgs
    {
        string _result = string.Empty;
        public string Result
        {
            get { return _result; }
        }
        public detectionEventArgs(string s)
        {
            _result = s;
        }
    }
    class detectionClass
    {
        static private detectionClass _this = null;
        static public detectionClass getInstance()
        {
            if (_this==null)
            {
                _this = new detectionClass();
            }
            return _this;
        }
        detectionClass()
        {
            detection_result=System.IO.Path.Combine(envClass.getInstance().RuntimePath, "info", "detection.xml");
            detection_watcher = new System.IO.FileSystemWatcher();
            detection_watcher.Path = System.IO.Path.GetDirectoryName(detection_result);
            detection_watcher.Filter = System.IO.Path.GetFileName(detection_result);
            detection_watcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
            detection_watcher.Changed += new System.IO.FileSystemEventHandler(detection_watcher_Changed);
            detection_watcher.EnableRaisingEvents = true;

        }

        public delegate void detectionEventHandler(object sender, detectionEventArgs e);
        public event detectionEventHandler detectionEvent;

        void detection_watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            try
            {
                if (e != null)
                {
                    if (string.Compare(e.Name, System.IO.Path.GetFileName(detection_result), true) == 0)
                    {
                        if (detectionEvent != null)
                        {
                            string s = string.Empty;
                            using (StreamReader sr = new StreamReader(e.FullPath))
                            {
                                s = sr.ReadToEnd();
                            }
                            if (!string.IsNullOrEmpty(s))
                            {
                                int hash = s.GetHashCode();
                                //envClass.getInstance().LogIt(string.Format("{0} vs {1}", hash_detection_result, hash));
                                if (hash_detection_result!=hash)
                                {
                                    hash_detection_result = hash;
                                    detectionEvent(this, new detectionEventArgs(s));
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                envClass.getInstance().LogIt(ex.Message);
            }
        }
        private string detection_result = string.Empty;
        private int hash_detection_result = 0;
        private System.Diagnostics.Process _detection = null;
        private System.IO.FileSystemWatcher detection_watcher = null;
        private bool _quit = false;
        private System.Threading.Thread _monitor_detect;

        public bool start(string dir)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(_start), dir);
            return true;
        }
        void _start(object obj)
        {
            string dir = (string)obj;
            // delay 5 seconds
            System.Threading.Thread.Sleep(5000);
            //string sHost = System.IO.Path.Combine(envClass.getInstance().ExePath, "fdAutoDetect.exe");
            string sHost = System.IO.Path.Combine(dir, "fdAutoDetect.exe");
            if (System.IO.File.Exists(sHost) && _detection == null)
            {
                _detection = new System.Diagnostics.Process();
                _detection.StartInfo.FileName = sHost;
                _detection.StartInfo.Arguments = string.Format("-cal=\"{0}\" -icss=\"{1}\" -output=\"{2}\" -dlltype={3} -ppid={4}",
                    System.IO.Path.Combine(dir, "calibration.ini"), System.IO.Path.Combine(dir, "icss.xml"), System.IO.Path.Combine(envClass.getInstance().RuntimePath, "info", "detection.xml"),
                    envClass.getInstance().GetConfigValueByKey("config", "dlltype", Program.dllType), System.Diagnostics.Process.GetCurrentProcess().Id);
                _detection.StartInfo.CreateNoWindow = true;
                _detection.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                _detection.StartInfo.WorkingDirectory = envClass.getInstance().ExePath;
                envClass.getInstance().LogIt(string.Format("Start detection: \"{0}\" {1}", _detection.StartInfo.FileName, _detection.StartInfo.Arguments));
                _detection.Start();

                // start monitor AutoDetect thread, add by steven
                System.Threading.Thread monitor_detect = new System.Threading.Thread(new System.Threading.ThreadStart(startMonitorDetect));
                monitor_detect.Name = "Monitor_AutoDetect";
                monitor_detect.IsBackground = true;
                monitor_detect.Start();
            }
        }
        public bool stop()
        {
            _quit = true;
            bool ret = false;
            return ret;
        }
        void startMonitorDetect()
        {
            while (!_quit)
            {
                System.Threading.Thread.Sleep(1000);
                if (_detection != null && _detection.HasExited)
                {
                    string sHost = System.IO.Path.Combine(envClass.getInstance().ExePath, "fdAutoDetect.exe");
                    _detection = null;
                    _detection = new System.Diagnostics.Process();
                    _detection.StartInfo.FileName = sHost;
                    _detection.StartInfo.Arguments = string.Format("-cal=\"{0}\" -icss=\"{1}\" -output=\"{2}\" -dlltype={3} -ppid={4}",
                        System.IO.Path.Combine(envClass.getInstance().ExePath, "calibration.ini"), System.IO.Path.Combine(envClass.getInstance().ExePath, "icss.xml"), System.IO.Path.Combine(envClass.getInstance().RuntimePath, "info", "detection.xml"),
                        envClass.getInstance().GetConfigValueByKey("config", "dlltype", Program.dllType), System.Diagnostics.Process.GetCurrentProcess().Id);
                    _detection.StartInfo.CreateNoWindow = true;
                    _detection.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    _detection.StartInfo.WorkingDirectory = envClass.getInstance().ExePath;
                    _detection.Start();
                }
            }
        }
    }
}
