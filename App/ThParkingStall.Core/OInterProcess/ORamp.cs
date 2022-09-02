using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
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
        //public Vector2D Vector { get; set; }
        public (double,double) Vector { get; set; }
        //坡道的面域
        public Polygon Area { get; set; }
        public double RoadWidth { get; set; }
        public ORamp(SegLine segLine, Polygon area)
        {
            var segLineStr = segLine.Splitter.GetLineString();
            InsertPt = area.Shell.Intersection(segLineStr).Coordinates.First().ToPoint();
            var outSidePart = segLineStr.Difference(area).Centroid;
            var vector = new Vector2D(InsertPt.Coordinate, outSidePart.Coordinate).Normalize();
            Vector = (vector.X,vector.Y);
            Area = area;
            if (segLine.RoadWidth == -1) RoadWidth = VMStock.RoadWidth;
            else RoadWidth = segLine.RoadWidth;
        }
    }
}
