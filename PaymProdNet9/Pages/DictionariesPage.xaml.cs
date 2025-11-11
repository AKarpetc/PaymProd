using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace PaymProdNet9.Pages;

public partial class DictionariesPage : Page
{
    private readonly DelicateRepository _delicateRepository;
    private readonly ProductRepository _productRepository;
    
    private ObservableCollection<DelicatesColl> _allDelicates;
    private ObservableCollection<ProductView> _allProducts;
    private ObservableCollection<Components> _currentDelicateComponents;
    
    private int? _currentDelicateId;

    public DictionariesPage()
    {
        InitializeComponent();
        
        _delicateRepository = new DelicateRepository();
        _productRepository = new ProductRepository();
        
        _allDelicates = new ObservableCollection<DelicatesColl>();
        _allProducts = new ObservableCollection<ProductView>();
        _currentDelicateComponents = new ObservableCollection<Components>();
        
        DelicateComponentsGrid.ItemsSource = _currentDelicateComponents;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadAllData();
    }

    /// <summary>
    /// Загрузка всех данных
    /// </summary>
    private void LoadAllData()
    {
        LoadDelicates();
        LoadProducts();
        LoadDelicateTypes();
        LoadProductTypes();
        LoadMeasures();
    }

    /// <summary>
    /// Загрузка списка блюд
    /// </summary>
    private void LoadDelicates()
    {
        try
        {
            _allDelicates.Clear();
            var delicates = _delicateRepository.GetAllDelicates();
            foreach (var delicate in delicates)
            {
                _allDelicates.Add(delicate);
            }
            DelicatesDataGrid.ItemsSource = _allDelicates;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка списка продуктов
    /// </summary>
    private void LoadProducts()
    {
        try
        {
            _allProducts.Clear();
            var products = _productRepository.GetAllProducts();
            foreach (var product in products)
            {
                _allProducts.Add(product);
            }
            ProductsDataGrid.ItemsSource = _allProducts;
            AvailableProductsList.ItemsSource = _allProducts;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка типов блюд
    /// </summary>
    private void LoadDelicateTypes()
    {
        try
        {
            var types = _delicateRepository.GetDelicateTypes();
            var typesList = types.Select(t => new { Id = t.Id, Name = t.Name }).ToList();
            DelicateTypeComboBox.ItemsSource = typesList;
            DelicateTypeComboBox.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов блюд: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка типов продуктов
    /// </summary>
    private void LoadProductTypes()
    {
        try
        {
            var types = _productRepository.GetProductTypes();
            var typesList = types.Select(t => new { Id = t.Id, Name = t.Name }).ToList();
            ProductTypeComboBox.ItemsSource = typesList;
            ProductTypeComboBox.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов продуктов: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка мер измерения
    /// </summary>
    private void LoadMeasures()
    {
        try
        {
            var measures = _productRepository.GetMeasures();
            var measuresList = measures.Select(m => new { Id = m.Id, Name = m.Name }).ToList();
            ProductMeasureComboBox.ItemsSource = measuresList;
            ProductMeasureComboBox.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке мер: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Выбор блюда в таблице
    /// </summary>
    private void DelicatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedDelicate = DelicatesDataGrid.SelectedItem as DelicatesColl;
        if (selectedDelicate == null) return;

        try
        {
            _currentDelicateId = selectedDelicate.Id;
            DelicateNameTextBox.Text = selectedDelicate.Name;
            DelicateWeightTextBox.Text = selectedDelicate.Ves.ToString();
            DelicateCountTextBox.Text = selectedDelicate.Count.ToString();
            
            // Выбираем тип
            var typeId = selectedDelicate.IDType;
            DelicateTypeComboBox.SelectedValue = typeId;

            // Загружаем компоненты
            LoadDelicateComponents(selectedDelicate.Id);
            
            DelicateEditPanel.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка компонентов блюда
    /// </summary>
    private void LoadDelicateComponents(int delicateId)
    {
        try
        {
            _currentDelicateComponents.Clear();
            var delicate = _delicateRepository.GetDelicateById(delicateId);
            if (delicate != null)
            {
                foreach (var component in delicate.Lcomp)
                {
                    _currentDelicateComponents.Add(component);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке состава: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Перейти к составу (создать новое блюдо)
    /// </summary>
    private void GoToComposition_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DelicateNameTextBox.Text))
            {
                MessageBox.Show("Введите название блюда!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DelicateTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип блюда!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var typeId = (int)DelicateTypeComboBox.SelectedValue;
            var ves = decimal.TryParse(DelicateWeightTextBox.Text, out var w) ? w : 0;
            var count = decimal.TryParse(DelicateCountTextBox.Text, out var c) ? c : 0;

            _currentDelicateId = _delicateRepository.AddDelicate(
                typeId, DelicateNameTextBox.Text, ves, count);

            LoadDelicates();
            DelicateEditPanel.IsEnabled = true;
            
            MessageBox.Show("Блюдо создано! Теперь можно добавлять продукты в состав.", 
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Создать новое блюдо
    /// </summary>
    private void NewDelicate_Click(object sender, RoutedEventArgs e)
    {
        _currentDelicateId = null;
        DelicateNameTextBox.Clear();
        DelicateWeightTextBox.Clear();
        DelicateCountTextBox.Clear();
        DelicateTypeComboBox.SelectedIndex = -1;
        _currentDelicateComponents.Clear();
        DelicateEditPanel.IsEnabled = true; // Активируем панель для создания нового блюда
        DelicateNameTextBox.Focus(); // Ставим фокус на поле ввода названия
    }

    /// <summary>
    /// Сохранить блюдо (создать новое или обновить существующее)
    /// </summary>
    private void UpdateDelicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Проверка заполнения полей
            if (string.IsNullOrWhiteSpace(DelicateNameTextBox.Text))
            {
                MessageBox.Show("Введите название блюда!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DelicateTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип блюда!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var typeId = (int)DelicateTypeComboBox.SelectedValue;
            var ves = decimal.TryParse(DelicateWeightTextBox.Text, out var w) ? w : 0;
            var count = decimal.TryParse(DelicateCountTextBox.Text, out var c) ? c : 1; // По умолчанию 1 порция

            if (_currentDelicateId.HasValue)
            {
                // Обновляем существующее блюдо
                _delicateRepository.UpdateDelicate(
                    _currentDelicateId.Value, typeId, DelicateNameTextBox.Text, ves, count);
                
                MessageBox.Show("Блюдо обновлено!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Создаем новое блюдо
                var newId = _delicateRepository.AddDelicate(typeId, DelicateNameTextBox.Text, ves, count);
                _currentDelicateId = newId;
                
                MessageBox.Show("Блюдо создано! Теперь можно добавить продукты в его состав.", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadDelicates();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Удалить блюдо
    /// </summary>
    private void DeleteDelicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var delicate = button?.DataContext as DelicatesColl;
            if (delicate == null) return;

            var result = MessageBox.Show("Удалить блюдо?", 
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _delicateRepository.DeleteDelicate(delicate.Id);
                LoadDelicates();
                NewDelicate_Click(sender, e);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Добавить продукт в состав блюда
    /// </summary>
    private void AddProductToDelicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentDelicateId.HasValue)
            {
                MessageBox.Show("Сначала создайте или выберите блюдо!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProduct = AvailableProductsList.SelectedItem as ProductView;
            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите продукт!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Запрашиваем вес
            var weightDialog = new WeightInputDialog();
            if (weightDialog.ShowDialog() == true)
            {
                var weight = weightDialog.Weight;
                _delicateRepository.AddComponent(_currentDelicateId.Value, selectedProduct.ID, weight);
                LoadDelicateComponents(_currentDelicateId.Value);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Удалить продукт из состава блюда
    /// </summary>
    private void RemoveProductFromDelicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentDelicateId.HasValue) return;

            var selectedComponent = DelicateComponentsGrid.SelectedItem as Components;
            if (selectedComponent == null)
            {
                MessageBox.Show("Выберите продукт для удаления!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _delicateRepository.DeleteComponentByProductAndDelicate(
                selectedComponent.Prodid, _currentDelicateId.Value);
            LoadDelicateComponents(_currentDelicateId.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Добавить новый продукт
    /// </summary>
    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text))
            {
                MessageBox.Show("Введите название продукта!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductTypeComboBox.SelectedValue == null || ProductMeasureComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип и меру измерения!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var typeId = (int)ProductTypeComboBox.SelectedValue;
            var measureId = (int)ProductMeasureComboBox.SelectedValue;

            _productRepository.AddProduct(ProductNameTextBox.Text, measureId, typeId, 1, measureId);
            
            LoadProducts();
            ProductNameTextBox.Clear();
            
            MessageBox.Show("Продукт добавлен!", 
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Удалить продукт
    /// </summary>
    private void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var product = button?.DataContext as ProductView;
            if (product == null) return;

            var result = MessageBox.Show(
                "Вы действительно хотите удалить продукт?", 
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (!_productRepository.DeleteProduct(product.ID))
                {
                    var deleteWithComponents = MessageBox.Show(
                        "Продукт используется в блюдах. Удалить продукт со всеми связями?", 
                        "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    
                    if (deleteWithComponents == MessageBoxResult.Yes)
                    {
                        _productRepository.DeleteProductWithComponents(product.ID);
                    }
                    else
                    {
                        return;
                    }
                }
                
                LoadProducts();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Поиск продуктов
    /// </summary>
    private void ProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = ProductSearchBox.Text.ToLower();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            AvailableProductsList.ItemsSource = _allProducts;
        }
        else
        {
            var filtered = _allProducts.Where(p => 
                p.Name.ToLower().Contains(searchText)).ToList();
            AvailableProductsList.ItemsSource = filtered;
        }
    }

    /// <summary>
    /// Валидация ввода только чисел (для веса и количества)
    /// </summary>
    private void NumericOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Разрешаем только цифры и десятичную точку/запятую
        var textBox = sender as TextBox;
        if (textBox == null) return;

        var text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
        
        // Проверяем, что можно распарсить как число
        e.Handled = !decimal.TryParse(text.Replace(',', '.'), 
            System.Globalization.NumberStyles.AllowDecimalPoint, 
            System.Globalization.CultureInfo.InvariantCulture, 
            out _);
    }
}

/// <summary>
/// Простой диалог для ввода веса
/// </summary>
public class WeightInputDialog : Window
{
    private TextBox _weightTextBox;
    public decimal Weight { get; private set; }

    public WeightInputDialog()
    {
        Title = "Введите вес";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Background = System.Windows.Media.Brushes.White;

        var stackPanel = new StackPanel { Margin = new Thickness(20) };
        
        var label = new TextBlock 
        { 
            Text = "Введите вес (в граммах):", 
            Margin = new Thickness(0, 0, 0, 10),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        };
        
        _weightTextBox = new TextBox 
        { 
            Margin = new Thickness(0, 0, 0, 10),
            Height = 35,
            FontSize = 14,
            Padding = new Thickness(8),
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(2),
            BorderBrush = System.Windows.Media.Brushes.LightGray
        };
        
        // Устанавливаем фокус на TextBox при загрузке
        _weightTextBox.Loaded += (s, e) => _weightTextBox.Focus();
        
        // Разрешаем только цифры
        _weightTextBox.PreviewTextInput += (s, e) =>
        {
            var textBox = s as TextBox;
            if (textBox == null) return;
            var text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !decimal.TryParse(text.Replace(',', '.'), 
                System.Globalization.NumberStyles.AllowDecimalPoint, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out _);
        };
        
        stackPanel.Children.Add(label);
        stackPanel.Children.Add(_weightTextBox);
        
        Grid.SetRow(stackPanel, 0);
        grid.Children.Add(stackPanel);

        var buttonPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20, 0, 20, 20)
        };
        
        var okButton = new Button 
        { 
            Content = "OK", 
            Width = 100, 
            Height = 35,
            Margin = new Thickness(0, 0, 10, 0),
            FontSize = 14,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0)
        };
        okButton.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_weightTextBox.Text))
            {
                MessageBox.Show("Введите вес!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (decimal.TryParse(_weightTextBox.Text.Replace(',', '.'), 
                System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture,
                out var weight) && weight > 0)
            {
                Weight = weight;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введите корректное положительное число!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        
        var cancelButton = new Button 
        { 
            Content = "Отмена", 
            Width = 100,
            Height = 35,
            FontSize = 14
        };
        cancelButton.Click += (s, e) => DialogResult = false;
        
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        
        Grid.SetRow(buttonPanel, 1);
        grid.Children.Add(buttonPanel);

        Content = grid;
        
        // Обработка Enter для подтверждения
        _weightTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        };
    }
}

