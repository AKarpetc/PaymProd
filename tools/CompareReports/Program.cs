using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var oldPath = args.Length > 0 ? args[0] : Path.Combine(baseDir, "tests.txt");
var newPath = args.Length > 1 ? args[1] : Path.Combine(baseDir, "from_new.txt");

if (!File.Exists(oldPath) || !File.Exists(newPath))
{
    Console.Error.WriteLine($"[ERROR] Missing input files.\nOld: {oldPath}\nNew: {newPath}");
    return 1;
}

var oldEntries = ParseReport(File.ReadAllLines(oldPath), CultureInfo.InvariantCulture);
var newEntries = ParseReport(File.ReadAllLines(newPath), new CultureInfo("ru-RU"));

Console.WriteLine($"Parsed entries: old={oldEntries.Count}, new={newEntries.Count}");

var oldDict = oldEntries.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
var newDict = newEntries.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);

var allNames = oldDict.Keys.Union(newDict.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();

var differences = new List<(string Name, decimal? OldValue, decimal? NewValue, string? OldUnit, string? NewUnit)>();

foreach (var name in allNames)
{
    oldDict.TryGetValue(name, out var oldEntry);
    newDict.TryGetValue(name, out var newEntry);

    if (oldEntry == null || newEntry == null)
    {
        differences.Add((name, oldEntry?.Quantity, newEntry?.Quantity, oldEntry?.Unit, newEntry?.Unit));
        continue;
    }

    var tolerance = GetTolerance(oldEntry);
    if (Math.Abs(oldEntry.Quantity - newEntry.Quantity) > tolerance ||
        !string.Equals(oldEntry.Unit, newEntry.Unit, StringComparison.OrdinalIgnoreCase))
    {
        differences.Add((name, oldEntry.Quantity, newEntry.Quantity, oldEntry.Unit, newEntry.Unit));
    }
}

if (differences.Count == 0)
{
    Console.WriteLine("Reports match.");
    return 0;
}

Console.WriteLine("Differences:");
Console.WriteLine("Product\tOld\tUnit\tNew\tUnit");
foreach (var diff in differences)
{
    Console.WriteLine($"{diff.Name}\t{diff.OldValue?.ToString(CultureInfo.InvariantCulture) ?? "-"}\t{diff.OldUnit ?? "-"}\t{diff.NewValue?.ToString(CultureInfo.InvariantCulture) ?? "-"}\t{diff.NewUnit ?? "-"}");
}

return 0;

static decimal GetTolerance(ReportEntry entry)
{
    if (entry.Unit.Equals("шт", StringComparison.OrdinalIgnoreCase) ||
        entry.Unit.Equals("пач", StringComparison.OrdinalIgnoreCase) ||
        entry.Unit.Equals("бут", StringComparison.OrdinalIgnoreCase) ||
        entry.Unit.Contains("Рул", StringComparison.OrdinalIgnoreCase) ||
        entry.Unit.Equals("Банки", StringComparison.OrdinalIgnoreCase))
    {
        return 0.001m;
    }

    return entry.Quantity < 1 ? 0.01m : 0.1m;
}

static List<ReportEntry> ParseReport(string[] lines, CultureInfo culture)
{
    var entries = new List<ReportEntry>();
    foreach (var line in lines)
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var normalizedLine = Regex.Replace(line, @"\s{2,}", "\t");
        var parts = normalizedLine.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var tokens = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (tokens.Count < 3) continue;

        for (var i = 0; i + 2 < tokens.Count; i += 3)
        {
            var name = tokens[i].Trim();
            if (!TryParseDecimal(tokens[i + 1], culture, out var value)) break;
            var unit = tokens[i + 2].Trim();

            entries.Add(new ReportEntry(name, value, unit));
        }
    }

    return entries;
}

static bool TryParseDecimal(string text, CultureInfo culture, out decimal value)
{
    var normalized = text.Replace(" ", "").Replace("\u00A0", "");
    if (decimal.TryParse(normalized, NumberStyles.Any, culture, out value))
    {
        return true;
    }

    // Fallback between comma/dot
    var swapped = normalized.Contains(',')
        ? normalized.Replace(',', '.')
        : normalized.Replace('.', ',');
    return decimal.TryParse(swapped, NumberStyles.Any, culture, out value);
}

record ReportEntry(string Name, decimal Quantity, string Unit);
