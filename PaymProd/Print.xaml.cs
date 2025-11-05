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
using Telerik.Windows.Data;
using Word = Microsoft.Office.Interop.Word;
using System.Xml.Linq;
namespace PaymProd
{
    /// <summary>
    /// Логика взаимодействия для Print.xaml
    /// </summary>
    public partial class Print : Window
    {
        public Print(ObservableCollection<MenuDel_act> lmda, List<string> ls)
        {
            InitializeComponent();
            //CultureInfo inf = new CultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
            // System.Threading.Thread.CurrentThread.CurrentCulture = inf;
            //  inf.NumberFormat.NumberDecimalSeparator = ",";
            // CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
            // Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            //   CultureInfo.CurrentCulture.NumberFormat.PercentDecimalSeparator = ".";
            lmda1 = lmda;
            ls1 = ls;
        }
        List<string> ls1 = new List<string>();
        ObservableCollection<MenuDel_act> lmda1 = new ObservableCollection<MenuDel_act>();
        List<Components> lc11 = new List<Components>();
        List<DelicatesCollForSvod> ld = new List<DelicatesCollForSvod>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ras.Inlines.Add("Банкет: " + ls1[0] + " \t\tНачало " + ls1[2] + " \t\tКоличество гостей " + ls1[1] + " человек");
                ras.FontFamily = new FontFamily("Times New Roman");
                List<Components> lc = new List<Components>();
                foreach (var list in from t in lmda1 where t.lcomp.Count > 0 select t)
                {
                    foreach (var list1 in list.lcomp)
                    {
                        try
                        {
                            if (list1 != null)
                            {
                                list1.count = list.countpor;
                            }
                        }
                        catch { }
                    }


                    if (list.lcomp[0] != null)
                    {
                        lc.AddRange(list.lcomp);
                    }

                }



                DelicatesCollForSvod dcf;
                foreach (var list in from t in lmda1 where t.lcomp.Count > 0 select t)
                {

                    foreach (var list1 in list.lcomp)
                    {
                        try
                        {
                            dcf = new DelicatesCollForSvod();
                            dcf.del = list.del;
                            dcf.del_id = list.del_id;
                            dcf.count = list.countpor;
                            dcf.name = list1.name;
                            dcf.type = list1.type;
                            dcf.ves = list1.ves;
                            dcf.countpor = list.countpor;
                            dcf.fass = list1.fass;
                            dcf.fassIz = list1.fassIz;
                            dcf.nameT = list1.nameT;
                            dcf.itog = dcf.countpor * Convert.ToDecimal(dcf.ves);
                            dcf.mera = list1.mera;

                            try
                            {
                                dcf.itogfass = Math.Round(dcf.fass == 0 ? dcf.itog : dcf.itog / dcf.fass, 2);
                            }
                            catch { }

                            ld.Add(dcf);
                        }
                        catch { }
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
            catch (Exception ex) { }

        }
        void gen(string name, decimal ves, string por, int flag)
        {
            ves = Math.Round(ves, 2);
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

            table.RowGroups.Add(tbrg);


        }
        void gen1(string name, decimal ves, string por, int flag)
        {
            ves = Math.Round(ves, 2);
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

            catch (Exception ex) { }
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
                        doc.Tables[1].Cell(i, 1).Height = 20;

                        doc.Tables[1].Cell(i, 1).Width = 150;
                        doc.Tables[1].Cell(i, 2).Width = 50;
                        doc.Tables[1].Cell(i, 3).Width = 50;
                        doc.Tables[1].Cell(i, 1).Range.Font.Name = "Times New Roman";
                        doc.Tables[1].Cell(i, 1).Range.Text = list.name;

                        // flag==1? (Convert.ToInt32(ves)+1).ToString():ves.ToString()

                        decimal ves = list.count < 1 && list.flag != 1 ? list.count * 1000 : list.count;
                        double t = (double)(ves - Math.Truncate(ves));
                        //  flag == 1 ? (t > 0 && t < 0.5 ? Convert.ToInt32(ves) + 1 : Convert.ToInt32(ves)).ToString() : ves.ToString()
                        doc.Tables[1].Cell(i, 2).Range.Text = (list.flag == 1 ? (t > 0 && t < 0.5 ? Convert.ToInt32(ves) + 1 : Convert.ToInt32(ves)).ToString() : ves.ToString()).ToString();

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

                            //   flag == 1 ? (t > 0 && t < 0.5 ? Convert.ToInt32(ves) + 1 : Convert.ToInt32(ves)).ToString() : ves.ToString()
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

                        double t = (double)(ves - Math.Truncate(ves));
                        //  flag == 1 ? (t > 0 && t < 0.5 ? Convert.ToInt32(ves) + 1 : Convert.ToInt32(ves)).ToString() : ves.ToString()
                        doc.Tables[1].Cell(i, 2).Range.Text = (list.flag == 1 ? (t > 0 && t < 0.5 ? Convert.ToInt32(ves) + 1 : Convert.ToInt32(ves)).ToString() : ves.ToString()).ToString();

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

            application.PrintPreview = false;
            application.PrintOut();
            application.Documents.Close(SaveOptions.None, Type.Missing, Type.Missing);

            application.DocumentBeforeClose += application_DocumentBeforeClose;
            Process[] ps1 = System.Diagnostics.Process.GetProcessesByName("WINWORD");
            foreach (Process pr in ps1)
            {

                pr.Kill();


            }
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
        private void ElementExportingLoK(object sender, Telerik.Windows.Controls.GridViewElementExportingEventArgs e)
        {
            /*
            decimal Zet;
            if (e.Value != null)
            {
                if (decimal.TryParse(e.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out Zet))
                {
                    e.Value = Zet;
                }
            }*/
            if (e.Element == Telerik.Windows.Controls.ExportElement.HeaderRow || e.Element == Telerik.Windows.Controls.ExportElement.FooterRow
                || e.Element == Telerik.Windows.Controls.ExportElement.GroupFooterRow)
            {

                e.FontSize = 20;
                e.FontWeight = FontWeights.Bold;

            }
            else if (e.Element == Telerik.Windows.Controls.ExportElement.Row)
            {

            }
            else if (e.Element == Telerik.Windows.Controls.ExportElement.Cell &&
                e.Value != null && e.Value.Equals("Chocolade"))
            {
                //     e.Value=e.Value.ToString().Replace(" ","");
                e.FontFamily = new FontFamily("Verdana");
                e.Background = Colors.LightGray;
                e.Foreground = Colors.Blue;
            }
            else if (e.Element == Telerik.Windows.Controls.ExportElement.GroupHeaderRow)
            {
                e.FontFamily = new FontFamily("Verdana");
                e.Background = Colors.LightGray;
                e.Height = 20;
            }
            else if (e.Element == Telerik.Windows.Controls.ExportElement.GroupHeaderCell &&
                e.Value != null && e.Value.Equals("Chocolade"))
            {
                e.Value = "MyNewValue";
            }
            else if (e.Element == Telerik.Windows.Controls.ExportElement.GroupFooterCell)
            {
                Telerik.Windows.Controls.GridViewDataColumn column = e.Context as Telerik.Windows.Controls.GridViewDataColumn;
                QueryableCollectionViewGroup qcvGroup = e.Value as QueryableCollectionViewGroup;

                if (column != null && qcvGroup != null && column.AggregateFunctions.Count() > 0)
                {
                    e.Value = GetAggregates(qcvGroup, column);
                }
            }
        }
        private string GetAggregates(QueryableCollectionViewGroup group, Telerik.Windows.Controls.GridViewDataColumn column)
        {
            List<string> aggregates = new List<string>();

            foreach (AggregateFunction f in column.AggregateFunctions)
            {
                foreach (AggregateResult r in group.AggregateResults)
                {
                    if (f.FunctionName == r.FunctionName && r.FormattedValue != null)
                    {
                        aggregates.Add(r.FormattedValue.ToString());
                    }
                }
            }

            return String.Join(",", aggregates.ToArray());
        }
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {

                if (rgv != null)
                {

                    rgv.ElementExporting -= this.ElementExportingLoK;
                    rgv.ElementExporting += this.ElementExportingLoK;

                    string extension = "xls";
                    Telerik.Windows.Controls.ExportFormat format = Telerik.Windows.Controls.ExportFormat.ExcelML;



                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.DefaultExt = extension;
                    //   dialog.DefaultFileName = "Новый документ";
                    dialog.Filter = String.Format("{1} files (*.{0})|*.{0}|All files (*.*)|*.*", extension, "Excel");
                    dialog.FilterIndex = 1;

                    if (dialog.ShowDialog() == true)
                    {
                        using (Stream stream = dialog.OpenFile())
                        {
                            Telerik.Windows.Controls.GridViewExportOptions exportOptions = new Telerik.Windows.Controls.GridViewExportOptions();
                            exportOptions.Format = format;
                            exportOptions.ShowColumnFooters = true;
                            exportOptions.ShowColumnHeaders = true;
                            exportOptions.ShowGroupFooters = true;

                            rgv.Export(stream, exportOptions);
                        }
                    }
                }

            }
            catch { }


        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {


            Microsoft.Office.Interop.Word.Application application = new Microsoft.Office.Interop.Word.Application();

            Microsoft.Office.Interop.Word.Application application1 = new Microsoft.Office.Interop.Word.Application();

            Object missing = Type.Missing;

            //application1.Documents.Open(AppDomain.CurrentDomain.BaseDirectory+ "\\1.doc");

            application.Documents.Add();
            //- application = application1;
            Microsoft.Office.Interop.Word.Document doc = application.ActiveDocument;
            //ras.Inlines.Add("Банкет: " + ls1[0] + " \t\tНачало " + ls1[2] + " \t\tКоличество гостей " + ls1[1] + " человек");
            // ras.FontFamily = new FontFamily("Times New Roman");

            doc.Paragraphs[1].Range.Text = "Банкет: " + ls1[0] + " \t\tНачало " + ls1[2] + " \t\tКоличество гостей: " + ls1[1] + " человек";
            doc.Paragraphs[1].Range.Font.Name = "Times New Roman";
            doc.Paragraphs[1].Range.Font.Size = 11;
            // doc.Paragraphs[1].Range.Font.Bold = 1;

            doc.Paragraphs[1].Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

            doc.Paragraphs.Add();
            //var r=doc.Paragraphs[2]   ;
            Microsoft.Office.Interop.Word.Range range = doc.Paragraphs[doc.Paragraphs.Count].Range;
            doc.GridDistanceHorizontal = 1;
            doc.GridDistanceVertical = 1;
            doc.GridOriginVertical = 1;
            doc.GridOriginHorizontal = 1;
            doc.PageSetup.TopMargin = 20;
            doc.PageSetup.LeftMargin = 30;
            doc.PageSetup.RightMargin = 30;
            doc.PageSetup.FooterDistance = 1;
            // = 10;

            doc.Tables.Add(range, ld.Count() + 1, 5, ref missing, ref missing);
            var m = doc.Tables[1];

            doc.Tables[1].Cell(1, 1).Range.Text = "Блюдо";
            doc.Tables[1].Cell(1, 1).Range.Bold = 1;
            doc.Tables[1].Cell(1, 2).Range.Bold = 1;
            doc.Tables[1].Cell(1, 3).Range.Bold = 1;
            doc.Tables[1].Cell(1, 4).Range.Bold = 1;
            doc.Tables[1].Cell(1, 5).Range.Bold = 1;
            doc.Tables[1].Cell(1, 2).Range.Text = "Продукт";
            doc.Tables[1].Cell(1, 4).Range.Text = "Вес не фассованно";
            doc.Tables[1].Cell(1, 5).Range.Text = "Фасованный вес";
            doc.Tables[1].Cell(1, 3).Range.Text = "Количество порций";
            int i = 2, j = 1, zap = 0;

            bool k = true;
            var delicates = ld.Select(x => new { x.del_id, x.del }).Distinct();
            foreach (var list in delicates)
            {
                doc.Tables[1].Cell(i, 1).Range.Text = list.del;
                foreach (var list1 in ld.Where(x => x.del_id == list.del_id))
                {




                    doc.Tables[1].Cell(i, 2).Range.Text = (list1 as DelicatesCollForSvod).nameT;
                    doc.Tables[1].Cell(i, 3).Range.Text = (list1 as DelicatesCollForSvod).count.ToString();
                    doc.Tables[1].Cell(i, 4).Range.Text = ((list1 as DelicatesCollForSvod).ves * (list1 as DelicatesCollForSvod).count).ToString() + (list1 as DelicatesCollForSvod).mera;
                    doc.Tables[1].Cell(i, 5).Range.Text = (list1 as DelicatesCollForSvod).itogfass.ToString() + (list1 as DelicatesCollForSvod).fassIz;
                    i++;
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

            try
            {
                application.PrintPreview = true;
                application.PrintOut();
                application.Documents.Close(SaveOptions.None, Type.Missing, Type.Missing);
            }
            catch { MessageBox.Show("Произошла ошибка. Проверьте подключение принтера."); }
            //  application.Documents.Close();

            //application.DocumentBeforeClose += application_DocumentBeforeClose;
            //Process[] ps1 = System.Diagnostics.Process.GetProcessesByName("WINWORD");
            //foreach (Process pr in ps1)
            //{

            //    pr.Kill();


            //} 
            //application.Documents.Close();          
        }

        private void RadTabControl_SelectionChanged(object sender, Telerik.Windows.Controls.RadSelectionChangedEventArgs e)
        {
            if (rt.SelectedIndex == 1)
            {
                tab.Visibility = Visibility.Visible;
            }
            else
            {
                tab.Visibility = Visibility.Collapsed;
            }

        }


    }
}
