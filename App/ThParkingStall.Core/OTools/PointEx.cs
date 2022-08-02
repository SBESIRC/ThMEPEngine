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
        public static double PositiveTol = 0.001;
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
            if(Math.Abs(P1.X-P0.X) < PositiveTol)//x相等
            {
                if (P1.Y > P0.Y) return true;
                else return false;  
            }
            //x不相等
            if (P1.X > P0.X) return true;
            else return false;
        }
        public static List<Coordinate> PositiveOrder(this IEnumerable<Coordinate> coordinates)
        {
            var result = new List<Coordinate>();
            if (coordinates.Count() == 0) return result;
            var X_ordered = coordinates.OrderBy(c => c.X).ToList();
            var pre = X_ordered.First();
            if (coordinates.Count() == 1) return new List<Coordinate>{ pre};
            //数量至少为2
            var Y_to_order = new List<Coordinate>();
            Y_to_order.Add(pre);
            for (int i = 1; i < X_ordered.Count; i++)
            {
                var current = X_ordered[i];
                if (current.X -pre.X> PositiveTol)//当前x值与之前差值过大
                {
                    result.AddRange(Y_to_order.OrderBy(c => c.Y));//根据y排序后清空
                    Y_to_order.Clear();
                }
                pre = current;
                Y_to_order.Add(pre);
            }
            result.AddRange(Y_to_order.OrderBy(c => c.Y));
            return result;
        }


    }
}
