using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ThIdentity.SDK
{
    public static class FuncMac
    {
        /// <summary>
        /// 取得设备网卡的MAC地址
        /// </summary>
        public static string GetNetCardMacAddress()
        {
            const int MIN_ADDR_LENGTH = 12;
            string chosenMacAddress = string.Empty;
            long fastestFoundSpeed = -1;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                string potentialMacAddress = nic.GetPhysicalAddress().ToString();
                if (nic.Speed > fastestFoundSpeed && !string.IsNullOrEmpty(potentialMacAddress) && potentialMacAddress.Length >= MIN_ADDR_LENGTH)
                {
                    chosenMacAddress = potentialMacAddress;
                    fastestFoundSpeed = nic.Speed;
                }
            }

            return chosenMacAddress;
        }

        /// <summary>
        /// 取得设备网卡的IP地址
        /// </summary>
        public static string GetIpAddress()
        {
            string _HostName = Dns.GetHostName();   //获取本机名
     
            IPHostEntry localhost = Dns.GetHostEntry(_HostName);     

            IPAddress localaddr = localhost.AddressList[0];

            return localaddr.ToString();
        }

    }
}
