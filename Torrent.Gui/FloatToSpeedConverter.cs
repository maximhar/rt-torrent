﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Torrent.Gui
{
    class FloatToSpeedConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter is bool)
                if (!(bool)parameter) return string.Empty;
            if (value is float)
                return FileSizeFormat((long)(float)value) + "/s";
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string FileSizeFormat(long size)
        {
            const int KB = 1024;
            const int MB = 1048576;
            const int GB = 1073741824;

            if (size < KB)
            {
                return string.Format("{0} bytes", size);
            }
            else if (size < MB)
            {
                return string.Format("{0:0.00} KiB", ((float)size / KB));
            }
            else if (size < GB)
            {
                return string.Format("{0:0.00} MiB", ((float)size / MB));
            }
            else
            {
                return string.Format("{0:0.00} GiB", ((float)size / GB));
            }
        }
    }
}
