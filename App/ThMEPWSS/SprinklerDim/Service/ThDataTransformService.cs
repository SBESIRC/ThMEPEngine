using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThDataTransformService
    {

        public static ThCADCoreNTSSpatialIndex GenerateSpatialIndex(List<Polyline> reference)
        {
            List<Line> referenceLines = Change(reference);
            return GenerateSpatialIndex(referenceLines);
        }

        public static ThCADCoreNTSSpatialIndex GenerateSpatialIndex(List<Line> reference)
        {
            // 把参考物拆解为线,做成空间索引
            DBObjectCollection referenceLines = new DBObjectCollection();
            foreach (Line r in reference)
            {
                referenceLines.Add(r);
            }
            ThCADCoreNTSSpatialIndex linesSI = new ThCADCoreNTSSpatialIndex(referenceLines);
            return linesSI;
        }

        public static List<Polyline> GetPolylines(DBObjectCollection dboc)
        {
            List<Polyline> polylines = new List<Polyline>();
            foreach (Polyline p in dboc.OfType<Polyline>())
            {
                polylines.Add(p);
            }

            foreach (Line l in dboc.OfType<Line>())
            {
                Polyline p = new Polyline();
                p.AddVertexAt(0, l.StartPoint.ToPoint2d(), 0, 0, 0);
                p.AddVertexAt(1, l.EndPoint.ToPoint2d(), 0, 0, 0);
                polylines.Add(p);
            }

            return polylines;
        }

        public static List<Point3d> GetPoints(List<Point3d> pts, List<int> idxs)
        {
            List<Point3d> line = new List<Point3d>();

            foreach (int i in idxs)
            {
                line.Add(pts[i]);
            }

            return line;
        }

        public static List<Polyline> Change(List<Line> lines)
        {
            List<Polyline> polylines = new List<Polyline>();
            foreach (var l in lines)
            {
                Polyline p = new Polyline();
                p.AddVertexAt(0, l.StartPoint.ToPoint2d(), 0, 0, 0);
                p.AddVertexAt(1, l.EndPoint.ToPoint2d(), 0, 0, 0);
                polylines.Add(p);
            }

            return polylines;
        }

        public static List<Line> Change(List<Polyline> polylines)
        {
            List<Line> lines = new List<Line>();

            foreach (Polyline p in polylines)
            {
                for (int i = 0; i < p.NumberOfVertices-1; i++)
                {
                    lines.Add(new Line(p.GetPoint3dAt(i), p.GetPoint3dAt(i + 1)));
                }
                if(p.Closed)
                    lines.Add(new Line(p.GetPoint3dAt(0), p.GetPoint3dAt(p.NumberOfVertices-1)));
            }

            return lines;
        }

        public static List<Line> Change(MPolygon mPolygon)
        {
            List<Line> lines = new List<Line>();

            lines.AddRange(Change(mPolygon.Shell()));
            foreach(Polyline hole in mPolygon.Holes())
            {
                lines.AddRange(Change(hole));
            }

            return lines;
        }

        public static List<Line> Change(Polyline p)
        {
            List<Line> lines = new List<Line>();

            for (int i = 0; i < p.NumberOfVertices - 1; i++)
            {
                lines.Add(new Line(p.GetPoint3dAt(i), p.GetPoint3dAt(i + 1)));
            }
            if (p.Closed)
                lines.Add(new Line(p.GetPoint3dAt(0), p.GetPoint3dAt(p.NumberOfVertices - 1)));

            return lines;
        }

    }
}
