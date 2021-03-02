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

        private readonly Dictionary<string, string> thcommanfunctiondict = new Dictionary<string, string>
        {
            // 登录界面
            {"THHLP", "帮助"},

            // 切换专业
            {"THPROFILE", "专业切换"},

            // 设置
            {"THALIAS", "快捷键"},
            
            // 图块图库
            {"THBLI", "图块集"},
            {"THBEE", "提电气块转换"},
            {"THBBR", "插块断线"},
            {"THBBE", "选块断线"},
            {"THBBS", "全选断线"},
            
            // 标注工具
            {"THCNU", "车位编号"},
            {"THDTA", "尺寸避让"},
            
            // 图层工具
            {"THALC", "建立建筑图层"},
            {"THSLC", "建立结构图层"},
            {"THMLC", "建立暖通图层"},
            {"THELC", "建立电气图层"},
            {"THAPL", "建立总图图层"},
            {"THAUL", "建立单体图层"},
            {"THPLC", "建立给排水图层"},
            {"THLPM", "暖通用"},
            {"THLPE", "电气用"},
            {"THLPP", "给排水用"},
            {"THMLK", "锁定暖通图层"},
            {"THMUK", "隔离暖通图层"},
            {"THUKA", "解锁所有图层"},
            {"THMOF", "关闭暖通图层"},
            {"THMON", "开启暖通图层"},
            {"THTF", "通风模式"},
            {"THSG", "水管模式"},
            {"THXF", "消防模式"},
            {"THNT", "暖通全显"},
            
            // 计算工具
            {"THBPS", "天华单体规整"},
            {"THSPS", "天华总体规整"},
            {"THBAC", "单体面积总汇"},
            {"THTET", "综合经济技术指标表"},
            {"THFET", "消防疏散表"},
            {"THABC", "房间面积框线"},

            // 校核工具
            {"THCRC", "柱校核"},

            // 平面绘图
            {"THSPC", "喷头布置"},
            {"THQS", "天华快选"},
            {"THAL", "天华对齐"},
            {"THCO", "天华复制"},
            {"THMA", "天华格式刷"},
            
            // 辅助工具
            {"THMSC",       "批量缩放"},
            {"THZ0",        "Z值归零"},
            {"THPURGE",     "DGN清理"},
            {"THBPT",       "批量打印PDF"},
            {"THBPD",       "批量打印DWF"},
            {"THBPP",       "批量打印PPT"},
            {"THSVM",       "版次信息修改"},
            {"THLTR",       "管线断线"},
            {"THMIR",       "文字块镜像"},

            // 第三方支持
            {"T20V4", "获取天正看图T20V4.0插件"},
            {"T20V5", "获取天正看图T20V5.0插件"},
        };

        public void Initialize()
        {
            // 从协同系统获取当前协同用户信息
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

        public void RecordCommandEvent(string cmdName, double duration)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("名称", cmdName);
            if (thcommanfunctiondict.ContainsKey(cmdName))
            {
                segmentation.Add("功能", thcommanfunctiondict[cmdName]);
            }
            THRecordingService.RecordEvent("CAD命令使用", (int)duration, segmentation);
        }

        public void RecordTHCommandEvent(string cmdName, double duration)
        {
            if (thcommanfunctiondict.ContainsKey(cmdName))
            {
                Segmentation thsegmentation = new Segmentation();
                thsegmentation.Add("名称", cmdName);
                thsegmentation.Add("功能", thcommanfunctiondict[cmdName]);
                THRecordingService.RecordEvent("天华命令使用", (int)duration, thsegmentation);
            }
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
