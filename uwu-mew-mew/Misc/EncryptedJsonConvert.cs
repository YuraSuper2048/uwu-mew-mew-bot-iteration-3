using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Newtonsoft.Json;

namespace uwu_mew_mew.Misc;

public static class EncryptedJsonConvert
{
    private const int IvLength = 16;
    private static readonly byte[] Key;

    static EncryptedJsonConvert()
    {
        var key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY")!;
        var argon = new Argon2d(Encoding.UTF8.GetBytes(key));
        argon.Salt = "uwu nyaa"u8.ToArray();
        argon.DegreeOfParallelism = 16;
        argon.MemorySize = 16000;
        argon.Iterations = 2;

        Key = argon.GetBytes(32);
        
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
    }

    public static void ComputeKey()
    {
        Key.Clone();
    }

    public static byte[] Serialize(object obj)
    {
        var json = JsonConvert.SerializeObject(obj);

        var jsonBytes = Encoding.UTF8.GetBytes(json);
        byte[] encryptedBytes;

        using (var aes = Aes.Create())
        {
            if (aes == null)
                throw new ApplicationException("Failed to create AES cipher.");

            aes.Key = Key;

            using (var buffer = new MemoryStream())
            {
                buffer.Write(aes.IV, 0, IvLength);

                using (var cryptoStream = new CryptoStream(buffer, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new BinaryWriter(cryptoStream))
                {
                    writer.Write(jsonBytes);
                }

                encryptedBytes = buffer.ToArray();
            }
        }

        return encryptedBytes;
    }

    public static T? Deserialize<T>(byte[] bytes)
    {
        byte[] decryptedBytes;
        var iv = new byte[IvLength];
        Array.Copy(bytes, iv, IvLength);

        using (var aes = Aes.Create())
        {
            if (aes == null)
                throw new ApplicationException("Failed to create AES cipher.");

            aes.Key = Key;
            aes.IV = iv;

            using (var buffer = new MemoryStream())
            using (var cryptoStream = new CryptoStream(new MemoryStream(bytes, IvLength, bytes.Length - IvLength),
                       aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                int read;
                var chunk = new byte[1024];

                while ((read = cryptoStream.Read(chunk, 0, chunk.Length)) > 0) buffer.Write(chunk, 0, read);

                decryptedBytes = buffer.ToArray();
            }
        }

        var json = Encoding.UTF8.GetString(decryptedBytes);

        return JsonConvert.DeserializeObject<T>(json);
    }
}