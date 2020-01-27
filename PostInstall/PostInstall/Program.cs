using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostInstall
{
    class Program
    {        
        static void logIt(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"[PostInstall]: [{DateTime.Now.ToString("o")}]: {msg}");
        }
        static void dumpArgs(System.Collections.Specialized.StringDictionary args)
        {
            //string s = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            logIt($"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}: version: {System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.FileVersion}");
            logIt($"Dump args: [{args.Count}]");
            foreach(System.Collections.DictionaryEntry de in args)
            {
                logIt($"{de.Key}={de.Value}");
            }
        }
        static void Main(string[] args)
        {
            System.Configuration.Install.InstallContext _args = new System.Configuration.Install.InstallContext(null, args);
            Trace.Listeners.Add(new TextWriterTraceListener($"PostInstall-{DateTime.Now.ToString("yyyyMMddTHHmmss")}.log", "myListener"));
            dumpArgs(_args.Parameters);
            if (_args.IsParameterTrue("debug"))
            {
                System.Console.WriteLine("Wait for debugger, press any key to continue...");
                System.Console.ReadKey();
            }

            Trace.Flush();
        }
    }
}
