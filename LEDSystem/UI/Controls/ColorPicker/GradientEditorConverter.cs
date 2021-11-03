using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace LEDSystem.UI.Controls.ColorPicker
{
    public class GradientEditorConverter : IValueConverter
    {
        private readonly Regex regex = new Regex(@"^[0-9]*$");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (System.Convert.ToInt32(value)).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (((string)value).Length > 0 && regex.IsMatch((string)value))
                return System.Convert.ToDouble(value);
            else
                return 0;
        }
    }
}
