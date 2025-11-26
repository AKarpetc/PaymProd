using ClosedXML.Excel;
using Microsoft.Win32;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Pages;
using PaymProdNet9.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PaymProdNet9;

public partial class MainNavigationWindow : Window
{
    private Button? _activeButton;

    public MainNavigationWindow()
    {
        InitializeComponent();

        // Инициализируем сервис навигации
        NavigationService.Instance.Initialize(MainFrame);

        // Устанавливаем имя пользователя
        UserNameText.Text = Environment.UserName;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // По умолчанию открываем текущее меню
        NavigateToCurrentMenu_Click(CurrentMenuButton, e);
    }

    private void NavigateToCurrentMenu_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Текущее меню - Составление банкета";
        NavigationService.Instance.NavigateTo<CurrentMenuPage>();
    }

    private void NavigateToSavedMenus_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Сохраненные меню - Все банкеты";
        NavigationService.Instance.NavigateTo<SavedMenusPage>();
    }

    private void NavigateToDelicates_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Справочник блюд";
        NavigationService.Instance.NavigateTo<DelicatesPage>();
    }

    private void NavigateToProducts_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Справочник продуктов";
        NavigationService.Instance.NavigateTo<ProductsPage>();
    }

    private void NavigateToProductPrices_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Общая цена продуктов";
        NavigationService.Instance.NavigateTo(new ProductPricesPage());
    }

    private void NavigateToMeasures_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Единицы измерения";
        NavigationService.Instance.NavigateTo<MeasuresPage>();
    }

    private void NavigateToProductTypes_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Типы продуктов";
        NavigationService.Instance.NavigateTo<ProductTypesPage>();
    }

    private void NavigateToDelicateTypes_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Типы блюд";
        NavigationService.Instance.NavigateTo<DelicateTypesPage>();
    }

    private void NavigateToDatabase_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Управление базой данных";
        NavigationService.Instance.NavigateTo<DatabaseManagerPage>();
    }

    private void GoBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Instance.GoBack();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Вы уверены, что хотите выйти из приложения?",
            "Подтверждение выхода",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes) Application.Current.Shutdown();
    }

    private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        // Обновляем видимость кнопки "Назад"
        BackButton.Visibility = NavigationService.Instance.CanGoBack
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SetActiveButton(Button? button)
    {
        // Сбрасываем предыдущую активную кнопку
        if (_activeButton != null) _activeButton.Background = System.Windows.Media.Brushes.Transparent;

        // Устанавливаем новую активную кнопку
        _activeButton = button;
        if (_activeButton != null)
            _activeButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2C3E50")!);
    }

    /// <summary>
    /// Получить данные текущего меню для отчетов
    /// </summary>
    private (ObservableCollection<MenuDel_act> menuDelicates, List<string> banquetInfo, int menuId)? GetCurrentMenuData()
    {
        try
        {
            var menuRepository = new MenuRepository();
            var openMenu = menuRepository.GetOpenMenu();

            if (openMenu == null)
            {
                MessageBox.Show("Нет открытого меню!\n\nСоздайте или откройте меню для генерации отчета.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var menuDelicates = menuRepository.GetMenuDelicates(openMenu.Id);
            if (menuDelicates.Count == 0)
            {
                MessageBox.Show("В меню нет блюд!\n\nДобавьте блюда в меню для генерации отчета.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var banquetInfo = new List<string>
            {
                openMenu.Name ?? "Без названия",
                openMenu.CountP.ToString(),
                openMenu.DateBan ?? DateTime.Now.ToString(),
                openMenu.Detail ?? ""
            };

            return (menuDelicates, banquetInfo, openMenu.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при получении данных меню: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }


    /// <summary>
    /// Навигация к отчету по продуктам
    /// </summary>
    private void NavigateToProductsReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetActiveButton(sender as Button);
            PageTitle.Text = "Отчет по продуктам";

            var menuData = GetCurrentMenuData();
            if (menuData == null) return;

            var (menuDelicates, banquetInfo, _) = menuData.Value;

            // Создаем страницу отчета по продуктам с данными
            var productsReportPage = new ProductsReportPage
            {
                MenuDelicates = menuDelicates,
                BanquetInfo = banquetInfo
            };

            // Навигируем к странице отчета
            NavigationService.Instance.NavigateTo(productsReportPage);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии отчета: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Навигация к сводной таблице
    /// </summary>
    private void NavigateToSummaryTable_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetActiveButton(sender as Button);
            PageTitle.Text = "Сводная таблица";

            var menuData = GetCurrentMenuData();
            if (menuData == null) return;

            var (menuDelicates, banquetInfo, _) = menuData.Value;

            // Создаем страницу сводной таблицы с данными
            var summaryTablePage = new SummaryTablePage
            {
                MenuDelicates = menuDelicates,
                BanquetInfo = banquetInfo
            };

            // Навигируем к странице сводной таблицы
            NavigationService.Instance.NavigateTo(summaryTablePage);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии сводной таблицы: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Печать меню
    /// </summary>
    private void PrintMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetActiveButton(sender as Button);
            PageTitle.Text = "Печать меню";

            var menuData = GetCurrentMenuData();
            if (menuData == null) return;

            var (menuDelicates, banquetInfo, menuId) = menuData.Value;

            var menuName = $"{banquetInfo[0]}, {banquetInfo[1]} человек, {banquetInfo[2]}";

            var delicateRepository = new DelicateRepository();
            var delicatesToPrint = new List<DelicatesColl>();
            foreach (var md in menuDelicates)
            {
                // Получаем тип блюда из справочника
                var delicate = delicateRepository.GetDelicateById(md.Del_id);
                delicatesToPrint.Add(new DelicatesColl
                {
                    Name = md.Del,
                    Count = md.Countpor,
                    Lcomp = md.Lcomp,
                    Type = delicate?.Type ?? "Без типа",
                    TypeSortOrder = delicate?.TypeSortOrder ?? 0
                });
            }

            var printMenuPage = new PrintMenuPage
            {
                Delicates = delicatesToPrint,
                BanquetInfo = banquetInfo,
                MenuId = menuId
            };

            NavigationService.Instance.NavigateTo(printMenuPage);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при печати меню: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}