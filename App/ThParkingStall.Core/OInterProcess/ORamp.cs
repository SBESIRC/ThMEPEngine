using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;
namespace ThParkingStall.Core.OInterProcess
{
    [Serializable]
    public class ORamp
    {
        //插入点
        public Point InsertPt { get; set; }
        //坡道上车道的向量
        public Vector2D Vector { get; set; }
        //坡道的面域
        public Polygon Area { get; set; }
        public ORamp(LineSegment segLine, Polygon area)
        {
            var segLineStr = segLine.GetLineString();
            InsertPt = area.Shell.Intersection(segLineStr).Get<Point>().First();
            var outSidePart = segLineStr.Difference(area).Centroid;
            Vector = new Vector2D(InsertPt.Coordinate, outSidePart.Coordinate);
            Area = area;
        }
    }
}
