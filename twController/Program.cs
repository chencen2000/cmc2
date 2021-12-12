using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;


namespace twController
{
    class Program
    {
        static System.Threading.Mutex _mutes = null;
        static public ResourceManager RM;
        static public string ResourceFolder = (System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)) + "\\Resource";
        static public string Store = string.Empty;
        static public string User = string.Empty;
        static public string dllType = "RPT";
        static bool alreadyStart()
        {
            bool ret = true;
            try
            {
                _mutes = System.Threading.Mutex.OpenExisting("TW_Controller");
            }
            catch (System.Threading.WaitHandleCannotBeOpenedException)
            {
                _mutes = new System.Threading.Mutex(false, "TW_Controller");
                ret = false;
            }
            catch (System.Exception ex)
            {
            	
            }
            return ret;
        }

        static void Main_old(string[] args)
        {

            if (alreadyStart())
            {
                System.Console.WriteLine("already started...");
                System.Diagnostics.Trace.WriteLine("already started...");
            }
            else
            {
                System.Configuration.Install.InstallContext commandLine = new System.Configuration.Install.InstallContext(null, args);
                if (commandLine.IsParameterTrue("debug"))
                {
                    System.Console.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                }
                envClass.getInstance().setParameters(commandLine);
                System.Diagnostics.Process ui = null;
                if (commandLine.Parameters.ContainsKey("ppid"))
                {
                    try
                    {
                        int i;
                        Int32.TryParse(commandLine.Parameters["ppid"], out i);
                        ui = System.Diagnostics.Process.GetProcessById(i);
                    }
                    catch (System.Exception ex)
                    {

                    }
                }
                // wait for terminate
                bool quit = false;
                ctrlClass.getInstance().start();
                //ui = ctrlClass.getInstance()._ENV_Check_Process;
                System.Console.WriteLine("Press the 'x' key to terminate...");
                while (!quit)
                {
                    if (ui != null)
                    {
                        if (ui.HasExited)
                        {
                            quit = true;
                        }
                    }
                    if (System.Console.KeyAvailable)
                    {
                        System.ConsoleKeyInfo k = System.Console.ReadKey();
                        if (k.Key == System.ConsoleKey.X)
                        {
                            quit = true;
                        }
                    }
                    if (!quit)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                ctrlClass.getInstance().stop();
            }
        }

        static int doProfileSelect(string psuiexe)
        {
            int ret = 1;
            System.Diagnostics.Process psui = new System.Diagnostics.Process();
            psui.StartInfo.FileName = psuiexe;
            psui.StartInfo.Arguments = string.Format("-profilepool=\"{0}\" -icss=\"{1}\"", envClass.getInstance().getProfilePoolFolder(), System.IO.Path.Combine(envClass.getInstance().ExePath, "icss.xml"));
            psui.StartInfo.UseShellExecute = false;
            psui.StartInfo.RedirectStandardOutput = true;
            psui.Start();
            string line = null;
            while ((line = psui.StandardOutput.ReadLine()) != null)
            {
                envClass.getInstance().LogIt(string.Format("{0}: {1}", System.IO.Path.GetFileName(psuiexe), line));
                if (line.StartsWith("[report]", StringComparison.OrdinalIgnoreCase))
                {
                    string s = line.Substring(8);
                    string[] ss = s.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Length==2)
                    {
                        envClass.getInstance().setParameter(ss[0], ss[1]);
                    }
                }
            }
            psui.WaitForExit();
            ret = psui.ExitCode;
            envClass.getInstance().LogIt(string.Format("{0}: ret={1}", System.IO.Path.GetFileName(psuiexe), ret.ToString()));
            return ret;
        }
       static public  Dictionary<string, string> saveParameters(System.Collections.Specialized.StringDictionary arg)
        {
            Dictionary<string, string> _args = new Dictionary<string, string>();
            foreach (string key in arg.Keys)
            {
                if (_args.ContainsKey(key))
                    _args[key] = arg[key];
                else
                    _args.Add(key, arg[key]);
            }
            return _args;
        }
        static System.Diagnostics.Process launchMainUI(string uiExe)
        {
            System.Diagnostics.Process ret = null;
            string exe = uiExe;
            string info = System.IO.Path.Combine(envClass.getInstance().RuntimePath, "info");
            System.IO.Directory.CreateDirectory(info);
            if (System.IO.File.Exists(exe))
            {
                // wait other uiExe quit;
                System.Diagnostics.Process[] pp = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(exe));
                foreach (System.Diagnostics.Process p in pp)
                {
                    p.WaitForExit();
                }
                ret = new System.Diagnostics.Process();
                ret.StartInfo.FileName = exe;
                int labels = envClass.getInstance().getLicensedPanels();
                /*
                 * send labels number to UI, UI will default show 9/12/24
                 * if send labels=7 to UI, UI will show 9 windows and latest 2 will show "Not Calibration"
                if (string.Compare(envClass.getInstance().getStatusString(9), "89", true) != 0)
                {
                    // if can't found : [status]9=89
                    if (labels <= 9)
                        labels = 9;
                    else if (labels > 9 && labels <= 12)
                        labels = 12;
                    else
                        labels = 24;
                }
                */
                //string ui_title = envClass.getInstance().GetConfigValueByKey("config", "uititle", "", "config.ini");
                string para = string.Format("-path=\"{0}\" -max={1} -quitevent={2} -{3}",
                    info, labels.ToString(), envClass.getInstance().GetConfigValueByKey("config", "quitevent", ctrlClass.TetherWingQuitEvent),
                    envClass.getInstance().CommandLine.Parameters.ContainsKey("product") ? envClass.getInstance().CommandLine.Parameters["product"] : "sts");

                Dictionary<string, string> args = saveParameters(envClass.getInstance().CommandLine.Parameters); 
                StringBuilder command_line = new StringBuilder();
                command_line.Append(para);
                foreach (string key in args.Keys)
                {
                    command_line.Append(" ");
                    if (string.Compare(key, "product", true) == 0)
                    {
                        
                    }
                    else
                    {
                        string s = string.Format("-{0}=\"{1}\"", key, args[key]);
                        command_line.Append(s);
                    }
                }

               
                //ret.StartInfo.Arguments = string.Format("-path=\"{0}\" -max={1} -quitevent={2} -{3} {4}",
                //    info, labels.ToString(), envClass.getInstance().GetConfigValueByKey("config", "quitevent", ctrlClass.TetherWingQuitEvent),
                //    envClass.getInstance().CommandLine.Parameters.ContainsKey("product") ? envClass.getInstance().CommandLine.Parameters["product"] : "sts",
                //    (string.IsNullOrEmpty(ui_title) ? "" : string.Format("-title=\"{0}\"",ui_title)));
                ret.StartInfo.Arguments = command_line.ToString();
                envClass.getInstance().LogIt(string.Format("Lunch UI {0} with: {1}", ret.StartInfo.FileName, ret.StartInfo.Arguments));
                ret.StartInfo.RedirectStandardOutput = true;
                ret.StartInfo.UseShellExecute = false;
                ret.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(UI_OutputDataReceived);
                ret.Start();
                ret.BeginOutputReadLine();
                ret.WaitForInputIdle(5000);
            }

            return ret;
        }

        static void UI_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            //throw new NotImplementedException();
            if (!string.IsNullOrEmpty(e.Data))
            {
                envClass.getInstance().LogIt(string.Format("[UI out]: {0}", e.Data));
                if (e.Data.StartsWith("[report]"))
                {
                    string s = e.Data.Substring(8);
                    string[] ss = s.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Length == 2)
                    {
                        envClass.getInstance().setParameter(ss[0].Trim(), ss[1].Trim());
                    }
                }
            }
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {

                envClass.getInstance().LogIt("Unknown Exception happen\n");
            }
            catch (System.Exception)
            {

            }
            envClass.getInstance().LogIt("End with Unknown Exception" + e.ToString());
            System.Environment.Exit(10000);
        }
        static void Main(string[] args)
        {
            
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            RM = ResourceManager.CreateFileBasedResourceManager("resource", ResourceFolder, null);
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            if (alreadyStart())
            {
                envClass.getInstance().LogIt("already started...");
            }
            else
            {
                System.Configuration.Install.InstallContext commandLine = new System.Configuration.Install.InstallContext(null, args);
                if (commandLine.IsParameterTrue("debug"))
                {
                    System.Console.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                }
                envClass.getInstance().setParameters(commandLine);
                Store = commandLine.Parameters["store"];
                User = commandLine.Parameters["user"];

                bool quit = false;
                while (!quit)
                {
                    bool profile_selected = true;
                    // 1. first loop for profile selection
                    if (!string.IsNullOrEmpty(envClass.getInstance().GetConfigValueByKey("config", "ProfileSelectionUI", "")))
                    {
                        string ps_ui_exe = System.IO.Path.GetFullPath(envClass.getInstance().GetConfigValueByKey("config", "ProfileSelectionUI", ""));
                        if (!string.IsNullOrEmpty(ps_ui_exe) && System.IO.File.Exists(ps_ui_exe))
                        {
                            if (doProfileSelect(ps_ui_exe) == 0)
                            {
                                // ok, user select a profile,
                                // let's continue
                            }
                            else
                            {
                                // user close the UI
                                // let's exit
                                profile_selected = false;
                                quit = true;
                            }
                        }
                    }
                    // 2. main operation UI
                    if (profile_selected)
                    {
                        System.Diagnostics.Process ui = launchMainUI(envClass.getInstance().CommandLine.Parameters["ui"]);

                        ctrlClass.getInstance().start();
                        System.Console.WriteLine("Press the 'x' key to terminate...");
                        while (!quit && ui!=null)
                        {
                            if (ui.HasExited)
                            {
                                envClass.getInstance().LogIt("Quit because of UI exited.");
                                quit = true;
                            }
                            if (System.Console.KeyAvailable)
                            {
                                System.ConsoleKeyInfo k = System.Console.ReadKey();
                                if (k.Key == System.ConsoleKey.X)
                                {
                                    quit = true;
                                }
                            }
                            if (!quit)
                            {
                                System.Threading.Thread.Sleep(1000);
                            }
                            else
                            {
                                if (!ui.HasExited)
                                {
                                    ui.CloseMainWindow();
                                }
                            }
                        }
                        ctrlClass.getInstance().stop();
                    }
                }
            }
        }
    }
}
