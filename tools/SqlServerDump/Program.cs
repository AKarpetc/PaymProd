using Microsoft.Data.SqlClient;

var mdfPath = args.Length > 0 && args[0].EndsWith(".mdf", StringComparison.OrdinalIgnoreCase)
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "lastDb", "MenuCaolc.mdf"));

if (!File.Exists(mdfPath))
{
    Console.Error.WriteLine($"[ERROR] MDF file not found: {mdfPath}");
    return 1;
}

var query = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "SELECT TOP 5 * FROM Producrs";

var connectionString =
    $"Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=master;AttachDbFilename={mdfPath};Integrated Security=True;Connect Timeout=30";

try
{
    using var connection = new SqlConnection(connectionString);
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = query;

    using var reader = command.ExecuteReader();
    var headers = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
    Console.WriteLine(string.Join("\t", headers));

    while (reader.Read())
    {
        var values = new object[reader.FieldCount];
        reader.GetValues(values);
        Console.WriteLine(string.Join("\t", values.Select(v => v?.ToString() ?? "NULL")));
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

return 0;
