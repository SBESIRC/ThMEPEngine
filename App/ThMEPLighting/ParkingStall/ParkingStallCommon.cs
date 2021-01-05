using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall
{
    class ParkingStallCommon
    {
        public static readonly double MaxEdgeLength = 1000; // 车库长边的阈值
        public static readonly double PolyClosedDistance = 100; // 多段线视觉认为是闭合多段线的误差距离
        public static readonly double ParkingPolyEnlargeLength = 100; // 车位外扩100mm

        public static readonly Scale3d BlockScale = new Scale3d(100, 100, 100);

        public static readonly double LaneOffset = 5000; // 车道线偏移距离
        public static readonly double LaneLineExtendLength = 5000; // 车道线最大延长距离
        public static readonly double ParkingStallGroupWidthRestrict = 4000; // 短边限制
        public static readonly double ParkingStallGroupLengthRestrict = 6200; // 长边限制

        public static readonly string PARK_LIGHT_LAYER = "E-LITE-LITE";
        public static readonly string PARK_LIGHT_BLOCK_NAME = "E-BL001-1";

        public static readonly string LANELINE_LAYER_NAME = "E-LANE-CENTER";




    }
}
