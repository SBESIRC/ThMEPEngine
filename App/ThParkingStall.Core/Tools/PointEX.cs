using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

namespace ThParkingStall.Core.Tools
{
    public static class PointEX
    {
        public static LineSegment LineBuffer(this Coordinate point,double Halfdistance, Vector2D vec)
        {
            var Vec = vec.Normalize();
            var P0 = Vec.RotateByQuarterCircle(1).Multiply (Halfdistance).Translate(point);
            var P1 = Vec.RotateByQuarterCircle(-1).Multiply(Halfdistance).Translate(point);
            return new LineSegment(P0, P1);
        }
        //从点左右halfbuffer
        public static LineSegment LineBuffer(this Point point, double Halfdistance, LineSegment line)
        {
            return LineBuffer(point.Coordinate, Halfdistance, line.GetVector());
        }
        public static Point Move(this Point point ,double distance,int flag)
        {
            if (flag == 0)//上
            {
                return new Point(point.X, point.Y + distance);
            }
            else if (flag == 1)//下
            {
                return new Point(point.X, point.Y - distance);
            }
            else if (flag == 2)//左
            {
                return new Point(point.X - distance, point.Y);
            }
            else if (flag == 3)//右
            {
                return new Point(point.X + distance, point.Y);
            }
            else return point;
        }
        public static Point ToPoint(this Coordinate coor)
        {
            return new Point(coor);
        }
        public static bool OnIncreaseDirectionOf(this Point pt,LineSegment lineSegment)
        {
            if (lineSegment.IsVertical())//比较水平坐标
            {
                return pt.X > lineSegment.P0.X;
            }
            else//比较垂直坐标
            {
                return pt.Y > lineSegment.P0.Y;
            }
        }

        public static double Distance(this Coordinate coor,List<Coordinate> coordinates)
        {
            return coordinates.Min(c => c.Distance(coor));
        }
        public static bool ExistPtInDirection(this Coordinate coor, IEnumerable<Coordinate> coordinates,bool PosDirection)
        {
            if(coordinates.Count() == 0) return false;
            if (PosDirection) return coordinates.Any(c => (c.X + c.Y) > (coor.X + coor.Y));
            else return coordinates.Any(c => (c.X + c.Y) < (coor.X + coor.Y));
        }
    }
}
