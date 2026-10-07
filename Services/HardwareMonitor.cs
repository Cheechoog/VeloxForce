using System.Management;

namespace FortniteBoost.Services
{
    /// <summary>
    /// Lee hardware real usando WMI (System.Management, ya referenciado).
    /// Nombres una sola vez (son fijos) y porcentajes de uso bajo demanda.
    /// No usa PerformanceCounter para no agregar paquetes NuGet.
    /// </summary>
    public static class HardwareMonitor
    {
        private static string? _cpuName, _gpuName;
        private static double _ramTotalGb = -1;

        public static string CpuName => _cpuName ??= QueryFirst("SELECT Name FROM Win32_Processor", "Name");
        public static string GpuName => _gpuName ??= QueryFirst("SELECT Name FROM Win32_VideoController", "Name");

        public static double RamTotalGb
        {
            get
            {
                if (_ramTotalGb > 0) return _ramTotalGb;
                try
                {
                    double total = 0;
                    using var s = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
                    foreach (var o in s.Get()) total += Convert.ToDouble(o["Capacity"]);
                    _ramTotalGb = Math.Round(total / (1024d * 1024 * 1024), 1);
                }
                catch { _ramTotalGb = 0; }
                return _ramTotalGb;
            }
        }

        /// <summary>Uso de CPU en % (0-100). Puede tardar ~200ms, llamar en background.</summary>
        public static int CpuUsagePercent()
        {
            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
                foreach (var o in s.Get())
                    return (int)Math.Round(Convert.ToDouble(o["PercentProcessorTime"]));
            }
            catch { }
            return -1;
        }

        /// <summary>Uso de RAM en % (0-100).</summary>
        public static int RamUsagePercent()
        {
            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (var o in s.Get())
                {
                    double free = Convert.ToDouble(o["FreePhysicalMemory"]);
                    double tot = Convert.ToDouble(o["TotalVisibleMemorySize"]);
                    if (tot > 0) return (int)Math.Round((tot - free) / tot * 100);
                }
            }
            catch { }
            return -1;
        }

        /// <summary>Uso de GPU en % sumando los engines 3D (puede no existir en equipos viejos).</summary>
        public static int GpuUsagePercent()
        {
            try
            {
                double total = 0; bool any = false;
                using var s = new ManagementObjectSearcher(
                    "SELECT UtilizationPercentage, Name FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
                foreach (var o in s.Get())
                {
                    string name = o["Name"]?.ToString() ?? "";
                    if (name.Contains("engtype_3D"))
                    {
                        total += Convert.ToDouble(o["UtilizationPercentage"]); any = true;
                    }
                }
                if (any) return Math.Min(100, (int)Math.Round(total));
            }
            catch { }
            return -1;
        }

        private static string QueryFirst(string query, string prop)
        {
            try
            {
                using var s = new ManagementObjectSearcher(query);
                foreach (var o in s.Get())
                {
                    string v = o[prop]?.ToString()?.Trim() ?? "";
                    if (v.Length > 0) return v;
                }
            }
            catch { }
            return "Desconocido";
        }
    }
}
