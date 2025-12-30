using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PaymProdNet9.Pages;

public partial class MultiMenuSelectionWindow : Window
{
    private readonly MenuRepository _menuRepository;
    
    public List<int> SelectedMenuIds { get; private set; } = new();
    public bool IncludePrices { get; private set; }

    public MultiMenuSelectionWindow(List<int> preSelectedIds = null)
    {
        InitializeComponent();
        _menuRepository = new MenuRepository();
        LoadMenus(preSelectedIds);
    }

    private void LoadMenus(List<int> preSelectedIds)
    {
        var menus = _menuRepository.GetAllMenus();
        // Sort descending by date (using Id roughly or parsing date currently stored as string)
        var items = menus.OrderByDescending(m => m.Id)
                         .Select(m => new MenuSelectionItem 
                         { 
                             Menu = m,
                             IsSelected = preSelectedIds != null && preSelectedIds.Contains(m.Id)
                         })
                         .ToList();
        MenusDataGrid.ItemsSource = items;
    }

    private void ReportWithPrices_Click(object sender, RoutedEventArgs e)
    {
        if (ConfirmSelection())
        {
            IncludePrices = true;
            DialogResult = true;
            Close();
        }
    }

    private void ReportWithoutPrices_Click(object sender, RoutedEventArgs e)
    {
        if (ConfirmSelection())
        {
            IncludePrices = false;
            DialogResult = true;
            Close();
        }
    }

    private bool ConfirmSelection()
    {
        var items = MenusDataGrid.ItemsSource as List<MenuSelectionItem>;
        var selected = items?.Where(i => i.IsSelected).Select(i => i.Menu.Id).ToList();

        if (selected == null || !selected.Any())
        {
            MessageBox.Show("Выберите хотя бы одно меню.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        SelectedMenuIds = selected;
        return true;
    }
}

public class MenuSelectionItem
{
    public Menus Menu { get; set; }
    public bool IsSelected { get; set; }
}
