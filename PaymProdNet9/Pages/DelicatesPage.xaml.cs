using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PaymProdNet9.Pages;

public partial class DelicatesPage : Page
{
    private readonly DelicateRepository _delicateRepository;
    private readonly ProductRepository _productRepository;
    
    private ObservableCollection<DelicatesColl> _allDelicates;
    private ObservableCollection<ProductView> _allProducts;
    private ObservableCollection<Components> _currentDelicateComponents;
    
    private int? _currentDelicateId;

    public DelicatesPage()
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
        LoadDelicates();
        LoadProducts();
        LoadDelicateTypes();
    }

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
            AvailableProductsList.ItemsSource = _allProducts;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadDelicateTypes()
    {
        try
        {
            var types = _delicateRepository.GetDelicateTypes();
            DelicateTypeComboBox.ItemsSource = types;
            DelicateTypeComboBox.DisplayMemberPath = "Name";
            DelicateTypeComboBox.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов блюд: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DelicatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DelicatesDataGrid.SelectedItem is DelicatesColl selectedDelicate)
        {
            _currentDelicateId = selectedDelicate.Id;
            DelicateEditPanel.IsEnabled = true;
            
            DelicateNameTextBox.Text = selectedDelicate.Name;
            DelicateWeightTextBox.Text = selectedDelicate.Ves.ToString();
            DelicateCountTextBox.Text = selectedDelicate.Count.ToString();
            
            var types = _delicateRepository.GetDelicateTypes();
            var typeToSelect = types.FirstOrDefault(t => t.Name == selectedDelicate.Type);
            if (typeToSelect.Id > 0)
            {
                DelicateTypeComboBox.SelectedValue = typeToSelect.Id;
            }
            
            LoadDelicateComponents(selectedDelicate.Id);
        }
    }

    private void LoadDelicateComponents(int delicateId)
    {
        try
        {
            _currentDelicateComponents.Clear();
            var delicate = _delicateRepository.GetDelicateById(delicateId);
            if (delicate?.Lcomp != null)
            {
                foreach (var component in delicate.Lcomp)
                {
                    _currentDelicateComponents.Add(component);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке состава блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewDelicate_Click(object sender, RoutedEventArgs e)
    {
        _currentDelicateId = null;
        DelicateNameTextBox.Clear();
        DelicateWeightTextBox.Clear();
        DelicateCountTextBox.Clear();
        DelicateTypeComboBox.SelectedIndex = -1;
        _currentDelicateComponents.Clear();
        DelicateEditPanel.IsEnabled = true;
        DelicateNameTextBox.Focus();
    }

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

    private void UpdateDelicate_Click(object sender, RoutedEventArgs e)
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
            var count = decimal.TryParse(DelicateCountTextBox.Text, out var c) ? c : 1;

            if (_currentDelicateId.HasValue)
            {
                _delicateRepository.UpdateDelicate(
                    _currentDelicateId.Value, typeId, DelicateNameTextBox.Text, ves, count);
                
                MessageBox.Show("Блюдо обновлено!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
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

    private void AddProductToDelicate_Click(object sender, RoutedEventArgs e)
    {
        if (AvailableProductsList.SelectedItem is ProductView selectedProduct)
        {
            if (_currentDelicateComponents.Any(c => c.Prodid == selectedProduct.ID))
            {
                MessageBox.Show("Этот продукт уже добавлен в состав", "Внимание", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var component = new Components
            {
                Prodid = selectedProduct.ID,
                NameT = selectedProduct.Name,
                Ves = 0,
                Mera = selectedProduct.IzName
            };

            _currentDelicateComponents.Add(component);
        }
        else
        {
            MessageBox.Show("Выберите продукт из списка", "Внимание", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveProductFromDelicate_Click(object sender, RoutedEventArgs e)
    {
        if (DelicateComponentsGrid.SelectedItem is Components selectedComponent)
        {
            _currentDelicateComponents.Remove(selectedComponent);
        }
        else
        {
            MessageBox.Show("Выберите продукт из состава", "Внимание", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GoToComposition_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDelicateComponents.Count == 0)
        {
            MessageBox.Show("Сначала добавьте продукты в состав блюда", "Внимание", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DelicateComponentsGrid.SelectedIndex = 0;
        DelicateComponentsGrid.Focus();
    }

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

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextNumeric(e.Text);
    }

    private static bool IsTextNumeric(string text)
    {
        return text.All(c => char.IsDigit(c) || c == ',' || c == '.');
    }
}
