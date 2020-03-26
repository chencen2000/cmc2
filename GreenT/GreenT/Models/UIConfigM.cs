using GreenT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GreenT.Models
{
    internal class UIConfigM : WindowBase
    {
        private string _WinTitle;
        public string WinTitle
        {
            get
            {
                return _WinTitle;
            }
            set
            {
                if (_WinTitle != value)
                {
                    _WinTitle = value;
                    RaisePropertyChanged(nameof(WinTitle));
                }
            }
        }

        private string _Version;
        public string SysVersion
        {
            get
            {
                return _Version;
            }
            set
            {
                if (_Version != value)
                {
                    _Version = value;
                    RaisePropertyChanged(nameof(SysVersion));
                }
            }
        }

        private string _UserName;
        public string UserName
        {
            get
            {
                return _UserName;
            }
            set
            {
                if (_UserName != value)
                {
                    _UserName = value;
                    RaisePropertyChanged(nameof(UserName));
                }
            }
        }

        public void UIConfigMContext()
        {
            WinTitle = Application.Current.FindResource("WindowTitle").ToString(); //"Lean One Touch";

            SysVersion = "7.0.0";

            UserName = "Futuredial";
        }
    }
}
