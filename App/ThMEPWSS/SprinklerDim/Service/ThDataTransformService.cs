using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThDataTransformService
    {

        public static ThCADCoreNTSSpatialIndex GenerateSpatialIndex(List<Polyline> reference)
        {
            // 把参考物拆解为线,做成空间索引
            DBObjectCollection referenceLines = new DBObjectCollection();
            foreach (Polyline r in reference)
            {
                for (int i = 0; i < r.NumberOfVertices; i++)
                {
                    referenceLines.Add(new Line(r.GetPoint3dAt(i), r.GetPoint3dAt((i + 1) % r.NumberOfVertices)));
                }

            }
            ThCADCoreNTSSpatialIndex linesSI = new ThCADCoreNTSSpatialIndex(referenceLines);
            return linesSI;
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

        public static List<Polyline> Change(List<Line> axisCurves)
        {
            List<Polyline> newAxisCurves = new List<Polyline>();
            foreach (var l in axisCurves)
            {
                Polyline p = new Polyline();
                p.AddVertexAt(0, l.StartPoint.ToPoint2d(), 0, 0, 0);
                p.AddVertexAt(1, l.EndPoint.ToPoint2d(), 0, 0, 0);
                newAxisCurves.Add(p);
            }

            return newAxisCurves;
        }

    }
}
