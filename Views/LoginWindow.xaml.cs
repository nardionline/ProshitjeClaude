using System.Windows;
using System.Windows.Input;
using MarketPrenga.Data;
using MarketPrenga.Models;
using MarketPrenga.Services;

namespace MarketPrenga.Views;

public partial class LoginWindow : Window
{
    readonly PerdoruesRepo _repo = new();
    int _attempts;

    public LoginWindow()
    {
        InitializeComponent();
        try
        {
            UserBox.ItemsSource = _repo.All();
            if (UserBox.Items.Count > 0) UserBox.SelectedIndex = 0;
        }
        catch (Exception ex) { ErrorText.Text = "Gabim leximi i përdoruesve: " + ex.Message; }
        Loaded += (_, _) => PassBox.Focus();
    }

    void PassBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Login_Click(sender, e);
    }

    void Login_Click(object sender, RoutedEventArgs e)
    {
        if (UserBox.SelectedItem is not Perdorues u)
        {
            ErrorText.Text = "Zgjidh përdoruesin.";
            return;
        }
        if (string.IsNullOrEmpty(PassBox.Password))
        {
            ErrorText.Text = "Duhet të vendosni një fjalëkalim.";
            return;
        }

        if (PassBox.Password == u.Fjalekalimi)
        {
            Session.User = u;
            _repo.LogLogin(u.Id);
            DialogResult = true;
            return;
        }

        _attempts++;
        PassBox.Clear();
        if (_attempts >= 3)
        {
            MessageBox.Show("Nuk mundeni të hyni. Kontaktoni Administratorin.", "Hyrje e kufizuar",
                MessageBoxButton.OK, MessageBoxImage.Stop);
            DialogResult = false;
            return;
        }
        ErrorText.Text = "Fjalëkalimi nuk përputhet. Provo përsëri.";
        PassBox.Focus();
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
