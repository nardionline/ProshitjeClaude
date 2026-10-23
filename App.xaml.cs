using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using MarketPrenga.Data;
using MarketPrenga.Services;
using MarketPrenga.Views;

namespace MarketPrenga;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // numrat në fushat WPF ndjekin cilësimet e Windows (presje/pikë)
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Gabim i papritur", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            AppConfig.Load();
            Db.Init(AppConfig.Current.DatabasePath, AppConfig.Current.DatabasePassword);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + (ex.InnerException != null ? "\n\n" + ex.InnerException.Message : ""),
                "Nuk u hap databaza", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var login = new LoginWindow();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var main = new global::MarketPrenga.MainWindow();
        MainWindow = main;
        main.Closed += (_, _) => Shutdown();
        main.Show();
    }
}
