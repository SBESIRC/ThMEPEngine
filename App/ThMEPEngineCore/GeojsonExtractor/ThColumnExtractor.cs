using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPEngineCore.GeojsonExtractor
{    
    public class ThColumnExtractor: ThExtractorBase,IPrint
    {
        public List<Polyline> Columns { get; private set; }
        private List<ThIfcRoom> Rooms { get; set; }
        public ThColumnExtractor()
        {
            Category = BuiltInCategory.Column.ToString();
            Columns = new List<Polyline>();
            Rooms = new List<ThIfcRoom>();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var isolateColumns = ThElementIsolateFilterService.Filter(Columns.Cast<Entity>().ToList(), Rooms);
            Columns.ForEach(o =>
            {                
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if(IsolateSwitch)
                {
                    var isolate = isolateColumns.Contains(o);
                    geometry.Properties.Add(ThExtractorPropertyNameManager.IsolatePropertyName, isolate);
                }                
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            using (var columnEngine = new ThColumnRecognitionEngine())
            {
                columnEngine.Recognize(database, pts);
                Columns = columnEngine.Elements.Select(o => o.Outline as Polyline).ToList();
            }
        }
        public override void SetRooms(List<ThIfcRoom> rooms)
        {
            this.Rooms = rooms;
        }

        public void Print(Database database)
        {
            Columns.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);            
        }
    }
}
