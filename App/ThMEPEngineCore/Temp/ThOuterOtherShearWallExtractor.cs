using System;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Temp
{
    public class ThOuterOtherShearWallExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry, IGroup
    {   
        private List<ThTempSpace> Spaces { get; set; }
        public List<Entity> OuterShearWalls { get ; set ; }
        public List<Entity> OtherShearWalls { get ; set ; }
        private Dictionary<Entity, List<string>> BelongArchitectureIdDic { get; set; }

        public ThOuterOtherShearWallExtractor()
        {
            ElementLayer = "剪力墙";
            TesslateLength = 20;
            Spaces = new List<ThTempSpace>();
            OuterShearWalls = new List<Entity>();
            OtherShearWalls = new List<Entity>(); 
            Category = BuiltInCategory.ShearWall.ToString();
            BelongArchitectureIdDic = new Dictionary<Entity, List<string>>();
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var service = new ThExtractOuterOtherShearWallService()
            {
                ElementLayer = this.ElementLayer,
            };
            service.Extract(database, pts);
            IShearWallData shearWallData = service;
            shearWallData.OuterShearWalls.ForEach(o =>
            {
                var tesslate = ThTesslateService.Tesslate(o, TesslateLength);
                OuterShearWalls.Add(tesslate);
                var archOutlineIds = ThParseArchitectureOutlineIdService.ParseBelongedArchitectureIds(o);
                BelongArchitectureIdDic.Add(tesslate, archOutlineIds);
            });
            OtherShearWalls = shearWallData.OtherShearWalls.Select(o =>ThTesslateService.Tesslate(o, TesslateLength)).ToList();
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            OuterShearWalls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "外圈剪力墙");
                geometry.Properties.Add(BelongedArchOutlineIdPropertyName, BuildString(BelongArchitectureIdDic, o,","));
                if (IsolateSwitch)
                {
                    var isolate = IsIsolate(Spaces, o);
                    geometry.Properties.Add(IsolatePropertyName, isolate);
                }
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            OtherShearWalls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "其他剪力墙");
                if (IsolateSwitch)
                {
                    var isolate = IsIsolate(Spaces, o);
                    geometry.Properties.Add(IsolatePropertyName, isolate);
                }
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
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

        public void SetSpaces(List<ThTempSpace> spaces)
        {
            Spaces = spaces;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                OuterShearWalls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
                OtherShearWalls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
