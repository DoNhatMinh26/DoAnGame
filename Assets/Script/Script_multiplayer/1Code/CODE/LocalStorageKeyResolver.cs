using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Tạo prefix key local riêng theo project hiện tại.
    /// Dùng để main/clone không đụng chung PlayerPrefs.
    /// </summary>
    public static class LocalStorageKeyResolver
    {
        private static string cachedPrefix;

        public static string Key(string baseKey)
        {
            return $"{GetPrefix()}:{baseKey}";
        }

        private static string GetPrefix()
        {
            if (!string.IsNullOrEmpty(cachedPrefix))
                return cachedPrefix;

            string source = $"{Application.companyName}|{Application.productName}|{Application.dataPath}";
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(source));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                cachedPrefix = $"local_{builder}";
                return cachedPrefix;
            }
        }
    }
}