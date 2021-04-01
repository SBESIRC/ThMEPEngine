using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Temp
{
    public class ThWaterSupplyPositionExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Curve> WaterSupplyPositions { get; private set; }
        public ThWaterSupplyPositionExtractor()
        {
            Category = "给水点位";
            WaterSupplyPositions = new List<Curve>();
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
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(GroupOwnerPropertyName, BuildString(GroupOwner, o)); 
                if(o is Polyline polyline && polyline.IsRectangle())
                {
                    var centerPt = ThGeometryTool.GetMidPt(polyline.GetPoint3dAt(0), polyline.GetPoint3dAt(2));
                    geometry.Boundary = new DBPoint(centerPt);
                }
                else
                {
                    geometry.Boundary = o;
                }
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

        public void Group(Dictionary<Entity, string> groupId)
        {
            WaterSupplyPositions.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }
    }
}
