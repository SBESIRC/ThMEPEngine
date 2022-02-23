using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Config
{
    public static class ThRegularMatchingService
    {
        public static string starMatching = @"([A-Z]?|\d+)";    //*匹配正则（*为任意大写字母或数字或不存在均可）
        public static string xMatching = @"(0*([1-9]+0*)+|)";   //x匹配正则（x为任意非0数字 或不存在）
        public static string textMatching = @"([\u4e00-\u9fa5]*)";                //#匹配正则（#为任意数量汉字字符）    

        public static bool Matching(this string str, string str1)
        {
            if (str == str1)
            {
                return true;
            }

            string replaceStr = str;
            replaceStr = '^' + replaceStr;
            replaceStr = replaceStr + '$';
            if (str.Contains("*"))
            {
                replaceStr = replaceStr.Replace("*", starMatching);
            }
            if (str.Contains("x"))
            {
                replaceStr = replaceStr.Replace("x", xMatching);
            }
            if (str.Contains("#"))
            {
                replaceStr = replaceStr.Replace("#", textMatching);
            }
            return Regex.IsMatch(str1, replaceStr);
        }
    }
}
