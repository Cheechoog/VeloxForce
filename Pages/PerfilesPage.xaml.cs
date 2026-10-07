using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FortniteBoost.Pages
{
    public partial class PerfilesPage : UserControl
    {
        public PerfilesPage()
        {
            InitializeComponent();
        }

        // Los 19 ajustes del perfil competitivo, todos en "yes".
        private static string CompetitiveArgs()
        {
            string[] on =
            {
                "Cpu","Energy","Services","Fortnite","HAGS","RAM","Visuals","CoreParking",
                "Network","GameDVR","Wallpaper","Notifications","MouseInput","PowerThrottle",
                "TimerRes","KillProcesses","Disk","GameMode","GPU"
            };
            var sb = new StringBuilder();
            foreach (var p in on) sb.Append($"-{p} yes ");
            return sb.ToString().Trim();
        }

        private void Run(string title, string script, string args)
        {
            var w = new ExecutionWindow(title, script, args) { Owner = Window.GetWindow(this) };
            w.ShowDialog();
        }

        private void Competitivo_Click(object sender, RoutedEventArgs e) =>
            Run("Aplicando perfil Competitivo", @"Scripts\OptimizeCompetitive.ps1", CompetitiveArgs());

        private void Streaming_Click(object sender, RoutedEventArgs e) =>
            Run("Aplicando perfil Streaming", @"Scripts\OptimizeStreaming.ps1", "-Fortnite yes -Services yes");

        private void General_Click(object sender, RoutedEventArgs e) =>
            Run("Optimizando el equipo", @"Scripts\OptimizeGeneral.ps1", "");

        private void Restaurar_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(
                "Esto devuelve Windows y Fortnite a como estaban antes de optimizar. \u00bfContinuar?",
                "Restaurar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
                Run("Restaurando valores originales", @"Scripts\RestoreDefault.ps1", "");
        }
    }
}
