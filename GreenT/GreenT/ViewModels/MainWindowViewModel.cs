using GreenT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenT.ViewModels
{
    internal class MainWindowViewModel : WindowBase
    {
        public UIConfigM ConfigUI { get; set; }
        public TimerModel TimerModel { get; set; }

        public MainWindowViewModel()
        {
            ConfigUI = new UIConfigM();
            ConfigUI.UIConfigMContext();

            TimerModel = new TimerModel();
            TimerModel.TimerDataContext();
        }
    }
}
