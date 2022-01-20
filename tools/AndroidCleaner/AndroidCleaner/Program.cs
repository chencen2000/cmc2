using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidCleaner
{
    internal class Program
    {
        static String TAG = "AndroidCleaner";
        static void logIt(String msg)
        {
            string s = $"[{DateTime.Now.ToString("o")}] [{TAG}]: {msg}";
            System.Console.WriteLine(s);
            System.Diagnostics.Trace.WriteLine(s);
        }
        static Tuple<string[], string[]> runExe_v2(string exeFilename, string param, out int exitCode, System.Collections.Specialized.StringDictionary env = null, int timeout = 60 * 1000)
        {
            List<string> stdout = new List<string>();
            List<string> errout = new List<string>();
            exitCode = 1;
            logIt(string.Format("[runExe]: ++ exe={0}, param={1}", exeFilename, param));
            try
            {
                if (System.IO.File.Exists(exeFilename))
                {
                    System.Threading.AutoResetEvent ev_stdout = new System.Threading.AutoResetEvent(false);
                    System.Threading.AutoResetEvent ev_stderr = new System.Threading.AutoResetEvent(false);
                    Process p = new Process();
                    p.StartInfo.FileName = exeFilename;
                    p.StartInfo.Arguments = param;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    if (env != null && env.Count > 0)
                    {
                        foreach (DictionaryEntry de in env)
                        {
                            p.StartInfo.EnvironmentVariables.Add(de.Key as string, de.Value as string);
                        }
                    }
                    p.OutputDataReceived += (obj, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            logIt(string.Format("[runExe]: [stdout]: {0}", args.Data));
                            stdout.Add(args.Data);
                        }
                        if (args.Data == null)
                            ev_stdout.Set();
                    };
                    p.ErrorDataReceived += (obj, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            logIt(string.Format("[runExe]: [stderr]: {0}", args.Data));
                            errout.Add(args.Data);
                        }
                        if (args.Data == null)
                            ev_stderr.Set();
                    };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    if (p.WaitForExit(timeout))
                    {
                        ev_stdout.WaitOne(timeout);
                        ev_stderr.WaitOne(timeout);
                        if (!p.HasExited)
                        {
                            exitCode = 1460;
                            p.Kill();
                        }
                        else
                            exitCode = p.ExitCode;
                    }
                    else
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                        }
                        exitCode = 1460;
                    }
                }
            }
            catch (Exception ex)
            {
                logIt(string.Format("[runExe]: {0}", ex.Message));
                logIt(string.Format("[runExe]: {0}", ex.StackTrace));
            }
            logIt(string.Format("[runExe]: -- ret={0}", exitCode));
            return new Tuple<string[], string[]>(stdout.ToArray(), errout.ToArray());
        }
        static int Main(string[] args)
        {
            int ret = -1;
            System.Configuration.Install.InstallContext _args = new System.Configuration.Install.InstallContext(null, args);
            if (_args.IsParameterTrue("debug"))
            {
                System.Console.WriteLine("Press any key to continue.");
                System.Console.ReadKey();
            }
            string adb = "";
            if (_args.Parameters.ContainsKey("adb"))
            {
                if (System.IO.File.Exists(_args.Parameters["adb"]))
                {
                    adb = _args.Parameters["adb"];
                }
            }
            if (string.IsNullOrEmpty(adb))
            {
                // search for adb.exe
                ret = 1;
            }
            else
            {
                logIt($"adb = {adb}");
                string sn = "";
                if (_args.Parameters.ContainsKey("sn"))
                {
                    sn = _args.Parameters["sn"];
                    logIt($"adb = {adb}");
                }
                ret = start(adb, sn);
            }
            return ret;
        }
        static Dictionary<string, string> get_devices(string adb)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            int i;
            Tuple <string[], string[]> res = runExe_v2(adb, "devices", out i);
            foreach(string s in res.Item1)
            {
                int pos = s.IndexOf('\t');
                if (pos > 0 && pos+1 < s.Length)
                {
                    string x = s.Substring(0, pos);
                    string y = s.Substring(pos + 1);
                    ret.Add(x, y);
                }
            }
            return ret;
        }
        public static Tuple<string[], string[]> get_FilesAndFolders_FastVersion(string adb, string sn, string folder)
        {
            List<string> dirs = new List<string>();
            List<string> files = new List<string>();
            System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(@"^([\w-]{10})\s+\d+\s+.+\s+.+\s+\d+\s+[\d\s\-:]+\s+(.+)$");
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"^.\/(.*):$");
            int exit_code;
            Tuple<string[], string[]> res = runExe_v2(adb, $"-s {sn} shell \"cd {folder}; ls -lRa ", out exit_code);
            String subfolder = "";
            foreach (string l in res.Item1)
            {
                System.Text.RegularExpressions.Match m = r.Match(l);
                if (m.Success)
                {
                    string s = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(s))
                    {
                        subfolder = folder + s;
                        dirs.Add(subfolder);
                    }
                }
                else
                {
                    m = re.Match(l);
                    if (m.Success)
                    {
                        string v = m.Groups[1].Value;
                        if (v[0] == 'd')
                        {
                        }
                        else
                        {
                            v = m.Groups[2].Value;
                            files.Add($"{subfolder}/{v}");
                        }
                    }
                }
            }
            return new Tuple<string[], string[]>(dirs.ToArray(), files.ToArray());
        }
        static int start(string adb, string sn)
        {
            int ret = -1;
            logIt($"start: ++ adb={adb} sn={sn}");
            try
            {
                Dictionary<string, string> devices = get_devices(adb);
                if(string.IsNullOrEmpty(sn))
                {
                    if (devices.Count == 1)
                    {
                        sn = devices.Keys.First();
                    }
                    else
                    {
                        logIt("More than one device, please use serial number to specify the device.");
                        ret = 2;
                    }
                }
                if (!string.IsNullOrEmpty(sn))
                {
                    if (devices.Keys.Contains(sn))
                    {
                        if(string.Compare(devices[sn], "offline") == 0)
                        {
                            logIt($"The device [{sn}] offline. Please remove device and plug again.");
                            ret = 4;
                        }
                        else if (string.Compare(devices[sn], "unauthorized") == 0)
                        {
                            logIt($"The device [{sn}] unauthorized. Please remove device and plug again and click Trust");
                            ret = 5;
                        }
                        else if (string.Compare(devices[sn], "device") == 0)
                        {
                            ret = clean_sdcard(adb, sn);
                        }
                        else
                        {
                            logIt($"The device [{sn}] {devices[sn]}. Please remove device and plug again.");
                            ret = 6;
                        }
                    }
                    else
                    {
                        logIt($"The device [{sn}] does not found.");
                        ret = 3;
                    }
                }
            }
            catch(Exception ex)
            {
                logIt(ex.Message);
            }
            logIt($"start: -- ret={ret}");
            return ret;
        }
        static int clean_sdcard(string adb, string sn)
        {
            int ret = -1;
            logIt($"clean_sdcard: ++ adb={adb} sn={sn}");
            try
            {
                String[] target_value = new String[] { "EXTERNAL_STORAGE" };
                int exit_code;
                Tuple<string[], string[]> res;
                // 1. get all storage from printenv
                Dictionary<string, string> android_env = new Dictionary<string, string>();
                res = runExe_v2(adb, $"-s {sn} shell printenv", out exit_code);
                foreach (string l in res.Item1)
                {
                    int pos = l.IndexOf('=');
                    if (pos > 0)
                    {
                        string k = l.Substring(0, pos);
                        string v = "";
                        if (pos + 1 < l.Length)
                        {
                            v = l.Substring(pos + 1);
                        }
                        android_env[k] = v;
                    }
                }
                ret = 8;
                foreach (KeyValuePair<string, string> kvp in android_env)
                {
                    if (target_value.Contains(kvp.Key))
                    {
                        logIt($"Start erasing path: {kvp.Key}={kvp.Value}");
                        res = runExe_v2(adb, $"-s {sn} shell \"cd {kvp.Value}; ls -Rla\"", out exit_code);
                        res = runExe_v2(adb, $"-s {sn} shell \"cd {kvp.Value}; rm -rf *\"", out exit_code);
                        foreach (string s in res.Item2)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(s, $"rm: not found"))
                            {
                                logIt($"rm not found");
                                ret = 7;
                            }

                        }
                        res = get_FilesAndFolders_FastVersion(adb, sn, kvp.Value.EndsWith("/") ? kvp.Value : kvp.Value + "/");
                        foreach (string r in res.Item2)
                        {
                            logIt($"clean_sdcard: rm file: {r}");
                            runExe_v2(adb, $"-s {sn} shell rm -f '{r}'", out exit_code);
                        }
                        foreach (string r in res.Item1)
                        {
                            logIt($"clean_sdcard: rm folder: {r}");
                            runExe_v2(adb, $"-s {sn} shell rm -Rf '{r}'", out exit_code);
                        }
                        ret = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logIt(ex.Message);
            }
            logIt($"clean_sdcard: -- ret={ret}");
            return ret;
        }
    }
}
