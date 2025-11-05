using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PaymProd
{
    /// <summary>
    /// Логика взаимодействия для Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        public Window2()
        {
            InitializeComponent();
        }

        private void rtb_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TextRange textRange;
                System.IO.FileStream fileStream;

                if (System.IO.File.Exists("1.rtf"))
                {
                    textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                    using (fileStream = new System.IO.FileStream("1.rtf", System.IO.FileMode.OpenOrCreate))
                    {
                        textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
                    }
                }

            }
            catch { }
        }
    }
}
