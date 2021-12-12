using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace twController
{
    class uiSameDeviceWarning
    {
        int _label = 1;
        int _result = 7;
        bool _ready = false;
        public bool Ready
        {
            get { return _ready; }
        }
        public int Result
        {
            get { return _result; }
        }
        object obj = null;
        void uiThreadProc()
        {
            string s = System.IO.Path.Combine(envClass.getInstance().ExePath, "GreenTMinPanel.dll");
            if (System.IO.File.Exists(s))
            {
                Assembly a = Assembly.LoadFrom(s);
                Type t = a.GetType("GreenTMinPanel.SameDeviceWarn");
                obj = Activator.CreateInstance(t, new object[] { _label.ToString() });
                t.InvokeMember("ShowDialog", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, null);
                _result = (int)t.InvokeMember("MessageResult", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty, null, obj, null);
                _ready = true;
            }
        }

        public void showDialog(int label)
        {
            _label = label;
            System.Threading.Thread t = new System.Threading.Thread(uiThreadProc);
            t.TrySetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            //t.Join();
            //return _result;
        }

        public void closeDialog()
        {
            if (obj!=null)
            {
                if (!_ready)
                {
                    try
                    {
                        System.Windows.Window w = (System.Windows.Window)obj;
                        w.Dispatcher.Invoke(new Action(() =>
                        {
                            w.Close();
                        }));
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.Message);
                    }
                }
            }
        }
    }
}
