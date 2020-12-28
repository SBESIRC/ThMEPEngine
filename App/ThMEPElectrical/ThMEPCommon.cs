using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical
{
    public static class ThMEPCommon
    {
        //广播
        public static readonly string ParkingLineLayer = "AD-SIGN";     //车位线图层
        public static readonly string BroadcastLayerName = "E-FAS-DEVC";     //消防应急广播图层
        public static readonly string BroadcastBlockName = "E-BFAS410-4";     //消防应急广播图块名
        public static readonly string BlindAreaLayer = "E-MQ-XFGB";     //广播盲区图层

        // 烟感温感
        public const string SENSORLAYERNMAE = "E-FAS-DEVC";
        public const string SMOKE_SENSOR_BLOCK_NAME = "E-BFAS110";
        public const string TEMPERATURE_SENSOR_BLOCK_NAME = "E-BFAS120";
        public const string UCS_COMPASS_BLOCK_NAME = "AI-UCS";
        public const string UCS_COMPASS_LAYER_NAME = "AI-UCS";
        public const string PROTECTAREA_LAYER_NAME = "E-FD-PR";
        public const string BLINDAREA_HATCH_LAYER_NAME = "E-FD-DA";

        // 图层
        public const short LANELINE_LAYER_COLORINDEX = 6;
        public const string LANELINE_LAYER_NAME = "E-LANE-CENTER";

        // 常量
        public static readonly Scale3d BlockScale = new Scale3d(100, 100, 100);
        public static readonly Point3d NullPoint3d = new Point3d(double.NaN, double.NaN, 0);
        public static readonly double spacingValue = 4500;

        public static readonly double ShrinkDistance = -500; // 内缩距离
        public static readonly double ShrinkSmallDistance = -3; // 误差内缩距离
        public static readonly double GridPolyExtendLength = 500; // 轴网两端延伸距离

        public static readonly double ExtendBeamLength = 25; // 梁的延伸长度
        public static readonly double WallProfileShrinkDistance = -100; // 用户选择墙线内缩距离
        public static readonly double PolyClosedDistance = 100; // 多段线视觉认为是闭合多段线的误差距离
        public static readonly double EntityExtendDistance = 1000; // 选择图元延长的距离
        public static readonly double SecondBeamDivideHeight = 600; // 梁高差高度差值大于等于600划分

        public static readonly double ValidBeamLength = 2000; // 有效的梁长度

        public static readonly double NearestDisTolerance = 2; //最近距离容差值

        public static readonly double ProtectAreaScatterLength = 200; // 保护半径离散长度
        public static readonly double PLbufferLength = 1; // pline buffer 宽度
    }
}

