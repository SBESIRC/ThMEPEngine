using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
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
        public List<Line> Query(Point3d pt,double squreLength=1.0,bool isLink=true)
        {
            Polyline envelope = ThDrawTool.CreateSquare(pt, squreLength+5.0);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            if(isLink)
            {
               return searchObjs
                .Cast<Line>()
                .Where(o => ThGarageLightUtils.IsLink(o, pt,5.0))
                .ToList();
            }
            else
            {
                return searchObjs.Cast<Line>().ToList();
            }
        }
        public List<Line> Query(Point3d pt, double envelop)
        {
            if(envelop<=1e-6)
            {
                envelop = 1.0;
            }
            Polyline envelope = ThDrawTool.CreateSquare(pt, envelop);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            return searchObjs.Cast<Line>().ToList();
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
        public List<Point3d> QueryZeroDegreePorts(double envelop)
        {
            var results = new List<Point3d>();
            Lines.ForEach(l =>
            {
                var startLinks = Query(l.StartPoint, envelop);
                startLinks.Remove(l);
                if(startLinks.Count==0)
                {
                    results.Add(l.StartPoint);
                }

                var endLinks = Query(l.EndPoint, envelop);
                endLinks.Remove(l);
                if (endLinks.Count == 0)
                {
                    results.Add(l.EndPoint);
                }
            });
            return results;
        }
    }
}
