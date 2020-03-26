using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TaskEng
{
    [ServiceContract]
    public interface ITaskEngine
    {
        [OperationContract]
        [WebGet(UriTemplate = "info")]
        Stream get_info();
        
        [OperationContract]
        [WebGet(UriTemplate = "uichange")]
        Stream get_uichange();

        [OperationContract]
        [WebInvoke(UriTemplate = "debug")]
        Stream debug(Stream ins);
    }
    class Program : ITaskEngine
    {
        static string NAME = "TaskEngineWebService";
        static System.Collections.Generic.Dictionary<string, object> Args = new Dictionary<string, object>();
        static void logIt(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"[{NAME}]: {msg}");
        }
        static void Main(string[] args)
        {
            System.Configuration.Install.InstallContext _args = new System.Configuration.Install.InstallContext(null, args);
            if (_args.IsParameterTrue("debug"))
            {
                System.Console.Out.WriteLine("Wait for debugger, press any key to continue...");
                System.Console.ReadKey();
            }
            System.Environment.SetEnvironmentVariable("PSExecutionPolicyPreference", "Bypass");
            logIt($"start: ++ version={System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.FileVersion}");
            load_config(_args.Parameters);
            if (_args.IsParameterTrue("start-service")) 
            {
                bool own = false;
                System.Threading.EventWaitHandle evt = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset, NAME, out own);
                if (own)
                {
                    start(evt);
                }
                else
                {
                    logIt($"Service already starts.");
                }
            }
            else if (_args.IsParameterTrue("kill-service"))
            {
                try
                {
                    System.Threading.EventWaitHandle evt = System.Threading.EventWaitHandle.OpenExisting(NAME);
                    evt.Set();
                }
                catch (Exception) { }
            }
            else
            {
                test();
            }
        }
        static void load_config(System.Collections.Specialized.StringDictionary args)
        {
            var app = ConfigurationManager.AppSettings;
            foreach(var k in app.AllKeys)
            {
                Args[k] = app[k];
            }
            foreach(System.Collections.DictionaryEntry de in args)
            {
                Args[de.Key.ToString()] = de.Value;
            }
            if (!Args.ContainsKey("workdir"))
            {
                Args.Add("workdir", System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));
            }
            if (!Args.ContainsKey("port"))
                Args["port"] = "1210";
            // dump args
            logIt($"dump args: ");
            foreach(KeyValuePair<string,object> kvp in Args)
            {
                logIt($"{kvp.Key} = {kvp.Value}");
            }
        }
        static void test()
        {
            string s = System.IO.Path.Combine(Args["workdir"].ToString(), Args["taskfolder"].ToString(), "get-info.ps1");
            //Runspace runspace = RunspaceFactory.CreateRunspace();
            //runspace.Open();
            //Pipeline pipeline = runspace.CreatePipeline();
            //Command command = new Command(s);
            //command.Parameters.Add("version", "1.2.3.4");
            //pipeline.Commands.Add(command);
            ////pipeline.Output.DataReady += Output_DataReady;
            //System.Collections.ObjectModel.Collection<PSObject> PSOutput = pipeline.Invoke();
            //var v = pipeline.Output.ReadToEnd();
            //runspace.Close();
            //PowerShell ps = PowerShell.Create().AddCommand(s).AddParameter("version", "1.0.0.0");
            //System.Collections.ObjectModel.Collection<PSObject> PSOutput = ps.Invoke();
            Dictionary<string, object> d = new Dictionary<string, object>();
            d.Add("version", "1.1.1.1");
            call_psscript("get-info", d);
        }

        static void start(System.Threading.EventWaitHandle quit)
        {
            try
            {
                Uri baseAddress = new Uri(string.Format("http://localhost:{0}/", Args["port"]));
                WebServiceHost svcHost = new WebServiceHost(typeof(Program), baseAddress);
                WebHttpBinding b = new WebHttpBinding();
                b.Name = NAME;
                b.HostNameComparisonMode = HostNameComparisonMode.Exact;
                svcHost.AddServiceEndpoint(typeof(ITaskEngine), b, "");
                logIt("WebService is running");
                svcHost.Open();
                quit.WaitOne();
                svcHost.Close();
            }
            catch (Exception ex) 
            {
                logIt(ex.Message);
                logIt(ex.StackTrace);
            }
        }
        static object call_psscript(string fn, Dictionary<string,object> args)
        {
            object ret = null;
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                string s = System.IO.Path.Combine(Args["workdir"].ToString(), Args["taskfolder"].ToString(), $"{fn}.ps1");
                PowerShellInstance.AddCommand(s);
                foreach(KeyValuePair<string,object> kvp in args)
                {
                    PowerShellInstance.AddParameter(kvp.Key, kvp.Value);
                }
                IAsyncResult result = PowerShellInstance.BeginInvoke();
                PSDataCollection<PSObject> PSOutput = PowerShellInstance.EndInvoke(result);
                if (PSOutput.Count > 0)
                {
                    ret = PSOutput.Last<PSObject>().BaseObject;
                }
            }
            return ret;
        }
        #region debug

        #endregion

        #region ITaskEngine
        public Stream get_info()
        {
            Stream ret = null;
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("version", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.FileVersion);
                //dic.Add("error", 0);
                //JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                //string s = jss.Serialize(dic);
                object o = call_psscript("get-info", dic);
                System.ServiceModel.Web.WebOperationContext op = System.ServiceModel.Web.WebOperationContext.Current;
                op.OutgoingResponse.Headers.Add("Content-Type", "application/json");
                ret = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(o.ToString()));
            }
            catch (Exception) { }
            return ret;
        }
        public Stream get_uichange()
        {
            Stream ret = null;
            try
            {
                if (System.Threading.Monitor.TryEnter(this))
                {
                    // hold 60 seconds
                    DateTime _start = DateTime.Now;
                    bool done = false;
                    while(!done && (DateTime.Now - _start).TotalSeconds < 60)
                    {
                        // for debug
                        try
                        {
                            string s = System.IO.File.ReadAllText(Args["testdata"] as string);
                            JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                            Dictionary<string, object> test_data = jss.Deserialize<Dictionary<string, object>>(s);
                            if (test_data.ContainsKey("uichange") && test_data["uichange"].GetType() == typeof(Dictionary<string, object>))
                            {
                                Dictionary<string, object> uichange = (Dictionary<string, object>)test_data["uichange"];
                                if (uichange.Count > 0)
                                {
                                    uichange["error"] = 0;
                                    s = jss.Serialize(uichange);
                                    ret = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(s));
                                    done = true;
                                    test_data["uichange"] = new Dictionary<string, object>();
                                    s = jss.Serialize(test_data);
                                    System.IO.File.WriteAllText(Args["testdata"] as string, s);
                                }
                            }
                        }
                        catch (Exception) { }

                        if (!done)
                            System.Threading.Thread.Sleep(1000);
                    }
                }
                else
                {
                    logIt("Other thread already monitoring.");
                }
            }
            catch (Exception) { }
            finally
            {
                try
                {
                    System.Threading.Monitor.Exit(this);
                }
                catch (Exception) { }
            }
            System.ServiceModel.Web.WebOperationContext.Current.OutgoingResponse.Headers.Add("Content-Type", "application/json");
            return ret;
        }
        public Stream debug(Stream ins)
        {
            Stream ret = null;
            try
            {
                using(StreamReader sr = new StreamReader(ins))
                {
                    string s = sr.ReadToEnd();
                    System.Web.Script.Serialization.JavaScriptSerializer jss = new JavaScriptSerializer();
                    Dictionary<string, object> data = jss.Deserialize<Dictionary<string, object>>(s);

                }
            }
            catch (Exception) { }
            return ret;
        }
        #endregion
    }
}
