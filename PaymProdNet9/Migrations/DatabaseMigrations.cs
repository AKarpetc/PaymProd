using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace PaymProdNet9.Migrations;

/// <summary>
/// Интерфейс одной миграции базы данных.
/// </summary>
public interface IDatabaseMigration
{
    /// <summary>
    /// Версия миграции (должна быть уникальна и возрастать).
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Короткое имя/описание миграции.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Применить миграцию к уже открытой базе.
    /// </summary>
    void Apply(SqliteConnection connection);
}

/// <summary>
/// Запуск всех зарегистрированных миграций.
/// </summary>
public static class MigrationRunner
{
    private const string MigrationsTableName = "Migrations";

    /// <summary>
    /// Запускает все ещё не применённые миграции.
    /// </summary>
    public static void RunAllMigrations(SqliteConnection connection)
    {
        EnsureMigrationsTable(connection);
        var appliedVersions = GetAppliedVersions(connection);

        // Список миграций приложения. Важно: порядок по Version.
        var migrations = new IDatabaseMigration[]
        {
            new AddAutoAddToDelicatesMigration(),
            new AddHideInMenuFlagsMigration()
        }.OrderBy(m => m.Version);

        foreach (var migration in migrations)
        {
            if (appliedVersions.Contains(migration.Version))
                continue;

            Services.Logger.Info($"Запуск миграции #{migration.Version}: {migration.Name}");

            migration.Apply(connection);
            MarkAsApplied(connection, migration);
        }
    }

    private static void EnsureMigrationsTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {MigrationsTableName} (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Version INTEGER NOT NULL,
                Name TEXT NOT NULL,
                AppliedAt TEXT NOT NULL,
                UNIQUE(Version)
            );";
        command.ExecuteNonQuery();
    }

    private static HashSet<int> GetAppliedVersions(SqliteConnection connection)
    {
        var result = new HashSet<int>();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Version FROM {MigrationsTableName}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(reader.GetInt32(0));
        }

        return result;
    }

    private static void MarkAsApplied(SqliteConnection connection, IDatabaseMigration migration)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"
            INSERT INTO {MigrationsTableName} (Version, Name, AppliedAt)
            VALUES (@version, @name, datetime('now'));";
        command.Parameters.AddWithValue("@version", migration.Version);
        command.Parameters.AddWithValue("@name", migration.Name);

        command.ExecuteNonQuery();
    }
}

/// <summary>
/// Миграция №1: добавление поля AutoAdd в таблицу Delicates
/// для поддержки старых баз, где этого поля нет.
/// </summary>
internal sealed class AddAutoAddToDelicatesMigration : IDatabaseMigration
{
    public int Version => 1;
    public string Name => "Add AutoAdd column to Delicates";

    public void Apply(SqliteConnection connection)
    {
        // Проверяем, есть ли уже колонка AutoAdd в Delicates
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "PRAGMA table_info(Delicates)";

            using var reader = checkCmd.ExecuteReader();
            var hasAutoAdd = false;

            while (reader.Read())
            {
                // PRAGMA table_info: 0 = cid, 1 = name, 2 = type, ...
                var columnName = reader.GetString(1);
                if (string.Equals(columnName, "AutoAdd", StringComparison.OrdinalIgnoreCase))
                {
                    hasAutoAdd = true;
                    break;
                }
            }

            if (hasAutoAdd)
            {
                Services.Logger.Debug("Миграция AddAutoAddToDelicates: колонка AutoAdd уже существует, пропускаем ALTER TABLE.");
                return;
            }
        }

        // Добавляем колонку AutoAdd, если её нет
        using (var alterCmd = connection.CreateCommand())
        {
            alterCmd.CommandText = "ALTER TABLE Delicates ADD COLUMN AutoAdd INTEGER DEFAULT 0";
            alterCmd.ExecuteNonQuery();
        }

        Services.Logger.Info("Миграция AddAutoAddToDelicates: колонка AutoAdd успешно добавлена в таблицу Delicates.");
    }
}

/// <summary>
/// Миграция №2: добавление флага HideInMenu для типов продуктов, продуктов и блюд.
/// </summary>
internal sealed class AddHideInMenuFlagsMigration : IDatabaseMigration
{
    public int Version => 2;
    public string Name => "Add HideInMenu flags to Produkt_Type, Producrs, Delicates";

    public void Apply(SqliteConnection connection)
    {
        AddColumnIfNotExists(connection, "Produkt_Type", "HideInMenu", "INTEGER DEFAULT 0");
        AddColumnIfNotExists(connection, "Producrs", "HideInMenu", "INTEGER DEFAULT 0");
        AddColumnIfNotExists(connection, "Delicates", "HideInMenu", "INTEGER DEFAULT 0");
    }

    private static void AddColumnIfNotExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = $"PRAGMA table_info({tableName})";

            using var reader = checkCmd.ExecuteReader();
            var hasColumn = false;

            while (reader.Read())
            {
                var existingName = reader.GetString(1);
                if (string.Equals(existingName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }

            if (hasColumn)
            {
                Services.Logger.Debug($"Миграция AddHideInMenuFlags: колонка {columnName} уже существует в {tableName}, пропускаем.");
                return;
            }
        }

        using (var alterCmd = connection.CreateCommand())
        {
            alterCmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
            alterCmd.ExecuteNonQuery();
        }

        Services.Logger.Info($"Миграция AddHideInMenuFlags: колонка {columnName} добавлена в таблицу {tableName}.");
    }
}

