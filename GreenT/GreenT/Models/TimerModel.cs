using GreenT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace GreenT.Models
{
    internal class TimerModel : WindowBase
    {
        #region 字段
        private static Timer SystemTimer = null;   /* 该对象持续存在于整个应用程序运行期间 */
        #endregion

        /// <summary>
        /// 菜单栏 - 时间戳
        /// </summary>
        private bool _TimeStampEnable;
        public bool TimeStampEnable
        {
            get
            {
                return _TimeStampEnable;
            }
            set
            {
                if (_TimeStampEnable != value)
                {
                    _TimeStampEnable = value;
                    RaisePropertyChanged(nameof(TimeStampEnable));
                }
            }
        }

        /// <summary>
        /// 状态栏 - 系统时间
        /// </summary>
        private string _SystemTime;
        public string SystemTime
        {
            get
            {
                return _SystemTime;
            }
            set
            {
                if (_SystemTime != value)
                {
                    _SystemTime = value;
                    RaisePropertyChanged(nameof(SystemTime));
                }
            }
        }

        public void InitSystemClockTimer()
        {
            SystemTimer = new Timer
            {
                Interval = 1000
            };

            SystemTimer.Elapsed += SystemTimer_Elapsed;
            SystemTimer.AutoReset = true;
            SystemTimer.Enabled = true;
        }

        private void SystemTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SystemTime = SystemTimeData();
        }

        private string SystemTimeData()
        {
            DateTime _DateTime = DateTime.Now;

            return string.Format(cultureInfo, "{0}-{1}-{2} {3}:{4}:{5}",
                _DateTime.Year.ToString("0000", cultureInfo),
                _DateTime.Month.ToString("00", cultureInfo),
                _DateTime.Day.ToString("00", cultureInfo),
                _DateTime.Hour.ToString("00", cultureInfo),
                _DateTime.Minute.ToString("00", cultureInfo),
                _DateTime.Second.ToString("00", cultureInfo));
        }

        public void TimerDataContext()
        {
            TimeStampEnable = false;

            SystemTime = string.Format(cultureInfo, "2019-08-31 12:13:15");
            InitSystemClockTimer();
        }

    }
}
