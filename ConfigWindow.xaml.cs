using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace FortniteBoost
{
    public partial class ConfigWindow : MetroWindow
    {
        public ConfigWindow()
        {
            InitializeComponent();
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            string idioma = (LanguageCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Español";
            string tema = (ThemeCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Oscuro";
            string licencia = LicenseBox.Text;

            MessageBox.Show($"✅ Configuración guardada:\n\nIdioma: {idioma}\nTema: {tema}\nLicencia: {licencia}",
                            "Configuración",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

    }
}
