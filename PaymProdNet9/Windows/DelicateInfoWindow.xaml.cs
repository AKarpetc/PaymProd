using System.Windows;

namespace PaymProdNet9.Windows;

public partial class DelicateInfoWindow : Window
{
    public DelicateInfoWindow(string delicateName, string composition)
    {
        InitializeComponent();
        DelicateNameTextBlock.Text = delicateName;
        CompositionTextBlock.Text = composition;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}