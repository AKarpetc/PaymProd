using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using PaymProdNet9.Services;

namespace PaymProdNet9.Pages;

public partial class EditMenuPage : Page
{
    private readonly Action<bool, string, int, DateTime, string> _callback;

    // Default constructor for XAML design time
    public EditMenuPage()
    {
        InitializeComponent();
    }

    public EditMenuPage(string currentName, int currentGuests, DateTime currentDate, string currentDescription, Action<bool, string, int, DateTime, string> callback)
    {
        InitializeComponent();
        _callback = callback;

        NameTextBox.Text = currentName;
        GuestsTextBox.Text = currentGuests.ToString();
        DatePicker.SelectedDateTime = currentDate;
        DescriptionTextBox.Text = currentDescription;
    }

    private void GuestsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        InputValidationHelper.IntegerOnly_PreviewTextInput(sender, e);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Введите название банкета.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(GuestsTextBox.Text, out var guests) || guests <= 0)
        {
            MessageBox.Show("Введите корректное количество гостей.", "Ошибка", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Return result via callback
        _callback?.Invoke(true, NameTextBox.Text, guests, DatePicker.SelectedDateTime ?? DateTime.Now, DescriptionTextBox.Text);
        
        // Go back
        if (NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Cancel - pass false
        _callback?.Invoke(false, null, 0, DateTime.MinValue, null);

        if (NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
    }
}
