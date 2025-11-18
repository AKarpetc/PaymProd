using System;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PaymProdNet9.Services;

/// <summary>
/// Сервис навигации между страницами
/// </summary>
public class NavigationService
{
    private static NavigationService? _instance;
    private Frame? _mainFrame;

    public static NavigationService Instance => _instance ??= new NavigationService();

    /// <summary>
    /// Инициализация сервиса с главным Frame
    /// </summary>
    public void Initialize(Frame mainFrame)
    {
        _mainFrame = mainFrame;
        _mainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
    }

    /// <summary>
    /// Навигация к странице
    /// </summary>
    public void NavigateTo<T>() where T : Page, new()
    {
        if (_mainFrame == null)
            throw new InvalidOperationException("NavigationService не инициализирован");

        var page = new T();
        _mainFrame.Navigate(page);
    }

    /// <summary>
    /// Навигация к странице с параметром
    /// </summary>
    public void NavigateTo<T>(object parameter) where T : Page, new()
    {
        if (_mainFrame == null)
            throw new InvalidOperationException("NavigationService не инициализирован");

        var page = new T();
        _mainFrame.Navigate(page, parameter);
    }

    /// <summary>
    /// Навигация к экземпляру страницы
    /// </summary>
    public void NavigateTo(Page page)
    {
        if (_mainFrame == null)
            throw new InvalidOperationException("NavigationService не инициализирован");

        _mainFrame.Navigate(page);
    }

    /// <summary>
    /// Вернуться назад
    /// </summary>
    public void GoBack()
    {
        if (_mainFrame?.CanGoBack == true) _mainFrame.GoBack();
    }

    /// <summary>
    /// Можно ли вернуться назад
    /// </summary>
    public bool CanGoBack => _mainFrame?.CanGoBack ?? false;

    /// <summary>
    /// Очистить историю навигации
    /// </summary>
    public void ClearHistory()
    {
        if (_mainFrame == null) return;

        while (_mainFrame.CanGoBack) _mainFrame.RemoveBackEntry();
    }
}