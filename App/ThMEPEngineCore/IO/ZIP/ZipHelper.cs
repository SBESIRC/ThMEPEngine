using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.IO.ZIP
{
    public class ZipHelper
    {
        public static void Zip(string[] files, ZipOutputStream outStream, string pwd)
        {
            for (int i = 0; i < files.Length; i++)
            {
                if (!File.Exists(files[i]))
                {
                    throw new Exception("文件不存在");
                }
                using (FileStream fs = File.OpenRead(files[i]))
                {
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    if (!string.IsNullOrWhiteSpace(pwd))
                    {
                        outStream.Password = pwd;
                    }
                    ZipEntry ZipEntry = new ZipEntry(Path.GetFileName(files[i]));
                    outStream.PutNextEntry(ZipEntry);
                    outStream.Write(buffer, 0, buffer.Length);
                }
                File.Delete(files[i]);
            }
        }

        public static Dictionary<string, MemoryStream> UnZip(string zipFile, string pwd)
        {
            Dictionary<string, MemoryStream> result = new Dictionary<string, MemoryStream>();
            try
            {
                using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(zipFile)))
                {
                    if (!string.IsNullOrWhiteSpace(pwd))
                    {
                        zipInputStream.Password = pwd;
                    }
                    ZipEntry theEntry;
                    while ((theEntry = zipInputStream.GetNextEntry()) != null)
                    {
                        byte[] data = new byte[1024 * 1024];
                        int dataLength = 0;
                        MemoryStream stream = new MemoryStream();
                        while ((dataLength = zipInputStream.Read(data, 0, data.Length)) > 0)
                        {
                            stream.Write(data, 0, dataLength);
                        }
                        result.Add(theEntry.Name, stream);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
    }
}
