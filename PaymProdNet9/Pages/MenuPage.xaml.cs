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

public partial class MenuPage : Page
{
    private readonly MenuRepository _menuRepository;
    private readonly DelicateRepository _delicateRepository;
    private readonly ProductRepository _productRepository;

    private int? _currentMenuId;
    private ObservableCollection<MenuDel_act> _currentMenuDelicates;
    private bool _isDataChanged = false;

    public MenuPage()
    {
        InitializeComponent();

        _menuRepository = new MenuRepository();
        _delicateRepository = new DelicateRepository();
        _productRepository = new ProductRepository();

        _currentMenuDelicates = new ObservableCollection<MenuDel_act>();
        MenuDelicatesDataGrid.ItemsSource = _currentMenuDelicates;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadSavedMenus();
            LoadDelicateTypes();

            // Проверяем открытое меню
            var openMenu = _menuRepository.GetOpenMenu();
            if (openMenu != null) LoadMenu(openMenu.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка сохраненных меню
    /// </summary>
    public void LoadSavedMenus()
    {
        try
        {
            var menus = _menuRepository.GetAllMenus();
            SavedMenusDataGrid.ItemsSource = menus;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке списка меню: {ex.Message}",
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

            // Проверяем и добавляем недостающие продукты с AutoAdd
            _menuRepository.EnsureAutoAddProductsInMenu(menuId, menu.CountP);

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

            // Конвертируем в формат для отображения
            var displayDelicates = delicates.Select(d => new
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

            AvailableDelicatesPanel.ItemsSource = displayDelicates;
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

        var filter = button.Tag.ToString() ?? "%";
        LoadAvailableDelicates(filter);
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
            LoadSavedMenus();
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

            LoadSavedMenus();
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

            if (!int.TryParse(textBox.Text, out var count))
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

            // Обновляем список
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
                _menuRepository.RemoveDelicateFromMenu(delicate.Idmen);
                _currentMenuDelicates.Remove(delicate);
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
    /// Редактирование сохраненного меню
    /// </summary>
    private void EditSavedMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var menu = button?.DataContext as Menus;
            if (menu == null) return;

            _menuRepository.OpenMenu(menu.Id);
            LoadMenu(menu.Id);
            MainTabControl.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии меню: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Удаление сохраненного меню
    /// </summary>
    private void DeleteSavedMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var menu = button?.DataContext as Menus;
            if (menu == null) return;

            var result = MessageBox.Show("Удалить меню?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _menuRepository.DeleteMenu(menu.Id);
                LoadSavedMenus();

                if (_currentMenuId == menu.Id) NewMenu_Click(sender, e);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении меню: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            LoadSavedMenus();
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
            var menuName =
                $"{BanquetNameTextBox.Text}, {PeopleCountTextBox.Text} человек, {BanquetDatePicker.SelectedDate?.ToShortDateString()}";

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
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }

        return null;
    }
}