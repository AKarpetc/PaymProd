using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
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
                if (pricesDict.TryGetValue(product.ID, out var price))
                {
                    product.Price = (decimal)price;
                }
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
                    
                    if (_menuId.HasValue)
                    {
                        // Сохраняем цену для меню
                        _productRepository.SaveMenuProductPrice(_menuId.Value, product.ID, (double)price);
                    }
                    else
                    {
                        // Сохраняем общую цену
                        _productRepository.UpdateProductPrice(product.ID, (double)price);
                    }
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
                MessageBox.Show($"Ошибка при сохранении цены: {ex.Message}",
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
}

