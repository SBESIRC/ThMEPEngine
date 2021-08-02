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

        public ThDrainageSDRegion(Polyline Frame, string areaId)
        {
            AreaId = areaId;
            this.Frame = Frame;
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
