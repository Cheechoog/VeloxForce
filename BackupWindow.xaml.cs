using MahApps.Metro.Controls;
using System.Windows;
using System.IO;

namespace FortniteBoost
{
    public partial class BackupWindow : MetroWindow
    {
        public BackupWindow()
        {
            InitializeComponent();
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "";
            string fortniteConfig = Path.Combine(localAppData,
                @"FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini");


            string backupDir = "Backups";
            string backupFile = Path.Combine(backupDir, "Fortnite_Default.bak");

            try
            {
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                if (File.Exists(fortniteConfig))
                {
                    File.Copy(fortniteConfig, backupFile, true);
                    BackupLog.Items.Add("✅ Backup creado en " + backupFile);
                }
                else
                {
                    BackupLog.Items.Add("⚠️ No se encontró el archivo de configuración de Fortnite.");
                }
            }
            catch (System.Exception ex)
            {
                BackupLog.Items.Add("⚠️ Error al crear backup: " + ex.Message);
            }
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "";
            string fortniteConfig = Path.Combine(localAppData,
                @"FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini");


            string backupFile = Path.Combine("Backups", "Fortnite_Default.bak");

            try
            {
                if (File.Exists(backupFile))
                {
                    File.Copy(backupFile, fortniteConfig, true);
                    BackupLog.Items.Add("✅ Configuración restaurada desde backup.");
                }
                else
                {
                    BackupLog.Items.Add("⚠️ No se encontró el archivo de backup.");
                }
            }
            catch (System.Exception ex)
            {
                BackupLog.Items.Add("⚠️ Error al restaurar backup: " + ex.Message);
            }
        }
    }
}
