using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
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
    //[Serializable]
    //public class ORamp
    //{
    //    //插入点
    //    public Point InsertPt { get; set; }
    //    //坡道上车道的向量
    //    //public Vector2D Vector { get; set; }
    //    public (double,double) Vector { get; set; }
    //    //坡道的面域
    //    public Polygon Area { get; set; }
    //    public double RoadWidth { get; set; }
    //    public ORamp(SegLine segLine, Polygon area)
    //    {
    //        var segLineStr = segLine.Splitter.GetLineString();
    //        InsertPt = area.Shell.Intersection(segLineStr).Coordinates.First().ToPoint();
    //        var outSidePart = segLineStr.Difference(area).Centroid;
    //        var vector = new Vector2D(InsertPt.Coordinate, outSidePart.Coordinate).Normalize();
    //        Vector = (vector.X,vector.Y);
    //        Area = area;
    //        if (segLine.RoadWidth == -1) RoadWidth = VMStock.RoadWidth;
    //        else RoadWidth = segLine.RoadWidth;
    //    }
    //    public ORamp()
    //    {

    //    }

    //    public ORamp(LineSegment rampLine, Polygon area)
    //    {

    //    }
    //    public ORamp Clone()
    //    {
    //        var clone = new ORamp();
    //        clone.InsertPt = InsertPt.Copy() as Point;
    //        clone.Vector = (Vector.Item1,Vector.Item2);
    //        clone.Area = Area.Copy() as Polygon;
    //        clone.RoadWidth = RoadWidth;
    //        return clone;
    //    }

    //    public ORamp Transform(Vector2D vector)
    //    {
    //        AffineTransformation transformation = new AffineTransformation();
    //        transformation.SetToTranslation(vector.X, vector.Y);
    //        var clone = new ORamp();
    //        clone.InsertPt = transformation.Transform(InsertPt) as Point;
    //        clone.Vector = (Vector.Item1, Vector.Item2);
    //        clone.Area = transformation.Transform(Area) as Polygon;
    //        clone.RoadWidth = RoadWidth;
    //        return clone;

    //    }
    //}

    [Serializable]
    public class ORamp
    {
        public Coordinate InsertPt { get; set; }
        //坡道上车道的向量
        //public Vector2D Vector { get; set; }
        public (double, double) Vector { get; set; }

        public ORamp(Coordinate insertPt, Vector2D vector)
        {
            InsertPt = insertPt;
            Vector = (vector.X,vector.Y);
        }
        public ORamp(Coordinate insertPt, double X,double Y)
        {
            InsertPt = insertPt;
            Vector = (X, Y);
        }
        public ORamp Clone()
        {
            return new ORamp(InsertPt,new Vector2D(Vector.Item1,Vector.Item2));
        }
        public ORamp Transform(Vector2D vector)
        {
            return new ORamp(vector.Translate(InsertPt), Vector.Item1, Vector.Item2);
        }
        public Vector2D GetVector()
        {
            return new Vector2D(Vector.Item1, Vector.Item2);
        }
    }

}
