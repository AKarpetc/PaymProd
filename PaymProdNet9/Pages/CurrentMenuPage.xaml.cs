using Microsoft.Win32;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PaymProdNet9.Pages;

public partial class CurrentMenuPage : Page
{
    private readonly MenuRepository _menuRepository;
    private readonly DelicateRepository _delicateRepository;
    private readonly ProductRepository _productRepository;

    private int? _currentMenuId;
    private ObservableCollection<MenuDel_act> _currentMenuDelicates;
    private ObservableCollection<dynamic> _availableDelicates;
    private string _currentTypeFilter = "%";
    private bool _isDataChanged = false;

    // Для редактирования блюда в меню
    private int? _editingDelicateId;
    private MenuDel_act? _editingDelicate;
    private ObservableCollection<Components> _editingDelicateComponents;
    private ObservableCollection<ProductView> _editingAvailableProducts;
    private Dictionary<int, ProductView> _productLookup;
    private Dictionary<string, Measure> _measureLookup;

    public CurrentMenuPage()
    {
        InitializeComponent();

        _menuRepository = new MenuRepository();
        _delicateRepository = new DelicateRepository();
        _productRepository = new ProductRepository();

        _currentMenuDelicates = new ObservableCollection<MenuDel_act>();
        _availableDelicates = new ObservableCollection<dynamic>();
        _editingDelicateComponents = new ObservableCollection<Components>();
        _editingAvailableProducts = new ObservableCollection<ProductView>();
        _productLookup = new Dictionary<int, ProductView>();
        _measureLookup = new Dictionary<string, Measure>(StringComparer.OrdinalIgnoreCase);

        MenuDelicatesDataGrid.ItemsSource = _currentMenuDelicates;
        EditDelicateComponentsGrid.ItemsSource = _editingDelicateComponents;
        EditAvailableProductsList.ItemsSource = _editingAvailableProducts;
    }

    private void RefreshLookups(bool force = false)
    {
        if (force || _measureLookup.Count == 0)
        {
            var measures = _productRepository.GetMeasures();
            // Обрабатываем дубликаты - берем первую меру с таким названием
            _measureLookup = measures
                .GroupBy(m => m.Name.ToLower().Trim())
                .ToDictionary(g => g.Key, g => g.First());
        }

        if (force || _productLookup.Count == 0)
        {
            var products = _productRepository.GetAllProducts();
            _productLookup = products.ToDictionary(p => p.ID, p => p);
        }
    }

    private int GetMenuPrecision(string? measureName)
    {
        if (string.IsNullOrWhiteSpace(measureName)) return 2;

        RefreshLookups();
        var key = measureName.ToLower().Trim();
        return _measureLookup.TryGetValue(key, out var measure)
            ? measure.MenuRoundingPrecision
            : 2;
    }

    private void ApplyMenuRoundingInfo(IEnumerable<Components> components)
    {
        RefreshLookups();

        foreach (var component in components)
        {
            if (_productLookup.TryGetValue(component.Prodid, out var product))
            {
                if (!string.IsNullOrWhiteSpace(product.Ves))
                    component.Mera = product.Ves;

                if (string.IsNullOrWhiteSpace(component.FassIz) && !string.IsNullOrWhiteSpace(product.IzName))
                    component.FassIz = product.IzName;
            }

            component.MenuRoundingPrecision = GetMenuPrecision(component.Mera);
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshLookups(true);
            LoadDelicateTypes();

            // Проверяем открытое меню
            var openMenu = _menuRepository.GetOpenMenu();
            if (openMenu != null) LoadMenu(openMenu.Id);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при загрузке данных", ex);
            MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка типов блюд для фильтрации
    /// </summary>
    private void LoadDelicateTypes()
    {
        try
        {
            var panel = AllTypesButton.Parent as StackPanel;
            if (panel == null) return;

            // Очищаем все кнопки кроме "Все"
            var buttonsToRemove = panel.Children.Cast<UIElement>()
                .Where(c => c != AllTypesButton).ToList();
            foreach (var button in buttonsToRemove) panel.Children.Remove(button);

            var types = _delicateRepository.GetDelicateTypes();
            foreach (var type in types)
            {
                var button = new Button
                {
                    Content = type.Name,
                    Tag = type.Name,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                button.Click += FilterDelicates_Click;
                button.SetResourceReference(StyleProperty, "MaterialDesignRaisedButton");
                panel.Children.Add(button);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов блюд: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка меню
    /// </summary>
    private void LoadMenu(int menuId)
    {
        try
        {
            ShowLoading(true);

            _currentMenuId = menuId;
            var menu = _menuRepository.GetOpenMenu();
            if (menu == null) return;

            // Заполняем информацию о банкете
            BanquetNameTextBox.Text = menu.Name;
            PeopleCountTextBox.Text = menu.CountP.ToString();
            DescriptionTextBox.Text = menu.Detail;

            if (DateTime.TryParse(menu.DateBan, out var date)) BanquetDatePicker.SelectedDate = date;

            // Загружаем блюда меню
            _currentMenuDelicates.Clear();
            var menuDelicates = _menuRepository.GetMenuDelicates(menuId);
            foreach (var item in menuDelicates) _currentMenuDelicates.Add(item);

            CurrentMenuInfo.Text = $"Банкет: {menu.Name} - {menu.CountP} человек, дата - {menu.DateBan}";

            // Включаем панель добавления блюд
            DelicatesPanel.IsEnabled = true;

            // Загружаем доступные блюда
            LoadAvailableDelicates("%");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке меню: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    /// <summary>
    /// Загрузка доступных блюд для добавления
    /// </summary>
    private void LoadAvailableDelicates(string typeFilter)
    {
        try
        {
            var delicates = _delicateRepository.GetAvailableDelicatesForMenu(typeFilter);

            // Получаем список ID блюд и продуктов, уже добавленных в текущее меню
            var addedDelicateIds = new HashSet<int>();
            if (_currentMenuId.HasValue)
            {
                var menuDelicates = _menuRepository.GetMenuDelicates(_currentMenuId.Value);
                foreach (var md in menuDelicates) addedDelicateIds.Add(md.Del_id);
            }

            // Исключаем уже добавленные блюда и продукты
            var availableDelicates = delicates.Where(d => !addedDelicateIds.Contains(d.Id)).ToList();

            // Конвертируем в формат для отображения
            _availableDelicates.Clear();
            var displayDelicates = availableDelicates.Select(d => new
            {
                Del = d.Name,
                Sost = d.Lcomp.Any()
                    ? "Состав: " + string.Join(", ", d.Lcomp.Select(c => c.Name))
                    : "Без состава",
                WeightInfo = d.Ves > 0 ? $"{d.Ves}г" : d.Count > 0 ? "Порция" : "",
                DelicateId = d.Id,
                DefaultCount = d.LinkedProductDefaultCount.HasValue
                    ? d.LinkedProductDefaultCount.Value.ToString(CultureInfo.CurrentCulture)
                    : PeopleCountTextBox.Text
            }).ToList();

            foreach (var item in displayDelicates) _availableDelicates.Add(item);

            AvailableDelicatesPanel.ItemsSource = _availableDelicates;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Фильтрация блюд по типу
    /// </summary>
    private void FilterDelicates_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button?.Tag == null) return;

        _currentTypeFilter = button.Tag.ToString() ?? "%";
        LoadAvailableDelicates(_currentTypeFilter);
    }

    /// <summary>
    /// Начать заполнение меню
    /// </summary>
    private void StartMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BanquetNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PeopleCountTextBox.Text))
            {
                MessageBox.Show("Заполните название банкета и количество человек!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentMenuId.HasValue)
            {
                MessageBox.Show("Сначала закончите данное меню и нажмите 'Начать новое'!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем новое меню
            var menuId = _menuRepository.CreateMenu(
                BanquetNameTextBox.Text,
                int.Parse(PeopleCountTextBox.Text),
                DescriptionTextBox.Text,
                BanquetDatePicker.SelectedDate?.ToString() ?? DateTime.Now.ToString()
            );

            LoadMenu(menuId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании меню: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Начать новое меню
    /// </summary>
    private void NewMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentMenuId.HasValue) _menuRepository.CloseMenu(_currentMenuId.Value);

            // Очищаем форму
            BanquetNameTextBox.Clear();
            PeopleCountTextBox.Clear();
            DescriptionTextBox.Clear();
            BanquetDatePicker.SelectedDate = DateTime.Now;

            _currentMenuId = null;
            _currentMenuDelicates.Clear();
            CurrentMenuInfo.Text = "Выберите или создайте новое меню";
            DelicatesPanel.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Двойной клик по карточке блюда - добавление в меню
    /// </summary>
    private void DelicateCard_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue) return;

            var card = sender as FrameworkElement;
            if (card == null) return;

            var data = card.DataContext as dynamic;
            if (data == null) return;

            // Находим TextBox с количеством
            var textBox = FindVisualChild<TextBox>(card);
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text)) return;

            if (!int.TryParse(textBox.Text, out var count) || count <= 0)
            {
                MessageBox.Show("Некорректно введено количество!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Добавляем блюдо в меню
            var delicateId = (int)data.DelicateId;
            Services.Logger.Debug($"Попытка добавить блюдо в меню: MenuId={_currentMenuId.Value}, DelicateId={delicateId}, Count={count}");
            _menuRepository.AddDelicateToMenu(_currentMenuId.Value, delicateId, count);
            Services.Logger.Debug($"Блюдо успешно добавлено в меню: DelicateId={delicateId}");

            // Удаляем блюдо из списка доступных
            var itemToRemove = _availableDelicates.FirstOrDefault(d => (int)d.DelicateId == delicateId);
            if (itemToRemove != null) _availableDelicates.Remove(itemToRemove);

            // Очищаем поле количества
            textBox.Clear();

            // Обновляем список меню
            LoadMenu(_currentMenuId.Value);
            _isDataChanged = true;
        }
        catch (Exception ex)
        {
            Services.Logger.Error("Ошибка при добавлении блюда в меню", ex);
            MessageBox.Show($"Ошибка при добавлении блюда: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Обработчик загрузки TextBox количества - устанавливает значение по умолчанию из количества человек
    /// </summary>
    private void QuantityTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        // Устанавливаем значение по умолчанию только если поле пустое
        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            // Получаем значение из PeopleCountTextBox
            if (!string.IsNullOrWhiteSpace(PeopleCountTextBox.Text) &&
                int.TryParse(PeopleCountTextBox.Text, out var peopleCount) && peopleCount > 0)
            {
                textBox.Text = peopleCount.ToString();
                
                // Включаем кнопку добавления, так как значение установлено
                var parent = textBox.Parent as FrameworkElement;
                if (parent != null)
                {
                    var addButton = FindAddButton(parent);
                    if (addButton != null)
                    {
                        addButton.IsEnabled = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Обработчик изменения текста в поле количества - включает/выключает кнопку добавления
    /// </summary>
    private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        // Находим родительский элемент (StackPanel)
        var parent = textBox.Parent as FrameworkElement;
        if (parent == null) return;

        // Находим кнопку добавления (с иконкой Plus) в том же StackPanel
        var addButton = FindAddButton(parent);
        if (addButton != null)
        {
            // Включаем кнопку только если введено число больше 0
            var hasValidValue = !string.IsNullOrWhiteSpace(textBox.Text) &&
                                int.TryParse(textBox.Text, out var count) && count > 0;
            addButton.IsEnabled = hasValidValue;
        }
    }

    /// <summary>
    /// Поиск кнопки добавления (с иконкой Plus) в визуальном дереве
    /// </summary>
    private Button? FindAddButton(DependencyObject parent)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is Button button)
            {
                // Проверяем, есть ли в кнопке PackIcon с Kind="Plus"
                var packIcon = FindVisualChild<MaterialDesignThemes.Wpf.PackIcon>(button);
                if (packIcon != null && packIcon.Kind == MaterialDesignThemes.Wpf.PackIconKind.Plus) return button;
            }

            var result = FindAddButton(child);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// Добавление блюда в меню по кнопке плюсика
    /// </summary>
    private void AddDelicateToMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue)
            {
                MessageBox.Show("Сначала создайте или откройте меню!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button == null) return;

            var data = button.DataContext as dynamic;
            if (data == null) return;

            // Находим родительский элемент (StackPanel)
            var parent = button.Parent as FrameworkElement;
            if (parent == null) return;

            // Находим TextBox с количеством
            var textBox = FindVisualChild<TextBox>(parent);
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
            {
                MessageBox.Show("Введите количество!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(textBox.Text, out var count) || count <= 0)
            {
                MessageBox.Show("Некорректно введено количество!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Добавляем блюдо в меню
            var delicateId = (int)data.DelicateId;
            _menuRepository.AddDelicateToMenu(_currentMenuId.Value, delicateId, count);

            // Удаляем блюдо из списка доступных
            var itemToRemove = _availableDelicates.FirstOrDefault(d => (int)d.DelicateId == delicateId);
            if (itemToRemove != null) _availableDelicates.Remove(itemToRemove);

            // Очищаем поле количества
            textBox.Clear();
            button.IsEnabled = false;

            // Обновляем список меню
            LoadMenu(_currentMenuId.Value);
            _isDataChanged = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении блюда: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Удаление блюда из меню
    /// </summary>
    private void DeleteDelicateFromMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var delicate = button?.DataContext as MenuDel_act;
            if (delicate == null) return;

            var result = MessageBox.Show("Удалить блюдо из меню?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var delicateId = delicate.Del_id;

                _menuRepository.RemoveDelicateFromMenu(delicate.Idmen);
                _currentMenuDelicates.Remove(delicate);

                // Возвращаем блюдо в список доступных
                ReturnDelicateToAvailableList(delicateId);

                _isDataChanged = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении блюда: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Возвращает блюдо в список доступных после удаления из меню
    /// </summary>
    private void ReturnDelicateToAvailableList(int delicateId)
    {
        try
        {
            // Проверяем, не добавлено ли уже это блюдо
            var exists = _availableDelicates.Any(d => (int)d.DelicateId == delicateId);
            if (exists) return;

            // Получаем информацию о блюде
            var delicate = _delicateRepository.GetDelicateById(delicateId);
            if (delicate == null) return;

            // Получаем текущий фильтр типа
            var currentFilter = GetCurrentTypeFilter();

            // Проверяем, соответствует ли блюдо текущему фильтру
            if (!string.IsNullOrEmpty(currentFilter) && currentFilter != "%")
            {
                if (delicate.IDType.HasValue)
                {
                    var delicateType = _delicateRepository.GetDelicateTypes()
                        .FirstOrDefault(t => t.Id == delicate.IDType.Value);
                    if (delicateType == null ||
                        delicateType.Name != currentFilter) return; // Блюдо не соответствует текущему фильтру
                }
                else
                {
                    return; // У блюда нет типа
                }
            }

            // Создаем объект для отображения
            var displayDelicate = new
            {
                Del = delicate.Name,
                Sost = delicate.Lcomp != null && delicate.Lcomp.Any()
                    ? "Состав: " + string.Join(", ", delicate.Lcomp.Select(c => c.Name))
                    : "Без состава",
                WeightInfo = delicate.Ves > 0 ? $"{delicate.Ves}г" : delicate.Count > 0 ? "Порция" : "",
                DelicateId = delicate.Id,
                DefaultCount = delicate.LinkedProductDefaultCount.HasValue
                    ? delicate.LinkedProductDefaultCount.Value.ToString(CultureInfo.CurrentCulture)
                    : PeopleCountTextBox.Text
            };

            _availableDelicates.Add(displayDelicate);
        }
        catch (Exception ex)
        {
            // Игнорируем ошибки при возврате блюда в список
            System.Diagnostics.Debug.WriteLine($"Ошибка при возврате блюда в список: {ex.Message}");
        }
    }

    /// <summary>
    /// Получает текущий активный фильтр типа блюд
    /// </summary>
    private string GetCurrentTypeFilter()
    {
        return _currentTypeFilter;
    }

    /// <summary>
    /// Редактирование блюда в меню
    /// </summary>
    private void EditDelicateInMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue)
            {
                MessageBox.Show("Сначала создайте или откройте меню!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            var delicate = button?.DataContext as MenuDel_act;
            if (delicate == null) return;

            // Продукты (ID < 0) нельзя редактировать
            if (delicate.Del_id < 0)
            {
                MessageBox.Show("Этот продукт доступен только для чтения. Для редактирования используйте справочник продуктов.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _editingDelicateId = delicate.Del_id;
            _editingDelicate = delicate;

            // Загружаем название блюда
            EditDelicateNameText.Text = delicate.Del;
            EditDelicateTitle.Text = $"Редактирование блюда: {delicate.Del}";

            // Загружаем количество
            EditDelicateCountTextBox.Text = delicate.Countpor.ToString();

            // Загружаем компоненты блюда для этого меню
            LoadEditingDelicateComponents();

            // Загружаем доступные продукты
            LoadEditingAvailableProducts();

            // Показываем панель редактирования
            EditDelicateInMenuPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии редактирования: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка компонентов редактируемого блюда
    /// </summary>
    private void LoadEditingDelicateComponents()
    {
        try
        {
            _editingDelicateComponents.Clear();

            if (!_currentMenuId.HasValue || !_editingDelicateId.HasValue) return;

            // Получаем компоненты для этого меню (из Components1 или Components)
            var components = _menuRepository.GetMenuDelicateComponents(
                _currentMenuId.Value,
                _editingDelicateId.Value);

            ApplyMenuRoundingInfo(components);

            foreach (var component in components) _editingDelicateComponents.Add(component);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке компонентов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка доступных продуктов для редактирования
    /// </summary>
    private void LoadEditingAvailableProducts()
    {
        try
        {
            RefreshLookups();
            _editingAvailableProducts.Clear();
            foreach (var product in _productLookup.Values.OrderBy(p => p.Name))
                _editingAvailableProducts.Add(product);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Поиск продуктов при редактировании
    /// </summary>
    private void EditProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = EditProductSearchBox.Text.ToLower();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            LoadEditingAvailableProducts();
            return;
        }

        try
        {
            _editingAvailableProducts.Clear();
            RefreshLookups();
            var filtered = _productLookup.Values.Where(p =>
                p.Name.ToLower().Contains(searchText) ||
                (p.Type != null && p.Type.ToLower().Contains(searchText))
            );

            foreach (var product in filtered) _editingAvailableProducts.Add(product);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при поиске продуктов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Добавление продукта в состав редактируемого блюда
    /// </summary>
    private void AddProductToEditDelicate_Click(object sender, RoutedEventArgs e)
    {
        if (EditAvailableProductsList.SelectedItem is ProductView selectedProduct)
        {
            if (_editingDelicateComponents.Any(c => c.Prodid == selectedProduct.ID))
            {
                MessageBox.Show("Этот продукт уже добавлен в состав", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RefreshLookups();

            var component = new Components
            {
                Prodid = selectedProduct.ID,
                NameT = selectedProduct.Name,
                Ves = 0,
                Mera = string.IsNullOrWhiteSpace(selectedProduct.Ves) ? selectedProduct.IzName : selectedProduct.Ves,
                FassIz = selectedProduct.IzName,
                MenuRoundingPrecision = GetMenuPrecision(
                    string.IsNullOrWhiteSpace(selectedProduct.Ves) ? selectedProduct.IzName : selectedProduct.Ves)
            };

            _editingDelicateComponents.Add(component);
        }
        else
        {
            MessageBox.Show("Выберите продукт из списка", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Удаление продукта из состава редактируемого блюда
    /// </summary>
    private void RemoveProductFromEditDelicate_Click(object sender, RoutedEventArgs e)
    {
        if (EditDelicateComponentsGrid.SelectedItem is Components selectedComponent)
            _editingDelicateComponents.Remove(selectedComponent);
        else
            MessageBox.Show("Выберите продукт из состава", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Сохранение изменений блюда в меню
    /// </summary>
    private void SaveEditDelicateInMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue || !_editingDelicateId.HasValue)
            {
                MessageBox.Show("Ошибка: не выбрано меню или блюдо",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Сохраняем измененные компоненты в Components1
            _menuRepository.SaveMenuDelicateComponents(
                _currentMenuId.Value,
                _editingDelicateId.Value,
                _editingDelicateComponents.ToList());

            // Сохраняем количество, если оно было изменено
            if (int.TryParse(EditDelicateCountTextBox.Text, out var newCount) && newCount > 0)
            {
                _menuRepository.UpdateMenuDelicateCount(
                    _currentMenuId.Value,
                    _editingDelicateId.Value,
                    newCount);
            }

            // Закрываем панель редактирования
            EditDelicateInMenuPanel.Visibility = Visibility.Collapsed;

            // Обновляем список меню
            LoadMenu(_currentMenuId.Value);
            _isDataChanged = true;

            MessageBox.Show("Изменения сохранены! Блюдо помечено как измененное в этом меню.",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Отмена редактирования блюда в меню
    /// </summary>
    private void CancelEditDelicateInMenu_Click(object sender, RoutedEventArgs e)
    {
        EditDelicateInMenuPanel.Visibility = Visibility.Collapsed;
        _editingDelicateId = null;
        _editingDelicate = null;
        _editingDelicateComponents.Clear();
        EditProductSearchBox.Clear();
    }

    /// <summary>
    /// Обновление информации о банкете
    /// </summary>
    private void BanquetInfo_LostFocus(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue || !DelicatesPanel.IsEnabled) return;

            _menuRepository.UpdateMenu(
                _currentMenuId.Value,
                BanquetNameTextBox.Text,
                int.Parse(PeopleCountTextBox.Text),
                DescriptionTextBox.Text,
                BanquetDatePicker.SelectedDate?.ToString() ?? DateTime.Now.ToString()
            );

            CurrentMenuInfo.Text =
                $"Банкет: {BanquetNameTextBox.Text} - {PeopleCountTextBox.Text} человек, дата - {BanquetDatePicker.SelectedDate?.ToString() ?? DateTime.Now.ToString()}";
        }
        catch
        {
        }
    }

    private void BanquetInfo_Changed(object sender, SelectionChangedEventArgs e)
    {
        BanquetInfo_LostFocus(sender, e);
    }

    /// <summary>
    /// Валидация числового ввода
    /// </summary>
    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextNumeric(e.Text);
    }

    private static bool IsTextNumeric(string text)
    {
        return text.All(c => char.IsDigit(c) || c == ',' || c == '.');
    }

    /// <summary>
    /// Сохранение изменений в меню
    /// </summary>
    private void SaveMenuChanges_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue)
            {
                MessageBox.Show("Нет открытого меню!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowLoading(true);
            _menuRepository.SaveMenuChanges(_currentMenuId.Value, _currentMenuDelicates);
            _isDataChanged = false;

            MessageBox.Show("Изменения сохранены!",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    /// <summary>
    /// Обновление меню
    /// </summary>
    private void RefreshMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_isDataChanged)
        {
            var result = MessageBox.Show(
                "Хотите ли вы сохранить изменения внесенные в меню? После обновления все не сохраненные изменения будут стерты.",
                "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                SaveMenuChanges_Click(sender, e);
            else if (result == MessageBoxResult.Cancel) return;
        }

        if (_currentMenuId.HasValue) LoadMenu(_currentMenuId.Value);
    }


    /// <summary>
    /// Показать/скрыть индикатор загрузки
    /// </summary>
    private void ShowLoading(bool show)
    {
        LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Открыть окно редактирования цен для меню
    /// </summary>
    private void EnterMenuPrices_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue)
            {
                MessageBox.Show("Сначала создайте или откройте меню!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var pricesPage = new ProductPricesPage(_currentMenuId.Value);
            Services.NavigationService.Instance.NavigateTo(pricesPage);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии окна цен: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Поиск визуального потомка определенного типа
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// Валидация числового ввода
    /// </summary>
    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
    }
}