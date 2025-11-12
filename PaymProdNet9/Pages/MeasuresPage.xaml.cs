using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace PaymProdNet9.Pages;

public partial class MeasuresPage : Page
{
    private readonly ProductRepository _productRepository;
    
    private ObservableCollection<Measure> _allMeasures;
    private int? _currentMeasureId;

    public MeasuresPage()
    {
        InitializeComponent();
        
        _productRepository = new ProductRepository();
        _allMeasures = new ObservableCollection<Measure>();
        
        MeasuresDataGrid.ItemsSource = _allMeasures;
        
        this.Loaded += MeasuresPage_LoadedInternal;
    }
    
    private void MeasuresPage_LoadedInternal(object sender, RoutedEventArgs e)
    {
        if (NavigationService != null)
        {
            NavigationService.Navigating -= MeasuresPage_Navigating;
            NavigationService.Navigating += MeasuresPage_Navigating;
        }
    }
    
    private void MeasuresPage_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (MeasureEditView.Visibility == Visibility.Visible && e.NavigationMode == NavigationMode.Back)
        {
            e.Cancel = true;
            ShowListView();
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadMeasures();
        ShowListView();
    }
    
    private void ShowListView()
    {
        MeasuresListView.Visibility = Visibility.Visible;
        MeasureEditView.Visibility = Visibility.Collapsed;
    }
    
    private void ShowEditView(bool isEdit)
    {
        MeasuresListView.Visibility = Visibility.Collapsed;
        MeasureEditView.Visibility = Visibility.Visible;
        EditModeTitle.Text = isEdit ? "Редактирование единицы измерения" : "Создание единицы измерения";
        SaveButton.Content = isEdit ? "💾 Сохранить изменения" : "💾 Создать";
    }

    private void LoadMeasures()
    {
        try
        {
            _allMeasures.Clear();
            var measures = _productRepository.GetMeasures();
            foreach (var measure in measures)
            {
                _allMeasures.Add(measure);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке единиц измерения: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void MeasureSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = MeasureSearchBox.Text.ToLower();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            LoadMeasures();
            return;
        }
        
        try
        {
            _allMeasures.Clear();
            var allMeasures = _productRepository.GetMeasures();
            var filtered = allMeasures.Where(m => 
                m.Name.ToLower().Contains(searchText) || 
                (m.FassIzmer != null && m.FassIzmer.ToLower().Contains(searchText))
            );
            
            foreach (var measure in filtered)
            {
                _allMeasures.Add(measure);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при поиске единиц измерения: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditMeasure_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var measure = button?.DataContext as Measure;
        if (measure == null) return;

        _currentMeasureId = measure.Id;
        
        MeasureNameTextBox.Text = measure.Name;
        MeasureFassTextBox.Text = measure.Fass.ToString();
        MeasureFassIzmerTextBox.Text = measure.FassIzmer;
        
        // Устанавливаем точность округления
        foreach (ComboBoxItem item in MeasureRoundingComboBox.Items)
        {
            if (item.Tag != null && int.TryParse(item.Tag.ToString(), out int tagValue) && tagValue == measure.RoundingPrecision)
            {
                MeasureRoundingComboBox.SelectedItem = item;
                break;
            }
        }
        if (MeasureRoundingComboBox.SelectedItem == null && MeasureRoundingComboBox.Items.Count > 1)
        {
            MeasureRoundingComboBox.SelectedIndex = 1; // По умолчанию до сотых
        }
        
        ShowEditView(true);
    }

    private void NewMeasure_Click(object sender, RoutedEventArgs e)
    {
        _currentMeasureId = null;
        
        MeasureNameTextBox.Clear();
        MeasureFassTextBox.Clear();
        MeasureFassIzmerTextBox.Clear();
        MeasureRoundingComboBox.SelectedIndex = 1; // По умолчанию до сотых
        
        ShowEditView(false);
        MeasureNameTextBox.Focus();
    }
    
    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ShowListView();
    }

    private void DeleteMeasure_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var measure = button?.DataContext as Measure;
            if (measure == null) return;

            var result = MessageBox.Show($"Удалить единицу измерения '{measure.Name}'?", 
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _productRepository.DeleteMeasure(measure.Id);
                LoadMeasures();
                MessageBox.Show("Единица измерения удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении единицы измерения: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveMeasure_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MeasureNameTextBox.Text))
            {
                MessageBox.Show("Введите название единицы измерения!", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fass = double.TryParse(MeasureFassTextBox.Text, out var f) ? f : 1.0;
            var fassIzmer = MeasureFassIzmerTextBox.Text ?? MeasureNameTextBox.Text;
            
            // Получаем точность округления из ComboBox
            int roundingPrecision = 2; // По умолчанию до сотых
            if (MeasureRoundingComboBox.SelectedItem is ComboBoxItem selectedItem && 
                selectedItem.Tag != null && 
                int.TryParse(selectedItem.Tag.ToString(), out int precision))
            {
                roundingPrecision = precision;
            }

            if (_currentMeasureId.HasValue)
            {
                _productRepository.UpdateMeasure(
                    _currentMeasureId.Value, 
                    MeasureNameTextBox.Text, 
                    fass, 
                    fassIzmer,
                    roundingPrecision);
                
                MessageBox.Show("Единица измерения обновлена!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _productRepository.AddMeasure(
                    MeasureNameTextBox.Text, 
                    fass, 
                    fassIzmer,
                    roundingPrecision);
                
                MessageBox.Show("Единица измерения создана!", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadMeasures();
            ShowListView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении единицы измерения: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextNumeric(e.Text);
    }

    private static bool IsTextNumeric(string text)
    {
        return text.All(c => char.IsDigit(c) || c == ',' || c == '.');
    }
}

