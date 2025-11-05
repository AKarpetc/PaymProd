using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PaymProd
{
    class Convertor
    {
    }
    public class GroupCon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {


            if (value == null) return !true;
                       

            var line = value;

            try
            {
              //  (((parameter as Telerik.Windows.Controls.RadButton).Content as Viewbox).Child as Grid).Children.Clear();
                var t = (value as Components);
                if (t != null)
                {
                    return new SolidColorBrush(Colors.LightGreen); 
                }
                else { return null; }

            }
            catch (Exception ex) { return null; }
           
        }                      

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
