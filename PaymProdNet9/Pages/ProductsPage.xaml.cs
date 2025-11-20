using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace PaymProdNet9.Pages;

public partial class ProductsPage : Page
{
    private readonly ProductRepository _productRepository;

    private ObservableCollection<ProductView> _allProducts;
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
            _allProducts.Clear();
            var products = _productRepository.GetAllProducts();
            foreach (var product in products) _allProducts.Add(product);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = ProductSearchBox.Text.ToLower();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            LoadProducts();
            return;
        }

        try
        {
            _allProducts.Clear();
            var allProducts = _productRepository.GetAllProducts();
            var filtered = allProducts.Where(p =>
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
        // Автоматически устанавливаем ту же единицу измерения для фасовки
        if (ProductMeasureComboBox.SelectedValue != null && ProductFassMeasureComboBox.Items.Count > 0)
        {
            ProductFassMeasureComboBox.SelectedValue = ProductMeasureComboBox.SelectedValue;
        }
    }

    private void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var product = button?.DataContext as ProductView;
        if (product == null) return;

        _currentProductId = product.ID;

        ProductNameTextBox.Text = product.Name;
        ProductCountTextBox.Text = product.Count.ToString();
        ProductFassTextBox.Text = product.Fass.ToString();
        ProductCountPeopleTextBox.Text = product.CountPeople.ToString();

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
        ProductAddToDishesCheckBox.IsChecked = false;
        ProductAutoAddCheckBox.IsChecked = false;
        ProductMainCountCheckBox.IsChecked = false;

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

            var result = MessageBox.Show($"Удалить продукт '{product.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _productRepository.DeleteProduct(product.ID);
                LoadProducts();
                MessageBox.Show("Продукт удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении продукта: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            if (!string.IsNullOrWhiteSpace(ProductFassTextBox.Text))
            {
                double.TryParse(ProductFassTextBox.Text, out fass);
            }

            int countPeople = 0;
            if (!string.IsNullOrWhiteSpace(ProductCountPeopleTextBox.Text))
            {
                int.TryParse(ProductCountPeopleTextBox.Text, out countPeople);
            }

            var prizMenu = ProductAddToDishesCheckBox.IsChecked == true ? 1 : 0;
            var automat = ProductAutoAddCheckBox.IsChecked == true;
            var mainCount = ProductMainCountCheckBox.IsChecked == true;

            // Используем основную единицу измерения как Ves (если нужно)
            int? vesId = measureId > 0 ? measureId : null;

            if (_currentProductId.HasValue)
            {
                // Обновление существующего продукта
                _productRepository.UpdateProduct(
                    _currentProductId.Value,
                    ProductNameTextBox.Text,
                    vesId,
                    typeId,
                    (decimal)fass,
                    fassMeasureId,
                    prizMenu,
                    count,
                    automat,
                    countPeople,
                    mainCount
                );

                MessageBox.Show("Продукт обновлен!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Создание нового продукта
                _productRepository.AddProduct(
                    ProductNameTextBox.Text,
                    vesId,
                    typeId,
                    fass,
                    fassMeasureId,
                    prizMenu,
                    count,
                    automat,
                    countPeople,
                    mainCount
                );

                MessageBox.Show("Продукт создан!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadProducts();
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
        // Разрешаем только цифры и десятичный разделитель
        var textBox = sender as TextBox;
        if (textBox == null) return;

        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var allowedChars = "0123456789" + decimalSeparator;

        // Проверяем, что вводимый символ разрешен
        if (allowedChars.IndexOf(e.Text) < 0)
        {
            e.Handled = true;
            return;
        }

        // Проверяем, что десятичный разделитель не дублируется
        if (e.Text == decimalSeparator && textBox.Text.Contains(decimalSeparator))
        {
            e.Handled = true;
        }
    }
}