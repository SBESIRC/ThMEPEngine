using System;
using System.Collections.Generic;
using ThAnalytics.SDK;

namespace ThAnalytics
{
    public class ThCybrosService : IADPServices
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThCybrosService instance = new ThCybrosService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThCybrosService() { }
        internal ThCybrosService() { }
        public static ThCybrosService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        private readonly Dictionary<string, string> THMEPCOMMANDS = new Dictionary<string, string>
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
            {"THYJZM", "车道应急照明"},

            // 给排水
            {"THPL", "喷头工具"},
        };

        public void Initialize()
        {
            //
        }

        public void UnInitialize()
        {
            //
        }

        public void StartSession()
        {
            THRecordingService.SessionBegin();
        }

        public void EndSession()
        {
            THRecordingService.SessionEnd();
        }

        public void RecordTHCommandEvent(string cmdName, double duration)
        {
            // 非协同用户
            if (string.IsNullOrEmpty(ThAcsSystemService.Instance.UserId))
            {
                return;
            }

            // 非指定命令
            if (!THMEPCOMMANDS.ContainsKey(cmdName))
            {
                return;
            }

            // 记录命令事件
            Segmentation thsegmentation = new Segmentation();
            thsegmentation.Add("名称", cmdName);
            thsegmentation.Add("功能", THMEPCOMMANDS[cmdName]);
            thsegmentation.Add("用户", ThAcsSystemService.Instance.UserId);
            thsegmentation.Add("项目", ThAcsSystemService.Instance.ProjectNumber);
            THRecordingService.RecordEvent("天华命令使用", (int)duration, thsegmentation);
        }

        public void RecordSysVerEvent(string sysverName, string sysverValue)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("名称", sysverName);
            segmentation.Add("值", sysverValue);
            THRecordingService.RecordEvent("CAD系统变量", 0, segmentation);
        }
    }
}
