using System.Management;

namespace FortniteBoost.Services
{
    public static class HardwareInfo
    {
        public static string GetCPUInfo()
        {
            string cpuName = "Desconocido";
            var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                cpuName = obj["Name"]?.ToString() ?? "Desconocido";
            }
            return cpuName;
        }

        public static string GetGPUInfo()
        {
            string gpuName = "Desconocido";
            var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                gpuName = obj["Name"]?.ToString() ?? "Desconocido";
            }
            return gpuName;
        }

        public static string GetRAMInfo()
        {
            ulong totalRam = 0;
            var searcher = new ManagementObjectSearcher("select Capacity from Win32_PhysicalMemory");
            foreach (ManagementObject obj in searcher.Get())
            {
                totalRam += (ulong)(obj["Capacity"] ?? 0);
            }
            return $"{totalRam / (1024 * 1024 * 1024)} GB";
        }
    }
}
