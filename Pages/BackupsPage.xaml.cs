using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FortniteBoost.Pages
{
    public partial class BackupsPage : UserControl
    {
        // Carpeta estable por usuario (antes usaba "Backups" relativo, que podia caer en System32)
        private static readonly string BackupDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "VeloxForge", "Backups");

        private static string FortniteConfig =>
            Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "",
                         @"FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini");

        public BackupsPage()
        {
            InitializeComponent();
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(BackupDir);
                if (File.Exists(FortniteConfig))
                {
                    string dest = Path.Combine(BackupDir, $"GameUserSettings_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                    File.Copy(FortniteConfig, dest, true);
                    Log("OK Backup creado: " + dest);
                }
                else
                {
                    Log("No se encontro el archivo de configuracion de Fortnite.");
                }
            }
            catch (Exception ex) { Log("Error al crear backup: " + ex.Message); }
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(BackupDir)) { Log("Aun no hay backups."); return; }
                var last = new DirectoryInfo(BackupDir).GetFiles("*.bak")
                    .OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                if (last == null) { Log("Aun no hay backups."); return; }

                File.Copy(last.FullName, FortniteConfig, true);
                Log("OK Restaurado desde: " + last.Name);
            }
            catch (Exception ex) { Log("Error al restaurar: " + ex.Message); }
        }

        private void Log(string text)
        {
            BackupLog.Items.Add($"{DateTime.Now:HH:mm:ss}  {text}");
            BackupLog.ScrollIntoView(BackupLog.Items[^1]);
        }
    }
}
