using CommonLibrary;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TrainsEditor.EditorLogic
{
    /// <summary>
    /// Konvertor pro WPF mezi objektem <see cref="Time"/> a jeho string reprezentací
    /// </summary>
    [ValueConversion(typeof(Time?), typeof(string))]
    public class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Time?)value)?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var result = new Time();
                result.LoadFromString((string)value, false);
                return new Time?(result);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
