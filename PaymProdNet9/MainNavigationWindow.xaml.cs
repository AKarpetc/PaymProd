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
        // По умолчанию открываем главную страницу
        NavigateToMenu_Click(MenuPageButton, e);
    }

    private void NavigateToMenu_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(sender as Button);
        PageTitle.Text = "Главная - Управление меню";
        NavigationService.Instance.NavigateTo<MenuPage>();
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

