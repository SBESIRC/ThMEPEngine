using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace ThParkingStall.Host.Controllers
{
    public static class GlobalParas
    {
        public static Queue<string> FILES = new Queue<string>();
        public static Queue<string> SERVERS = null;
        public static void InitSERVERS()
        {
            SERVERS = new Queue<string>();
            SERVERS.Enqueue("http://172.15.3.134/");
        }
    }


    [ApiController]
    [Route("[controller]")]
    public class GetGenome : ControllerBase
    {
        object obj=new object();
        [HttpGet]
        public string Process(string filename = "")
        {
            if (GlobalParas.SERVERS == null)
                GlobalParas.InitSERVERS();

            //
            var server = "";
            var file = "";
            var waitServerTime = 0.5 * 60;//unit:s
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
                        Thread.Sleep(10 * 1000);
                        curWaitServerTime += 10;
                    }
                }
            }
            if (curWaitServerTime == -1)
            {
                return "服务器繁忙中，等待超时,请稍候再试。";
            }

            //
            var responseContent = "";
            HttpWebRequest httpWebRequest = WebRequest.Create($"{server}Cal/RunParkingStall?filename={file}") as HttpWebRequest;
            httpWebRequest.Method = "GET";
            HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse; // 获取响应
            if (httpWebResponse != null)
            {
                using (StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    responseContent = sr.ReadToEnd();
                }
                httpWebResponse.Close();
            }
            lock (obj)
            {
                GlobalParas.SERVERS.Enqueue(server);
            }
            return responseContent;

            return "hello world"+ filename;
        }
    }


    [ApiController]
    [Route("[controller]")]
    public class RunParkingStall : ControllerBase
    {
        [HttpGet]
        public string Run(string filename = "")
        {
            Thread.Sleep (60 * 1000);
            return "RunParkingStall" + filename;
            try
            {
                var pro = new System.Diagnostics.Process();
                pro.StartInfo.FileName = "ThParkingStallServer.Core\\ThParkingStallServer.Core.exe";
                pro.StartInfo.CreateNoWindow = false;
                pro.StartInfo.UseShellExecute = false;
                pro.StartInfo.RedirectStandardOutput = true;
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
