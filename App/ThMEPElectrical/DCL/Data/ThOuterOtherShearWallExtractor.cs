using System;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO;

namespace ThMEPElectrical.DCL.Data
{
    public class ThOuterOtherShearWallExtractor : ThExtractorBase, IPrint, IGroup
    {   
        public List<Entity> OuterShearWalls { get ; set ; }
        public List<Entity> OtherShearWalls { get ; set ; }
        public Dictionary<Entity, string> BelongArchitectureIdDic { get; set; }

        public ThOuterOtherShearWallExtractor()
        {
            ElementLayer = "剪力墙";
            OuterShearWalls = new List<Entity>();
            OtherShearWalls = new List<Entity>(); 
            Category = BuiltInCategory.ShearWall.ToString();
            BelongArchitectureIdDic = new Dictionary<Entity, string>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            OuterShearWalls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "外圈剪力墙");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner, o));
                geometry.Properties.Add(ThExtractorPropertyNameManager.BelongedArchOutlineIdPropertyName, BelongArchitectureIdDic[o]);                  
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            OtherShearWalls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "其他剪力墙");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner, o));
                //geometry.Properties.Add(ThExtractorPropertyNameManager.BelongedArchOutlineIdPropertyName, BuildString(BelongArchitectureIdDic, o, ","));                
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }        

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var outerShearWallIds = new ObjectIdList();
                OuterShearWalls.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    outerShearWallIds.Add(db.ModelSpace.Add(o));
                });
                if (outerShearWallIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), outerShearWallIds);
                }
                var otherShearWallIds = new ObjectIdList();
                OtherShearWalls.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    otherShearWallIds.Add(db.ModelSpace.Add(o));
                });
                if (otherShearWallIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), otherShearWallIds);
                }
            }
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                OuterShearWalls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
                OtherShearWalls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
        public void SetShearWallBelongArchOutlineId(Dictionary<Entity, string> archOutlineId)
        {
            OuterShearWalls.ForEach(o =>
            {
                foreach (Entity obj in archOutlineId.Keys)
                {
                    if (obj.IsContains(o))
                    {
                        BelongArchitectureIdDic.Add(o, archOutlineId[obj]);
                        break;
                    }
                }
            });
        }
    }
}
