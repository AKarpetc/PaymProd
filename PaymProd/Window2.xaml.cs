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
using System.Xml.Linq;

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
          
               
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (TBlogin.Text.Trim().ToUpper() == "USER" + DateTime.Now.Month.ToString() && TBpass.Password.ToString().ToUpper() == "USER" + DateTime.Now.Month.ToString())
            {
                XDocument xDocument = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory+@"\conect.xml");
                try
                {

                    xDocument.Element("connect").Element("user").Value = "USER" + DateTime.Now.Month.ToString();
                    Close();
                    xDocument.Save("conect.xml");
                }
                catch (Exception ex) {/* MessageBox.Show(ex.ToString());*/ }

            }
            else
            {
                MessageBox.Show("Пароль или номер сетификата указан неверно!");
            }

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.MainWindow.Close();
                Close();
            }
            catch { }

        }

        private void TBpass_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
