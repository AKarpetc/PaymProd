namespace PaymProdNet9.Models;

/// <summary>
/// Тип продукта
/// </summary>
public class ProductType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    /// <summary>
    /// Флаг "не показывать в меню". Продукты этого типа не отображаются в меню,
    /// но учитываются в отчёте по продуктам.
    /// </summary>
    public bool HideInMenu { get; set; }
}