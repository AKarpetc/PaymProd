using PaymProdNet9.Pages;
using PaymProdNet9.Services;
using System;
using System.Windows;
using System.Windows.Controls;

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

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
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
        if (_activeButton != null)
        {
            _activeButton.Background = System.Windows.Media.Brushes.Transparent;
        }

        // Устанавливаем новую активную кнопку
        _activeButton = button;
        if (_activeButton != null)
        {
            _activeButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2C3E50")!);
        }
    }
}

