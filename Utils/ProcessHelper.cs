using System.Diagnostics;

namespace FortniteBoost.Utils
{
    public static class ProcessHelper
    {
        public static void RunPowerShellScript(string scriptPath)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process? process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.WaitForExit();
                }
            }
        }
    }
}
