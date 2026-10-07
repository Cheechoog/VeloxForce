using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FortniteBoost
{
    public partial class ProfilesWindow : MahApps.Metro.Controls.MetroWindow
    {
        public ProfilesWindow()
        {
            InitializeComponent();
        }

        // Añade una línea al LogBox (thread-safe)
        private void AddLog(string text)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    LogBox.Items.Add(text);
                    LogBox.ScrollIntoView(LogBox.Items[LogBox.Items.Count - 1]);
                }
                catch { /* evitar que un fallo en UI rompa el flujo */ }
            });
        }

        // Pregunta simple sí/no
        private bool AskConfirm(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        // Obtiene ruta absoluta al script buscando en varios lugares (incluye tu ruta OneDrive)
        private string GetScriptFullPath(string relativePath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            string candidate = Path.GetFullPath(Path.Combine(baseDir, relativePath));
            if (File.Exists(candidate)) return candidate;

            candidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativePath));
            if (File.Exists(candidate)) return candidate;

            string oneDriveCandidate = Path.Combine(
                "C:\\Users\\nelso\\OneDrive\\Documentos\\APPS\\Optimizacion Juegos\\FortniteBoost\\FortniteBoost\\FortniteBoost",
                relativePath.Replace('/', '\\').TrimStart('\\'));
            if (File.Exists(oneDriveCandidate)) return oneDriveCandidate;

            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                candidate = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* ignore */ }

            return Path.GetFullPath(Path.Combine(baseDir, relativePath));
        }

        // Ejecuta PowerShell y captura stdout/stderr; guarda salida en archivo PowerShellOutput.txt
        private async Task<int> RunPowerShellCapture(string scriptFullPath, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFullPath}\" {args}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var outputSb = new StringBuilder();
            var errorSb = new StringBuilder();

            var tcs = new TaskCompletionSource<int>();
            bool promptDetected = false;

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                outputSb.AppendLine(e.Data);
                AddLog(e.Data);

                string line = e.Data.Trim();

                if (line.StartsWith("PROGRESS:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int percent))
                    {
                        UpdateProgressAnimated(percent);
                        Dispatcher.Invoke(() => ProgressText.Text = $"{percent}%");
                    }
                    return;
                }

                if (line.StartsWith("STEP:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        string stepText = parts[1].Trim();
                        Dispatcher.Invoke(() => ProgressText.Text = ProgressText.Text + $"  • {stepText}");
                    }
                    return;
                }

                if (!promptDetected && line.StartsWith("PROMPT:", StringComparison.OrdinalIgnoreCase))
                {
                    promptDetected = true;
                    AddLog("⚠️ PROMPT detectado en script: " + line);
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                            AddLog("Proceso PowerShell en background detenido para abrir consola elevada.");
                        }
                    }
                    catch (Exception exKill)
                    {
                        AddLog("ERR al detener proceso: " + exKill.Message);
                    }

                    SaveProcessOutputToFile(scriptFullPath, outputSb.ToString(), errorSb.ToString());
                    RunPowerShellElevatedVisible(scriptFullPath, args);
                    tcs.TrySetResult(-2);
                    return;
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                errorSb.AppendLine(e.Data);
                AddLog("ERR: " + e.Data);
            };

            try
            {
                bool started = process.Start();
                if (!started)
                {
                    AddLog("⚠️ No se pudo iniciar el proceso PowerShell (Start devolvió false).");
                    return -1;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var waitTask = Task.Run(() => process.WaitForExit());
                var completed = await Task.WhenAny(waitTask, tcs.Task);

                if (completed == tcs.Task)
                {
                    SaveProcessOutputToFile(scriptFullPath, outputSb.ToString(), errorSb.ToString());
                    return tcs.Task.Result;
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (ProfileProgress.Value < 100)
                        {
                            UpdateProgressAnimated(100);
                            ProgressText.Text = "100%";
                        }
                    });

                    SaveProcessOutputToFile(scriptFullPath, outputSb.ToString(), errorSb.ToString());

                    if (errorSb.Length > 0)
                    {
                        string stderr = errorSb.ToString();
                        if (stderr.IndexOf("Falta la llave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            stderr.IndexOf("ParserError", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            stderr.IndexOf("UnexpectedToken", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            AddLog("✗ Error de sintaxis detectado en el script. Revisa PowerShellOutput.txt para detalles.");
                        }
                    }

                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                AddLog("⚠️ Excepción al iniciar PowerShell: " + ex.Message);
                try { SaveProcessOutputToFile(scriptFullPath, outputSb.ToString(), errorSb.ToString()); } catch { }
                throw;
            }
        }

        // Guarda stdout/stderr en PowerShellOutput.txt junto al script
        private void SaveProcessOutputToFile(string scriptFullPath, string stdout, string stderr)
        {
            try
            {
                string scriptDir = Path.GetDirectoryName(scriptFullPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                string outPath = Path.Combine(Path.GetTempPath(), "PowerShellOutput.txt");
                var sb = new StringBuilder();
                sb.AppendLine("=== STDOUT ===");
                sb.AppendLine(stdout ?? "");
                sb.AppendLine();
                sb.AppendLine("=== STDERR ===");
                sb.AppendLine(stderr ?? "");
                File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
                AddLog("Salida guardada en: " + outPath);
            }
            catch (Exception ex)
            {
                AddLog("ERR al guardar PowerShellOutput.txt: " + ex.Message);
            }
        }

        // Método auxiliar: anima la ProgressBar desde su valor actual hasta 'targetPercent'
        private void UpdateProgressAnimated(int targetPercent)
        {
            if (targetPercent < 0) targetPercent = 0;
            if (targetPercent > 100) targetPercent = 100;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    double from = ProfileProgress.Value;
                    double to = targetPercent;

                    if (Math.Abs(to - from) < 0.5)
                    {
                        ProfileProgress.Value = to;
                        ProgressText.Text = $"{(int)to}%";
                        return;
                    }

                    var durationMs = Math.Min(600, Math.Max(150, (int)(Math.Abs(to - from) * 8)));
                    var anim = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMs))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    ProfileProgress.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, anim);
                }
                catch (Exception ex)
                {
                    ProfileProgress.Value = targetPercent;
                    ProgressText.Text = $"{targetPercent}%";
                    AddLog("ERR animación progreso: " + ex.Message);
                }
            });
        }

        // Fallback: abrir PowerShell visible y elevado (no captura salida)
        private void RunPowerShellElevatedVisible(string scriptFullPath, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -NoExit -File \"{scriptFullPath}\" {args}",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                AddLog("✅ PowerShell elevado abierto (ventana visible). Sigue las instrucciones en la consola.");
            }
            catch (Exception ex)
            {
                AddLog("✗ No se pudo abrir PowerShell elevado: " + ex.Message);
            }
        }

        // ── ACTUALIZADO: ahora incluye todos los parámetros nuevos del script v3 ──
        private string BuildArgsAll(
            bool cpu, bool energy, bool services, bool fortnite,
            bool hags, bool ram, bool visuals, bool coreParking,
            bool network = true,
            bool gameDvr = true,
            bool wallpaper = true,
            bool notifications = true,
            bool mouseInput = true,
            bool powerThrottle = true,
            bool timerRes = true,
            bool killProcesses = true,
            bool disk = true,
            bool gameMode = true,
            bool gpu = true)
        {
            string Arg(string name, bool v) => $"-{name} {(v ? "yes" : "no")}";
            var sb = new StringBuilder();
            sb.Append(Arg("Cpu", cpu)).Append(' ');
            sb.Append(Arg("Energy", energy)).Append(' ');
            sb.Append(Arg("Services", services)).Append(' ');
            sb.Append(Arg("Fortnite", fortnite)).Append(' ');
            sb.Append(Arg("HAGS", hags)).Append(' ');
            sb.Append(Arg("RAM", ram)).Append(' ');
            sb.Append(Arg("Visuals", visuals)).Append(' ');
            sb.Append(Arg("CoreParking", coreParking)).Append(' ');
            sb.Append(Arg("Network", network)).Append(' ');
            sb.Append(Arg("GameDVR", gameDvr)).Append(' ');
            sb.Append(Arg("Wallpaper", wallpaper)).Append(' ');
            sb.Append(Arg("Notifications", notifications)).Append(' ');
            sb.Append(Arg("MouseInput", mouseInput)).Append(' ');
            sb.Append(Arg("PowerThrottle", powerThrottle)).Append(' ');
            sb.Append(Arg("TimerRes", timerRes)).Append(' ');
            sb.Append(Arg("KillProcesses", killProcesses)).Append(' ');
            sb.Append(Arg("Disk", disk)).Append(' ');
            sb.Append(Arg("GameMode", gameMode)).Append(' ');
            sb.Append(Arg("GPU", gpu));
            return sb.ToString();
        }

        // ── ACTUALIZADO: CompetitiveProfile_Click con los nuevos parámetros ──
        private async void CompetitiveProfile_Click(object sender, RoutedEventArgs e)
        {
            LogBox.Visibility = Visibility.Visible;
            ProfileProgress.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;

            LogBox.Items.Clear();
            AddLog("Iniciando perfil competitivo (paso a paso)...");

            // Confirmaciones originales
            bool cpu = AskConfirm("Confirmar CPU", "¿Habilitar todos los núcleos del procesador?");
            bool energy = AskConfirm("Confirmar Energía", "¿Activar plan Ultimate / Alto Rendimiento?");
            bool services = AskConfirm("Confirmar Servicios", "¿Desactivar servicios innecesarios? (SysMain, DiagTrack, WSearch y más)");
            bool fortnite = AskConfirm("Confirmar Fortnite", "¿Aplicar ajustes de Fortnite? (Performance Mode, sombras OFF, FPS visible)");
            bool hags = AskConfirm("Confirmar HAGS", "¿Activar HAGS (Hardware Accelerated GPU Scheduling)? Requiere reinicio.");
            bool ram = AskConfirm("Confirmar RAM", "¿Optimizar RAM? (kernel en RAM, Prefetch/Superfetch OFF)");
            bool visuals = AskConfirm("Confirmar Visuals", "¿Desactivar animaciones, transparencias y efectos visuales de Windows?");
            bool coreParking = AskConfirm("Confirmar CoreParking", "¿Desactivar Core Parking? (todos los núcleos activos siempre)");

            // Confirmaciones nuevas (parámetros nuevos del script v3)
            bool network = AskConfirm("Confirmar Red", "¿Optimizar red? (Nagle's Algorithm OFF, DNS flush — reduce ping ~5-15ms)");
            bool gameDvr = AskConfirm("Confirmar Game DVR", "¿Desactivar Game DVR y Xbox Bar? (reduce stutter de GPU)");
            bool wallpaper = AskConfirm("Confirmar Fondo", "¿Poner fondo negro sólido? (libera recursos de GPU)");
            bool notifications = AskConfirm("Confirmar Notificaciones", "¿Desactivar notificaciones y apps en segundo plano?");
            bool mouseInput = AskConfirm("Confirmar Mouse", "¿Desactivar aceleración del mouse? (aim más preciso y consistente)");
            bool powerThrottle = AskConfirm("Confirmar Power Throttle", "¿Desactivar Power Throttling? (sin estrangulamiento de CPU/GPU) Requiere reinicio.");
            bool timerRes = AskConfirm("Confirmar Timer", "¿Optimizar Timer Resolution? (menos stutter, 1% lows más estables) Requiere reinicio.");
            bool killProcesses = AskConfirm("Confirmar Kill procesos", "¿Cerrar Chrome, OneDrive, Discord y otras apps en segundo plano? (libera RAM inmediatamente)");
            bool disk = AskConfirm("Confirmar Disco", "¿Optimizar disco? (TRIM/desfrag según SSD o HDD, timestamps OFF)");
            bool gameMode = AskConfirm("Confirmar Game Mode", "¿Activar Game Mode avanzado? (scheduler de Windows prioriza Fortnite)");
            bool gpu = AskConfirm("Confirmar GPU", "¿Aplicar tweaks de GPU? (MPO OFF, prioridad gráfica, telemetría NVIDIA OFF)");

            ProfileProgress.Value = 5;
            ProgressText.Text = "5%";

            string scriptFull = GetScriptFullPath(@"Scripts\OptimizeCompetitive.ps1");
            AddLog("Ruta script (buscando): " + scriptFull);

            if (!File.Exists(scriptFull))
            {
                AddLog("⚠️ No se encontró el script en: " + scriptFull);
                AddLog("Asegúrate de que OptimizeCompetitive.ps1 esté en la carpeta Scripts y tenga 'Copiar siempre' en sus propiedades.");
                return;
            }

            string args = BuildArgsAll(
                cpu, energy, services, fortnite, hags, ram, visuals, coreParking,
                network: network,
                gameDvr: gameDvr,
                wallpaper: wallpaper,
                notifications: notifications,
                mouseInput: mouseInput,
                powerThrottle: powerThrottle,
                timerRes: timerRes,
                killProcesses: killProcesses,
                disk: disk,
                gameMode: gameMode,
                gpu: gpu);

            AddLog("Argumentos construidos: " + args);

            try
            {
                AddLog("Ejecutando PowerShell (captura de salida)...");
                int exitCode = await RunPowerShellCapture(scriptFull, args);

                if (exitCode == -2)
                {
                    AddLog("⚠️ Se abrió PowerShell elevado para interacción. Completa la consola y revisa el log.");
                    return;
                }

                AddLog($"Proceso finalizado. Código de salida: {exitCode}");
                ProfileProgress.Value = 100;
                ProgressText.Text = "100%";

                string? scriptDir = Path.GetDirectoryName(scriptFull);
                if (!string.IsNullOrEmpty(scriptDir))
                {
                    string rebootFlagPath = Path.Combine(scriptDir, "REBOOT_REQUIRED.flag");
                    if (File.Exists(rebootFlagPath))
                    {
                        var result = MessageBox.Show(
                            "Algunos cambios requieren reinicio para activarse:\n• HAGS\n• Power Throttling\n• Timer Resolution\n• CPU cores (bcdedit)\n\n¿Reiniciar ahora?",
                            "Reinicio requerido",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            AddLog("Reiniciando equipo...");
                            Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { CreateNoWindow = true, UseShellExecute = true });
                        }
                        else
                        {
                            AddLog("Reinicio pospuesto por el usuario. Recuerda reiniciar para completar la optimización.");
                        }
                    }
                    else
                    {
                        AddLog("✅ No se requiere reinicio. ¡Abre Fortnite y disfruta los FPS!");
                    }
                }
                else
                {
                    AddLog("⚠️ No se pudo determinar el directorio del script para comprobar reinicio.");
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                AddLog("✗ Permiso denegado al ejecutar PowerShell: " + uaEx.Message);
                AddLog("Intentando abrir PowerShell elevado (ventana visible) para que aceptes UAC...");
                RunPowerShellElevatedVisible(scriptFull, args);
            }
            catch (Exception ex)
            {
                AddLog("✗ Error durante ejecución: " + ex.Message);
                AddLog("Intentando abrir PowerShell elevado (ventana visible) como fallback...");
                RunPowerShellElevatedVisible(scriptFull, args);
            }
        }

        // --- PERFIL STREAMING (sin cambios) ---
        private async void StreamingProfile_Click(object sender, RoutedEventArgs e)
        {
            LogBox.Visibility = Visibility.Visible;
            ProfileProgress.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;

            LogBox.Items.Clear();
            AddLog("Iniciando perfil streaming...");

            bool fortnite = AskConfirm("Confirmar Fortnite", "¿Aplicar ajustes streaming para calidad media y FPS estables?");
            bool services = AskConfirm("Confirmar Servicios", "¿Desactivar servicios no críticos para streaming?");

            ProfileProgress.Value = 10;
            ProgressText.Text = "10%";

            string scriptFull = GetScriptFullPath(@"Scripts\OptimizeStreaming.ps1");
            AddLog("Ruta script streaming: " + scriptFull);
            if (!File.Exists(scriptFull))
            {
                AddLog("⚠️ No se encontró el script: " + scriptFull);
                return;
            }

            string args = $"-Fortnite {(fortnite ? "yes" : "no")} -Services {(services ? "yes" : "no")}";

            try
            {
                AddLog("Ejecutando script streaming (captura)...");
                int exitCode = await RunPowerShellCapture(scriptFull, args);
                AddLog($"✅ Streaming finalizado. Código salida: {exitCode}");
                ProfileProgress.Value = 100;
                ProgressText.Text = "100%";
            }
            catch
            {
                AddLog("✗ Error al ejecutar streaming. Abriendo PowerShell elevado visible...");
                RunPowerShellElevatedVisible(scriptFull, args);
            }
        }

        // --- PERFIL DEFAULT / RESTAURAR (sin cambios) ---
        private async void DefaultProfile_Click(object sender, RoutedEventArgs e)
        {
            LogBox.Visibility = Visibility.Visible;
            ProfileProgress.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;

            LogBox.Items.Clear();
            AddLog("Restaurando perfil default...");

            ProfileProgress.Value = 10;
            ProgressText.Text = "10%";

            string scriptFull = GetScriptFullPath(@"Scripts\RestoreDefault.ps1");
            AddLog("Ruta script restore: " + scriptFull);
            if (!File.Exists(scriptFull))
            {
                AddLog("⚠️ No se encontró el script: " + scriptFull);
                return;
            }

            AddLog("Ejecutando script de restauración...");
            try
            {
                int exitCode = await RunPowerShellCapture(scriptFull, "");
                AddLog($"✅ Restauración finalizada. Código de salida: {exitCode}");
                ProfileProgress.Value = 100;
                ProgressText.Text = "100%";
            }
            catch
            {
                AddLog("✗ Error al ejecutar restauración. Abriendo PowerShell elevado visible...");
                RunPowerShellElevatedVisible(scriptFull, "");
            }
        }

        // --- Botones auxiliares (sin cambios) ---
        private void OpenNotificationsSettings_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("ms-settings:notifications") { UseShellExecute = true }); }
            catch (Exception ex) { AddLog("ERR abrir Notificaciones: " + ex.Message); }
        }

        private void OpenDisplaySettings_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true }); }
            catch (Exception ex) { AddLog("ERR abrir Display: " + ex.Message); }
        }

        private void PrioritizeFortnite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var procs = Process.GetProcessesByName("FortniteClient-Win64-Shipping");
                if (procs.Length == 0) procs = Process.GetProcessesByName("FortniteClient");
                if (procs.Length == 0)
                {
                    AddLog("⚠️ No se detectó proceso de Fortnite. Inicia el juego y vuelve a intentar.");
                    return;
                }

                foreach (var p in procs)
                {
                    try
                    {
                        p.PriorityClass = ProcessPriorityClass.High;
                        AddLog($"✅ Prioridad establecida a High para PID {p.Id} ({p.ProcessName})");
                    }
                    catch (Exception ex)
                    {
                        AddLog("ERR Priorizar proceso: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog("ERR Priorizar Fortnite: " + ex.Message);
            }
        }
    }
}