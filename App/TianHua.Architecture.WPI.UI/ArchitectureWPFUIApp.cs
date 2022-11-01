﻿using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThMEPArchitecture.MultiProcess;
using ThMEPIdentity;
using TianHua.Architecture.WPI.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThParkingStall.ClientUpdate;
namespace TianHua.Architecture.WPI.UI
{
    public class ArchitectureWPFUIApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        /// <summary>
        /// 地下车库车位排布, 天华地下车位(THZDCWBZ)
        /// </summary>
        /// 
        [CommandMethod("TIANHUACAD", "THZDCWBZ", "CWBZ_CmdId", CommandFlags.Modal)]
        public void ThCreateParkingStallsWithUI()
        {
            if (!UiParkingStallArrangement.DebugLocal)
            {
                if (!Validate())
                    return;
            }
            var w = new UiParkingStallArrangement();
            AcadApp.ShowModelessWindow(w);
        }
        [CommandMethod("TIANHUACAD", "CWBZ", CommandFlags.Modal)]
        public void ThCreateParkingStallsWithUI1()
        {
            if (!UiParkingStallArrangement.DebugLocal)
            {
                if (!Validate())
                    return;
            }
            var w = new UiParkingStallArrangement();
            AcadApp.ShowModelessWindow(w);
        }
        bool Validate()
        {
            if (!ValidateIdentity())
                return false;
            if (!ValidateVersion())
                return false;
            return true;
        }
        bool ValidateIdentity()
        {
            ThAcsSystemService.Instance.Initialize();
            var employeeId = ThAcsSystemService.Instance.EmployeeId;
            //请求服务器身份验证
            //开发用户豁免
            var userNameMatched = false;
            var userName = System.Environment.UserName.ToUpper();
            var userList = new List<string>() { "WANGWENGUANG", "ZHANGWENXUAN", "YUZHONGSHENG" };
            foreach (var name in userList)
                if (name.Contains(userName))
                    userNameMatched = true;
            //
            if (!userNameMatched)
            {
                if (employeeId != "")
                {
                    var appHttp = $"http://172.16.1.84:8088/Cal/ValidateUser?employeeId={employeeId}";
                    ServerGenerationService.SetCertificatePolicy();
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
                    if (pageHtml == "0")
                    {
                        MessageBox.Show("对不起，您暂未获取授权");
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show("对不起，您暂未获取授权");
                    return false;
                }
            }
            return true;
        }
        bool ValidateVersion()
        {
            var curVersion = UiParkingStallArrangement.Version;
            var dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var romoteVersion = "";
            var appHttp = $"http://172.16.1.84:8088/Cal/ReadVersion";
            ServerGenerationService.SetCertificatePolicy();
            List<Byte> pageData = new List<byte>();
            string pageHtml = "";
            WebClientEx MyWebClient = new WebClientEx();
            MyWebClient.Credentials = new NetworkCredential("upload", "Thape123123");
            MyWebClient.Timeout = 10 * 60 * 1000;
            Task.Factory.StartNew(() =>
            {
                pageData = MyWebClient.DownloadData(appHttp).ToList();
            }).Wait(-1);
            romoteVersion = Encoding.UTF8.GetString(pageData.ToArray());

            if (romoteVersion != curVersion)
            {
                var dialogResult = MessageBox.Show("您当前的地库版本过低，点击升级自动更新为最新版：(请先保存CAD文件数据)", "版本提示", MessageBoxButton.OKCancel);
                if (dialogResult==MessageBoxResult.OK)
                {
                    var pro = new System.Diagnostics.Process();
                    pro.StartInfo.FileName = "ThParkingStall.ClientUpdate.exe";
                    pro.StartInfo.Arguments = dir;
                    pro.StartInfo.CreateNoWindow = false;
                    pro.StartInfo.UseShellExecute = false;
                    pro.StartInfo.RedirectStandardOutput = true;
                    var started = pro.Start();
                    pro.WaitForExit();
                }
                return false;
            }
            else
                return true;
        }
    }
}
