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
        /// 天华MEP命令集
        /// </summary>
        private static Dictionary<string, string> THMEPCOMMANDS = new Dictionary<string, string>
        {
            // 给排水.地上平面图
            {"THPYSPM", "住宅排水雨水"},

            // 给排水.地上系统图
            {"THJSXTT",     "给水"},
            {"THYSXTT",     "雨水"},
            {"THPSXTT",     "排水"},
            {"THXHSXTT",    "消火栓"},

            // 给排水.地下平面图
            {"THPL",        "消防喷头"},
            {"THDXCX",      "冲洗点位"},
            {"THSJSB",      "潜水泵"},
            {"THDXXHS",     "消火栓"},

            // 给排水.地下系统图
            {"THDXXHSXTT",  "消火栓"},

            // 给排水.大样轴侧图
            {"THJSDY",      "给水大样"},
            {"THJSZC",      "给水轴测"},

            // 给排水.校核
            {"THXHSJH",      "消火栓校核"},

            // 暖通.设备选型
            {"THFJ",                "风机选型"},
            {"THFJSYSTEMCOPY",      "复制风机"},
            {"THFJSYSTEMERASE",     "删除风机" },
            {"THFJSYSTEMINSERT",    "插入风机"},

            // 暖通.平面图
            {"THFJF",       "机房平面"},
            {"THDKFPM",     "地库风平面"},
            {"THDKFPMFG",   "风管修改"},
            {"THDKFPMXG",   "整体修改"},

            // 电气.前置输入
            {"THLCDY",      "楼层定义"},
            {"THTCD",       "提车道中心线"},

            // 电气.火灾报警系统
            {"THYWG",       "烟感温感布置"},
            {"THFDL",       "无吊顶避梁"},
            {"THFDCP",      "有吊顶避梁"},
            {"THFDFS",      "无梁楼盖"},
            {"THYGMQ",      "烟感盲区"},
            {"THGB",        "地库广播"},
            {"THGBLX",      "广播连线"},
            {"THGBMQ",      "广播盲区"},
            {"THHZXT",      "火灾报警系统"},

            // 电气.照明
            {"THCDZM",      "车道照明"},
            {"THCDBH",      "车道照明编号"},
            {"THCDTJ",      "车道照明回路"},
            {"THCDZMBZ",    "车道照明布置"},
            {"THDXC",       "布灯线槽中心线绘制"},
            {"THFDXC",      "非布灯线槽中心线绘制"},
            {"THYJZM",      "车道应急照明"},
            {"THYJZMLX",    "应急照明连线"},
            {"THSSLJ",      "生成疏散途径"},
            {"THSSZSDBZ",   "疏散指示灯布置"},

            // 电气.提资接收
            {"THTZZH",      "提资转换"},

            // 建筑.机电提资
            {"THFJJC",      "风机基础"},
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
