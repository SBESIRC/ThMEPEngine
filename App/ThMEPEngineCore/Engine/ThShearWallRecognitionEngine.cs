using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThShearWallRecognitionEngine : ThBuildingElementRecognitionEngine, IDisposable
    {
        public ThShearWallRecognitionEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var shearWallDbExtension = new ThStructureShearWallDbExtension(database))
            {
                shearWallDbExtension.BuildElementCurves();
                List<Curve> curves = new List<Curve>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    shearWallDbExtension.ShearWallCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        curves.Add(filterObj as Curve);
                    }
                }
                else
                {
                    curves = shearWallDbExtension.ShearWallCurves;
                }
                curves.ForEach(o =>
                {
                    if (o is Polyline polyline && polyline.Length > 0.0)
                    {
                        Elements.Add(ThIfcWall.CreateWallEntity(polyline.Clone() as Polyline));
                    }
                });
            }
        }
    }
}
