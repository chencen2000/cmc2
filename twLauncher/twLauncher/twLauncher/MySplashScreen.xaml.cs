using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace twLauncher
{
    /// <summary>
    /// Interaction logic for MySplashScreen.xaml
    /// </summary>
    public partial class MySplashScreen : Window
    {
        public MySplashScreen()
        {
            InitializeComponent();
            //this.Loaded += delegate { img.Source = twLauncher.Properties.Resources.FutureDial_SplashScreen; };
        }
        public void setStatusText(string text)
        {
            this.Dispatcher.Invoke(delegate { this.textStatus.Content = text; });
        }

    }
}
