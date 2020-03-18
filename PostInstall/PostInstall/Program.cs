using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PostInstall
{
    class Program
    {
        static EventLog mlog = null;
        static void logIt(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"[PostInstall]: [{DateTime.Now.ToString("o")}]: {msg}");
        }
        static void dumpArgs(System.Collections.Specialized.StringDictionary args)
        {
            //string s = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            if (!args.ContainsKey("dir"))
            {
                args.Add("dir", System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));
            }
            logIt($"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}: version: {System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.FileVersion}");
            logIt($"Dump args: [{args.Count}]");
            foreach(System.Collections.DictionaryEntry de in args)
            {
                logIt($"{de.Key}={de.Value}");
            }
        }
        static int Main(string[] args)
        {
            int ret = 0;
            if (!System.Diagnostics.EventLog.SourceExists("CMCPostInstallation"))
            {
                System.Diagnostics.EventLog.CreateEventSource("PostInstallation", "CMC");
            }
            try
            {
                mlog = new EventLog("CMC");
                mlog.Source = "CMCPostInstallation";
                mlog.WriteEntry("PostInstallation starts.");
            }
            catch (Exception) { }
            System.Configuration.Install.InstallContext _args = new System.Configuration.Install.InstallContext(null, args);
            Trace.Listeners.Add(new TextWriterTraceListener($"PostInstall-{DateTime.Now.ToString("yyyyMMddTHHmmss")}.log", "myListener"));
            dumpArgs(_args.Parameters);
            if (_args.IsParameterTrue("debug"))
            {
                System.Console.WriteLine("Wait for debugger, press any key to continue...");
                System.Console.ReadKey();
            }
            //ret=start(_args.Parameters);
            test();
            Trace.Flush();
            return ret;
        }
        static void test()
        {
            System.Environment.SetEnvironmentVariable("PSExecutionPolicyPreference", "Bypass");

            string s = System.IO.File.ReadAllText(@"C:\projects\temp\test.ps1");
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                PowerShellInstance.AddScript(@"$PSVersionTable");
                Collection<PSObject> PSOutput1 = PowerShellInstance.Invoke();

                PowerShellInstance.AddScript(@"C:\projects\temp\test.ps1", true);

                //Collection<PSObject> PSOutput = PowerShellInstance.Invoke();
                //foreach (PSObject outputItem in PSOutput)
                //{

                //}
                PSDataCollection<PSObject> outputCollection = new PSDataCollection<PSObject>();
                outputCollection.DataAdded += OutputCollection_DataAdded;
                //PowerShellInstance.Streams.Information.DataAdded += Information_DataAdded;
                //IAsyncResult result = PowerShellInstance.BeginInvoke<PSObject, PSObject>(null, outputCollection);
                IAsyncResult result = PowerShellInstance.BeginInvoke();
                //while (!result.IsCompleted)
                //{
                //    System.Threading.Thread.Sleep(1000);
                //}

                PSDataCollection<PSObject> PSOutput = PowerShellInstance.EndInvoke(result);
                foreach (PSObject outputItem in outputCollection)
                {
                    //TODO: handle/process the output items if required
                    Console.WriteLine(outputItem.BaseObject.ToString());
                }
                if (PowerShellInstance.Streams.Information.Count > 0)
                {
                    foreach(InformationRecord i in PowerShellInstance.Streams.Information)
                    {
                        System.Console.Out.WriteLine($"[{i.TimeGenerated}]: {i.MessageData}");
                    }
                }

            }

        }

        private static void Information_DataAdded(object sender, DataAddedEventArgs e)
        {
            
        }

        private static void OutputCollection_DataAdded(object sender, DataAddedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        static int start(System.Collections.Specialized.StringDictionary args)
          {
            int ret = 0;
            string root = args.ContainsKey("dir") ? args["dir"] : System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);            
            string s = System.IO.Path.Combine(root, "config.ini");
            if (System.IO.File.Exists(s))
            {
                utility.IniFile config = new utility.IniFile(s);
                string pi_type = config.GetString("config", "pitype", "");
                logIt($"pitype={pi_type}");
                string pi_id = config.GetString("config", "piid", "");
                logIt($"piid={pi_id}");
                string sid= config.GetString("config", "solutionid", "");
                logIt($"solutionid={sid}");
                string pid = config.GetString("config", "productid", "");
                logIt($"productid={pid}");
                string server = config.GetString("config", "adminconsoleserver", "");
                logIt($"adminconsoleserver={server}");
                if (string.IsNullOrEmpty(pi_type) || string.IsNullOrEmpty(pi_id) || string.IsNullOrEmpty(sid) ||
                    string.IsNullOrEmpty(server))
                {
                    // no need post installation.
                }
                else
                {
                    try
                    {
                        Uri u = new Uri(new Uri(server), "/api/pkginfo/");
                        NameValueCollection q = new NameValueCollection();
                        q.Add("solutionid", sid);
                        q.Add("type", pi_type);
                        q.Add("pkgid", pi_id);
                        WebClient wc = new WebClient();
                        wc.QueryString = q;
                        s = wc.DownloadString(u);
                        if (!string.IsNullOrEmpty(s))
                        {
                            var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                            Dictionary<string,object> info = jss.Deserialize<Dictionary<string,object>>(s);
                            if (info != null)
                            {
                                if (info.ContainsKey("readableid") && string.Compare(info["readableid"].ToString(), pi_id, true) == 0)
                                {
                                    ret= doPostInstall(info);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        logIt(ex.Message);
                        logIt(ex.StackTrace);
                    }
                }
            }
            else
            {
                logIt($"Error: {s} not exists. ");
                ret = 1;
            }
            return ret;
        }
        static int doPostInstall(Dictionary<string,object> args)                                      
        {
            int ret = 0;
            string temp = System.IO.Path.GetRandomFileName();
            string root = System.IO.Path.GetRandomFileName();
            try
            {
                string url = args?["url"].ToString();
                WebClient wc = new WebClient();
                wc.DownloadFile(url, temp);
                if (System.IO.File.Exists(temp))
                {
                    ZipFile.ExtractToDirectory(temp, root);
                    string s = System.IO.Path.Combine(root, "info.ini");
                    string resource = System.IO.Path.Combine(root, "resource");
                    if (System.IO.File.Exists(s))
                    {
                        utility.IniFile info = new utility.IniFile(s);
                        Process p = new Process();
                        p.StartInfo.FileName = info.GetString("information", "exe", "");
                        p.StartInfo.Arguments=info.GetString("information", "arg", "");
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.WorkingDirectory = resource;
                        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.OutputDataReceived += (e1, e2) => 
                        {
                            if (string.IsNullOrEmpty(e2.Data))
                                logIt($"[output]: {e2.Data}");
                        };
                        p.Start();
                        p.BeginOutputReadLine();
                        p.WaitForExit();
                        ret = p.ExitCode;
                    }
                }
            }
            catch(Exception ex)
            {
                logIt(ex.Message);
                logIt(ex.StackTrace);
                ret = 2;
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(temp);
                    System.IO.Directory.Delete(root, true);
                }
                catch (Exception) { }
            }
            return ret;
        }
    }
}
