using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClientCentralino_vs2
{
    public class BrushCloneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Clona solo se è un SolidColorBrush
            if (value is SolidColorBrush brush)
            {
                return brush.Clone();
            }

            return value; // Altrimenti restituisce il valore così com'è
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

