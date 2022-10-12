using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
