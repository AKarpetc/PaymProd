using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;

namespace PaymProdNet9.Data;

/// <summary>
/// Репозиторий для работы с блюдами
/// </summary>
public class DelicateRepository
{
    /// <summary>
    /// Получить все блюда (включая продукты с Priz_menu = 1)
    /// </summary>
    public ObservableCollection<DelicatesColl> GetAllDelicates()
    {
        var delicates = new ObservableCollection<DelicatesColl>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Получаем все компоненты
        var allComponents = GetAllComponents(connection);

        // Получаем блюда (все, включая помеченные как удалённые; фильтрация выполняется на уровне UI)
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT d.Del_id, d.Del_Name, COALESCE(d.Del_opis, ''), 
                   COALESCE(d.Del_count, 0), COALESCE(d.Del_Ves, 0), 
                   COALESCE(td.Type_del_opis, ''), td.Type_Del_ID, COALESCE(td.SortOrder, 0),
                   d.LinkedProductId, COALESCE(d.AutoAdd, 0),
                   COALESCE(d.HideInMenu, 0),
                   COALESCE(d.IsDeleted, 0)
            FROM Delicates d
            LEFT JOIN Type_Del td ON td.Type_Del_ID = d.Del_Type
            ORDER BY COALESCE(td.SortOrder, 0), td.Type_del_opis, d.Del_Name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var delId = reader.GetInt32(0);

            var delicate = new DelicatesColl
            {
                Id = delId,
                Name = reader.GetString(1),
                Opis = reader.GetString(2),
                Count = reader.GetDecimal(3),
                Ves = reader.GetDecimal(4),
                Type = reader.GetString(5),
                IDType = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                TypeSortOrder = reader.GetInt32(7),
                LinkedProductId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                AutoAdd = !reader.IsDBNull(9) && reader.GetInt32(9) == 1,
                HideInMenu = !reader.IsDBNull(10) && reader.GetInt32(10) == 1,
                IsDeleted = !reader.IsDBNull(11) && reader.GetInt32(11) == 1,
                Lcomp = allComponents.Where(c => c.Delid == delId).ToList()
            };

            delicates.Add(delicate);
        }

        return delicates;
    }

    /// <summary>
    /// Получить все компоненты
    /// </summary>
    private List<Components> GetAllComponents(SqliteConnection connection)
    {
        var components = new List<Components>();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c.Comp_Id, c.Delic_id, c.ProductID, 
                   p.Name, c.Ves, m.Name_Mera, pt.Type_Opis,
                   COALESCE(p.Fass, 1), p.Name
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
                Name = reader.GetString(8)
            });

        return components;
    }

    /// <summary>
    /// Получить блюдо по ID (включая продукты с отрицательным ID)
    /// </summary>
    public DelicatesColl? GetDelicateById(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Получаем компоненты блюда
        var components = GetDelicateComponents(connection, id);

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT d.Del_id, d.Del_Name, COALESCE(d.Del_opis, ''), 
                   COALESCE(d.Del_count, 0), COALESCE(d.Del_Ves, 0), 
                   COALESCE(td.Type_del_opis, ''), td.Type_Del_ID, COALESCE(td.SortOrder, 0),
                   d.LinkedProductId, COALESCE(d.AutoAdd, 0),
                   COALESCE(d.HideInMenu, 0),
                   COALESCE(d.IsDeleted, 0)
            FROM Delicates d
            LEFT JOIN Type_Del td ON td.Type_Del_ID = d.Del_Type
            WHERE d.Del_id = @id";

        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
            return new DelicatesColl
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Opis = reader.GetString(2),
                Count = reader.GetDecimal(3),
                Ves = reader.GetDecimal(4),
                Type = reader.GetString(5),
                TypeSortOrder = reader.GetInt32(7),
                IDType = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                LinkedProductId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                AutoAdd = !reader.IsDBNull(9) && reader.GetInt32(9) == 1,
                HideInMenu = !reader.IsDBNull(10) && reader.GetInt32(10) == 1,
                IsDeleted = !reader.IsDBNull(11) && reader.GetInt32(11) == 1,
                Lcomp = components
            };

        return null;
    }

    /// <summary>
    /// Получить компоненты блюда
    /// </summary>
    private List<Components> GetDelicateComponents(SqliteConnection connection, int delicateId)
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
            INNER JOIN Mera m ON m.Mera_ID = p.Ves
            WHERE c.Delic_id = @delicateId";

        command.Parameters.AddWithValue("@delicateId", delicateId);

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
    /// Добавить блюдо
    /// </summary>
    public int AddDelicate(int typeId, string name, decimal ves, decimal count, bool autoAdd, bool hideInMenu = false)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Delicates (Del_Type, Del_Name, Del_Ves, Del_count, Datew, AutoAdd, HideInMenu) 
            VALUES (@type, @name, @ves, @count, datetime('now'), @autoAdd, @hideInMenu);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@ves", (double)ves);
        command.Parameters.AddWithValue("@count", (double)count);
        command.Parameters.AddWithValue("@autoAdd", autoAdd ? 1 : 0);
        command.Parameters.AddWithValue("@hideInMenu", hideInMenu ? 1 : 0);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Обновить блюдо
    /// </summary>
    public void UpdateDelicate(int id, int typeId, string name, decimal ves, decimal count, bool autoAdd, bool hideInMenu = false)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Delicates 
            SET Del_Name = @name, Del_Type = @type, Del_Ves = @ves, Del_count = @count, Datew = datetime('now'),
                AutoAdd = @autoAdd, HideInMenu = @hideInMenu
            WHERE Del_id = @id";

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@ves", (double)ves);
        command.Parameters.AddWithValue("@count", (double)count);
        command.Parameters.AddWithValue("@autoAdd", autoAdd ? 1 : 0);
        command.Parameters.AddWithValue("@hideInMenu", hideInMenu ? 1 : 0);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить блюдо
    /// </summary>
    public void DeleteDelicate(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Мягкое удаление блюда:
        // - само блюдо и его компоненты остаются в базе и во всех меню;
        // - блюдо скрывается из справочников и списков доступных блюд.
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Delicates SET IsDeleted = 1 WHERE Del_id = @id";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Восстановить блюдо (снять пометку об удалении)
    /// </summary>
    public void RestoreDelicate(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Delicates SET IsDeleted = 0 WHERE Del_id = @id";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Добавить компонент в блюдо
    /// </summary>
    public void AddComponent(int delicateId, int productId, decimal ves, string? detail = null)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Components (Delic_id, ProductID, Ves, Detail) 
            VALUES (@delicateId, @productId, @ves, @detail)";

        command.Parameters.AddWithValue("@delicateId", delicateId);
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@ves", (double)ves);
        command.Parameters.AddWithValue("@detail", (object?)detail ?? DBNull.Value);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить компонент
    /// </summary>
    public void DeleteComponent(int componentId)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Components WHERE Comp_Id = @id";
        command.Parameters.AddWithValue("@id", componentId);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить компонент по продукту и блюду
    /// </summary>
    public void DeleteComponentByProductAndDelicate(int productId, int delicateId)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Components WHERE ProductID = @productId AND Delic_id = @delicateId";
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@delicateId", delicateId);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Обновить вес компонента
    /// </summary>
    public void UpdateComponentWeight(int delicateId, int productId, decimal ves)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Components 
            SET Ves = @ves 
            WHERE Delic_id = @delicateId AND ProductID = @productId";

        command.Parameters.AddWithValue("@ves", (double)ves);
        command.Parameters.AddWithValue("@delicateId", delicateId);
        command.Parameters.AddWithValue("@productId", productId);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Получить типы блюд
    /// </summary>
    public List<DelicateType> GetDelicateTypes()
    {
        var types = new List<DelicateType>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Получаем типы блюд
        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Type_Del_ID, Type_del_opis, COALESCE(SortOrder, 0), LinkedProductTypeId, " +
            "COALESCE(IsDeleted, 0) " +
            "FROM Type_Del " +
            "WHERE COALESCE(IsDeleted, 0) = 0 " +
            "ORDER BY COALESCE(SortOrder, 0), Type_del_opis";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            types.Add(new DelicateType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                SortOrder = reader.GetInt32(2),
                LinkedProductTypeId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IsDeleted = !reader.IsDBNull(4) && reader.GetInt32(4) == 1
            });

        return types.OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToList();
    }

    /// <summary>
    /// Добавить тип блюда
    /// </summary>
    public int AddDelicateType(string name, int sortOrder = 0)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Type_Del (Type_del_opis, SortOrder) VALUES (@name, @sortOrder);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Обновить тип блюда
    /// </summary>
    public void UpdateDelicateType(int id, string name, int sortOrder = 0)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Type_Del 
            SET Type_del_opis = @name, SortOrder = @sortOrder
            WHERE Type_Del_ID = @id";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить тип блюда
    /// </summary>
    public bool DeleteDelicateType(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        // Мягкое удаление типа блюда
        command.CommandText = "UPDATE Type_Del SET IsDeleted = 1 WHERE Type_Del_ID = @id";
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Получить список блюд для меню (доступные для добавления, включая продукты с Priz_menu = 1)
    /// </summary>
    public List<DelicatesColl> GetAvailableDelicatesForMenu(string? typeFilter = null)
    {
        var delicates = new List<DelicatesColl>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Получаем обычные блюда (только не удалённые)
        var sql = @"
            SELECT d.Del_id, d.Del_Name, COALESCE(d.Del_Ves, 0), 
                   COALESCE(d.Del_count, 0), td.Type_del_opis, td.Type_Del_ID, COALESCE(td.SortOrder, 0),
                   d.LinkedProductId, COALESCE(d.HideInMenu, 0)
            FROM Delicates d
            INNER JOIN Type_Del td ON td.Type_Del_ID = d.Del_Type
            WHERE d.Del_Type != -1
              AND COALESCE(d.IsDeleted, 0) = 0";

        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "%") sql += " AND td.Type_del_opis = @type";

        sql += " ORDER BY COALESCE(td.SortOrder, 0), td.Type_del_opis, d.Del_Name";

        var command = connection.CreateCommand();
        command.CommandText = sql;

        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "%")
            command.Parameters.AddWithValue("@type", typeFilter);

        using (var reader = command.ExecuteReader())
        {
                while (reader.Read())
                delicates.Add(new DelicatesColl
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Ves = reader.GetDecimal(2),
                    Count = reader.GetDecimal(3),
                    Type = reader.GetString(4),
                    IDType = reader.GetInt32(5),
                    TypeSortOrder = reader.GetInt32(6),
                    LinkedProductId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    HideInMenu = !reader.IsDBNull(8) && reader.GetInt32(8) == 1
                });
        }

        // Подтягиваем состав всех блюд одним запросом
        var delicateIds = delicates.Select(d => d.Id).ToList();
        if (delicateIds.Count > 0)
        {
            var componentsByDelicate = GetComponentsForDelicates(connection, delicateIds);
            foreach (var delicate in delicates)
                if (componentsByDelicate.TryGetValue(delicate.Id, out var components))
                    delicate.Lcomp = components;
        }

        // Подтягиваем информацию о связанных продуктах (для автоподстановки количества)
        var linkedProductIds = delicates.Where(d => d.LinkedProductId.HasValue)
            .Select(d => d.LinkedProductId!.Value)
            .Distinct()
            .ToList();
        if (linkedProductIds.Count > 0)
        {
            var parameterNames = linkedProductIds.Select((_, index) => $"@prod{index}").ToList();
            var productCommand = connection.CreateCommand();
            productCommand.CommandText = $@"
                SELECT Prod_ID, COALESCE(Count, 0) AS CountValue, 
                       COALESCE(Isdiap, 0) AS IsdiapValue, 
                       COALESCE(Priz_menu, 0) AS PrizMenuValue
                FROM Producrs
                WHERE Prod_ID IN ({string.Join(", ", parameterNames)})";

            for (var i = 0; i < linkedProductIds.Count; i++)
                productCommand.Parameters.AddWithValue(parameterNames[i], linkedProductIds[i]);

            var products = new Dictionary<int, (decimal Count, bool Isdiap, bool PrizMenu)>();
            using (var reader = productCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    products[reader.GetInt32(0)] = (reader.GetDecimal(1),
                        reader.GetInt32(2) == 1,
                        reader.GetInt32(3) == 1);
                }
            }

            foreach (var delicate in delicates)
            {
                if (delicate.LinkedProductId.HasValue &&
                    products.TryGetValue(delicate.LinkedProductId.Value, out var info) &&
                    info.Isdiap && info.PrizMenu && info.Count > 0)
                {
                    delicate.LinkedProductDefaultCount = info.Count;
                }
            }
        }

        return delicates;
    }

    /// <summary>
    /// Получить компоненты для набора блюд одним запросом
    /// </summary>
    private Dictionary<int, List<Components>> GetComponentsForDelicates(SqliteConnection connection,
        IReadOnlyCollection<int> delicateIds)
    {
        var result = new Dictionary<int, List<Components>>();
        if (delicateIds == null || delicateIds.Count == 0) return result;

        var distinctIds = delicateIds.Distinct().ToList();
        foreach (var id in distinctIds) result[id] = new List<Components>();

        var parameterNames = distinctIds.Select((_, index) => $"@id{index}").ToList();
        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT c.Delic_id,
                   c.Comp_Id,
                   c.ProductID,
                   CASE WHEN COALESCE(TRIM(c.Detail), '') = '' THEN p.Name 
                        ELSE p.Name || '(' || c.Detail || ')' END AS Name,
                   c.Ves,
                   m.Name_Mera,
                   pt.Type_Opis,
                   CASE WHEN p.Fass = 0 THEN COALESCE(m.Fass_Def, 1) 
                        ELSE COALESCE(COALESCE(p.Fass, m.Fass_Def), 1) END as Fass,
                   COALESCE(
                       CASE WHEN p.Izmer = p.Ves THEN m.Fass_Izmer 
                            ELSE (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer) END, 
                       (SELECT m2.Name_Mera FROM Mera m2 WHERE m2.Mera_ID = p.Izmer)
                   ) as FassIzmer,
                   CASE WHEN p.Izmer != p.Ves AND p.Izmer IS NOT NULL THEN 1 ELSE 0 END as Flag,
                   p.Name as ProdName
            FROM Components c
            INNER JOIN Producrs p ON p.Prod_ID = c.ProductID
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            INNER JOIN Mera m ON m.Mera_ID = p.Ves
            WHERE c.Delic_id IN ({string.Join(",", parameterNames)})
            ORDER BY c.Delic_id, c.Comp_Id";

        for (var i = 0; i < distinctIds.Count; i++)
            command.Parameters.AddWithValue(parameterNames[i], distinctIds[i]);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var delicateId = reader.GetInt32(0);
            if (!result.TryGetValue(delicateId, out var components))
            {
                components = new List<Components>();
                result[delicateId] = components;
            }

            components.Add(new Components
            {
                Id = reader.GetInt32(1),
                Delid = delicateId,
                Prodid = reader.GetInt32(2),
                NameT = reader.GetString(3),
                Ves = reader.GetDecimal(4),
                Mera = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Type = reader.GetString(6),
                Fass = reader.GetDecimal(7),
                FassIz = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                Flag = reader.GetInt32(9),
                Name = reader.GetString(10)
            });
        }

        return result;
    }
}