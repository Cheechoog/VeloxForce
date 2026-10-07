using System.Windows;
using System.Windows.Controls;
using FortniteBoost.Pages;
using FortniteBoost.Services;
using MahApps.Metro.Controls;

namespace FortniteBoost
{
    public partial class MainWindow : MetroWindow
    {
        // Las paginas se crean una vez y se reutilizan (conservan su estado)
        private readonly DashboardPage _dashboard = new();
        private readonly PerfilesPage _perfiles = new();
        private readonly HardwarePage _hardware = new();
        private readonly BackupsPage _backups = new();
        private readonly FpsPage _fps = new();

        public MainWindow()
        {
            InitializeComponent();
            ShowLicenseInfo();
            Navigate("dashboard");
            // Permite que el Dashboard mande a la pagina de perfiles con sus botones
            _dashboard.NavigateRequested += page => Navigate(page);
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string tag) Navigate(tag);
        }

        private void Navigate(string page)
        {
            // Resalta el boton activo del sidebar
            NavDashboard.Style = StyleFor("dashboard", page);
            NavPerfiles.Style = StyleFor("perfiles", page);
            NavFps.Style = StyleFor("fps", page);
            NavHardware.Style = StyleFor("hardware", page);
            NavBackups.Style = StyleFor("backups", page);

            switch (page)
            {
                case "perfiles":
                    PageHost.Content = _perfiles; CrumbText.Text = " / Perfiles Gamer"; break;
                case "fps":
                    PageHost.Content = _fps; CrumbText.Text = " / Mostrar FPS"; break;
                case "hardware":
                    PageHost.Content = _hardware; CrumbText.Text = " / Hardware"; break;
                case "backups":
                    PageHost.Content = _backups; CrumbText.Text = " / Backups"; break;
                default:
                    PageHost.Content = _dashboard; CrumbText.Text = " / Dashboard"; break;
            }
        }

        private Style StyleFor(string thisPage, string current) =>
            (Style)FindResource(thisPage == current ? "GamerButtonActive" : "GamerButton");

        private void ShowLicenseInfo()
        {
            var lic = LicenseService.CheckStored();
            if (lic.Expires.HasValue)
            {
                LicenseBadge.Text = "LICENCIA ACTIVA";
                LicenseFooter.Text = $"Vence {lic.Expires:dd/MM hh:mm tt}";
            }
        }
    }
}
