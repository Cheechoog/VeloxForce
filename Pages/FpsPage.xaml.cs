using System.Diagnostics;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FortniteBoost.Services;
using Microsoft.Win32;

namespace FortniteBoost.Pages
{
    public partial class FpsPage : UserControl
    {
        private enum Vendor { Nvidia, Amd, Intel, Unknown }
        private Vendor _vendor = Vendor.Unknown;

        public FpsPage()
        {
            InitializeComponent();
            Loaded += (_, _) => Detect();
        }

        private void Detect()
        {
            string gpu = HardwareMonitor.GpuName;
            GpuText.Text = gpu;

            string g = gpu.ToLowerInvariant();
            if (g.Contains("nvidia") || g.Contains("geforce") || g.Contains("rtx") || g.Contains("gtx")) _vendor = Vendor.Nvidia;
            else if (g.Contains("amd") || g.Contains("radeon") || g.Contains("rx ")) _vendor = Vendor.Amd;
            else if (g.Contains("intel")) _vendor = Vendor.Intel;

            MonitorText.Text = GetMonitorInfo();

            // La recomendacion depende del hardware
            switch (_vendor)
            {
                case Vendor.Nvidia:
                    RecoDesc.Text = "Tu GPU es NVIDIA. Puedes usar el overlay de NVIDIA (muestra FPS, temperatura y uso) " +
                                    "o el contador de Windows. Activa el de Windows aqui; funciona en cualquier equipo.";
                    break;
                case Vendor.Amd:
                    RecoDesc.Text = "Tu GPU es AMD. AMD Adrenalin trae su propio overlay, pero el contador de Windows " +
                                    "es mas simple. Activalo aqui.";
                    break;
                default:
                    RecoDesc.Text = "Contador de FPS de Windows (Xbox Game Bar). No requiere instalar nada y funciona en tu equipo.";
                    break;
            }

            BuildSteps();
        }

        private static string GetMonitorInfo()
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT CurrentRefreshRate, CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController");
                foreach (var o in s.Get())
                {
                    var hz = o["CurrentRefreshRate"];
                    var w = o["CurrentHorizontalResolution"];
                    var h = o["CurrentVerticalResolution"];
                    if (hz != null && w != null)
                        return $"Pantalla: {w}x{h} a {hz} Hz";
                }
            }
            catch { }
            return "";
        }

        private void BuildSteps()
        {
            string[] steps = _vendor == Vendor.Nvidia
                ? new[]
                {
                    "1. Da clic en ACTIVAR CONTADOR DE FPS (abre la configuracion de Windows ya lista).",
                    "2. Abre Fortnite.",
                    "3. Presiona la tecla Windows + G, fija el widget de Rendimiento y elige FPS.",
                    "Alternativa NVIDIA: dentro del juego presiona Alt + Z y activa el overlay de rendimiento."
                }
                : new[]
                {
                    "1. Da clic en ACTIVAR CONTADOR DE FPS (abre la configuracion de Windows ya lista).",
                    "2. Abre Fortnite.",
                    "3. Presiona la tecla Windows + G, fija el widget de Rendimiento y elige FPS."
                };

            StepsPanel.Children.Clear();
            foreach (var s in steps)
            {
                StepsPanel.Children.Add(new TextBlock
                {
                    Text = s,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    Foreground = (Brush)FindResource(s.StartsWith("Alternativa") ? "NeonBlueBrush" : "TextPrimaryBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 3)
                });
            }
        }

        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            EnableGameBar();
            try { Process.Start(new ProcessStartInfo("ms-settings:gaming-gamebar") { UseShellExecute = true }); }
            catch { }

            MainButton.Content = "CONTADOR ACTIVADO ✓";
            MainButton.IsEnabled = false;
            RecoTitle.Text = "LISTO - SIGUE LOS PASOS DE ABAJO";
        }

        // Habilita las funciones de Game Bar necesarias para el overlay de rendimiento
        private static void EnableGameBar()
        {
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore"))
                    k.SetValue("GameDVR_Enabled", 1, RegistryValueKind.DWord);
                using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\GameDVR"))
                    k.SetValue("AppCaptureEnabled", 1, RegistryValueKind.DWord);
                using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar"))
                    k.SetValue("UseNexusForGameBarEnabled", 1, RegistryValueKind.DWord);
            }
            catch { }
        }
    }
}
