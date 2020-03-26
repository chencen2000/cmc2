using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GreenT.Models
{
 
    internal class ProcessControlM : UserControlM
    {
        private double _ProcessValue;
        public double ProcessValue
        {
            get
            {
                return _ProcessValue;
            }
            set
            {
                if (_ProcessValue != value)
                {
                    _ProcessValue = value;
                    RaisePropertyChanged(nameof(ProcessValue));
                }
            }
        }
        private String _ModelName;
        public String ModelName
        {
            get
            {
                return _ModelName;
            }
            set
            {
                if (_ModelName != value)
                {
                    _ModelName = value;
                    RaisePropertyChanged(nameof(ModelName));
                }
            }
        }

        private String _UniqueID;
        public String UniqueID
        {
            get
            {
                return _UniqueID;
            }
            set
            {
                if (_UniqueID != value)
                {
                    _UniqueID = value;
                    RaisePropertyChanged(nameof(UniqueID));
                }
            }
        }

        private String _OperationStatus;
        public String OperationStatus
        {
            get
            {
                return _OperationStatus;
            }
            set
            {
                if (_OperationStatus != value)
                {
                    _OperationStatus = value;
                    RaisePropertyChanged(nameof(OperationStatus));
                }
            }
        }

        public void ProcessControlMContext()
        {
            Label = 1;
            IsConnected = false;
            Background = new SolidColorBrush(Colors.DarkOrange);
            ProcessValue = 0.0;
            ModelName = "";
            UniqueID = "";
            OperationStatus = "Refurbish in process";
        }
    }
}
