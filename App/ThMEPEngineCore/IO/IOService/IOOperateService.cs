using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.IO.IOService
{
    public static class IOOperateService
    {
        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool FileExist(string url)
        {
            return File.Exists(url);
        }

        /// <summary>
        /// 判断文件夹是否存在
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool FolderExist(string url)
        {
            return Directory.Exists(url);
        }

        /// <summary>
        /// 检查文件是否存在，如果不存在则创建该文件
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool CreateFile(string url)
        {
            if (!File.Exists(url))
            {
                File.Create(url);
            }
            return true;
        }

        /// <summary>
        /// 检查文件夹是否存在，如果不存在则创建该文件夹
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool CreateFolder(string url)
        {
            if (!Directory.Exists(url))
            {
                Directory.CreateDirectory(url);
            }
            return true;
        }
    }
}
