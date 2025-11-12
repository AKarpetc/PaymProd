using Microsoft.Win32;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.ObjectModel;
using System.Globalization;
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

    public CurrentMenuPage()
    {
        InitializeComponent();
        
        _menuRepository = new MenuRepository();
        _delicateRepository = new DelicateRepository();
        _productRepository = new ProductRepository();
        
        _currentMenuDelicates = new ObservableCollection<MenuDel_act>();
        _availableDelicates = new ObservableCollection<dynamic>();
        MenuDelicatesDataGrid.ItemsSource = _currentMenuDelicates;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadDelicateTypes();
            
            // Проверяем открытое меню
            var openMenu = _menuRepository.GetOpenMenu();
            if (openMenu != null)
            {
                LoadMenu(openMenu.Id);
            }
        }
        catch (Exception ex)
        {
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
            foreach (var button in buttonsToRemove)
            {
                panel.Children.Remove(button);
            }

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
            
            if (DateTime.TryParse(menu.DateBan, out var date))
            {
                BanquetDatePicker.SelectedDate = date;
            }

            // Загружаем блюда меню
            _currentMenuDelicates.Clear();
            var menuDelicates = _menuRepository.GetMenuDelicates(menuId);
            foreach (var item in menuDelicates)
            {
                _currentMenuDelicates.Add(item);
            }

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
            
            // Получаем список ID блюд, уже добавленных в текущее меню
            var addedDelicateIds = new HashSet<int>();
            if (_currentMenuId.HasValue)
            {
                var menuDelicates = _menuRepository.GetMenuDelicates(_currentMenuId.Value);
                foreach (var md in menuDelicates)
                {
                    addedDelicateIds.Add(md.Del_id);
                }
            }
            
            // Исключаем уже добавленные блюда
            var availableDelicates = delicates.Where(d => !addedDelicateIds.Contains(d.Id)).ToList();
            
            // Получаем компоненты для каждого блюда
            foreach (var delicate in availableDelicates)
            {
                var delicateWithComponents = _delicateRepository.GetDelicateById(delicate.Id);
                if (delicateWithComponents != null)
                {
                    delicate.Lcomp = delicateWithComponents.Lcomp;
                }
            }

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
                DefaultCount = PeopleCountTextBox.Text
            }).ToList();

            foreach (var item in displayDelicates)
            {
                _availableDelicates.Add(item);
            }

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
            if (_currentMenuId.HasValue)
            {
                _menuRepository.CloseMenu(_currentMenuId.Value);
            }

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

            if (!int.TryParse(textBox.Text, out int count) || count <= 0)
            {
                MessageBox.Show("Некорректно введено количество!", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Добавляем блюдо в меню
            int delicateId = (int)data.DelicateId;
            _menuRepository.AddDelicateToMenu(_currentMenuId.Value, delicateId, count);
            
            // Удаляем блюдо из списка доступных
            var itemToRemove = _availableDelicates.FirstOrDefault(d => (int)d.DelicateId == delicateId);
            if (itemToRemove != null)
            {
                _availableDelicates.Remove(itemToRemove);
            }
            
            // Очищаем поле количества
            textBox.Clear();
            
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
            bool hasValidValue = !string.IsNullOrWhiteSpace(textBox.Text) &&
                                int.TryParse(textBox.Text, out int count) && count > 0;
            addButton.IsEnabled = hasValidValue;
        }
    }

    /// <summary>
    /// Поиск кнопки добавления (с иконкой Plus) в визуальном дереве
    /// </summary>
    private Button? FindAddButton(DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is Button button)
            {
                // Проверяем, есть ли в кнопке PackIcon с Kind="Plus"
                var packIcon = FindVisualChild<MaterialDesignThemes.Wpf.PackIcon>(button);
                if (packIcon != null && packIcon.Kind == MaterialDesignThemes.Wpf.PackIconKind.Plus)
                {
                    return button;
                }
            }
            
            var result = FindAddButton(child);
            if (result != null)
            {
                return result;
            }
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

            if (!int.TryParse(textBox.Text, out int count) || count <= 0)
            {
                MessageBox.Show("Некорректно введено количество!", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Добавляем блюдо в меню
            int delicateId = (int)data.DelicateId;
            _menuRepository.AddDelicateToMenu(_currentMenuId.Value, delicateId, count);
            
            // Удаляем блюдо из списка доступных
            var itemToRemove = _availableDelicates.FirstOrDefault(d => (int)d.DelicateId == delicateId);
            if (itemToRemove != null)
            {
                _availableDelicates.Remove(itemToRemove);
            }
            
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
                    if (delicateType == null || delicateType.Name != currentFilter)
                    {
                        return; // Блюдо не соответствует текущему фильтру
                    }
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
                DefaultCount = PeopleCountTextBox.Text
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

            CurrentMenuInfo.Text = $"Банкет: {BanquetNameTextBox.Text} - {PeopleCountTextBox.Text} человек, дата - {BanquetDatePicker.SelectedDate?.ToString() ?? DateTime.Now.ToString()}";
        }
        catch { }
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
            {
                SaveMenuChanges_Click(sender, e);
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return;
            }
        }

        if (_currentMenuId.HasValue)
        {
            LoadMenu(_currentMenuId.Value);
        }
    }

    /// <summary>
    /// Генерация отчета
    /// </summary>
    private void GenerateReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue || _currentMenuDelicates.Count == 0)
            {
                MessageBox.Show("Нет данных для отчета!\n\nСоздайте меню и добавьте блюда.", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var banquetInfo = new List<string>
            {
                BanquetNameTextBox.Text,
                PeopleCountTextBox.Text,
                BanquetDatePicker.SelectedDate?.ToString() ?? DateTime.Now.ToString(),
                DescriptionTextBox.Text
            };

            // Создаем страницу отчета с данными
            var reportPage = new ReportPage
            {
                MenuDelicates = _currentMenuDelicates,
                BanquetInfo = banquetInfo
            };

            // Навигируем к странице отчета
            Services.NavigationService.Instance.NavigateTo(reportPage);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Печать всего меню
    /// </summary>
    private void PrintAllMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var menuPrinter = new MenuPrinter();
            var allDelicates = _delicateRepository.GetAllDelicates().ToList();
            
            if (!allDelicates.Any())
            {
                MessageBox.Show("Нет блюд для печати!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            menuPrinter.PrintMenu(allDelicates, "Полное меню");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при печати: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Печать текущего меню
    /// </summary>
    private void PrintCurrentMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentMenuId.HasValue || _currentMenuDelicates.Count == 0)
            {
                MessageBox.Show("Нет данных для печати!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var menuPrinter = new MenuPrinter();
            var menuName = $"{BanquetNameTextBox.Text}, {PeopleCountTextBox.Text} человек, {BanquetDatePicker.SelectedDate?.ToShortDateString()}";
            
            var delicatesToPrint = _currentMenuDelicates.Select(md => new DelicatesColl
            {
                Name = md.Del,
                Count = md.Countpor,
                Lcomp = md.Lcomp
            }).ToList();

            menuPrinter.PrintMenu(delicatesToPrint, menuName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при печати: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Показать/скрыть индикатор загрузки
    /// </summary>
    private void ShowLoading(bool show)
    {
        LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Поиск визуального потомка определенного типа
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}

