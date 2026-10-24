using System.Windows;
using System.Windows.Input;
using MarketPrenga.Models;
using MarketPrenga.Services;

namespace MarketPrenga.Views;

public partial class HeldWindow : Window
{
    public HeldSale? Chosen { get; private set; }

    public HeldWindow()
    {
        InitializeComponent();
        Reload();
    }

    void Reload()
    {
        List.ItemsSource = null;
        List.ItemsSource = HeldStore.Items.ToList();
    }

    void Recall_Click(object sender, RoutedEventArgs e) => Choose();
    void List_DoubleClick(object sender, MouseButtonEventArgs e) => Choose();

    void Choose()
    {
        if (List.SelectedItem is not HeldSale h) return;
        Chosen = h;
        DialogResult = true;
    }

    void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is not HeldSale h) return;
        if (MessageBox.Show("Fshi këtë shportë në pritje?", "Konfirmo", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        HeldStore.Remove(h);
        Reload();
    }
}
