using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using GreenT.ViewModels;

namespace GreenT.Models
{
    internal class PanelControlM : UserControlM
    {
        public void PanelControlMContext()
        {
            Label = 1;
            IsConnected = false;
            Background = new SolidColorBrush(Colors.LightGray);
            //try
            //{
            //    string s = new WebClient().DownloadString("http://localhost:1210/ui/TaskControl");
            //    System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            //    Dictionary<string, object> data = jss.Deserialize<Dictionary<string, object>>(s);

            //}
            //catch (Exception) { }
        }
    }
}
