using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace FortniteBoost.Services
{
    /// <summary>
    /// Huella unica del PC (UUID de placa + ID de CPU + serial de board + MachineGuid de Windows).
    /// Si el cliente formatea Windows o cambia la board, el codigo cambia y debe pedir uno nuevo.
    /// </summary>
    public static class HardwareId
    {
        private static byte[]? _cached;

        /// <summary>8 bytes que identifican este PC. Van dentro del codigo de activacion.</summary>
        public static byte[] GetHash8()
        {
            if (_cached != null) return _cached;

            string raw = string.Join("|",
                Wmi("SELECT UUID FROM Win32_ComputerSystemProduct", "UUID"),
                Wmi("SELECT ProcessorId FROM Win32_Processor", "ProcessorId"),
                Wmi("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber"),
                MachineGuid());

            byte[] full = SHA256.HashData(Encoding.UTF8.GetBytes("VELOX-HWID-v1|" + raw));
            _cached = full[..8];
            return _cached;
        }

        /// <summary>
        /// Codigo que el cliente te manda por WhatsApp. Formato XXXX-XXXX-XXXX-XXXX.
        /// = 8 bytes de huella + 2 bytes de verificacion (detecta si lo copiaron mal).
        /// </summary>
        public static string GetRequestCode()
        {
            byte[] id = GetHash8();
            byte[] check = SHA256.HashData(id)[..2];
            string b32 = Base32.Encode(id.Concat(check).ToArray()); // 16 caracteres
            return $"{b32[..4]}-{b32[4..8]}-{b32[8..12]}-{b32[12..16]}";
        }

        private static string Wmi(string query, string prop)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(query);
                foreach (var item in searcher.Get())
                {
                    string v = item[prop]?.ToString()?.Trim() ?? "";
                    if (v.Length > 0) return v;
                }
            }
            catch { /* si WMI falla seguimos con lo demas */ }
            return "NA";
        }

        private static string MachineGuid()
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                return key?.GetValue("MachineGuid")?.ToString() ?? "NA";
            }
            catch { return "NA"; }
        }
    }
}
