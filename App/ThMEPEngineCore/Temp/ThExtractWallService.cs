using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractWallService : ThExtractService
    {
        public List<Polyline> Walls { get; set; }
        public ThExtractWallService()
        {
            Walls = new List<Polyline>();
        }
        public override void Extract(Database db, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is Polyline polyline)
                    {
                        if (IsElementLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            Walls.Add(newPolyline);
                        }
                    }
                }
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Walls.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Walls = objs.Cast<Polyline>().ToList();
                }                
            }
        }

        public override bool IsElementLayer(string layer)
        {
            return layer == ElementLayer;
        }
    }
}
