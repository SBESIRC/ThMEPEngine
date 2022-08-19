using System;

namespace ThMEPLighting.Common
{
    public class ThGarageLightCommon
    {
        public static readonly string DxCenterLineLayerName = "E-LANE-CENTER";        //布灯线槽中心线图层
        public static readonly string FdxCenterLineLayerName = "E-LITE-CENTER-JOIN";   //非布灯线槽中心线图层
        public static readonly string SingleRowCenterLineLayerName = "E-LITE-CENTER-SINGLE";   //单排线槽中心线图层
        public static readonly string TCHCableTrayLayerName = "E-LITE-CMTB";   //天正桥架图层
        public static readonly string LightNumberPrefix = "WL"; //灯编号前缀
        public static readonly double RegionBorderBufferDistance = 500.0; //用于裁剪
        public static readonly double RepeatedPointDistance = 2.1; //两点重合的距离
        public static readonly double BranchPortToMainDistance = 2.0; //分支端点距离主分支的距离容差
        public static readonly double LineCoincideTolerance = 1e-4; //线重合容差
        public static readonly string LaneLineLightBlockName = "E-BL001-1";
        public static readonly string ThGarageLightAppName = "ThGarageLight";
        public static readonly double ThShortLightLineLength = 5.0;
        public static readonly double LineOuterAngLimited = Math.PI / 4.0; // 两直线外角
    }
}
