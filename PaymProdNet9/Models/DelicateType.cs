namespace PaymProdNet9.Models;

/// <summary>
/// Тип блюда
/// </summary>
public class DelicateType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int? LinkedProductTypeId { get; set; }
}