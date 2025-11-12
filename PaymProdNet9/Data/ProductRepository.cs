using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;

namespace PaymProdNet9.Data;

/// <summary>
/// Репозиторий для работы с продуктами
/// </summary>
public class ProductRepository
{
    /// <summary>
    /// Получить все продукты
    /// </summary>
    public List<ProductView> GetAllProducts()
    {
        var products = new List<ProductView>();
        
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT p.Prod_ID, p.Name, pt.Type_Opis, m.Name_Mera, 
                   pt.TypeProdId, p.Ves, COALESCE(p.Fass, 0), 
                   p.Izmer, mi.Name_Mera, p.Priz_menu, 
                   COALESCE(p.Count, 0), p.Avtomat, p.Chel, p.Isdiap
            FROM Producrs p
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            INNER JOIN Mera m ON m.Mera_ID = p.Ves
            LEFT JOIN Mera mi ON mi.Mera_ID = p.Izmer";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var product = new ProductView
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                Ves = reader.GetString(3),
                TID = reader.GetInt32(4),
                VID = reader.GetInt32(5),
                Fass = reader.GetDecimal(6),
                Iz = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                IzName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                PrizMen = reader.GetInt32(9),
                PrizMen1 = reader.GetInt32(9) == 1,
                Count = reader.GetDecimal(10),
                AutoAdd = reader.GetInt32(11) == 1,
                CountPeople = reader.GetInt32(12),
                MainCount = reader.GetInt32(13) == 1
            };
            
            products.Add(product);
        }
        
        return products;
    }

    /// <summary>
    /// Добавить продукт
    /// </summary>
    public int AddProduct(string name, int? vesId, int typeId, double fass, int izmerId, int prizMenu = 0)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Producrs (Name, Type, Ves, Fass, Izmer, Priz_menu) 
            VALUES (@name, @type, @ves, @fass, @izmer, @prizMenu);
            SELECT last_insert_rowid();";
        
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@ves", vesId.HasValue && vesId.Value > 0 ? (object)vesId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@fass", fass);
        command.Parameters.AddWithValue("@izmer", izmerId);
        command.Parameters.AddWithValue("@prizMenu", prizMenu);
        
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Добавить продукт с автодобавлением
    /// </summary>
    public int AddProductWithAutoAdd(string name, int vesId, int typeId, double fass, int izmerId, 
        int prizMenu, decimal count, int avtomat, int chel, int isdiap)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Producrs (Name, Type, Ves, Fass, Izmer, Priz_menu, Count, Avtomat, Chel, Isdiap) 
            VALUES (@name, @type, @ves, @fass, @izmer, @prizMenu, @count, @avtomat, @chel, @isdiap);
            SELECT last_insert_rowid();";
        
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@ves", vesId);
        command.Parameters.AddWithValue("@fass", fass);
        command.Parameters.AddWithValue("@izmer", izmerId);
        command.Parameters.AddWithValue("@prizMenu", prizMenu);
        command.Parameters.AddWithValue("@count", count);
        command.Parameters.AddWithValue("@avtomat", avtomat);
        command.Parameters.AddWithValue("@chel", chel);
        command.Parameters.AddWithValue("@isdiap", isdiap);
        
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Обновить продукт
    /// </summary>
    public void UpdateProduct(int id, string name, int? vesId, int typeId, decimal fass, int izmerId, 
        int prizMenu, decimal count, bool automat, int countPeople, bool mainCount)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Producrs 
            SET Name = @name, Type = @type, Ves = @ves, Fass = @fass, Izmer = @izmer, 
                Priz_menu = @prizMenu, Count = @count, Avtomat = @avtomat, Chel = @chel, Isdiap = @isdiap
            WHERE Prod_ID = @id";
        
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@ves", vesId.HasValue && vesId.Value > 0 ? (object)vesId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@fass", (double)fass);
        command.Parameters.AddWithValue("@izmer", izmerId);
        command.Parameters.AddWithValue("@prizMenu", prizMenu);
        command.Parameters.AddWithValue("@count", (double)count);
        command.Parameters.AddWithValue("@avtomat", automat ? 1 : 0);
        command.Parameters.AddWithValue("@chel", countPeople);
        command.Parameters.AddWithValue("@isdiap", mainCount ? 1 : 0);
        
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить продукт
    /// </summary>
    public bool DeleteProduct(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        // Проверяем, используется ли продукт в блюдах
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Components WHERE ProductID = @id";
        command.Parameters.AddWithValue("@id", id);
        
        var count = Convert.ToInt32(command.ExecuteScalar());
        if (count > 0)
        {
            return false; // Продукт используется
        }
        
        // Удаляем продукт
        command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Producrs WHERE Prod_ID = @id";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
        
        return true;
    }

    /// <summary>
    /// Удалить продукт со всеми связями
    /// </summary>
    public void DeleteProductWithComponents(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Components WHERE ProductID = @id;
            DELETE FROM Components1 WHERE ProductID = @id;
            DELETE FROM Producrs WHERE Prod_ID = @id;";
        command.Parameters.AddWithValue("@id", id);
        
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Получить типы продуктов
    /// </summary>
    public List<ProductType> GetProductTypes()
    {
        var types = new List<ProductType>();
        
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT TypeProdId, Type_Opis, COALESCE(SortOrder, 0) FROM Produkt_Type ORDER BY COALESCE(SortOrder, 0), Type_Opis";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            types.Add(new ProductType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                SortOrder = reader.GetInt32(2)
            });
        }
        
        return types;
    }

    /// <summary>
    /// Получить меры
    /// </summary>
    public List<Measure> GetMeasures()
    {
        var measures = new List<Measure>();
        
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Mera_ID, Name_Mera, COALESCE(Fass_Def, 1), COALESCE(Fass_Izmer, Name_Mera) FROM Mera ORDER BY Name_Mera";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            measures.Add(new Measure
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Fass = reader.GetDouble(2),
                FassIzmer = reader.GetString(3)
            });
        }
        
        return measures;
    }

    /// <summary>
    /// Добавить тип продукта
    /// </summary>
    public int AddProductType(string name, int sortOrder = 0)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Produkt_Type (Type_Opis, SortOrder) VALUES (@name, @sortOrder);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);
        
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Добавить меру
    /// </summary>
    public int AddMeasure(string name, double fassDef, string fassIzmer)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Mera (Name_Mera, Fass_Def, Fass_Izmer) 
            VALUES (@name, @fassDef, @fassIzmer);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@fassDef", fassDef);
        command.Parameters.AddWithValue("@fassIzmer", fassIzmer);
        
        return Convert.ToInt32(command.ExecuteScalar());
    }
    
    /// <summary>
    /// Обновить единицу измерения
    /// </summary>
    public void UpdateMeasure(int id, string name, double fassDef, string fassIzmer)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Mera 
            SET Name_Mera = @name, Fass_Def = @fassDef, Fass_Izmer = @fassIzmer
            WHERE Mera_ID = @id";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@fassDef", fassDef);
        command.Parameters.AddWithValue("@fassIzmer", fassIzmer);
        
        command.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Удалить единицу измерения
    /// </summary>
    public bool DeleteMeasure(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Mera WHERE Mera_ID = @id";
        command.Parameters.AddWithValue("@id", id);
        
        return command.ExecuteNonQuery() > 0;
    }
    
    /// <summary>
    /// Обновить тип продукта
    /// </summary>
    public void UpdateProductType(int id, string name, int sortOrder = 0)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Produkt_Type 
            SET Type_Opis = @name, SortOrder = @sortOrder
            WHERE TypeProdId = @id";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);
        
        command.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Удалить тип продукта
    /// </summary>
    public bool DeleteProductType(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Produkt_Type WHERE TypeProdId = @id";
        command.Parameters.AddWithValue("@id", id);
        
        return command.ExecuteNonQuery() > 0;
    }
}

