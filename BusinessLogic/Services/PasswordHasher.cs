using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BiblioGest.BusinessLogic.Interfaces;

namespace BiblioGest.BusinessLogic.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int HashSize = 32; // 256 bit
        private const int Iterations = 100_000;



        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            var result = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, result, SaltSize, HashSize);

            return Convert.ToBase64String(result);
        }
        public bool Verify(string password, string passwordHash)
        {
            var bytes = Convert.FromBase64String(passwordHash);

            var salt = bytes[..SaltSize];
            var storedHash = bytes[SaltSize..];

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
    }
}
