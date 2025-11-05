using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;

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
        {
            menus.Add(new Menus
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                CountP = reader.GetInt32(2),
                DateBan = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Detail = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
            });
        }
        
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
        {
            return new Menus
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                CountP = reader.GetInt32(2),
                DateBan = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Detail = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
            };
        }
        
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
        
        // Получаем компоненты
        var components = GetAllComponents(connection, menuId);
        
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
            
            var menuDel = new MenuDel_act
            {
                Idmen = reader.GetInt32(0),
                Del_id = delId,
                Countpor = reader.GetInt32(3),
                Del = reader.GetString(4),
                Lcomp = components.Where(c => c.Delid == delId).ToList()
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
}

