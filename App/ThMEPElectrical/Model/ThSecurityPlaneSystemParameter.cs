using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Config;

namespace ThMEPElectrical.Model
{
    public class ThSecurityPlaneSystemParameter
    {
        public readonly string VideoMonitoringSystem = "视频监控系统";
        public readonly string IntrusionAlarmSystem = "入侵报警系统";
        public readonly string AccessControlSystem = "出入口控制系统";
        public readonly string GuardTourSystem = "电子巡更系统";
        public readonly string RoomNameControl = "房间名称处理";
        /// <summary>
        /// 摄像机均布间距
        /// </summary>
        public double videoDistance { get; set; }

        /// <summary>
        /// 摄像机纵向盲区距离
        /// </summary>
        public double videoBlindArea { get; set; }

        /// <summary>
        /// 摄像机最大成像距离
        /// </summary>
        public double videaMaxArea { get; set; }

        /// <summary>
        /// 出入口控制系统配置表
        /// </summary>
        public DataTable accessControlSystemTable { get; set; }

        /// <summary>
        /// 视频监控系统配置表
        /// </summary>
        public DataTable videoMonitoringSystemTable { get; set; }

        /// <summary>
        /// 入侵报警系统配置表
        /// </summary>
        public DataTable intrusionAlarmSystemTable { get; set; }

        /// <summary>
        /// 电子巡更系统配置表
        /// </summary>
        public DataTable guardTourSystemTable { get; set; }

        /// <summary>
        /// 房间名称映射处理表
        /// </summary>
        public DataTable RoomInfoMappingTable { get; set; }

        /// <summary>
        /// 房间名称映射处理表树状结构
        /// </summary>
        public List<RoomTableTree> RoomInfoMappingTree { get; set; }
    }
}