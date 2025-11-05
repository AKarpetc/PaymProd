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
}

/// <summary>
/// Модель действующего меню с блюдами
/// </summary>
public class MenuDel_act : INotifyPropertyChanged
{
    private int _idmen;
    private string _del = string.Empty;
    private int _del_id;
    private string _sost = string.Empty;
    private decimal _countpor;
    private List<Components> _lcomp = new List<Components>();

    public int Idmen
    {
        get => _idmen;
        set { _idmen = value; OnPropertyChanged(nameof(Idmen)); }
    }

    public string Del
    {
        get => _del;
        set { _del = value; OnPropertyChanged(nameof(Del)); }
    }

    public int Del_id
    {
        get => _del_id;
        set { _del_id = value; OnPropertyChanged(nameof(Del_id)); }
    }

    public string Sost
    {
        get => _sost;
        set { _sost = value; OnPropertyChanged(nameof(Sost)); }
    }

    public decimal Countpor
    {
        get => _countpor;
        set { _countpor = value; OnPropertyChanged(nameof(Countpor)); }
    }

    public List<Components> Lcomp
    {
        get => _lcomp;
        set { _lcomp = value; OnPropertyChanged(nameof(Lcomp)); }
    }

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
    public List<Components> Lcomp { get; set; } = new List<Components>();
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

    public decimal Fass1
    {
        get => _fass1;
        set { _fass1 = value; OnPropertyChanged(nameof(Fass1)); }
    }

    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(nameof(Id)); }
    }

    public int Delid
    {
        get => _delid;
        set { _delid = value; OnPropertyChanged(nameof(Delid)); }
    }

    public int Prodid
    {
        get => _prodid;
        set { _prodid = value; OnPropertyChanged(nameof(Prodid)); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string Type
    {
        get => _type;
        set { _type = value; OnPropertyChanged(nameof(Type)); }
    }

    public decimal Ves
    {
        get => _ves;
        set { _ves = value; OnPropertyChanged(nameof(Ves)); }
    }

    public string Mera
    {
        get => _mera;
        set { _mera = value; OnPropertyChanged(nameof(Mera)); }
    }

    public decimal Fass
    {
        get => _fass;
        set { _fass = value; OnPropertyChanged(nameof(Fass)); }
    }

    public string FassIz
    {
        get => _fassIz;
        set { _fassIz = value; OnPropertyChanged(nameof(FassIz)); }
    }

    public decimal Count
    {
        get => _count;
        set { _count = value; OnPropertyChanged(nameof(Count)); }
    }

    public int Flag
    {
        get => _flag;
        set { _flag = value; OnPropertyChanged(nameof(Flag)); }
    }

    public string NameT
    {
        get => _nameT;
        set { _nameT = value; OnPropertyChanged(nameof(NameT)); }
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

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string Type
    {
        get => _type;
        set { _type = value; OnPropertyChanged(nameof(Type)); }
    }

    public string Ves
    {
        get => _ves;
        set { _ves = value; OnPropertyChanged(nameof(Ves)); }
    }

    public int ID
    {
        get => _id;
        set { _id = value; OnPropertyChanged(nameof(ID)); }
    }

    public decimal Fass
    {
        get => _fass;
        set { _fass = value; OnPropertyChanged(nameof(Fass)); }
    }

    public int TID
    {
        get => _tid;
        set { _tid = value; OnPropertyChanged(nameof(TID)); }
    }

    public int VID
    {
        get => _vid;
        set { _vid = value; OnPropertyChanged(nameof(VID)); }
    }

    public int Iz
    {
        get => _iz;
        set { _iz = value; OnPropertyChanged(nameof(Iz)); }
    }

    public string IzName
    {
        get => _izname;
        set { _izname = value; OnPropertyChanged(nameof(IzName)); }
    }

    public int PrizMen
    {
        get => _prizMen;
        set { _prizMen = value; OnPropertyChanged(nameof(PrizMen)); }
    }

    public bool PrizMen1
    {
        get => _prizMen1;
        set { _prizMen1 = value; OnPropertyChanged(nameof(PrizMen1)); }
    }

    public decimal Count
    {
        get => _count;
        set { _count = value; OnPropertyChanged(nameof(Count)); }
    }

    public bool AutoAdd
    {
        get => _autoAdd;
        set { _autoAdd = value; OnPropertyChanged(nameof(AutoAdd)); }
    }

    public int CountPeople
    {
        get => _countPeople;
        set { _countPeople = value; OnPropertyChanged(nameof(CountPeople)); }
    }

    public bool MainCount
    {
        get => _mainCount;
        set { _mainCount = value; OnPropertyChanged(nameof(MainCount)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
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

