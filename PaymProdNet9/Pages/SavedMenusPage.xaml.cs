using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PaymProdNet9.Pages;

public partial class SavedMenusPage : Page
{
    private readonly MenuRepository _menuRepository;

    public SavedMenusPage()
    {
        InitializeComponent();
        _menuRepository = new MenuRepository();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSavedMenus();
    }

    /// <summary>
    /// Загрузка сохраненных меню
    /// </summary>
    private void LoadSavedMenus()
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
            
            // Переходим к странице текущего меню
            Services.NavigationService.Instance.NavigateTo<CurrentMenuPage>();
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
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении меню: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

