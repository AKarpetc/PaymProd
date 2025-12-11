namespace PaymProdNet9.Services;

/// <summary>
/// Глобальная настройка отображения удалённых элементов в справочниках.
/// Управляется из раздела "База данных" и читается страницами/окнами справочников.
/// </summary>
public static class DeletedItemsViewSettings
{
    /// <summary>
    /// Если true – в справочниках показываются элементы с IsDeleted = 1
    /// (строки подсвечиваются и доступны только для восстановления).
    /// По умолчанию false.
    /// </summary>
    public static bool ShowDeletedItems { get; set; }
}




