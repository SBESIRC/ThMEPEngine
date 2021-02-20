using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThShearWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override List<ThRawIfcBuildingElementData> Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var shearWallDbExtension = new ThStructureShearWallDbExtension(database))
            {
                shearWallDbExtension.BuildElementCurves();
                return shearWallDbExtension.ShearWallCurves.Select(o => new ThRawIfcBuildingElementData()
                {
                    Geometry = o,
                }).ToList();
            }
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            List<Entity> ents = new List<Entity>();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                {
                    ents.Add(filterObj as Entity);
                }
            }
            else
            {
                ents = objs.Cast<Entity>().ToList();
            }
            ents.ForEach(o =>
            {
                if (o is Polyline polyline && polyline.Area > 0.0)
                {
                    var bufferObjs = polyline.Buffer(ThMEPEngineCoreCommon.ShearWallBufferDistance);
                    if (bufferObjs.Count == 1)
                    {
                        var outline = bufferObjs[0] as Polyline;
                        Elements.Add(ThIfcWall.Create(outline));
                    }
                }
            });
        }
    }
}
