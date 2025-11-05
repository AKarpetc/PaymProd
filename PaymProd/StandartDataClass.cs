using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;

namespace PaymProd
{
    public static class IDProd
    {
        public static int ID { get; set; }
        public static double ves  { get; set; }
    }
    public class MenuDel_act
    {
        
        public int idmen { get; set; }
        public string del  {get;set;}
        public int del_id { get; set; }
        public string sost { get; set; }
        public decimal countpor { get; set; }
        public List<Components> lcomp { get; set; }

    }

    public class DelicatesColl
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public decimal ves { get; set; }
        public decimal count { get; set; }
        public string opis { get; set; }
        public int? IDType { get; set; }
        public List<Components> lcomp { get; set; }
        
    }
    public class DelicatesCollForSvod : Components
    {
        public int idmen { get; set; }
        public string del { get; set; }
        public int del_id { get; set; }
        public string sost { get; set; }
        public decimal countpor { get; set; }
        public decimal itog { get; set; }
        public decimal itogfass { get; set; }
     

    }
    public class Components
    {
        public decimal fass1 { get; set; }
        public int id { get; set; }
        public int Delid { get; set; }
        public int Prodid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public decimal ves { get; set; }
        public string mera { get; set; }
        public decimal fass { get; set; }
        public string fassIz { get; set; }
        public decimal count { get; set; }
        public int flag { get; set; }
        public string nameT { get; set; }
    }
    public class ProductView
    {
        public string name {get;set;}
           
        public string type { get; set; }
        public string ves { get; set; }
        public int ID { get; set; }
        public decimal fass { get; set; }
        public int TID { get; set; }
        public int VID { get; set; }
        public int iz { get; set; }
        public string izname { get; set; }
        public int prizMen { get; set; }
        public bool prizMen1 { get; set; }
        public decimal count { get; set; }
        public bool AutoAdd { get; set; }
        public int CountPeople { get; set; }
        public bool MainCount { get; set; }
    }
    public static class productEdit
    {
        public static bool flag;
      public static ProductView pv { get; set; }
    }
    class StandartDataClass
    {
    }
    public class TelerikCustomLocalizationManager : LocalizationManager
    {
        public override string GetStringOverride(string key)
        {
            switch (key)
            {


                case "GridViewAlwaysVisibleNewRow":
                    return "Кликните, чтобы добавить новую строку";
                case "GridViewClearFilter":
                    return "Очистить фильтр";
                case "GridViewFilter":
                    return "Фильтр";
                case "GridViewFilterAnd":
                    return "И";
                case "GridViewFilterContains":
                    return "Содержит";
                case "GridViewFilterDoesNotContain":
                    return "Не содержит";
                case "GridViewFilterEndsWith":
                    return "Заканчивается";
                case "GridViewFilterIsContainedIn":
                    return "Содержится";
                case "GridViewFilterIsEqualTo":
                    return "Равно";
                case "GridViewFilterIsGreaterThan":
                    return "Больше";
                case "GridViewFilterIsGreaterThanOrEqualTo":
                    return "Больше или равно";
                case "GridViewFilterIsNotContainedIn":
                    return "Не содержится";
                case "GridViewFilterIsLessThan":
                    return "Меньше";
                case "GridViewFilterIsLessThanOrEqualTo":
                    return "Меньше или равно";
                case "GridViewFilterIsNotEqualTo":
                    return "Не равно";
                case "GridViewFilterMatchCase":
                    return "С учетом регистра";
                case "GridViewFilterOr":
                    return "Или";
                case "GridViewFilterSelectAll":
                    return "Выбрать все";
                case "GridViewFilterShowRowsWithValueThat":
                    return "Показывать строки со значением";
                case "GridViewFilterStartsWith":
                    return "Начинается с";
                case "GridViewFilterIsNull":
                    return "Равно нулю";
                case "GridViewFilterIsNotNull":
                    return "Не равно нулю";
                case "GridViewGroupPanelText":
                    return "Перетащите сюда заголовок для группировки по данной колонке";
                case "GridViewGroupPanelTopText":
                    return "Заголовок группы";
                case "GridViewGroupPanelTopTextGrouped":
                    return "Сгруппировано по:";
                case "Close":
                    return "Закрыть";

            }
            return base.GetStringOverride(key);
        }
    }
}
