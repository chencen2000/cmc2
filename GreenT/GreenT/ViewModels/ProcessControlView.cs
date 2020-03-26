using GreenT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GreenT.ViewModels
{
   
    internal class ProcessControlView : WindowBase
    {
        public ProcessControlM ProcessCM { get; set; }

        public void SetLabel(int label)
        {
            ProcessCM.Label = label;
        }

        public void SetConnect(bool bConn)
        {
            ProcessCM.IsConnected = bConn;
        }

        public void SetBackGround(Brush scolor)
        {
            ProcessCM.Background = scolor;// "DarkOrange";
        }

        public ProcessControlView()
        {
            ProcessCM = new ProcessControlM();
            ProcessCM.ProcessControlMContext();
        }

        internal  void GoProecess()
        {
            ProcessCM.ProcessValue = 0;
            for(int iloop = 1; iloop <= 100; iloop++)
            {
                ProcessCM.ProcessValue = iloop;
                Thread.Sleep(100);
            }
            return;
        }
    }
}
