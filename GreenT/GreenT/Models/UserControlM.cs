using GreenT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GreenT.Models
{
    internal class UserControlM : WindowBase
    {
        /// <summary>
        /// Label
        /// </summary>
        private int _label;
        public int Label
        {
            get
            {
                return _label;
            }
            set
            {
                if (_label != value)
                {
                    _label = value;
                    LabelConnect = String.Format("{0} {1}", _label, _IsConnected ? 
                        Application.Current.FindResource("DeviceConnted").ToString() : Application.Current.FindResource("NotConnted").ToString());
                }
            }
        }

        private String _labelconnect;
        public String LabelConnect
        {
            get
            {
                return _labelconnect;
            }
            set
            {
                if (_labelconnect != value)
                {
                    _labelconnect = value;
                    RaisePropertyChanged(nameof(LabelConnect));
                }
            }
        }
        /// <summary>
        /// isConnected
        /// </summary>
        private bool _IsConnected;
        public bool IsConnected
        {
            get
            {
                return _IsConnected;
            }
            set
            {
                if (_IsConnected != value)
                {
                    _IsConnected = value;
                    LabelConnect = String.Format("{0} {1}", _label, _IsConnected ?
                        Application.Current.FindResource("DeviceConnted").ToString() : Application.Current.FindResource("NotConnted").ToString());
                }
            }
        }

        private Brush _Background;
        public Brush Background
        {
            get
            {
                return _Background;
            }
            set
            {
                if(_Background!=value)
                {
                    _Background = value;
                    RaisePropertyChanged(nameof(Background));
                }
            }
        }

    }
}
