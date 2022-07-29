using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.Tools;

namespace ThParkingStall.Core.InterProcess
{
    [Serializable]
    public class Ramp
    {
        //插入点
        public Point InsertPt { get; set; }
        //坡道的面域
        public Polygon Area { get; set; }
        public Ramp(Point insertPt, Polygon area)
        {
            InsertPt = insertPt;
            Area = area;
        }
        public LineSegment GetLine(double tol = 100)
        {
            var rampLine = Area.Shell.ToLineSegments().OrderBy(l=>l.Distance(InsertPt.Coordinate)).First();
            var X_dif = Math.Abs(rampLine.P0.X - rampLine.P1.X);
            var Y_dif = Math.Abs(rampLine.P0.Y - rampLine.P1.Y);
            var insideSize = 500;
            var outsideSize = VMStock.RoadWidth / 2;
            if(X_dif > Y_dif)//坡道线横向
            {
                var pt0 = new Coordinate(InsertPt.X, InsertPt.Y - tol);
                var pt1 = new Coordinate(InsertPt.X, InsertPt.Y + tol);
                if(Area.Contains(pt0.ToPoint()))
                {
                    return new LineSegment(InsertPt.X, InsertPt.Y - insideSize, InsertPt.X, InsertPt.Y + outsideSize);
                }
                else
                {
                    return new LineSegment(InsertPt.X, InsertPt.Y - outsideSize, InsertPt.X, InsertPt.Y + insideSize);
                }
                
            }
            else
            {
                var pt0 = new Coordinate(InsertPt.X - tol, InsertPt.Y);
                var pt1 = new Coordinate(InsertPt.X + tol, InsertPt.Y);
                //return new LineSegment(InsertPt.X-tol, InsertPt.Y , InsertPt.X+tol, InsertPt.Y);
                if (Area.Contains(pt0.ToPoint()))
                {
                    return new LineSegment(InsertPt.X - insideSize, InsertPt.Y, InsertPt.X + outsideSize, InsertPt.Y);
                }
                else
                {
                    return new LineSegment(InsertPt.X - outsideSize, InsertPt.Y, InsertPt.X + insideSize, InsertPt.Y);
                }
            }
        }
    }
}
