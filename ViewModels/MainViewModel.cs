using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Services;

namespace MarketPrenga.ViewModels;

public partial class MainViewModel : ObservableObject
{
    ShitjaViewModel? _shitja;   // mbahet gjallë që shporta të mos humbasë kur ndërron faqe

    [ObservableProperty] private object? _current;
    [ObservableProperty] private string _section = "";

    public string UserLabel => $"{Session.User?.Emri}  ·  {Session.User?.Access}";
    public bool IsAdmin => Session.IsAdmin;

    public bool IsDashboard => Section == "dashboard";
    public bool IsShitja => Section == "shitja";
    public bool IsFaturat => Section == "faturat";
    public bool IsArtikuj => Section == "artikuj";
    public bool IsKlientet => Section == "klientet";
    public bool IsFurnitoret => Section == "furnitoret";
    public bool IsRaporte => Section == "raporte";

    public MainViewModel() => Navigate(Session.IsAdmin ? "dashboard" : "shitja");

    partial void OnSectionChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboard)); OnPropertyChanged(nameof(IsShitja));
        OnPropertyChanged(nameof(IsFaturat)); OnPropertyChanged(nameof(IsArtikuj));
        OnPropertyChanged(nameof(IsKlientet)); OnPropertyChanged(nameof(IsFurnitoret));
        OnPropertyChanged(nameof(IsRaporte));
    }

    [RelayCommand]
    void Navigate(string? section)
    {
        section ??= "shitja";
        if (!Session.IsAdmin && section != "shitja") section = "shitja";
        Section = section;
        Current = section switch
        {
            "dashboard" => new DashboardViewModel(),
            "faturat" => new FaturatViewModel(),
            "artikuj" => new ArtikujViewModel(),
            "klientet" => new KlientetViewModel(),
            "furnitoret" => new FurnitoretViewModel(),
            "raporte" => new RaporteViewModel(),
            _ => _shitja ??= new ShitjaViewModel()
        };
    }

    [RelayCommand] void Exit() => Application.Current.Shutdown();
}
