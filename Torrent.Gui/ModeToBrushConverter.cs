using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace Torrent.Gui
{
    class ModeToBrushConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Mode)
            {
                var mode = (Mode)value;
                switch (mode)
                {
                    case Mode.Seed:
                    case Mode.Completed:
                        return Brushes.DarkGreen;
                    case Mode.Error:
                        return Brushes.Red;
                    case Mode.Stopped:
                    case Mode.Idle:
                        return Brushes.Gray;
                    case Mode.Hash:
                        return Brushes.Green;
                    case Mode.Download:
                        return Brushes.LightGreen;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class ModeToBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Mode)
            {
                var mode = (Mode)value;
                switch (mode)
                {
                    case Mode.Seed:
                    case Mode.Completed:
                        return new SolidColorBrush(Color.FromRgb(237, 255, 237));
                    default:
                        return Brushes.White;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
