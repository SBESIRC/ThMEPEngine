using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThWaterSupplyPositionExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Curve> WaterSupplyPositions { get; private set; }
        private Dictionary<Curve, List<Polyline>> WaterSupplyPositionOwner { get; set; }
        public ThWaterSupplyPositionExtractor()
        {
            Category = "给水点位";
            WaterSupplyPositions = new List<Curve>();
            WaterSupplyPositionOwner = new Dictionary<Curve, List<Polyline>>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractWaterSupplyPositionService();
            instance.Extract(database, pts);
            WaterSupplyPositions = instance.WaterSupplyPositions;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            WaterSupplyPositions.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add("Category", Category);
                geometry.Properties.Add("Group", BuildString(WaterSupplyPositionOwner, o));
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }        

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var columnIds = new ObjectIdList();
                WaterSupplyPositions.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    columnIds.Add(db.ModelSpace.Add(o));
                });
                if (columnIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), columnIds);
                }
            }
        }

        public void Group(List<Polyline> groups)
        {
            WaterSupplyPositions.ForEach(o =>
            {
                WaterSupplyPositionOwner.Add(o, groups.Where(g => g.Contains(o)).ToList());
            });
        }
    }
}
