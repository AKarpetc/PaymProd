using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PaymProdNet9.Windows
{
    public partial class PrintFullMenuConfigWindow : Window
    {
        private readonly DelicateRepository _delicateRepository;
        public ObservableCollection<CategorySelection> Categories { get; set; } = new();
        
        public bool ShowCost { get; private set; }
        public bool ShowPrice { get; private set; }
        public List<int> SelectedCategoryIds { get; private set; } = new();

        public PrintFullMenuConfigWindow()
        {
            InitializeComponent();
            _delicateRepository = new DelicateRepository();
            CategoriesListBox.ItemsSource = Categories;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var types = _delicateRepository.GetDelicateTypes().OrderBy(t => t.Name).ToList();
            foreach(var t in types)
            {
                Categories.Add(new CategorySelection { Id = t.Id, Name = t.Name, IsSelected = true });
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach(var c in Categories) c.IsSelected = true;
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach(var c in Categories) c.IsSelected = false;
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            ShowCost = CostCheckBox.IsChecked == true;
            ShowPrice = PriceCheckBox.IsChecked == true;
            SelectedCategoryIds = Categories.Where(c => c.IsSelected).Select(c => c.Id).ToList();
            
            if (SelectedCategoryIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один тип блюд", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CategorySelection : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
