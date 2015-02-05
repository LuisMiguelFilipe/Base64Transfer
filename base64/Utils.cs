using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;

namespace base64
{
    public class Utils
    {
        //static void Main(string[] args)
        //{
        //    //var tempFiles = @"C:\Users\sdf\Desktop\chunks\{0}.txt";
        //    //var tempFinalFile = @"C:\Users\sdf\Desktop\final.txt";

        //    ////JoinFiles(tempFiles, 35, tempFinalFile);

        //    //FromBase64(tempFinalFile, @"C:\Users\sdf\Desktop\snapshot.bak");
        //    ToBase64(@"C:\Users\sdf\Desktop\hsbc_form_scan.pdf", @"C:\Users\sdf\Desktop\hsbc_form_scan.pdf.txt");
        //}

        public static int ToBase64(string filepath, string tofilepath, int size = 25000000)
        {
            using (FileStream inputFilStream = File.OpenRead(filepath))
            using (FileStream outputFileStream = File.Create(tofilepath))
            using (CryptoStream cs = new CryptoStream(outputFileStream, new ToBase64Transform(), CryptoStreamMode.Write))
            using (DeflateStream compressionStream = new DeflateStream(cs, System.IO.Compression.CompressionMode.Compress))
            {
                inputFilStream.CopyTo(compressionStream);
            }

            FileInfo fi = new FileInfo(tofilepath);
            if (fi.Length > size)
            {
                int number = (int)fi.Length / size;
                if (fi.Length % size > 0)
                    number++;

                byte[] bytes;

                for (int i = 0; i < number; i++)
                {
                    if (i + 1 == number) // last
                    {
                        bytes = new byte[fi.Length % size];
                    }
                    else
                    {
                        bytes = new byte[size];
                    }

                    using (FileStream inputFilStream = File.OpenRead(tofilepath))
                    using (FileStream outputFileStream = File.Create(string.Format(tofilepath + ".64Part.part{0}", i)))
                    {
                        inputFilStream.Seek(i * size, SeekOrigin.Begin);
                        var e = inputFilStream.Read(bytes, 0, bytes.Length);
                        outputFileStream.Write(bytes, 0, e);
                    }

                    
                }
                File.Delete(tofilepath);

                return number;
            }
            return 1;
        }

        public static void FromBase64(string filepath, string tofilepath)
        {
            using (FileStream inputFilStream = File.OpenRead(filepath))
            using (FileStream outputFileStream = File.Create(tofilepath))
            using (CryptoStream cs = new CryptoStream(inputFilStream, new FromBase64Transform(), CryptoStreamMode.Read))
            using (DeflateStream compressionStream = new DeflateStream(cs, System.IO.Compression.CompressionMode.Decompress))
            {
                compressionStream.CopyTo(outputFileStream);
            }
        }

        public static void FromTextBase64(string text, string tofilepath)
        {
            using (Stream sr = GenerateStreamFromString(text))
            using (FileStream outputFileStream = File.Create(tofilepath))
            using (CryptoStream cs = new CryptoStream(sr, new FromBase64Transform(), CryptoStreamMode.Read))
            using (DeflateStream compressionStream = new DeflateStream(cs, System.IO.Compression.CompressionMode.Decompress))
            {
                compressionStream.CopyTo(outputFileStream);
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static void JoinFiles(string filepath, string tofilepath)
        {
            using (FileStream outputFileStream = File.Create(tofilepath))
            {
                for (int i = 0; ; i++)
                {
                    var filename = string.Format(filepath, i);

                    if (!File.Exists(filename))
                        break;

                    var bytes = File.ReadAllBytes(filename);
                    outputFileStream.Write(bytes,0,bytes.Length);
                }

                outputFileStream.Close();
            }
        }
    }
}
