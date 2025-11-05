using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class typeDel
    {
        public int id { get; set; }
        public string name { get; set; }
         public int fassdef { get; set; }
         public string fass_izmer { get; set; }
    }
    /// <summary>
    /// Логика взаимодействия для Dicshen.xaml
    /// </summary>
    /// 
    public partial class Dicshen : Window
    {
        public Dicshen(int i1)
        {
            InitializeComponent();
             i=i1;
        }
        int i;
        void update() 
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection();
                sqlconn = new SqlConnection(MainWindow.Conectionstring);
                SqlCommand sqlcomm = new SqlCommand("select * from Type_Del", sqlconn);

                List<typeDel> li = new List<typeDel>();
                typeDel td;
                sqlconn.Open();
                SqlDataReader sqlread = sqlcomm.ExecuteReader();
                while (sqlread.Read())
                {
                    td = new typeDel();
                    td.name = sqlread.GetString(1);
                    td.id = sqlread.GetInt32(0);
                    li.Add(td);

                }
                rgv.ItemsSource = li;
                rgv.Columns[2].IsVisible = false;
                rgv.Columns[3].Header = "Название";
                rgv.Columns[4].IsVisible = false;
                rgv.Columns[5].IsVisible = false;
                sqlconn.Close();

            }
            catch (Exception ex) { }
        }
        void update1()
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection();
                sqlconn = new SqlConnection(MainWindow.Conectionstring);
                SqlCommand sqlcomm = new SqlCommand("select * from produkt_type", sqlconn);

                List<typeDel> li = new List<typeDel>();
                typeDel td;
                sqlconn.Open();
                SqlDataReader sqlread = sqlcomm.ExecuteReader();
                while (sqlread.Read())
                {
                    td = new typeDel();
                    td.name = sqlread.GetString(1);
                    td.id = sqlread.GetInt32(0);
                    li.Add(td);

                }

                rgv.ItemsSource = li;
                rgv.Columns[2].IsVisible = false;
                rgv.Columns[3].Header = "Название";
                rgv.Columns[4].IsVisible = false;
                rgv.Columns[5].IsVisible = false;
                sqlconn.Close();

            }
            catch (Exception ex) { }
        }
        void update3()
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection();
                sqlconn = new SqlConnection(MainWindow.Conectionstring);
                SqlCommand sqlcomm = new SqlCommand("select mera_id,name_mera,isnull(fass_def,1),isnull(fass_izmer,name_mera) from mera", sqlconn);

                List<typeDel> li = new List<typeDel>();
                typeDel td;
                sqlconn.Open();
                SqlDataReader sqlread = sqlcomm.ExecuteReader();
                while (sqlread.Read())
                {
                    td = new typeDel();
                    td.name = sqlread.GetString(1);
                    td.id = sqlread.GetInt32(0);
                    td.fassdef = sqlread.GetInt32(2);
                    td.fass_izmer = sqlread.GetString(3);
                    
                    li.Add(td);

                }
                rgv.ItemsSource = li;
               rgv.Columns[2].IsVisible=false;
                  rgv.Columns[3].Header="Название";
                   rgv.Columns[4].Header="Фассовка";
                   rgv.Columns[5].Header = "Мера фассовки";
                sqlconn.Close();
                val1.Visibility = Visibility.Visible;
                val2.Visibility = Visibility.Visible;
                valp.Visibility = Visibility.Visible;
                valp1.Visibility = Visibility.Visible;
            }
            catch (Exception ex) { }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (i == 1) 
            {
                update();

            }else if (i == 2)
            {
                update1();
            }
            else if (i == 3) { update3(); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (val.Text.Length != 0)
            {
                if (i == 1)
                {
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("insert into Type_Del (type_del_opis) values('{0}')", val.Text), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update();

                } if (i == 2)
                {
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("insert into produkt_type (type_opis) values('{0}')", val.Text), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update1();
                } 
                if (i == 3)
                {
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("insert into mera (name_mera,fass_def,fass_izmer) values('{0}',{1},'{2}')", val.Text, Convert.ToInt32(val1.Text == "" ? "1" : val1.Text), val2.Text), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update3();
                }
                val.Text = "";
            }
        }

        private void RadButton_Click3(object sender, RoutedEventArgs e)
        {
            try
            {
                if (i == 1)
                {
                    var t = ((e.OriginalSource as Telerik.Windows.Controls.RadButton).CommandParameter as typeDel);
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("delete from Type_Del where type_del_id={0}", t.id), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update();
                }
                else if (i == 2)
                {
                    var t = ((e.OriginalSource as Telerik.Windows.Controls.RadButton).CommandParameter as typeDel);
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("delete from  produkt_type where typeProdid={0}", t.id), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update1();

                }
                else if (i == 3)
                {
                    var t = ((e.OriginalSource as Telerik.Windows.Controls.RadButton).CommandParameter as typeDel);
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("delete from mera where mera_id={0}", t.id), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update3();

                }

            }
            catch(Exception ex) { }

        }

        private void rgv_CellEditEnded(object sender, Telerik.Windows.Controls.GridViewCellEditEndedEventArgs e)
        {
           
        }

        private void rgv_CurrentCellChanged(object sender, Telerik.Windows.Controls.GridViewCurrentCellChangedEventArgs e)
        {
           

        }

        private void RadButton_Click2(object sender, RoutedEventArgs e)
        {
            try
            {
                if (i == 1)
                {
                    var t = ((e.OriginalSource as Telerik.Windows.Controls.RadButton).CommandParameter as typeDel);
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("update Type_Del set type_del_opis= '{0}' where  type_del_ID='{1}'", t.name,t.id), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update();
                }
                else if (i == 2)
                {
                    var t = ((e.OriginalSource as Telerik.Windows.Controls.RadButton).CommandParameter as typeDel);
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("update produkt_type set type_opis= '{0}' where  typeprodid='{1}'", t.name, t.id), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update1();

                } if (i == 3)
                {
                    var t = ((e.OriginalSource as Telerik.Windows.Controls.RadButton).CommandParameter as typeDel);
                    SqlConnection sqlconn = new SqlConnection();
                    sqlconn = new SqlConnection(MainWindow.Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("update mera set name_mera= '{0}', fass_def='{2}',fass_izmer='{3}'  where  mera_id='{1}'", t.name, t.id, t.fassdef,t.fass_izmer), sqlconn);
                    sqlconn.Open();
                    sqlcomm.ExecuteReader();
                    sqlconn.Close(); update3();

                }
            }
            catch { }
        }

        private void val1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789 ,.".IndexOf(e.Text) < 0;
        }
    }
}
