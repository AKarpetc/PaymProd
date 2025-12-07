using Microsoft.Data.SqlClient;
using System.Text;

string defaultMdfPath()
{
    var baseDir = AppContext.BaseDirectory;
    var combined = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "lastDb", "MenuCaolc.mdf"));
    return combined;
}

var mdfPath = args.Length > 0 && args[0].EndsWith(".mdf", StringComparison.OrdinalIgnoreCase)
    ? args[0]
    : defaultMdfPath();

if (!File.Exists(mdfPath))
{
    Console.Error.WriteLine($"[ERROR] MDF file not found: {mdfPath}");
    return 1;
}

var queryArgsOffset = args.Length > 0 && args[0].EndsWith(".mdf", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
var query = args.Length > queryArgsOffset
    ? string.Join(" ", args.Skip(queryArgsOffset))
    : "SELECT TOP 20 * FROM Mera";

var connectionString =
    $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={mdfPath};Integrated Security=True;Connect Timeout=30;User Instance=False";

try
{
    using var connection = new SqlConnection(connectionString);
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = query;

    using var reader = command.ExecuteReader();
    var headers = Enumerable.Range(0, reader.FieldCount)
        .Select(reader.GetName)
        .ToList();

    Console.WriteLine(string.Join("\t", headers));

    while (reader.Read())
    {
        var row = new StringBuilder();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (i > 0) row.Append('\t');
            var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i);
            row.Append(value);
        }
        Console.WriteLine(row.ToString());
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

return 0;
