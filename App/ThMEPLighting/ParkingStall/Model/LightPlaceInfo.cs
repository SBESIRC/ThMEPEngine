using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public enum Light_Place_Type
    {
        LONG_EDGE,
        SHORT_EDGE,
    }

    public enum ParkingSpace_Type
    {
        Invalid,
        Parallel_Parking,
        Reverse_stall_Parking,
    }

    public class BigGroupInformation
    {
        public Polyline BigGroupPoly;
        public Line BigGroupLongLine;
        public Line BigGroupShortLine;

        public BigGroupInformation(Polyline groupPoly, Line groupLongLine, Line groupShortLine)
        {
            BigGroupPoly = groupPoly;
            BigGroupLongLine = groupLongLine;
            BigGroupShortLine = groupShortLine;
        }
    }

    public class LightPlaceInfo
    {
        public Point3d Position;
        public Line LongDirLength;
        public Line ShortDirLength;

        public double Angle;

        public BigGroupInformation BigGroupInfo; // 每组的大轮廓

        public bool IsUsed = false;

        public ParkingSpace_Type ParkingSpace_TypeInfo = ParkingSpace_Type.Invalid;

        public LightPlaceInfo(Point3d position, Line longDirLength, Line shortDirLength)
        {
            Position = position;
            LongDirLength = longDirLength;
            ShortDirLength = shortDirLength;
        }
    }
}
