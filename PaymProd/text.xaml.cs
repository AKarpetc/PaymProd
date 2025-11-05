using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
    /// Логика взаимодействия для text.xaml
    /// </summary>
    public partial class text : Window
    {
        public text(int b,int p)
        {
            InitializeComponent();
            b1 = b; a1 = p;
        }
        int b1, a1;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SqlConnection sqlcon = new SqlConnection(MainWindow.Conectionstring);
                SqlCommand sqlcom = new SqlCommand(string.Format("select isnull(detail,'') from components  where delic_id={0} and productID={1}", b1, a1), sqlcon);
                sqlcon.Open();
                SqlDataReader sqlread = sqlcom.ExecuteReader();
                while (sqlread.Read())
                {
                    text1.Text = sqlread.GetString(0);
                }
                sqlcon.Close();
             
            }
            catch { }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SqlConnection sqlcon = new SqlConnection(MainWindow.Conectionstring);
                SqlCommand sqlcom = new SqlCommand(string.Format("update components set detail ='{2}'    where delic_id={0} and productID={1}", b1, a1, text1.Text), sqlcon);
                sqlcon.Open();
                sqlcom.ExecuteNonQuery();
                sqlcon.Close();
                Close();
            }
            catch { }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
