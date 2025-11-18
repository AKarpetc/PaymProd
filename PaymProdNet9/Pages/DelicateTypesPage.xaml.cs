using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PaymProdNet9.Pages;

public partial class DelicateTypesPage : Page
{
    private readonly DelicateRepository _delicateRepository;

    private ObservableCollection<DelicateType> _allDelicateTypes;
    private int? _currentDelicateTypeId;

    public DelicateTypesPage()
    {
        InitializeComponent();

        _delicateRepository = new DelicateRepository();
        _allDelicateTypes = new ObservableCollection<DelicateType>();

        DelicateTypesDataGrid.ItemsSource = _allDelicateTypes;

        Loaded += DelicateTypesPage_LoadedInternal;
    }

    private void DelicateTypesPage_LoadedInternal(object sender, RoutedEventArgs e)
    {
        if (NavigationService != null)
        {
            NavigationService.Navigating -= DelicateTypesPage_Navigating;
            NavigationService.Navigating += DelicateTypesPage_Navigating;
        }
    }

    private void DelicateTypesPage_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (DelicateTypeEditView.Visibility == Visibility.Visible && e.NavigationMode == NavigationMode.Back)
        {
            e.Cancel = true;
            ShowListView();
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadDelicateTypes();
        ShowListView();
    }

    private void ShowListView()
    {
        DelicateTypesListView.Visibility = Visibility.Visible;
        DelicateTypeEditView.Visibility = Visibility.Collapsed;
    }

    private void ShowEditView(bool isEdit)
    {
        DelicateTypesListView.Visibility = Visibility.Collapsed;
        DelicateTypeEditView.Visibility = Visibility.Visible;
        EditModeTitle.Text = isEdit ? "Редактирование типа блюда" : "Создание типа блюда";
        SaveButton.Content = isEdit ? "💾 Сохранить изменения" : "💾 Создать";
    }

    private void LoadDelicateTypes()
    {
        try
        {
            _allDelicateTypes.Clear();
            var types = _delicateRepository.GetDelicateTypes();
            foreach (var type in types) _allDelicateTypes.Add(type);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов блюд: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DelicateTypeSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = DelicateTypeSearchBox.Text.ToLower();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            LoadDelicateTypes();
            return;
        }

        try
        {
            _allDelicateTypes.Clear();
            var allTypes = _delicateRepository.GetDelicateTypes();
            var filtered = allTypes.Where(t =>
                t.Name.ToLower().Contains(searchText)
            );

            foreach (var type in filtered) _allDelicateTypes.Add(type);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при поиске типов блюд: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditDelicateType_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var delicateType = button?.DataContext as DelicateType;
        if (delicateType == null) return;

        _currentDelicateTypeId = delicateType.Id;

        DelicateTypeNameTextBox.Text = delicateType.Name;
        DelicateTypeSortOrderTextBox.Text = delicateType.SortOrder.ToString();

        ShowEditView(true);
    }

    private void NewDelicateType_Click(object sender, RoutedEventArgs e)
    {
        _currentDelicateTypeId = null;

        DelicateTypeNameTextBox.Clear();
        DelicateTypeSortOrderTextBox.Text = "0";

        ShowEditView(false);
        DelicateTypeNameTextBox.Focus();
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ShowListView();
    }

    private void DeleteDelicateType_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var delicateType = button?.DataContext as DelicateType;
            if (delicateType == null) return;

            var result = MessageBox.Show($"Удалить тип блюда '{delicateType.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _delicateRepository.DeleteDelicateType(delicateType.Id);
                LoadDelicateTypes();
                MessageBox.Show("Тип блюда удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении типа блюда: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveDelicateType_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DelicateTypeNameTextBox.Text))
            {
                MessageBox.Show("Введите название типа блюда!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DelicateTypeSortOrderTextBox.Text, out var sortOrder)) sortOrder = 0;

            if (_currentDelicateTypeId.HasValue)
            {
                _delicateRepository.UpdateDelicateType(
                    _currentDelicateTypeId.Value,
                    DelicateTypeNameTextBox.Text,
                    sortOrder);

                MessageBox.Show("Тип блюда обновлен!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _delicateRepository.AddDelicateType(DelicateTypeNameTextBox.Text, sortOrder);

                MessageBox.Show("Тип блюда создан!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadDelicateTypes();
            ShowListView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении типа блюда: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NumericOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Разрешаем только цифры
        e.Handled = !e.Text.All(char.IsDigit);
    }
}