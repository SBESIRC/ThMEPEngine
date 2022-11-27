using Serilog;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.OInterProcess;

namespace ThParkingStallServer.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() != 3)
            {
                return;
            }
            var filename = args[0];
            var guid = args[1];
            var isNotInHost = args[2] == "0" ? true : false;
            SetCertificatePolicy();
            //Read Datawraper
            var dir = @"C:\AIIIS\DATAIIS";
            var LogDir = @"C:\AIIIS\DATAIIS\log";
            var path = dir + $"\\dataWraper\\{filename}";
            DisplayLogFilePut.guid = guid;
            ReadDataWraperService readDataWraperService = new ReadDataWraperService(path);
            var dataWraper = new DataWraper();
            try
            {
                dataWraper = readDataWraperService.Read();
            }
            catch (Exception ex)
            {
                string __dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                FileStream fs = new FileStream(__dir + $"\\BUG__{filename}.txt", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(ex.Message);
                sw.WriteLine(ex.StackTrace);
                sw.Close();
                fs.Close();
                return;
            }
            //run GA
            OInterParameter.Init(dataWraper);
            int fileSize = 64; // 64Mb
            var nbytes = fileSize * 1024 * 1024;
            Genome Solution = new Genome();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("DataWraper", nbytes))
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, dataWraper);
                }
                var GA_Engine = new ServerGAGenerator(dataWraper);
                //logger
                var LogFileName = Path.Combine(LogDir, $"MPLog_{guid}.txt");
                var DisplayLogFileName = Path.Combine(LogDir, $"DisplayLog_{guid}.txt");
                var Logger = new Serilog.LoggerConfiguration().WriteTo
                                    .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: null).CreateLogger();
                //var DisplayLogger = new Serilog.LoggerConfiguration().WriteTo
                //            .File(DisplayLogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: null).CreateLogger();
                //
                GA_Engine.isNotInHost = isNotInHost;
                GA_Engine.Logger = Logger;
                //GA_Engine.DisplayLogger = DisplayLogger;
                //GA_Engine.displayInfo = displayInfos.Last();
                DisplayLogFilePut.LogDisplayLog($"开始服务器计算;");
                Solution = GA_Engine.Run().First();
                DisplayLogFilePut.LogDisplayLog($"服务器计算结束;");
                if (isNotInHost)
                    DisplayLogFilePut.PutDisplayLogFileToHost();
            }
            //Serialize Genome
            path = dir + $"\\genome\\genome_{guid}.dat";
            FileStream fileStream = new FileStream(path, FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, Solution); //序列化 参数：流 对象
            fileStream.Close();

            Console.WriteLine("success.");
            //Console.ReadKey();
            return;
        }
        static void SetCertificatePolicy()
        {
            ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidate;
        }

        ///  <summary>
        ///  远程证书验证
        ///  </summary>
        static bool RemoteCertificateValidate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            return true;
        }
    }

    public static class DisplayLogFilePut
    {
        private static string _guid = null;
        public static string guid
        {
            get { return _guid; }
            set
            {
                _guid = value;
                DisplayLogFileName = Path.Combine(LogDir, $"DisplayLog_{guid}.txt");
            }
        }
        public static string LogDir = @"C:\AIIIS\DATAIIS\log";
        public static string DisplayLogFileName = Path.Combine(LogDir, $"DisplayLog_{guid}.txt");
        public static void LogDisplayLog(string info)
        {
            FileStream fs = new FileStream(DisplayLogFileName, FileMode.Append,FileAccess.Write,FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(System.DateTime.Now.ToString() + info);
            sw.Close();
            fs.Close();
        }
        public static void PutDisplayLogFileToHost()
        {
            string url = $"http://172.16.1.84:8089/log/DisplayLog_{guid}.txt";
            //发送至Host服务器
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("upload", "Thape123123");
                //client.UploadProgressChanged += Client_UploadProgressChanged;
                //client.UploadFileCompleted += Client_UploadFileCompleted;
                byte[] data = client.UploadFile(new Uri(url), "PUT", DisplayLogFileName);
                //byte[] data = client.UploadFile(new Uri(url), path);
                string reply = Encoding.UTF8.GetString(data);
            }
        }
    }
}
