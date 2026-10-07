using ControlzEx.Theming;
using System.Windows;
using System.Windows.Threading;
using FortniteBoost.Services;

namespace FortniteBoost
{
    public partial class App : Application
    {
        private DispatcherTimer? _licenseTimer;
        private MainWindow? _main;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Aplica tema Dark + Blue al iniciar
            ThemeManager.Current.ChangeTheme(this, "Dark.Blue");

            // Mientras se muestra la ventana de licencia, la app no debe cerrarse sola
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var license = LicenseService.CheckStored();
            if (!license.IsValid)
            {
                if (!AskForLicense(license)) { Shutdown(); return; }
                license = LicenseService.CheckStored();
            }

            _main = new MainWindow();
            MainWindow = _main;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            UpdateTitle(license);
            _main.Show();

            StartLicenseTimer();
        }

        private static bool AskForLicense(LicenseResult current)
        {
            var w = new LicenseWindow(current);
            return w.ShowDialog() == true;
        }

        private void UpdateTitle(LicenseResult license)
        {
            if (_main != null && license.Expires.HasValue)
                _main.Title = $"{LicenseService.AppName}  -  licencia hasta {license.Expires:dd/MM/yyyy hh:mm tt}";
        }

        // Revisa cada 5 minutos: si vence con la app abierta, se bloquea
        private void StartLicenseTimer()
        {
            _licenseTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _licenseTimer.Tick += (_, _) =>
            {
                var r = LicenseService.CheckStored();
                if (r.IsValid) return;

                _licenseTimer.Stop();
                _main?.Hide();

                if (AskForLicense(r))
                {
                    UpdateTitle(LicenseService.CheckStored());
                    _main?.Show();
                    _licenseTimer.Start();
                }
                else
                {
                    Shutdown();
                }
            };
            _licenseTimer.Start();
        }
    }
}
