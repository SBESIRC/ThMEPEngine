using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ThParkingStallProgramDisplay
{
    public class Program
    {
        [STAThread]
        
        static void Main()
        {
            string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog_process.txt");

            var Logger = new Serilog.LoggerConfiguration().WriteTo.File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5),
            rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: null).CreateLogger();
            try
            {
                Logger?.Information("创建显示UI进程成功");
                Run(Logger);
            }
            catch (Exception ex)
            {
                Logger?.Information(ex.Message);
                Logger?.Information("----------------------------------");
                Logger?.Information(ex.StackTrace);
                Logger?.Information("##################################");
            }
        }
        static void Run(Logger Logger)
        {
            var contents = new List<string>();
            var endprocess = false;
            while (true)
            {
                readLocal(contents, "发送至服务器计算", Logger,ref endprocess);
                readfromserver(contents, "服务器计算结束", Logger);
                endprocess = false;
                readLocal(contents, "地库程序运行结束,总用时", Logger,ref endprocess, "单地库用时");
                if (endprocess)
                    break;
            }
            Console.ReadKey();
        }
        static void readfromserver(List<string> contents, string end, Logger Logger)
        {
            Thread.Sleep(3 * 1000);
            var contentCount = contents.Count;
            var guid = readGuidFromMemory(Logger);
            if (guid == "")
            {
                Console.WriteLine("未读取到fileID，接受服务器数据失败");
                return;
            }
            Logger.Information($"guid:{guid}");
            var filename = $"DisplayLog_{guid}.txt";
            Logger.Information(guid);
            var quit = false;
            var cycleCount = 0;
            while (true)
            {
                cycleCount++;
                if (cycleCount > 10 && contents.Count == contentCount)
                {
                    Console.WriteLine("未读取到服务器数据。");
                    return;
                }
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential("upload", "Thape123123");
                        client.DownloadFile($"http://172.16.1.84:8089/log/{filename}", filename);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("进程显示出错: "+ex.Message);
                    Console.WriteLine("进程显示出错: " + ex.StackTrace);
                    Logger.Information(ex.Message);
                    Logger.Information(ex.StackTrace);
                }
                if (File.Exists(filename))
                {
                    var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using (var sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            if (!contents.Contains(line))
                            {
                                contents.Add(line);
                                Console.WriteLine(line);
                                if (line.Contains(end))
                                {
                                    Logger.Information("end:______"+end);
                                    quit = true;
                                    break;
                                }
                            }
                        }
                    }
                    fs.Close();
                }
                if (quit)
                    break;
                Thread.Sleep(2 * 1000);
            }
        }
        static string readGuidFromMemory(Logger Logger)
        {
            //MemoryMappedFile memory = MemoryMappedFile.CreateOrOpen("AI-guid", 36);  // 创建指定大小的内存文件，会在应用程序退出时自动释放
            //MemoryMappedViewAccessor accessor1 = memory.CreateViewAccessor();           // 访问内存文件对象
            //var bytes = new byte[36];
            //accessor1.ReadArray<byte>(0, bytes, 0, bytes.Length);
            //accessor1.Dispose();
            //string str = Encoding.UTF8.GetString(bytes);
            //return str;

            string content = "";
            try
            {
                string filepath = Path.Combine(System.IO.Path.GetTempPath(), "AICal_File_id.txt");
                System.IO.StreamReader file = new System.IO.StreamReader(filepath);
                while ((content = file.ReadLine()) != null)
                {
                    break;
                }
                file.Close();
            }
            catch(Exception ex)
            {
                Logger.Information(ex.ToString());
                Logger.Information(ex.StackTrace);
            }
            return content;
        }

        static void readLocal(List<string> contents, string end, Logger Logger,ref bool endprocess, string continueCycleStr = "")
        {
            string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog.txt");
            var fs = new FileStream(LogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var quit = false;
            while (true)
            {
                try
                {
                    fs = new FileStream(LogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using (var sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            if (!contents.Contains(line))
                            {
                                contents.Add(line);
                                Console.WriteLine(line);
                                if (line.Contains(end))
                                {
                                    quit = true;
                                    endprocess=true;
                                    break;
                                }
                                else if (continueCycleStr != "" && line.Contains(continueCycleStr))
                                {
                                    quit = true;
                                    break;
                                }
                            }
                        }
                        if (quit)
                            break;
                    }
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Logger.Information(ex.Message);
                    Logger.Information(ex.StackTrace);
                }
            }
            fs.Close();
        }

        static void _Run(Logger Logger)
        {
            string _LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog.txt");
            string LogFileName2 = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog2.txt");
            Logger?.Information(_LogFileName);
            var logs = new List<string>();
            bool process = true;
            bool hasBug = false;
            var LogFileName = _LogFileName;


            while (process)
            {
                var fs = new FileStream(LogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    using (var sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            Logger?.Information(line);
                            var val = line.Split('[', ']').Last();

                            //If met end of computation, break;
                            if (line.Contains("程序出错"))
                            {
                                process = false;
                                hasBug = true;
                                break;
                            }
                            if (!logs.Contains(line))
                            {
                                logs.Add(line);
                                System.Console.WriteLine(val);
                            }
                            if (line.Contains("块名"))
                            {
                                process = false;
                                break;
                            }
                            Thread.Sleep(100);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Logger?.Information(ex.Message);
                }
                finally
                {
                    fs.Close();
                }
            }
            process = true;
            LogFileName = "DisplayLog.txt";
            while (process)
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        client.Credentials = new NetworkCredential("upload", "Thape123123");
                        client.DownloadFile("http://172.16.1.3/Loggers/DisplayLog.txt", "DisplayLog.txt");
                    }
                    catch (Exception ex) { }
                }
                var fs = new FileStream(LogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    using (var sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            Logger?.Information(line);
                            var val = line.Split('[', ']').Last();

                            //If met end of computation, break;
                            if (line.Contains("程序出错"))
                            {
                                process = false;
                                hasBug = true;
                                break;
                            }
                            if (!logs.Contains(line))
                            {
                                logs.Add(line);
                                System.Console.WriteLine(val);
                            }
                            if (line.Contains("收敛情况"))
                            {
                                process = false;
                                break;
                            }
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Information(ex.Message);
                }
                finally
                {
                    fs.Close();
                }
            }

            process = true;
            LogFileName = _LogFileName;
            while (process)
            {
                var fs = new FileStream(LogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    using (var sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            Logger?.Information(line);
                            var val = line.Split('[', ']').Last();

                            //If met end of computation, break;
                            if (line.Contains("地库程序运行结束"))
                            {
                                process = false;
                                break;
                            }
                            if (line.Contains("程序出错"))
                            {
                                process = false;
                                hasBug = true;
                                break;
                            }
                            if (!logs.Contains(line))
                            {
                                logs.Add(line);
                                System.Console.WriteLine(val);
                            }
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Information(ex.Message);
                }
                finally
                {
                    fs.Close();
                }
            }

            process = !hasBug;
            while (process)
            {
                var fs2 = new FileStream(LogFileName2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    using (var sr = new StreamReader(fs2))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            Logger?.Information(line);
                            var val = line.Split('[', ']').Last();

                            //If met end of computation, break;
                            if (line.Contains("地库程序运行结束"))
                            {
                                process = false;
                                break;
                            }

                            logs.Add(line);
                            System.Console.WriteLine(val);
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Information(ex.Message);
                }
                finally
                {
                    fs2.Close();
                }
            }
            System.Console.ReadKey();
        }
    }
}
