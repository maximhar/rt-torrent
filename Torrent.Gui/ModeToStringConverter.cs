using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Torrent.Gui
{
    class ModeToStringConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Mode)
            {
                var mode = (Mode)value;
                switch (mode)
                {
                    case Mode.Stopped:
                        return "Stopped.";
                    case Mode.Seed:
                        return "Seeding...";
                    case Mode.Idle:
                        return "Idle.";
                    case Mode.Download:
                        return "Downloading...";
                    case Mode.Error:
                        return "An error occured.";
                    case Mode.Hash:
                        return "Hashing...";
                    case Mode.Completed:
                        return "Completed.";
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
