using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using NFox.Cad;
using ThMEPEngineCore.IO;
using ThCADExtension;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThShearwallExtractor : ThExtractorBase, IPrint
    {
        public List<Entity> Walls { get; set; }
        protected List<ThIfcRoom> Rooms { get; set; }
        public ThShearwallExtractor()
        {
            Category = BuiltInCategory.ShearWall.ToString();
            Walls = new List<Entity>();
            Rooms = new List<ThIfcRoom>();
            TesslateLength = 100.0;
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var isolateShearwalls = new List<Entity>();
            if (IsolateSwitch)
            {
                isolateShearwalls = ThElementIsolateFilterService.Filter(Walls, Rooms);
            }
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var isolate = isolateShearwalls.Contains(o);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IsolatePropertyName, isolate);
                geometry.Boundary = o;
                if (IsolateSwitch) // 表示只传入孤立的剪力墙
                {
                    if (isolate)
                    {
                        geos.Add(geometry);
                    }
                }
                else
                {
                    geos.Add(geometry);
                }
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var shearWallEngine = new ThShearWallBuilderEngine();
                shearWallEngine.Build(database, pts);
                Walls = shearWallEngine.Elements
                    .Select(o => o.Outline)
                    .OfType<Entity>()
                    .ToList();
            }
            else
            {
                var instance = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                instance.Extract(database, pts);

                Walls = instance.Polys
                    .Select(o=>o.TessellatePolylineWithArc(TesslateLength))
                    .Cast<Entity>()
                    .ToList();
            }
            if (FilterMode == FilterMode.Window)
            {
                Walls = FilterWindowPolygon(pts, Walls);
            }
        }
        public override void SetRooms(List<ThIfcRoom> rooms)
        {
            this.Rooms = rooms;
        }

        public void Print(Database database)
        {
            Walls.CreateGroup(database, ColorIndex);
        }

        public override List<Entity> GetEntities()
        {
            return Walls;
        }
    }
}
