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

    /// <summary>
    /// Флаг мягкого удаления. Если true, тип не отображается в справочниках,
    /// но может присутствовать в старых блюдах и меню.
    /// </summary>
    public bool IsDeleted { get; set; }
}