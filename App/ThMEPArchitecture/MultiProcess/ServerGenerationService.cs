using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore.Command;
using ThParkingStall.Core.IO;
using Autodesk.AutoCAD.ApplicationServices;
using AcHelper;
using ThMEPArchitecture.ParkingStallArrangement.General;
using Linq2Acad;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThParkingStall.Core.OInterProcess;
using Serilog;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using Autodesk.AutoCAD.Geometry;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.PostProcess;
using ThMEPEngineCore;
using ThParkingStall.Core.InterProcess;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Utils = ThMEPArchitecture.ParkingStallArrangement.General.Utils;
using ThParkingStall.Core.OTools;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess;
using static ThMEPArchitecture.PartitionLayout.DisplayTools;
using ThParkingStall.Core.Tools;
using ThParkingStall.Core.ObliqueMPartitionLayout;
using ThMEPArchitecture.MultiProcess;
using ThParkingStall.Core;
using static ThParkingStall.Core.MPartitionLayout.MCompute;
using System.Reflection;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace ThMEPArchitecture.MultiProcess
{
    public class ServerGenerationService
    {
        public ServerGenerationService()
        {

        }
        public Genome GetGenome(DataWraper dataWraper)
        {
            var solution = new Genome();
            var guid = (Guid.NewGuid()).ToString();
            //序列化dataWraper
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local_path = dir + $"\\dataWraper_{guid}.dat";
            FileStream fileStream = new FileStream(local_path, FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, dataWraper); //序列化 参数：流 对象
            fileStream.Close();
            //发送至服务器
            string url = $"http://172.16.1.84:8089/dataWraper/dataWraper_{guid}.dat";
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("upload", "Thape123123");
                //client.UploadProgressChanged += Client_UploadProgressChanged;
                //client.UploadFileCompleted += Client_UploadFileCompleted;
                byte[] data = client.UploadFile(new Uri(url), "PUT", local_path);
                //byte[] data = client.UploadFile(new Uri(url), path);
                string reply = Encoding.UTF8.GetString(data);
            }
            //启动服务器应用程序
            var appHttp = $"http://172.16.1.84:8088/Cal/GetGenome?filename=dataWraper_{guid}.dat&guid={guid}";
            SetCertificatePolicy();
            List<Byte> pageData = new List<byte>();
            string pageHtml = "";
            WebClientEx MyWebClient = new WebClientEx();
            MyWebClient.Credentials = new NetworkCredential("upload", "Thape123123");
            MyWebClient.Timeout = 10 * 60 * 1000;
            Task.Factory.StartNew(() =>
            {
                pageData = MyWebClient.DownloadData(appHttp).ToList();
            }).Wait(-1);
            pageHtml = Encoding.UTF8.GetString(pageData.ToArray());
            if (!pageHtml.Contains("success"))
                return solution;
            //返回数据
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("upload", "Thape123123");
                client.DownloadFile($"http://172.16.1.84:8089/genome/genome_{guid}.dat", $"genome_{guid}.dat");
            }
            //反序列化
            fileStream = new FileStream($"genome_{guid}.dat", FileMode.Open);
            var formatter = new BinaryFormatter
            {
                Binder = new UBinder()
            };
            var readSolution = (Genome)formatter.Deserialize(fileStream);
            fileStream.Close();

            solution = readSolution;
            return solution;
        }
        public Genome GetGenomeOld(DataWraper dataWraper)
        {
            var solution = new Genome();
            var guid = (Guid.NewGuid()).ToString();
            //序列化dataWraper
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local_path = dir + $"\\dataWraper_{guid}.dat";
            FileStream fileStream = new FileStream(local_path, FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, dataWraper); //序列化 参数：流 对象
            fileStream.Close();
            //发送至服务器
            string url = "http://172.16.1.3:80/ParkingTransferedDatas/dataWraper.dat";
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("upload", "Thape123123");
                //client.UploadProgressChanged += Client_UploadProgressChanged;
                //client.UploadFileCompleted += Client_UploadFileCompleted;
                byte[] data = client.UploadFile(new Uri(url), "PUT", local_path);
                //byte[] data = client.UploadFile(new Uri(url), path);
                string reply = Encoding.UTF8.GetString(data);
            }
            //启动服务器应用程序
            var appHttp = "http://172.16.1.3:8001/Run/winservermonitor";
            SetCertificatePolicy();
            List<Byte> pageData = new List<byte>();
            int max_runTime = 10 * 60;
            int step_viewTime = 30;
            int already_runTime = 0;
            string pageHtml = "";
            WebClientEx MyWebClient = new WebClientEx();
            MyWebClient.Credentials = new NetworkCredential("upload", "Thape123123");
            MyWebClient.Timeout = 10 * 60 * 1000;
            Task.Factory.StartNew(() =>
            {
                pageData = MyWebClient.DownloadData(appHttp).ToList();
            }).Wait(-1);
            pageHtml = Encoding.UTF8.GetString(pageData.ToArray());
            if (!pageHtml.Contains("success"))
                return solution;
            //返回数据
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("upload", "Thape123123");
                client.DownloadFile("http://172.16.1.3:80/ParkingTransferedDatas/genome.dat", "genome.dat");
            }
            //反序列化
            fileStream = new FileStream("genome.dat", FileMode.Open);
            var formatter = new BinaryFormatter
            {
                Binder = new UBinder()
            };
            var readSolution = (Genome)formatter.Deserialize(fileStream);
            fileStream.Close();

            solution = readSolution;
            return solution;
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
    public class UBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;
            typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
                typeName, assemblyName));

            return typeToDeserialize;
        }
    }
    public class WebClientEx : WebClient
    {
        public int Timeout { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = Timeout;
            return request;
        }
    }
}
