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
    public int RoundingPrecision { get; set; } = 2; // 0 = до целого, 2 = до сотых
}