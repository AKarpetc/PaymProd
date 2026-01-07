using System;
using System.ComponentModel;

namespace PaymProdNet9.Models;

/// <summary>
/// Модель меню банкета
/// </summary>
public class Menus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CountP { get; set; }
    public string DateBan { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public decimal? ServicePercent { get; set; }

    /// <summary>
    /// Вспомогательное поле для корректной сортировки по дате в таблицах (SavedMenusPage и др.).
    /// Хранит распарсенное значение DateBan как DateTime, если это возможно.
    /// </summary>
    public DateTime? DateBanParsed
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DateBan))
                return null;

            if (DateTime.TryParse(DateBan, out var dt))
                return dt;

            return null;
        }
    }
}

/// <summary>
/// Модель действующего меню с блюдами
/// </summary>
public class MenuDel_act : INotifyPropertyChanged
{
    private int _id;
    private int _idmen;
    private string _del = string.Empty;
    private int _del_id;
    private string _sost = string.Empty;
    private decimal _countpor;
    private List<Components> _lcomp = new();
    private bool _isModified;
    private bool _hideInMenu;
    private bool _hideInProductReport;

    public int Id
    {
        get => _id;
        set
        {
            _id = value;
            OnPropertyChanged(nameof(Id));
        }
    }

    public int Idmen
    {
        get => _idmen;
        set
        {
            _idmen = value;
            OnPropertyChanged(nameof(Idmen));
        }
    }

    public string Del
    {
        get => _del;
        set
        {
            _del = value;
            OnPropertyChanged(nameof(Del));
        }
    }

    public int Del_id
    {
        get => _del_id;
        set
        {
            _del_id = value;
            OnPropertyChanged(nameof(Del_id));
        }
    }

    public string Sost
    {
        get => _sost;
        set
        {
            _sost = value;
            OnPropertyChanged(nameof(Sost));
        }
    }

    public decimal Countpor
    {
        get => _countpor;
        set
        {
            _countpor = value;
            OnPropertyChanged(nameof(Countpor));
        }
    }

    public List<Components> Lcomp
    {
        get => _lcomp;
        set
        {
            _lcomp = value;
            OnPropertyChanged(nameof(Lcomp));
        }
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            _isModified = value;
            OnPropertyChanged(nameof(IsModified));
        }
    }

    /// <summary>
    /// Флаг "не показывать в меню" для конкретной строки меню (блюдо/продукт).
    /// Такие позиции могут участвовать только в отчетах по продуктам.
    /// </summary>
    public bool HideInMenu
    {
        get => _hideInMenu;
        set
        {
            _hideInMenu = value;
            OnPropertyChanged(nameof(HideInMenu));
        }
    }

    /// <summary>
    /// Флаг "не показывать в отчете по продуктам".
    /// </summary>
    public bool HideInProductReport
    {
        get => _hideInProductReport;
        set
        {
            _hideInProductReport = value;
            OnPropertyChanged(nameof(HideInProductReport));
        }
    }

    public decimal? Markup { get; set; }
    public decimal DefaultMarkup { get; set; } = 200;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Модель блюда
/// </summary>
public class DelicatesColl
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Ves { get; set; }
    public decimal Count { get; set; }
    public string Opis { get; set; } = string.Empty;
    public int? IDType { get; set; }
    public int TypeSortOrder { get; set; }
    public int? LinkedProductId { get; set; }
    public decimal? LinkedProductDefaultCount { get; set; }
    /// <summary>
    /// Флаг "автоматически добавлять в меню" (при создании нового меню).
    /// </summary>
    public bool AutoAdd { get; set; }
    /// <summary>
    /// Флаг "не показывать в меню". Такое блюдо отображается только в отчёте по продуктам,
    /// но не должно выводиться в отчётах меню (печать меню и т.п.).
    /// </summary>
    public bool HideInMenu { get; set; }
    /// <summary>
    /// Флаг "не показывать в отчете по продуктам".
    /// Если true, блюдо и его компоненты не учитываются в сводном отчете по продуктам.
    /// </summary>
    public bool HideInProductReport { get; set; }
    /// <summary>
    /// Наценка по умолчанию (в процентах)
    /// </summary>
    public decimal DefaultMarkup { get; set; } = 200;
    public List<Components> Lcomp { get; set; } = new();

    /// <summary>
    /// Флаг мягкого удаления. Если true, блюдо скрыто из справочников и доступных для добавления списков,
    /// но продолжает использоваться в уже созданных меню и отчетах.
    /// </summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Модель для сводного отчета
/// </summary>
public class DelicatesCollForSvod : Components
{
    public int Idmen { get; set; }
    public string Del { get; set; } = string.Empty;
    public int Del_id { get; set; }
    public string Sost { get; set; } = string.Empty;
    public decimal Countpor { get; set; }
    public decimal Itog { get; set; }
    public decimal ItogFass { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// Модель компонента (продукта в блюде)
/// </summary>
public class Components : INotifyPropertyChanged
{
    private decimal _fass1;
    private int _id;
    private int _delid;
    private int _prodid;
    private string _name = string.Empty;
    private string _type = string.Empty;
    private decimal _ves;
    private string _mera = string.Empty;
    private decimal _fass;
    private string _fassIz = string.Empty;
    private decimal _count;
    private int _flag;
    private string _nameT = string.Empty;
    private int _menuRoundingPrecision = 2;
    private bool _doNotConvertToPackInMenu;

    public decimal Fass1
    {
        get => _fass1;
        set
        {
            _fass1 = value;
            OnPropertyChanged(nameof(Fass1));
        }
    }

    public int Id
    {
        get => _id;
        set
        {
            _id = value;
            OnPropertyChanged(nameof(Id));
        }
    }

    public int Delid
    {
        get => _delid;
        set
        {
            _delid = value;
            OnPropertyChanged(nameof(Delid));
        }
    }

    public int Prodid
    {
        get => _prodid;
        set
        {
            _prodid = value;
            OnPropertyChanged(nameof(Prodid));
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            _type = value;
            OnPropertyChanged(nameof(Type));
        }
    }

    public decimal Ves
    {
        get => _ves;
        set
        {
            _ves = value;
            OnPropertyChanged(nameof(Ves));
            OnPropertyChanged(nameof(DisplayVes));
        }
    }

    public string Mera
    {
        get => _mera;
        set
        {
            _mera = value;
            OnPropertyChanged(nameof(Mera));
        }
    }

    public decimal Fass
    {
        get => _fass;
        set
        {
            _fass = value;
            OnPropertyChanged(nameof(Fass));
        }
    }

    public string FassIz
    {
        get => _fassIz;
        set
        {
            _fassIz = value;
            OnPropertyChanged(nameof(FassIz));
        }
    }

    public decimal Count
    {
        get => _count;
        set
        {
            _count = value;
            OnPropertyChanged(nameof(Count));
        }
    }

    public int Flag
    {
        get => _flag;
        set
        {
            _flag = value;
            OnPropertyChanged(nameof(Flag));
        }
    }

    public string NameT
    {
        get => _nameT;
        set
        {
            _nameT = value;
            OnPropertyChanged(nameof(NameT));
        }
    }

    public int MenuRoundingPrecision
    {
        get => _menuRoundingPrecision;
        set
        {
            _menuRoundingPrecision = value;
            OnPropertyChanged(nameof(MenuRoundingPrecision));
            OnPropertyChanged(nameof(DisplayVes));
        }
    }

    /// <summary>
    /// Флаг продукта: "не переводить в фасованные в меню/отчете меню".
    /// </summary>
    public bool DoNotConvertToPackInMenu
    {
        get => _doNotConvertToPackInMenu;
        set
        {
            _doNotConvertToPackInMenu = value;
            OnPropertyChanged(nameof(DoNotConvertToPackInMenu));
        }
    }

    public decimal DisplayVes
    {
        get => RoundForMenu(_ves, _menuRoundingPrecision);
        set => Ves = value;
    }

    private static decimal RoundForMenu(decimal value, int precision)
    {
        var doubleValue = (double)value;
        if (precision <= 0) return (decimal)Math.Ceiling(doubleValue);

        var multiplier = Math.Pow(10, precision);
        return (decimal)(Math.Ceiling(doubleValue * multiplier) / multiplier);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Модель представления продукта
/// </summary>
public class ProductView : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _type = string.Empty;
    private string _ves = string.Empty;
    private int _id;
    private decimal _fass;
    private int _tid;
    private int _vid;
    private int _iz;
    private string _izname = string.Empty;
    private int _prizMen;
    private bool _prizMen1;
    private decimal _count;
    private bool _autoAdd;
    private int _countPeople;
    private bool _mainCount;
    private decimal _price;
    private decimal _basePrice;
    private bool _saveToBasePrice = true;
    private bool _isModified;
    private decimal _originalPrice;
    private bool _hideInMenu;
    private bool _doNotConvertToPackInMenu;
    private bool _isDeleted;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            _type = value;
            OnPropertyChanged(nameof(Type));
        }
    }

    public string Ves
    {
        get => _ves;
        set
        {
            _ves = value;
            OnPropertyChanged(nameof(Ves));
        }
    }

    public int ID
    {
        get => _id;
        set
        {
            _id = value;
            OnPropertyChanged(nameof(ID));
        }
    }

    public decimal Fass
    {
        get => _fass;
        set
        {
            _fass = value;
            OnPropertyChanged(nameof(Fass));
        }
    }

    public int TID
    {
        get => _tid;
        set
        {
            _tid = value;
            OnPropertyChanged(nameof(TID));
        }
    }

    public int VID
    {
        get => _vid;
        set
        {
            _vid = value;
            OnPropertyChanged(nameof(VID));
        }
    }

    public int Iz
    {
        get => _iz;
        set
        {
            _iz = value;
            OnPropertyChanged(nameof(Iz));
        }
    }

    public string IzName
    {
        get => _izname;
        set
        {
            _izname = value;
            OnPropertyChanged(nameof(IzName));
        }
    }

    public int PrizMen
    {
        get => _prizMen;
        set
        {
            _prizMen = value;
            OnPropertyChanged(nameof(PrizMen));
        }
    }

    public bool PrizMen1
    {
        get => _prizMen1;
        set
        {
            _prizMen1 = value;
            OnPropertyChanged(nameof(PrizMen1));
        }
    }

    public decimal Count
    {
        get => _count;
        set
        {
            _count = value;
            OnPropertyChanged(nameof(Count));
        }
    }

    public bool AutoAdd
    {
        get => _autoAdd;
        set
        {
            _autoAdd = value;
            OnPropertyChanged(nameof(AutoAdd));
        }
    }

    public int CountPeople
    {
        get => _countPeople;
        set
        {
            _countPeople = value;
            OnPropertyChanged(nameof(CountPeople));
        }
    }

    public bool MainCount
    {
        get => _mainCount;
        set
        {
            _mainCount = value;
            OnPropertyChanged(nameof(MainCount));
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            _price = value;
            OnPropertyChanged(nameof(Price));
        }
    }

    public decimal BasePrice
    {
        get => _basePrice;
        set
        {
            _basePrice = value;
            OnPropertyChanged(nameof(BasePrice));
        }
    }

    public bool SaveToBasePrice
    {
        get => _saveToBasePrice;
        set
        {
            _saveToBasePrice = value;
            OnPropertyChanged(nameof(SaveToBasePrice));
        }
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            _isModified = value;
            OnPropertyChanged(nameof(IsModified));
        }
    }

    public decimal OriginalPrice
    {
        get => _originalPrice;
        set => _originalPrice = value;
    }

    /// <summary>
    /// Флаг "не показывать в меню" для продукта.
    /// Такие продукты остаются в отчете по продуктам, но могут быть скрыты в меню.
    /// </summary>
    public bool HideInMenu
    {
        get => _hideInMenu;
        set
        {
            _hideInMenu = value;
            OnPropertyChanged(nameof(HideInMenu));
        }
    }

    /// <summary>
    /// Флаг "не переводить в фасованные в меню/отчете меню".
    /// Если true — в отчете по меню всегда показываем в базовой единице (без пересчета в фасовку).
    /// </summary>
    public bool DoNotConvertToPackInMenu
    {
        get => _doNotConvertToPackInMenu;
        set
        {
            _doNotConvertToPackInMenu = value;
            OnPropertyChanged(nameof(DoNotConvertToPackInMenu));
        }
    }

    /// <summary>
    /// Флаг мягкого удаления. Если true, продукт скрывается из справочников и списков выбора,
    /// но остается во всех старых блюдах, меню и отчетах.
    /// </summary>
    public bool IsDeleted
    {
        get => _isDeleted;
        set
        {
            _isDeleted = value;
            OnPropertyChanged(nameof(IsDeleted));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Модель для редактирования наценки на блюда
/// </summary>
public class DishMarkupView : INotifyPropertyChanged
{
    private int _id;
    private string _name = string.Empty;
    private string _shortComposition = string.Empty;
    private decimal _defaultMarkup;
    private decimal _markup;
    private bool _saveToDefault;
    private bool _isModified;
    private string _type = string.Empty;
    private int _typeId;

    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(nameof(Id)); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string ShortComposition
    {
        get => _shortComposition;
        set { _shortComposition = value; OnPropertyChanged(nameof(ShortComposition)); }
    }

    public string Type
    {
        get => _type;
        set { _type = value; OnPropertyChanged(nameof(Type)); }
    }

    public int TypeId
    {
        get => _typeId;
        set { _typeId = value; OnPropertyChanged(nameof(TypeId)); }
    }

    public decimal BaseCost { get; set; }
    public decimal Count { get; set; }

    /// <summary>
    /// Наценка по умолчанию (из справочника)
    /// </summary>
    public decimal DefaultMarkup
    {
        get => _defaultMarkup;
        set { _defaultMarkup = value; OnPropertyChanged(nameof(DefaultMarkup)); }
    }

    /// <summary>
    /// Текущая наценка (для меню)
    /// </summary>
    public decimal Markup
    {
        get => _markup;
        set 
        { 
            _markup = value; 
            _isModified = true;
            OnPropertyChanged(nameof(Markup)); 
            OnPropertyChanged(nameof(IsModified));
        }
    }

    public bool SaveToDefault
    {
        get => _saveToDefault;
        set { _saveToDefault = value; OnPropertyChanged(nameof(SaveToDefault)); }
    }

    public bool IsModified
    {
        get => _isModified;
        set { _isModified = value; OnPropertyChanged(nameof(IsModified)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Статический класс для передачи ID продукта
/// </summary>
public static class IDProd
{
    public static int ID { get; set; }
    public static double Ves { get; set; }
}

/// <summary>
/// Статический класс для редактирования продукта
/// </summary>
public static class ProductEdit
{
    public static bool Flag { get; set; }
    public static ProductView? Pv { get; set; }
}