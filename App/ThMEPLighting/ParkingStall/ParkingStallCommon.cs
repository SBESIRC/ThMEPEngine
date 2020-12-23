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
        public static readonly double ParkingPolyExtendLength = 100; // 车位外扩100mm

        public static readonly Scale3d BlockScale = new Scale3d(100, 100, 100);

        public static readonly string PARK_LIGHT_LAYER = "E-LITE-LITE";
        public static readonly string PARK_LIGHT_BLOCK_NAME = "E-BL001-1";






    }
}
