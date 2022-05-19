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

        private readonly Dictionary<string, string> BLACKLIST = new Dictionary<string, string>
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

            // 杂项
            {"THMEPVERSION", "版本查看"},
            {"THMEPPROFILE", "专业配置"},
        };

        private readonly Dictionary<string, string> WHITELIST = new Dictionary<string, string>
        {
            // 云筑
            {"BASICSALIGN", "基础对柱中"},
            {"CSLPJYKL", "梁纵筋初始设置"},
            {"DRAWRAILING", "水平栏杆"},
            {"FILLFIREZONE", "分区填充"},
            {"FYDKFH", "楼板洞口"},
            {"FYDYXT", "墙身配筋"},
            {"FYJBBX", "降板边线"},
            {"LPJTOOL", "梁纵筋校改"},
            {"RAMPWAY2", "坡道剖面"},
            {"DRIPPING", "滴水绘制"},
            {"SFSB", "梯板配筋"},
            {"WND1", "凸窗墙身"},
            {"WND2", "平窗墙身"},
            {"WND3", "阳台墙身"},
            {"WND4", "平台墙身"},
            {"FINDTEXT2", "相同文字选择"},
            {"FINDTEXTINCLUDE2", "包含文字选择"},
            {"TEXTMERGE", "文字合并"},
            {"TH-TZ", "云线提资"},
            {"WZJS", "数字批处理"},
            {"DIMLINE", "标多段线"},
            {"REDIMLINE", "刷新线长"},
            {"GOW", "世界坐标系"},
            {"U2P", "两点坐标系"},
            {"MEASUREPATH", "距离校核"},
            {"XTZS", "标准图库"},
            {"SETWALLXLINE", "设置墙身"},
            {"DRAWWALLXLINE", " 绘制墙身"},
        };

        private readonly Dictionary<string, string> USERWHITELIST = new Dictionary<string, string>
        {
            // AI研究中心
            {"000176", "聂琳"},

            // 上海公建一所
            {"000329", "黄先岳"},
            {"000167", "马恒"},
            {"000769", "黄国强"},

            // 武汉天华建筑七所
            {"000189", "辜权恒"},

            // 武汉天华建筑八所
            {"009978", "王宽"},
            {"000881", "王旭升"},
            {"000124", "胡建科"},

            // 武汉天华建筑十所
            {"000981", "冯云聪"},

            // 武汉天华总师室
            {"025143", "齐雪钦"},
        };

        public bool IsTHCommand(string name)
        {
            if (BLACKLIST.ContainsKey(name))
            {
                return false;
            }
            if (WHITELIST.ContainsKey(name))
            {
                return true;
            }
            return name.StartsWith("TH");
        }

        public bool IsAuthorizedTHCommand(string cmd, string eid)
        {
            // 车位布置
            if (cmd == "THZDCWBZ")
            {
                return USERWHITELIST.ContainsKey(eid);
            }

            // 其他命令
            return true;
        }

        public string Description(string name)
        {
            return string.Empty;
        }
    }
}
