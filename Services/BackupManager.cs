using System;
using System.IO;
using System.Windows;

namespace FortniteBoost.Services
{
    public class BackupManager
    {
        private readonly string backupFolder = "Backups";

        public BackupManager()
        {
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }
        }

        // Guardar configuración actual
        public void CreateBackup(string name, string content)
        {
            string path = Path.Combine(backupFolder, $"{name}.bak");
            File.WriteAllText(path, content);
            MessageBox.Show($"Backup '{name}' creado correctamente.");
        }

        // Restaurar configuración
        public string RestoreBackup(string name)
        {
            string path = Path.Combine(backupFolder, $"{name}.bak");
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                MessageBox.Show($"Backup '{name}' restaurado correctamente.");
                return content;
            }
            else
            {
                MessageBox.Show($"No se encontró el backup '{name}'.");
                return string.Empty;
            }
        }
    }
}
