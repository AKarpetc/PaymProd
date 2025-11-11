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
    /// Получить все блюда
    /// </summary>
    public ObservableCollection<DelicatesColl> GetAllDelicates()
    {
        var delicates = new ObservableCollection<DelicatesColl>();
        
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        // Получаем все компоненты
        var allComponents = GetAllComponents(connection);
        
        // Получаем блюда
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT d.Del_id, d.Del_Name, COALESCE(d.Del_opis, ''), 
                   COALESCE(d.Del_count, 0), COALESCE(d.Del_Ves, 0), 
                   td.Type_del_opis, td.Type_Del_ID
            FROM Delicates d
            INNER JOIN Type_Del td ON td.Type_Del_ID = d.Del_Type
            WHERE d.Del_Type != -1
            ORDER BY td.Type_del_opis, d.Del_Name";
        
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
                IDType = reader.GetInt32(6),
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
        {
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
        }
        
        return components;
    }

    /// <summary>
    /// Получить блюдо по ID
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
                   td.Type_del_opis, td.Type_Del_ID
            FROM Delicates d
            INNER JOIN Type_Del td ON td.Type_Del_ID = d.Del_Type
            WHERE d.Del_id = @id";
        
        command.Parameters.AddWithValue("@id", id);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new DelicatesColl
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Opis = reader.GetString(2),
                Count = reader.GetDecimal(3),
                Ves = reader.GetDecimal(4),
                Type = reader.GetString(5),
                IDType = reader.GetInt32(6),
                Lcomp = components
            };
        }
        
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
        {
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
        }
        
        return components;
    }

    /// <summary>
    /// Добавить блюдо
    /// </summary>
    public int AddDelicate(int typeId, string name, decimal ves, decimal count)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Delicates (Del_Type, Del_Name, Del_Ves, Del_count, Datew) 
            VALUES (@type, @name, @ves, @count, datetime('now'));
            SELECT last_insert_rowid();";
        
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@ves", (double)ves);
        command.Parameters.AddWithValue("@count", (double)count);
        
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Обновить блюдо
    /// </summary>
    public void UpdateDelicate(int id, int typeId, string name, decimal ves, decimal count)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Delicates 
            SET Del_Name = @name, Del_Type = @type, Del_Ves = @ves, Del_count = @count, Datew = datetime('now')
            WHERE Del_id = @id";
        
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@ves", (double)ves);
        command.Parameters.AddWithValue("@count", (double)count);
        
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить блюдо
    /// </summary>
    public void DeleteDelicate(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Components WHERE Delic_id = @id;
            DELETE FROM Components1 WHERE Delic_id = @id;
            DELETE FROM Menu_Delicates WHERE Id_delic = @id;
            DELETE FROM Delicates WHERE Del_id = @id;";
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
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Type_Del_ID, Type_del_opis FROM Type_Del ORDER BY Type_del_opis";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            types.Add(new DelicateType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }
        
        return types;
    }

    /// <summary>
    /// Добавить тип блюда
    /// </summary>
    public int AddDelicateType(string name)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Type_Del (Type_del_opis) VALUES (@name);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", name);
        
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Получить список блюд для меню (доступные для добавления)
    /// </summary>
    public List<DelicatesColl> GetAvailableDelicatesForMenu(string? typeFilter = null)
    {
        var delicates = new List<DelicatesColl>();
        
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var sql = @"
            SELECT d.Del_id, d.Del_Name, COALESCE(d.Del_Ves, 0), 
                   COALESCE(d.Del_count, 0), td.Type_del_opis, td.Type_Del_ID
            FROM Delicates d
            INNER JOIN Type_Del td ON td.Type_Del_ID = d.Del_Type
            WHERE d.Del_Type != -1";
        
        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "%")
        {
            sql += " AND td.Type_del_opis = @type";
        }
        
        sql += " ORDER BY td.Type_del_opis, d.Del_Name";
        
        var command = connection.CreateCommand();
        command.CommandText = sql;
        
        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "%")
        {
            command.Parameters.AddWithValue("@type", typeFilter);
        }
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            delicates.Add(new DelicatesColl
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Ves = reader.GetDecimal(2),
                Count = reader.GetDecimal(3),
                Type = reader.GetString(4),
                IDType = reader.GetInt32(5)
            });
        }
        
        return delicates;
    }
}

