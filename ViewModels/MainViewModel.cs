using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Services;

namespace MarketPrenga.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private object? _current;

    public string UserLabel => $"{Session.User?.Emri}  ({Session.User?.Access})";
    public bool IsAdmin => Session.IsAdmin;

    public MainViewModel() => Current = new ShitjaViewModel();

    [RelayCommand] private void ShowShitja() => Current = new ShitjaViewModel();
    [RelayCommand] private void ShowArtikuj() => Current = new ArtikujViewModel();
    [RelayCommand] private void ShowKlientet() => Current = new KlientetViewModel();
}
