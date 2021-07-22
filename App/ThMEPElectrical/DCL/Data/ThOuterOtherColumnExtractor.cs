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

namespace ThMEPElectrical.DCL.Data
{
    public class ThOuterOtherColumnExtractor:ThExtractorBase, IPrint, IGroup
    {   
        public List<Entity> OuterColumns { get ; set ; }
        public List<Entity> OtherColumns { get ; set ; }    
        public Dictionary<Entity, string> BelongArchitectureIdDic { get; set; } 

        public ThOuterOtherColumnExtractor()
        {
            ElementLayer = "柱";
            OuterColumns = new List<Entity>();
            OtherColumns = new List<Entity>(); 
            Category = BuiltInCategory.Column.ToString();
            BelongArchitectureIdDic = new Dictionary<Entity, string>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
           //
        }
        
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            OuterColumns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "外圈柱");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner,o));
                geometry.Properties.Add(ThExtractorPropertyNameManager.BelongedArchOutlineIdPropertyName, BelongArchitectureIdDic[o]);
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            OtherColumns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "其他柱");
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
                var outerColumnIds = new ObjectIdList();
                OuterColumns.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    outerColumnIds.Add(db.ModelSpace.Add(o));
                });
                if (outerColumnIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), outerColumnIds);
                }
                var otherColumnIds = new ObjectIdList();
                OtherColumns.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    otherColumnIds.Add(db.ModelSpace.Add(o));
                });
                if (otherColumnIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), otherColumnIds);
                }
            }
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                OuterColumns.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
                OtherColumns.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
