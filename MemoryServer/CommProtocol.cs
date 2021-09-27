using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryServer
{
    class aesParams
    {
        public byte[] key;
        public byte[] iv;
        public aesParams(byte[]key,byte[]iv)
        {
            this.key = key;
            this.iv = iv;
        }
    }


    class CommProtocol
    {        
        public static Dictionary<NetworkStream, aesParams> clientKeys = new Dictionary<NetworkStream,aesParams>();
        private static RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

        public static void setAes(NetworkStream stream)
        {
            string keyPath = Directory.GetCurrentDirectory() + "\\keys\\priv.txt";
            byte[] privateKey = File.ReadAllBytes(keyPath);
            rsa.ImportCspBlob(privateKey);

            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                string msg = sr.ReadLine();
                
                byte[] key = rsa.Decrypt(Convert.FromBase64String(msg), false);

                msg = sr.ReadLine();
                Console.WriteLine("new client connected, new key accepted");
                byte[] iv = rsa.Decrypt(Convert.FromBase64String(msg), false);

                aesParams ap = new aesParams(key, iv);
                clientKeys.Add(stream, ap);
            }
        }

        static byte[] Encrypt(string plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted;
            // Create a new AesManaged.    
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.PKCS7;
                // Create encryptor    
                ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
                // Create MemoryStream                    
                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt    
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream    
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(plainText);
                        encrypted = ms.ToArray();
                    }
                }
            }
            // Return encrypted data    
            return encrypted;
        }

        static string Decrypt(byte[] cipherText, byte[] Key, byte[] IV)
        {
            string plaintext = null;
            // Create AesManaged    
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.PKCS7;
                // Create a decryptor    
                ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);
                // Create the streams used for decryption.    
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    // Create crypto stream    
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        // Read crypto stream    
                        using (StreamReader reader = new StreamReader(cs))
                            plaintext = reader.ReadToEnd();
                    }
                }
            }
            return plaintext;
        }

        public static string Read(NetworkStream stream)
        {
            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                string str = sr.ReadLine();

                byte[] bytes = Convert.FromBase64String(str);

                string command = Decrypt(bytes, clientKeys[stream].key, clientKeys[stream].iv);

                return command;
            }
        }

        public static void Write(NetworkStream stream, string msg)
        {

            using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                try
                { 
                    byte[] encrytped = Encrypt(msg, clientKeys[stream].key, clientKeys[stream].iv);

                    string command = Convert.ToBase64String(encrytped);
                    
                    sw.WriteLine(command);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        public static string[] CheckMessage(string sData)
        {
            return sData.Split(' ');
        }
    }
}
