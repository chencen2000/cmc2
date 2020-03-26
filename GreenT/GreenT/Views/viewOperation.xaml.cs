using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GreenT.Views
{
    /// <summary>
    /// Interaction logic for viewOperation.xaml
    /// </summary>
    public partial class viewOperation : UserControl
    {
        int _label = 0;
        public viewOperation(int label)
        {
            _label = label;
            InitializeComponent();
            labelInfo_task.Content = _label.ToString();
        }
        public void updateUI()
        {
            App.logIt("[viewOperation]: updateView: ++");
            try
            {
                string str = new WebClient().DownloadString($"http://localhost:1210/ui/{_label.ToString()}/{this.GetType().FullName}");
                if (!string.IsNullOrEmpty(str))
                {
                    System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                    Dictionary<string, object> data = jss.Deserialize<Dictionary<string, object>>(str);

                }
            }
            catch (Exception) { }
            App.logIt("[viewOperation]: updateView: --");
        }

    }
}
