using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace Project_OLDR.src
{
    public static class OpenSsl
    {
        private const string DllName = "libcrypto-4-x64.dll";


        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern IntPtr EVP_aes_256_cbc();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int EVP_EncryptInit_ex(IntPtr ctx, IntPtr cipher, IntPtr impl, byte[] key, byte[] iv);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int EVP_EncryptUpdate(IntPtr ctx, byte[] outBytes, ref int outLen, byte[] inBytes, int inLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int EVP_EncryptFinal_ex(IntPtr ctx, byte[] outBytes, ref int outLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int EVP_DecryptInit_ex(IntPtr ctx, IntPtr cipher, IntPtr impl, byte[] key, byte[] iv);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int EVP_DecryptUpdate(IntPtr ctx, byte[] outBytes, ref int outLen, byte[] inBytes, int inLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int EVP_DecryptFinal_ex(IntPtr ctx, byte[] outBytes, ref int outLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern IntPtr EVP_CIPHER_CTX_new();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern void EVP_CIPHER_CTX_free(IntPtr ctx);

        public static string EncryptString(string plainText, string secretKey)
        {
            byte[] key = PrepareKey(secretKey);
            byte[] iv = new byte[16]; 
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length + 16];

            IntPtr ctx = EVP_CIPHER_CTX_new();
            try
            {
                EVP_EncryptInit_ex(ctx, EVP_aes_256_cbc(), IntPtr.Zero, key, iv);

                int len = 0;
                int cipherLen = 0;
                EVP_EncryptUpdate(ctx, cipherBytes, ref len, plainBytes, plainBytes.Length);
                cipherLen = len;

                EVP_EncryptFinal_ex(ctx, cipherBytes, ref len);
                cipherLen += len;

                Array.Resize(ref cipherBytes, cipherLen);
                return Convert.ToBase64String(cipherBytes);
            }
            finally
            {
                EVP_CIPHER_CTX_free(ctx);
            }
        }

        public static string DecryptString(string cipherBase64, string secretKey)
        {
            byte[] key = PrepareKey(secretKey);
            byte[] iv = new byte[16];
            byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
            byte[] plainBytes = new byte[cipherBytes.Length];

            IntPtr ctx = EVP_CIPHER_CTX_new();
            try
            {
                EVP_DecryptInit_ex(ctx, EVP_aes_256_cbc(), IntPtr.Zero, key, iv);

                int len = 0;
                int plainLen = 0;
                EVP_DecryptUpdate(ctx, plainBytes, ref len, cipherBytes, cipherBytes.Length);
                plainLen = len;

                EVP_DecryptFinal_ex(ctx, plainBytes, ref len);
                plainLen += len;

                return Encoding.UTF8.GetString(plainBytes, 0, plainLen);
            }
            finally
            {
                EVP_CIPHER_CTX_free(ctx);
            }
        }

        public static XDocument LoadAndDecryptEmbeddedConfig(string resourceName, string secretKey)
        {
            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"The embedded resource '{resourceName}' is not found.");

                using (StreamReader reader = new StreamReader(stream))
                {
                    string encryptedContent = reader.ReadToEnd().Trim();
                    string decryptedXml = DecryptString(encryptedContent, secretKey);
                    return XDocument.Parse(decryptedXml);
                }
            }
        }

        private static byte[] PrepareKey(string password)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(password);
            byte[] fixedKey = new byte[32];
            Array.Copy(keyBytes, fixedKey, Math.Min(keyBytes.Length, 32));
            return fixedKey;
        }
    }
}
