using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class ZiFu
    {
        /// <summary>
        /// 提取某个字符前所有的字符
        /// </summary>
        /// <param name="text"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string Right(this string text, string a)
        {
            return text.Substring(0, text.IndexOf(a));
        }

        /// <summary>
        /// 提取某个字符后的所有字符
        /// </summary>s
        /// <param name="text">文字内容</param>
        /// <param name="a">字符</param>
        /// <returns></returns>
        public static string Left(this string text, string a)
        {
            return text.Substring(text.IndexOf(a) + a.Length);
        }
    }
}
