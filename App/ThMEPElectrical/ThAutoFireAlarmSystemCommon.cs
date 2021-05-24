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


        //列数量
        //public static readonly int SystemColNum = 21;
        //左部分的列数量
        public static readonly int SystemColLeftNum = 5;
        //左部分的列数量
        public static readonly int SystemColRightNum = 16;

        //图层
        public static readonly string BlockByLayer = "E-FAS-DEVC";     //块的图层名

        //块
        public static readonly string OuterBorderBlockName = "E-ES01";     //黄色外边框块名
        public static readonly string OuterBorderBlockByLayer = "E-UNIV-DIAG";     //黄色外边框图层名
        public static readonly int OuterBorderBlockColorIndex = 2;         //黄色外边框线颜色

        public static readonly int SystemDiagramChartHeight = 1500;         //表头的高度
        public static readonly string SystemDiagramChartHeader1 = "楼层或防火分区";
        public static readonly string SystemDiagramChartHeader2 = "楼层或防火分区分线箱";
        public static readonly string SystemDiagramChartHeader3 = "楼层或防火分区火灾自动报警设备";
    }
}
