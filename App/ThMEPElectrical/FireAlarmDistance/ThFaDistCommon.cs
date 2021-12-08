using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.FireAlarmDistance
{
    public class ThFaDistCommon
    {
        public static string ManualAlartTag = "声光手报";
        public static string BroadcastTag = "消防广播";
        public static Dictionary<string, string> LayoutTagDict = new Dictionary<string, string>(){
                                                                    { "允许布置", "" },
                                                                    { "需要保护" , "不可布区域" },
                                                                    { "无需考虑" , "无需考虑" },
                                                                    {"必须布置" , "必布区域"},
                                                                };
        public static string LayoutTagRemove = "无需考虑";
    }
}
