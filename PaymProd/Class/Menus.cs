using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using Word = Microsoft.Office.Interop.Word;
namespace PaymProd.Class
{
  public  class MenusPrint
    {

      public Microsoft.Office.Interop.Word.Application GetApplication(List<DelicatesColl> mususCollection, int fromType, string Name)
      {
          Microsoft.Office.Interop.Word.Application application = new Microsoft.Office.Interop.Word.Application();


          Object missing = Type.Missing;


          application.Documents.Add();

          Microsoft.Office.Interop.Word.Document doc = application.ActiveDocument;
                  
          doc.Paragraphs.Add();
          doc.Paragraphs.Add();
          doc.Paragraphs[1].Range.Text = "Меню "+Name;

          doc.Paragraphs[1].Range.Bold = 1;
          doc.Paragraphs[1].Range.Font.Name = "Times New Roman";
          
          doc.Paragraphs[1].Range.Font.Size = fromType==0?16:12;

           doc.Paragraphs[1].Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;

          doc.Paragraphs.PageBreakBefore = 0;

          Microsoft.Office.Interop.Word.Range range = doc.Paragraphs[2].Range;
          doc.Paragraphs.Add();
          doc.GridDistanceHorizontal = 1;
          doc.GridDistanceVertical = 1;
          doc.GridOriginVertical = 1;
          doc.GridOriginHorizontal = 1;
          doc.PageSetup.TopMargin = 30;
          doc.PageSetup.LeftMargin = 30;
          doc.PageSetup.RightMargin = 30;
          doc.PageSetup.FooterDistance = 1;
          // = 10;
          doc.Paragraphs[2].Range.Bold = 0;
          doc.Paragraphs[2].Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphLeft;

          doc.Tables.Add(range, mususCollection.Count() + mususCollection.Select(x => new { x.type, x.IDType }).Distinct().OrderBy(x => x.type).Count(), 2, ref missing, ref missing);
          var m = doc.Tables[1];

          int i = 1, j = 1, zap = 0;
          doc.Tables[1].Columns[1].Width = 120;
          doc.Tables[1].Columns[2].Width = 430;
         
          foreach (var list2 in mususCollection.Select(x => new { x.type, x.IDType }).Distinct().OrderBy(x => x.type))
          {
              doc.Tables[1].Cell(i, 1).Range.Text = list2.type;
     
              doc.Tables[1].Cell(i, 1).Range.Font.Size = fromType == 0 ? 14 : 11; 
              doc.Tables[1].Cell(i, 1).Range.Bold = 1;
              doc.Tables[1].Cell(i, 1).TopPadding = 0;
              doc.Tables[1].Cell(i, 1).BottomPadding = 0;
             
              doc.Tables[1].Cell(i, 1).Range.Font.Name = "Times New Roman";
              i++;
              foreach (var list in mususCollection.Where(x => x.IDType == list2.IDType))
              {

                  doc.Tables[1].Cell(i, 1).Range.Text = list.name;
                  doc.Tables[1].Cell(i, 1).Range.Font.Name = "Times New Roman";
                  doc.Tables[1].Cell(i, 1).Range.Font.Size = fromType == 0 ? 14 : 10; 
                  string content = "Cостав: ";

                  foreach (var list1 in list.lcomp)
                  {
                      try
                      {
                          decimal fass = 0;
                          if (list1.fass > 0)
                          {
                               fass = Math.Round(Convert.ToDecimal(((list1.ves * list.count / (list1.fass == 0 ? 1 : list1.fass)) <= 1 ? 0 : (list1.ves * list.count / list1.fass))), 2, MidpointRounding.AwayFromZero);
                          }
                        
                          
                          var fassIzmer = list1.fassIz;

                          if (fass < 1)
                          {
                              fass = Math.Round(Convert.ToDecimal((list1.ves * list.count)),2, MidpointRounding.AwayFromZero);
                              fassIzmer = list1.mera;
                          }

                          decimal Summ = 0;

                        
                          //if (fass >= 1)
                          //{
                          //    Summ = Convert.ToInt32(fass) < fass ? Convert.ToInt32(fass) + 1 : Convert.ToInt32(fass);
                          //}
                          //else
                          {
                              Summ = fass;
                          }

                          content += list1.name + (fromType == 0 ? "" : "(" + Summ + fassIzmer + ")") + ",";
                      }
                      catch { }
                  }
                  doc.Tables[1].Cell(i, 2).Range.Font.Size = fromType == 0 ? 12 : 10; 
                 
                  doc.Tables[1].Cell(i, 2).Range.Text = content;
                  i++;
              }
          }

          //doc.Tables[1].Select();
          doc.PageSetup.HeaderDistance = 5;
          Word.Border[] borders = new Word.Border[3];
          Word.Table tbl = doc.Tables[doc.Tables.Count];

          //borders[0] = tbl.Borders[Word.WdBorderType.wdBorderLeft];
          //  borders[1] = tbl.Borders[Word.WdBorderType.wdBorderRight];
          borders[0] = tbl.Borders[Word.WdBorderType.wdBorderTop];
          borders[1] = tbl.Borders[Word.WdBorderType.wdBorderBottom];
          borders[2] = tbl.Borders[Word.WdBorderType.wdBorderHorizontal];
          // borders[5] = tbl.Borders[Word.WdBorderType.wdBorderVertical];
          foreach (Microsoft.Office.Interop.Word.Border border in borders)
          {
              try
              {
                  border.LineWidth = Microsoft.Office.Interop.Word.WdLineWidth.wdLineWidth150pt;
              }
              catch { }
              border.LineStyle = Microsoft.Office.Interop.Word.WdLineStyle.wdLineStyleSingle;
              border.Color = Microsoft.Office.Interop.Word.WdColor.wdColorBlack;
          }

          return application;
      }
      public    ObservableCollection<DelicatesColl> Get_Menu()
      {
        
            SqlConnection sqlcon = new SqlConnection(MainWindow.Conectionstring);
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
                        com.ves = Myreader1.GetDecimal(4);
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
                    SqlCommand sqlcom = new SqlCommand("select del_id,del_name,isnull(del_opis,''),isnull(Del_count,0) ,isnull(del_ves,0) ,Type_del_opis,del_type from View_Delicstes where del_type!=-1", sqlcon);
             
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

                    try
                    {
                        dc.IDType = Myreader.GetInt32(6);
                    }
                    catch { }
                    dc.lcomp = new List<Components>();

                    dc.lcomp.AddRange((from t in lcom where t.Delid == dc.id select t).ToList());


                     
                    
                    ldc.Add(dc);
                }
                sqlcon.Close();
               return  ldc;

            }
            catch(Exception ex) 
            {
                return null;
            }

        }
      


    }
}
