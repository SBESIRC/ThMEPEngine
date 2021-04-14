using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThColumnExtractor : ThExtractorBase, IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> Columns { get; private set; }
        private List<ThTempSpace> Spaces { get; set; }
        public bool UseDb3ColumnEngine { get; set; }
        public ThColumnExtractor()
        {
            Columns = new List<Polyline>();
            Category = "Column";
            UseDb3ColumnEngine = false;
            ElementLayer = "柱";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3ColumnEngine)
            {
                var columnEngine = new ThColumnRecognitionEngine();
                columnEngine.Recognize(database, pts);
                Columns = columnEngine.Elements.Select(o => o.Outline as Polyline).ToList();
            }
            else
            {
                var instance = new ThExtractColumnService()
                {
                    ElementLayer = this.ElementLayer,
                };
                instance.Extract(database, pts);
                Columns = instance.Columns;
            }
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Columns.ForEach(o =>
            {                
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                var isolate = IsIsolate(Spaces, o);
                geometry.Properties.Add(IsolatePropertyName, isolate);
                geometry.Properties.Add(AreaOwnerPropertyName, BuildString(GroupOwner, o));
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
            Columns.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }
    }
}
