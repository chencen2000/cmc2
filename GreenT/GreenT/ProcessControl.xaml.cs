using GreenT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace GreenT
{
    /// <summary>
    /// Interaction logic for ProcessControl.xaml
    /// </summary>
    public partial class ProcessControl : UserControl
    {
        internal ProcessControlView panelControlViewModel = null;
        public ProcessControl()
        {
            InitializeComponent();
            panelControlViewModel = new ProcessControlView();
            DataContext = panelControlViewModel;

           
        }

        public void SetLabel(int label)
        {
            panelControlViewModel.SetLabel(label);
        }

        public void SetConnect(bool bconn)
        {
            panelControlViewModel.SetConnect(bconn);
        }

        public void SetBackGround(Color sclr)
        {
            panelControlViewModel.SetBackGround(new SolidColorBrush(sclr));
        }

        private  void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            new Thread(() => {
                panelControlViewModel.GoProecess(); 
            }).Start();
           
        }
    }
}
