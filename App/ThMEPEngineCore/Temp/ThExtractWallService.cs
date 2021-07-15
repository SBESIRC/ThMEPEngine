using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractWallService : ThExtractService
    {
        public List<Entity> Walls { get; set; }
        public ThExtractWallService()
        {
            Walls = new List<Entity>();
        }
        public override void Extract(Database db, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if(!IsValidType(ent))
                    {
                        continue;
                    }
                    if (ent is Polyline polyline)
                    {
                        if (IsElementLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            Walls.Add(newPolyline);
                        }
                    }
                    else if(ent is Hatch hatch)
                    {
                        if (IsElementLayer(hatch.Layer))
                        {
                            var polygons = hatch.ToPolygons();
                            Walls.AddRange(polygons.Select(o=>o.ToDbEntity()));
                        }
                    }
                    else if(ent is Solid solid)
                    {
                        if (IsElementLayer(solid.Layer))
                        {
                            var clone = solid.WashClone();
                            Walls.Add(clone.ToPolyline());
                        }
                    }
                }
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Walls.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Walls = objs.Cast<Entity>().ToList();
                }                
            }
        }
    }
}
