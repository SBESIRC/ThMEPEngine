using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThOuterOtherColumnExtractor:ThExtractorBase, IExtract, IPrint, IBuildGeometry, IGroup
    {   
        private List<ThTempSpace> Spaces { get; set; }
        public List<Entity> OuterColumns { get ; set ; }
        public List<Entity> OtherColumns { get ; set ; }       

        public ThOuterOtherColumnExtractor()
        {
            ElementLayer = "柱";
            TesslateLength = 10.0;
            Spaces = new List<ThTempSpace>();
            OuterColumns = new List<Entity>();
            OtherColumns = new List<Entity>(); 
            Category = BuiltInCategory.Column.ToString();
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var service = new ThExtractOuterOtherColumnService()
            {
                ElementLayer = this.ElementLayer,
            };
            service.Extract(database, pts);
            IColumnData columnData = service;
            OuterColumns = columnData.OuterColumns.Select(o=>ThTesslateService.Tesslate(o,TesslateLength)).ToList();
            OtherColumns = columnData.OtherColumns.Select(o => ThTesslateService.Tesslate(o, TesslateLength)).ToList();
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            OuterColumns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName,"外圈柱");
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

            OtherColumns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "其他柱");
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

        public void SetSpaces(List<ThTempSpace> spaces)
        {
            Spaces = spaces;
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
