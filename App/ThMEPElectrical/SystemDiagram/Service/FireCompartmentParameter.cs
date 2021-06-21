using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Service
{
    /// <summary>
    /// 火灾自动报警系统图的参数信息，这里用在界面UI和命令之间传递参数
    /// </summary>
    public static class FireCompartmentParameter
    {
        public static List<string> LayerNames { get; set; } = new List<string>() { ThAutoFireAlarmSystemCommon.FireDistrictByLayer };

        /// <summary>
        /// 控制总线计数模块
        /// </summary>
        public static int ControlBusCount = 200;

        /// <summary>
        /// 短路隔离器计数模块
        /// </summary>
        public static int ShortCircuitIsolatorCount = 32;

        /// <summary>
        /// 消防广播火灾启动计数模块
        /// </summary>
        public static int FireBroadcastingCount = 20;

        /// <summary>
        /// 底部固定部分:1.包含消防室 2.不含消防室 3.仅绘制计数模块
        /// </summary>
        public static int FixedPartType = 1;

        /// <summary>
        /// 系统图生成方式： 
        /// V1.0 按防火分区区分
        /// V2.0 按回路区分
        /// </summary>
        public static int SystemDiagramGenerationType = 1;
    }
}
