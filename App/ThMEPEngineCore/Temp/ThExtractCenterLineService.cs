using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractCenterLineService : ThExtractService
    {
        public List<Curve> CenterLines { get; set; }

        public ThExtractCenterLineService()
        {
            CenterLines = new List<Curve>();
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
                            CenterLines.Add(newPolyline);
                        }
                    }
                    else if(ent is Line line)
                    {
                        if (IsElementLayer(line.Layer))
                        {
                            var newLine = line.Clone() as Line;
                            CenterLines.Add(newLine);
                        }
                    }
                }
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(CenterLines.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    CenterLines = objs.Cast<Curve>().ToList();
                }                
            }
        }
    }
}
