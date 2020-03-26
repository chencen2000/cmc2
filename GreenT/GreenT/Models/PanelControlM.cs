using System;
using System.Collections.Generic;
using System.Linq;
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
        }
    }
}
