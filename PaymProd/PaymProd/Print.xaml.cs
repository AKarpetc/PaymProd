using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xaml;
using Microsoft.Win32;
using Word = Microsoft.Office.Interop.Word;
namespace PaymProd
{
    /// <summary>
    /// Логика взаимодействия для Print.xaml
    /// </summary>
    public partial class Print : Window
    {
        public Print(ObservableCollection<MenuDel_act> lmda,List<string> ls)
        {
            InitializeComponent();
            //CultureInfo inf = new CultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
           // System.Threading.Thread.CurrentThread.CurrentCulture = inf;
          //  inf.NumberFormat.NumberDecimalSeparator = ",";
            // CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
             Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            //   CultureInfo.CurrentCulture.NumberFormat.PercentDecimalSeparator = ".";
            lmda1 = lmda;
            ls1 = ls;
        }
        List<string> ls1 = new List<string>();
        ObservableCollection<MenuDel_act> lmda1 = new ObservableCollection<MenuDel_act>();
        List<Components> lc11 = new List<Components>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ras.Inlines.Add("Банкет: " +ls1[0]+" \t\tНачало " +ls1[2]+" \t\tКоличество гостей "+ls1[1]+" человек");
                ras.FontFamily = new FontFamily("Times New Roman");
                List<Components> lc = new List<Components>();
                foreach (var list in lmda1)
                {
                    foreach (var list1 in list.lcomp)
                    {
                        list1.count = list.countpor;
                    }


                    lc.AddRange(list.lcomp);
                }

              
                List<DelicatesCollForSvod> ld = new List<DelicatesCollForSvod>();
                DelicatesCollForSvod dcf;
                foreach (var list in lmda1)
                {
                    
                    foreach (var list1 in list.lcomp)
                    {
                        dcf = new DelicatesCollForSvod();
                        dcf.del = list.del;
                         
                        dcf.count = list.countpor;
                        dcf.name = list1.name;
                        dcf.type = list1.type;
                        dcf.ves = list1.ves;
                        dcf.countpor = list.countpor;
                        dcf.fass = list1.fass;
                        dcf.fassIz = list1.fassIz;
                        try
                        {
                            dcf.itog = dcf.countpor * Convert.ToDecimal(dcf.ves);
                        }
                        catch
                        {
                            dcf.itog = dcf.countpor * Convert.ToDecimal(dcf.ves.Replace(',','.'));
                        }
                        try
                        {
                            dcf.itogfass = Math.Round(dcf.fass == 0 ? dcf.itog : dcf.itog / dcf.fass, 2);
                        }
                        catch { }
                        
                        ld.Add(dcf);
                    }
                    

                }

                rgv.ItemsSource = ld;
                Components c;
                var l = (from t in lc group t by new { prodid = t.Prodid, name = t.name, type = t.type, mera = t.mera, fassIZ = t.fassIz, flag = t.flag } into temp select new { sum = temp.Sum(x => (Convert.ToDecimal(x.ves) * x.count) / x.fass), temp.Key.type, temp.Key.name, temp.Key.mera, temp.Key.fassIZ, temp.Key.flag }).ToList();
               
                foreach (var list in l)
                {
                    //c.count = list.sum;
                    //c.name = list.name;
                    //c.type = list.type;
                    //c.flag = list.flag;
                    //c.fassIz = list.fassIZ;
                    //c.mera = list.mera;
                    //lc11.Add(c);
                }
                bool b = true;
                foreach (var list in (from t in l select t.type).Distinct())
                {
                    TableRowGroup tbrg = new TableRowGroup();
                    TableRow te = new TableRow();
                    TableCell tc = new TableCell(new Paragraph(new Run(list)));
                    tc.FontWeight = FontWeights.Bold;
                    tc.ColumnSpan = 3;
                    tc.TextAlignment = TextAlignment.Center;
                    tc.BorderBrush = new SolidColorBrush(Colors.Black);
                    tc.BorderThickness = new Thickness(1);
                    te.Cells.Add(tc);
                    tbrg.Rows.Add(te);
                    if (b == true)
                    { table.RowGroups.Add(tbrg); }
                    else { table2.RowGroups.Add(tbrg); }

                    foreach (var list1 in (from t in l where t.type == list select t))
                    {
                        c = new Components();
                        c.count = list1.sum;
                        c.name = list1.name;
                        c.type = list1.type;
                        c.flag = list1.flag;
                        c.fassIz = list1.fassIZ;
                        c.mera = list1.mera;

                        lc11.Add(c);

                        if (b == true)
                        {
                            gen(list1.name, list1.sum < 1 && list1.flag != 1 ? list1.sum * 1000 : list1.sum, list1.sum < 1 && list1.flag != 1 ? list1.mera : list1.fassIZ.Trim() == "" ? list1.mera : list1.fassIZ, list1.flag);
                        }
                        else
                        {

                            gen1(list1.name, list1.sum < 1 && list1.flag != 1 ? list1.sum * 1000 : list1.sum, list1.sum < 1 && list1.flag != 1 ? list1.mera : list1.fassIZ.Trim() == "" ? list1.mera : list1.fassIZ, list1.flag);
                        }

                    }
                    TableRowGroup tbrg1 = new TableRowGroup();
                    TableRow te1 = new TableRow();
                    TableCell tc1 = new TableCell();
                    tc1.ColumnSpan = 3;
                    te1.Cells.Add(tc1);
                    tbrg1.Rows.Add(te1);
                    if (b == true)
                    {
                        table.RowGroups.Add(tbrg1);
                    }
                    else { table2.RowGroups.Add(tbrg1); }
                    if (b == true) { b = false; } else { b = true; }
                }


                //  table.RowGroups.Add(new TableRowGroup{ } 
            }
            catch(Exception ex) { }
           
        }
        void gen(string name, decimal ves,string por,int flag )
        {
            TableRowGroup tbrg = new TableRowGroup();

            TableRow te = new TableRow();
            TableCell tc = new TableCell(new Paragraph(new Run(name)));
            tc.LineHeight = 30;
       
            tc.TextAlignment = TextAlignment.Left;
            double t= (double)(ves- Math.Truncate(ves));
            TableCell tc1 = new TableCell(new Paragraph(new Run(flag == 1 ? (t > 0 && t < 0.5 ? Convert.ToInt32(ves) + 1 : Convert.ToInt32(ves)).ToString() : ves.ToString())));
            TableCell tc2 = new TableCell(new Paragraph(new Run(por.ToString())));
            tc.TextAlignment = TextAlignment.Center;
            
            tc.BorderBrush = new SolidColorBrush(Colors.Black);
            tc.BorderThickness = new Thickness(1);
            tc1.TextAlignment = TextAlignment.Center;
            tc1.BorderBrush = new SolidColorBrush(Colors.Black);
            tc1.BorderThickness = new Thickness(1);
            tc2.TextAlignment = TextAlignment.Center;
            tc2.BorderBrush = new SolidColorBrush(Colors.Black);
            tc2.BorderThickness = new Thickness(1);
            te.Cells.Add(tc);
            te.Cells.Add(tc1);
            te.Cells.Add(tc2);
            tbrg.Rows.Add(te);
           
            table.RowGroups.Add(tbrg);
         

        }
        void gen1(string name, decimal ves, string por, int flag)
        {
            TableRowGroup tbrg = new TableRowGroup();

            TableRow te = new TableRow();
            TableCell tc = new TableCell(new Paragraph(new Run(name)));
            tc.LineHeight = 30;
            tc.TextAlignment = TextAlignment.Left;
            double t = (double)(ves - Math.Truncate(ves));
            TableCell tc1 = new TableCell(new Paragraph(new Run(flag == 1 ? (t > 0 && t < 0.5 ? Convert.ToInt32(ves) + 1 : Convert.ToInt32(ves)).ToString() : ves.ToString())));
            
            TableCell tc2 = new TableCell(new Paragraph(new Run(por.ToString())));
           
            
            tc.TextAlignment = TextAlignment.Center;

            tc.BorderBrush = new SolidColorBrush(Colors.Black);
            tc.BorderThickness = new Thickness(1);
            tc1.TextAlignment = TextAlignment.Center;
            tc1.BorderBrush = new SolidColorBrush(Colors.Black);
            tc1.BorderThickness = new Thickness(1);
            tc2.TextAlignment = TextAlignment.Center;
            tc2.BorderBrush = new SolidColorBrush(Colors.Black);
            tc2.BorderThickness = new Thickness(1);
            te.Cells.Add(tc);
            te.Cells.Add(tc1);
            te.Cells.Add(tc2);
            tbrg.Rows.Add(te);

            table2.RowGroups.Add(tbrg);


        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Table2();
            }

            catch(Exception ex) { }
        }
        private void Table2()
        {


            Microsoft.Office.Interop.Word.Application application = new Microsoft.Office.Interop.Word.Application(AppDomain.CurrentDomain.BaseDirectory + "\\1.doc");

    Microsoft.Office.Interop.Word.Application application1 = new Microsoft.Office.Interop.Word.Application();
            
            Object missing = Type.Missing;

//application1.Documents.Open(AppDomain.CurrentDomain.BaseDirectory+ "\\1.doc");

            application.Documents.Add(AppDomain.CurrentDomain.BaseDirectory + "\\1.doc");
          //- application = application1;
            Microsoft.Office.Interop.Word.Document doc = application.ActiveDocument;


            doc.Paragraphs[1].Range.Text = "Банкет: " + ls1[0] + "   \tНачало " + ls1[2] + "   \tКоличество гостей: " + ls1[1] + "   человек";
            doc.Paragraphs[1].Range.Font.Name = "Times New Roman";
            doc.Paragraphs[1].Range.Font.Size = 12;
            doc.Paragraphs[1].Range.Font.Bold = 2;
            doc.Paragraphs[1].Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
           
           // doc.Paragraphs.Add();
            //var r=doc.Paragraphs[2]   ;
            Microsoft.Office.Interop.Word.Range range = doc.Paragraphs[doc.Paragraphs.Count].Range;
doc.GridDistanceHorizontal = 1;
doc.GridDistanceVertical = 1;
doc.GridOriginVertical = 1;
doc.GridOriginHorizontal = 1;
doc.PageSetup.TopMargin = 20;
doc.PageSetup.LeftMargin = 30;
doc.PageSetup.RightMargin = 30;
// = 10;

doc.Tables.Add(range, (from t in lc11 select t.type).Distinct().Count()+lc11.Count(), 3, ref missing, ref missing);
var m = doc.Tables[1];
int i = 1, j = 1,zap=0 ;

bool k = true;
foreach (var list1 in (from t in lc11 select t.type).Distinct())
{
    if (k == true)
    {
        zap = i;
        Word.Row row = doc.Tables[1].Rows[i];

        doc.Range(row.Cells[1].Range.Start, row.Cells[3].Range.End).Cells.Merge();
        row.Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
        row.Cells[1].Width = 250;
       
        row.Cells[1].Range.Font.Size = 14;
        row.Cells[1].Range.Font.Bold = 2;
        row.Cells[1].Range.Text = list1;
        row.Cells[1].Range.Font.Name = "Times New Roman";

        i++;
        foreach (var list in from t in lc11 where t.type == list1 select t)
        {
            doc.Tables[1].Cell(i, 1).Height = 20;

            doc.Tables[1].Cell(i, 1).Width = 150;
            doc.Tables[1].Cell(i, 2).Width = 50;
            doc.Tables[1].Cell(i, 3).Width = 50;
            doc.Tables[1].Cell(i, 1).Range.Font.Name = "Times New Roman";
            doc.Tables[1].Cell(i, 1).Range.Text = list.name;

            // flag==1? (Convert.ToInt32(ves)+1).ToString():ves.ToString()
            decimal ves = list.count < 1 && list.flag != 1 ? list.count * 1000 : list.count;


            doc.Tables[1].Cell(i, 2).Range.Text = (list.flag == 1 ? (Convert.ToInt32(ves) + 1).ToString() : ves.ToString()).ToString();

            doc.Tables[1].Cell(i, 3).Range.Font.Name = "Times New Roman";
            doc.Tables[1].Cell(i, 3).Range.Text = list.count < 1 && list.flag != 1 ? list.mera : list.fassIz.Trim() == "" ? list.mera : list.fassIz;

            //doc.Tables[1].Cell(i, 3).Range.Font.Name = "Times New Roman";
            // doc.Tables[1].Cell(i, 5).Range.Text = list.fass.ToString();
            // doc.Tables[1].Cell(i, 6).Range.Text = list.count.ToString();
            // doc.Tables[1].Cell(i, 7).Range.Text = list.mera;
            i++;
            

        }
      //  k = false;
    }
    else
    {
        Word.Row row = doc.Tables[1].Rows[j];
        if (j == zap)
        {
            doc.Range(row.Cells[3].Range.Start, row.Cells[6].Range.End).Cells.Merge();
        }
        else
        {
            doc.Range(row.Cells[4].Range.Start, row.Cells[7].Range.End).Cells.Merge();
        }
        row.Cells[2].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
        row.Cells[2].Width = 250;
        row.Cells[2].Range.Font.Size = 14;
        row.Cells[2].Range.Font.Bold = 2;
        row.Cells[2].Range.Text = list1;
        row.Cells[2].Range.Font.Name = "Times New Roman";

        j++;
        foreach (var list in from t in lc11 where t.type == list1 select t)
        {
            try
            {

                doc.Tables[1].Cell(j, 4).Width = 150;
                doc.Tables[1].Cell(j, 5).Width = 50;
                doc.Tables[1].Cell(j, 6).Width = 50;
                doc.Tables[1].Cell(j, 4).Range.Font.Name = "Times New Roman";
                doc.Tables[1].Cell(j, 4).Range.Text = list.name;

                // flag==1? (Convert.ToInt32(ves)+1).ToString():ves.ToString()
                decimal ves = list.count < 1 && list.flag != 1 ? list.count * 1000 : list.count;


                doc.Tables[1].Cell(j, 5).Range.Text = (list.flag == 1 ? (Convert.ToInt32(ves) + 1).ToString() : ves.ToString()).ToString();

                doc.Tables[1].Cell(j, 6).Range.Font.Name = "Times New Roman";
                doc.Tables[1].Cell(j, 6).Range.Text = list.count < 1 && list.flag != 1 ? list.mera : list.fassIz.Trim() == "" ? list.mera : list.fassIz;

                //doc.Tables[1].Cell(i, 3).Range.Font.Name = "Times New Roman";
                // doc.Tables[1].Cell(i, 5).Range.Text = list.fass.ToString();
                // doc.Tables[1].Cell(i, 6).Range.Text = list.count.ToString();
                // doc.Tables[1].Cell(i, 7).Range.Text = list.mera;
                j++;
        
            }
            catch
            {
                j++;
            
            }
        }

        k = true;
    }
}
//doc.Tables[1].Select();
doc.PageSetup.HeaderDistance = 15;
Word.Border[] borders = new Word.Border[6];
Word.Table tbl = doc.Tables[doc.Tables.Count];
borders[0] = tbl.Borders[Word.WdBorderType.wdBorderLeft];
borders[1] = tbl.Borders[Word.WdBorderType.wdBorderRight];
borders[2] = tbl.Borders[Word.WdBorderType.wdBorderTop];
borders[3] = tbl.Borders[Word.WdBorderType.wdBorderBottom];
borders[4] = tbl.Borders[Word.WdBorderType.wdBorderHorizontal];
borders[5] = tbl.Borders[Word.WdBorderType.wdBorderVertical];
foreach (Word.Border border in borders)
{
    try
    {
        border.LineWidth = Word.WdLineWidth.wdLineWidth075pt;
    }
    catch { }
    border.LineStyle = Word.WdLineStyle.wdLineStyleSingle;
    border.Color = Word.WdColor.wdColorBlack;
}
application.Visible = true;
application.DocumentBeforeClose += application_DocumentBeforeClose;
//application.Documents.Close();          
        }
        private void Table3()
        {


            Microsoft.Office.Interop.Word.Application application = new Microsoft.Office.Interop.Word.Application(AppDomain.CurrentDomain.BaseDirectory + "\\1.doc");

            Microsoft.Office.Interop.Word.Application application1 = new Microsoft.Office.Interop.Word.Application();

            Object missing = Type.Missing;

            //application1.Documents.Open(AppDomain.CurrentDomain.BaseDirectory+ "\\1.doc");

            application.Documents.Add(AppDomain.CurrentDomain.BaseDirectory + "\\1.doc");
            //- application = application1;
            Microsoft.Office.Interop.Word.Document doc = application.ActiveDocument;
            //ras.Inlines.Add("Банкет: " + ls1[0] + " \t\tНачало " + ls1[2] + " \t\tКоличество гостей " + ls1[1] + " человек");
           // ras.FontFamily = new FontFamily("Times New Roman");
          
            doc.Paragraphs[1].Range.Text = "Банкет: " + ls1[0] + " \t\tНачало " + ls1[2] + " \t\tКоличество гостей: " + ls1[1] + " человек";
            doc.Paragraphs[1].Range.Font.Name = "Times New Roman";
            doc.Paragraphs[1].Range.Font.Size = 14;
            doc.Paragraphs[1].Range.Font.Bold = 2;
            doc.Paragraphs[1].Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

            // doc.Paragraphs.Add();
            //var r=doc.Paragraphs[2]   ;
            Microsoft.Office.Interop.Word.Range range = doc.Paragraphs[doc.Paragraphs.Count].Range;
            doc.GridDistanceHorizontal = 1;
            doc.GridDistanceVertical = 1;
            doc.GridOriginVertical = 1;
            doc.GridOriginHorizontal = 1;
            doc.PageSetup.TopMargin = 20;
            doc.PageSetup.LeftMargin = 30;
            doc.PageSetup.RightMargin = 30;
            // = 10;

            doc.Tables.Add(range, (from t in lc11 select t.type).Distinct().Count() + lc11.Count(), 3, ref missing, ref missing);
            var m = doc.Tables[1];
            int i = 1, j = 1, zap = 0;

            bool k = true;
            foreach (var list1 in (from t in lc11 select t.type).Distinct())
            {
                if (k == true)
                {
                    zap = i;
                    Word.Row row = doc.Tables[1].Rows[i];

                    doc.Range(row.Cells[1].Range.Start, row.Cells[3].Range.End).Cells.Merge();
                    row.Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    row.Cells[1].Width = 250;
                    row.Cells[1].Range.Font.Size = 14;
                    row.Cells[1].Range.Font.Bold = 2;
                    row.Cells[1].Range.Text = list1;
                    row.Cells[1].Range.Font.Name = "Times New Roman";

                    i++;
                    foreach (var list in from t in lc11 where t.type == list1 select t)
                    {

                        doc.Tables[1].Cell(i, 1).Width = 150;
                        doc.Tables[1].Cell(i, 2).Width = 50;
                        doc.Tables[1].Cell(i, 3).Width = 50;
                        doc.Tables[1].Cell(i, 1).Range.Font.Name = "Times New Roman";
                        doc.Tables[1].Cell(i, 1).Range.Text = list.name;

                        // flag==1? (Convert.ToInt32(ves)+1).ToString():ves.ToString()
                        decimal ves = list.count < 1 && list.flag != 1 ? list.count * 1000 : list.count;


                        doc.Tables[1].Cell(i, 2).Range.Text = (list.flag == 1 ? (Convert.ToInt32(ves) + 1).ToString() : ves.ToString()).ToString();

                        doc.Tables[1].Cell(i, 3).Range.Font.Name = "Times New Roman";
                        doc.Tables[1].Cell(i, 3).Range.Text = list.count < 1 && list.flag != 1 ? list.mera : list.fassIz.Trim() == "" ? list.mera : list.fassIz;

                        //doc.Tables[1].Cell(i, 3).Range.Font.Name = "Times New Roman";
                        // doc.Tables[1].Cell(i, 5).Range.Text = list.fass.ToString();
                        // doc.Tables[1].Cell(i, 6).Range.Text = list.count.ToString();
                        // doc.Tables[1].Cell(i, 7).Range.Text = list.mera;
                        i++;


                    }
                    //  k = false;
                }
                else
                {
                    Word.Row row = doc.Tables[1].Rows[j];
                    if (j == zap)
                    {
                        doc.Range(row.Cells[3].Range.Start, row.Cells[6].Range.End).Cells.Merge();
                    }
                    else
                    {
                        doc.Range(row.Cells[4].Range.Start, row.Cells[7].Range.End).Cells.Merge();
                    }
                    row.Cells[2].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    row.Cells[2].Width = 250;
                    row.Cells[2].Range.Font.Size = 14;
                    row.Cells[2].Range.Font.Bold = 2;
                    row.Cells[2].Range.Text = list1;
                    row.Cells[2].Range.Font.Name = "Times New Roman";

                    j++;
                    foreach (var list in from t in lc11 where t.type == list1 select t)
                    {
                        try
                        {

                            doc.Tables[1].Cell(j, 4).Width = 150;
                            doc.Tables[1].Cell(j, 5).Width = 50;
                            doc.Tables[1].Cell(j, 6).Width = 50;
                            doc.Tables[1].Cell(j, 4).Range.Font.Name = "Times New Roman";
                            doc.Tables[1].Cell(j, 4).Range.Text = list.name;

                            // flag==1? (Convert.ToInt32(ves)+1).ToString():ves.ToString()
                            decimal ves = list.count < 1 && list.flag != 1 ? list.count * 1000 : list.count;


                            doc.Tables[1].Cell(j, 5).Range.Text = (list.flag == 1 ? (Convert.ToInt32(ves) + 1).ToString() : ves.ToString()).ToString();

                            doc.Tables[1].Cell(j, 6).Range.Font.Name = "Times New Roman";
                            doc.Tables[1].Cell(j, 6).Range.Text = list.count < 1 && list.flag != 1 ? list.mera : list.fassIz.Trim() == "" ? list.mera : list.fassIz;

                            //doc.Tables[1].Cell(i, 3).Range.Font.Name = "Times New Roman";
                            // doc.Tables[1].Cell(i, 5).Range.Text = list.fass.ToString();
                            // doc.Tables[1].Cell(i, 6).Range.Text = list.count.ToString();
                            // doc.Tables[1].Cell(i, 7).Range.Text = list.mera;
                            j++;

                        }
                        catch
                        {
                            j++;

                        }
                    }

                    k = true;
                }
            }
            //doc.Tables[1].Select();
            doc.PageSetup.HeaderDistance = 15;
            Word.Border[] borders = new Word.Border[6];
            Word.Table tbl = doc.Tables[doc.Tables.Count];
            borders[0] = tbl.Borders[Word.WdBorderType.wdBorderLeft];
            borders[1] = tbl.Borders[Word.WdBorderType.wdBorderRight];
            borders[2] = tbl.Borders[Word.WdBorderType.wdBorderTop];
            borders[3] = tbl.Borders[Word.WdBorderType.wdBorderBottom];
            borders[4] = tbl.Borders[Word.WdBorderType.wdBorderHorizontal];
            borders[5] = tbl.Borders[Word.WdBorderType.wdBorderVertical];
            foreach (Word.Border border in borders)
            {
                try
                {
                    border.LineWidth = Word.WdLineWidth.wdLineWidth150pt;
                }
                catch { }
                border.LineStyle = Word.WdLineStyle.wdLineStyleSingle;
                border.Color = Word.WdColor.wdColorBlack;
            }


            application.PrintOut();
            application.PrintPreview = true;
            application.DocumentBeforeClose += application_DocumentBeforeClose;
            //application.Documents.Close();          
        }

        void application_DocumentBeforeClose(Word.Document Doc, ref bool Cancel)
        {
            Process[] ps1 = System.Diagnostics.Process.GetProcessesByName("WINWORD");
            foreach (Process pr in ps1)
            {

                pr.Kill();


            }

        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Table3();
            //PrintDialog printDialog = new PrintDialog();
            //if (printDialog.ShowDialog() == true)
            //{
            //    FlowDocument doc = (rtb.Document as FlowDocument);

            //    //doc.PageHeight = 2150;// printDialog.PrintableAreaHeight;
            //    // doc.PageWidth = 2790;// printDialog.PrintableAreaWidth;

            //    printDialog.PrintDocument(
            //        ((IDocumentPaginatorSource)rtb.Document).DocumentPaginator,
            //        "A Flow Document");
            //    Close();
            //}
           

        }
    }
}
