using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.OTools
{
    public static class PointEx
    {
        public static LineSegment LineBuffer(this Coordinate point, double Halfdistance, Vector2D vec)
        {
            var Vec = vec.Normalize();
            var P0 = Vec.RotateByQuarterCircle(1).Multiply(Halfdistance).Translate(point);
            var P1 = Vec.RotateByQuarterCircle(-1).Multiply(Halfdistance).Translate(point);
            return new LineSegment(P0, P1);
        }


    }
}
