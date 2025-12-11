namespace PaymProdNet9.Models;

/// <summary>
/// Единица измерения
/// </summary>
public class Measure
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Fass { get; set; }
    public string FassIzmer { get; set; } = string.Empty;
    public int RoundingPrecision { get; set; } = 2; // для отчетов
    public int MenuRoundingPrecision { get; set; } = 2; // для меню и блюд

    /// <summary>
    /// Флаг мягкого удаления. Если true, мера не предлагается в справочниках,
    /// но может использоваться для старых продуктов и блюд.
    /// </summary>
    public bool IsDeleted { get; set; }
}