using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;

namespace PaymProd
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
   
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Telerik.Windows.Controls.LocalizationManager.Manager = new TelerikCustomLocalizationManager();
         
        }
        public static string Conectionstring = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";
        //public static string Conectionstring = @"Server=.\SQLExpress;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Database=MenuCaolc;
//Trusted_Connection=Yes;";
        int proverka()
       {
           SqlConnection sqlcon = new SqlConnection(Conectionstring);
           SqlCommand sqlcomm = new SqlCommand("select id,Name,count_people,deteils,dateban from menus where isopen=1",sqlcon);
           sqlcon.Open();
           SqlDataReader sqldr = sqlcomm.ExecuteReader();
           int id = -1;
           while(sqldr.Read())  
           {
               id = sqldr.GetInt32(0);
               FIOM.Tag = id;
               FIOM.Text = sqldr.GetString(1);
               CountM.Text = Convert.ToString(sqldr.GetInt32(2));
               detM.Text = sqldr.GetString(3);
               dtb.SelectedValue = Convert.ToDateTime(sqldr.GetString(4));
               //dtb.SelectedTime = TimeSpan.FromMinutes(sqldr.GetDateTime(4).Hour * 60 + sqldr.GetDateTime(4).Minute);
               men.IsEnabled = true;
               tbForFIO.Tag = id; ;
               tbForFIO.Text ="Банкет:"+ FIOM.Text + " - " + CountM.Text + " Человек, дата - "+(dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
               }

     
        
           sqlcon.Close();
           
           return id;
       }
       void updateAllMen()
       {
           try
           {
               SqlConnection sqlcon = new SqlConnection(Conectionstring);
               SqlCommand sqlcomm = new SqlCommand(@"select id,name,count_people,dateBan,deteils from menus",sqlcon);


               sqlcon.Open();
               List<Menus> lmen = new List<Menus>();
               Menus men;

               SqlDataReader sqlread = sqlcomm.ExecuteReader();

               while (sqlread.Read())
               {
                   men = new Menus();
                   men.id = sqlread.GetInt32(0);
                   men.name = sqlread.GetString(1);
                   men.countP = sqlread.GetInt32(2);
                   men.dateban = sqlread.GetString(3);
                   men.detail = sqlread.GetString(4);
                   lmen.Add(men);

               }
              
              
              
               menu.ItemsSource = lmen;
               //menu.Columns[0].IsVisible = false;
               //menu.Columns[1].Header = "Название";
               //menu.Columns[2].Header = "Количество человек";
               //menu.Columns[3].Header = "Дата";
               //menu.Columns[3].Header = "Доп.Инф";
               sqlcon.Close();
           }
           catch { }

       }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updateAllMen();



            StyleManager.ApplicationTheme = (new Windows8Theme());
            try
            {
                dtb.SelectedDate = DateTime.Now;
                int y = 0;
                if (proverka() >= 0)
                {

                    UpdateMenu_comp(proverka());
                    y = 1;
                }
                else { lmda1.Clear(); }
                nametype = "%";
                Update("%%");
                if (y == 1)
                {
                    foreach (var list in lb.Children)
                    {
                        if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                        {
                            (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                        }
                        else
                        {
                            (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                        }
                    }
                    y = 0;
                }
            }
            catch(Exception ex) { }
        }
        void Update(string type)
        {
            SqlConnection sqlcon = new SqlConnection(@"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True");
            if (type == "%%")
            {
                SqlCommand sqlcom = new SqlCommand(@"
                
                select distinct t.Type_del_opis  from type_Del t
union 
select distinct Type_Opis from View_Producte  where priz_menu=1
                ", sqlcon);
                sqlcon.Open();
                SqlDataReader Myreader = sqlcom.ExecuteReader();
                Button tb;
             
                tb = new Button();
                tb.Click += tb_Click1;
                tb.Margin = new Thickness(3, 3, 3, 3);
                tb.Width = 100;
                tb.Height = 25;
                tb.Tag ="%";
                tb.Content ="Все";
                wp1.Children.Add(tb);
                
                while (Myreader.Read())
                {
                    tb = new Button();
                    tb.Click += tb_Click;
                    tb.Margin = new Thickness(3, 3, 3, 3);
                    tb.Width = 100;
                    tb.Height = 25;
                  //  tb.Tag = Myreader.GetInt32(0);
                    tb.Content = Myreader.GetString(0);
                    wp1.Children.Add(tb);

                }
                sqlcon.Close();
            }
            lb.Children.Clear();
            SqlDataAdapter sqld = new SqlDataAdapter(string.Format("select * from View_Delicstes where Type_del_opis like '{0}'",type), sqlcon);
            DataSet ds = new DataSet();
            sqld.Fill(ds, "View_Delicstes");
            SqlDataAdapter sqld1 = new SqlDataAdapter("select * from view_comp", sqlcon);
            sqld1.Fill(ds, "view_comp");
            TextBlock butt,tbl,tblves;
            TextBox tbp;
            StackPanel sp;Grid sp1;
            Telerik.Windows.Controls.RadButton bt;
            Button tb2;
            foreach (DataRow list in ds.Tables[0].Rows)
            {
                bt = new Telerik.Windows.Controls.RadButton();
                tbp = new TextBox();
                tb2 = new Button();
                tbp.KeyUp += tbp_KeyUp;
                tbp.Width = 100;
                tbp.Height = 30;
                tbp.Margin = new Thickness(0, 0, 100, 0);
                tbp.HorizontalAlignment = HorizontalAlignment.Right;
                tb2.Content = "+";
                tbp.VerticalAlignment = VerticalAlignment.Top;
               // tbp.Text = CountM.Text;
                bt.Click += bt_Click;
                bt.MouseDoubleClick += bt_MouseDoubleClick;
                bt.KeyDown += bt_KeyDown;
                bt.MouseLeftButtonDown += bt_MouseLeftButtonDown;
                bt.MouseDown += bt_MouseDown;
                bt.HorizontalAlignment = HorizontalAlignment.Stretch;
                bt.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                sp = new StackPanel();
                sp.Tag = list[0];
                sp1 = new Grid();
                sp1.HorizontalAlignment = HorizontalAlignment.Stretch;
                tblves = new TextBlock();
                sp1.HorizontalAlignment = HorizontalAlignment.Stretch;
                //MainWindow mw = new MainWindow();
                //sp1.Width = mw.Width;
                tbl=new TextBlock();
         
                butt = new TextBlock();
                tblves.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right);
                butt.Text = (string)list[2];
                butt.FontSize = 14;
                tbl.Text = "Состав: ";
                tbl.TextWrapping = TextWrapping.Wrap;
                tbl.Width = 800;
                foreach (DataRow list1 in ds.Tables[1].Rows)
                {
                    if (list[0].ToString() == list1[0].ToString())
                    {
                        tbl.Text += list1[1].ToString()+", ";
                    }
                }
                sp1.Children.Add(tbp);
                if ((decimal)list[6] == 0) {
                    if ((decimal)list[5] != 0)
                    {
                        tblves.Text = list[5].ToString() + "г";
                        tblves.Tag = -1;
                    }
                    else { tblves.Text = "Порция"; tblves.Tag = -1; }
                }
                else
                {
                    tblves.Tag = list[6].ToString();
                    tblves.Text = list[6].ToString() + "шт";
                }
                
                sp.Children.Add(butt);
                sp.Children.Add(tbl);
                sp1.Children.Add(sp);
                sp1.Children.Add(tblves);
              //  sp1.Children.Add(tb2);
                bt.Content= sp1;
                if ((from t in lmda1 where t.del_id == (int)sp.Tag select t).Count() == 0)
                {
                    lb.Children.Add(bt);
                }
            }
      //   sqlcom = new SqlCommand(@"select t.Del_Name,t2.Type_del_opis,t.Del_count,t.Del_Ves,  t.del_id,t.Del_Type
//from Delicates t inner join Type_Del t2 on t2.Type_Del_ID=t.Del_Type ", myConnection);

        }

        void tbp_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {

              //  bt_MouseDoubleClick(((sender as TextBox).Parent as RadButton), null);
            }
            //throw new NotImplementedException();
        }

        void bt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) 
            {
                bt_MouseDoubleClick(sender,null);
            }
            //throw new NotImplementedException();
        }

        void bt_Click(object sender, RoutedEventArgs e)
        {
            var t = e.OriginalSource;
           (((t as RadButton).Content as Grid).Children[0] as TextBox).Focus();


            //throw new NotImplementedException();
        }

        void bt_MouseDown(object sender, MouseButtonEventArgs e)
        {
          //  throw new NotImplementedException();
            var t = e.OriginalSource;
        }

        void bt_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var t = e.OriginalSource;
            //throw new NotImplementedException();
        }

        RadButton r;
        void bt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if(r==(sender as RadButton)) {}else{
                r = (sender as RadButton);
               // (sender as RadButton).Visibility = Visibility.Collapsed;
               // men.IsEnabled = false;
               
                var t = (((sender as RadButton).Content as Grid).Children[1] as StackPanel).Tag;
                var t1 = (((sender as RadButton).Content as Grid).Children[0] as TextBox)  ;
                SqlConnection sqlcon = new SqlConnection(Conectionstring);
                SqlCommand sqlcomm = new SqlCommand(string.Format("insert into Menu_Delicates (id_men,id_delic,delcount) values ('{0}','{1}','{2}')", (int)FIOM.Tag, t,Convert.ToInt32( t1.Text)), sqlcon);
                   
                sqlcon.Open();
                    sqlcomm.ExecuteNonQuery();
                    sqlcon.Close();
                    lb.Children.Remove((sender as RadButton));
                    UpdateMenu_comp((int)FIOM.Tag);

                   // men.IsEnabled = true ;
            }                               }
            catch (Exception ex) { MessageBox.Show("Некоректно введенно количество"); }   
        }
        void tb_Click1(object sender, RoutedEventArgs e)
        {
            Update("%");
            nametype = (sender as Button).Content.ToString();
            foreach (var list in lb.Children)
            {
                if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                {
                    (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                }
                else
                {
                    (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                }
            }
            //throw new NotImplementedException();
        }
        void tb_Click(object sender, RoutedEventArgs e)
        {
            Update((sender as Button).Content.ToString());
            nametype = (sender as Button).Content.ToString();
            foreach (var list in lb.Children)
            {
                if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                {
                    (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                }
                else
                {
                    (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                }
            }
            //throw new NotImplementedException();
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window1 w1 = new Window1();
            w1.ShowDialog();
            wp1.Children.Clear();
            Window_Loaded(null, null);

        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
          

        }
        ObservableCollection<MenuDel_act> lmda1 = new ObservableCollection<MenuDel_act>();

        void UpdateMenu_comp(int id)
        {
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
            SqlDataAdapter sqlad = new SqlDataAdapter(string.Format(@"select * from menuDel where id_men={0}", id), sqlcon);
           List<Components> lcom = new List<Components>();


                try
                {
                    // SqlConnection sqlcon = new SqlConnection(conectionstring);
                    SqlCommand sqlcom1 = new SqlCommand(@"select t.Comp_Id,t.Delic_id,t.ProductID, CASE WHEN isnull(rtrim(ltrim(t.Detail)), '') = '' OR
                      t .Detail IS NULL THEN t1.Name ELSE t1.Name + '(' + t.Detail + ')' END AS Name
,t.Ves,t3.name_mera ,t2.Type_Opis, case when t1.Fass=0  then isnull(t3.Fass_Def,1) else isnull(isnull(t1.Fass,t3.Fass_Def),1) end,

 isnull(case when  t1.Izmer=t1.ves  then  t3.fass_izmer else isnull( (select t5.name_mera from mera t5 where t5.mera_ID=t1.Izmer ),t3.fass_izmer) end, (select t5.name_mera from mera t5 where t5.mera_ID=t1.Izmer ))
 
 
 ,case when  t1.Izmer!=t1.ves and t1.Izmer is not null then 1 else 0 end  ,t1.name
 from Components t inner join Producrs t1 on t1.Prod_ID=t.ProductID inner join Produkt_Type t2 on t1.Type=t2.TypeProdId inner join mera t3 on t3.mera_ID=t1.ves       --  inner join       mera t4 on t4.mera_ID=t1.izmer
 
 ", sqlcon);

                    sqlcon.Open();
                    SqlDataReader Myreader1 = sqlcom1.ExecuteReader();
                
                    Components com;
                    while (Myreader1.Read())
                    {
                        try
                        {
                            com = new Components();
                            com.id = Myreader1.GetInt32(0);
                            com.Delid = Myreader1.GetInt32(1);
                            com.Prodid = Myreader1.GetInt32(2);
                            com.nameT = Myreader1.GetString(3);
                            com.ves = Myreader1.GetString(4);
                            com.mera = Myreader1.GetString(5);
                            com.type = Myreader1.GetString(6);
                            com.fass = Myreader1.GetDecimal(7);
                            com.fassIz = Myreader1.GetString(8);
                            com.flag = Myreader1.GetInt32(9);
                            com.name = Myreader1.GetString(10);
                            lcom.Add(com);
                        }
                        catch { }

                    }
                          sqlcon.Close();
                }
                catch(Exception ex) { sqlcon.Close(); }


            DataSet ds = new DataSet();
            sqlad.Fill(ds, "menuDel");
            MenuDel_act mda ;//= new MenuDel_act();
            ObservableCollection<MenuDel_act> lmda = new ObservableCollection<MenuDel_act>();

            try
            {
                foreach (DataRow list in ds.Tables[0].Rows)
                {
                    mda = new MenuDel_act();
                    mda.idmen = (int)list[0];
                    mda.del = list[6].ToString();
                    mda.del_id = Convert.ToInt32(list[2]);
                    mda.countpor = (int)list[3];
                    mda.lcomp = (from t in lcom where t.Delid == mda.del_id select t).ToList();
                    try
                    {
                        foreach (var list1 in (from t in lcom where t.Delid == mda.del_id select t).ToList())
                        {
                            mda.sost += list1.nameT + ",";
                        }
                    }
                    catch { }
                    try
                    {
                        
                           mda.sost=  mda.sost.Remove(mda.sost.Length - 1);
                        
                    }
                    catch { }
                    lmda.Add(mda);
                }
                rtv.ItemsSource = lmda;
                lmda1 = lmda;
            }
            catch(Exception ex) { }

        }
        void UpdateMenu()
        {
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
             SqlDataAdapter sqlad = new  SqlDataAdapter(string.Format(@"select * from menus where id={0}", (int)FIOM.Tag), sqlcon);
            sqlcon.Open();
            DataSet ds = new DataSet();
            sqlad.Fill(ds, "menus");
         
            foreach( DataRow list in ds.Tables[0].Rows)
            {

                tbForFIO.Tag = (int)list[0];
                tbForFIO.Text ="Банкед: "+ (string)list[1]+" - "+ list[2].ToString()+" Человек, в "+ list[6].ToString();
            }

            sqlcon.Close();
            

        }
        private void RadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FIOM.Tag == null)
                {
                    if (CountM.Text.Length > 0 || FIOM.Text.Length > 0)
                    {
                        foreach (var list in lb.Children)
                        {
                            if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                            {
                                (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                            }
                            else
                            {
                                (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                            }
                        }

                        SqlConnection sqlcon = new SqlConnection(Conectionstring);
                        var t = dtb.SelectedValue;//.Value.ToString() + " " + dtb.SelectedTime.ToString();
                        SqlCommand sqlcom = new SqlCommand(string.Format(@"insert into menus (name,count_people,deteils,datew,isopen,dateban) values ('{0}',{1},'{2}',getdate(),{3},'{4}')", FIOM.Text, Convert.ToInt32(CountM.Text), detM.Text, 1, Convert.ToDateTime( dtb.SelectedValue).ToString()), sqlcon);
                        sqlcon.Open();
                        sqlcom.ExecuteNonQuery();
                        sqlcon.Close();
                        sqlcom = new SqlCommand(string.Format(@"select top 1 id from menus order by datew desc"), sqlcon);
                        sqlcon.Open();
                        SqlDataReader sqlr = sqlcom.ExecuteReader();

                        while (sqlr.Read())
                        {
                            FIOM.Tag = sqlr.GetInt32(0);
                        }
                        sqlcon.Close();

                        men.IsEnabled = true;
                        // FIOM.IsEnabled = false;
                        // CountM.IsEnabled = false;
                        //  detM.IsEnabled = false;
                        UpdateMenu();
                        updateAllMen();
                        tbForFIO.Text = "";
                        tbForFIO.Text = "Банкет:" + FIOM.Text + " - " + CountM.Text + " Человек, дата - " + (dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
             
                    }
                    else { MessageBox.Show("Не все поля заполненны!"); }
                }
                else { MessageBox.Show("Сначало закончите данное меню м кликнете на кнопку начать новое!"); }
            }
            catch(Exception ex) { }   

        }

        private void detM_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789 ,".IndexOf(e.Text) < 0;

        }
        string nametype;
        private void RadButton_Click3(object sender, RoutedEventArgs e)
        {
            SqlConnection sqlconn = new SqlConnection(Conectionstring);
               
            try
            {
                var t = ((e.OriginalSource as RadButton).CommandParameter as MenuDel_act);
                SqlCommand sqlcomm = new SqlCommand(string.Format("delete from menu_delicates where id={0} ", t.idmen), sqlconn);

                sqlconn.Open();
                sqlcomm.ExecuteNonQuery();
                sqlconn.Close();
             
               // UpdateMenu_comp((int)FIOM.Tag);
                rtv.Items.Remove(t);
                Update(nametype == "Все" ? "%" : nametype);
                foreach (var list in lb.Children)
                {
                    if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                    {
                        (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                    }
                    else
                    {
                        (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                    }
                }

            }
            catch(Exception ex) { sqlconn.Close(); }

        }

        private void RadButton_Click2(object sender, RoutedEventArgs e)
        {
        

            

        }

        void UpdateStatusMen(int id)
        {
            SqlConnection sqlconn = new SqlConnection(Conectionstring);
            SqlCommand sqlcomm = new SqlCommand(string.Format("update menus set isopen=0 where id='{0}' ",id),sqlconn);
            sqlconn.Open();
            sqlcomm.ExecuteNonQuery();
            sqlconn.Close();
        }
        private void RadButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusMen((int)FIOM.Tag);
                FIOM.Clear();
                CountM.Clear();
                detM.Clear();
                FIOM.Tag = null;
                FIOM.Text = "";
                CountM.Text = "";
                detM.Text = "";
                men.IsEnabled = false;

                rtv.ItemsSource = null;
                wp1.Children.Clear();
                Window_Loaded(null, null);
            }
            catch(Exception ex) { }
            

        }

        public Components addtiprod(int id)
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection(Conectionstring);
                SqlCommand sqlcomm = new SqlCommand(string.Format(" prod_for_men  {0} ", id), sqlconn);
                sqlconn.Open();
                SqlDataReader sqldr = sqlcomm.ExecuteReader();
                List<Components> lcom = new List<Components>();
                Components com;
                while (sqldr.Read())
                {
                    com = new Components();
                    com.Prodid = sqldr.GetInt32(0);
                    com.name = sqldr.GetString(1);
                    com.type = sqldr.GetString(2);
                    com.mera = sqldr.GetString(3);
                    com.fass = sqldr.GetDecimal(6);

                    com.count = sqldr.GetDecimal(10);
                    com.ves = sqldr.GetDecimal(10).ToString();
                    com.fassIz = sqldr.GetString(8);
                    //  com.mera=
                    // com.d
                    lcom.Add(com);


                }
             

                sqlconn.Close();
                return lcom[0];
            }
            catch { return null; }
 
        }
        private void RadButton_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var list in lmda1)
                {
                    if (list.lcomp.Count() == 0)
                    {
                        list.lcomp.Add(addtiprod(list.del_id));
                    }
                }

                List<string> ls = new List<string>();
                ls.Add(FIOM.Text);
                ls.Add(CountM.Text);
                ls.Add((dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString());
                ls.Add(detM.Text);
                Print pr = new Print(lmda1, ls);
                pr.ShowDialog();
            }
            catch { }

        }

        private void RadButton_Click5(object sender, RoutedEventArgs e)
        {
            var y = ((e.OriginalSource as RadButton).CommandParameter as Menus);
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
            SqlCommand sqlcomm = new SqlCommand(string.Format(@"
            delete from menus where id={0}
delete      from menu_delicates where id_men={0}
            ",y.id),sqlcon);
            sqlcon.Open();
            sqlcomm.ExecuteNonQuery();
            sqlcon.Close();
            updateAllMen();
            FIOM.Clear();
            CountM.Clear();
            detM.Clear();
            FIOM.Tag = null;
            FIOM.Text = "";
            CountM.Text = "";
            detM.Text = "";
            men.IsEnabled = false;

            rtv.ItemsSource = null;
            wp1.Children.Clear();
            Window_Loaded(null, null);
           
        }

        private void RadButton_Click6(object sender, RoutedEventArgs e)
        {
            var y = ((e.OriginalSource as RadButton).CommandParameter as Menus);
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
            SqlCommand sqlcomm = new SqlCommand(string.Format(@"
            update  menus set isopen=0
               update  menus set isopen=1 where id={0}

", y.id), sqlcon);
            sqlcon.Open();
            sqlcomm.ExecuteNonQuery();
            sqlcon.Close();
            updateAllMen();
            FIOM.Clear();
            CountM.Clear();
            detM.Clear();
            FIOM.Tag = null;
            FIOM.Text = "";
            CountM.Text = "";
            detM.Text = "";
            men.IsEnabled = false;

            rtv.ItemsSource = null;
            wp1.Children.Clear();
            Window_Loaded(null, null);

            rt.SelectedIndex = 0;
        }

        private void FIOM_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (men.IsEnabled == true)
                {
                    SqlConnection sqlcon = new SqlConnection(Conectionstring);
                    SqlCommand sqlcom = new SqlCommand(string.Format(@"update menus set name='{0}',count_people={1},deteils='{2}',dateban='{4}'", FIOM.Text, Convert.ToInt32(CountM.Text), detM.Text, 1,Convert.ToDateTime(dtb.SelectedValue).ToString()), sqlcon);
                    sqlcon.Open();
                    sqlcom.ExecuteNonQuery();
                    sqlcon.Close();
                    tbForFIO.Text = "Банкет:" + FIOM.Text + " - " + CountM.Text + " Человек, дата - " + (dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
                    updateAllMen();
                    foreach (var list in lb.Children)
                    {
                        if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                        {
                            (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                        }
                        else
                        {
                            (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                        }
                    }
                }
            }
            catch { }

        }

        private void d(object sender, SelectionChangedEventArgs e)
        {
            if (dtb.IsDropDownOpen == true)
            {
                try
                {
                    if (men.IsEnabled == true)
                    {
                        SqlConnection sqlcon = new SqlConnection(Conectionstring);
                        SqlCommand sqlcom = new SqlCommand(string.Format(@"update menus set name='{0}',count_people={1},deteils='{2}',dateban='{4}'", FIOM.Text, Convert.ToInt32(CountM.Text), detM.Text, 1, Convert.ToDateTime(dtb.SelectedValue).ToString()), sqlcon);
                        sqlcon.Open();
                        sqlcom.ExecuteNonQuery();
                        sqlcon.Close();
                        tbForFIO.Text = "Банкет:" + FIOM.Text + " - " + CountM.Text + " Человек, дата - " + (dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
                        updateAllMen();
                        foreach (var list in lb.Children)
                        {
                            if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                            {
                                (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                            }
                            else
                            {
                                (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                            }
                        }
                    }
                }
                catch { }

            }

        }

        private void s_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var list in lmda1)
                {
                    if (list.lcomp.Count() == 0)
                    {
                        list.lcomp.Add(addtiprod(list.del_id));
                    }
                }

                List<string> ls = new List<string>();
                ls.Add(FIOM.Text);
                ls.Add(CountM.Text);
                ls.Add((dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString());
                ls.Add(detM.Text);
                Print pr = new Print(lmda1, ls);
                pr.ShowDialog();
            }
            catch { }

        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Разработчик данного программного продукта Карпец А.В.");

        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            Process.Start("I.docx"); 

        }
    }
    public class Menus
    {
        public int id { get; set; }
        public string name { get; set; }
        public int countP { get; set; }
        public string dateban { get; set; }
        public string detail { get; set; }
    }
}
