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
        //P1是否在P0正方向
        public static bool PositiveTo(this Coordinate P1,Coordinate P0)
        {
            if (P1.X > P0.X) return true;
            else if (P1.X < P0.X) return false;
            else if (P1.Y > P0.Y) return true;
            else return false;
        }

    }
}
