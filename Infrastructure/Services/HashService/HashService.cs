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
    }
}