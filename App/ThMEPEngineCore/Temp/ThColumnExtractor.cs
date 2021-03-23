using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Temp
{
    public class ThColumnExtractor : ThExtractorBase, IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> Columns { get; private set; }
        public bool UseDb3ColumnEngine { get; set; }
        private Dictionary<Curve, List<Polyline>> ColumnOwner { get; set; }

        public ThColumnExtractor()
        {
            Columns = new List<Polyline>();
            Category = "Column";
            UseDb3ColumnEngine = false;
            ColumnOwner = new Dictionary<Curve, List<Polyline>>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if(UseDb3ColumnEngine)
            {
                var columnEngine = new ThColumnRecognitionEngine();
                columnEngine.Recognize(database, pts);
                Columns = columnEngine.Elements.Select(o => o.Outline as Polyline).ToList();
            }
            else
            {
                var instance = new ThExtractColumnService();
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
                geometry.Properties.Add("Category", Category);
                geometry.Properties.Add("Group", BuildString(ColumnOwner,o));
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

        public void Group(List<Polyline> groups)
        {
            Columns.ForEach(o =>
            {
                ColumnOwner.Add(o, groups.Where(g => g.Contains(o)).ToList());
            });
        }
    }
}
