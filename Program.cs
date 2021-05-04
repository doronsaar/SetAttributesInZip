using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;

namespace SetAttributesInZip
{
    class Program
    {
        static void Main(string[] args)
        {
            string tempDir = null;

            try
            {

                if (args.Length == 0)
                {
                    Console.WriteLine("Missing parameter: zip file name");
                    Environment.Exit(-1);
                    return;
                }


                var zipFileName = args[0];
                if (!File.Exists(zipFileName))
                {
                    Console.WriteLine("Zip file not found: " + zipFileName);
                    Environment.Exit(-1);
                    return;
                }

                int attributes = -1578303488;
                if(args.Length>1) int.TryParse(args[1], out attributes);

                // extract
                tempDir = GetTempDir(zipFileName, attributes);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(zipFileName, tempDir, null);

                // delete zip
                File.Delete(zipFileName);

                // recompress
                var directoryInfo = new DirectoryInfo(tempDir);
                if (!directoryInfo.Exists) directoryInfo.Create();
                using (var fs = File.Create(zipFileName))
                using (var zipOutputStream = new ZipOutputStream(fs))
                {
                    CompressDirectory(zipOutputStream, directoryInfo, baseDirectoryPath: directoryInfo.FullName, attributes);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error: " + ex.Message + " " + ex.InnerException?.Message);
                Environment.Exit(-1);
            }
            finally
            {
                // delete temp
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception) { }
            }

        }

        private static void ExtractAttributesFromZip()
        {
            using (var fs = new FileStream(@"c:\1\main.zip", FileMode.Open, FileAccess.Read))
            {
                using (var zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fs))
                {
                    foreach (ZipEntry ze in zf)
                    {
                        Console.WriteLine($"{ze.Name}: {ze.ExternalFileAttributes}");
                    }
                }
            }
        }

        private static string GetTempDir(string zipFileName, long attributes)
        {
            return zipFileName.Substring(0, zipFileName.EndsWith(".zip") ? zipFileName.Length - 4 : zipFileName.Length)
                + "_" + Guid.NewGuid().ToString();
        }

        private static void CompressDirectory(ZipOutputStream zipOutputStream, DirectoryInfo directoryInfo, string baseDirectoryPath, int attributes)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                var dataBytes = File.ReadAllBytes(fileInfo.FullName);
                var zipEntry = new ZipEntry(fileInfo.FullName.Substring(baseDirectoryPath.Length));
                zipEntry.HostSystem = 1;
                zipEntry.ExternalFileAttributes = attributes;
                zipOutputStream.PutNextEntry(zipEntry);
                zipOutputStream.Write(dataBytes);
            }
            foreach (var subDirectoryInfo in directoryInfo.GetDirectories())
            {
                CompressDirectory(zipOutputStream, subDirectoryInfo, baseDirectoryPath, attributes);
            }
        }
    }
}
