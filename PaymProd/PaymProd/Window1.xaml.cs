using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
using Telerik.Windows.Controls;

namespace PaymProd
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            Telerik.Windows.Controls.LocalizationManager.Manager = new TelerikCustomLocalizationManager();
            InitializeComponent();
           
        }
        void UpdateTypeB()
        {
            try
            {
                StyleManager.ApplicationTheme = (new Windows8Theme());
                conectionstring = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";
                SqlConnection sql = new SqlConnection(conectionstring);
                SqlCommand mycom = new SqlCommand("select * from Type_Del",sql);
                sql.Open();
                SqlDataReader myread = mycom.ExecuteReader();
                TextBlock tb;
                while (myread.Read())
                {
                    tb = new TextBlock();
                    tb.Tag = (int)myread.GetInt32(0);
                    tb.Text = myread.GetString(1);
                    TypeB.Items.Add(tb);

                }
                sql.Close();
            }
            catch(ExecutionEngineException) { }

        }
        List<int> SB = new List<int>(); 
        void componentsUpdate(List<Components> coll)
        {
            sostavB.Items.Clear();
            RadButton rbt;
            SB.Clear();
            ProductView pw;
            TextBlock tbO;
            TextBox tbOves;
            TextBlock tbOmerra;
            StackPanel prodOsp;
            foreach (var list in coll)
            {
                tbO = new TextBlock();
                prodOsp = new StackPanel();
                tbOmerra = new TextBlock();
                tbOves = new TextBox();

                prodOsp.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;
                tbO.MouseLeftButtonDown += tbO_MouseLeftButtonDown;
                prodOsp.Orientation = Orientation.Horizontal;
                tbO.TextWrapping = TextWrapping.Wrap;
                tbO.Width = 250;
                tbOves.LostFocus += Window1_LostFocus;
                tbOves.Width = 50;
                // tbOves.IsReadOnly = true;

                tbOmerra.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;
                pw = new ProductView();
                pw.ID = list.id;
                prodOsp.Tag = list.Prodid;
                SB.Add(list.Prodid);
                tbOmerra.Margin = new Thickness(5, 0, 5, 0);
                pw.name = list.name;
                tbO.Text = pw.name;
                pw.type = list.type;
                tbO.Tag = pw.type;
                pw.ves = list.mera;
                tbOves.Text = list.ves;
                tbOmerra.Text = pw.ves;
                             
               

                prodOsp.Children.Add(tbO);
                prodOsp.Children.Add(tbOves);
                prodOsp.Children.Add(tbOmerra);
                rbt = new RadButton();
                rbt.Click += rbt_Click;
                rbt.ToolTip = "Добавить заметку";
                rbt.Width = 20;
                rbt.Content = "+";
                prodOsp.Children.Add(rbt);
                sostavB.Items.Add(prodOsp);

            }
        }

        void rbt_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection sqlcon = new SqlConnection(conectionstring);
            try
            {
                int t = (int)((e.OriginalSource as RadButton).Parent as StackPanel).Tag;
                text tex = new text((int)NameB.Tag, t);
                tex.Show();

                //SqlCommand sqlcom = new SqlCommand(string.Format("update components set ves={2} where delic_id={0} and productID={1}", (int)NameB.Tag, t, Convert.ToInt32((e.OriginalSource as TextBox).Text)), sqlcon);
                //sqlcon.Open();
                //sqlcom.ExecuteNonQuery();
                //sqlcon.Close();
                //updateDelColl();
            }
            catch (Exception ex) { sqlcon.Close(); }
           // throw new NotImplementedException();
        }
        void UpdateSostavfromDB()
        {
            try
            {
                conectionstring = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";
             
                sotavO.Items.Clear();
                lpw.Clear();
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
                    prodOsp.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;
                    tbO.MouseLeftButtonDown += tbO_MouseLeftButtonDown;
                    prodOsp.Orientation = Orientation.Horizontal;


                    tbOves.Width = 50;
                    // tbOves.IsReadOnly = true;

                    tbOmerra.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;
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
                    lpw.Add(pw);
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
                    rbt.Click += rbt_Click;
                    rbt.Visibility = Visibility.Collapsed;
                    prodOsp.Children.Add(rbt);
                    if (SB.Contains((int)list[0]))
                    {
                    }
                    else
                    {
                        sotavO.Items.Add(prodOsp);
                    }
                }


                dg.ItemsSource = lpw;
            }
            catch { }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateSostavfromDB();
                UpdateTypeB();
               // conectionstring = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30";
                SqlConnection sql = new SqlConnection(conectionstring);
                //SqlCommand sqlcom = new SqlCommand(string.Format(@"select * from view_producte "), sql);
                SqlDataAdapter sqld = new SqlDataAdapter("select * from view_producte", sql);

                DataSet ds = new DataSet();
              
               
                

                sqld = new SqlDataAdapter("select * from Produkt_Type", sql);
               ds = new DataSet();

                sqld.Fill(ds, "Produkt_Type");
                TextBlock tb;
                foreach (DataRow list in ds.Tables[0].Rows)
                {
                    tb = new TextBlock();
                    tb.Tag=   list[0];
                    tb.Text = list[1].ToString();


                    typeP.Items.Add(tb);
                  
                }
                sqld = new SqlDataAdapter("select * from mera", sql);
                ds = new DataSet();

                sqld.Fill(ds, "mera");
                int i = 0, j = 0; ;
                foreach (DataRow list in ds.Tables[0].Rows)
                {
                    tb = new TextBlock();
                    tb.Tag = list[0];
                    tb.Text = list[1].ToString();
                    vesP.Items.Add(tb);
                    tb = new TextBlock();
                    tb.Tag = list[0];
                    tb.Text = list[1].ToString();
                    typePS.Items.Add(tb);
                    if (list[1].ToString() == "г")  
                    {
                    j=i;
                    }
                    i++;
                }

                typePS.SelectedIndex = j;
            }
            catch (Exception ex) { }
        }

        void prodOsp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.ToString();// throw new NotImplementedException();
        }

        void tbO_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            try
            {
                var t = ((e.OriginalSource as TextBlock).Parent as StackPanel);
               var m= t.Children[1];

               (m as TextBox).IsEnabled = true;

                (m).Focus();
            }
            catch (Exception ex) { ex.ToString(); }
            //throw new NotImplementedException();
        }

        void tbOves_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //throw new NotImplementedException();
        }
       public string conectionstring;
       
        void producteUpdate()
        {
            List<ProductView> lpw = new List<ProductView>();
            SqlConnection sql = new SqlConnection(conectionstring);
            //SqlCommand sqlcom = new SqlCommand(string.Format(@"select * from view_producte "), sql);
            SqlDataAdapter sqld = new SqlDataAdapter("select * from view_producte", sql);

            DataSet ds = new DataSet();

            sqld.Fill(ds, "view_producte");

            ProductView pw;

            foreach (DataRow list in ds.Tables[0].Rows)
            {
                pw = new ProductView();
                pw.ID = (int)list[0];
                pw.name = list[1].ToString();
                pw.type = list[2].ToString();
                pw.ves = list[3].ToString();
                pw.TID = (int)list[4];
                pw.VID = (int)list[5];
                pw.fass = Convert.ToDecimal(list[6]);
                pw.iz = (int)list[7];
                pw.izname = list[8].ToString();
                pw.prizMen = (int)list[9];
                pw.count = (decimal)list[10];
                if (pw.prizMen == 1)
                {
                    pw.prizMen1 = true;
                }
                else { pw.prizMen1 = false; }
                lpw.Add(pw);

            }
            dg.ItemsSource = lpw;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (vesP.SelectedIndex != typePS.SelectedIndex && Convert.ToDecimal(fasP.Text) <= 1) { MessageBox.Show("Введите  фассовку."); }  else{
                bool l = true;
                foreach(var t in dg.Items)
                {
                    if (NameP.Text.ToUpper().Trim() == (t as ProductView).name.Trim().ToUpper())
                    {
                        l = false;
                    }
                }

                if (l != false)
                {

                    int addb = 0;
                    if (addB.IsChecked == true)
                    {
                        addb = 1;
                    }
                    List<ProductView> lpw = new List<ProductView>();
                    if (addB.IsChecked == true)
                    {
                        InsertProdcuct1(NameP.Text, (int)(vesP.SelectedItem as TextBlock).Tag, (int)(typeP.SelectedItem as TextBlock).Tag, Convert.ToInt32(fasP.Text), (int)(typePS.SelectedItem as TextBlock).Tag, addb, Convert.ToDecimal(countP.Text));
                    }
                    else
                    {
                        InsertProdcuct(NameP.Text, (int)(vesP.SelectedItem as TextBlock).Tag, (int)(typeP.SelectedItem as TextBlock).Tag, Convert.ToInt32(fasP.Text), (int)(typePS.SelectedItem as TextBlock).Tag, addb);
                    }
                    producteUpdate();
                    typeP.SelectedIndex = -1;
                    typePS.SelectedIndex = -1;
                    vesP.SelectedIndex = -1;
                    NameP.Text = "";
                }
                else
                {
                    MessageBox.Show("Продукт с данным нахванием уже существует!");
                }
            
                }      
            }
            catch (Exception ex) { MessageBox.Show("Заполненны не все поля","Внимание"); }   
           
        }
        void InsertProdcuct(string name, int ves,int type,int fas,int izmer,int priz)
        {
            try
            {
                SqlConnection sql = new SqlConnection(conectionstring);
                SqlCommand sqlcom = new SqlCommand(string.Format(@"INSERT INTO Producrs
                         ( Name, Type, ves,fass,izmer,priz_menu)
VALUES        ('{0}','{1}','{2}','{3}','{4}','{5}')", name, type, ves,fas,izmer,priz), sql);
                sql.Open();

                sqlcom.ExecuteNonQuery();

                sql.Close();
            }
            catch(Exception ex) { }
        }
        void InsertProdcuct1(string name, int ves, int type, int fas, int izmer, int priz,decimal count)
        {
            try
            {
                SqlConnection sql = new SqlConnection(conectionstring);
                SqlCommand sqlcom = new SqlCommand(string.Format(@"INSERT INTO Producrs
                         ( Name, Type, ves,fass,izmer,priz_menu,count)
VALUES        ('{0}','{1}','{2}','{3}','{4}','{5}',{6})", name, type, ves, fas, izmer, priz,count), sql);
                sql.Open();

                sqlcom.ExecuteNonQuery();

                sql.Close();
            }
            catch (Exception ex) { }
        }
        bool DeleteProdcuct(int id)
        {
            try
            {
                SqlConnection sql = new SqlConnection(conectionstring);
                SqlCommand sqlcom = new SqlCommand(string.Format(@"delete from Producrs where Prod_id='{0}'",id), sql);
                sql.Open();

                sqlcom.ExecuteNonQuery();

                sql.Close();
                return true;
            }
            catch (Exception ex) { return false; }
        }
        private void RadButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удолить данную строку?", "Внимание", MessageBoxButton.OKCancel).ToString() == "OK")
            {
                var t = ((e.OriginalSource as RadButton).CommandParameter as ProductView);

                if (DeleteProdcuct(t.ID) == true)
                {

                    producteUpdate();
                    
                }
            }

        }

        private void RadButton_Click1(object sender, RoutedEventArgs e)
        {
            try
            {
                var t = ((e.OriginalSource as RadButton).CommandParameter as ProductView);
                productEdit.pv = t;
                EditTypeProd etp = new EditTypeProd();
                etp.ShowDialog();
                t = productEdit.pv;
                if (productEdit.flag == true)
                {
                    UpdateProdcuct(t.name, t.VID, t.TID, t.ID, t.fass, t.iz,t.prizMen,t.count);
                }
                producteUpdate();
            }
            catch(Exception ex) { }
        }
        bool UpdateProdcuct(string name, int ves, int type,int ID,decimal fass,int iz,int priz,decimal count)
        {
            try
            {
                SqlConnection sql = new SqlConnection(conectionstring);
                SqlCommand sqlcom = new SqlCommand(string.Format(@"Update  Producrs
                        set Name='{0}', Type='{1}', ves='{2}',fass={4},izmer={5},priz_menu={6},count = {7} where prod_id='{3}'", name, type, ves,ID,Convert.ToDouble( fass),iz,priz,Convert.ToDouble( count)), sql);
                sql.Open();

                sqlcom.ExecuteNonQuery();

                sql.Close();
                return true;
            }
            catch (Exception ex) { return false; }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as ComboBox).ItemsSource = typeP.Items;

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
         //   e.Handled = true;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {

            w1.WindowState = WindowState.Maximized; 
        }

        private void Window_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            conectionstring = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";

             try
             {
                 SqlConnection sql = new SqlConnection(conectionstring);
                 //SqlCommand sqlcom = new SqlCommand(string.Format(@"select * from view_producte "), sql);

                
                 DataSet ds = new DataSet();




                 SqlDataAdapter sqld = new SqlDataAdapter("select * from Produkt_Type", sql);
                 ds = new DataSet();
                 typePB.Items.Clear();
                 sqld.Fill(ds, "Produkt_Type");
                 TextBlock tb;
                 tb = new TextBlock();
                 tb.Tag = "-1";
                 tb.Text = "Все";

                 typePB.Items.Add(tb);

                 foreach (DataRow list in ds.Tables[0].Rows)
                 {
                     tb = new TextBlock();
                     tb.Tag = list[0];
                     tb.Text = list[1].ToString();
                    
                     typePB.Items.Add(tb);
                     
                 }
                 typePB.SelectedIndex = 0;
             }
             catch { }

        }

        private void sotavO_Selected(object sender, RoutedEventArgs e)
        {
            ((e.OriginalSource as StackPanel).Children[1] as TextBox).IsEnabled = true;
        }

        private void sotavO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).IsEnabled = true;

                ((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).Focus();
            }
            catch (Exception ex) { ex.ToString(); }
       
            }

        private void dg_DataLoading(object sender, Telerik.Windows.Controls.GridView.GridViewDataLoadingEventArgs e)
        {
            
        }
        bool insertDelicatesComp(int ID,decimal ves,int prodID)
        {
            try
            {
                SqlConnection sqlcon = new SqlConnection(conectionstring);
                SqlCommand sqlcomm = new SqlCommand(string.Format("insert into Components (delic_id,ves,productID) values ('{0}','{1}',{2}) ", ID, ves, prodID), sqlcon);
                sqlcon.Open();
                sqlcomm.ExecuteNonQuery();
                sqlcon.Close();
                updateDelColl();
                return true;
            }
            catch(Exception ex) { return false; };
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).Text != "")
                {
                    StackPanel sp = new StackPanel();
                    sp =
                        (sotavO.SelectedItem as StackPanel);
                    if (insertDelicatesComp(DELID, Convert.ToDecimal((sp.Children[1] as TextBox).Text), (int)sp.Tag) == true)
                    {
                        sotavO.Items.Remove(sotavO.SelectedItem);
                        (sp.Children[1] as TextBox).LostFocus += Window1_LostFocus;
                       
                        sostavB.Items.Add(sp); w1.Focus();
                        (sp.Children[3] as RadButton).Visibility = Visibility.Visible;
                        var l = sp.Tag;
                       // insertDelicatesComp(DELID, Convert.ToDecimal((sp.Children[1] as TextBox).Text), (int)sp.Tag);
                    }
                    else { MessageBox.Show("Ошибка в вводе продукта или блюда!"); }
                }
                else MessageBox.Show("Вес компонента не указан");
            }
            catch { }
        }

        void Window1_LostFocus(object sender, RoutedEventArgs e)
        {
            SqlConnection sqlcon = new SqlConnection(conectionstring);
            try
            {
                int t = (int)((e.OriginalSource as TextBox).Parent as StackPanel).Tag;
                
                SqlCommand sqlcom = new SqlCommand(string.Format("update components set ves={2} where delic_id={0} and productID={1}", (int)NameB.Tag, t, Convert.ToInt32((e.OriginalSource as TextBox).Text)), sqlcon);
                sqlcon.Open();
                sqlcom.ExecuteNonQuery();
                sqlcon.Close();
                updateDelColl();
            }
            catch(Exception ex) { sqlcon.Close(); }
            //throw new NotImplementedException();
        }

      

        private void sotavO_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click_1(null, null);

            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((sostavB.SelectedItem as StackPanel).Children[1] as TextBox).Text != "")
                {
                    StackPanel sp = new StackPanel();
                    sp =
                        (sostavB.SelectedItem as StackPanel);
                    sostavB.Items.Remove(sostavB.SelectedItem);
                    (sp.Children[3] as RadButton).Visibility = Visibility.Collapsed;
                    sotavO.Items.Add(sp);// w1.Focus();
                    delete_delicates((int)sp.Tag, 3,(int)NameB.Tag);
                   
                    updateDelColl();
                }
                else { }
            }
            catch(Exception ex) { }
 
        }

        private void typePB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                UpdateSostav();
            }
            catch { }

        }
      public  List<ProductView> lpw = new List<ProductView>();
        public void UpdateSostav()
        {
            sotavO.Items.Clear();
 
            TextBlock tbO;
            TextBox tbOves;
            TextBlock tbOmerra;
            StackPanel prodOsp;
            RadButton rbt;
            
            if ((typePB.SelectedItem as TextBlock).Tag.ToString() == "-1")
            {
                foreach (var list in lpw )
                {
                    tbO = new TextBlock();

                    tbO.TextWrapping = TextWrapping.Wrap;
                    tbO.Width = 250;
                    prodOsp = new StackPanel();
                    prodOsp.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;
                    tbO.MouseLeftButtonDown += tbO_MouseLeftButtonDown;
                    prodOsp.Orientation = Orientation.Horizontal;
                    tbOves = new TextBox();

                    tbOves.Width = 50;
                    // tbOves.IsReadOnly = true;
                    tbOmerra = new TextBlock();
                    tbOmerra.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;

                    prodOsp.Tag = list.ID;
                    tbOmerra.Margin = new Thickness(5, 0, 5, 0);

                    tbO.Text = list.name;

                    tbO.Tag = list.type;

                    tbOmerra.Text = list.ves;

                    // lpw.Add(pw);
                 
                    prodOsp.Children.Add(tbO);
                    prodOsp.Children.Add(tbOves);
                    prodOsp.Children.Add(tbOmerra);
                    rbt = new RadButton();
                    rbt.Width = 20;
                    rbt.Content = "+";
                    rbt.Click += rbt_Click;
                    rbt.Visibility = Visibility.Collapsed;
                    prodOsp.Children.Add(rbt);
                    if (SB.Contains((int)list.ID))
                    {
                    }
                    else
                    {
                        sotavO.Items.Add(prodOsp);
                    }
                }
            }
            else
            {
                foreach (var list in (from t in lpw where t.type == (typePB.SelectedItem as TextBlock).Text.ToString() select t))
                {
                    tbO = new TextBlock();

                    tbO.TextWrapping = TextWrapping.Wrap;
                    tbO.Width = 250;
                    prodOsp = new StackPanel();
                    prodOsp.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;
                    tbO.MouseLeftButtonDown += tbO_MouseLeftButtonDown;
                    prodOsp.Orientation = Orientation.Horizontal;
                    tbOves = new TextBox();

                    tbOves.Width = 50;
                    // tbOves.IsReadOnly = true;
                    tbOmerra = new TextBlock();
                    tbOmerra.MouseLeftButtonDown += prodOsp_MouseLeftButtonDown;

                    prodOsp.Tag = list.ID;
                    tbOmerra.Margin = new Thickness(5, 0, 5, 0);

                    tbO.Text = list.name;

                    tbO.Tag = list.type;

                    tbOmerra.Text = list.ves;

                    // lpw.Add(pw);
               
                    prodOsp.Children.Add(tbO);
                    prodOsp.Children.Add(tbOves);
                    prodOsp.Children.Add(tbOmerra);
                    rbt = new RadButton();
                    rbt.Width = 20;
                    rbt.Content = "+";
                    rbt.Click += rbt_Click;
                    rbt.Visibility = Visibility.Collapsed;
                    prodOsp.Children.Add(rbt);
                    
                    if (SB.Contains((int)list.ID))
                    {
                    }
                    else
                    {
                        sotavO.Items.Add(prodOsp);
                    }
                }
            }

        }

        private void OnSearchTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            foreach (var list in sotavO.Items)
            {
                if (((list as StackPanel).Children[0] as TextBlock).Text.ToString().ToUpper().Contains(searchTextBox.Text.ToUpper())) { sotavO.ScrollIntoView(list); sotavO.SelectedItem = list; break; }
            }

        }

        private void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var list in sotavO.Items)
            {
                if (((list as StackPanel).Children[0] as TextBlock).Text.ToString().ToUpper().Contains(searchTextBox.Text.ToUpper())) { sotavO.ScrollIntoView(list); sotavO.SelectedItem = list; break; }
            }

        }

        private void sotavO_SelectionChanged(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).IsEnabled = true;

                ((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).Focus();
            }
            catch (Exception ex) { ex.ToString(); }

        }

        private void sotavO_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).IsEnabled = true;

                ((sotavO.SelectedItem as StackPanel).Children[1] as TextBox).Focus();
            }
            catch (Exception ex) { ex.ToString(); }
        }

        private void sotavO_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {      

        }

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
         

        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tcMain.SelectedIndex == 1)
            {     //typePB
                UpdateSostavfromDB();
            }
        }

        private void OnSearchTextBoxKeyUp1(object sender, KeyEventArgs e)
        {
            foreach (var list in sostavB.Items)
            {
                if (((list as StackPanel).Children[0] as TextBlock).Text.ToString().ToUpper().Contains(searchTextBox1.Text.ToUpper())) { sostavB.ScrollIntoView(list); sostavB.SelectedItem = list; break; }
            }

        }

        private void OnSearchButtonClick1(object sender, RoutedEventArgs e)
        {
            foreach (var list in sostavB.Items)
            {
                if (((list as StackPanel).Children[0] as TextBlock).Text.ToString().Contains(searchTextBox1.Text.ToUpper())) { sostavB.ScrollIntoView(list); sostavB.SelectedItem = list; break; }
            }
        }
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            try
            {
                int type = Convert.ToInt32((TypeB.SelectedItem as TextBlock).Tag);
                insertDelicates(type, NameB.Text, Convert.ToDecimal(vesB.Text == "" ? "0" : vesB.Text), Convert.ToDecimal(countB.Text == "" ? "0" : countB.Text));
              //  updateDelColl();
                sostavB.Items.Clear();
                Blud.IsEnabled = true;
                GoSost.IsEnabled = false;
                UpdDet.IsEnabled = true;
                UpdD.IsEnabled = true;
                updateDelColl();
            }
            catch { }

        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {   /*
                int type = Convert.ToInt32((TypeB.SelectedItem as TextBlock).Tag);
                insertDelicates(type, NameB.Text, Convert.ToDecimal(vesB.Text == "" ? "0" : vesB.Text), Convert.ToDecimal(countB.Text == "" ? "1" : countB.Text));
                updateDelColl();
                Blud.IsEnabled = true;
                GoSost.IsEnabled = false;
                UpdDet.IsEnabled = true;  */
                GoSost.IsEnabled = true;
                UpdDet.IsEnabled = false;
                UpdD.IsEnabled = false;
                sostavB.Items.Clear();
                NameB.Clear();
                TypeB.SelectedIndex = -1;
                vesB.Clear();
                countB.Clear();
                SB.Clear();
                UpdateSostav();
                updateDelColl();
                Blud.IsEnabled = false;


            }
            catch (Exception ex) 
            {

            }
        }
        int DELID;
        public void insertDelicates(int type,string name,decimal ves,decimal count)
        {
            try
            {
                SqlConnection conn = new SqlConnection(conectionstring);
                SqlCommand scom = new SqlCommand(string.Format(@"
                insert into delicates (del_type,del_name,del_ves,del_count,datew) values ('{0}','{1}','{2}','{3}',getdate())
                select top 1   del_id from      delicates where      del_type='{0}' and   del_name='{1}'   and del_ves='{2}' and del_count='{3}'   order by datew desc
                ", type, name, ves, count), conn);
                conn.Open();
                SqlDataReader myRead = scom.ExecuteReader();
                  while(myRead.Read()) {
                      DELID = myRead.GetInt32(0);
                  }
                conn.Close();
                NameB.Tag = DELID;

            }
            catch(Exception ex)
            { };
 
        }

        private void rtw_Loaded(object sender, RoutedEventArgs e)
        {
            updateDelColl();

        }
        void updateDelColl()
        {
            SqlConnection sqlcon = new SqlConnection(conectionstring);
               List<Components> lcom = new List<Components>();
                try
                {
                    // SqlConnection sqlcon = new SqlConnection(conectionstring);
                    SqlCommand sqlcom1 = new SqlCommand(@"select t.Comp_Id,t.Delic_id,t.ProductID,t1.Name,t.Ves,t3.name_mera ,t2.Type_Opis, isnull(t1.Fass,1)from Components t inner join Producrs t1 on t1.Prod_ID=t.ProductID inner join Produkt_Type t2 on t1.Type=t2.TypeProdId inner join mera t3 on t3.mera_ID=t1.ves", sqlcon);

                    sqlcon.Open();
                    SqlDataReader Myreader1 = sqlcom1.ExecuteReader();
                
                    Components com;
                    while (Myreader1.Read())
                    {
                        com = new Components();
                        com.id = Myreader1.GetInt32(0);
                        com.Delid = Myreader1.GetInt32(1);
                        com.Prodid = Myreader1.GetInt32(2);
                        com.name = Myreader1.GetString(3);
                        com.ves = Myreader1.GetString(4);
                        com.mera = Myreader1.GetString(5);
                        com.type = Myreader1.GetString(6);
                        com.fass = Myreader1.GetDecimal(7);

                        lcom.Add(com);


                    }
                          sqlcon.Close();
                }
                catch(Exception ex) { sqlcon.Close(); }
                try
                {
                SqlCommand sqlcom = new SqlCommand("select del_id,del_name,isnull(del_opis,''),isnull(Del_count,0) ,isnull(del_ves,0) ,Type_del_opis from View_Delicstes where del_type!=-1", sqlcon);
             
                sqlcon.Open();
                SqlDataReader Myreader = sqlcom.ExecuteReader();
                ObservableCollection<DelicatesColl> ldc = new ObservableCollection<DelicatesColl>();
                DelicatesColl dc;
                Components com1;
                List<Components> lc = new List<Components>();
                while (Myreader.Read())
                {
                    dc = new DelicatesColl();
                    dc.id = Myreader.GetInt32(0);
                    dc.name = Myreader.GetString(1);
                    dc.opis = Myreader.GetValue(2).ToString();
                    dc.count = Myreader.GetDecimal(3);
                    dc.ves = Myreader.GetDecimal(4);
                    dc.type = Myreader.GetString(5);
                  //  dc.IDType = Myreader.GetString(5);
                    dc.lcomp = new List<Components>();

                    dc.lcomp.AddRange((from t in lcom where t.Delid == dc.id select t).ToList());


                     
                    
                    ldc.Add(dc);
                }
                sqlcon.Close();
                rtw.ItemsSource = ldc;

            }
            catch(Exception ex) 
            {
                sqlcon.Close();
            }

        }

        private void rtw_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           var t= e.OriginalSource;
        }
        ObservableCollection<DelicatesColl>  sel_delfrID(int id)
        {
            SqlConnection sqlcon = new SqlConnection(conectionstring);
            List<Components> lcom = new List<Components>();
            try
            {
                // SqlConnection sqlcon = new SqlConnection(conectionstring);
                SqlCommand sqlcom1 = new SqlCommand(string.Format(@"select t.Comp_Id,t.Delic_id,t.ProductID,t1.Name,t.Ves,t3.name_mera ,t2.Type_Opis, isnull(t1.Fass,1)from Components t inner join Producrs t1 on t1.Prod_ID=t.ProductID inner join Produkt_Type t2 on t1.Type=t2.TypeProdId inner join mera t3 on t3.mera_ID=t1.ves and t.Delic_id={0} ",id), sqlcon);

                sqlcon.Open();
                SqlDataReader Myreader1 = sqlcom1.ExecuteReader();

                Components com;
                while (Myreader1.Read())
                {
                    com = new Components();
                    com.id = Myreader1.GetInt32(0);
                    com.Delid = Myreader1.GetInt32(1);
                    com.Prodid = Myreader1.GetInt32(2);
                    com.name = Myreader1.GetString(3);
                    com.ves = Myreader1.GetString(4);
                    com.mera = Myreader1.GetString(5);
                    com.type = Myreader1.GetString(6);
                    com.fass = Myreader1.GetDecimal(7);

                    lcom.Add(com);


                }

                sqlcon.Close();
            }
            catch (Exception ex) { sqlcon.Close();  }
            try
            {
                SqlCommand sqlcom = new SqlCommand(string.Format("select del_id,del_name,isnull(del_opis,''),isnull(Del_count,0),isnull(del_ves,0),Type_del_opis,Type_del_id from View_Delicstes where del_id={0} ", id), sqlcon);

                sqlcon.Open();
                SqlDataReader Myreader = sqlcom.ExecuteReader();
                ObservableCollection<DelicatesColl> ldc = new ObservableCollection<DelicatesColl>();
                DelicatesColl dc;
                Components com1;
                List<Components> lc = new List<Components>();
                while (Myreader.Read())
                {
                    dc = new DelicatesColl();
                    dc.id = Myreader.GetInt32(0);
                    dc.name = Myreader.GetString(1);
                    dc.opis = Myreader.GetValue(2).ToString();
                    dc.count = Myreader.GetDecimal(3);    
 dc.ves = Myreader.GetDecimal(4);
                   
                    dc.type = Myreader.GetString(5);
                    dc.IDType = Myreader.GetInt32(6);
                    dc.lcomp = new List<Components>();

                    dc.lcomp.AddRange((from t in lcom where t.Delid == dc.id select t).ToList());




                    ldc.Add(dc);
                }
                return ldc;
                sqlcon.Close();
               // rtw.ItemsSource = ldc;

            }
            catch (Exception ex)
            {
              
                sqlcon.Close();
                return null;
            }

        }
       
        private void RadButton_Click2(object sender, RoutedEventArgs e)
        {
            var t = ((e.OriginalSource as RadButton).CommandParameter as DelicatesColl);
            if (t != null) 
            {
               
              //  var t1 = ((e.OriginalSource as RadButton).CommandParameter as Components);

                var l = sel_delfrID(t.id);
                NameB.Text = l[0].name;
                vesB.Text = l[0].ves.ToString(); ;
                int i = 0,sel=0;
                foreach (var list in TypeB.Items)
                {
                    if ((int)(list as TextBlock).Tag == l[0].IDType)
                    {
                        sel = i;
                    }
                    i++;
                }
                countB.Text = l[0].count.ToString(); ;
                NameB.Tag = l[0].id;
                DELID = l[0].id;
                TypeB.SelectedIndex = sel;     
                GoSost.IsEnabled = false;
                UpdDet.IsEnabled = true;
                UpdD.IsEnabled = true;
                Blud.IsEnabled = true;
               componentsUpdate(l[0].lcomp)  ;
               UpdateSostavfromDB();
               typePB.SelectedIndex = 0;
                    
            }

        }

       
        void delete_delicates(int id,int type,int delIDfProd)
        {
            SqlConnection sqlcon = new SqlConnection(conectionstring);
            if (type == 1)
            {
               
                try
                {
                    //SqlConnection sqlcon = new SqlConnection(conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("exec Delete_Dilicates '{0}' ", id), sqlcon);
                    sqlcon.Open();
                    sqlcomm.ExecuteNonQuery();
                    sqlcomm.Clone();
                }
                catch { sqlcon.Close(); }
            }
            else if (type==2)
            {
                SqlCommand sqlcomm = new SqlCommand(string.Format("delete from components where Comp_ID={0}", id), sqlcon);
                sqlcon.Open();
                sqlcomm.ExecuteNonQuery();
                sqlcomm.Clone();

            }
            else if (type == 3)
            {
                SqlCommand sqlcomm = new SqlCommand(string.Format("delete from components where ProductID={0} and delic_id={1} ", id,delIDfProd), sqlcon);
                sqlcon.Open();
                sqlcomm.ExecuteNonQuery();
                sqlcomm.Clone();

            }
        }

        private void RadButton_Click3(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удолить блюдо?", "Внимание", MessageBoxButton.OKCancel).ToString() == "OK")
            {
                try
                {
                    var t1 = ((e.OriginalSource as RadButton).CommandParameter as DelicatesColl);
                    if (t1 != null)
                    {
                        int t = ((e.OriginalSource as RadButton).CommandParameter as DelicatesColl).id;
                        delete_delicates(t, 1, 0);
                        updateDelColl();
                    }
                    else
                    {
                        int t = ((e.OriginalSource as RadButton).CommandParameter as Components).id;
                        delete_delicates(t, 2, 0);
                        updateDelColl();

                    }
                }
                catch (Exception ex) { }

            }
        }
        
        public void UpdD_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection(conectionstring);
                SqlCommand sqlcom = new SqlCommand(string.Format("update  delicates set del_name='{0}',del_type={1},del_ves={2},del_count={3},datew=getdate() where del_id={4} ", NameB.Text, Convert.ToInt32((TypeB.SelectedItem as TextBlock).Tag), Convert.ToDouble(vesB.Text.Trim() == "" ? "0" : vesB.Text), Convert.ToDouble(countB.Text.Trim() == "" ? "0" : countB.Text), Convert.ToInt32(NameB.Tag)), sqlconn);
                sqlconn.Open();
                sqlcom.ExecuteNonQuery();
                sqlconn.Close();
                updateDelColl();
            }
            catch { }

        }

        private void vesB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789 ,.".IndexOf(e.Text) < 0;
        }

        private void countB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789 ,.".IndexOf(e.Text) < 0;

        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            Dicshen dic = new Dicshen(1);
            dic.ShowDialog();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            Dicshen dic = new Dicshen(2);
            dic.Show();

        }

        private void selChan(object sender, SelectionChangedEventArgs e)
        {
            typePS.SelectedIndex = vesP.SelectedIndex;

        }

        private void addB_Checked(object sender, RoutedEventArgs e)
        {
            if (addB.IsChecked == true)
            {
                countP.Visibility = Visibility.Visible;
                countT.Visibility = Visibility.Visible;
            }
            else 
            {
                countP.Visibility = Visibility.Collapsed;
                countT.Visibility = Visibility.Collapsed;
            }
        }

        private void fg(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789 ,.".IndexOf(e.Text) < 0;
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            Dicshen dcs = new Dicshen(3);
            dcs.ShowDialog();

        }

       
    }
}
