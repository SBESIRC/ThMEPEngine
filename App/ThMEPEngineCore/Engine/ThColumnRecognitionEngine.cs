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
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var columnDbExtension = new ThStructureColumnDbExtension(database))
            {
                columnDbExtension.BuildElementCurves();
                List<Curve> curves = new List<Curve>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    columnDbExtension.ColumnCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex columnCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
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
                    curves = columnDbExtension.ColumnCurves;
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
}
