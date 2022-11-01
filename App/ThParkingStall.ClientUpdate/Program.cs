using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.ClientUpdate
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //关闭当前cad应用程序
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (p.ProcessName.Equals("acad"))
                {
                    p.Kill();
                }
            } 
            //删除目标文件所有内容
            var curDir=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var fileDir = curDir+"\\di";
            DelectDir(fileDir);
            //从服务器拿到更新文件
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("upload", "Thape123123");
                client.DownloadFile($"http://172.16.1.84:8089/ServerBuild/build.zip", $"build.zip");
            }
            //解压缩到目标文件夹
            ZipFile.ExtractToDirectory($"build.zip", fileDir);
        }
        public static void DelectDir(string srcPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
