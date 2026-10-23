using System.Windows;
using System.Windows.Controls;
using MarketPrenga.ViewModels;

namespace MarketPrenga.Views;

public partial class ShitjaView : UserControl
{
    public ShitjaView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BarcodeBox.Focus();
            if (DataContext is ShitjaViewModel vm)
            {
                vm.FocusBarcodeRequested -= FocusBarcode;
                vm.FocusBarcodeRequested += FocusBarcode;
            }
        };
    }

    void FocusBarcode()
    {
        Dispatcher.BeginInvoke(() => { BarcodeBox.Focus(); BarcodeBox.SelectAll(); });
    }
}
