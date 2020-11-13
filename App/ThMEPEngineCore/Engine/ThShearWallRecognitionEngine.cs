using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThShearWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var shearWallDbExtension = new ThStructureShearWallDbExtension(database))
            {
                shearWallDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    shearWallDbExtension.ShearWallCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = shearWallDbExtension.ShearWallCurves;
                }
                ents.ForEach(o =>
                {
                    if (o is Polyline polyline && polyline.Area > 0.0)
                    {
                        var bufferObjs = polyline.Buffer(ThMEPEngineCoreCommon.ShearWallBufferDistance);
                        if (bufferObjs.Count == 1)
                        {
                            var outline = bufferObjs[0] as Polyline;
                            Elements.Add(ThIfcWall.CreateWallEntity(outline));
                        }
                    }
                    else if(o is MPolygon mPolygon)
                    {
                        Elements.Add(ThIfcWall.CreateWallEntity(mPolygon));
                    }
                });
            }
        }
    }
}
