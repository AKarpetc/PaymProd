using System;
using System.Globalization;
using System.Windows.Data;

namespace PaymProdNet9.Converters;

/// <summary>
/// Конвертер для скрытия кнопок редактирования/удаления для продуктов (ID < 0)
/// </summary>
public class ProductVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int id)
            // Если ID отрицательный, это продукт - скрываем кнопки
            return id < 0 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        return System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}