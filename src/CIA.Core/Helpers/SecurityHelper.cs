using System;
using System.Security.Cryptography;
using System.Text;

namespace CIA.Core.Helpers
{
    public static class SecurityHelper
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        /// <summary>
        /// Hashes a password using PBKDF2 with SHA256.
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = ComputeHash(password, salt);

            byte[] combined = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Verifies a password against a stored hash.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                byte[] combined = Convert.FromBase64String(storedHash);
                if (combined.Length != SaltSize + HashSize) return false;

                byte[] salt = new byte[SaltSize];
                byte[] storedHashBytes = new byte[HashSize];

                Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(combined, SaltSize, storedHashBytes, 0, HashSize);

                byte[] computedHash = ComputeHash(password, salt);
                return SlowEquals(storedHashBytes, computedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random token.
        /// </summary>
        public static string GenerateSecureToken(int length = 32)
        {
            byte[] tokenBytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }

        /// <summary>
        /// Encrypts a string using AES encryption.
        /// </summary>
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            byte[] keyBytes = DeriveKey(key);
            byte[] iv = new byte[16];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv);
            }

            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    byte[] result = new byte[iv.Length + cipherBytes.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }

        /// <summary>
        /// Decrypts a string using AES encryption.
        /// </summary>
        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                byte[] keyBytes = DeriveKey(key);
                byte[] combined = Convert.FromBase64String(cipherText);

                byte[] iv = new byte[16];
                byte[] cipherBytes = new byte[combined.Length - 16];

                Buffer.BlockCopy(combined, 0, iv, 0, 16);
                Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Validates password strength.
        /// </summary>
        public static (bool IsValid, string Message) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Şifre boş olamaz.");

            if (password.Length < 8)
                return (false, "Şifre en az 8 karakter olmalıdır.");

            bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }

            if (!hasUpper) return (false, "Şifre en az bir büyük harf içermelidir.");
            if (!hasLower) return (false, "Şifre en az bir küçük harf içermelidir.");
            if (!hasDigit) return (false, "Şifre en az bir rakam içermelidir.");
            if (!hasSpecial) return (false, "Şifre en az bir özel karakter içermelidir.");

            return (true, "Şifre güçlü.");
        }

        private static byte[] ComputeHash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);
            return diff == 0;
        }

        private static byte[] DeriveKey(string key)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }
    }
}
