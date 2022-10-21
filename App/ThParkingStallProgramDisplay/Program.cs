using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
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
            rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
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
            readLocal(contents, "发送至服务器计算", Logger);
            //readfromserver(contents, "服务器计算结束", Logger);
            //readLocal(contents, "地库程序运行结束,总用时", Logger);
            Console.ReadKey();
        }
        static void readfromserver(List<string> contents, string end, Logger Logger)
        {
            var guid = readGuidFromMemory();
            var filename = $"DisplayLog_{guid}.txt";
            Logger.Information(guid);
            var quit = false;
            while (true)
            {
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
                                    quit = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (quit)
                    break;
                Thread.Sleep(1 * 1000);
            }
        }
        static string readGuidFromMemory()
        {
            MemoryMappedFile memory = MemoryMappedFile.CreateOrOpen("AI-guid", 36);  // 创建指定大小的内存文件，会在应用程序退出时自动释放
            MemoryMappedViewAccessor accessor1 = memory.CreateViewAccessor();           // 访问内存文件对象
            var bytes = new byte[36];
            accessor1.ReadArray<byte>(0, bytes, 0, bytes.Length);
            accessor1.Dispose();
            string str = Encoding.UTF8.GetString(bytes);
            return str;
        }

        static void readLocal(List<string> contents,string end, Logger Logger)
        {
            string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog.txt");
            var fs = new FileStream(LogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var quit = false;
            while (true)
            {
                try
                {
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
