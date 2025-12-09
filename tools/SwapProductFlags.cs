using System;
using Microsoft.Data.Sqlite;
using PaymProdNet9.Data;
using PaymProdNet9.Services;

namespace Tools;

/// <summary>
/// Скрипт для перестановки значений флагов Priz_menu и Avtomat в таблице Producrs.
/// Используется для исправления результата миграции, когда галочки "В блюда" и "Авто в отчет" перепутались местами.
/// </summary>
public static class SwapProductFlags
{
    public static void Run()
    {
        try
        {
            Logger.Info("Начало одноразового скрипта SwapProductFlags (перестановка Priz_menu и Avtomat)...");

            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            // Логируем количество записей до изменения (для контроля)
            var countCmd = connection.CreateCommand();
            countCmd.Transaction = transaction;
            countCmd.CommandText = "SELECT COUNT(*) FROM Producrs";
            var total = Convert.ToInt32(countCmd.ExecuteScalar());
            Logger.Info($"В таблице Producrs записей: {total}");

            // Само обновление: меняем местами значения Priz_menu и Avtomat
            var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = transaction;
            updateCmd.CommandText = @"
                UPDATE Producrs
                SET
                    Avtomat = Priz_menu,
                    Priz_menu = Avtomat;
            ";

            var affected = updateCmd.ExecuteNonQuery();
            Logger.Info($"SwapProductFlags: обновлено строк Producrs: {affected}");

            transaction.Commit();
            Logger.Info("SwapProductFlags: транзакция успешно зафиксирована.");
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при выполнении SwapProductFlags", ex);
            throw;
        }
    }
}


