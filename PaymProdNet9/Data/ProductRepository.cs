using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;
using PaymProdNet9.Services;

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
                   p.Izmer, mi.Name_Mera,
                   COALESCE(m.Fass_Def, 1) AS BaseFassDef,
                   COALESCE(m.Fass_Izmer, m.Name_Mera) AS BaseFassIzmer,
                   COALESCE(mi.Fass_Def, 1) AS PackFassDef,
                   COALESCE(mi.Fass_Izmer, mi.Name_Mera) AS PackFassIzmer,
                   p.Priz_menu, 
                   COALESCE(p.Count, 0), p.Avtomat, p.Chel, p.Isdiap,
                   COALESCE(p.Price, 0),
                   COALESCE(p.HideInMenu, 0),
                   COALESCE(p.IsDeleted, 0)
            FROM Producrs p
            INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
            LEFT JOIN Mera m ON m.Mera_ID = p.Ves
            LEFT JOIN Mera mi ON mi.Mera_ID = p.Izmer";

        using var reader = command.ExecuteReaderWithLog();
        while (reader.Read())
        {
            products.Add(BuildProductView(reader));
        }

        return products;
    }

    /// <summary>
    /// Добавить продукт
    /// </summary>
    public int AddProduct(string name, int? vesId, int typeId, double fass, int izmerId, int prizMenu = 0, 
        decimal count = 0, bool automat = false, int countPeople = 0, bool mainCount = false, double price = 0,
        bool hideInMenu = false)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Producrs (Name, Type, Ves, Fass, Izmer, Priz_menu, Count, Avtomat, Chel, Isdiap, Price, HideInMenu) 
            VALUES (@name, @type, @ves, @fass, @izmer, @prizMenu, @count, @avtomat, @chel, @isdiap, @price, @hideInMenu);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@ves", vesId.HasValue && vesId.Value > 0 ? (object)vesId.Value : DBNull.Value);
        Logger.Debug($"AddProduct: Сохранение фасовки Fass={fass} для продукта '{name}'");
        command.Parameters.AddWithValue("@fass", fass);
        command.Parameters.AddWithValue("@izmer", izmerId);
        command.Parameters.AddWithValue("@prizMenu", prizMenu);
        command.Parameters.AddWithValue("@count", (double)count);
        command.Parameters.AddWithValue("@avtomat", automat ? 1 : 0);
        command.Parameters.AddWithValue("@chel", countPeople);
        command.Parameters.AddWithValue("@isdiap", mainCount ? 1 : 0);
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@hideInMenu", hideInMenu ? 1 : 0);

        var productId = Convert.ToInt32(command.ExecuteScalar());

        if (prizMenu == 1)
            EnsureLinkedDelicate(connection, productId, name, typeId, count);
        else
            RemoveLinkedDelicate(connection, productId);

        return productId;
    }

    /// <summary>
    /// Добавить продукт с автодобавлением
    /// </summary>
    public int AddProductWithAutoAdd(string name, int vesId, int typeId, double fass, int izmerId,
        int prizMenu, decimal count, int avtomat, int chel, int isdiap, double price = 0, bool hideInMenu = false)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Producrs (Name, Type, Ves, Fass, Izmer, Priz_menu, Count, Avtomat, Chel, Isdiap, Price, HideInMenu) 
            VALUES (@name, @type, @ves, @fass, @izmer, @prizMenu, @count, @avtomat, @chel, @isdiap, @price, @hideInMenu);
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
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@hideInMenu", hideInMenu ? 1 : 0);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Обновить продукт
    /// </summary>
    public void UpdateProduct(int id, string name, int? vesId, int typeId, decimal fass, int izmerId,
        int prizMenu, decimal count, bool automat, int countPeople, bool mainCount, double price = 0,
        bool hideInMenu = false)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Producrs 
            SET Name = @name, Type = @type, Ves = @ves, Fass = @fass, Izmer = @izmer, 
                Priz_menu = @prizMenu, Count = @count, Avtomat = @avtomat, Chel = @chel, Isdiap = @isdiap,
                Price = @price, HideInMenu = @hideInMenu
            WHERE Prod_ID = @id";

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", typeId);
        command.Parameters.AddWithValue("@ves", vesId.HasValue && vesId.Value > 0 ? (object)vesId.Value : DBNull.Value);
        var fassValue = (double)fass;
        Logger.Debug($"UpdateProduct: Сохранение фасовки Fass={fassValue} (decimal={fass}) для продукта ID={id}");
        command.Parameters.AddWithValue("@fass", fassValue);
        command.Parameters.AddWithValue("@izmer", izmerId);
        command.Parameters.AddWithValue("@prizMenu", prizMenu);
        command.Parameters.AddWithValue("@count", (double)count);
        command.Parameters.AddWithValue("@avtomat", automat ? 1 : 0);
        command.Parameters.AddWithValue("@chel", countPeople);
        command.Parameters.AddWithValue("@isdiap", mainCount ? 1 : 0);
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@hideInMenu", hideInMenu ? 1 : 0);

        var rowsAffected = command.ExecuteNonQueryWithLog();
        Logger.Debug($"UpdateProduct: Обновлено строк: {rowsAffected}");

        // Проверяем, что значение действительно сохранилось
        var verifyCommand = connection.CreateCommand();
        verifyCommand.CommandText = "SELECT Fass FROM Producrs WHERE Prod_ID = @id";
        verifyCommand.Parameters.AddWithValue("@id", id);
        var savedFass = Convert.ToDouble(verifyCommand.ExecuteScalar());
        Logger.Debug($"UpdateProduct: Проверка сохраненного значения Fass={savedFass} (ожидалось {fassValue})");
        
        if (Math.Abs(savedFass - fassValue) > 0.001)
        {
            Logger.Error($"UpdateProduct: ОШИБКА! Сохраненное значение Fass={savedFass} не совпадает с ожидаемым {fassValue}");
        }

        if (prizMenu == 1)
            EnsureLinkedDelicate(connection, id, name, typeId, count);
        else
            RemoveLinkedDelicate(connection, id);
    }

    /// <summary>
    /// Удалить продукт
    /// </summary>
    public bool DeleteProduct(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Мягкое удаление продукта:
        // - сам продукт остаётся в базе и во всех старых блюдах/меню;
        // - скрывается из справочников и списков выбора.
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Producrs SET IsDeleted = 1 WHERE Prod_ID = @id";
        command.Parameters.AddWithValue("@id", id);
        var affected = command.ExecuteNonQuery();

        // Связанное блюдо (если создавалось автоматически) НЕ удаляем физически,
        // чтобы не ломать существующие меню. При необходимости его тоже можно
        // пометить как удалённое через интерфейс блюд.

        return affected > 0;
    }

    /// <summary>
    /// Восстановить продукт (снять пометку об удалении)
    /// </summary>
    public void RestoreProduct(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Producrs SET IsDeleted = 0 WHERE Prod_ID = @id";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить продукт со всеми связями (устаревший метод, теперь не используется)
    /// </summary>
    public void DeleteProductWithComponents(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Для совместимости оставляем метод пустым: фактическое удаление
        // компонентов и продукта больше не выполняется, чтобы не ломать меню.
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Producrs SET IsDeleted = 1 WHERE Prod_ID = @id";
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
        command.CommandText =
            "SELECT TypeProdId, Type_Opis, COALESCE(SortOrder, 0), COALESCE(HideInMenu, 0), " +
            "COALESCE(IsDeleted, 0) " +
            "FROM Produkt_Type " +
            "WHERE COALESCE(IsDeleted, 0) = 0 " +
            "ORDER BY COALESCE(SortOrder, 0), Type_Opis";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            types.Add(new ProductType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                SortOrder = reader.GetInt32(2),
                HideInMenu = reader.GetInt32(3) == 1,
                IsDeleted = !reader.IsDBNull(4) && reader.GetInt32(4) == 1
            });

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
        command.CommandText =
            "SELECT Mera_ID, Name_Mera, COALESCE(Fass_Def, 1), COALESCE(Fass_Izmer, Name_Mera), " +
            "COALESCE(RoundingPrecision, 2), COALESCE(MenuRoundingPrecision, 2), " +
            "COALESCE(IsDeleted, 0) " +
            "FROM Mera ORDER BY Name_Mera";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            measures.Add(new Measure
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Fass = reader.GetDouble(2),
                FassIzmer = reader.GetString(3),
                RoundingPrecision = reader.GetInt32(4),
                MenuRoundingPrecision = reader.GetInt32(5),
                IsDeleted = !reader.IsDBNull(6) && reader.GetInt32(6) == 1
            });

        return measures;
    }

    /// <summary>
    /// Добавить тип продукта
    /// </summary>
    public int AddProductType(string name, int sortOrder = 0, bool hideInMenu = false)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Produkt_Type (Type_Opis, SortOrder, HideInMenu) 
            VALUES (@name, @sortOrder, @hideInMenu);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);
        command.Parameters.AddWithValue("@hideInMenu", hideInMenu ? 1 : 0);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Добавить меру
    /// </summary>
    public int AddMeasure(string name, double fassDef, string fassIzmer, int roundingPrecision = 2,
        int menuRoundingPrecision = 2)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Mera (Name_Mera, Fass_Def, Fass_Izmer, RoundingPrecision, MenuRoundingPrecision) 
            VALUES (@name, @fassDef, @fassIzmer, @roundingPrecision, @menuRoundingPrecision);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@fassDef", fassDef);
        command.Parameters.AddWithValue("@fassIzmer", fassIzmer);
        command.Parameters.AddWithValue("@roundingPrecision", roundingPrecision);
        command.Parameters.AddWithValue("@menuRoundingPrecision", menuRoundingPrecision);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Обновить единицу измерения
    /// </summary>
    public void UpdateMeasure(int id, string name, double fassDef, string fassIzmer, int roundingPrecision = 2,
        int menuRoundingPrecision = 2)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Mera 
            SET Name_Mera = @name, Fass_Def = @fassDef, Fass_Izmer = @fassIzmer, 
                RoundingPrecision = @roundingPrecision, MenuRoundingPrecision = @menuRoundingPrecision
            WHERE Mera_ID = @id";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@fassDef", fassDef);
        command.Parameters.AddWithValue("@fassIzmer", fassIzmer);
        command.Parameters.AddWithValue("@roundingPrecision", roundingPrecision);
        command.Parameters.AddWithValue("@menuRoundingPrecision", menuRoundingPrecision);

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
        // Мягкое удаление меры, чтобы не ломать существующие продукты/блюда
        command.CommandText = "UPDATE Mera SET IsDeleted = 1 WHERE Mera_ID = @id";
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Обновить тип продукта
    /// </summary>
    public void UpdateProductType(int id, string name, int sortOrder = 0, bool hideInMenu = false)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Produkt_Type 
            SET Type_Opis = @name, SortOrder = @sortOrder, HideInMenu = @hideInMenu
            WHERE TypeProdId = @id";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);
        command.Parameters.AddWithValue("@hideInMenu", hideInMenu ? 1 : 0);

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
        // Мягкое удаление типа продукта
        command.CommandText = "UPDATE Produkt_Type SET IsDeleted = 1 WHERE TypeProdId = @id";
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Обновить цену продукта (общая цена)
    /// </summary>
    public void UpdateProductPrice(int productId, double price)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Producrs SET Price = @price WHERE Prod_ID = @id";
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@id", productId);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Получить продукты, добавленные в меню (через Components1 и Components)
    /// </summary>
    public List<ProductView> GetMenuProducts(int menuId)
    {
        var products = new List<ProductView>();
        var productIds = new HashSet<int>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Получаем уникальные ID продуктов из Components1 для данного меню (измененные компоненты)
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DISTINCT ProductID 
            FROM Components1 
            WHERE Idmen = @menuId";
        command.Parameters.AddWithValue("@menuId", menuId);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                productIds.Add(reader.GetInt32(0));
            }
        }

        // Также получаем продукты из стандартных компонентов блюд, которые есть в меню
        command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DISTINCT c.ProductID
            FROM Components c
            INNER JOIN Menu_Delicates md ON md.Id_delic = c.Delic_id
            WHERE md.Id_men = @menuId";
        command.Parameters.AddWithValue("@menuId", menuId);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                productIds.Add(reader.GetInt32(0));
            }
        }

        // Получаем полную информацию о продуктах
        if (productIds.Count > 0)
        {
            var idsString = string.Join(",", productIds);
            command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT p.Prod_ID, p.Name, pt.Type_Opis, m.Name_Mera, 
                       pt.TypeProdId, p.Ves, COALESCE(p.Fass, 0), 
                       p.Izmer, mi.Name_Mera,
                       COALESCE(m.Fass_Def, 1) AS BaseFassDef,
                       COALESCE(m.Fass_Izmer, m.Name_Mera) AS BaseFassIzmer,
                       COALESCE(mi.Fass_Def, 1) AS PackFassDef,
                       COALESCE(mi.Fass_Izmer, mi.Name_Mera) AS PackFassIzmer,
                       p.Priz_menu, 
                       COALESCE(p.Count, 0), p.Avtomat, p.Chel, p.Isdiap,
                       COALESCE(p.Price, 0),
                       COALESCE(p.HideInMenu, 0)
                FROM Producrs p
                INNER JOIN Produkt_Type pt ON p.Type = pt.TypeProdId
                LEFT JOIN Mera m ON m.Mera_ID = p.Ves
                LEFT JOIN Mera mi ON mi.Mera_ID = p.Izmer
                WHERE p.Prod_ID IN ({idsString})";

            using var reader = command.ExecuteReader();
            while (reader.Read())
                products.Add(BuildProductView(reader));
        }

        return products;
    }

    private ProductView BuildProductView(SqliteDataReader reader)
    {
        var productId = reader.GetInt32(0);
        var fassValue = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6);
        Logger.Debug($"BuildProductView: Загрузка продукта ID={productId}, Fass из базы={fassValue}");
        
        var product = new ProductView
        {
            ID = productId,
            Name = reader.GetString(1),
            Type = reader.GetString(2),
            Ves = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            TID = reader.GetInt32(4),
            VID = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            Fass = fassValue,
            Iz = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
            IzName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            PrizMen = reader.GetInt32(13),
            PrizMen1 = reader.GetInt32(13) == 1,
            Count = reader.GetDecimal(14),
            AutoAdd = reader.GetInt32(15) == 1,
            CountPeople = reader.GetInt32(16),
            MainCount = reader.GetInt32(17) == 1,
            Price = reader.IsDBNull(18) ? 0 : Convert.ToDecimal(reader.GetDouble(18)),
            HideInMenu = reader.GetInt32(19) == 1,
            IsDeleted = !reader.IsDBNull(20) && reader.GetInt32(20) == 1
        };

        var baseFassDef = reader.IsDBNull(9) ? 0 : Convert.ToDecimal(reader.GetDouble(9));
        var baseFassIzmer = reader.IsDBNull(10) ? product.Ves : reader.GetString(10) ?? product.Ves;
        var packFassDef = reader.IsDBNull(11) ? 0 : Convert.ToDecimal(reader.GetDouble(11));
        var packFassIzmer = reader.IsDBNull(12) ? product.IzName : reader.GetString(12) ?? product.IzName;

        ApplyPackagingDefaults(product, baseFassDef, baseFassIzmer, packFassDef, packFassIzmer);

        return product;
    }

    private static void ApplyPackagingDefaults(ProductView product, decimal baseFassDef, string baseFassIzmer,
        decimal packFassDef, string packFassIzmer)
    {
        // НЕ перезаписываем Fass продукта - используем значение из базы данных
        var packagingFass = product.Fass;
        var packagingName = product.IzName;

        if (product.Iz == 0)
        {
            // НЕ перезаписываем Fass, если оно уже установлено (больше 0)
            // Используем baseFassDef только если Fass продукта не установлен (равен 0)
            if (baseFassDef > 0 && product.Fass == 0)
                packagingFass = baseFassDef;

            packagingName = string.IsNullOrWhiteSpace(baseFassIzmer) ? product.Ves : baseFassIzmer;
        }
        else
        {
            // НЕ перезаписываем Fass, если оно уже установлено (больше 0)
            // Используем packFassDef только если Fass продукта не установлен (равен 0)
            if (packFassDef > 0 && product.Fass == 0)
                packagingFass = packFassDef;

            packagingName = !string.IsNullOrWhiteSpace(product.IzName)
                ? product.IzName
                : (!string.IsNullOrWhiteSpace(packFassIzmer)
                    ? packFassIzmer
                    : (string.IsNullOrWhiteSpace(baseFassIzmer) ? product.Ves : baseFassIzmer));
        }

        // НЕ устанавливаем значение по умолчанию 1, если Fass продукта уже установлено
        // Используем значение по умолчанию только если Fass продукта не установлено (равен 0)
        if (product.Fass == 0 && packagingFass <= 0) packagingFass = 1;
        if (string.IsNullOrWhiteSpace(packagingName)) packagingName = product.Ves;

        // Сохраняем значение Fass продукта - НЕ перезаписываем, если оно уже установлено
        if (product.Fass > 0)
        {
            // Fass продукта уже установлено - не изменяем его
            Logger.Debug($"ApplyPackagingDefaults: Сохраняем существующее значение Fass={product.Fass} для продукта ID={product.ID}");
        }
        else
        {
            // Fass продукта не установлено - используем значение по умолчанию
            product.Fass = packagingFass;
        }
        product.IzName = packagingName;
    }

    /// <summary>
    /// Получить цены продуктов для меню
    /// </summary>
    public List<MenuProductPrice> GetMenuProductPrices(int menuId)
    {
        var prices = new List<MenuProductPrice>();

        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT ProductID, Price 
            FROM Menu_Product_Prices 
            WHERE Id_men = @menuId";
        command.Parameters.AddWithValue("@menuId", menuId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            prices.Add(new MenuProductPrice
            {
                ProductID = reader.GetInt32(0),
                Price = reader.GetDouble(1)
            });
        }

        return prices;
    }

    /// <summary>
    /// Сохранить цену продукта для меню
    /// </summary>
    public void SaveMenuProductPrice(int menuId, int productId, double price)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Menu_Product_Prices (Id_men, ProductID, Price)
            VALUES (@menuId, @productId, @price)
            ON CONFLICT(Id_men, ProductID) DO UPDATE SET Price = @price";
        command.Parameters.AddWithValue("@menuId", menuId);
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@price", price);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Копировать цену продукта в меню при добавлении продукта
    /// </summary>
    public void CopyProductPriceToMenu(int menuId, int productId)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        // Получаем цену продукта из справочника
        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT COALESCE(Price, 0) FROM Producrs WHERE Prod_ID = @id";
        selectCommand.Parameters.AddWithValue("@id", productId);

        var price = Convert.ToDouble(selectCommand.ExecuteScalar());

        // Сохраняем цену в меню, если её ещё нет
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT OR IGNORE INTO Menu_Product_Prices (Id_men, ProductID, Price)
            VALUES (@menuId, @productId, @price)";
        insertCommand.Parameters.AddWithValue("@menuId", menuId);
        insertCommand.Parameters.AddWithValue("@productId", productId);
        insertCommand.Parameters.AddWithValue("@price", price);

        insertCommand.ExecuteNonQuery();
    }

    #region Linked dishes helpers

    private void EnsureLinkedDelicate(SqliteConnection connection, int productId, string productName, int productTypeId, decimal productCount)
    {
        var delicateTypeId = EnsureLinkedDelicateType(connection, productTypeId);
        var portion = productCount > 0 ? productCount : 1m;

        int delicateId;
        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT Del_id FROM Delicates WHERE LinkedProductId = @productId";
        selectCommand.Parameters.AddWithValue("@productId", productId);

        using (var reader = selectCommand.ExecuteReader())
        {
            if (reader.Read())
            {
                delicateId = reader.GetInt32(0);
                var update = connection.CreateCommand();
                update.CommandText = @"
                    UPDATE Delicates
                    SET Del_Type = @typeId,
                        Del_Name = @name,
                        Del_Ves = @ves,
                        Del_count = @count
                    WHERE Del_id = @delicateId";
                update.Parameters.AddWithValue("@typeId", delicateTypeId);
                update.Parameters.AddWithValue("@name", productName);
                update.Parameters.AddWithValue("@ves", (double)portion);
                update.Parameters.AddWithValue("@count", (double)portion);
                update.Parameters.AddWithValue("@delicateId", delicateId);
                update.ExecuteNonQuery();
            }
            else
            {
                var insert = connection.CreateCommand();
                insert.CommandText = @"
                    INSERT INTO Delicates (Del_Type, Del_Name, Del_Ves, Del_count, Datew, LinkedProductId)
                    VALUES (@typeId, @name, @ves, @count, datetime('now'), @productId);
                    SELECT last_insert_rowid();";
                insert.Parameters.AddWithValue("@typeId", delicateTypeId);
                insert.Parameters.AddWithValue("@name", productName);
                insert.Parameters.AddWithValue("@ves", (double)portion);
                insert.Parameters.AddWithValue("@count", (double)portion);
                insert.Parameters.AddWithValue("@productId", productId);
                delicateId = Convert.ToInt32(insert.ExecuteScalar());
            }
        }

        EnsureLinkedComponent(connection, delicateId, productId, portion);
    }

    private void EnsureLinkedComponent(SqliteConnection connection, int delicateId, int productId, decimal weight)
    {
        var cleanCommand = connection.CreateCommand();
        cleanCommand.CommandText = "DELETE FROM Components WHERE Delic_id = @delicId AND ProductID <> @productId";
        cleanCommand.Parameters.AddWithValue("@delicId", delicateId);
        cleanCommand.Parameters.AddWithValue("@productId", productId);
        cleanCommand.ExecuteNonQuery();

        var selectComponent = connection.CreateCommand();
        selectComponent.CommandText = "SELECT Comp_Id FROM Components WHERE Delic_id = @delicId AND ProductID = @productId LIMIT 1";
        selectComponent.Parameters.AddWithValue("@delicId", delicateId);
        selectComponent.Parameters.AddWithValue("@productId", productId);

        using var reader = selectComponent.ExecuteReader();
        if (reader.Read())
        {
            var update = connection.CreateCommand();
            update.CommandText = "UPDATE Components SET Ves = @ves WHERE Comp_Id = @id";
            update.Parameters.AddWithValue("@ves", (double)weight);
            update.Parameters.AddWithValue("@id", reader.GetInt32(0));
            update.ExecuteNonQuery();
        }
        else
        {
            var insert = connection.CreateCommand();
            insert.CommandText = @"
                INSERT INTO Components (Delic_id, ProductID, Ves, Detail)
                VALUES (@delicId, @productId, @ves, NULL)";
            insert.Parameters.AddWithValue("@delicId", delicateId);
            insert.Parameters.AddWithValue("@productId", productId);
            insert.Parameters.AddWithValue("@ves", (double)weight);
            insert.ExecuteNonQuery();
        }
    }

    private int EnsureLinkedDelicateType(SqliteConnection connection, int productTypeId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Type_Del_ID FROM Type_Del WHERE LinkedProductTypeId = @typeId LIMIT 1";
        command.Parameters.AddWithValue("@typeId", productTypeId);

        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
                return reader.GetInt32(0);
        }

        var typeInfo = GetProductTypeInfo(connection, productTypeId);

        var insert = connection.CreateCommand();
        insert.CommandText = @"
            INSERT INTO Type_Del (Type_del_opis, SortOrder, LinkedProductTypeId)
            VALUES (@name, @sortOrder, @linked);
            SELECT last_insert_rowid();";
        insert.Parameters.AddWithValue("@name", typeInfo.Name);
        insert.Parameters.AddWithValue("@sortOrder", typeInfo.SortOrder);
        insert.Parameters.AddWithValue("@linked", productTypeId);

        return Convert.ToInt32(insert.ExecuteScalar());
    }

    private (string Name, int SortOrder) GetProductTypeInfo(SqliteConnection connection, int productTypeId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Type_Opis, COALESCE(SortOrder, 0) FROM Produkt_Type WHERE TypeProdId = @typeId";
        command.Parameters.AddWithValue("@typeId", productTypeId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
            return (reader.GetString(0), reader.GetInt32(1));

        return ($"Тип {productTypeId}", 0);
    }

    private void RemoveLinkedDelicate(SqliteConnection connection, int productId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Del_id, Del_Type FROM Delicates WHERE LinkedProductId = @productId";
        command.Parameters.AddWithValue("@productId", productId);

        int? delicateId = null;
        int? delicateTypeId = null;

        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                delicateId = reader.GetInt32(0);
                delicateTypeId = reader.IsDBNull(1) ? null : reader.GetInt32(1);
            }
        }

        if (!delicateId.HasValue)
            return;

        var deleteComponents1 = connection.CreateCommand();
        deleteComponents1.CommandText = "DELETE FROM Components1 WHERE Delic_id = @delicId";
        deleteComponents1.Parameters.AddWithValue("@delicId", delicateId.Value);
        deleteComponents1.ExecuteNonQuery();

        var deleteMenuDelicates = connection.CreateCommand();
        deleteMenuDelicates.CommandText = "DELETE FROM Menu_Delicates WHERE Id_delic = @delicId";
        deleteMenuDelicates.Parameters.AddWithValue("@delicId", delicateId.Value);
        deleteMenuDelicates.ExecuteNonQuery();

        var deleteComponents = connection.CreateCommand();
        deleteComponents.CommandText = "DELETE FROM Components WHERE Delic_id = @delicId";
        deleteComponents.Parameters.AddWithValue("@delicId", delicateId.Value);
        deleteComponents.ExecuteNonQuery();

        var deleteDelicate = connection.CreateCommand();
        deleteDelicate.CommandText = "DELETE FROM Delicates WHERE Del_id = @delicId";
        deleteDelicate.Parameters.AddWithValue("@delicId", delicateId.Value);
        deleteDelicate.ExecuteNonQuery();

        if (delicateTypeId.HasValue)
            CleanupLinkedDelicateType(connection, delicateTypeId.Value);
    }

    private void CleanupLinkedDelicateType(SqliteConnection connection, int delicateTypeId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT LinkedProductTypeId FROM Type_Del WHERE Type_Del_ID = @typeId";
        command.Parameters.AddWithValue("@typeId", delicateTypeId);

        int? linkedProductTypeId = null;
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read() && !reader.IsDBNull(0))
                linkedProductTypeId = reader.GetInt32(0);
        }

        if (!linkedProductTypeId.HasValue)
            return;

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Delicates WHERE Del_Type = @typeId";
        countCommand.Parameters.AddWithValue("@typeId", delicateTypeId);
        var usage = Convert.ToInt32(countCommand.ExecuteScalar());

        if (usage > 0)
            return;

        var deleteType = connection.CreateCommand();
        deleteType.CommandText = "DELETE FROM Type_Del WHERE Type_Del_ID = @typeId";
        deleteType.Parameters.AddWithValue("@typeId", delicateTypeId);
        deleteType.ExecuteNonQuery();
    }

    #endregion
}

/// <summary>
/// Модель для цены продукта в меню
/// </summary>
public class MenuProductPrice
{
    public int ProductID { get; set; }
    public double Price { get; set; }
}