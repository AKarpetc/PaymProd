using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.ComponentModel;

namespace PaymProdNet9.Pages;

public partial class ProductPricesPage : Page
{
    private readonly ProductRepository _productRepository;
    private readonly DelicateRepository _delicateRepository;
    private readonly MenuRepository _menuRepository;
    private readonly MenuPriceService _menuPriceService;
    private readonly SettingsRepository _settingsRepository;
    private ObservableCollection<ProductView> _allProducts;
    private ObservableCollection<DishMarkupView> _allDishes;
    private string _currentTypeFilter = "%";

    private string _currentDishTypeFilter = "%";
    private int? _menuId; // Если null - редактирование общих цен, иначе - цены для конкретного меню

    public ProductPricesPage(int? menuId = null)
    {
        InitializeComponent();
        _productRepository = new ProductRepository();
        _delicateRepository = new DelicateRepository();
        _menuRepository = new MenuRepository();
        _menuPriceService = new MenuPriceService();
        _settingsRepository = new SettingsRepository();
        _allProducts = new ObservableCollection<ProductView>();
        _allDishes = new ObservableCollection<DishMarkupView>();
        _menuId = menuId;
        ProductsPricesDataGrid.ItemsSource = _allProducts;
        DishMarkupDataGrid.ItemsSource = _allDishes;
        
        // Подписываемся на событие навигации
        if (NavigationService != null)
        {
            NavigationService.Navigating -= ProductPricesPage_Navigating;
            NavigationService.Navigating += ProductPricesPage_Navigating;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Скрываем колонки "Старая цена" и "Сохранить в цену" для общей цены продуктов
            if (!_menuId.HasValue)
            {
                BasePriceColumn.Visibility = Visibility.Collapsed;
                SaveToBasePriceColumn.Visibility = Visibility.Collapsed;
            }
            else
            {
                BasePriceColumn.Visibility = Visibility.Visible;
                SaveToBasePriceColumn.Visibility = Visibility.Visible;
            }
            
            // Сначала загружаем продукты (для меню это нужно для определения типов)
            LoadProducts();
            // Затем загружаем типы (они будут отфильтрованы по продуктам меню)
            LoadProductTypes();
            
            if (_menuId.HasValue)
            {
                LoadMenuPrices();
            }

            // Загружаем блюда и типы блюд
            LoadDishes();
            LoadDishTypes();
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при загрузке данных", ex);
            MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadProductTypes()
    {
        try
        {
            var panel = FilterPanel;
            if (panel == null) return;

            // Очищаем все кнопки кроме "Все"
            var buttonsToRemove = panel.Children.Cast<UIElement>()
                .Where(c => c != AllTypesButton).ToList();
            foreach (var button in buttonsToRemove) panel.Children.Remove(button);

            List<ProductType> types;
            
            // Если это меню, показываем только типы продуктов, которые есть в загруженных продуктах
            if (_menuId.HasValue)
            {
                // Используем уже загруженные продукты из _allProducts
                var menuProductTypes = _allProducts.Select(p => p.Type).Distinct().ToHashSet();
                var allTypes = _productRepository.GetProductTypes();
                types = allTypes.Where(t => menuProductTypes.Contains(t.Name)).ToList();
            }
            else
            {
                types = _productRepository.GetProductTypes().ToList();
            }
            
            foreach (var type in types.OrderBy(t => t.SortOrder).ThenBy(t => t.Name))
            {
                var button = new Button
                {
                    Content = type.Name,
                    Tag = type.Name,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                button.Click += FilterByType_Click;
                button.SetResourceReference(StyleProperty, "MaterialDesignRaisedButton");
                panel.Children.Add(button);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при загрузке типов продуктов", ex);
            MessageBox.Show($"Ошибка при загрузке типов продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterByType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            _currentTypeFilter = button.Tag?.ToString() ?? "%";
            
            // Обновляем стиль кнопок
            foreach (Button btn in FilterPanel.Children.OfType<Button>())
            {
                btn.Style = (Style)FindResource("MaterialDesignRaisedButton");
            }
            button.Style = (Style)FindResource("MaterialDesignFlatButton");
            
            LoadProducts();
        }
    }

    private void FilterDishByType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            _currentDishTypeFilter = button.Tag?.ToString() ?? "%";
            
            // Обновляем стиль кнопок
            foreach (Button btn in DishFilterPanel.Children.OfType<Button>())
            {
                btn.Style = (Style)FindResource("MaterialDesignRaisedButton");
            }
            button.Style = (Style)FindResource("MaterialDesignFlatButton");
            
            LoadDishes();
        }
    }

    private void LoadDishTypes()
    {
        try
        {
             var panel = DishFilterPanel;
             if (panel == null) return;
 
             // Очищаем все кнопки кроме "Все", если они были добавлены динамически (хотя здесь "DishAllTypesButton" статическая)
             var buttonsToRemove = panel.Children.Cast<UIElement>()
                 .Where(c => c != DishAllTypesButton).ToList();
             foreach (var button in buttonsToRemove) panel.Children.Remove(button);
 
             List<DelicateType> types;
             
             // Для блюд всегда показываем все типы, которые есть в меню
             if (_menuId.HasValue)
             {
                 var menuDelicates = _menuRepository.GetMenuDelicates(_menuId.Value);
                 // Здесь сложно получить типы напрямую из MenuDel_act, так как там нет TypeId.
                 // Но мы можем получить все доступные типы.
                 // Для простоты пока загружаем все типы.
                 types = _delicateRepository.GetDelicateTypes();
             }
             else
             {
                 types = _delicateRepository.GetDelicateTypes();
             }
             
             foreach (var type in types.OrderBy(t => t.SortOrder).ThenBy(t => t.Name))
             {
                 var button = new Button
                 {
                     Content = type.Name,
                     Tag = type.Name,
                     Margin = new Thickness(0, 0, 5, 0)
                 };
                 button.Click += FilterDishByType_Click;
                 button.SetResourceReference(StyleProperty, "MaterialDesignRaisedButton");
                 panel.Children.Add(button);
             }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при загрузке типов блюд", ex);
            MessageBox.Show($"Ошибка при загрузке типов блюд: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadDishes()
    {
        try
        {
            _allDishes.Clear();
            if (!_menuId.HasValue) return;

            var menuDelicates = _menuRepository.GetMenuDelicates(_menuId.Value);

            // Получаем мапу типов для фильтрации (в MenuDel_act нет названий типов)
            // Придется подгружать деликатесы чтобы узнать тип? 
            // Или расширить MenuDel_act?
            // Пока просто загружаем все, фильтрацию реализуем на клиенте если типы есть в справочнике
            
            // Оптимизация: нам нужно знать тип блюда для фильтрации.
            // MenuDel_act имеет Del_id. Можно получить DelicatesColl по ID.
            // Но делать это в цикле дорого.
            // Мы можем загрузить все Delicates (GetAllDelicates) и сделать словарь Id -> Type.
            var allDelicatesDict = _delicateRepository.GetAllDelicates().ToDictionary(d => d.Id, d => d.Type);

            var filtered = menuDelicates.Where(d => 
            {
                if (d.HideInMenu) return false;
                if (_currentDishTypeFilter == "%") return true;
                if (allDelicatesDict.TryGetValue(d.Del_id, out var type))
                {
                    return type == _currentDishTypeFilter;
                }
                return false; 
            }).ToList();

            Logger.Debug($"LoadDishes: Loading {filtered.Count} dishes. MenuId={_menuId}");

            var settings = _settingsRepository.GetSettings();
            ServicePercentRun.Text = $"{settings.ServicePercent:G}%";

            foreach (var md in filtered)
            {
                // Для расчета себестоимости и общей суммы нам нужно знать стоимость ингредиентов
                // Мы можем использовать MenuPriceService, но для этого нужен Components список
                // В MenuDel_act Components уже загружены (Lcomp)
                
                decimal baseCost = 0;
                if (md.Lcomp != null)
                {
                    foreach (var comp in md.Lcomp)
                    {
                        var priceInfo = _menuPriceService.GetComponentPriceInfo(_menuId.Value, comp, md.Countpor);
                        baseCost += priceInfo.TotalPrice;
                    }
                }

                var view = new DishMarkupView
                {
                    Id = md.Id, 
                    Name = md.Del,
                    ShortComposition = md.Sost,
                    DefaultMarkup = md.DefaultMarkup,
                    Markup = md.Markup ?? md.DefaultMarkup, 
                    SaveToDefault = true,
                    IsModified = false,
                    Type = allDelicatesDict.ContainsKey(md.Del_id) ? allDelicatesDict[md.Del_id] : "",
                    Count = md.Countpor,
                    BaseCost = baseCost
                };
                
                // Подписываемся на изменение свойств для пересчета итогов
                view.PropertyChanged += DishView_PropertyChanged;
                
                _allDishes.Add(view);
            }
            
            RecalculateTotals();
        }
        catch (Exception ex)
        {
             Logger.Error("Ошибка при загрузке блюд", ex);
             MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void DishView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DishMarkupView.Markup))
        {
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        try
        {
            if (!_menuId.HasValue) return;

            decimal totalDishesSum = 0;
            var settings = _settingsRepository.GetSettings();

            foreach (var dish in _allDishes)
            {
                // Цена блюда = (Себестоимость) * (Наценка / 100)
                // Если наценка 0 или меньше, считаем без наценки? Обычно наценка 200% это множитель 2.0? Нет, 200% это +200% или умножить на 2?
                // Проверим MenuPrinter: dishTotal = dishTotal * (delicate.DefaultMarkup / 100);
                // То есть если Markup = 200, то цена = cost * 2. 
                // Обычно наценка 200% означает: Цена = Себестоимость * (1 + 200/100) = Cost * 3.
                // НО в MenuPrinter код: dishTotal = dishTotal * (delicate.DefaultMarkup / 100);
                // Это значит, если Markup = 200, то Цена = Cost * 2. 
                // Это странно (обычно markup добавляется), но я буду следовать логике MenuPrinter.
                
                decimal markupMultiplier = (dish.Markup > 0) ? (dish.Markup / 100) : 1;
                decimal dishPrice = dish.BaseCost * markupMultiplier;
                
                // Учитываем количество порций? 
                // BaseCost расчитан на md.Countpor (так как GetComponentPriceInfo берет delicate.Count)
                // Подождите. GetComponentPriceInfo(..., delicate.Count).
                // Если delicate.Count - это количество порций блюда в заказе.
                // То PriceInfo возвращает общую стоимость ВСЕХ порций этого компонента.
                // Значит baseCost - это уже полная себестоимость ВСЕХ порций блюда.
                
                totalDishesSum += dishPrice;
            }

            decimal serviceCharge = totalDishesSum * (settings.ServicePercent / 100);
            decimal grandTotal = totalDishesSum + serviceCharge;

            TotalDishesRun.Text = FormatCurrency(totalDishesSum);
            TotalServiceRun.Text = FormatCurrency(serviceCharge);
            GrandTotalRun.Text = FormatCurrency(grandTotal);
        }
        catch (Exception ex)
        {
            Logger.Error("Error recalculating totals", ex);
        }
    }

    private string FormatCurrency(decimal value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    private void LoadProducts()
    {
        try
        {
            _allProducts.Clear();
            
            List<ProductView> productsToShow;
            
            // Если это меню, показываем только продукты, добавленные в меню
            if (_menuId.HasValue)
            {
                productsToShow = _productRepository.GetMenuProducts(_menuId.Value);
            }
            else
            {
                productsToShow = _productRepository.GetAllProducts();
            }
            
            // Сохраняем базовые цены для всех продуктов
            foreach (var product in productsToShow)
            {
                product.BasePrice = product.Price; // Сохраняем цену из справочника как базовую
                product.OriginalPrice = product.Price; // Сохраняем оригинальную цену для отслеживания изменений
                product.SaveToBasePrice = true; // По умолчанию галочка включена
                product.IsModified = false; // Сбрасываем флаг изменений
            }
            
            // Фильтруем по типу
            var filtered = productsToShow;
            if (_currentTypeFilter != "%")
            {
                filtered = productsToShow.Where(p => p.Type == _currentTypeFilter).ToList();
            }
            
            foreach (var product in filtered.OrderBy(p => p.Name))
            {
                _allProducts.Add(product);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при загрузке продуктов", ex);
            MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadMenuPrices()
    {
        if (!_menuId.HasValue) return;
        
        try
        {
            var menuPrices = _productRepository.GetMenuProductPrices(_menuId.Value);
            var pricesDict = menuPrices.ToDictionary(p => p.ProductID, p => p.Price);
            
            foreach (var product in _allProducts)
            {
                // BasePrice уже установлена при загрузке продуктов (цена из справочника)
                // Теперь загружаем цену из меню, если она есть
                if (pricesDict.TryGetValue(product.ID, out var price))
                {
                    product.Price = (decimal)price;
                }
                else
                {
                    // Если цены в меню нет, используем базовую цену
                    product.Price = product.BasePrice;
                }
                // Сохраняем оригинальную цену для отслеживания изменений
                product.OriginalPrice = product.Price;
                product.IsModified = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при загрузке цен меню", ex);
            MessageBox.Show($"Ошибка при загрузке цен меню: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProductsPricesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel) return;
        
        if (e.Column.Header.ToString() == "Цена, тг" && e.Row.Item is ProductView product)
        {
            try
            {
                var textBox = e.EditingElement as TextBox;
                if (textBox == null) return;

                if (decimal.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var price))
                {
                    product.Price = price;
                    // Помечаем как измененную, если цена отличается от оригинальной
                    product.IsModified = product.Price != product.OriginalPrice;
                }
                else
                {
                    MessageBox.Show("Неверный формат цены. Используйте число.", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при изменении цены", ex);
                MessageBox.Show($"Ошибка при изменении цены: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
        }
    }

    private void ProductsPricesDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.Column.Header?.ToString() == "Цена, тг" && e.EditingElement is TextBox textBox)
        {
            textBox.PreviewTextInput += NumericOnly_PreviewTextInput;
        }
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Services.InputValidationHelper.NumericOnly_PreviewTextInput(sender, e);
    }

    private void NumericField_LostFocus(object sender, RoutedEventArgs e)
    {
        Services.InputValidationHelper.ValidateNumericField_LostFocus(sender, e);
    }

    private void ProductsPricesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var dataGrid = sender as DataGrid;
        if (dataGrid == null) return;

        // Находим редактируемую колонку (Цена, тг)
        var priceColumn = dataGrid.Columns
            .OfType<DataGridTextColumn>()
            .FirstOrDefault(c => c.Header?.ToString() == "Цена, тг");

        if (priceColumn == null) return;

        // Получаем текущую позицию
        var currentRow = dataGrid.Items.IndexOf(dataGrid.CurrentItem);
        var currentColumn = dataGrid.CurrentColumn;

        if (currentRow < 0 || currentColumn == null) return;

        if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            // Завершаем редактирование текущей ячейки
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Переходим на следующую строку
            var nextRow = currentRow + 1;
            if (nextRow < dataGrid.Items.Count)
            {
                // Используем Dispatcher для выполнения после завершения редактирования
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGrid.SelectedIndex = nextRow;
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[nextRow], priceColumn);
                    
                    // Начинаем редактирование
                    dataGrid.BeginEdit();
                }), System.Windows.Threading.DispatcherPriority.Input);
                
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down)
        {
            // Завершаем редактирование текущей ячейки
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Переходим на следующую строку
            var nextRow = currentRow + 1;
            if (nextRow < dataGrid.Items.Count)
            {
                // Используем Dispatcher для выполнения после завершения редактирования
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGrid.SelectedIndex = nextRow;
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[nextRow], currentColumn);
                    
                    // Если это колонка "Цена, тг", начинаем редактирование
                    if (currentColumn == priceColumn)
                    {
                        dataGrid.BeginEdit();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
                
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Up)
        {
            // Завершаем редактирование текущей ячейки
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Переходим на предыдущую строку
            var prevRow = currentRow - 1;
            if (prevRow >= 0)
            {
                // Используем Dispatcher для выполнения после завершения редактирования
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGrid.SelectedIndex = prevRow;
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[prevRow], currentColumn);
                    
                    // Если это колонка "Цена, тг", начинаем редактирование
                    if (currentColumn == priceColumn)
                    {
                        dataGrid.BeginEdit();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
                
                e.Handled = true;
            }
        }
    }

    private bool HasUnsavedChanges()
    {
        return _allProducts.Any(p => p.IsModified) || _allDishes.Any(d => d.IsModified);
    }

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int count = SaveProductsInternal();
            if (count > 0)
            {
                MessageBox.Show($"Сохранено изменений: {count}", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                 MessageBox.Show("Нет изменений для сохранения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при сохранении изменений", ex);
            MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int SaveProductsInternal()
    {
        var modifiedProducts = _allProducts.Where(p => p.IsModified).ToList();
        if (modifiedProducts.Count == 0) return 0;

        foreach (var product in modifiedProducts)
        {
            if (_menuId.HasValue)
            {
                _productRepository.SaveMenuProductPrice(_menuId.Value, product.ID, (double)product.Price);
                if (product.SaveToBasePrice)
                {
                    _productRepository.UpdateProductPrice(product.ID, (double)product.Price);
                    product.BasePrice = product.Price; 
                }
            }
            else
            {
                _productRepository.UpdateProductPrice(product.ID, (double)product.Price);
                product.BasePrice = product.Price;
            }
            
            product.OriginalPrice = product.Price;
            product.IsModified = false;
        }
        return modifiedProducts.Count;
    }

    private void CancelChanges_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var modifiedProducts = _allProducts.Where(p => p.IsModified).ToList();
            
            if (modifiedProducts.Count == 0)
            {
                MessageBox.Show("Нет изменений для отмены.", 
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Отменить все изменения ({modifiedProducts.Count} продуктов)?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var product in modifiedProducts)
                {
                    product.Price = product.OriginalPrice;
                    product.IsModified = false;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при отмене изменений", ex);
            MessageBox.Show($"Ошибка при отмене изменений: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveDishChanges_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int count = SaveDishesInternal();
            if (count > 0)
            {
                MessageBox.Show($"Сохранено изменений блюд: {count}", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Нет изменений для сохранения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при сохранении блюд", ex);
            MessageBox.Show($"Ошибка при сохранении блюд: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int SaveDishesInternal()
    {
        var modifiedDishes = _allDishes.Where(d => d.IsModified).ToList();
        Logger.Debug($"SaveDishesInternal: Found {modifiedDishes.Count} modified dishes.");
        
        if (modifiedDishes.Count == 0) return 0;

        foreach (var dish in modifiedDishes)
        {
            Logger.Debug($"Saving Dish: Id={dish.Id}, Name={dish.Name}, NewMarkup={dish.Markup}, SaveToDefault={dish.SaveToDefault}");
            
            _menuRepository.UpdateMenuDelicateMarkup(dish.Id, dish.Markup);

            if (dish.SaveToDefault)
            {
                var md = _menuRepository.GetMenuDelicateById(dish.Id, _menuId.Value);
                if (md != null)
                {
                    var delicateId = md.Del_id;
                    if (delicateId > 0)
                    {
                        Logger.Debug($"Updating DefaultMarkup for Dish {delicateId} to {dish.Markup}");
                        _delicateRepository.UpdateDelicateDefaultMarkup(delicateId, dish.Markup);
                        dish.DefaultMarkup = dish.Markup;
                    }
                }
            }

            dish.IsModified = false;
        }
        return modifiedDishes.Count;
    }

    private void CancelDishChanges_Click(object sender, RoutedEventArgs e)
    {
        var modified = _allDishes.Where(d => d.IsModified).Count();
        if (modified == 0) return;

        if (MessageBox.Show($"Отменить изменения ({modified})?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            LoadDishes(); // Проще перезагрузить список
        }
    }

    private void ProductPricesPage_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (HasUnsavedChanges())
        {
            var result = MessageBox.Show(
                "У вас есть несохраненные изменения. Сохранить их перед выходом?",
                "Несохраненные изменения",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int prodCount = SaveProductsInternal();
                    int dishCount = SaveDishesInternal();
                    // Optional: Show summary message? Usually silent save on close is better or just simple OK.
                    // But if errors occur, exceptions are thrown.
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при сохранении перед выходом", ex);
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true; // Отменяем выход, если ошибка
                }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }
    }

    private void DishMarkupDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var dataGrid = sender as DataGrid;
        if (dataGrid == null) return;

        // Находим редактируемую колонку (Надбавка %)
        var markupColumn = dataGrid.Columns
            .OfType<DataGridTextColumn>()
            .FirstOrDefault(c => c.Header?.ToString() == "Надбавка %");

        if (markupColumn == null) return;

        // Получаем текущую позицию
        var currentRow = dataGrid.Items.IndexOf(dataGrid.CurrentItem);
        var currentColumn = dataGrid.CurrentColumn;

        if (currentRow < 0 || currentColumn == null) return;

        if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            // Завершаем редактирование текущей ячейки
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Переходим на следующую строку
            var nextRow = currentRow + 1;
            if (nextRow < dataGrid.Items.Count)
            {
                // Используем Dispatcher для выполнения после завершения редактирования
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGrid.SelectedIndex = nextRow;
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[nextRow], markupColumn);
                    
                    // Начинаем редактирование
                    dataGrid.BeginEdit();
                }), System.Windows.Threading.DispatcherPriority.Input);
                
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down)
        {
            // Завершаем редактирование текущей ячейки
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Переходим на следующую строку
            var nextRow = currentRow + 1;
            if (nextRow < dataGrid.Items.Count)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGrid.SelectedIndex = nextRow;
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[nextRow], currentColumn);
                    
                    if (currentColumn == markupColumn)
                    {
                        dataGrid.BeginEdit();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
                
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Up)
        {
            // Завершаем редактирование текущей ячейки
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Переходим на предыдущую строку
            var prevRow = currentRow - 1;
            if (prevRow >= 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGrid.SelectedIndex = prevRow;
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[prevRow], currentColumn);
                    
                    if (currentColumn == markupColumn)
                    {
                        dataGrid.BeginEdit();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
                
                e.Handled = true;
            }
        }
    }
    }


/// <summary>
/// Модель для редактирования наценки на блюда
/// </summary>
public class DishMarkupView : INotifyPropertyChanged
{
    private int _id;
    private string _name = string.Empty;
    private string _shortComposition = string.Empty;
    private decimal _defaultMarkup;
    private decimal _markup;
    private bool _saveToDefault;
    private bool _isModified;
    private string _type = string.Empty;
    private int _typeId;

    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(nameof(Id)); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string ShortComposition
    {
        get => _shortComposition;
        set { _shortComposition = value; OnPropertyChanged(nameof(ShortComposition)); }
    }

    public string Type
    {
        get => _type;
        set { _type = value; OnPropertyChanged(nameof(Type)); }
    }

    public int TypeId
    {
        get => _typeId;
        set { _typeId = value; OnPropertyChanged(nameof(TypeId)); }
    }
    
    // Новые поля для подсчета итогов
    public decimal Count { get; set; }
    public decimal BaseCost { get; set; }

    /// <summary>
    /// Наценка по умолчанию (из справочника)
    /// </summary>
    public decimal DefaultMarkup
    {
        get => _defaultMarkup;
        set { _defaultMarkup = value; OnPropertyChanged(nameof(DefaultMarkup)); }
    }

    /// <summary>
    /// Текущая наценка (для меню)
    /// </summary>
    public decimal Markup
    {
        get => _markup;
        set 
        { 
            _markup = value; 
            _isModified = true;
            OnPropertyChanged(nameof(Markup)); 
            OnPropertyChanged(nameof(IsModified));
        }
    }

    public bool SaveToDefault
    {
        get => _saveToDefault;
        set { _saveToDefault = value; OnPropertyChanged(nameof(SaveToDefault)); }
    }

    public bool IsModified
    {
        get => _isModified;
        set { _isModified = value; OnPropertyChanged(nameof(IsModified)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Статический класс для передачи ID продукта
/// </summary>
public static class IDProd
{
    public static int ID { get; set; }
    public static double Ves { get; set; }
}

/// <summary>
/// Статический класс для редактирования продукта
/// </summary>
public static class ProductEdit
{
    public static bool Flag { get; set; }
    public static ProductView? Pv { get; set; }
}

