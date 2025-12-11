using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using PaymProdNet9.Data;

namespace PaymProdNet9.Pages;

public partial class ProductsPage : Page
{
    private readonly ProductRepository _productRepository;

    private ObservableCollection<ProductView> _allProducts;
    private List<ProductView> _productsCache = new();
    private int? _currentProductId;

    public ProductsPage()
    {
        InitializeComponent();

        _productRepository = new ProductRepository();
        _allProducts = new ObservableCollection<ProductView>();

        ProductsDataGrid.ItemsSource = _allProducts;

        // Подписываемся на событие Loaded для установки обработчика навигации
        Loaded += ProductsPage_LoadedInternal;
    }

    /// <summary>
    /// Обработчик загрузки страницы для установки навигационного обработчика
    /// </summary>
    private void ProductsPage_LoadedInternal(object sender, RoutedEventArgs e)
    {
        // Подписываемся на событие навигации Frame
        if (NavigationService != null)
        {
            NavigationService.Navigating -= ProductsPage_Navigating; // Отписываемся, если уже подписаны
            NavigationService.Navigating += ProductsPage_Navigating;
        }
    }

    /// <summary>
    /// Обработка навигации назад - работает как "Отмена" в режиме редактирования
    /// </summary>
    private void ProductsPage_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        // Если открыт режим редактирования и пользователь нажал "Назад"
        if (ProductEditView.Visibility == Visibility.Visible && e.NavigationMode == NavigationMode.Back)
        {
            // Отменяем навигацию
            e.Cancel = true;

            // Вызываем метод отмены (возвращаемся к списку)
            ShowListView();
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProducts();
        ApplyProductFilter();
        LoadProductTypes();
        LoadMeasures();
        ShowListView(); // Start in list view
    }

    private void ShowListView()
    {
        ProductsListView.Visibility = Visibility.Visible;
        ProductEditView.Visibility = Visibility.Collapsed;
    }

    private void ShowEditView(bool isEdit)
    {
        ProductsListView.Visibility = Visibility.Collapsed;
        ProductEditView.Visibility = Visibility.Visible;
        EditModeTitle.Text = isEdit ? "Редактирование продукта" : "Создание нового продукта";
        SaveButton.Content = isEdit ? "💾 Сохранить изменения" : "💾 Создать продукт";
    }

    private void LoadProducts()
    {
        try
        {
            var showDeleted = DeletedItemsViewSettings.ShowDeletedItems;
            var products = _productRepository.GetAllProducts()
                .Where(p => showDeleted || !p.IsDeleted)
                .ToList();
            _productsCache = products;

            _allProducts.Clear();
            foreach (var product in _productsCache) _allProducts.Add(product);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Добавить продукт с флагом AutoAdd в открытое меню
    /// </summary>
    private void AddAutoProductToOpenMenu(int productId, decimal baseCount, int countPeople, bool mainCount)
    {
        try
        {
            var menuRepository = new MenuRepository();
            var openMenu = menuRepository.GetOpenMenu();
            
            if (openMenu == null)
            {
                Logger.Debug("Нет открытого меню для автоматического добавления продукта");
                return;
            }

            Logger.Debug($"Автоматическое добавление продукта ID={productId} в меню ID={openMenu.Id}");
            
            // Используем метод MenuRepository для добавления продукта с AutoAdd
            menuRepository.AddAutoProductToMenu(openMenu.Id, productId, baseCount, openMenu.CountP);
            
            Logger.Info($"Продукт ID={productId} автоматически добавлен в меню ID={openMenu.Id}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Ошибка при автоматическом добавлении продукта в меню", ex);
            MessageBox.Show($"Продукт сохранен, но не удалось автоматически добавить его в текущее меню: {ex.Message}",
                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ProductSearchClearButton != null)
            ProductSearchClearButton.Visibility =
                string.IsNullOrWhiteSpace(ProductSearchBox.Text)
                    ? Visibility.Collapsed
                    : Visibility.Visible;

        ApplyProductFilter();
    }

    /// <summary>
    /// Применяет фильтр поиска к списку продуктов на основе текста в ProductSearchBox.
    /// </summary>
    private void ApplyProductFilter()
    {
        if (_productsCache == null || _allProducts == null)
            return;

        var searchText = (ProductSearchBox.Text ?? string.Empty).ToLower();

        _allProducts.Clear();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Возвращаем полный список из кэша, не обращаясь к базе
            foreach (var product in _productsCache) _allProducts.Add(product);
            return;
        }

        try
        {
            var filtered = _productsCache.Where(p =>
                p.Name.ToLower().Contains(searchText) ||
                (p.Type != null && p.Type.ToLower().Contains(searchText)) ||
                (p.IzName != null && p.IzName.ToLower().Contains(searchText))
            );

            foreach (var product in filtered) _allProducts.Add(product);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при поиске продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProductSearchClearButton_Click(object sender, RoutedEventArgs e)
    {
        ProductSearchBox.Text = string.Empty; // TextChanged вызовет ApplyProductFilter
    }

    private void LoadProductTypes()
    {
        try
        {
            var types = _productRepository.GetProductTypes();
            ProductTypeComboBox.ItemsSource = types;
            ProductTypeComboBox.DisplayMemberPath = "Name";
            ProductTypeComboBox.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadMeasures()
    {
        try
        {
            var measures = _productRepository.GetMeasures();
            ProductMeasureComboBox.ItemsSource = measures;
            ProductMeasureComboBox.DisplayMemberPath = "Name";
            ProductMeasureComboBox.SelectedValuePath = "Id";
            
            ProductFassMeasureComboBox.ItemsSource = measures;
            ProductFassMeasureComboBox.DisplayMemberPath = "Name";
            ProductFassMeasureComboBox.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке единиц измерения: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProductMeasureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Автоматическое заполнение единицы измерения и значения фасовки только при создании продукта
        if (_currentProductId.HasValue)
        {
            // При редактировании продукта ничего не делаем
            return;
        }

        // Режим создания нового продукта
        if (ProductMeasureComboBox.SelectedValue != null && ProductFassMeasureComboBox.Items.Count > 0)
        {
            try
            {
                // Получаем список мер из источника данных
                var measures = ProductMeasureComboBox.ItemsSource as List<Measure>;
                if (measures == null)
                {
                    measures = _productRepository.GetMeasures();
                }

                // Находим выбранную меру
                var selectedMeasureId = (int)ProductMeasureComboBox.SelectedValue;
                var selectedMeasure = measures.FirstOrDefault(m => m.Id == selectedMeasureId);
                
                if (selectedMeasure != null)
                {
                    // Устанавливаем единицу измерения фасовки из справочника мер
                    if (!string.IsNullOrWhiteSpace(selectedMeasure.FassIzmer))
                    {
                        // Ищем меру по имени FassIzmer
                        var fassMeasure = measures.FirstOrDefault(m => 
                            m.Name.Equals(selectedMeasure.FassIzmer, StringComparison.OrdinalIgnoreCase));
                        
                        if (fassMeasure != null)
                        {
                            ProductFassMeasureComboBox.SelectedValue = fassMeasure.Id;
                        }
                    }
                    else
                    {
                        // Если FassIzmer не указан, используем ту же единицу измерения
                        ProductFassMeasureComboBox.SelectedValue = selectedMeasureId;
                    }

                    // Устанавливаем значение фасовки из справочника мер
                    if (selectedMeasure.Fass > 0)
                    {
                        ProductFassTextBox.Text = selectedMeasure.Fass.ToString(CultureInfo.CurrentCulture);
                    }
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки просто игнорируем - не блокируем создание продукта
                System.Diagnostics.Debug.WriteLine($"Ошибка при установке фасовки: {ex.Message}");
            }
        }
    }

    private void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var product = button?.DataContext as ProductView;
        if (product == null) return;

        if (product.IsDeleted)
        {
            MessageBox.Show(
                $"Продукт \"{product.Name}\" помечен как удалённый.\n" +
                "Сначала восстановите его, чтобы редактировать.",
                "Нельзя редактировать удалённый продукт",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _currentProductId = product.ID;

        ProductNameTextBox.Text = product.Name;
        ProductCountTextBox.Text = product.Count.ToString(CultureInfo.CurrentCulture);
        // Форматируем фасовку с учетом культуры (без лишних нулей, если целое число)
        var fassValue = product.Fass;
        Logger.Debug($"Загрузка продукта ID={product.ID}: Fass из базы={fassValue}");
        ProductFassTextBox.Text = fassValue == (int)fassValue 
            ? ((int)fassValue).ToString(CultureInfo.CurrentCulture)
            : fassValue.ToString(CultureInfo.CurrentCulture);
        Logger.Debug($"Загрузка продукта ID={product.ID}: Fass в TextBox='{ProductFassTextBox.Text}'");
        ProductCountPeopleTextBox.Text = product.CountPeople.ToString();
        ProductPriceTextBox.Text = product.Price == 0
            ? string.Empty
            : product.Price.ToString(CultureInfo.CurrentCulture);

        // Устанавливаем тип
        var types = _productRepository.GetProductTypes();
        var typeToSelect = types.FirstOrDefault(t => t.Name == product.Type);
        if (typeToSelect != null && typeToSelect.Id > 0) ProductTypeComboBox.SelectedValue = typeToSelect.Id;

        // Устанавливаем основную единицу измерения
        var measures = _productRepository.GetMeasures();
        if (product.VID > 0)
        {
            var measureToSelect = measures.FirstOrDefault(m => m.Id == product.VID);
            if (measureToSelect != null) ProductMeasureComboBox.SelectedValue = measureToSelect.Id;
        }

        // Устанавливаем единицу измерения для фасовки
        if (product.Iz > 0)
        {
            var fassMeasureToSelect = measures.FirstOrDefault(m => m.Id == product.Iz);
            if (fassMeasureToSelect != null) ProductFassMeasureComboBox.SelectedValue = fassMeasureToSelect.Id;
        }

        // Устанавливаем чекбоксы
        ProductAddToDishesCheckBox.IsChecked = product.PrizMen1;
        ProductAutoAddCheckBox.IsChecked = product.AutoAdd;
        ProductMainCountCheckBox.IsChecked = product.MainCount;
        ProductHideInMenuCheckBox.IsChecked = product.HideInMenu;

        ShowEditView(true);
    }

    private void NewProduct_Click(object sender, RoutedEventArgs e)
    {
        _currentProductId = null;

        ProductNameTextBox.Clear();
        ProductTypeComboBox.SelectedIndex = -1;
        ProductMeasureComboBox.SelectedIndex = -1;
        ProductFassMeasureComboBox.SelectedIndex = -1;
        ProductCountTextBox.Clear();
        ProductFassTextBox.Clear();
        ProductCountPeopleTextBox.Clear();
        ProductPriceTextBox.Clear();
        ProductAddToDishesCheckBox.IsChecked = false;
        ProductAutoAddCheckBox.IsChecked = false;
        ProductMainCountCheckBox.IsChecked = false;
        ProductHideInMenuCheckBox.IsChecked = false;

        ShowEditView(false);
        ProductNameTextBox.Focus();
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ShowListView();
    }

    private void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var product = button?.DataContext as ProductView;
            if (product == null) return;

            if (product.IsDeleted)
            {
                // Восстановление продукта
                var restoreResult = MessageBox.Show(
                    $"Восстановить продукт '{product.Name}'?\n\n" +
                    "Он снова будет доступен в справочнике и при выборе в блюдах/меню.",
                    "Восстановление продукта",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (restoreResult == MessageBoxResult.Yes)
                {
                    _productRepository.RestoreProduct(product.ID);
                    LoadProducts();
                    MessageBox.Show("Продукт восстановлен.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                // Мягкое удаление продукта
                var result = MessageBox.Show(
                    $"Пометить продукт '{product.Name}' как удалённый?\n\n" +
                    "Продукт исчезнет из справочника и новых блюд/меню,\n" +
                    "но останется во всех уже созданных блюдах и отчетах.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _productRepository.DeleteProduct(product.ID);
                    LoadProducts();
                    MessageBox.Show("Продукт помечен как удалённый.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении продукта: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Отдельный обработчик для кнопки восстановления в таблице (для наглядности XAML),
    /// фактически перенаправляет на DeleteProduct_Click, где есть логика восстановления.
    /// </summary>
    private void RestoreProduct_Click(object sender, RoutedEventArgs e)
    {
        DeleteProduct_Click(sender, e);
    }

    private void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text))
            {
                MessageBox.Show("Введите название продукта!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип продукта!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductMeasureComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите основную единицу измерения!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductFassMeasureComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите единицу измерения для фасовки!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var typeId = (int)ProductTypeComboBox.SelectedValue;
            var measureId = (int)ProductMeasureComboBox.SelectedValue;
            var fassMeasureId = (int)ProductFassMeasureComboBox.SelectedValue;
            
            // Парсим числовые значения
            decimal count = 0;
            if (!string.IsNullOrWhiteSpace(ProductCountTextBox.Text))
            {
                decimal.TryParse(ProductCountTextBox.Text, out count);
            }

            double fass = 0;
            var fassText = ProductFassTextBox.Text?.Trim() ?? string.Empty;
            Logger.Debug($"Парсинг фасовки: исходный текст='{fassText}'");
            
            if (!string.IsNullOrWhiteSpace(fassText))
            {
                // Используем правильный парсинг с учетом культуры (как для цены)
                bool parsed = false;
                if (double.TryParse(fassText, NumberStyles.Any, CultureInfo.CurrentCulture, out fass))
                {
                    parsed = true;
                    Logger.Debug($"Фасовка распарсена с CurrentCulture: {fass}");
                }
                else if (double.TryParse(fassText, NumberStyles.Any, CultureInfo.InvariantCulture, out fass))
                {
                    parsed = true;
                    Logger.Debug($"Фасовка распарсена с InvariantCulture: {fass}");
                }
                else
                {
                    // Пробуем заменить запятую на точку и наоборот
                    var textWithDot = fassText.Replace(',', '.');
                    var textWithComma = fassText.Replace('.', ',');
                    
                    if (double.TryParse(textWithDot, NumberStyles.Any, CultureInfo.InvariantCulture, out fass))
                    {
                        parsed = true;
                        Logger.Debug($"Фасовка распарсена после замены запятой на точку: {fass}");
                    }
                    else if (double.TryParse(textWithComma, NumberStyles.Any, CultureInfo.CurrentCulture, out fass))
                    {
                        parsed = true;
                        Logger.Debug($"Фасовка распарсена после замены точки на запятую: {fass}");
                    }
                }
                
                if (!parsed)
                {
                    Logger.Error($"Не удалось распарсить фасовку: '{fassText}'");
                    MessageBox.Show($"Неверное значение фасовки! Используйте число (например: 600 или 600,0).\nВведено: '{fassText}'",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                Logger.Debug("Фасовка пустая, используется значение по умолчанию 0");
            }
            
            Logger.Debug($"Итоговое значение фасовки для сохранения: {fass}");

            int countPeople = 0;
            if (!string.IsNullOrWhiteSpace(ProductCountPeopleTextBox.Text))
            {
                int.TryParse(ProductCountPeopleTextBox.Text, out countPeople);
            }

            var prizMenu = ProductAddToDishesCheckBox.IsChecked == true ? 1 : 0;
            var automat = ProductAutoAddCheckBox.IsChecked == true;
            var mainCount = ProductMainCountCheckBox.IsChecked == true;
        var hideInMenu = ProductHideInMenuCheckBox.IsChecked == true;

        double price = 0;
        if (!string.IsNullOrWhiteSpace(ProductPriceTextBox.Text))
            double.TryParse(ProductPriceTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out price);

            // Используем основную единицу измерения как Ves (если нужно)
            int? vesId = measureId > 0 ? measureId : null;

            int productId;
            if (_currentProductId.HasValue)
            {
                // Обновление существующего продукта
                var fassValue = (decimal)fass;
                Logger.Debug($"Сохранение продукта ID={_currentProductId.Value}, Fass={fassValue} (из текста '{ProductFassTextBox.Text}')");
                _productRepository.UpdateProduct(
                    _currentProductId.Value,
                    ProductNameTextBox.Text,
                    vesId,
                    typeId,
                    fassValue,
                    fassMeasureId,
                    prizMenu,
                    count,
                    automat,
                    countPeople,
                    mainCount,
                    price,
                    hideInMenu
                );
                productId = _currentProductId.Value;

                MessageBox.Show("Продукт обновлен!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Создание нового продукта
                Logger.Debug($"Создание продукта, Fass={fass} (из текста '{ProductFassTextBox.Text}')");
                productId = _productRepository.AddProduct(
                    ProductNameTextBox.Text,
                    vesId,
                    typeId,
                    fass, // AddProduct ожидает double
                    fassMeasureId,
                    prizMenu,
                    count,
                    automat,
                    countPeople,
                    mainCount,
                    price,
                    hideInMenu
                );

                MessageBox.Show("Продукт создан!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Если продукт имеет флаг автоматического добавления, добавляем его в открытое меню
            // В старом приложении продукты с AutoAdd добавлялись автоматически независимо от Priz_menu
            if (automat)
            {
                AddAutoProductToOpenMenu(productId, count, countPeople, mainCount);
            }

            LoadProducts();
            
            // Проверяем значение после загрузки
            if (_currentProductId.HasValue)
            {
                var loadedProduct = _allProducts.FirstOrDefault(p => p.ID == _currentProductId.Value);
                if (loadedProduct != null)
                {
                    Logger.Debug($"SaveProduct: После LoadProducts продукт ID={_currentProductId.Value}, Fass={loadedProduct.Fass}");
                }
            }

            // Обновляем список и сохраняем текущий фильтр поиска
            LoadProducts();
            ApplyProductFilter();

            ShowListView(); // Return to list view
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении продукта: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        InputValidationHelper.NumericOnly_PreviewTextInput(sender, e);
    }

    private void NumericField_LostFocus(object sender, RoutedEventArgs e)
    {
        InputValidationHelper.ValidateNumericField_LostFocus(sender, e);
    }

    private void TextField_LostFocus(object sender, RoutedEventArgs e)
    {
        InputValidationHelper.ValidateTextField_LostFocus(sender, e);
    }
}