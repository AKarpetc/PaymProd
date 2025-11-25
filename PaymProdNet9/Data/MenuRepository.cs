using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static PaymProdNet9.Data.DatabaseCommandExtensions;

namespace PaymProdNet9.Data;

/// <summary>
/// Репозиторий для работы с меню
/// </summary>
public class MenuRepository
{
    /// <summary>
    /// Получить все меню
    /// </summary>
    public List<Menus> GetAllMenus()
    {
        var menus = new List<Menus>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Count_people, Dateban, Deteils FROM Menus ORDER BY Datew DESC";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            menus.Add(new Menus
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                CountP = reader.GetInt32(2),
                DateBan = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Detail = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
            });

        return menus;
    }

    /// <summary>
    /// Получить открытое меню
    /// </summary>
    public Menus? GetOpenMenu()
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Count_people, Dateban, Deteils FROM Menus WHERE Isopen = 1 LIMIT 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
            return new Menus
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                CountP = reader.GetInt32(2),
                DateBan = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Detail = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
            };

        return null;
    }

    /// <summary>
    /// Создать новое меню
    /// </summary>
    public int CreateMenu(string name, int countPeople, string details, string dateBan)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Menus (Name, Count_people, Deteils, Datew, Isopen, Dateban) 
            VALUES (@name, @count, @details, datetime('now'), 1, @dateban);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@count", countPeople);
        command.Parameters.AddWithValue("@details", details);
        command.Parameters.AddWithValue("@dateban", dateBan);

        var menuId = Convert.ToInt32(command.ExecuteScalar());

        // Автоматически добавляем продукты с Avtomat = 1 и Priz_menu = 1
        command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Prod_ID, COALESCE(Count, 0)
            FROM Producrs
            WHERE Avtomat = 1 AND Priz_menu = 1";

        using var reader = command.ExecuteReader();
        var productRepository = new ProductRepository();
        
        // Отключаем проверку внешних ключей для продуктов с отрицательным ID (один раз для всех продуктов)
        var pragmaOffCommand = connection.CreateCommand();
        pragmaOffCommand.CommandText = "PRAGMA foreign_keys = OFF";
        pragmaOffCommand.ExecuteNonQueryWithLog();
        
        try
        {
            while (reader.Read())
            {
                var productId = reader.GetInt32(0);
                var productCount = reader.GetDecimal(1);
                
                // Добавляем продукт в меню как блюдо (используем отрицательный ID)
                // Для продуктов с Isdiap = 1 используем общее количество на банкет
                command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Isdiap, COALESCE(Count, 0)
                    FROM Producrs
                    WHERE Prod_ID = @productId";
                command.Parameters.AddWithValue("@productId", productId);
                
                bool isdiap = false;
                decimal totalCount = productCount;
                
                using (var productReader = command.ExecuteReader())
                {
                    if (productReader.Read())
                    {
                        isdiap = productReader.GetInt32(0) == 1;
                        if (isdiap)
                        {
                            // Если Isdiap = 1, используем Count из продукта или умножаем на количество людей
                            var productCountValue = productReader.GetDecimal(1);
                            totalCount = productCountValue > 0 ? productCountValue : productCount * countPeople;
                        }
                        else
                        {
                            // Если Isdiap = 0, используем Count из продукта или количество людей
                            totalCount = productCount > 0 ? productCount : countPeople;
                        }
                    }
                }
                
                // Добавляем продукт в Menu_Delicates с отрицательным ID
                command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Menu_Delicates (Id_men, Id_delic, Delcount) 
                    VALUES (@menuId, @delicateId, @count)";
                command.Parameters.AddWithValue("@menuId", menuId);
                command.Parameters.AddWithValue("@delicateId", -productId); // Отрицательный ID для продукта
                command.Parameters.AddWithValue("@count", (int)totalCount);
                command.ExecuteNonQueryWithLog();
                
                // Копируем цену продукта в меню (используя существующее соединение)
                CopyProductPriceToMenuInternal(connection, (SqliteTransaction?)null, menuId, productId);
                
                // Если Isdiap = 1, добавляем в Components1 с общим количеством
                if (isdiap)
                {
                    command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Components1 (Idmen, Delic_id, ProductID, Ves, Detail)
                        VALUES (@menuId, @delicateId, @productId, @ves, NULL)";
                    command.Parameters.AddWithValue("@menuId", menuId);
                    command.Parameters.AddWithValue("@delicateId", -productId);
                    command.Parameters.AddWithValue("@productId", productId);
                    command.Parameters.AddWithValue("@ves", (double)totalCount);
                    command.ExecuteNonQueryWithLog();
                }
                
                Logger.Debug($"Автоматически добавлен продукт в меню: ProductID={productId}, Count={totalCount}, Isdiap={isdiap}");
            }
        }
        finally
        {
            // Включаем обратно проверку внешних ключей
            var pragmaOnCommand = connection.CreateCommand();
            pragmaOnCommand.CommandText = "PRAGMA foreign_keys = ON";
            pragmaOnCommand.ExecuteNonQueryWithLog();
        }

        return menuId;
    }

    /// <summary>
    /// Обновить меню
    /// </summary>
    public void UpdateMenu(int id, string name, int countPeople, string details, string dateBan)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Menus 
            SET Name = @name, Count_people = @count, Deteils = @details, Dateban = @dateban 
            WHERE Id = @id";

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@count", countPeople);
        command.Parameters.AddWithValue("@details", details);
        command.Parameters.AddWithValue("@dateban", dateBan);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Закрыть меню
    /// </summary>
    public void CloseMenu(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Menus SET Isopen = 0 WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Открыть меню
    /// </summary>
    public void OpenMenu(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Закрываем все другие меню
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Menus SET Isopen = 0";
        command.ExecuteNonQuery();

        // Открываем выбранное
        command.CommandText = "UPDATE Menus SET Isopen = 1 WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить меню
    /// </summary>
    public void DeleteMenu(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Components1 WHERE Idmen = @id;
            DELETE FROM Menu_Delicates WHERE Id_men = @id;
            DELETE FROM Menus WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Получить блюда меню
    /// </summary>
    public ObservableCollection<MenuDel_act> GetMenuDelicates(int menuId)
    {
        var menuDelicates = new ObservableCollection<MenuDel_act>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Получаем блюда меню (включая продукты с отрицательным ID)
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT md.Id, md.Id_men, md.Id_delic, md.Delcount,
                   CASE 
                       WHEN md.Id_delic < 0 THEN p.Name
                       ELSE d.Del_Name
                   END as Del_Name
            FROM Menu_Delicates md
            LEFT JOIN Delicates d ON d.Del_id = md.Id_delic AND md.Id_delic > 0
            LEFT JOIN Producrs p ON p.Prod_ID = -md.Id_delic AND md.Id_delic < 0
            WHERE md.Id_men = @menuId";

        command.Parameters.AddWithValue("@menuId", menuId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var delId = reader.GetInt32(2);
            var delName = reader.GetString(4);
            bool isProduct = delId < 0;

            List<Components> components;
            bool isModified = false;

            if (isProduct)
            {
                // Это продукт (отрицательный ID) - получаем из Components1 если есть
                components = GetProductComponents(connection, menuId, -delId);
                isModified = components.Count > 0; // Если есть в Components1, значит изменен
            }
            else
            {
                // Это обычное блюдо
                var customComponents = GetCustomComponents(connection, menuId, delId);
                var standardComponents = GetStandardComponents(connection, delId);

                // Определяем, изменено ли блюдо
                if (customComponents.Count > 0)
                {
                    // Если есть измененные компоненты, сравниваем со справочником
                    isModified = !AreComponentsEqual(customComponents, standardComponents);

                    // Если компоненты совпадают со справочником, удаляем записи из Components1
                    if (!isModified)
                    {
                        var deleteCommand = connection.CreateCommand();
                        deleteCommand.CommandText = @"
                            DELETE FROM Components1 
                            WHERE Idmen = @menuId AND Delic_id = @delicateId";
                        deleteCommand.Parameters.AddWithValue("@menuId", menuId);
                        deleteCommand.Parameters.AddWithValue("@delicateId", delId);
                        deleteCommand.ExecuteNonQuery();
                    }
                }

                // Используем измененные компоненты, если они есть, иначе стандартные
                components = customComponents.Count > 0 ? customComponents : standardComponents;
            }

            var menuDel = new MenuDel_act
            {
                Idmen = reader.GetInt32(0),
                Del_id = delId,
                Countpor = reader.GetInt32(3),
                Del = delName,
                Lcomp = components,
                IsModified = isModified
            };

            // Формируем состав
            if (components.Count > 0)
                menuDel.Sost = string.Join(", ", components.Select(c => c.NameT));
            else if (isProduct)
                menuDel.Sost = "Продукт"; // Для продуктов без компонентов

            menuDelicates.Add(menuDel);
        }

        return menuDelicates;
    }

    /// <summary>
    /// Получить все компоненты
    /// </summary>
    private List<Components> GetAllComponents(SqliteConnection connection, int menuId)
    {
        var components = new List<Components>();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c.Comp_Id, c.Delic_id, c.ProductID, 
                   CASE WHEN COALESCE(TRIM(c.Detail), '') = '' THEN p.Name 
                        ELSE p.Name || '(' || c.Detail || ')' END AS Name,
                   c.Ves, m.Name_Mera, pt.Type_Opis,
                   CASE WHEN p.Fass = 0 THEN COALESCE(m.Fass_Def, 1) 
                        ELSE COALESCE(COALESCE(p.Fass, m.Fass_Def), 1) END as Fass,
                   COALESCE(CASE WHEN p.Izmer = p.Ves THEN m.Fass_Izmer 
                            ELSE (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer) END, 
                           (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer)) as FassIzmer,
                   CASE WHEN p.Izmer != p.Ves AND p.Izmer IS NOT NULL THEN 1 ELSE 0 END as Flag,
                   p.Name as ProdName
            FROM Components c
            INNER JOIN Producrs p ON p.Prod_ID = c.ProductID
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            INNER JOIN Mera m ON m.Mera_ID = p.Ves";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            components.Add(new Components
            {
                Id = reader.GetInt32(0),
                Delid = reader.GetInt32(1),
                Prodid = reader.GetInt32(2),
                NameT = reader.GetString(3),
                Ves = reader.GetDecimal(4),
                Mera = reader.GetString(5),
                Type = reader.GetString(6),
                Fass = reader.GetDecimal(7),
                FassIz = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                Flag = reader.GetInt32(9),
                Name = reader.GetString(10)
            });

        return components;
    }

    /// <summary>
    /// Добавить блюдо в меню
    /// </summary>
    public void AddDelicateToMenu(int menuId, int delicateId, int count)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        // Если delicateId отрицательный, это продукт (Priz_menu = 1)
        bool isProduct = delicateId < 0;
        int actualId = isProduct ? -delicateId : delicateId;

        // Для продуктов с отрицательным ID нужно временно отключить проверку внешних ключей
        // так как отрицательные ID не существуют в таблице Delicates
        // PRAGMA foreign_keys работает на уровне соединения, поэтому отключаем ДО транзакции
        if (isProduct)
        {
            Logger.Debug($"Отключение проверки внешних ключей для продукта с ID={actualId}");
            var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA foreign_keys = OFF";
            pragmaCommand.ExecuteNonQueryWithLog();
            
            // Проверяем, что foreign_keys действительно отключены
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "PRAGMA foreign_keys";
            var fkStatus = checkCommand.ExecuteScalarWithLog();
            Logger.Debug($"Статус foreign_keys после отключения: {fkStatus}");
        }
        else
        {
            // Для обычных блюд проверяем, что блюдо существует
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Delicates WHERE Del_id = @delicateId";
            checkCommand.Parameters.AddWithValue("@delicateId", delicateId);
            var exists = Convert.ToInt32(checkCommand.ExecuteScalarWithLog()) > 0;
            
            if (!exists)
            {
                Logger.Error($"Попытка добавить несуществующее блюдо: DelicateId={delicateId}");
                throw new InvalidOperationException($"Блюдо с ID {delicateId} не найдено в базе данных");
            }
        }

        try
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO Menu_Delicates (Id_men, Id_delic, Delcount) 
                    VALUES (@menuId, @delicateId, @count)";

                command.Parameters.AddWithValue("@menuId", menuId);
                command.Parameters.AddWithValue("@delicateId", delicateId); // Сохраняем отрицательный ID для продуктов
                command.Parameters.AddWithValue("@count", count);

                command.ExecuteNonQueryWithLog();
            
                // Получаем количество людей в меню
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "SELECT Count_people FROM Menus WHERE Id = @menuId";
                command.Parameters.AddWithValue("@menuId", menuId);
                var countPeople = Convert.ToInt32(command.ExecuteScalarWithLog());
            
            var productRepository = new ProductRepository();
            
            if (isProduct)
            {
                // Это продукт с Priz_menu = 1
                // Копируем цену продукта в меню (используя существующее соединение)
                CopyProductPriceToMenuInternal(connection, transaction, menuId, actualId);
                
                // Проверяем Isdiap для продукта
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    SELECT Isdiap, COALESCE(Count, 0) 
                    FROM Producrs 
                    WHERE Prod_ID = @productId";
                command.Parameters.AddWithValue("@productId", actualId);
                
                bool isdiap = false;
                decimal productCount = 0;
                
                using (var reader = command.ExecuteReaderWithLog())
                {
                    if (reader.Read())
                    {
                        isdiap = reader.GetInt32(0) == 1;
                        productCount = reader.GetDecimal(1);
                    }
                }
                
                // Если Isdiap = 1, добавляем в Components1 с общим количеством на банкет
                if (isdiap)
                {
                    decimal totalVes = productCount > 0 ? productCount : count * countPeople;
                    
                    command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT OR REPLACE INTO Components1 (Idmen, Delic_id, ProductID, Ves, Detail)
                        VALUES (@menuId, @delicateId, @productId, @ves, NULL)";
                    command.Parameters.AddWithValue("@menuId", menuId);
                    command.Parameters.AddWithValue("@delicateId", delicateId);
                    command.Parameters.AddWithValue("@productId", actualId);
                    command.Parameters.AddWithValue("@ves", (double)totalVes);
                    
                    command.ExecuteNonQueryWithLog();
                    
                    Logger.Debug($"Добавлен продукт с общим количеством на банкет: ProductID={actualId}, TotalVes={totalVes}, CountPeople={countPeople}");
                }
            }
            else
            {
                // Это обычное блюдо
                // Копируем цены продуктов из справочника в меню
                var components = GetDelicateComponents(connection, delicateId);
                
                // Получаем информацию о продуктах для проверки Isdiap
                var productInfo = new Dictionary<int, (bool Isdiap, decimal Count)>();
                foreach (var component in components)
                {
                    command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        SELECT Isdiap, COALESCE(Count, 0) 
                        FROM Producrs 
                        WHERE Prod_ID = @productId";
                    command.Parameters.AddWithValue("@productId", component.Prodid);
                    
                    using var reader = command.ExecuteReaderWithLog();
                    if (reader.Read())
                    {
                        productInfo[component.Prodid] = (reader.GetInt32(0) == 1, reader.GetDecimal(1));
                    }
                    
                    // Копируем цену продукта в меню (используя существующее соединение)
                    CopyProductPriceToMenuInternal(connection, transaction, menuId, component.Prodid);
                }
                
                // Добавляем продукты с Isdiap = 1 (общее количество на банкет) в Components1
                foreach (var component in components)
                {
                    if (productInfo.ContainsKey(component.Prodid) && productInfo[component.Prodid].Isdiap)
                    {
                        // Получаем текущий вес из Components
                        command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = @"
                            SELECT Ves 
                            FROM Components 
                            WHERE Delic_id = @delicateId AND ProductID = @productId";
                        command.Parameters.AddWithValue("@delicateId", delicateId);
                        command.Parameters.AddWithValue("@productId", component.Prodid);
                        
                        decimal baseVes = 0;
                        using (var reader = command.ExecuteReaderWithLog())
                        {
                            if (reader.Read())
                            {
                                baseVes = reader.GetDecimal(0);
                            }
                        }
                        
                        // Вычисляем общее количество: если указан Count в продукте, используем его, иначе baseVes * countPeople
                        decimal totalVes = productInfo[component.Prodid].Count > 0 
                            ? productInfo[component.Prodid].Count 
                            : baseVes * countPeople;
                        
                        // Добавляем в Components1 с общим количеством
                        command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = @"
                            INSERT OR REPLACE INTO Components1 (Idmen, Delic_id, ProductID, Ves, Detail)
                            VALUES (@menuId, @delicateId, @productId, @ves, NULL)";
                        command.Parameters.AddWithValue("@menuId", menuId);
                        command.Parameters.AddWithValue("@delicateId", delicateId);
                        command.Parameters.AddWithValue("@productId", component.Prodid);
                        command.Parameters.AddWithValue("@ves", (double)totalVes);
                        
                        command.ExecuteNonQueryWithLog();
                        
                        Logger.Debug($"Добавлен продукт с общим количеством на банкет: ProductID={component.Prodid}, TotalVes={totalVes}, CountPeople={countPeople}");
                    }
                }
            }
            
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при добавлении блюда в меню", ex);
            throw;
        }
        finally
        {
            // Включаем обратно проверку внешних ключей (если была отключена)
            if (isProduct)
            {
                try
                {
                    var pragmaCommand = connection.CreateCommand();
                    pragmaCommand.CommandText = "PRAGMA foreign_keys = ON";
                    pragmaCommand.ExecuteNonQueryWithLog();
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при включении проверки внешних ключей", ex);
                }
            }
        }
    }
    
    /// <summary>
    /// Внутренний метод для копирования цены продукта в меню (использует существующее соединение)
    /// </summary>
    private void CopyProductPriceToMenuInternal(SqliteConnection connection, SqliteTransaction? transaction, int menuId, int productId)
    {
        // Получаем цену продукта из справочника
        var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = transaction;
        selectCommand.CommandText = "SELECT COALESCE(Price, 0) FROM Producrs WHERE Prod_ID = @id";
        selectCommand.Parameters.AddWithValue("@id", productId);

        var priceResult = selectCommand.ExecuteScalarWithLog();
        if (priceResult == null || priceResult == DBNull.Value)
        {
            Logger.Warning($"Не удалось скопировать цену: продукт {productId} отсутствует или не содержит цены. Запись в Menu_Product_Prices пропущена.");
            return;
        }

        var price = Convert.ToDouble(priceResult);

        // Сохраняем цену в меню, если её ещё нет
        var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText = @"
            INSERT OR IGNORE INTO Menu_Product_Prices (Id_men, ProductID, Price)
            VALUES (@menuId, @productId, @price)";
        insertCommand.Parameters.AddWithValue("@menuId", menuId);
        insertCommand.Parameters.AddWithValue("@productId", productId);
        insertCommand.Parameters.AddWithValue("@price", price);

        insertCommand.ExecuteNonQueryWithLog();
    }
    
    /// <summary>
    /// Получить компоненты блюда из справочника
    /// </summary>
    private List<Components> GetDelicateComponents(SqliteConnection connection, int delicateId)
    {
        var components = new List<Components>();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT ProductID 
            FROM Components 
            WHERE Delic_id = @delicateId";
        command.Parameters.AddWithValue("@delicateId", delicateId);
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            components.Add(new Components
            {
                Prodid = reader.GetInt32(0)
            });
        }
        
        return components;
    }

    /// <summary>
    /// Удалить блюдо из меню
    /// </summary>
    public void RemoveDelicateFromMenu(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Menu_Delicates WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Сохранить изменения в меню
    /// </summary>
    public void SaveMenuChanges(int menuId, ObservableCollection<MenuDel_act> menuDelicates)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            // Удаляем старые измененные компоненты
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Components1 WHERE Idmen = @menuId";
            command.Parameters.AddWithValue("@menuId", menuId);
            command.ExecuteNonQuery();

            foreach (var menuDel in menuDelicates)
            {
                // Обновляем количество порций
                command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Menu_Delicates 
                    SET Delcount = @count 
                    WHERE Id_delic = @delicateId AND Id_men = @menuId";
                command.Parameters.AddWithValue("@count", menuDel.Countpor);
                command.Parameters.AddWithValue("@delicateId", menuDel.Del_id);
                command.Parameters.AddWithValue("@menuId", menuId);
                command.ExecuteNonQuery();

                // Сохраняем измененные компоненты
                foreach (var component in menuDel.Lcomp)
                {
                    command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Components1 (Delic_id, Ves, ProductID, Idmen) 
                        VALUES (@delicateId, @ves, @productId, @menuId)";
                    command.Parameters.AddWithValue("@delicateId", menuDel.Del_id);
                    command.Parameters.AddWithValue("@ves", component.Ves);
                    command.Parameters.AddWithValue("@productId", component.Prodid);
                    command.Parameters.AddWithValue("@menuId", menuId);
                    command.ExecuteNonQuery();
                }
            }

            // Отмечаем меню как измененное
            command = connection.CreateCommand();
            command.CommandText = "UPDATE Menus SET Ifchan = 1 WHERE Id = @menuId";
            command.Parameters.AddWithValue("@menuId", menuId);
            command.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Получить компоненты блюда для конкретного меню (сначала из Components1, если нет - из Components)
    /// </summary>
    public List<Components> GetMenuDelicateComponents(int menuId, int delicateId)
    {
        var components = new List<Components>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Сначала проверяем, есть ли измененные компоненты в Components1
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c1.Comp_Id, c1.Delic_id, c1.ProductID, c1.Ves, 
                   p.Name as ProdName, 
                   COALESCE(mBase.Name_Mera, '') as MeraName,
                   COALESCE(mBase.MenuRoundingPrecision, 2) as MenuPrecision,
                   CASE WHEN p.Fass = 0 THEN COALESCE(mPack.Fass_Def, COALESCE(mBase.Fass_Def, 1), 1) 
                        ELSE COALESCE(p.Fass, mPack.Fass_Def, COALESCE(mBase.Fass_Def, 1), 1) END as Fass,
                   COALESCE(
                       CASE WHEN p.Izmer = p.Ves THEN mBase.Fass_Izmer 
                            ELSE COALESCE(mPack.Name_Mera, mBase.Fass_Izmer) END,
                       mBase.Fass_Izmer,
                       ''
                   ) as FassIzmer,
                   pt.Type_Opis
            FROM Components1 c1
            INNER JOIN Producrs p ON p.Prod_ID = c1.ProductID
            LEFT JOIN Mera mBase ON mBase.Mera_ID = p.Ves
            LEFT JOIN Mera mPack ON mPack.Mera_ID = p.Izmer
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            WHERE c1.Idmen = @menuId AND c1.Delic_id = @delicateId";

        command.Parameters.AddWithValue("@menuId", menuId);
        command.Parameters.AddWithValue("@delicateId", delicateId);

        using var reader = command.ExecuteReader();
        var hasCustomComponents = reader.HasRows;

        if (hasCustomComponents)
        {
            // Используем измененные компоненты
            while (reader.Read())
                components.Add(new Components
                {
                    Id = reader.GetInt32(0),
                    Delid = reader.GetInt32(1),
                    Prodid = reader.GetInt32(2),
                    Ves = reader.GetDecimal(3),
                    NameT = reader.GetString(4),
                    Mera = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Fass = reader.GetDecimal(6),
                    FassIz = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Type = reader.GetString(8)
                });
        }
        else
        {
            // Используем стандартные компоненты из справочника
            command = connection.CreateCommand();
            command.CommandText = @"
                SELECT c.Comp_Id, c.Delic_id, c.ProductID, c.Ves, 
                       CASE WHEN COALESCE(TRIM(c.Detail), '') = '' THEN p.Name 
                            ELSE p.Name || '(' || c.Detail || ')' END AS Name,
                       COALESCE(m.Name_Mera, '') as MeraName,
                       CASE WHEN p.Fass = 0 THEN COALESCE(m.Fass_Def, 1) 
                            ELSE COALESCE(COALESCE(p.Fass, m.Fass_Def), 1) END as Fass,
                       COALESCE(CASE WHEN p.Izmer = p.Ves THEN m.Fass_Izmer 
                                ELSE (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer) END, 
                               (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer)) as FassIzmer,
                       pt.Type_Opis
                FROM Components c
                INNER JOIN Producrs p ON p.Prod_ID = c.ProductID
                LEFT JOIN Mera m ON m.Mera_ID = p.Izmer
                INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
                WHERE c.Delic_id = @delicateId";

            command.Parameters.AddWithValue("@delicateId", delicateId);

            using var reader2 = command.ExecuteReader();
            while (reader2.Read())
                components.Add(new Components
                {
                    Id = reader2.GetInt32(0),
                    Delid = reader2.GetInt32(1),
                    Prodid = reader2.GetInt32(2),
                    Ves = reader2.GetDecimal(3),
                    NameT = reader2.GetString(4),
                    Mera = reader2.IsDBNull(5) ? "" : reader2.GetString(5),
                    Fass = reader2.GetDecimal(6),
                    FassIz = reader2.IsDBNull(7) ? "" : reader2.GetString(7)
                });
        }

        return components;
    }

    /// <summary>
    /// Получить измененные компоненты из Components1 для конкретного меню
    /// </summary>
    private List<Components> GetCustomComponents(SqliteConnection connection, int menuId, int delicateId)
    {
        var components = new List<Components>();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c1.Comp_Id, c1.Delic_id, c1.ProductID, c1.Ves, 
                   p.Name as ProdName, 
                   COALESCE(m.Name_Mera, '') as MeraName,
                   CASE WHEN p.Fass = 0 THEN COALESCE(m.Fass_Def, 1) 
                        ELSE COALESCE(COALESCE(p.Fass, m.Fass_Def), 1) END as Fass,
                   COALESCE(CASE WHEN p.Izmer = p.Ves THEN m.Fass_Izmer 
                            ELSE (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer) END, 
                           (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer)) as FassIzmer,
                   pt.Type_Opis
            FROM Components1 c1
            INNER JOIN Producrs p ON p.Prod_ID = c1.ProductID
            LEFT JOIN Mera m ON m.Mera_ID = p.Izmer
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            WHERE c1.Idmen = @menuId AND c1.Delic_id = @delicateId";

        command.Parameters.AddWithValue("@menuId", menuId);
        command.Parameters.AddWithValue("@delicateId", delicateId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
            components.Add(new Components
            {
                Id = reader.GetInt32(0),
                Delid = reader.GetInt32(1),
                Prodid = reader.GetInt32(2),
                Ves = reader.GetDecimal(3),
                NameT = reader.GetString(4),
                Mera = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Fass = reader.GetDecimal(6),
                FassIz = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Type = reader.GetString(8)
            });

        return components;
    }

    /// <summary>
    /// Получить компоненты продукта из Components1 (для продуктов с Isdiap)
    /// </summary>
    private List<Components> GetProductComponents(SqliteConnection connection, int menuId, int productId)
    {
        var components = new List<Components>();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c1.Comp_Id, c1.Delic_id, c1.ProductID, c1.Ves, 
                   p.Name as ProdName, 
                   COALESCE(m.Name_Mera, '') as MeraName,
                   COALESCE(m.MenuRoundingPrecision, 2) as MenuRoundingPrecision,
                   CASE WHEN p.Fass = 0 THEN COALESCE(m.Fass_Def, 1) 
                        ELSE COALESCE(COALESCE(p.Fass, m.Fass_Def), 1) END as Fass,
                   COALESCE(CASE WHEN p.Izmer = p.Ves THEN m.Fass_Izmer 
                            ELSE (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer) END, 
                           (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer)) as FassIzmer,
                   pt.Type_Opis
            FROM Components1 c1
            INNER JOIN Producrs p ON p.Prod_ID = c1.ProductID
            LEFT JOIN Mera m ON m.Mera_ID = p.Izmer
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            WHERE c1.Idmen = @menuId AND c1.Delic_id = -@productId AND c1.ProductID = @productId";

        command.Parameters.AddWithValue("@menuId", menuId);
        command.Parameters.AddWithValue("@productId", productId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
            components.Add(new Components
            {
                Id = reader.GetInt32(0),
                Delid = reader.GetInt32(1),
                Prodid = reader.GetInt32(2),
                Ves = reader.GetDecimal(3),
                NameT = reader.GetString(4),
                Mera = reader.IsDBNull(5) ? "" : reader.GetString(5),
                MenuRoundingPrecision = reader.GetInt32(6),
                Fass = reader.GetDecimal(7),
                FassIz = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Type = reader.GetString(9)
            });

        return components;
    }

    /// <summary>
    /// Получить стандартные компоненты из Components (справочник)
    /// </summary>
    private List<Components> GetStandardComponents(SqliteConnection connection, int delicateId)
    {
        var components = new List<Components>();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c.Comp_Id, c.Delic_id, c.ProductID, c.Ves, 
                   CASE WHEN COALESCE(TRIM(c.Detail), '') = '' THEN p.Name 
                        ELSE p.Name || '(' || c.Detail || ')' END AS Name,
                   COALESCE(m.Name_Mera, '') as MeraName,
                   CASE WHEN p.Fass = 0 THEN COALESCE(m.Fass_Def, 1) 
                        ELSE COALESCE(COALESCE(p.Fass, m.Fass_Def), 1) END as Fass,
                   COALESCE(CASE WHEN p.Izmer = p.Ves THEN m.Fass_Izmer 
                            ELSE (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer) END, 
                           (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer)) as FassIzmer,
                   pt.Type_Opis
            FROM Components c
            INNER JOIN Producrs p ON p.Prod_ID = c.ProductID
            LEFT JOIN Mera m ON m.Mera_ID = p.Izmer
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            WHERE c.Delic_id = @delicateId";

        command.Parameters.AddWithValue("@delicateId", delicateId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
            components.Add(new Components
            {
                Id = reader.GetInt32(0),
                Delid = reader.GetInt32(1),
                Prodid = reader.GetInt32(2),
                Ves = reader.GetDecimal(3),
                NameT = reader.GetString(4),
                Mera = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Fass = reader.GetDecimal(6),
                FassIz = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Type = reader.GetString(8)
            });

        return components;
    }

    /// <summary>
    /// Сравнить два списка компонентов (продукты и вес должны совпадать)
    /// </summary>
    private bool AreComponentsEqual(List<Components> list1, List<Components> list2)
    {
        if (list1.Count != list2.Count)
            return false;

        // Сортируем по ProductID для сравнения
        var sorted1 = list1.OrderBy(c => c.Prodid).ThenBy(c => c.Ves).ToList();
        var sorted2 = list2.OrderBy(c => c.Prodid).ThenBy(c => c.Ves).ToList();

        for (var i = 0; i < sorted1.Count; i++)
        {
            if (sorted1[i].Prodid != sorted2[i].Prodid)
                return false;

            // Сравниваем вес с точностью до 2 знаков после запятой
            var weightDiff = sorted1[i].Ves - sorted2[i].Ves;
            if (weightDiff < 0) weightDiff = -weightDiff;
            if (weightDiff > 0.01m)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Сохранить измененные компоненты блюда для конкретного меню в Components1
    /// </summary>
    public void SaveMenuDelicateComponents(int menuId, int delicateId, List<Components> components)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Удаляем старые измененные компоненты для этого блюда в этом меню
            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM Components1 WHERE Idmen = @menuId AND Delic_id = @delicateId";
            deleteCommand.Parameters.AddWithValue("@menuId", menuId);
            deleteCommand.Parameters.AddWithValue("@delicateId", delicateId);
            deleteCommand.ExecuteNonQuery();

            // Добавляем новые компоненты
            foreach (var component in components)
                if (component.Ves > 0)
                {
                    var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO Components1 (Delic_id, ProductID, Ves, Idmen) 
                        VALUES (@delicateId, @productId, @ves, @menuId)";
                    insertCommand.Parameters.AddWithValue("@delicateId", delicateId);
                    insertCommand.Parameters.AddWithValue("@productId", component.Prodid);
                    insertCommand.Parameters.AddWithValue("@ves", component.Ves);
                    insertCommand.Parameters.AddWithValue("@menuId", menuId);
                    insertCommand.ExecuteNonQuery();
                }

            // Проверяем, совпадают ли сохраненные компоненты со справочником
            var savedComponents = GetCustomComponents(connection, menuId, delicateId);
            var standardComponents = GetStandardComponents(connection, delicateId);

            var isModified = !AreComponentsEqual(savedComponents, standardComponents);

            // Если компоненты совпадают со справочником, удаляем их из Components1
            if (!isModified && savedComponents.Count > 0)
            {
                var deleteCommand2 = connection.CreateCommand();
                deleteCommand2.CommandText = "DELETE FROM Components1 WHERE Idmen = @menuId AND Delic_id = @delicateId";
                deleteCommand2.Parameters.AddWithValue("@menuId", menuId);
                deleteCommand2.Parameters.AddWithValue("@delicateId", delicateId);
                deleteCommand2.ExecuteNonQuery();
            }

            // Отмечаем меню как измененное только если компоненты действительно отличаются
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE Menus SET Ifchan = @ifchan WHERE Id = @menuId";
            updateCommand.Parameters.AddWithValue("@ifchan", isModified ? 1 : 0);
            updateCommand.Parameters.AddWithValue("@menuId", menuId);
            updateCommand.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}