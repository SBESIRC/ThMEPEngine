using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThArchitectureWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (ThCADCoreNTSFixedPrecision fixedPrecision = new ThCADCoreNTSFixedPrecision())
            using (var archWallDbExtension = new ThArchitectureWallDbExtension(database))
            {
                archWallDbExtension.BuildElementCurves();
                List<Curve> curves = new List<Curve>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    archWallDbExtension.WallCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        curves.Add(filterObj as Curve);
                    }
                }
                else
                {
                    curves = archWallDbExtension.WallCurves;
                }
                curves.ForEach(o =>
                {
                    foreach (Polyline item in ThArchitectureWallSimplifier.Simplify(o as Polyline))
                    {
                        Elements.Add(ThIfcWall.Create(item));
                    }
                });
            }
        }
    }
}
