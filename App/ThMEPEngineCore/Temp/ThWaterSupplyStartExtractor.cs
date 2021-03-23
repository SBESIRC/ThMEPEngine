using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThWaterSupplyStartExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Curve> WaterSupplyStarts { get; private set; }
        public ThWaterSupplyStartExtractor()
        {
            Category = "给水起点";
            WaterSupplyStarts = new List<Curve>();
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
                geometry.Properties.Add("Group", BuildString(GroupOwner, o));
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }    
        
        public void Group(Dictionary<Polyline, string> groupId)
        {
            WaterSupplyStarts.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
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
