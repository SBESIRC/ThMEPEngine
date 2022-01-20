using System.Collections.Generic;

namespace ThMEPIdentity
{
    public class ThMEPCmdService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPCmdService instance = new ThMEPCmdService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPCmdService() { }
        internal ThMEPCmdService() { }
        public static ThMEPCmdService Instance { get { return instance; } }

        private readonly Dictionary<string, string> WHITELIST = new Dictionary<string, string>
        {
            // 数字化设计中心
            {"THAFL", "户型平面"},
            {"THAEC", "参数建构"},
            {"THDIM", "轴网标注"},
            {"THFLR", "厨卫家具"},
            {"THPLT", "淡彩出图"},
            {"THDATA", "一键解析"},
            {"THAREA", "更新面积表"},
            {"THSEL", "批量选择"},
            {"THBLD", "生成标准楼栋块"},
            {"THSAMPLEPLAN", "素材库"},
        };

        public bool IsTHCommand(string name)
        {
            if (WHITELIST.ContainsKey(name))
            {
                return false;
            }
            return name.StartsWith("TH");
        }

        public string Description(string name)
        {
            return string.Empty;
        }
    }
}
