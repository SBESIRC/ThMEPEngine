using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.Engine
{
    public class ThArchitectureWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        private const double AbnormalBufferDis = 30.0;
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (ThCADCoreNTSFixedPrecision fixedPrecision=new ThCADCoreNTSFixedPrecision())
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
                    if (o is Polyline polyline && polyline.Area > 0.0)
                    {
                        var handleObjs = HandleAbnormalEdge(polyline);
                        handleObjs.ForEach(m =>
                        {
                            var bufferObjs = m.Buffer(ThMEPEngineCoreCommon.ShearWallBufferDistance);
                            if (bufferObjs.Count > 0)
                            {
                                var first = bufferObjs.Cast<Polyline>().OrderByDescending(n => n.Area).First();
                                Elements.Add(ThIfcWall.Create(first));
                            }
                        });
                    }
                });
            }
        }
        private List<Polyline> HandleAbnormalEdge(Polyline origin)
        {
            List<Polyline> results = new List<Polyline>();
            var polyline = origin.ToNTSLineString().ToDbPolyline();
            var innerObjs = polyline.Buffer(-AbnormalBufferDis);
            if(innerObjs.Count==0)
            {
                return results;
            }
            var inner = innerObjs[0] as Polyline;
            var outerObjs = inner.Buffer(AbnormalBufferDis);
            if (outerObjs.Count == 0)
            {
                return results;
            }
            results.Add(outerObjs[0] as Polyline);
            return results;
        }
    }
}
