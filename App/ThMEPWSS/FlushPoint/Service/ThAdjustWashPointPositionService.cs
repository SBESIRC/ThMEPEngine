using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThAdjustWashPointPositionService
    {
        public static void Adjust(List<Point3d> washPoints, List<Polyline> columns)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
            for (int i = 0; i < washPoints.Count; i++)
            {
                var pt = washPoints[i];
                var square = ThDrawTool.CreateSquare(pt, 5.0);
                var objs = spatialIndex.SelectCrossingPolygon(square);
                if (objs.Count > 0)
                {
                    var column = objs.Cast<Polyline>().OrderBy(o => o.Distance(pt)).First();
                    washPoints[i] = Adjust(column, pt); 
                }
            }
        }
        private static Point3d Adjust(Polyline column,Point3d pt)
        {
            for (int i = 0; i < column.NumberOfVertices; i++)
            {
               var lineSegment = column.GetLineSegmentAt(i);
                if (lineSegment.IsOn(pt, new Tolerance(1.0, 1.0)))
                {
                    return ThGeometryTool.GetMidPt(lineSegment.StartPoint, lineSegment.EndPoint);
                }
            }
            return pt;
        }
    }
}
