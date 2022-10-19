using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.Tools;

namespace ThParkingStall.Core.OTools
{
    public static class PointEx
    {
        public static double EqualTol = 0.01;
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
            if(Math.Abs(P1.X-P0.X) < EqualTol)//x相等
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
                if (current.X -pre.X> EqualTol)//当前x值与之前差值过大
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

        public static HashSet<Coordinate> GroupAndFilter(this List<Coordinate> coordinates,double tol = 0.1)
        {
            var groups = new List<List<int>>();
            var rest_idx = new List<int>();
            for (int i = 0; i < coordinates.Count; ++i) rest_idx.Add(i);
            while (rest_idx.Count != 0)
            {
                bool foundRelation = false;
                foreach (var group in groups)
                {
                    foreach (var idx in rest_idx)
                    {
                        var coor = coordinates[idx];
                        foundRelation = coor.IsWithInDistance(coordinates.Slice(group),tol);
                        if (foundRelation)
                        {
                            foundRelation = true;
                            group.Add(idx);
                            rest_idx.Remove(idx);
                            break;
                        }
                    }
                    if (foundRelation) break;
                }
                if (!foundRelation)
                {
                    groups.Add(new List<int> { rest_idx.First() });
                    rest_idx.RemoveAt(0);
                }
            }
            var result = new HashSet<Coordinate>();
            foreach(var group in groups)
            {
                var coors = coordinates.Slice(group);
                result.Add(new Coordinate(coors.Average(c=>c.X), coors.Average(c=>c.Y)));
            }
            return result;
        }

        public static bool IsWithInDistance(this Coordinate coordinate,List<Coordinate> others,double tol = 0.1)
        {
            return others.Min(c =>c.Distance(coordinate))< tol;
        }
    }
}
