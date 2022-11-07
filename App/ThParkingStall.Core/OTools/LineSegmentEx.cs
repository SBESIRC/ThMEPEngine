using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.OTools
{
    public static class LineSegmentEx
    {
        //方向向量
        public static Vector2D DirVector(this LineSegment lineSegment,bool normalize = true)
        {
            if (normalize) return new Vector2D(lineSegment.P0, lineSegment.P1).Normalize();
            else return new Vector2D(lineSegment.P0, lineSegment.P1);
        }
        //法向量
        public static Vector2D NormalVector(this LineSegment lineSegment)
        {
            return lineSegment.DirVector(true).RotateByQuarterCircle(1);
        }

        //返回原点与线垂直的向量，（保证正向）
        //以及若线在原点，沿向量平移多远（distance）可以到当前线的位置
        public static Vector2D OrgVector(this LineSegment lineSegment,out double distance)
        {
            var origin= new Coordinate(0, 0);
            var projection = lineSegment.Project(origin);
            distance = origin.Distance(projection);
            if (distance < PointEx.EqualTol)//线段延长线与原点重合
            {
                distance = 0;
                return lineSegment.NormalVector().Positivize();//取原线法向正向化向量
            }
            else
            {
                var orgvec = new Vector2D(projection.X, projection.Y).Normalize();
                if (!orgvec.IsPositive()) distance = -distance;
                return orgvec.Positivize();
            }
        }
        //正向化 保证P1 X坐标大于等于P0 X,相等则 P1 Y坐标大于等于X
        public static LineSegment Positivize(this LineSegment lineSegment)
        {
            var P0 = lineSegment.P0;
            var P1 = lineSegment.P1;
            if (Math.Abs(P1.X - P0.X) < PointEx.EqualTol)//x相等
            {
                if (P1.Y > P0.Y) return lineSegment;
                else return new LineSegment(P1,P0);
            }
            //x不相等
            if (P1.X > P0.X) return lineSegment;
            else return new LineSegment(P1, P0);

        }
        //public static bool IsPositive(this LineSegment lineSegment)
        //{
        //    if (lineSegment.P1.X > lineSegment.P0.X) return true;
        //    else if (lineSegment.P1.X < lineSegment.P0.X) return false;
        //    else if (lineSegment.P1.Y > lineSegment.P0.Y) return true;
        //    else return false;
        //}
        public static LineSegment Translate(this LineSegment lineSegment, Vector2D vector)
        {
            return new LineSegment(vector.Translate(lineSegment.P0), vector.Translate(lineSegment.P1));
        }
        //平移buffer 以初始线的位置和平移后线的位置组成的矩形做buffer
        public static Polygon ShiftBuffer(this LineSegment lineSegment,double distance, Vector2D direction)
        {
            return lineSegment.ShiftBuffer(direction.Normalize().Multiply(distance));
        }

        public static Polygon ShiftBuffer(this LineSegment lineSegment, Vector2D vector)
        {
            var points = new Coordinate[] { lineSegment.P0, lineSegment.P1, 
                vector.Translate(lineSegment.P1), vector.Translate(lineSegment.P0), lineSegment.P0 };

            return new Polygon(new LinearRing(points));
        }
        //获取线上下offset 给定距离的矩形
        public static Polygon OGetRect(this LineSegment lineSegment,double offsetSize)
        {
            var vector = lineSegment.NormalVector().Multiply(offsetSize);
            var orgpt = vector.Translate(lineSegment.P0);
            var points = new Coordinate[] { orgpt,vector.Translate( lineSegment.P1),
                vector.Negate().Translate(lineSegment.P1), vector.Negate().Translate(lineSegment.P0), orgpt };
            return new Polygon(new LinearRing(points));
        }
        //延长
        public static LineSegment OExtend(this LineSegment lineSegment,double distance,bool extendP0= true,bool extendP1 = true)
        {
            if (lineSegment == null) return null;
            var direction = lineSegment.DirVector();
            var P0 = lineSegment.P0;
            var P1 = lineSegment.P1;
            if (extendP0) P0 = direction.Negate().Multiply(distance).Translate(P0);
            if (extendP1) P1 = direction.Multiply(distance).Translate(P1);
            return new LineSegment(P0, P1);
        }

        public static bool ParallelTo(this LineSegment lineSegment, LineSegment other,double tol = 0.01)
        {
            var dirThis = lineSegment.DirVector();
            var dirOther = other.DirVector();

            return dirThis.Distance(dirOther) < tol || dirThis.Distance(dirOther.Negate()) < tol;
        }

    }
}
