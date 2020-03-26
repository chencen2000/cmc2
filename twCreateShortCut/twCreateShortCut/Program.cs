using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace twCreateShortCut
{
    class Program
    {
        public static void logIt(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"[twCreateShortCut]: {msg}");
        }

        static void Main(string[] args)
        {
            string fn = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("apsthome"), "createshortcut.ps1");
            if (System.IO.File.Exists(fn))
            {
                run_powershell(fn, "");
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

    }
}
