using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThParkingStall.ClientUpdate
{
    public class Program
    {
        static void Main(string[] args)
        {
            var dir = args[0];
            //关闭当前cad应用程序
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (p.ProcessName.Equals("acad"))
                {
                    p.Kill();
                }
            }
            Thread.Sleep(1000);

            //删除目标文件所有内容
            var fileDir = dir;
            DirectoryInfo _dir = new DirectoryInfo(fileDir);
            //var files=_dir.GetFiles();
            //foreach (var f in files)
            //{
            //    try
            //    {
            //        File.Delete(f.FullName);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}
            //Console.WriteLine("已移除旧版本");
            //DelectDir(fileDir);
            //从服务器拿到更新文件
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("upload", "Thape123123");
                client.DownloadFile($"http://172.16.1.84:8089/ServerBuild/build.zip", $"build.zip");
            }
            Console.WriteLine("远端资源下载成功");
            //解压缩到temp文件夹
            var tmpDir = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var tmpFileDir= tmpDir + "\\AiCalBuild";
            if (!Directory.Exists(tmpFileDir))
            {
                Directory.CreateDirectory(tmpFileDir);
            }
            DelectDir(tmpFileDir);
            ZipFile.ExtractToDirectory($"build.zip", tmpFileDir);
            Console.WriteLine("远端资源包解压缩成功");
            //移动到目标文件夹
            var tmpFiles = Directory.GetFiles(tmpFileDir);
            foreach (var f in tmpFiles)
            {
                FileInfo info = new FileInfo(f);
                if (File.Exists(Path.Combine(fileDir, info.Name)))
                {
                    try
                    {
                        File.Delete(Path.Combine(fileDir, info.Name));
                        File.Move(info.FullName, Path.Combine(fileDir, info.Name));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    File.Move(info.FullName, Path.Combine(fileDir, info.Name));
                }
            }

            MessageBox.Show("更新成功，请重启CAD使用");
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
