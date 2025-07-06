using System;
using System.Security.Cryptography;
using System.Text;

namespace SecureFileManagementSystem.Services
{
    public static class RSAService
    {
        // Generate RSA key pair (returns as XML strings)
        public static void GenerateKeyPair(out string publicKeyXml, out string privateKeyXml)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                publicKeyXml = rsa.ToXmlString(false); // public only
                privateKeyXml = rsa.ToXmlString(true); // public + private
            }
        }

        // Encrypt AES key with receiver’s RSA public key
        public static byte[] EncryptAESKeyWithRSA(byte[] aesKey, string receiverPublicKeyXml)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(receiverPublicKeyXml);
                return rsa.Encrypt(aesKey, RSAEncryptionPadding.Pkcs1);
            }
        }

        // Decrypt AES key using your own private key
        public static byte[] DecryptAESKeyWithRSA(byte[] encryptedKey, string privateKeyXml)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKeyXml);
                return rsa.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);
            }
        }
    }
}
