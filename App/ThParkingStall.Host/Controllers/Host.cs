using System;
using System.Collections.Generic;
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
            SERVERS.Enqueue("http://172.16.1.109");
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
            var waitServerTime = 0.1 * 60;//unit:s
            var curWaitServerTime = 0;
            lock (obj)
            {
                GlobalParas.FILES.Enqueue(filename);
                while (true)
                {
                    if (GlobalParas.SERVERS.Count > 0)
                    {
                        server = GlobalParas.SERVERS.Dequeue();
                        file = GlobalParas.FILES.Dequeue();
                        break;
                    }
                    else
                    {
                        if (curWaitServerTime > waitServerTime)
                        {
                            GlobalParas.FILES.Dequeue();
                            curWaitServerTime = -1;
                            break;
                        }
                        Thread.Sleep(2 * 1000);
                        curWaitServerTime += 10;
                    }
                }
            }
            if (curWaitServerTime == -1)
            {
                return "服务器繁忙中，等待超时,请稍候再试。";
            }


            //把文件发送到对应服务器
            var data_dir = @"C:\AIIIS\DATAIIS";
            if (!IsHost(server))
            {
                var local_path = data_dir + $"\\{file}";
                string url = $"{server}:{GlobalParas.tcp_data}/{file}";
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential("upload", "Thape123123");
                    byte[] data = client.UploadFile(new Uri(url), "PUT", local_path);
                    string reply = Encoding.UTF8.GetString(data);
                }
            }

            //对应服务器开始计算
            //var responseContent = "";
            //HttpWebRequest httpWebRequest = WebRequest.Create($"{server}Cal/RunParkingStall?filename={file}") as HttpWebRequest;
            //httpWebRequest.Method = "GET";
            //HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse; // 获取响应
            //if (httpWebResponse != null)
            //{
            //    using (StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream()))
            //    {
            //        responseContent = sr.ReadToEnd();
            //    }
            //    httpWebResponse.Close();
            //}
            var appHttp = $"{server}:{GlobalParas.tcp_app}/Cal/RunParkingStall?filename={file}&guid={guid}";
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
            //结果从对应服务器返回host服务器
            if (pageHtml.Contains("success") && !IsHost(server))
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential("upload", "Thape123123");
                    client.DownloadFile($"{server}:{GlobalParas.tcp_data}/genome_{guid}.dat", data_dir + $"\\genome_{guid}.dat");
                }
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
    public class RunParkingStall : ControllerBase
    {
        [HttpGet]
        public string Run(string filename = "",string guid="")
        {
            try
            {
                var pro = new System.Diagnostics.Process();
                pro.StartInfo.FileName = "ThParkingStallServer.Core\\ThParkingStallServer.Core.exe";
                pro.StartInfo.CreateNoWindow = false;
                pro.StartInfo.UseShellExecute = false;
                pro.StartInfo.RedirectStandardOutput = true;
                pro.StartInfo.Arguments = $"{filename} {guid}";
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
