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

        /// <summary>
        /// 天华命令（白名单）
        /// </summary>
        private static Dictionary<string, string> THMEPCOMMANDS = new Dictionary<string, string>
        {
            // 暖通
            {"THFJ", "风机选型"},
            {"THFJSYSTEMCOPY", "复制风机"},
            {"THFJSYSTEMERASE", "删除风机" },
            {"THFJSYSTEMINSERT", "插入风机"},
            {"THFJF", "机房平面"},

            // 电气
            {"THYWG", "烟感温感布置"},
            {"THFDL", "无吊顶避梁"},
            {"THFDCP", "有吊顶避梁"},
            {"THFDFS", "无梁楼盖"},
            {"THYGMQ", "烟感盲区"},
            {"THTCD",  "提车道中心线"},
            {"THDXC", "布灯线槽中心线绘制"},
            {"THFDXC", "非布灯线槽中心线绘制"},
            {"THGB", "地库广播"},
            {"THGBLX", "广播连线"},
            {"THGBMQ", "广播盲区"},
            {"THCDZM", "车道照明"},
            {"THCDBH", "车道照明编号"},
            {"THCDTJ", "车道照明回路"},
            {"THCDZMBZ", "车道照明布置"},

            // 照明
            {"THYJZM", "车道应急照明"},
            {"THYJZMLX", "应急照明连线"},
            {"THSSLJ", "生成疏散途径"},
            {"THSSZSDBZ", "疏散指示灯布置"},

            // 给排水
            {"THPL", "喷头工具"},
        };

        public bool IsTHCommand(string name)
        {
            return THMEPCOMMANDS.ContainsKey(name);
        }

        public string Description(string name)
        {
            return THMEPCOMMANDS[name];
        }
    }
}
