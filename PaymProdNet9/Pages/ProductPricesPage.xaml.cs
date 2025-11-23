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

namespace PaymProdNet9.Pages;

public partial class ProductPricesPage : Page
{
    private readonly ProductRepository _productRepository;
    private ObservableCollection<ProductView> _allProducts;
    private string _currentTypeFilter = "%";
    private int? _menuId; // Если null - редактирование общих цен, иначе - цены для конкретного меню

    public ProductPricesPage(int? menuId = null)
    {
        InitializeComponent();
        _productRepository = new ProductRepository();
        _allProducts = new ObservableCollection<ProductView>();
        _menuId = menuId;
        ProductsPricesDataGrid.ItemsSource = _allProducts;
        
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
                // Загружаем цены из меню
                LoadMenuPrices();
            }
        }
        catch (Exception ex)
        {
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
        var textBox = sender as TextBox;
        if (textBox == null) return;

        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var allowedChars = "0123456789" + decimalSeparator;

        if (allowedChars.IndexOf(e.Text) < 0)
        {
            e.Handled = true;
            return;
        }

        if (e.Text == decimalSeparator && textBox.Text.Contains(decimalSeparator))
        {
            e.Handled = true;
        }
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
        return _allProducts.Any(p => p.IsModified);
    }

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var modifiedProducts = _allProducts.Where(p => p.IsModified).ToList();
            
            if (modifiedProducts.Count == 0)
            {
                MessageBox.Show("Нет изменений для сохранения.", 
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var product in modifiedProducts)
            {
                if (_menuId.HasValue)
                {
                    // Сохраняем цену для меню
                    _productRepository.SaveMenuProductPrice(_menuId.Value, product.ID, (double)product.Price);
                    
                    // Если галочка включена, обновляем базовую цену в справочнике
                    if (product.SaveToBasePrice)
                    {
                        _productRepository.UpdateProductPrice(product.ID, (double)product.Price);
                        product.BasePrice = product.Price; // Обновляем отображаемую базовую цену
                    }
                }
                else
                {
                    // Сохраняем общую цену
                    _productRepository.UpdateProductPrice(product.ID, (double)product.Price);
                    product.BasePrice = product.Price; // Обновляем базовую цену
                }
                
                // Обновляем оригинальную цену и сбрасываем флаг изменений
                product.OriginalPrice = product.Price;
                product.IsModified = false;
            }

            MessageBox.Show($"Сохранено изменений: {modifiedProducts.Count}", 
                "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            MessageBox.Show($"Ошибка при отмене изменений: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                SaveChanges_Click(SaveButton, new RoutedEventArgs());
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }
    }
}

