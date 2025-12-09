using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using PaymProdNet9.Data;
using PaymProdNet9.Services;

namespace PaymProdDbTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string? _dbPath;
    private string? _currentTableName;
    private DataTable? _currentTable;

    public MainWindow()
    {
        InitializeComponent();
        UseDefaultDb();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTablesAsync();
    }

    private void UseDefaultDb()
    {
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PaymProdNet9",
            "MenuCalc.db");

        _dbPath = defaultPath;
        DbPathTextBox.Text = _dbPath;
    }

    private bool EnsureDbPath()
    {
        if (string.IsNullOrWhiteSpace(DbPathTextBox.Text))
        {
            MessageBox.Show("Укажите путь к базе данных.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        _dbPath = DbPathTextBox.Text.Trim();
        return true;
    }

    private SqliteConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_dbPath))
            throw new InvalidOperationException("Путь к базе данных не задан.");

        return new SqliteConnection($"Data Source={_dbPath}");
    }

    private async void DownloadStartDb_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureDbPath())
            return;

        try
        {
            var directory = Path.GetDirectoryName(_dbPath!);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var result = await UpdateService.TryDownloadStartDatabaseAsync(_dbPath!, this, replaceExisting: true, silentSuccess: false);
            if (result)
            {
                await LoadTablesAsync();
                await ReloadTableAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при скачивании стартовой базы:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UseDefaultDb_Click(object sender, RoutedEventArgs e)
    {
        UseDefaultDb();
        _ = LoadTablesAsync();
    }

    private void BrowseDb_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "SQLite базы (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*",
            CheckFileExists = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            _dbPath = dialog.FileName;
            DbPathTextBox.Text = _dbPath;
            _ = LoadTablesAsync();
        }
    }

    private async void ReloadData_Click(object sender, RoutedEventArgs e)
    {
        await LoadTablesAsync();
        await ReloadTableAsync();
    }

    private async void LoadProducts_Click(object sender, RoutedEventArgs e)
    {
        await ReloadTableAsync("Producrs");
    }

    private async Task LoadTablesAsync()
    {
        if (!EnsureDbPath())
            return;

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

            using var reader = await cmd.ExecuteReaderAsync();
            var names = new List<string>();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            TablesListBox.ItemsSource = names;

            if (TablesListBox.SelectedItem == null && names.Count > 0)
            {
                var defaultTable = names.Contains("Producrs") ? "Producrs" : names[0];
                TablesListBox.SelectedItem = defaultTable;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке списка таблиц:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SwapFlags_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureDbPath())
            return;

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                UPDATE Producrs
                SET
                    Avtomat = Priz_menu,
                    Priz_menu = Avtomat;
            ";

            var affected = await cmd.ExecuteNonQueryAsync();
            transaction.Commit();

            MessageBox.Show($"Готово. Обновлено строк: {affected}.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            await ReloadTableAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при перестановке флагов:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveDbAs_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureDbPath())
            return;

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SQLite базы (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*",
                FileName = Path.GetFileName(_dbPath)
            };

            if (dialog.ShowDialog(this) == true)
            {
                if (_dbPath is null)
                    return;

                SqliteConnection.ClearAllPools();
                File.Copy(_dbPath, dialog.FileName, overwrite: true);
                MessageBox.Show("База данных сохранена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении базы:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecuteSql_Click(object sender, RoutedEventArgs e)
    {
        SqlStatusTextBlock.Text = string.Empty;
        SqlResultGrid.ItemsSource = null;

        if (!EnsureDbPath())
            return;

        var sql = SqlTextBox.Text;
        if (string.IsNullOrWhiteSpace(sql))
        {
            MessageBox.Show("Введите SQL-запрос или скрипт.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            var firstWord = sql.TrimStart().Split(' ', '\n', '\r', '\t')[0];
            var isSelect = firstWord.Equals("select", StringComparison.OrdinalIgnoreCase)
                           || firstWord.Equals("pragma", StringComparison.OrdinalIgnoreCase);

            if (isSelect)
            {
                using var reader = await cmd.ExecuteReaderAsync();
                var table = new DataTable();
                table.Load(reader);
                SqlResultGrid.ItemsSource = table.DefaultView;
                SqlStatusTextBlock.Text = $"Выборка: {table.Rows.Count} строк.";
            }
            else
            {
                var affected = await cmd.ExecuteNonQueryAsync();
                SqlStatusTextBlock.Text = $"Команда выполнена, затронуто строк: {affected}.";
            }
        }
        catch (Exception ex)
        {
            SqlStatusTextBlock.Text = $"Ошибка: {ex.Message}";
            MessageBox.Show($"Ошибка при выполнении SQL:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TablesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        await ReloadTableAsync();
    }

    private const string RowIdColumnName = "__rowid_internal";

    private async Task ReloadTableAsync(string? tableName = null)
    {
        if (!EnsureDbPath())
            return;

        tableName ??= TablesListBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(tableName))
            return;

        try
        {
            ProductsGrid.ItemsSource = null;
            _currentTableName = tableName;
            _currentTable = null;

            using var connection = CreateConnection();
            await connection.OpenAsync();

            var safeName = tableName.Replace("\"", string.Empty);

            var cmd = connection.CreateCommand();
            // добавляем скрытое поле rowid, чтобы знать, какую строку обновлять
            cmd.CommandText = $"SELECT rowid as __rowid_internal, * FROM \"{safeName}\"";

            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();
            table.Load(reader);

            _currentTable = table;
            ProductsGrid.ItemsSource = table.DefaultView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке данных таблицы '{tableName}':\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProductsGrid_AutoGeneratingColumn(object sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName == RowIdColumnName)
        {
            e.Cancel = true; // скрываем служебный столбец с rowid
        }
    }

    private async void ProductsGrid_RowEditEnding(object sender, System.Windows.Controls.DataGridRowEditEndingEventArgs e)
    {
        if (e.EditAction != System.Windows.Controls.DataGridEditAction.Commit)
            return;

        if (_currentTable is null || string.IsNullOrWhiteSpace(_currentTableName))
            return;

        // После завершения редактирования коммитим изменения и сохраняем строку в базу
        await Dispatcher.InvokeAsync(async () =>
        {
            if (e.Row.Item is not System.Data.DataRowView rowView)
                return;

            var row = rowView.Row;
            if (!_currentTable.Columns.Contains(RowIdColumnName))
                return;

            var rowId = row[RowIdColumnName];
            if (rowId == DBNull.Value)
                return;

            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();

                var safeName = _currentTableName.Replace("\"", string.Empty);

                // формируем список изменённых колонок (кроме rowid)
                var setClauses = new List<string>();
                cmd.Parameters.AddWithValue("@rowid", rowId);

                foreach (DataColumn col in _currentTable.Columns)
                {
                    if (col.ColumnName == RowIdColumnName)
                        continue;

                    var current = row[col, DataRowVersion.Current];
                    object? original = row.HasVersion(DataRowVersion.Original)
                        ? row[col, DataRowVersion.Original]
                        : current;

                    bool changed = (original == DBNull.Value && current != DBNull.Value)
                                   || (original != DBNull.Value && current == DBNull.Value)
                                   || (!Equals(original, current) && original is not DBNull && current is not DBNull);

                    if (!changed)
                        continue;

                    var paramName = "@p_" + col.ColumnName;
                    setClauses.Add($"\"{col.ColumnName}\" = {paramName}");
                    cmd.Parameters.AddWithValue(paramName, current ?? DBNull.Value);
                }

                if (setClauses.Count == 0)
                    return; // нечего сохранять

                cmd.CommandText = $"UPDATE \"{safeName}\" SET {string.Join(", ", setClauses)} WHERE rowid = @rowid";

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }
}