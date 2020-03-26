using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GreenT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static public void logIt(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"[GreenT]: {msg}");
        }
        //MainWindow mw = new MainWindow();
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // debug
            int labels = 10;
            int rows = 4;
            int cols = 3;
            // preparation
            Application.Current.Properties.Add("labels", labels);
            Application.Current.Properties.Add("rows", rows);
            Application.Current.Properties.Add("cols", cols);
            for(int i = 1; i <= labels; i++)
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                Application.Current.Properties.Add(i.ToString(), d);
                d.Add("label", i);
                d.Add("row", (i - 1) / cols);
                d.Add("col", (i - 1) % cols);
            }

            MainWindow mw = new MainWindow();
            Application.Current.Properties.Add("MainWindow", mw);
            mw.Show();
            Task.Run(() =>
            {
                pull_uichange();
            });
        }

            
        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }

        Type getTypeByFullname(string fullname)
        {
            Type ret = null;
            try
            {
                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                //foreach (var v in a.ExportedTypes)
                //{
                //    if (string.Compare(id, v.Name, true) == 0)
                //    {
                //        ret = Activator.CreateInstance(v) as UserControl;
                //        break;
                //    }
                //}
                ret = a.GetType(fullname, false, true);
            }
            catch (Exception) { }
            return ret;
        }

        void pull_uichange()
        {
            MainWindow mw = (MainWindow)Application.Current.Properties["MainWindow"];
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            while (true)
            {
                try
                {
                    WebClient wc = new WebClient();
                    string s = wc.DownloadString("http://localhost:1210/uichange");
                    if (!string.IsNullOrEmpty(s))
                    {
                        //mw.onUIUdateCallback(s);
                        Dictionary<string, object> data = jss.Deserialize<Dictionary<string, object>>(s);
                        if (data.ContainsKey("error") && data["error"].GetType() == typeof(int) && (int)data["error"] == 0)
                        {
                            foreach (KeyValuePair<string, object> kvp in data)
                            {
                                Match m = Regex.Match(kvp.Key, @"label_(\d+)", RegexOptions.IgnoreCase);
                                if (m.Success)
                                {
                                    if (Application.Current.Properties.Contains(m.Groups[1].Value))
                                    {
                                        Dictionary<string, object> d = (Dictionary<string, object>)Application.Current.Properties[m.Groups[1].Value];
                                        if (d.ContainsKey("view") && d["view"].GetType().IsSubclassOf(typeof(UserControl)))
                                        {
                                            UserControl uc = d["view"] as UserControl;
                                            if(string.Compare(kvp.Value.ToString(), uc.GetType().FullName, true) != 0)
                                            {
                                                // update view
                                                Type t = getTypeByFullname(kvp.Value.ToString());
                                                if (t != null)
                                                {
                                                    mw.updateView(t, d);
                                                    //d["view"] = uc1;
                                                }
                                            }
                                        }
                                        // call updateUI of view
                                        if (d.ContainsKey("view") && d["view"].GetType().IsSubclassOf(typeof(UserControl)))
                                        {
                                            UserControl uc = d["view"] as UserControl;
                                            Type t = uc.GetType();
                                            MethodInfo mi = t.GetMethod("updateUI");
                                            if (mi != null)
                                            {
                                                mi.Invoke(uc, null);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
