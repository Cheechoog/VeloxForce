using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace FortniteBoost.Services
{
    public enum LicenseStatus { Valid, NoLicense, Invalid, OtherPc, Expired, ClockTampered, NotConfigured }

    public sealed class LicenseResult
    {
        public LicenseStatus Status { get; init; }
        public DateTime? Expires { get; init; }   // hora local
        public string Message { get; init; } = "";
        public bool IsValid => Status == LicenseStatus.Valid;
    }

    /// <summary>
    /// Licencias offline firmadas.
    /// - VeloxKeygen (solo en tu PC) firma con la LLAVE PRIVADA.
    /// - La app solo tiene la LLAVE PUBLICA: puede verificar codigos, pero NO fabricarlos.
    /// Formato del codigo (81 bytes en Base32 = 130 caracteres):
    ///   [0] version=1 | [1..8] huella PC | [9..12] emitido (unix) | [13..16] vence (unix) | [17..80] firma ECDSA P-256
    /// </summary>
    public static class LicenseService
    {
        // ===== CONFIGURA ESTO =====
        public const string AppName = "VeloxForge";
        public static string WhatsAppNumber => new string(Convert.FromBase64String("b21pa2hobmJuYmNv").Select(b => (char)(b ^ 0x5A)).ToArray()); // numero cifrado
        private const string PublicKeyB64 = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEkq81ERFzQiirIF90w6LDK759bspeHdyhDCR+l7vk6+AODOEROt8Eosq8nxFHVMNNj5ScRlGeh8DCWYPsPGaZFQ=="; // la imprime VeloxKeygen la primera vez
        // ==========================

        private const string RegPath = @"Software\VeloxForge";
        private const int PayloadLen = 17;
        private const int SignatureLen = 64;
        private const long ClockToleranceSec = 3600; // 1 hora de margen

        public static LicenseResult CheckStored()
        {
            string? code = ReadReg("Lic");
            if (string.IsNullOrWhiteSpace(code))
                return new LicenseResult { Status = LicenseStatus.NoLicense, Message = "Esta copia aun no esta activada." };
            return Verify(code);
        }

        public static LicenseResult Activate(string code)
        {
            var result = Verify(code);
            if (result.IsValid) WriteReg("Lic", Clean(code));
            return result;
        }

        private static LicenseResult Verify(string code)
        {
            try
            {
                if (PublicKeyB64.StartsWith("PEGA_"))
                    return Fail(LicenseStatus.NotConfigured, "Falta configurar la llave publica en LicenseService.cs.");

                byte[]? bytes = Base32.Decode(code);
                if (bytes == null || bytes.Length < PayloadLen + SignatureLen)
                    return Fail(LicenseStatus.Invalid, "El codigo de activacion no es valido. Revisalo y vuelve a pegarlo completo.");

                byte[] payload = bytes[..PayloadLen];
                byte[] signature = bytes[PayloadLen..(PayloadLen + SignatureLen)];

                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeyB64), out _);
                if (!ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256) || payload[0] != 1)
                    return Fail(LicenseStatus.Invalid, "El codigo de activacion no es valido. Revisalo y vuelve a pegarlo completo.");

                if (!payload.AsSpan(1, 8).SequenceEqual(HardwareId.GetHash8()))
                    return Fail(LicenseStatus.OtherPc, "Este codigo pertenece a otro equipo. Solicita uno para este PC.");

                long issued = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(9, 4));
                long expires = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(13, 4));
                DateTime expLocal = DateTimeOffset.FromUnixTimeSeconds(expires).LocalDateTime;

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long lastSeen = long.TryParse(ReadReg("LastSeen"), out var ls) ? ls : 0;

                // Reloj atrasado: la fecha actual es anterior a la ultima vez que abrio la app,
                // o anterior a cuando tu emitiste el codigo.
                if (now + ClockToleranceSec < lastSeen || now + ClockToleranceSec < issued)
                    return Fail(LicenseStatus.ClockTampered, "La fecha de Windows parece alterada. Corrige la hora del equipo e intenta de nuevo.");

                WriteReg("LastSeen", Math.Max(now, lastSeen).ToString());

                if (now >= expires)
                    return new LicenseResult
                    {
                        Status = LicenseStatus.Expired,
                        Expires = expLocal,
                        Message = $"Tu licencia vencio el {expLocal:dd/MM/yyyy hh:mm tt}. Solicita un nuevo codigo."
                    };

                return new LicenseResult
                {
                    Status = LicenseStatus.Valid,
                    Expires = expLocal,
                    Message = $"Licencia activa hasta {expLocal:dd/MM/yyyy hh:mm tt}"
                };
            }
            catch
            {
                return Fail(LicenseStatus.Invalid, "El codigo de activacion no es valido. Revisalo y vuelve a pegarlo completo.");
            }
        }

        private static LicenseResult Fail(LicenseStatus s, string msg) => new() { Status = s, Message = msg };

        private static string Clean(string code) =>
            new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        private static string? ReadReg(string name)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegPath);
                return key?.GetValue(name)?.ToString();
            }
            catch { return null; }
        }

        private static void WriteReg(string name, string value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegPath);
                key.SetValue(name, value);
            }
            catch { /* no bloquear la app por esto */ }
        }
    }
}
