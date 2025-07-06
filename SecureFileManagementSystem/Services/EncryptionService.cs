using System;
using System.IO;
using System.Security.Cryptography;

namespace SecureFileManagementSystem.Services
{
    public static class EncryptionService
    {
        // Generates a random 256-bit AES key
        public static byte[] GenerateAesKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                return aes.Key;
            }
        }

        // Encrypt a file using AES
        public static void EncryptFile(string inputFile, string outputFile, byte[] key, out byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                iv = aes.IV;

                using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                using (CryptoStream cs = new CryptoStream(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                {
                    fsOut.Write(iv, 0, iv.Length); // Prepend IV
                    fsIn.CopyTo(cs);
                }
            }
        }

        public static byte[] EncryptBytes(byte[] plainBytes, byte[] key, out byte[] iv)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
            }

            return ms.ToArray();
        }


        // Decrypt a file using AES
        public static void DecryptFile(string inputFile, string outputFile, byte[] key)
        {
            using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
            {
                byte[] iv = new byte[16];
                fsIn.Read(iv, 0, iv.Length); // Read prepended IV

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (CryptoStream cs = new CryptoStream(fsIn, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                    {
                        cs.CopyTo(fsOut);
                    }
                }
            }
        }

        // Encrypt file from byte array and write to disk (with IV prepended)
        public static void EncryptFileFromBytes(byte[] fileData, string outputFile, byte[] key, out byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                iv = aes.IV;

                using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                {
                    fsOut.Write(iv, 0, iv.Length); // Write IV first

                    using (CryptoStream cs = new CryptoStream(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(fileData, 0, fileData.Length);
                    }
                }
            }
        }

    }
}
