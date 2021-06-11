using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThColumnExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry, IGroup
    {
        public List<Polyline> Columns { get; private set; }
        private List<ThTempSpace> Spaces { get; set; }  
        
        public ThColumnExtractor()
        {
            Columns = new List<Polyline>();
            Category = BuiltInCategory.Column.ToString();
            UseDb3Engine = true;
            ElementLayer = "柱";
            Spaces = new List<ThTempSpace>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var columnEngine = new ThColumnRecognitionEngine();
                columnEngine.Recognize(database, pts);
                Columns = columnEngine.Elements.Select(o => o.Outline as Polyline).ToList();
            }
            else
            {
                var instance = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                instance.Extract(database, pts);
                Columns = instance.Polys;
            }
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Columns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
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
            using (var db =AcadDatabase.Use(database))
            {
                var columnIds = new ObjectIdList();
                Columns.ForEach(o =>
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

        public void SetSpaces(List<ThTempSpace> spaces)
        {
            Spaces = spaces;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                Columns.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
