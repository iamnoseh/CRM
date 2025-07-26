using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services.HashService
{
    public class HashService : IHashService
    {
        #region ConvertToHash
        public string ConvertToHash(string rawData)
        {
            try
            {
                using var sha256Hash = SHA256.Create();
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                var builder = new StringBuilder();
                foreach (var t in bytes)
                {
                    builder.Append(t.ToString("x2"));
                }
                return builder.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        #endregion

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
        }

        public async Task<string> GenerateRandomCode(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            await Task.Run(() => rng.GetBytes(bytes));
            
            var code = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                code.Append((bytes[i] % 10).ToString());
            }
            return code.ToString();
        }
    }
}