using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PaymProdNet9.Pages;

public partial class ProductTypesPage : Page
{
    private readonly ProductRepository _productRepository;
    
    private ObservableCollection<ProductType> _allProductTypes;
    private int? _currentProductTypeId;

    public ProductTypesPage()
    {
        InitializeComponent();
        
        _productRepository = new ProductRepository();
        _allProductTypes = new ObservableCollection<ProductType>();
        
        ProductTypesDataGrid.ItemsSource = _allProductTypes;
        
        this.Loaded += ProductTypesPage_LoadedInternal;
    }
    
    private void ProductTypesPage_LoadedInternal(object sender, RoutedEventArgs e)
    {
        if (NavigationService != null)
        {
            NavigationService.Navigating -= ProductTypesPage_Navigating;
            NavigationService.Navigating += ProductTypesPage_Navigating;
        }
    }
    
    private void ProductTypesPage_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (ProductTypeEditView.Visibility == Visibility.Visible && e.NavigationMode == NavigationMode.Back)
        {
            e.Cancel = true;
            ShowListView();
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProductTypes();
        ShowListView();
    }
    
    private void ShowListView()
    {
        ProductTypesListView.Visibility = Visibility.Visible;
        ProductTypeEditView.Visibility = Visibility.Collapsed;
    }
    
    private void ShowEditView(bool isEdit)
    {
        ProductTypesListView.Visibility = Visibility.Collapsed;
        ProductTypeEditView.Visibility = Visibility.Visible;
        EditModeTitle.Text = isEdit ? "Редактирование типа продукта" : "Создание типа продукта";
        SaveButton.Content = isEdit ? "💾 Сохранить изменения" : "💾 Создать";
    }

    private void LoadProductTypes()
    {
        try
        {
            _allProductTypes.Clear();
            var types = _productRepository.GetProductTypes();
            foreach (var type in types)
            {
                _allProductTypes.Add(type);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов продуктов: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ProductTypeSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = ProductTypeSearchBox.Text.ToLower();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            LoadProductTypes();
            return;
        }
        
        try
        {
            _allProductTypes.Clear();
            var allTypes = _productRepository.GetProductTypes();
            var filtered = allTypes.Where(t => 
                t.Name.ToLower().Contains(searchText)
            );
            
            foreach (var type in filtered)
            {
                _allProductTypes.Add(type);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при поиске типов продуктов: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditProductType_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var productType = button?.DataContext as ProductType;
        if (productType == null) return;

        _currentProductTypeId = productType.Id;
        
        ProductTypeNameTextBox.Text = productType.Name;
        ProductTypeSortOrderTextBox.Text = productType.SortOrder.ToString();
        
        ShowEditView(true);
    }

    private void NewProductType_Click(object sender, RoutedEventArgs e)
    {
        _currentProductTypeId = null;
        
        ProductTypeNameTextBox.Clear();
        ProductTypeSortOrderTextBox.Text = "0";
        
        ShowEditView(false);
        ProductTypeNameTextBox.Focus();
    }
    
    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ShowListView();
    }

    private void DeleteProductType_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var productType = button?.DataContext as ProductType;
            if (productType == null) return;

            var result = MessageBox.Show($"Удалить тип продукта '{productType.Name}'?", 
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _productRepository.DeleteProductType(productType.Id);
                LoadProductTypes();
                MessageBox.Show("Тип продукта удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении типа продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveProductType_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProductTypeNameTextBox.Text))
            {
                MessageBox.Show("Введите название типа продукта!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ProductTypeSortOrderTextBox.Text, out int sortOrder))
            {
                sortOrder = 0;
            }

            if (_currentProductTypeId.HasValue)
            {
                _productRepository.UpdateProductType(
                    _currentProductTypeId.Value, 
                    ProductTypeNameTextBox.Text,
                    sortOrder);
                
                MessageBox.Show("Тип продукта обновлен!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _productRepository.AddProductType(ProductTypeNameTextBox.Text, sortOrder);
                
                MessageBox.Show("Тип продукта создан!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadProductTypes();
            ShowListView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении типа продукта: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NumericOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Разрешаем только цифры
        e.Handled = !e.Text.All(char.IsDigit);
    }
}

