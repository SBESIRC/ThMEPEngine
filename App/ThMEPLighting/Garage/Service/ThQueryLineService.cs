using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThQueryLineService
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public List<Line> Lines { get; set; }
        private ThQueryLineService(List<Line> lines)
        {
            Lines = lines.Where(o => o.Length > 0.0).ToList();
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(Lines);
        }
        public static ThQueryLineService Create(List<Line> lines)
        {
            var instance = new ThQueryLineService(lines);
            return instance;
        }
        public List<Line> Query(Point3d pt,double squreLength=1.0)
        {
            Polyline envelope = ThDrawTool.CreateSquare(pt, squreLength);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            return searchObjs
                .Cast<Line>()
                .Where(o => ThGarageLightUtils.IsLink(o, pt))
                .ToList();
        }
        public List<Line> QueryUnparallellines(Point3d startPt,Point3d endPt,double width=1.0)
        {
            var outline = ThDrawTool.ToOutline(startPt, endPt, width);
            return SpatialIndex.SelectCrossingPolygon(outline).Cast<Line>()
                .Where(o=>o.Length>0.0)
                .Where(o=>!ThGeometryTool.IsCollinearEx(startPt,endPt,o.StartPoint,o.EndPoint))
                .ToList();
        }
        public List<Line> QueryCollinearLines(Point3d startPt, Point3d endPt, double width = 1.0)
        {
            var outline = ThDrawTool.ToOutline(startPt, endPt, width);
            return SpatialIndex.SelectCrossingPolygon(outline).Cast<Line>()
                .Where(o => o.Length > 0.0)
                .Where(o => ThGeometryTool.IsCollinearEx(startPt, endPt, o.StartPoint, o.EndPoint))
                .ToList();
        }
    }
}
