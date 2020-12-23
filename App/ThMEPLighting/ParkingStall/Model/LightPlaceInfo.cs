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

    public class LightPlaceInfo
    {
        public Point3d Position;
        public Line LongDirLength;
        public Line ShortDirLength;

        public double Angle;

        public LightPlaceInfo(Point3d position, Line longDirLength, Line shortDirLength)
        {
            Position = position;
            LongDirLength = longDirLength;
            ShortDirLength = shortDirLength;
        }
    }
}
