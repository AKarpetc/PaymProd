using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace PaymProdNet9.Pages;

public partial class DelicatesPage : Page
{
    private readonly DelicateRepository _delicateRepository;
    private readonly ProductRepository _productRepository;
    
    private ObservableCollection<DelicatesColl> _allDelicates;
    private ObservableCollection<ProductView> _allProducts;
    private ObservableCollection<Components> _currentDelicateComponents;
    
    private int? _currentDelicateId;
    private bool _isEditMode; // true = редактирование, false = создание

    public DelicatesPage()
    {
        InitializeComponent();
        
        _delicateRepository = new DelicateRepository();
        _productRepository = new ProductRepository();
        
        _allDelicates = new ObservableCollection<DelicatesColl>();
        _allProducts = new ObservableCollection<ProductView>();
        _currentDelicateComponents = new ObservableCollection<Components>();
        
        DelicateComponentsGrid.ItemsSource = _currentDelicateComponents;
        
        // Подписываемся на событие Loaded для установки обработчика навигации
        this.Loaded += DelicatesPage_LoadedInternal;
    }
    
    /// <summary>
    /// Обработчик загрузки страницы для установки навигационного обработчика
    /// </summary>
    private void DelicatesPage_LoadedInternal(object sender, RoutedEventArgs e)
    {
        // Подписываемся на событие навигации Frame
        if (NavigationService != null)
        {
            NavigationService.Navigating -= DelicatesPage_Navigating; // Отписываемся, если уже подписаны
            NavigationService.Navigating += DelicatesPage_Navigating;
        }
    }
    
    /// <summary>
    /// Обработка навигации назад - работает как "Отмена" в режиме редактирования
    /// </summary>
    private void DelicatesPage_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        // Если открыт режим редактирования и пользователь нажал "Назад"
        if (EditViewPanel.Visibility == Visibility.Visible && e.NavigationMode == NavigationMode.Back)
        {
            // Отменяем навигацию
            e.Cancel = true;
            
            // Вызываем метод отмены (возвращаемся к списку)
            ShowListView();
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadDelicates();
        LoadProducts();
        LoadDelicateTypes();
    }

    /// <summary>
    /// Загрузка всех блюд (новые вверху)
    /// </summary>
    private void LoadDelicates()
    {
        try
        {
            _allDelicates.Clear();
            var delicates = _delicateRepository.GetAllDelicates()
                .OrderByDescending(d => d.Id) // Новые блюда вверху
                .ToList();
            
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
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов блюд: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Переключение в режим просмотра списка
    /// </summary>
    private void ShowListView()
    {
        ListViewPanel.Visibility = Visibility.Visible;
        EditViewPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Переключение в режим создания/редактирования
    /// </summary>
    private void ShowEditView(bool isEdit)
    {
        _isEditMode = isEdit;
        
        ListViewPanel.Visibility = Visibility.Collapsed;
        EditViewPanel.Visibility = Visibility.Visible;
        
        if (isEdit)
        {
            EditPanelTitle.Text = "Редактирование блюда";
            SaveButton.Content = "💾 Сохранить изменения";
        }
        else
        {
            EditPanelTitle.Text = "Создание нового блюда";
            SaveButton.Content = "💾 Создать блюдо";
        }
    }

    /// <summary>
    /// Кнопка "Создать новое блюдо"
    /// </summary>
    private void NewDelicate_Click(object sender, RoutedEventArgs e)
    {
        _currentDelicateId = null;
        DelicateNameTextBox.Clear();
        DelicateWeightTextBox.Clear();
        DelicateCountTextBox.Clear();
        DelicateTypeComboBox.SelectedIndex = -1;
        _currentDelicateComponents.Clear();
        
        ShowEditView(false);
        DelicateNameTextBox.Focus();
    }

    /// <summary>
    /// Кнопка "Редактировать блюдо"
    /// </summary>
    private void EditDelicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var delicate = button?.DataContext as DelicatesColl;
            if (delicate == null) return;

            _currentDelicateId = delicate.Id;
            
            DelicateNameTextBox.Text = delicate.Name;
            DelicateWeightTextBox.Text = delicate.Ves.ToString();
            DelicateCountTextBox.Text = delicate.Count.ToString();
            
            // Устанавливаем тип
            var types = _delicateRepository.GetDelicateTypes();
            var typeToSelect = types.FirstOrDefault(t => t.Name == delicate.Type);
            if (typeToSelect != null && typeToSelect.Id > 0)
            {
                DelicateTypeComboBox.SelectedValue = typeToSelect.Id;
            }
            
            // Загружаем компоненты
            LoadDelicateComponents(delicate.Id);
            
            ShowEditView(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

    /// <summary>
    /// Кнопка "Сохранить" (создать или обновить)
    /// </summary>
    private void SaveDelicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(DelicateNameTextBox.Text))
            {
                MessageBox.Show("Введите название блюда!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                DelicateNameTextBox.Focus();
                return;
            }

            if (DelicateTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип блюда!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                DelicateTypeComboBox.Focus();
                return;
            }

            var typeId = (int)DelicateTypeComboBox.SelectedValue;
            var ves = decimal.TryParse(DelicateWeightTextBox.Text, out var w) ? w : 0;
            var count = decimal.TryParse(DelicateCountTextBox.Text, out var c) ? c : 1;

            if (_isEditMode && _currentDelicateId.HasValue)
            {
                // Обновление существующего блюда
                _delicateRepository.UpdateDelicate(
                    _currentDelicateId.Value, typeId, DelicateNameTextBox.Text, ves, count);
                
                // Сохранение компонентов
                SaveDelicateComponents(_currentDelicateId.Value);
                
                MessageBox.Show("Блюдо успешно обновлено!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Создание нового блюда
                var newId = _delicateRepository.AddDelicate(typeId, DelicateNameTextBox.Text, ves, count);
                _currentDelicateId = newId;
                
                // Сохранение компонентов
                if (_currentDelicateComponents.Count > 0)
                {
                    SaveDelicateComponents(newId);
                }
                
                MessageBox.Show("Блюдо успешно создано!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadDelicates();
            ShowListView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Сохранение компонентов блюда
    /// </summary>
    private void SaveDelicateComponents(int delicateId)
    {
        try
        {
            // Удаляем старые компоненты
            var existing = _delicateRepository.GetDelicateById(delicateId);
            if (existing?.Lcomp != null)
            {
                foreach (var component in existing.Lcomp)
                {
                    _delicateRepository.DeleteComponentByProductAndDelicate(component.Prodid, delicateId);
                }
            }
            
            // Добавляем новые компоненты
            foreach (var component in _currentDelicateComponents)
            {
                if (component.Ves > 0) // Только если указан вес
                {
                    _delicateRepository.AddComponent(delicateId, component.Prodid, component.Ves);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении состава блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Кнопка "Отмена"
    /// </summary>
    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Отменить изменения и вернуться к списку?", 
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            ShowListView();
        }
    }

    /// <summary>
    /// Удаление блюда
    /// </summary>
    private void DeleteDelicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var delicate = button?.DataContext as DelicatesColl;
            if (delicate == null) return;

            var result = MessageBox.Show($"Удалить блюдо \"{delicate.Name}\"?", 
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _delicateRepository.DeleteDelicate(delicate.Id);
                LoadDelicates();
                
                MessageBox.Show("Блюдо успешно удалено!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении блюда: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Добавление продукта в состав
    /// </summary>
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

    /// <summary>
    /// Удаление продукта из состава
    /// </summary>
    private void RemoveProductFromDelicate_Click(object sender, RoutedEventArgs e)
    {
        if (DelicateComponentsGrid.SelectedItem is Components selectedComponent)
        {
            _currentDelicateComponents.Remove(selectedComponent);
        }
        else
        {
            MessageBox.Show("Выберите продукт из состава для удаления", "Внимание", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
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
    /// Валидация числового ввода
    /// </summary>
    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextNumeric(e.Text);
    }

    private static bool IsTextNumeric(string text)
    {
        return text.All(c => char.IsDigit(c) || c == ',' || c == '.');
    }
}
