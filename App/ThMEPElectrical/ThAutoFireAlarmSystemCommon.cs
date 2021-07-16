using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical
{
    public static class ThAutoFireAlarmSystemCommon
    {
        //防火分区最短长度
        public static readonly double FireDistrictShortestLength = 200;
        public static readonly string FireDistrictByLayer = "AD-AREA-DIVD";     //防火分区图层
        public static readonly string WireCircuitByLayer = "E-FAS-NUMB";     //电路编号图层

        //左部分的列数量
        public static readonly int SystemColLeftNum = 6;
        //左部分的列数量
        public static readonly int SystemColRightNum = 15;

        //图层
        public static readonly string BlockByLayer = "E-FAS-DEVC";     //块的图层名
        public static readonly string CountBlockByLayer = "E-UNIV-NOTE";     //计数模块的图层名

        //块
        public static readonly string OuterBorderBlockName = "E-ES01";     //黄色外边框块名
        public static readonly string CountBlockName = "E-ANNO04";     //计数模块块名
        public static readonly string OuterBorderBlockByLayer = "E-UNIV-DIAG";     //黄色外边框图层名
        public static readonly int OuterBorderBlockColorIndex = 2;         //黄色外边框线颜色

        public static readonly int SystemDiagramChartHeight = 1500;         //表头的高度
        public static readonly string SystemDiagramChartHeader1 = "楼层或防火分区";
        public static readonly string SystemDiagramChartHeader2 = "楼层或防火分区分线箱";
        public static readonly string SystemDiagramChartHeader3 = "楼层或防火分区火灾自动报警设备";
        public static readonly string FixedPartContainsFireRoom = "系统图固定部分（含消控室）";
        public static readonly string ManualControlCircuitModuleContainsFireRoom = "手动控制线路模块（含消控室）";
        public static readonly string LiquidLevelSignalCircuitModuleContainsFireRoom = "液位信号线路模块（含消控室）";
        public static readonly string FixedPartExcludingFireRoom = "系统图固定部分（不含消控室）";
        public static readonly string ManualControlCircuitModuleExcludingFireRoom = "手动控制线路模块（不含消控室）";
        public static readonly string LiquidLevelSignalCircuitModuleExcludingFireRoom = "液位信号线路模块（不含消控室）";
        public static readonly string FixedPartSmokeExhaust = "联动关闭排烟风机信号线";
        public static readonly string FireHydrantPumpDirectStartSignalLine = "消火栓泵直接启动信号线";
        public static readonly string SprinklerPumpDirectStartSignalLine = "喷淋泵直接启动信号线";
        public static readonly string SprinklerPumpDirectStartSignalLineModuleContainsFireRoom = "喷淋泵联动直接启动线路模块（含消控室）";
        public static readonly string SprinklerPumpDirectStartSignalLineModuleExcludingFireRoom = "喷淋泵联动直接启动线路模块（不含消控室）";
        public static readonly string FireHydrantPumpDirectStartSignalLineModuleContainsFireRoom = "消火栓泵联动直接启动线路模块（含消控室）";
        public static readonly string FireHydrantPumpDirectStartSignalLineModuleExcludingFireRoom = "消火栓泵联动直接启动线路模块（不含消控室）";
        public static readonly string FireHydrantPumpManualControlCircuitModuleContainsFireRoom = "消火栓泵手动控制线路模块（含消控室）";
        public static readonly string FireHydrantPumpManualControlCircuitModuleExcludingFireRoom = "消火栓泵手动控制线路模块（不含消控室）";
        public static readonly string SprinklerPumpManualControlCircuitModuleContainsFireRoom = "喷淋泵手动控制线路模块（含消控室）";
        public static readonly string SprinklerPumpManualControlCircuitModuleExcludingFireRoom = "喷淋泵手动控制线路模块（不含消控室）";

        //
        public static bool CanDrawFixedPartSmokeExhaust = false;
        public static bool CanDrawFireHydrantPump = false;
        public static bool CanDrawSprinklerPump = false;
        //配置
        public static readonly List<string> AlarmControlWireCircuitBlocks = new List<string>() { 
            "手动火灾报警按钮(带消防电话插座)", 
            "感烟火灾探测器", 
            "感温火灾探测器",
            "红外光束感烟火灾探测器发射器",
            "红外光束感烟火灾探测器接收器",
            "非编址感烟火灾探测器",
            "防爆型感烟火灾探测器",
            "家用感烟火灾探测报警器",
            "独立式感烟火灾探测报警器",
            "非编址感温火灾探测器",
            "防爆型感温火灾探测器",
            "线型差定温火灾探测器",
            "缆型感温火灾探测器",
            "家用感温火灾探测报警器",
            "独立式感温火灾探测报警器",
            "火焰探测器",
            "复合型感烟感温火灾探测器",
            "吸气式感烟火灾探测器",
            "图像型火灾探测器",
            "强电间总线控制模块", 
            "弱电间总线控制模块", 
            "防火卷帘模块", 
            "电梯模块", 
            "70℃防火阀+输入模块", 
            "150℃防火阀+输入模块", 
            "280℃防火阀+输入模块", 
            "电动防火阀", 
            "防排抽烟机", 
            "旁通阀", 
            "消火栓按钮", 
            "灭火系统流量开关", 
            "水流指示器", 
            "灭火系统压力开关", 
            "消防水箱", 
            "消火栓泵", 
            "喷淋泵", 
            "消防水池"
        };
        public static readonly List<string> Detectors = new List<string>()
        {
            "非编址感烟火灾探测器",
            "防爆型感烟火灾探测器",
            "家用感烟火灾探测报警器",
            "独立式感烟火灾探测报警器",
            "非编址感温火灾探测器",
            "防爆型感温火灾探测器",
            "线型差定温火灾探测器",
            "缆型感温火灾探测器",
            "家用感温火灾探测报警器",
            "独立式感温火灾探测报警器",
            "火焰探测器",
            "复合型感烟感温火灾探测器",
            "吸气式感烟火灾探测器",
            "图像型火灾探测器",
        };


        //按回路区分部分
        //连接点允许误差
        public const int ConnectionTolerance = 25;
    }
}
