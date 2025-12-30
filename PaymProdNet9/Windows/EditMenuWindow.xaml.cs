using System;
using System.Windows;
using System.Windows.Input;
using PaymProdNet9.Services;

namespace PaymProdNet9.Windows;

public partial class EditMenuWindow : Window
{
    public string BanquetName { get; private set; } = string.Empty;
    public int GuestCount { get; private set; }
    public DateTime SelectedDate { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public EditMenuWindow(string currentName, int currentGuests, DateTime currentDate, string currentDescription)
    {
        InitializeComponent();

        NameTextBox.Text = currentName;
        GuestsTextBox.Text = currentGuests.ToString();
        DatePicker.SelectedDateTime = currentDate;
        DescriptionTextBox.Text = currentDescription;

        BanquetName = currentName;
        GuestCount = currentGuests;
        SelectedDate = currentDate;
        Description = currentDescription;
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
             MessageBox.Show("Введите корректное количество гостей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
             return;
        }

        BanquetName = NameTextBox.Text;
        GuestCount = guests;
        SelectedDate = DatePicker.SelectedDateTime ?? DateTime.Now;
        Description = DescriptionTextBox.Text;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
