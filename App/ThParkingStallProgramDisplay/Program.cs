using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ThParkingStallProgramDisplay
{
    public class Program
    {
        [STAThread]
        
        static void Main()
        {
            string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog_2.txt");

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
            string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog.txt");
            Logger?.Information(LogFileName);
            var logs = new List<string>();
            bool process = true;
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
                            if (!logs.Contains(line))
                            {
                                logs.Add(line);
                                System.Console.WriteLine(val);
                            }
                            Thread.Sleep(2000);
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
            System.Console.ReadKey();
        }
    }
}
