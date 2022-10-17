using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace ThParkingStall.Host.Controllers
{
    public static class GlobalParas
    {
        public static int NUM = 0;
    }

    [ApiController]
    [Route("[controller]")]
    public class Tester : ControllerBase
    {
        [HttpGet]
        public string DispatchInvoke(string s = "")
        {
            GlobalParas.NUM++;
            return "nihao" + s + GlobalParas.NUM.ToString();
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class TesterX : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            try
            {
                var pro = new System.Diagnostics.Process();
                //pro.StartInfo.FileName = "ThParkingStallServer.Core.exe";
                pro.StartInfo.FileName = "TestApp\\Happy.exe";
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
