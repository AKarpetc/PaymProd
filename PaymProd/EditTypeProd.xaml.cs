using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
    /// <summary>
    /// Логика взаимодействия для EditTypeProd.xaml
    /// </summary>
    public partial class EditTypeProd : Window
    {
        public EditTypeProd()
        {
            InitializeComponent();
        }
        string conectionstring;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            conectionstring = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|MenuCaolc.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";

            try
            {
                SqlConnection sql = new SqlConnection(conectionstring);
                //SqlCommand sqlcom = new SqlCommand(string.Format(@"select * from view_producte "), sql);

                var t = productEdit.pv;
                NameP.Text = t.name;
                DataSet ds = new DataSet();
                if (t.prizMen == 1) { addB.IsChecked = true; }

                if (t.MainCount) { MainCount.IsChecked = true; }

                SqlDataAdapter sqld = new SqlDataAdapter("select * from Produkt_Type", sql);
                ds = new DataSet();
                count.Text = t.count.ToString();
                sqld.Fill(ds, "Produkt_Type");
                TextBlock tb; int index=0;
                int i=0,index1=0,i1=0;
                foreach (DataRow list in ds.Tables[0].Rows)
                {
                    tb = new TextBlock();
                    tb.Tag = list[0];
                    tb.Text = list[1].ToString();
                   if (list[0].ToString() == t.TID.ToString())
                    { index = i; }
                           
                    typeP.Items.Add(tb);

                        i++;
                }
                countGen.Text = t.CountPeople.ToString();
                addB1.IsChecked = t.AutoAdd;
                FasP.Text = t.fass.ToString();
                typeP.SelectedIndex = index;
                sqld = new SqlDataAdapter("select * from mera", sql);
                ds = new DataSet();

                sqld.Fill(ds, "mera");
                i = 0; index = 0; i1 = 0; index1 = 0;
                foreach (DataRow list in ds.Tables[0].Rows)
                {
                    tb = new TextBlock();
                    tb.Tag = list[0];
                    tb.Text = list[1].ToString();
                    vesP.Items.Add(tb);
                    if (list[0].ToString() == t.VID.ToString())
                    { index = i; }
                    if (list[0].ToString() == t.iz.ToString())
                    { index1 = i1; }
                    tb = new TextBlock();
                    tb.Tag = list[0];
                    tb.Text = list[1].ToString();
                    vesPS.Items.Add(tb);
                    i++;
                    i1++;
                }
                vesP.SelectedIndex = index;
                vesPS.SelectedIndex = index1;
            }
            catch (Exception ex) { }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int addb = 0;
            if (addB.IsChecked == true)
            {
                addb = 1;
            }
            ProductView pv = new ProductView();
            pv.ID = productEdit.pv.ID;
            pv.name = NameP.Text;
            pv.TID = (int)(typeP.SelectedItem as TextBlock).Tag;
            pv.VID = (int)(vesP.SelectedItem as TextBlock).Tag;
            pv.fass = Convert.ToDecimal(FasP.Text == "" ? "0" : FasP.Text);
            pv.iz = (int)(vesPS.SelectedItem as TextBlock).Tag;
            pv.count = Convert.ToDecimal( count.Text);
            pv.prizMen = addb;
            pv.AutoAdd = addB1.IsChecked.Value;
            pv.CountPeople = Convert.ToInt32(countGen.Text == null ? "0" : countGen.Text);
            pv.MainCount = MainCount.IsChecked.Value;
            
            productEdit.pv = pv;
            productEdit.flag = true;
           
            Close();


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            productEdit.flag = false;
            Close();

        }

        private void FasP_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = ("0123456789" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToString()).IndexOf(e.Text) < 0;
   

        }

        private void vesP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vesPS.SelectedIndex = vesP.SelectedIndex;

        }
    }
}
