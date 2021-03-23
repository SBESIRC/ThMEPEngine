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
    public class ThWaterSupplyStartExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Curve> WaterSupplyStarts { get; private set; }
        private Dictionary<Curve, List<Polyline>> WaterSupplyStartOwner { get; set; }
        public ThWaterSupplyStartExtractor()
        {
            Category = "给水起点";
            WaterSupplyStarts = new List<Curve>();
            WaterSupplyStartOwner = new Dictionary<Curve, List<Polyline>>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractWaterSupplyStartService();
            instance.Extract(database, pts);
            WaterSupplyStarts = instance.WaterSupplyStarts;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            WaterSupplyStarts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add("Category", Category);
                geometry.Properties.Add("Group", BuildString(WaterSupplyStartOwner,o));
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }    
        
        public void Group(List<Polyline> groups)
        {
            WaterSupplyStarts.ForEach(o =>
            {
                WaterSupplyStartOwner.Add(o, groups.Where(g => g.Contains(o)).ToList());
            });
        }

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var columnIds = new ObjectIdList();
                WaterSupplyStarts.ForEach(o =>
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
    }
}
