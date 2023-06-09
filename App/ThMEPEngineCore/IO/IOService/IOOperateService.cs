﻿using System;
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

        /// <summary>
        /// 创建新文件
        /// </summary>
        /// <param name="oldUrl"></param>
        /// <param name="strAttURL"></param>
        public static void CreateNewFile(string oldUrl, string strAttURL)
        {
            File.Copy(oldUrl, strAttURL);
        }

        /// <summary> 
        /// 输出成txt
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="content"></param>
        public static void OutputTxt(string filePath, List<string> content)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                StreamWriter sw = new StreamWriter(fs);
                foreach (var con in content)
                {
                    sw.Write(con);
                }
                
                sw.Close();
            }
        }
    }
}
