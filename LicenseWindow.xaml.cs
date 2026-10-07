using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using FortniteBoost.Services;
using MahApps.Metro.Controls;

namespace FortniteBoost
{
    public partial class LicenseWindow : MetroWindow
    {
        public LicenseWindow(LicenseResult current)
        {
            InitializeComponent();
            Title = $"{LicenseService.AppName} - Activacion";
            TitleText.Text = $"Activar {LicenseService.AppName}";
            RequestCodeBox.Text = HardwareId.GetRequestCode();
            ShowStatus(current);
        }

        private void ShowStatus(LicenseResult r)
        {
            StatusText.Text = r.Message;
            string brushKey = r.Status switch
            {
                LicenseStatus.NoLicense => "TextMutedBrush",
                LicenseStatus.Valid => "NeonGreenBrush",
                _ => "NeonRedBrush"
            };
            StatusText.Foreground = (Brush)FindResource(brushKey);
        }

        private void CopyRequest_Click(object sender, RoutedEventArgs e)
        {
            try { Clipboard.SetText(RequestCodeBox.Text); } catch { }
        }

        private void WhatsApp_Click(object sender, RoutedEventArgs e)
        {
            string msg = $"Hola, quiero activar {LicenseService.AppName}. Mi codigo de equipo es: {RequestCodeBox.Text}";
            string url = $"https://wa.me/{LicenseService.WhatsAppNumber}?text={Uri.EscapeDataString(msg)}";
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { MessageBox.Show("No se pudo abrir WhatsApp. Copia el codigo y envialo manualmente.", LicenseService.AppName); }
        }

        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            var result = LicenseService.Activate(ActivationBox.Text);
            if (result.IsValid)
            {
                MessageBox.Show($"Activacion exitosa.\n\n{result.Message}", LicenseService.AppName,
                                MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
            {
                ShowStatus(result);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
