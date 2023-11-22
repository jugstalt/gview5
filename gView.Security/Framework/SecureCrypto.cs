using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace gView.Framework.Security
{
    public class SecureCrypto
    {
        #region RSA

        public static byte[] EncryptDataOaepSha(X509Certificate2 cert, byte[] data, RSAEncryptionPadding strengh)
        {
            using (RSA rsa = cert.GetRSAPublicKey())
            {
                return rsa.Encrypt(data, strengh);
            }
        }

        public static byte[] DecryptDataOaepSha(X509Certificate2 cert, byte[] data, RSAEncryptionPadding strengh)
        {
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                return rsa.Decrypt(data, strengh);
            }
        }

        public static string EncryptText(X509Certificate2 cert, string text, ResultType resultType = ResultType.Base64)
        {
            var data = DecryptDataOaepSha(cert, Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.OaepSHA256);

            switch (resultType)
            {
                case ResultType.Hex:
                    return "0x" + string.Concat(data.Select(b => b.ToString("X2")));
                default: // base64
                    return Convert.ToBase64String(data);
            }
        }

        public static string DecryptText(X509Certificate2 cert, string cipherText)
        {
            var data = DecryptDataOaepSha(cert, StringToByteArray(cipherText), RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(data);
        }

        public static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._";
            var token = new char[length];
            var random = new RNGCryptoServiceProvider();

            for (int i = 0; i < token.Length; i++)
            {
                byte[] randomByte = new byte[1];
                random.GetBytes(randomByte);
                token[i] = chars[randomByte[0] % chars.Length];
            }

            return new String(token);
        }

        #endregion

        #region AES

        #region AES Base

        private const int _saltSize = 4; //, _iterations = 1000;

        static private byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes, int keySize = 128, bool useRandomSalt = true, byte[] salt = null, byte[] g1 = null)
        {
            byte[] encryptedBytes = null;

            if (useRandomSalt)
            {
                // Add Random Salt in front -> two ident objects will produce differnt results
                // Remove the Bytes after decryption
                byte[] randomSalt = GetRandomBytes();
                byte[] bytesToEncrpytWidhSalt = new byte[randomSalt.Length + bytesToBeEncrypted.Length];
                Buffer.BlockCopy(randomSalt, 0, bytesToEncrpytWidhSalt, 0, randomSalt.Length);
                Buffer.BlockCopy(bytesToBeEncrypted, 0, bytesToEncrpytWidhSalt, randomSalt.Length, bytesToBeEncrypted.Length);

                bytesToBeEncrypted = bytesToEncrpytWidhSalt;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = keySize;
                    AES.BlockSize = 128;

                    // Faster (store 4 bytes to generating IV...)
                    byte[] ivInitialBytes = GetRandomBytes();
                    ms.Write(ivInitialBytes, 0, _saltSize);

                    AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                    AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8, salt: salt, g1: g1);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        static private byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes, int keySize = 128, bool useRandomSalt = true, byte[] salt = null, byte[] g1 = null)
        {
            byte[] decryptedBytes = null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = keySize;
                    AES.BlockSize = 128;

                    // Faster get bytes for IV from 
                    var ivInitialBytes = new byte[_saltSize];
                    Buffer.BlockCopy(bytesToBeDecrypted, 0, ivInitialBytes, 0, _saltSize);

                    AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                    AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8, salt: salt, g1: g1);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, _saltSize, bytesToBeDecrypted.Length - _saltSize);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            if (useRandomSalt)
            {
                byte[] ret = new byte[decryptedBytes.Length - _saltSize];
                Buffer.BlockCopy(decryptedBytes, _saltSize, ret, 0, ret.Length);
                decryptedBytes = ret;
            }

            return decryptedBytes;
        }

        static private byte[] GetRandomBytes()
        {
            byte[] ba = new byte[_saltSize];
            RNGCryptoServiceProvider.Create().GetBytes(ba);
            return ba;
        }

        static private byte[] GetBytes(byte[] initialBytes, int size)
        {
            var ret = new byte[size];
            Buffer.BlockCopy(initialBytes, 0, ret, 0, Math.Min(initialBytes.Length, ret.Length));

            return ret;
        }

        private static byte[] _g1 = new Guid("956F94BF45B44609B243A0B744DFFBE3").ToByteArray();
        static private byte[] GetHashedBytes(byte[] initialBytes, int size, byte[] salt, byte[] g1)
        {
            var hash = SHA256.Create().ComputeHash(initialBytes);

            var ret = new byte[size];
            Buffer.BlockCopy(hash, 0, ret, 0, Math.Min(hash.Length, ret.Length));

            byte[] saltBytes = salt ?? new byte[] { 167, 123, 23, 12, 64, 198, 177, 114 };
            var key = new Rfc2898DeriveBytes(hash, g1 ?? _g1, 10); // 10 is enough for this...
            ret = key.GetBytes(size);

            return ret;
        }

        public static string EncryptToken(X509CertificateWrapper cert, string text, ResultType resultType = ResultType.Base64)
        {
            var data = AES_Encrypt(Encoding.UTF8.GetBytes(text), cert.AESPassword, useRandomSalt: true, salt: cert.AESSalt, g1: cert.AESG1);

            switch (resultType)
            {
                case ResultType.Hex:
                    return "0x" + string.Concat(data.Select(b => b.ToString("X2")));
                case ResultType.Base62:
                    return "_" + data.ToBase62();
                default: // base64
                    return Convert.ToBase64String(data);
            }
        }


        public static string DecryptToken(X509CertificateWrapper cert, string cipherText)
        {
            byte[] cipherBytes = null;
            if (IsHexString(cipherText))
            {
                cipherBytes = StringToByteArray(cipherText);
            }
            else
            {
                cipherBytes = Convert.FromBase64String(cipherText);
            }

            var data = AES_Decrypt(cipherBytes, cert.AESPassword, useRandomSalt: true, salt: cert.AESSalt, g1: cert.AESG1);
            return Encoding.UTF8.GetString(data);
        }

        #endregion

        #endregion

        #region Hash

        public static string Hash64(string password, string username = "")
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password + username?.Trim().ToLower());
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        public static bool VerifyPassword(string cleanPassword, string hash, string username = "")
        {
            if (Hash64(cleanPassword + username?.Trim().ToLower()) == hash)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region General

        static private byte[] StringToByteArray(String input)
        {
            if (input.StartsWith("0x"))  // Base 16 (HEX)
            {
                input = input.Substring(2, input.Length - 2);

                int NumberChars = input.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(input.Substring(i, 2), 16);
                }

                return bytes;
            }
            else if (input.StartsWith("_"))  // Base 62
            {
                input = input.Substring(1);

                return input.FromBase62();
            }

            return Convert.FromBase64String(input);
        }

        static private bool IsHexString(string hex)
        {
            if (hex.StartsWith("0x"))
            {
                hex = hex.Substring(2);

                bool isHex;
                foreach (var c in hex)
                {
                    isHex = ((c >= '0' && c <= '9') ||
                             (c >= 'a' && c <= 'f') ||
                             (c >= 'A' && c <= 'F'));

                    if (!isHex)
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (hex.StartsWith("_"))
            {
                hex = hex.Substring(1);

                bool isHex;
                foreach (var c in hex)
                {
                    isHex = ((c >= '0' && c <= '9') ||
                             (c >= 'a' && c <= 'z') ||
                             (c >= 'A' && c <= 'Z'));

                    if (!isHex)
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        #endregion

        #region Classes Enums

        public enum ResultType
        {
            Base64,
            Base62,
            Hex
        }

        #endregion
    }
}
