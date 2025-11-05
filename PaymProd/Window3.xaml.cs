using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
using Telerik.Windows.Controls;

namespace PaymProd
{
    /// <summary>
    /// Логика взаимодействия для Window3.xaml
    /// </summary>
    public partial class Window3 : Window
    {
        public Window3()
        {
            InitializeComponent();
        }
        void UpdateSostavfromDB()
        {
            try
            {
               string conectionstring = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";

                sotavO.Items.Clear();
              
                SqlConnection sql = new SqlConnection(conectionstring);
                //SqlCommand sqlcom = new SqlCommand(string.Format(@"select * from view_producte "), sql);
                SqlDataAdapter sqld = new SqlDataAdapter("select * from view_producte", sql);

                DataSet ds = new DataSet();

                sqld.Fill(ds, "view_producte");
                //   List<ProductView> lpw = new List<ProductView>();
                ProductView pw;
                TextBlock tbO;
                TextBox tbOves;
                TextBlock tbOmerra;
                StackPanel prodOsp;
                RadButton rbt;

                foreach (DataRow list in ds.Tables[0].Rows)
                {

                    tbO = new TextBlock();
                    prodOsp = new StackPanel();
                    tbOmerra = new TextBlock();
                    tbOves = new TextBox();
                    tbO.TextWrapping = TextWrapping.Wrap;
                    tbO.Width = 250;
                   // prodOsp.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;
                    tbO.MouseLeftButtonDown += tbO_MouseLeftButtonDown;
                    prodOsp.Orientation = Orientation.Horizontal;


                    tbOves.Width = 50;
                    // tbOves.IsReadOnly = true;

                   // tbOmerra.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;


                    pw = new ProductView();
                    pw.fass = list[6] == DBNull.Value ? 0 : (decimal)list[6];
                    pw.ID = (int)list[0];
                    prodOsp.Tag = pw.ID;
                    tbOmerra.Margin = new Thickness(5, 0, 5, 0);
                    pw.name = list[1].ToString();
                    tbO.Text = pw.name;
                    pw.type = list[2].ToString();
                    tbO.Tag = pw.type;
                    pw.ves = list[3].ToString();
                    tbOmerra.Text = pw.ves;
                    pw.TID = (int)list[4];
                    pw.VID = (int)list[5];
                   // lpw.Add(pw);
                    pw.iz = (int)list[7];
                    pw.izname = (string)list[8];
                    pw.prizMen = (int)list[9];
                    pw.count = (decimal)list[10];
                    prodOsp.Children.Add(tbO);
                    prodOsp.Children.Add(tbOves);

                    if (pw.prizMen == 1)
                    {
                        pw.prizMen1 = true;
                    }
                    else { pw.prizMen1 = false; }
                    prodOsp.Children.Add(tbOmerra);
                    rbt = new RadButton();
                    rbt.Width = 20;
                    rbt.Content = "+";
                   // rbt.Click += rbt_Click;
                    rbt.Visibility = Visibility.Collapsed;
                    prodOsp.Children.Add(rbt);
                    sotavO.Items.Add(prodOsp);
                    
                }


          
            }
            catch { }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSostavfromDB();
        }
        void tbO_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            try
            {
                var t = ((e.OriginalSource as TextBlock).Parent as StackPanel);
                var m = t.Children[1];

                (m as TextBox).IsEnabled = true;

                (m).Focus();
                m.PreviewTextInput += m_PreviewTextInput;

            }
            catch (Exception ex) { ex.ToString(); }
            //throw new NotImplementedException();
        }

        void m_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = ("0123456789" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToString()).IndexOf(e.Text) < 0;

            //throw new NotImplementedException();
        }
        private void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var list in sotavO.Items)
            {
                if (((list as StackPanel).Children[0] as TextBlock).Text.ToString().ToUpper().Contains(searchTextBox.Text.ToUpper())) { sotavO.ScrollIntoView(list); sotavO.SelectedItem = list; break; }
            }


        }

        private void OnSearchTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            foreach (var list in sotavO.Items)
            {
                if (((list as StackPanel).Children[0] as TextBlock).Text.ToString().ToUpper().Contains(searchTextBox.Text.ToUpper())) { sotavO.ScrollIntoView(list); sotavO.SelectedItem = list; break; }
            }

        }

        private void sotavO_SelectionChanged(object sender, MouseButtonEventArgs e)
        {

        }

        private void sotavO_KeyUp(object sender, KeyEventArgs e)
        {
            
            if (e.Key == Key.Enter)
            {
                sotavO_MouseDoubleClick(sender, null);
            }
        }
      int i;
        bool insertDelicatesComp(int ID, double ves, int prodID)
        {
            try
            {
                SqlConnection sqlcon = new SqlConnection(MainWindow.Conectionstring);
                SqlCommand sqlcomm = new SqlCommand(string.Format("insert into Components (delic_id,ves,productID) values ('{0}','{1}',{2}) ", ID, ves.ToString().Replace(',', '.'), prodID), sqlcon);
                sqlcon.Open();
                sqlcomm.ExecuteNonQuery();
                sqlcon.Close();
              
                return true;
            }
            catch (Exception ex) { return false; };
        }
        private void sotavO_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).Text != "")
                {
                    int o = (int)(sotavO.SelectedItem as StackPanel).Tag;
                    IDProd.ID = o;
                    IDProd.ves = Convert.ToDouble(((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).Text);
                    i = 1;
                    Close();

                }
                else { MessageBox.Show("Не введен вес продукта"); }
            }
            catch { }
        }

        private void sotavO_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                
                    if (i == 0)    {
                        IDProd.ID = 0;
                    IDProd.ves = 0;
                }
            }
            catch { }     

        }

        private void sotavO_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
