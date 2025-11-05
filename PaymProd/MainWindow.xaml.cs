using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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
using System.Xml.Linq;
using Telerik.Windows.Controls;
using PaymProd.Class;
using Word = Microsoft.Office.Interop.Word;
using System.IO;
using System.Collections.ObjectModel;
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
            SqlCommand sqlcomm = new SqlCommand("select id,Name,count_people,deteils,dateban from menus where isopen=1", sqlcon);
            sqlcon.Open();
            SqlDataReader sqldr = sqlcomm.ExecuteReader();
            int id = -1;
            while (sqldr.Read())
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
                tbForFIO.Text = "Банкет:" + FIOM.Text + " - " + CountM.Text + " Человек, дата - " + (dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
            }



            sqlcon.Close();

            return id;
        }
        void updateAllMen()
        {
            try
            {
                SqlConnection sqlcon = new SqlConnection(Conectionstring);
                SqlCommand sqlcomm = new SqlCommand(@"select id,name,count_people,dateBan,deteils from menus", sqlcon);


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
        public void GenerateButtonText(int type)
        {


            foreach (var list in lb.Children)
            {
                if (((list as RadButton).Tag as DataRow)["exp1"].ToString() == "1")
                {
                    (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = ((list as RadButton).Tag as DataRow)["CountChel"].ToString();

                }
                else
                    if (Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag) == -1)
                    {
                        (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = CountM.Text;
                    }
                    else
                    {
                        (((list as RadButton).Content as Grid).Children[0] as TextBox).Text = Convert.ToInt32((Convert.ToDecimal(CountM.Text) * Convert.ToDecimal((((list as RadButton).Content as Grid).Children[2] as TextBlock).Tag))).ToString();
                    }

                if (type == 1)
                {

                    if (((list as RadButton).Tag as DataRow)["avtoadd"].ToString() == "1" && ((list as RadButton).Tag as DataRow)["Del_Type"].ToString()=="-1") 
                    {
                        bt_MouseDoubleClick(list, null);

                    }

                }

            }

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            XDocument xDocument = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "\\conect.xml");
            try
            {
                producteUpdate();
                string t = xDocument.Element("connect").Element("user").Value;
                if (t.Trim() == "")
                {
                    Window2 w2 = new Window2();
                    w2.ShowDialog();
                }


            }
            catch (Exception ex) {/* MessageBox.Show(ex.ToString());*/ }


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
                    GenerateButtonText(0);
                    y = 0;
                }
            }
            catch (Exception ex) { }
            rabt = 0;
        }
        void Update(string type)
        {

            SqlConnection sqlcon = new SqlConnection(@"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True");
            if (type == "%%")
            {
                wp1.Children.Clear();
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
                tb.Tag = "%";
                tb.Content = "Все";
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
            SqlDataAdapter sqld = new SqlDataAdapter(string.Format("select * from View_Delicstes where Type_del_opis like '{0}'", type), sqlcon);
            DataSet ds = new DataSet();
            sqld.Fill(ds, "View_Delicstes");
            SqlDataAdapter sqld1 = new SqlDataAdapter("select * from view_comp", sqlcon);
            sqld1.Fill(ds, "view_comp");
            TextBlock butt, tbl, tblves;
            TextBox tbp;
            StackPanel sp; Grid sp1;
            Telerik.Windows.Controls.RadButton bt;
            Button tb2;
            foreach (DataRow list in ds.Tables[0].Rows)
            {
                bt = new Telerik.Windows.Controls.RadButton();
                bt.Tag = list;
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
                tbl = new TextBlock();

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
                        tbl.Text += list1[1].ToString() + ", ";
                    }
                }
                sp1.Children.Add(tbp);
                if ((decimal)list[6] == 0)
                {
                    if ((decimal)list[5] != 0)
                    {
                        tblves.Text = list[5].ToString() + "г";
                        tblves.Tag = -1;
                    }
                    else
                    {
                        tblves.Text = "Порция"; tblves.Tag = -1;
                    }
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
                bt.Content = sp1;
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
                bt_MouseDoubleClick(sender, null);
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
        void addHoz(int i, int i1)
        {
            try
            {
                //if (r == (sender as RadButton)) { }
                // else
                {

                    // (sender as RadButton).Visibility = Visibility.Collapsed;
                    // men.IsEnabled = false;

                    var t = i;
                    var t1 = i1;
                    SqlConnection sqlcon = new SqlConnection(Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("insert into Menu_Delicates (id_men,id_delic,delcount) values ('{0}','{1}','{2}')", (int)FIOM.Tag, t, Convert.ToInt32(t1)), sqlcon);

                    sqlcon.Open();
                    sqlcomm.ExecuteNonQuery();
                    sqlcon.Close();
                    //lb.Children.Remove((sender as RadButton));
                    UpdateMenu_comp1((int)FIOM.Tag, Convert.ToInt32(t));

                    // men.IsEnabled = true ;
                }
            }
            catch (Exception ex) { MessageBox.Show("Некоректно введенно количество"); }
        }
        RadButton r;
        void bt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (r == (sender as RadButton)) { }
                else
                {
                    r = (sender as RadButton);
                    // (sender as RadButton).Visibility = Visibility.Collapsed;
                    // men.IsEnabled = false;

                    var t = (((sender as RadButton).Content as Grid).Children[1] as StackPanel).Tag;
                    var t1 = (((sender as RadButton).Content as Grid).Children[0] as TextBox);
                    SqlConnection sqlcon = new SqlConnection(Conectionstring);
                    SqlCommand sqlcomm = new SqlCommand(string.Format("insert into Menu_Delicates (id_men,id_delic,delcount) values ('{0}','{1}','{2}')", (int)FIOM.Tag, t, Convert.ToInt32(t1.Text)), sqlcon);

                    sqlcon.Open();
                    sqlcomm.ExecuteNonQuery();
                    sqlcon.Close();

                    if (e != null)
                    {
                        lb.Children.Remove((sender as RadButton));
                                                
                    }
                    UpdateMenu_comp1((int)FIOM.Tag, Convert.ToInt32(t));
                    // men.IsEnabled = true ;
                }
            }
            catch (Exception ex) { MessageBox.Show("Некоректно введенно количество"); }
        }
        void tb_Click1(object sender, RoutedEventArgs e)
        {
            Update("%");
            nametype = (sender as Button).Content.ToString();
            GenerateButtonText(0);
            //throw new NotImplementedException();
        }
        void tb_Click(object sender, RoutedEventArgs e)
        {
            Update((sender as Button).Content.ToString());
            nametype = (sender as Button).Content.ToString();
            GenerateButtonText(0);
            //throw new NotImplementedException();
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window1 w1 = new Window1();
            w1.ShowDialog();
            // wp1.Children.Clear();
            // Window_Loaded(null, null);

        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {


        }
        ObservableCollection<MenuDel_act> lmda1 = new ObservableCollection<MenuDel_act>();
        void UpdateMenu_comp1(int id, int id_bl)
        {
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
            SqlDataAdapter sqlad = new SqlDataAdapter(string.Format(@"select * from menuDel where id_men={0} and id_delic={1}", id, id_bl), sqlcon);
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
                        com.ves = Myreader1.GetDecimal(4);
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
            catch (Exception ex) { sqlcon.Close(); }


            DataSet ds = new DataSet();
            sqlad.Fill(ds, "menuDel");
            MenuDel_act mda;//= new MenuDel_act();
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
                    var ff = (int)list["ff"];
                    if (ff != 1)
                    {
                        mda.lcomp = (from t in lcom where t.Delid == mda.del_id  select t).ToList();
                    }
                    else
                    {
                        mda.lcomp = new List<Components>();
                    }
                    
                    try
                    {
                        foreach (var list1 in mda.lcomp) 
                        {
                            mda.sost += list1.nameT + ",";
                        }
                    }
                    catch { }
                    try
                    {

                        mda.sost = mda.sost.Remove(mda.sost.Length - 1);

                    }
                    catch { }
                    lmda.Add(mda);
                }
                // rtv.Items.Add(lmda);

                foreach (var list in lmda)
                {
                    if (list.lcomp.Count == 0)
                    {
                        list.lcomp.Add(addtiprod(list.del_id));
                    }
                    else { }

                    lmda1.Add(list);

                    //  rtv.Items.Add(list);
                }


                rtv.ItemsSource = lmda1;

                //  lmda1.Add(lmda);


            }
            catch (Exception ex) { }

        }
        ObservableCollection<MenuDel_act> lmda = new ObservableCollection<MenuDel_act>();

        void UpdateMenu_comp(int id)
        {
             lmda = new ObservableCollection<MenuDel_act>();
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
            SqlDataAdapter sqlad;
            SqlCommand sqlad1;
            if (rabt == 0)
            {

                sqlad = new SqlDataAdapter(string.Format(@"
select * from menuDel where id_men={0}
update menus set ifchan=0 where id={0}
", id), sqlcon);
            }
            else
            {
                sqlad1 = new SqlCommand(string.Format(@"
update menus set ifchan=1 where id={0}

", id), sqlcon);
                sqlcon.Open();
                sqlad1.ExecuteNonQuery();
                sqlcon.Close();




                sqlad = new SqlDataAdapter(string.Format(@"
select * from menuDel where id_men={0}

", id), sqlcon);
            }

            // Components com;
            List<Components> lcom = new List<Components>();

            //тут мы заполняем меню
            try
            {


                SqlCommand sqlcom1 = new SqlCommand(string.Format(@"

declare @i int;

set @i= (select max(isnull(ifchan,0)) from menus where id={0})
if(@i=0)
begin
select t.Comp_Id,t.Delic_id,t.ProductID, CASE WHEN isnull(rtrim(ltrim(t.Detail)), '') = '' OR
                      t .Detail IS NULL THEN t1.Name ELSE t1.Name + '(' + t.Detail + ')' END AS Name
,t.Ves,t3.name_mera ,t2.Type_Opis, case when t1.Fass=0  then isnull(t3.Fass_Def,1) else isnull(isnull(t1.Fass,t3.Fass_Def),1) end,

 isnull(case when  t1.Izmer=t1.ves  then  t3.fass_izmer else isnull( (select t5.name_mera from mera t5 where t5.mera_ID=t1.Izmer ),t3.fass_izmer) end, (select t5.name_mera from mera t5 where t5.mera_ID=t1.Izmer ))
 
 
 ,case when  t1.Izmer!=t1.ves and t1.Izmer is not null then 1 else 0 end  ,t1.name
 from Components t inner join Producrs t1 on t1.Prod_ID=t.ProductID inner join Produkt_Type t2 on t1.Type=t2.TypeProdId inner join mera t3 on t3.mera_ID=t1.ves 
 end
 if(@i=1)
begin
select t.Comp_Id,t.Delic_id,t.ProductID, CASE WHEN isnull(rtrim(ltrim(t.Detail)), '') = '' OR
                      t .Detail IS NULL THEN t1.Name ELSE t1.Name + '(' + t.Detail + ')' END AS Name
,t.Ves,t3.name_mera ,t2.Type_Opis, case when t1.Fass=0  then isnull(t3.Fass_Def,1) else isnull(isnull(t1.Fass,t3.Fass_Def),1) end,

 isnull(case when  t1.Izmer=t1.ves  then  t3.fass_izmer else isnull( (select t5.name_mera from mera t5 where t5.mera_ID=t1.Izmer ),t3.fass_izmer) end, (select t5.name_mera from mera t5 where t5.mera_ID=t1.Izmer ))
 
 
 ,case when  t1.Izmer!=t1.ves and t1.Izmer is not null then 1 else 0 end  ,t1.name
 from Components1 t inner join Producrs t1 on t1.Prod_ID=t.ProductID inner join Produkt_Type t2 on t1.Type=t2.TypeProdId inner join mera t3 on t3.mera_ID=t1.ves 
and t.idmen={0}
 end


 ", id), sqlcon);
      
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
                        com.ves = Myreader1.GetDecimal(4);
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
            catch (Exception ex) { sqlcon.Close(); }


            DataSet ds = new DataSet();
            sqlad.Fill(ds, "menuDel");
            MenuDel_act mda;//= new MenuDel_act();

            try
            {
                foreach (DataRow list in ds.Tables[0].Rows)
                {
                    mda = new MenuDel_act();
                    mda.idmen = (int)list[0];
                    mda.del = list[6].ToString();
                    mda.del_id = Convert.ToInt32(list[2]);
                    mda.countpor = (int)list[3];

                    if ((int)list["ff"] != 1)
                    {
                        mda.lcomp = (from t in lcom where t.Delid == mda.del_id select t).ToList();
                    }
                    else 
                    {
                        mda.lcomp = new List<Components>();
                    }
                    try
                    {
                        foreach (var list1 in mda.lcomp)
                        {
                            mda.sost += list1.nameT + ",";
                        }
                    }
                    catch { }
                    try
                    {

                        mda.sost = mda.sost.Remove(mda.sost.Length - 1);

                    }
                    catch { }
                    lmda.Add(mda);
                }

           
                rtv.ItemsSource = lmda;
                lmda1 = lmda;

                CollectionViewSource shin = this.Resources["nameT"] as CollectionViewSource;


                shin.Source = (from t in lcom select t.name).Distinct().ToList();

                CollectionViewSource mera = this.Resources["mera"] as CollectionViewSource;


                mera.Source = (from t in lcom select t.mera).Distinct().ToList();

            }
            catch (Exception ex) { }

        }
        void UpdateMenu()
        {
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
            SqlDataAdapter sqlad = new SqlDataAdapter(string.Format(@"select * from menus where id={0}", (int)FIOM.Tag), sqlcon);
            sqlcon.Open();
            DataSet ds = new DataSet();
            sqlad.Fill(ds, "menus");

            foreach (DataRow list in ds.Tables[0].Rows)
            {

                tbForFIO.Tag = (int)list[0];
                tbForFIO.Text = "Банкед: " + (string)list[1] + " - " + list[2].ToString() + " Человек, в " + list[6].ToString();
            }

            sqlcon.Close();


        }

        /// <summary>
        /// Перейти к меню
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadButton_Click(object sender, RoutedEventArgs e)
        {
            //Перейти к меню
            try
            {
                if (FIOM.Tag == null)
                {
                    if (CountM.Text.Length > 0 || FIOM.Text.Length > 0)
                    {
                 

                        SqlConnection sqlcon = new SqlConnection(Conectionstring);
                        var t = dtb.SelectedValue;//.Value.ToString() + " " + dtb.SelectedTime.ToString();
                        SqlCommand sqlcom = new SqlCommand(string.Format(@"insert into menus (name,count_people,deteils,datew,isopen,dateban) values ('{0}',{1},'{2}',getdate(),{3},'{4}')", FIOM.Text, Convert.ToInt32(CountM.Text), detM.Text, 1, Convert.ToDateTime(dtb.SelectedValue).ToString()), sqlcon);
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
                        UpdateMenu();
                        updateAllMen();
                        tbForFIO.Text = "";
                        tbForFIO.Text = "Банкет:" + FIOM.Text + " - " + CountM.Text + " Человек, дата - " + (dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
                        GenerateButtonText(1);

                    }
                    else { MessageBox.Show("Не все поля заполненны!"); }
                }
                else { MessageBox.Show("Сначало закончите данное меню м кликнете на кнопку начать новое!"); }
            }
            catch (Exception ex) { }

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
                GenerateButtonText(0);
            }
            catch (Exception ex) { sqlconn.Close(); }

        }

        private void RadButton_Click2(object sender, RoutedEventArgs e)
        {




        }

        void UpdateStatusMen(int id)
        {

            SqlConnection sqlconn = new SqlConnection(Conectionstring);
            SqlCommand sqlcomm = new SqlCommand(string.Format("update menus set isopen=0 where id='{0}' ", id), sqlconn);
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
            catch (Exception ex) { }


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
                    com.nameT = sqldr.GetString(1);
                    com.type = sqldr.GetString(2);
                    com.mera = sqldr.GetString(3);
                    com.fass = sqldr.GetDecimal(6);

                    com.count = sqldr.GetDecimal(10);
                    com.ves = sqldr.GetDecimal(10);
                    com.fassIz = sqldr.GetString(8);
                    com.flag = com.mera != com.fassIz ? 1 : 0;
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
                // lmda1 = (rtv.Items as List<MenuDel_act>);
                //foreach (var list in lmda1)
                //{
                //    if (list.lcomp.Count() == 0)
                //    {
                //  list.lcomp.Add(addtiprod(list.del_id));
                //    }
                //}

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
            if (MessageBox.Show("Удалить запись?", "Предупреждение.", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {

                var y = ((e.OriginalSource as RadButton).CommandParameter as Menus);
                SqlConnection sqlcon = new SqlConnection(Conectionstring);
                SqlCommand sqlcomm = new SqlCommand(string.Format(@"
            delete from menus where id={0}
delete      from menu_delicates where id_men={0}
delete   FROM [Components1]    where idmen={0}
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
            }
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
            UpdateMenu_comp(y.id);
         
  
            rt.SelectedIndex = 0;
        }

        private void FIOM_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (men.IsEnabled == true)
                {
                    SqlConnection sqlcon = new SqlConnection(Conectionstring);
                    SqlCommand sqlcom = new SqlCommand(string.Format(@"update menus set name='{0}',count_people={1},deteils='{2}',dateban='{4}' where id={5}", FIOM.Text, Convert.ToInt32(CountM.Text), detM.Text, 1, Convert.ToDateTime(dtb.SelectedValue).ToString(), FIOM.Tag), sqlcon);
                    sqlcon.Open();
                    sqlcom.ExecuteNonQuery();
                    sqlcon.Close();
                    tbForFIO.Text = "Банкет:" + FIOM.Text + " - " + CountM.Text + " Человек, дата - " + (dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
                    updateAllMen();
                    GenerateButtonText(1);
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
                        SqlCommand sqlcom = new SqlCommand(string.Format(@"update menus set name='{0}',count_people={1},deteils='{2}',dateban='{4}' where id={5}", FIOM.Text, Convert.ToInt32(CountM.Text), detM.Text, 1, Convert.ToDateTime(dtb.SelectedValue).ToString(), (int)FIOM.Tag), sqlcon);
                        sqlcon.Open();
                        sqlcom.ExecuteNonQuery();
                        sqlcon.Close();
                        tbForFIO.Text = "Банкет:" + FIOM.Text + " - " + CountM.Text + " Человек, дата - " + (dtb.SelectedDate.Value.AddHours(dtb.SelectedTime.Value.Hours).AddMinutes(dtb.SelectedTime.Value.Minutes)).ToString();
                        updateAllMen();
                        GenerateButtonText(1);
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
            SqlConnection sqlcon = new SqlConnection(Conectionstring);
           
            SqlCommand sqlcomm = new SqlCommand(string.Format(@"
alter view    View_Delicstes   as
SELECT     del_id, Del_Type, Del_Name, Del_opis, isnull(Del_Cost, 0) Del_Cost, Del_Ves, Del_count, Type_Del_ID, Type_del_opis, Datew, - 1 exp1, 0 minB, 
                      0 maxB, 0 minV, 0 medV, 0 maxV, - 1 avtoadd, 0 CountChel
FROM         Delicates t INNER JOIN
                      Type_Del t1 ON t .Del_Type = t1.Type_Del_ID
UNION ALL
SELECT     prod_id, - 1, name, avtoadd, 0, chel, 1, TypeProdId, Type_Opis, getdate(), expr1, isnull(minB, 0), isnull(maxB, 0), isnull(minN, 0), isnull(medN, 0), 
                      isnull(max, 0), avtoadd, chel CountChel
FROM         View_Producte
WHERE     Priz_menu = 1    
            "), sqlcon);
            sqlcon.Open();
            sqlcomm.ExecuteNonQuery();
            sqlcon.Close();
            MessageBox.Show("Программа предназначенна для частного использования. Не для массового распространения.");

        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            Process.Start("I.docx");

        }
        List<ProductView> lpw = new List<ProductView>();

        void producteUpdate()
        {

            SqlConnection sql = new SqlConnection(Conectionstring);
            //SqlCommand sqlcom = new SqlCommand(string.Format(@"select * from view_producte "), sql);
            SqlDataAdapter sqld = new SqlDataAdapter("select * from View_for_men", sql);

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


        }
        private void newitem(object sender, Telerik.Windows.Controls.GridView.GridViewAddingNewEventArgs e)
        {


            Window3 w3 = new Window3();
            w3.ShowDialog();

            if (IDProd.ID != 0)
            {


                var t = (e.OwnerGridViewItemsControl.ParentRow.Item as MenuDel_act);

                //  case when  t1.Izmer!=t1.ves and t1.Izmer is not null then 1 else 0 end
                e.NewObject = new Components
                {
                    count = t.countpor,
                    Delid = t.del_id,
                    fass = (from t1 in lpw where t1.ID == IDProd.ID select t1.fass).First(),
                    mera = (from t1 in lpw where t1.ID == IDProd.ID select t1.ves).First(),
                    ves = Convert.ToDecimal(IDProd.ves),
                    nameT = (from t1 in lpw where t1.ID == IDProd.ID select t1.name).First(),
                    name = (from t1 in lpw where t1.ID == IDProd.ID select t1.name).First(),
                    fassIz = (from t1 in lpw where t1.ID == IDProd.ID select t1.izname).First(),
                    flag = (from t1 in lpw where t1.ID == IDProd.ID select t1.izname).First() != (from t1 in lpw where t1.ID == IDProd.ID select t1.ves).First() && (from t1 in lpw where t1.ID == IDProd.ID select t1.fass).First() != 1000 ? 1 : 0,
                    type = (from t1 in lpw where t1.ID == IDProd.ID select t1.type).First(),
                    Prodid = (from t1 in lpw where t1.ID == IDProd.ID select t1.ID).First(),


                };
            }
            else
            {
                e.Cancel = true;
            }

        }

        private void ik(object sender, ContextMenuEventArgs e)
        {

        }

        private void childGrid_CellEditEnded(object sender, GridViewCellEditEndedEventArgs e)
        {

        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Хотити ли вы сохранить изменения внесенные в меню? После обновление все не сохраненные изменения будут стерты.", "Внимание", MessageBoxButton.OKCancel).ToString() == "OK")
            {
                rabt = 1;
                Window_Loaded(null, null);
            }
        }


        public int menid, rabt;
        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {

            rbi.IsBusy = true;
            l = false;
            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += new DoWorkEventHandler(worker_DoWork1);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            menid = (int)FIOM.Tag;
            bw.RunWorkerAsync(lmda1);

        }
        void bw_RunWorkerCompleted1(object sender, RunWorkerCompletedEventArgs e)
        {
            end = 1;
            rbi.IsBusy = false;
            if (l == false)
            { MessageBox.Show("Сохранить изменения не удолось проверьте правильность заполнения таблицы!"); }
           
            Close();

        }
        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            rbi.IsBusy = false;
            if (l == false)
            { MessageBox.Show("Сохранить изменения не удолось проверьте правильность заполнения таблицы!"); }
            //throw new NotImplementedException();
        }
        bool l;
        void worker_DoWork1(object sender, DoWorkEventArgs e)
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection(Conectionstring);
                SqlCommand sqlcom;//= new SqlCommand("update [Menu_Delicates] set [Delcount]= ");
                ObservableCollection<MenuDel_act> temp = new ObservableCollection<MenuDel_act>();

                temp = (e.Argument as ObservableCollection<MenuDel_act>);

                foreach (var list in temp)
                {
                    sqlcom = new SqlCommand(string.Format(@"
                    update [Menu_Delicates] set [Delcount]={0} where [id_delic]={1} and id_men={2}

delete from   [Components1] where delic_id={1} and [idmen]={2}", list.countpor, list.del_id, menid), sqlconn);
                    sqlconn.Open();
                    sqlcom.ExecuteNonQuery();
                    sqlconn.Close();
                    foreach (var list1 in list.lcomp)
                    {
                        sqlcom = new SqlCommand(string.Format(@" 
                        insert into [Components1] (delic_id,ves,[ProductID],idmen) values({0},'{1}',{2},{3})", list.del_id, list1.ves.ToString().Replace(',', '.'), list1.Prodid, menid), sqlconn);
                        sqlconn.Open();
                        sqlcom.ExecuteNonQuery();
                        sqlconn.Close();

                    }
                }

                sqlcom = new SqlCommand(string.Format(@" 
update menus set ifchan=1 where id={0}
                        ", menid), sqlconn);
                sqlconn.Open();
                sqlcom.ExecuteNonQuery();
                sqlconn.Close();
                l = true;
            }
            catch { l = false; }
        }
        int end;
        private void Window_Closing(object sender, CancelEventArgs e)
        {
           
            if (rtv.Items.Count > 0)
            {
                if (end == 0)
                {
                    if (MessageBox.Show("Хотити ли вы сохранить изменения внесенные в меню?", "Внимание", MessageBoxButton.OKCancel).ToString() == "OK")
                    {
                        e.Cancel = true;
                        rbi.IsBusy = true;
                        l = false;
                        BackgroundWorker bw = new BackgroundWorker();

                        bw.DoWork += new DoWorkEventHandler(worker_DoWork1);
                        bw.RunWorkerCompleted += bw_RunWorkerCompleted1;
                        menid = (int)FIOM.Tag;
                        bw.RunWorkerAsync(lmda1);

                    }
                }
            }

        }

        private void rtv_LostFocus(object sender, RoutedEventArgs e)
        {


        }
        MenusPrint mp = new MenusPrint();
        private void s_Click1(object sender, RoutedEventArgs e)
        {

            var mususCollection = mp.Get_Menu().OrderBy(x => x.type);

            application = mp.GetApplication(mususCollection.ToList(), 0,"");


            try
            {
                //application.PrintPreview = true;
                application.Visible = true;
              //  application.PrintOut();
               // application.Documents.Close(SaveOptions.None, Type.Missing, Type.Missing);
              //  application.Visible = false;
            }
            catch { try { application.Documents.Close(SaveOptions.None, Type.Missing, Type.Missing); } catch { } }

        }
        Word.Application application;
        private void PrintMenuReal_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var Types = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "/NotInclude.txt").Split('|');

                var mususCollection = lmda;

                var mususCollectionAll = mp.Get_Menu().OrderBy(x => x.type);


                var mun = lmda.Select(x => new DelicatesColl { lcomp = x.lcomp, name = mususCollectionAll.Where(y => y.id == x.del_id).Select(y => y.name).FirstOrDefault(), type = mususCollectionAll.Where(y => y.id == x.del_id).Select(y => y.type).FirstOrDefault(), IDType = mususCollectionAll.Where(y => y.id == x.del_id).Select(y => y.IDType).FirstOrDefault(), ves = mususCollectionAll.Where(y => y.id == x.del_id).Select(y => y.ves).FirstOrDefault(), count = x.countpor }).Where(x => x.lcomp.Count() > 0);

                application = mp.GetApplication(mun.Where(x => !Types.Contains(x.type)).ToList(), 1, FIOM.Text + ", " + CountM.Text + " человек, " + dtb.DisplayDate.ToShortDateString());

                try
                {
                    //  application.PrintPreview = true;
                     application.Visible = true;
                   // application.PrintOut();
                   // application.Documents.Close(SaveOptions.None, Type.Missing, Type.Missing);
                   // application.Visible = false;
                  //  application.Quit(SaveOptions.None, Type.Missing, Type.Missing);

                }
                catch { try { application.Documents.Close(SaveOptions.None, Type.Missing, Type.Missing); } catch { } }


            }
            catch (Exception ex) { MessageBox.Show("Произошла ошибка " + ex.ToString()); }
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
