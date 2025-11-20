using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Linq;

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

        return Convert.ToInt32(command.ExecuteScalar());
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

        // Получаем блюда меню
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT md.Id, md.Id_men, md.Id_delic, md.Delcount, d.Del_Name, 
                   COALESCE((SELECT 1 FROM Produkt_Type pt 
                            JOIN Producrs p ON p.Type = pt.TypeProdId 
                            WHERE p.Prod_ID = d.Del_id AND pt.TypeProdId = -1), 0) as ff
            FROM Menu_Delicates md
            INNER JOIN Delicates d ON d.Del_id = md.Id_delic
            WHERE md.Id_men = @menuId";

        command.Parameters.AddWithValue("@menuId", menuId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var delId = reader.GetInt32(2);
            var ff = reader.GetInt32(5);

            // Получаем компоненты из Components1 (измененные) и Components (справочник)
            var customComponents = GetCustomComponents(connection, menuId, delId);
            var standardComponents = GetStandardComponents(connection, delId);

            // Определяем, изменено ли блюдо
            var isModified = false;
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
            // Если customComponents пустой, значит блюдо не изменено (isModified = false)

            // Используем измененные компоненты, если они есть, иначе стандартные
            var components = customComponents.Count > 0 ? customComponents : standardComponents;

            var menuDel = new MenuDel_act
            {
                Idmen = reader.GetInt32(0),
                Del_id = delId,
                Countpor = reader.GetInt32(3),
                Del = reader.GetString(4),
                Lcomp = components,
                IsModified = isModified
            };

            // Формируем состав
            menuDel.Sost = string.Join(", ", menuDel.Lcomp.Select(c => c.NameT));

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

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Menu_Delicates (Id_men, Id_delic, Delcount) 
            VALUES (@menuId, @delicateId, @count)";

        command.Parameters.AddWithValue("@menuId", menuId);
        command.Parameters.AddWithValue("@delicateId", delicateId);
        command.Parameters.AddWithValue("@count", count);

        command.ExecuteNonQuery();
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