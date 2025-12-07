using Microsoft.Data.Sqlite;

var defaultDb = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "lastDb", "MenuCalc.db"));

string dbPath;
string query;

if (args.Length > 0 && File.Exists(args[0]))
{
    dbPath = args[0];
    query = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "SELECT name FROM sqlite_master WHERE type='table'";
}
else
{
    dbPath = defaultDb;
    query = args.Length > 0 ? string.Join(" ", args) : "SELECT name FROM sqlite_master WHERE type='table'";
}

if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"[ERROR] Database not found: {dbPath}");
    return 1;
}

try
{
    using var connection = new SqliteConnection($"Data Source={dbPath}");
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
    return 1;
}

return 0;
