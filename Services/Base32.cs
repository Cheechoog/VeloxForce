using System.Text;

namespace FortniteBoost.Services
{
    /// <summary>
    /// Base32 (A-Z, 2-7) sin padding. Se usa porque WhatsApp no deforma estas letras
    /// (con Base64 los "_" se vuelven cursiva y el codigo se rompe al copiarlo).
    /// IMPORTANTE: debe ser identico al Base32 de VeloxKeygen.
    /// </summary>
    public static class Base32
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Encode(byte[] data)
        {
            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = 0, bits = 0;
            foreach (byte b in data)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
                buffer &= (1 << bits) - 1;
            }
            if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        /// <summary>Ignora guiones, espacios y saltos de linea. Devuelve null si hay caracteres invalidos.</summary>
        public static byte[]? Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var bytes = new List<byte>();
            int buffer = 0, bits = 0;
            foreach (char raw in input.ToUpperInvariant())
            {
                if (!char.IsLetterOrDigit(raw)) continue;          // salta "-", espacios, etc.
                char c = raw switch { '0' => 'O', '1' => 'I', '8' => 'B', _ => raw }; // confusiones tipicas
                int v = Alphabet.IndexOf(c);
                if (v < 0) return null;
                buffer = (buffer << 5) | v;
                bits += 5;
                if (bits >= 8)
                {
                    bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
                buffer &= (1 << bits) - 1;
            }
            return bytes.ToArray();
        }
    }
}
