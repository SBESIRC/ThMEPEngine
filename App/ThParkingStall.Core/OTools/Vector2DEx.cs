using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.OTools
{
    public static class Vector2DEx
    {
        public static Vector2D Positivize(this Vector2D vector,bool normalize = true)
        {
            //Vector2D result;
            if(Math.Abs(vector.X) < PointEx.EqualTol)//x 值为0
            {
                if (normalize)
                {
                    return new Vector2D(0, 1.0);
                }
                else
                {
                    return new Vector2D(0,Math.Abs( vector.Y));
                }
            }
            else
            {
                Vector2D result = vector;
                if(normalize) result = result.Normalize();
                if (vector.X > 0) return result;
                else return new Vector2D(-result.X, -result.Y);
            }
        }

        public static bool IsPositive(this Vector2D vector)
        {
            if (Math.Abs(vector.X) < PointEx.EqualTol)//x 值为0
            {
                if(vector.Y > 0) return true;   
                else return false;
            }
            else
            {
                if (vector.X > 0) return true;
                else return false;
            }
        }

        public static double CrossProduct(this Vector2D vector , Vector2D other)
        {
            return (vector.X * other.Y) - (vector.Y * other.X);
        }
    }
}
