using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace FortniteBoost
{
    /// <summary>
    /// Ventana de ejecucion "bonita": corre un script y muestra solo pasos
    /// amigables + progreso. Oculta la salida tecnica (rutas, errores crudos,
    /// codigos de salida). Esos detalles se guardan en un log aparte por si
    /// hiciera falta diagnosticar.
    /// </summary>
    public partial class ExecutionWindow : MetroWindow
    {
        private readonly string _scriptRelative;
        private readonly string _args;
        private TextBlock? _currentStep;
        private bool _rebootRequested;
        private readonly List<string> _summary = new();

        public ExecutionWindow(string friendlyTitle, string scriptRelative, string args)
        {
            InitializeComponent();
            TitleText.Text = friendlyTitle;
            Title = friendlyTitle;
            _scriptRelative = scriptRelative;
            _args = args;
            Loaded += async (_, _) => await Run();
        }

        private async System.Threading.Tasks.Task Run()
        {
            string? script = ResolveScript(_scriptRelative);
            if (script == null)
            {
                Fail("No se encontraron los archivos de optimizacion. Reinstala la aplicacion.");
                return;
            }

            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VeloxForge", "logs");
            Directory.CreateDirectory(logPath);
            string logFile = Path.Combine(logPath, $"run_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {_args}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            try
            {
                using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var writer = new StreamWriter(logFile) { AutoFlush = true };

                p.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null) return;
                    writer.WriteLine(e.Data);
                    Dispatcher.Invoke(() => HandleLine(e.Data));
                };
                p.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null) writer.WriteLine("ERR: " + e.Data); // solo al log, nunca a la vista
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                await System.Threading.Tasks.Task.Run(() => p.WaitForExit());
                writer.Dispose();

                Finish();
            }
            catch (Exception)
            {
                Fail("No se pudo aplicar la optimizacion. Intenta ejecutar la app como administrador.");
            }
        }

        // ── Interpreta cada linea del script ──
        private void HandleLine(string line)
        {
            if (line.StartsWith("PROGRESS:"))
            {
                if (int.TryParse(line.Substring(9).Trim(), out int pct)) SetProgress(pct);
            }
            else if (line.StartsWith("STEP:"))
            {
                MarkCurrentDone();
                AddStep(Friendly(line.Substring(5).Trim()));
            }
            else if (line.StartsWith("RESULT:"))
            {
                string r = line.Substring(7).Trim();
                if (r.StartsWith("SUMMARY:")) return;              // interno, no se muestra
                _summary.Add(r.Replace("OK ", "").Replace("WARN ", ""));
            }
            else if (line.Trim() == "REBOOT_REQUIRED")
            {
                _rebootRequested = true;
            }
            // cualquier otra linea (stdout crudo) se ignora en la vista
        }

        private void SetProgress(int pct)
        {
            pct = Math.Clamp(pct, 0, 100);
            Progress.Value = pct;
            PercentText.Text = pct + "%";
        }

        private void AddStep(string text)
        {
            var tb = new TextBlock
            {
                Text = "•  " + text,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                Margin = new Thickness(0, 3, 0, 3),
                TextWrapping = TextWrapping.Wrap
            };
            StepList.Children.Add(tb);
            _currentStep = tb;
            StepScroll.ScrollToEnd();
        }

        private void MarkCurrentDone()
        {
            if (_currentStep == null) return;
            _currentStep.Text = "✓  " + _currentStep.Text.Substring(3);
            _currentStep.Foreground = (Brush)FindResource("NeonGreenBrush");
        }

        private void Finish()
        {
            MarkCurrentDone();
            SetProgress(100);
            SubtitleText.Text = "¡Listo! La optimizacion termino.";
            DoneSummary.Text = _rebootRequested
                ? "Reinicia el PC para que todo quede aplicado."
                : "Ya puedes abrir Fortnite.";
            CloseButton.IsEnabled = true;

            if (_rebootRequested)
            {
                var r = MessageBox.Show(
                    "Algunos cambios necesitan reiniciar el PC para activarse.\n\n¿Reiniciar ahora?",
                    "VeloxForge", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo("shutdown", "/r /t 5") { CreateNoWindow = true, UseShellExecute = true }); }
                    catch { }
                }
            }
        }

        private void Fail(string message)
        {
            SubtitleText.Text = message;
            SubtitleText.Foreground = (Brush)FindResource("NeonRedBrush");
            CloseButton.IsEnabled = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // ── Busca el script en varias ubicaciones (instalado y en desarrollo) ──
        private static string? ResolveScript(string relative)
        {
            string direct = Path.Combine(AppContext.BaseDirectory, relative);
            if (File.Exists(direct)) return direct;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        // ── Traduce los pasos tecnicos a lenguaje sencillo ──
        private static string Friendly(string raw)
        {
            string s = raw.ToLowerInvariant();
            (string key, string nice)[] map =
            {
                ("game dvr",        "Desactivando la grabacion de juego de Windows"),
                ("xbox",            "Desactivando la grabacion de juego de Windows"),
                ("game mode",       "Activando el Modo Juego"),
                ("cpu responsiv",   "Priorizando el procesador para el juego"),
                ("prioridad",       "Priorizando el procesador para el juego"),
                ("plan de energia", "Aplicando plan de energia de maximo rendimiento"),
                ("ultimate",        "Aplicando plan de energia de maximo rendimiento"),
                ("power throttl",   "Quitando el ahorro de energia del procesador"),
                ("core parking",    "Activando todos los nucleos del procesador"),
                ("hags",            "Activando la aceleracion por hardware de la GPU"),
                ("gpu tweaks",      "Optimizando la tarjeta grafica"),
                ("timer resolution","Afinando la respuesta del sistema"),
                ("ram",             "Optimizando la memoria"),
                ("servicios",       "Desactivando servicios innecesarios"),
                ("cerrando proce",  "Cerrando programas en segundo plano"),
                ("efectos visual",  "Ajustando Windows para mejor rendimiento"),
                ("fondo",           "Aplicando un fondo liviano"),
                ("notificacion",    "Silenciando notificaciones mientras juegas"),
                ("red ",            "Optimizando la conexion de red"),
                ("nagle",           "Optimizando la conexion de red"),
                ("mouse",           "Quitando la aceleracion del mouse"),
                ("disco",           "Optimizando el disco"),
                ("trim",            "Optimizando el disco"),
                ("gameusersettings","Aplicando los mejores graficos para FPS en Fortnite"),
                ("fortnite",        "Aplicando los mejores graficos para FPS en Fortnite"),
                ("ini",             "Aplicando los mejores graficos para FPS en Fortnite"),
                ("limpieza",        "Limpiando archivos temporales"),
                ("completada",      "Finalizando"),
                ("inicio",          "Preparando la optimizacion"),
                ("streaming",       "Preparando la optimizacion"),
            };
            foreach (var (key, nice) in map)
                if (s.Contains(key)) return nice;
            return "Optimizando el sistema";
        }
    }
}
