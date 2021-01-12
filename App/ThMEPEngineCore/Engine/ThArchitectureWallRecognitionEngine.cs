using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThArchitectureWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var archWallDbExtension = new ThArchitectureWallDbExtension(database))
            {
                archWallDbExtension.BuildElementCurves();
                var curves = new DBObjectCollection();
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
                    curves = archWallDbExtension.WallCurves.ToCollection();
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
}
