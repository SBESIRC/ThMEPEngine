using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThColumnRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override List<ThRawIfcBuildingElementData> Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var columnDbExtension = new ThStructureColumnDbExtension(database))
            {
                columnDbExtension.BuildElementCurves();
                return columnDbExtension.ColumnCurves.Select(o => new ThRawIfcBuildingElementData()
                {
                    Geometry=o,
                }).ToList();                
            }
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            List<Curve> curves = new List<Curve>();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex columnCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(polygon);
                foreach (var filterObj in columnCurveSpatialIndex.SelectCrossingPolygon(pline))
                {
                    curves.Add(filterObj as Curve);
                }
            }
            else
            {
                curves = objs.Cast<Curve>().ToList();
            }
            curves.ToCollection().UnionPolygons().Cast<Curve>()
                .ForEach(o =>
                {
                    if (o is Polyline polyline && polyline.Area > 0.0)
                    {
                        var bufferObjs = polyline.Buffer(ThMEPEngineCoreCommon.ColumnBufferDistance);
                        if (bufferObjs.Count == 1)
                        {
                            var outline = bufferObjs[0] as Polyline;
                            Elements.Add(ThIfcColumn.Create(outline));
                        }
                    }
                });
        }
    }
}
