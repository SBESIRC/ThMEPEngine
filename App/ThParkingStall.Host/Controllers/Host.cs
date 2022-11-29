using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


namespace ThParkingStall.Host.Controllers
{
    public static class GlobalParas
    {
        public static Queue<string> FILES = new Queue<string>();
        public static Queue<string> SERVERS = null;
        public const string tcp_app = "8088";
        public const string tcp_data = "8089";
        public static void InitSERVERS()
        {
            SERVERS = new Queue<string>();
            SERVERS.Enqueue("http://172.17.1.73");
            SERVERS.Enqueue("http://172.16.1.84");//host服务器
            //SERVERS.Enqueue("http://172.16.1.109");
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

    [ApiController]
    [Route("[controller]")]
    public class GetGenome : ControllerBase
    {
        object obj = new object();
        [HttpGet]
        public string Process(string filename = "", string guid = "")
        {
            if (GlobalParas.SERVERS == null)
                GlobalParas.InitSERVERS();
            //
            var server = "";
            var file = "";
            lock (obj)
            {
                GlobalParas.FILES.Enqueue(filename);
                int cycleCount = 0;
                while (true)
                {
                    cycleCount++;
                    if (GlobalParas.SERVERS.Count > 0)
                    {
                        server = GlobalParas.SERVERS.Dequeue();
                        if (askAnswer(server))
                        {
                            file = GlobalParas.FILES.Dequeue();
                            break;
                        }
                        else
                        {
                            GlobalParas.SERVERS.Enqueue(server);
                            if (cycleCount > 2)
                            {
                                return "服务器繁忙中，等待超时,请稍候再试。";
                            }
                        }
                    }
                    else
                    {
                        GlobalParas.FILES.Dequeue();
                        return "服务器繁忙中，等待超时,请稍候再试。";
                    }
                }
            }

            //把文件发送到对应服务器
            var data_dir = @"C:\AIIIS\DATAIIS";
            if (!IsHost(server))
            {
                var local_path = data_dir + $"\\dataWraper\\{file}";
                string url = $"{server}:{GlobalParas.tcp_data}/dataWraper/{file}";
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential("upload", "Thape123123");
                    byte[] data = client.UploadFile(new Uri(url), "PUT", local_path);
                    string reply = Encoding.UTF8.GetString(data);
                }
            }

            //对应服务器开始计算
            string pageHtml = "";
            try
            {
                var isinhost = IsHost(server);
                var isinhost_str = isinhost ? "1" : "0";
                var appHttp = $"{server}:{GlobalParas.tcp_app}/Cal/RunParkingStall?filename={file}&guid={guid}&isinHost={isinhost_str}";
                SetCertificatePolicy();
                List<Byte> pageData = new List<byte>();
                WebClientEx MyWebClient = new WebClientEx();
                MyWebClient.Credentials = new NetworkCredential("upload", "Thape123123");
                MyWebClient.Timeout = 30 * 60 * 1000;
                Task.Factory.StartNew(() =>
                {
                    pageData = MyWebClient.DownloadData(appHttp).ToList();
                }).Wait(-1);
                pageHtml = Encoding.UTF8.GetString(pageData.ToArray());

                //结果从对应服务器返回host服务器
                if (pageHtml.Contains("success") && !IsHost(server))
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential("upload", "Thape123123");
                        client.DownloadFile($"{server}:{GlobalParas.tcp_data}/genome/genome_{guid}.dat", data_dir + $"\\genome\\genome_{guid}.dat");
                        client.DownloadFile($"{server}:{GlobalParas.tcp_data}/log/MPLog_{guid}.txt", data_dir + $"\\log\\MPLog_{guid}.txt");
                    }
                }
                else if (!pageHtml.Contains("success"))
                {
                    //杀死可能存在的多余进程
                    try
                    {
                        Process[] processes = System.Diagnostics.Process.GetProcesses();
                        foreach (var process in processes)
                        {
                            if (process.ProcessName.Contains("ThParkingStallServer.Core") || process.ProcessName.Contains("ThParkingStall.Core"))
                                process.Kill();
                        }
                    }
                    catch { }
                    //释放服务器资源
                    lock (obj)
                    {
                        GlobalParas.SERVERS.Enqueue(server);
                    }
                    return "很抱歉！计算超时，图纸数据或程序发送错误。请将CAD图纸反馈至产品经理处理。";
                }
            }
            catch (Exception ex)
            {
                //杀死可能存在的多余进程
                try
                {
                    Process[] processes = System.Diagnostics.Process.GetProcesses();
                    foreach (var process in processes)
                    {
                        if (process.ProcessName.Contains("ThParkingStallServer.Core") || process.ProcessName.Contains("ThParkingStall.Core"))
                            process.Kill();
                    }
                }
                catch { }
                //释放服务器资源
                lock (obj)
                {
                    GlobalParas.SERVERS.Enqueue(server);
                }
                return "很抱歉！计算超时，图纸数据或程序发送错误。请将CAD图纸反馈至产品经理处理。";
            }
            //释放服务器资源
            lock (obj)
            {
                GlobalParas.SERVERS.Enqueue(server);
            }
            return pageHtml;
        }

        bool IsHost(string server)
        {
            return server.Equals("http://172.16.1.84");
        }
        static void SetCertificatePolicy()
        {
            ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidate;
        }
        static bool RemoteCertificateValidate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            return true;
        }
        static bool askAnswer(string server)
        {
            var appHttp = $"{server}:{GlobalParas.tcp_app}/Cal/AskAnswer";
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
            if(pageHtml.Contains("1"))
                return true;
            else return false;
        }

    }
    [ApiController]
    [Route("[controller]")]
    public class AskAnswer : ControllerBase
    {
        [HttpGet]
        public string askAnswer()
        {
            return "1";
        }
    }
    [ApiController]
    [Route("[controller]")]
    public class ReadServers:ControllerBase
    {
        [HttpGet]
        public string readServers()
        {
            if (GlobalParas.SERVERS == null)
                GlobalParas.InitSERVERS();
            var res = "";
            var list=GlobalParas.SERVERS.ToList();
            foreach (var item in list)
                res += item + ";";
            return res;
        }
    }
    [ApiController]
    [Route("[controller]")]
    public class ReadVersion : ControllerBase
    {
        [HttpGet]
        public string readVersion()
        {
            string path = @"C:\AIIIS\DATAIIS\version.dat";
            var ids = System.IO.File.ReadAllLines(path, Encoding.UTF8).ToList();
            return ids[0];
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class ValidateUser : ControllerBase
    {
        [HttpGet]
        public string validate(string employeeId)
        {
            string path = @"C:\AIIIS\DATAIIS\userEmployeeId.txt";
            var ids = System.IO.File.ReadAllLines(path, Encoding.UTF8);
            foreach (var id in ids)
            {
                //var realId = HexStringToString(id, Encoding.UTF8);
                if (id.Contains(employeeId))
                    return "1";
            }
            return "0";
        }
        public static string HexStringToString(string hs, Encoding encode)
        {
            string strTemp = "";
            byte[] b = new byte[hs.Length / 2];
            for (int i = 0; i < hs.Length / 2; i++)
            {
                strTemp = hs.Substring(i * 2, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            //按照指定编码将字节数组变为字符串
            return encode.GetString(b);
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class RunParkingStall : ControllerBase
    {
        [HttpGet]
        public string Run(string filename = "",string guid="",string isinHost="")
        {
            //杀死可能存在的多余进程
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    if (process.ProcessName.Contains("ThParkingStallServer.Core") || process.ProcessName.Contains("ThParkingStall.Core"))
                        process.Kill();
                }
            }
            catch { }
            try
            {
                var pro = new System.Diagnostics.Process();
                pro.StartInfo.FileName = "ThParkingStallServer.Core\\ThParkingStallServer.Core.exe";
                pro.StartInfo.CreateNoWindow = false;
                pro.StartInfo.UseShellExecute = false;
                pro.StartInfo.RedirectStandardOutput = true;
                pro.StartInfo.Arguments = $"{filename} {guid} {isinHost}";
                var started = pro.Start();
                pro.WaitForExit();
                var rst = pro.StandardOutput.ReadToEnd();
                if (rst.Equals(""))
                {
                    rst = "未读取到任何数据！";
                }
                return rst;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
