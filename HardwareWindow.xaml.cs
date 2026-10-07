using MahApps.Metro.Controls;
using System.Windows;
using System.Management;

namespace FortniteBoost
{
    public partial class HardwareWindow : MetroWindow
    {
        public HardwareWindow()
        {
            InitializeComponent();
            LoadHardwareInfo();
        }

        private void LoadHardwareInfo()
        {
            CpuText.Text = "CPU: " + GetCpuName();
            GpuText.Text = "GPU: " + GetGpuName();
            RamText.Text = "RAM: " + GetRamSize() + " GB";
        }

        private void RefreshHardware_Click(object sender, RoutedEventArgs e)
        {
            LoadHardwareInfo();
        }

        private string GetCpuName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Name from Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                        return item["Name"]?.ToString() ?? "Desconocido";
                }
            }
            catch { }
            return "Desconocido";
        }

        private string GetGpuName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController"))
                {
                    foreach (var item in searcher.Get())
                        return item["Name"]?.ToString() ?? "Desconocido";
                }
            }
            catch { }
            return "Desconocido";
        }


        private double GetRamSize()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Capacity from Win32_PhysicalMemory"))
                {
                    double total = 0;
                    foreach (var item in searcher.Get())
                        total += Convert.ToDouble(item["Capacity"]);
                    return Math.Round(total / (1024 * 1024 * 1024), 2);
                }
            }
            catch { }
            return 0;
        }
    }
}
