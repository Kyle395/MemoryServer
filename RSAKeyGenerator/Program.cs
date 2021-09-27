using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RSAKeyGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Aes aes = Aes.Create();
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            byte[] pub = RSA.ExportCspBlob(false);
            byte[] priv = RSA.ExportCspBlob(true);
            File.WriteAllBytes("keys\\pub.txt", pub);
            File.WriteAllBytes("keys\\priv.txt", priv);

            string str = Encoding.UTF8.GetString(RSA.Decrypt(RSA.Encrypt(aes.Key, false), false));
            string str2 = Encoding.UTF8.GetString(aes.Key);
            Console.WriteLine(str2);
            Console.WriteLine(str);

            string str3 = Encoding.UTF8.GetString(RSA.ExportCspBlob(false));
            string str4 = Encoding.UTF8.GetString(File.ReadAllBytes("keys\\pub.txt"));
            Console.WriteLine(str3);
            Console.WriteLine(str4);
            if (str3 == str4)
                Console.WriteLine("good");
            Console.ReadLine();
        }
    }
}
