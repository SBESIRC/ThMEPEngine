using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThArchitectureWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override List<ThRawIfcBuildingElementData> Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var archWallDbExtension = new ThArchitectureWallDbExtension(database))
            {
                archWallDbExtension.BuildElementCurves();
                return archWallDbExtension.WallCurves.Select(o => new ThRawIfcBuildingElementData()
                {
                    Geometry = o,
                }).ToList();
            }
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                {
                    curves.Add(filterObj as Curve);
                }
            }
            else
            {
                curves = objs;
            }
            if (curves.Count > 0)
            {
                var results = ThArchitectureWallSimplifier.Normalize(curves);
                results = ThArchitectureWallSimplifier.Simplify(results);
                results = ThArchitectureWallSimplifier.BuildArea(results);
                results.Cast<Entity>().ForEach(o => Elements.Add(ThIfcWall.Create(o)));
            }
        }
    }
}
