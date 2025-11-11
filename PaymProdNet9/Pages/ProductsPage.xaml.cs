using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

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
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProducts();
        LoadProductTypes();
        LoadMeasures();
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
            ProductsDataGrid.ItemsSource = _allProducts;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}", 
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
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке единиц измерения: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsDataGrid.SelectedItem is ProductView selectedProduct)
        {
            _currentProductId = selectedProduct.ID;
            ProductEditPanel.IsEnabled = true;
            
            ProductNameTextBox.Text = selectedProduct.Name;
            
            var types = _productRepository.GetProductTypes();
            var typeToSelect = types.FirstOrDefault(t => t.Name == selectedProduct.Type);
            if (typeToSelect.Id > 0)
            {
                ProductTypeComboBox.SelectedValue = typeToSelect.Id;
            }
            
            var measures = _productRepository.GetMeasures();
            var measureToSelect = measures.FirstOrDefault(m => m.Name == selectedProduct.IzName);
            if (measureToSelect.Id > 0)
            {
                ProductMeasureComboBox.SelectedValue = measureToSelect.Id;
            }
        }
    }

    private void NewProduct_Click(object sender, RoutedEventArgs e)
    {
        _currentProductId = null;
        ProductEditPanel.IsEnabled = true;
        
        ProductNameTextBox.Clear();
        ProductTypeComboBox.SelectedIndex = -1;
        ProductMeasureComboBox.SelectedIndex = -1;
        
        ProductNameTextBox.Focus();
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
                
                if (_currentProductId == product.ID)
                {
                    _currentProductId = null;
                    ProductEditPanel.IsEnabled = false;
                }
                
                MessageBox.Show("Продукт удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateProduct_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("Выберите единицу измерения!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var typeId = (int)ProductTypeComboBox.SelectedValue;
            var measureId = (int)ProductMeasureComboBox.SelectedValue;

            if (_currentProductId.HasValue)
            {
                // Обновление существующего продукта
                _productRepository.UpdateProduct(
                    _currentProductId.Value, 
                    ProductNameTextBox.Text, 
                    0, // vesId
                    typeId, 
                    0, // fass
                    measureId, 
                    0, // prizMenu
                    0, // count
                    false, // automat
                    0, // countPeople
                    false // mainCount
                );
                
                MessageBox.Show("Продукт обновлен!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Создание нового продукта
                _productRepository.AddProduct(
                    ProductNameTextBox.Text, 
                    0, // vesId
                    typeId, 
                    0, // fass
                    measureId
                );
                
                MessageBox.Show("Продукт создан!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadProducts();
            NewProduct_Click(sender, e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
