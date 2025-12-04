using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SoftielRemote.App;

/// <summary>
/// String değerini boş olup olmadığına göre Visibility'e çeviren converter.
/// String boş veya null ise Visible, değilse Collapsed döndürür.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return Visibility.Visible;
        }

        if (value is string stringValue)
        {
            return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


