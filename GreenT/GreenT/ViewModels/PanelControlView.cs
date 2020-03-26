using GreenT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenT.ViewModels
{
    internal class PanelControlView:WindowBase
    {
        public PanelControlM PanelCM { get; set; }

        public void SetLabel(int label)
        {
            PanelCM.Label = label;
        }

        public void SetConnect(bool bConn)
        {
            PanelCM.IsConnected = bConn;
        }


        public PanelControlView()
        {
            PanelCM = new PanelControlM();
            PanelCM.PanelControlMContext();
        }
    }
}
