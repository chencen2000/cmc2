using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace GreenT.Common
{
    [ValueConversion(typeof(string), typeof(string))]
    public class ProgressBarConverter : MarkupExtension, IValueConverter
    {
        private static ProgressBarConverter _instance;

        public ProgressBarConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        { // do not let the culture default to local to prevent variable outcome re decimal syntax
            double size = System.Convert.ToDouble(value)/100.0;
            return size.ToString("P2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { // read only converter...
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new ProgressBarConverter());
        }

    }
}
