using System.Collections.Generic;
using System.Management;

namespace FortniteBoost.Services
{
    public class HardwareService
    {
        public List<string> GetHardwareInfo()
        {
            var info = new List<string>();

            // CPU
            var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            foreach (var item in searcher.Get())
            {
                info.Add("CPU: " + item["Name"]);
            }

            // RAM
            searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
            foreach (var item in searcher.Get())
            {
                info.Add("RAM: " + item["Capacity"]);
            }

            // GPU
            searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
            foreach (var item in searcher.Get())
            {
                info.Add("GPU: " + item["Name"]);
            }

            return info;
        }
    }
}
