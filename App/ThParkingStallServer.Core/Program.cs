using NetTopologySuite.Geometries;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.IO;
using ThParkingStall.Core.OInterProcess;

namespace ThParkingStallServer.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SetCertificatePolicy();
            //Read Datawraper
            var dir = @"C:\webiis\calParkingTest\ParkingTransferedDatas";
            var LogDir = @"C:\webiis\calParkingTest\Loggers";
            var path = dir + "\\dataWraper.dat";
            ReadDataWraperService readDataWraperService = new ReadDataWraperService(path);
            var dataWraper = new DataWraper();
            try
            {
                dataWraper = readDataWraperService.Read();
            }
            catch (Exception ex)
            {
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
                var LogFileName = Path.Combine(GetPath.GetAppDataPath(), "MPLog.txt");
                var DisplayLogFileName = Path.Combine(LogDir, "DisplayLog.txt");
                File.Delete(DisplayLogFileName);
                var Logger = new Serilog.LoggerConfiguration().WriteTo
                                    .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
                var DisplayLogger = new Serilog.LoggerConfiguration().WriteTo
                            .File(DisplayLogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: null).CreateLogger();
                //
                GA_Engine.Logger = Logger;
                GA_Engine.DisplayLogger = DisplayLogger;
                //GA_Engine.displayInfo = displayInfos.Last();
                Solution = GA_Engine.Run().First();
            }
            //Serialize Genome
            path = dir + "\\genome.dat";
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
}
