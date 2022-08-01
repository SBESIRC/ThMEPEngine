using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.Reinforcement.Service
{
    public class ThIniTool
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,string key,string value,string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,string key,string defValue,StringBuilder retValue,int size,string filePath);
        /// <summary>
        /// 读取ini文件
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="defValue">未读取到值时的默认值</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static int ReadIni(string section,string key,string defValue,string filePath)
        {
            StringBuilder retValue = new StringBuilder();
            return GetPrivateProfileString(section,key,defValue,retValue,256,filePath);
        }
        /// <summary>
        /// 写入ini文件
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static long WriteIni(string section,string key,string value,string filePath)
        {
            return WritePrivateProfileString(section, key, value, filePath);
        }
        /// <summary>
        /// 删除节
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static long DeleteSection(string section,string filePath)
        {
            return WritePrivateProfileString(section,null,null,filePath);
        }
        /// <summary>
        /// 删除键
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static long DeleteKey(string section,string key, string filePath)
        {
            return WritePrivateProfileString(section, key, null, filePath);
        }
    }
}
