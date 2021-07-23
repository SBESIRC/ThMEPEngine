using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCoolSupplyStart
    {
        public Point3d Pt { get; private set; }
        public String AreaId { get; private set; }

        private const string CategoryPropertyName = "Category";
        private const string Category = "WaterSupplyStartPoint";
        public static string AreaIdPropertyName = "AreaId";
        public ThDrainageSDCoolSupplyStart(Point3d pt, string areaId)
        {
            Pt = pt;
            AreaId = areaId;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();

            var geometry = new ThGeometry();
            geometry.Properties.Add(CategoryPropertyName, Category);
            geometry.Properties.Add(AreaIdPropertyName, AreaId);
            geometry.Boundary = new DBPoint(Pt);
            geos.Add(geometry);

            return geos;
        }
    }

    public class ThDrainageSDRegion
    {
        private const string AlignmentVectorPropertyName = "AlignmentVector";
        private const string NeibourIdsPropertyName = "NeighborIds";
        private const string IdPropertyName = "Id";
        private const string CategoryPropertyName = "Category";
        private const string Category = "Area";

        public string AreaId { get; private set; }

        public Polyline Frame { get; private set; }

        public ThDrainageSDRegion(Tuple<Point3d, Point3d> leftRight, string areaId)
        {
            AreaId = areaId;
            Frame = toFrame(leftRight);
        }
        private Polyline toFrame(Tuple<Point3d, Point3d> leftRight)
        {
            var pl = new Polyline();
            var ptRT = new Point2d(leftRight.Item2.X, leftRight.Item1.Y);
            var ptLB = new Point2d(leftRight.Item1.X, leftRight.Item2.Y);

            pl.AddVertexAt(pl.NumberOfVertices, leftRight.Item1.ToPoint2D(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, ptRT, 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, leftRight.Item2.ToPoint2D(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, ptLB, 0, 0, 0);

            pl.Closed = true;

            return pl;

        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();

            var geometry = new ThGeometry();
            geometry.Properties.Add(IdPropertyName, AreaId);
            geometry.Properties.Add(CategoryPropertyName, Category);
            geometry.Properties.Add(AlignmentVectorPropertyName, new double[] { 1.000000, 0.000000 });
            geometry.Properties.Add(NeibourIdsPropertyName, new string[] { });
            geometry.Boundary = Frame;
            geos.Add(geometry);

            return geos;
        }
    }
}
