using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace twLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void logIt(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"[twLauncher]: {msg}");
        }
        void runFromAnotherFolder()
        {
            string dir = System.Environment.GetEnvironmentVariable("apsthome");
            string fn = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.ToLower();
            if (!string.IsNullOrEmpty(dir))
            {
                if (fn.StartsWith(dir.ToLower()))
                {
                    dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), "Futuredial");
                    System.IO.File.Copy(fn, System.IO.Path.Combine(dir, "twLauncher.exe"), true);
                    var SelfProc = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(fn),
                        FileName = System.IO.Path.Combine(dir, "AviaUI.exe"),
                        Verb = "runas"
                    };
                    try
                    {
                        Process.Start(SelfProc);
                        logIt("runFromAnotherFolder: Run from futuredial folder.");
                        Shutdown(0);
                    }
                    catch
                    {
                        logIt("Unable to elevate!");
                        Shutdown(2);
                    }
                }
            }
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            System.Configuration.Install.InstallContext _args = new System.Configuration.Install.InstallContext(null, e.Args);
            if (_args.IsParameterTrue("debug"))
            {
                MessageBox.Show("Wait for debugger");
            }

            runFromAnotherFolder();
            MySplashScreen ss = new MySplashScreen();
            bool start_splashscreen = true;
            string fn_startui = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("apsthome"), "startui.ps1");
            if (!System.IO.File.Exists(fn_startui))
            {
                //start_splashscreen = false;
                // check if download complete
                string fn = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("apsthome"), "FDAutoUpdate.ini");
                if(System.IO.File.Exists(fn))
                {
                    utility.IniFile ini = new utility.IniFile(fn);
                    int i = ini.GetInt32("config", "status", 0);
                    if (i == 12)
                    {
                        // download complete, ready to deploy
                        start_splashscreen = true;
                    }
                }
            }
            if (start_splashscreen)
            {
                ss.Show();
                // 1. deploy
                // 2. start ui
                new TaskFactory().StartNew(new Action<object>((o1) =>
                {
                    MySplashScreen mss = (MySplashScreen)o1;
                    // deploy
                    mss.setStatusText("Deploy the update ...");
                    System.Threading.Thread.Sleep(5000);
                    deployment();
                    // launch ui
                    //string fn = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("apsthome"), "startui.ps1");
                    int ret = App.run_powershell(fn_startui, "");
                    if (ret != 0)
                    {
                        MessageBox.Show("Error to launch the main UI.");
                    }
                    this.Dispatcher.Invoke(delegate { mss.Close(); });
                }), ss);
            }
            else
            {
                MessageBox.Show("Please wait for downloading complete.");
                Shutdown(1);
            }
        }

        public static int run_powershell(string script, string args)
        {
            //string script = @"C:\projects\powershell\proj1\test.ps1";
            //string args = "123";
            int ret = 2;
            if (System.IO.File.Exists(script))
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = "Powershell.exe";
                p.StartInfo.Arguments = $"-ExecutionPolicy Bypass -file \"{script}\" {args}";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null)
                    {
                        // pipe has been terminated
                    }
                    else
                    {
                        // 
                        logIt(e.Data);
                    }
                };
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();
                ret = p.ExitCode;
            }
            return ret;
        }

        void deployment()
        {
            string fn = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("apsthome"), "deploy.ps1");
            if (System.IO.File.Exists(fn))
            {
                string cp = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), "Futuredial", "deploy.ps1");
                System.IO.File.Copy(fn, cp, true);
                App.run_powershell(cp, "");
            }
            else
            {
                fn = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), "Futuredial", "FDAcorn.exe");
                if (System.IO.File.Exists(fn))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = fn;
                    p.StartInfo.Arguments = "-UpdateEnv";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
                    {
                        if (e.Data == null)
                        {
                            // pipe has been terminated
                        }
                        else
                        {
                            // 
                            logIt(e.Data);
                        }
                    };
                    p.StartInfo.RedirectStandardOutput = true;
                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                }
            }
            // continue to download
            {
                fn = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("APSTHOME"), "FDAcorn.exe");
                Process proc = new Process();
                proc.StartInfo.FileName = fn;
                proc.StartInfo.Arguments = "-StartDownLoad";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                proc.Start();
            }
        }
    }
}
