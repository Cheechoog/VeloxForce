using System.Windows;
using System.Windows.Controls;
using FortniteBoost.Services;

namespace FortniteBoost.Pages
{
    public partial class HardwarePage : UserControl
    {
        public HardwarePage()
        {
            InitializeComponent();
            Loaded += (_, _) => Load();
        }

        private void Load()
        {
            CpuText.Text = HardwareMonitor.CpuName;
            GpuText.Text = HardwareMonitor.GpuName;
            RamText.Text = $"{HardwareMonitor.RamTotalGb} GB";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => Load();
    }
}
