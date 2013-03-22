using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Torrent.Gui
{
    class NumericProgressToStringConverter:IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value is NumericProgress)
            {
                var pr = (NumericProgress)value;
                return string.Format("{0} / {1}", FloatToSpeedConverter.FileSizeFormat(pr.BytesComplete), FloatToSpeedConverter.FileSizeFormat(pr.BytesTotal));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
