using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FortniteBoost.Services;

namespace FortniteBoost.Pages
{
    public partial class DashboardPage : UserControl
    {
        private DispatcherTimer? _timer;
        public event Action<string>? NavigateRequested;

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += (_, _) => StartMonitor();
            Unloaded += (_, _) => _timer?.Stop();
        }

        private void GoPerfiles_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("perfiles");

        private void StartMonitor()
        {
            // Nombres fijos (una vez)
            CpuName.Text = HardwareMonitor.CpuName;
            GpuName.Text = HardwareMonitor.GpuName;
            RamTotal.Text = $"{HardwareMonitor.RamTotalGb} GB totales";

            if (_timer == null)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _timer.Tick += async (_, _) => await Refresh();
            }
            _timer.Start();
            _ = Refresh();
        }

        private async Task Refresh()
        {
            // Consultas WMI en background para no congelar la UI
            var (cpu, ram, gpu) = await Task.Run(() =>
                (HardwareMonitor.CpuUsagePercent(),
                 HardwareMonitor.RamUsagePercent(),
                 HardwareMonitor.GpuUsagePercent()));

            SetMetric(CpuPercent, CpuBar, cpu);
            SetMetric(RamPercent, RamBar, ram);
            if (gpu >= 0) SetMetric(GpuPercent, GpuBar, gpu);
            else { GpuPercent.Text = "N/D"; GpuBar.Value = 0; }
        }

        private static void SetMetric(TextBlock label, ProgressBar bar, int value)
        {
            if (value < 0) { label.Text = "--"; bar.Value = 0; return; }
            label.Text = $"{value}%";
            bar.Value = value;
        }
    }
}
