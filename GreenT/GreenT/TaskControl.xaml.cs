using GreenT.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GreenT
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>

    public partial class TaskControl : UserControl
    {
        internal PanelControlView panelControlViewModel = null;
        public TaskControl()
        {
            InitializeComponent();

            panelControlViewModel = new PanelControlView();
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
        public void updateUI()
        {

        }
    }
}
