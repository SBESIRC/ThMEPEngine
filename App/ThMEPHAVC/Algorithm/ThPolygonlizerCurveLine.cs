using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThCADExtension;
using NetTopologySuite.Geometries;
using Linq2Acad;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.Algorithm
{
    public class ThPolygonlizerCurveLine
    {
        private List<Curve> curves;
        private DBObjectCollection lines;
        private HashSet<Point3d> pSet;
        public ThPolygonlizerCurveLine(List<Curve> curves, DBObjectCollection lines)
        {
            this.lines = lines;
            this.curves = curves;
            pSet = new HashSet<Point3d>();
        }
        public void Draw()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var mat = Matrix3d.Displacement(new Vector3d(20000, 0, 0));

                foreach (Polygon obj in lines.Polygonize())
                {
                    foreach (var e in obj.ToDbCollection().OfType<Entity>())
                    {
                        e.TransformBy(mat);
                        acadDatabase.ModelSpace.Add(e);
                    }
                }
            }
        }
        private bool ContainsCurve(Arc a)
        {
            var sp = ThMEPHVACService.RoundPoint(a.StartPoint, 6);
            var ep = ThMEPHVACService.RoundPoint(a.EndPoint, 6);
            return !(!pSet.Contains(sp) || !pSet.Contains(ep));
        }
        private void RecorfCurve(Arc a)
        {
            var sp = ThMEPHVACService.RoundPoint(a.StartPoint, 6);
            var ep = ThMEPHVACService.RoundPoint(a.EndPoint, 6);
            pSet.Add(sp);
            pSet.Add(ep);
        }
        private void BreakArcByLine(DBObjectCollection res, Arc c, Dictionary<int, Curve> dicLine2Curve, DBObjectCollection arcLines)
        {
            // 弧和线有交点，将弧打断后添加
            var pts = new Point3dCollection();
            foreach (Line l in res)
            {
                c.IntersectWith(l, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 2)
                    throw new NotImplementedException(" Arc cross with line more than 2 points!");
                var splitCurves = c.GetSplitCurves(pts);
                pts.Clear();
                foreach (Arc sc in splitCurves)
                {
                    if (!ContainsCurve(sc))
                    {
                        RecorfCurve(sc);
                        var line = new Line(sc.StartPoint, sc.EndPoint);
                        arcLines.Add(line);
                        dicLine2Curve.Add(line.GetHashCode(), sc);
                    }
                }
            }
        }
        private void RecordArcLine(Arc c, Dictionary<int, Curve> dicLine2Curve, DBObjectCollection arcLines)
        {
            if (!ContainsCurve(c))
            {
                RecorfCurve(c);
                var l = new Line(c.StartPoint, c.EndPoint);
                arcLines.Add(l);
                dicLine2Curve.Add(l.GetHashCode(), c);
            }
        }
        public void SplitArcs()
        {
            var index = new ThCADCoreNTSSpatialIndex(lines);
            var dicLine2Curve = new Dictionary<int, Curve>();
            var arcLines = new DBObjectCollection();
            foreach (Arc c in curves)
            {
                var pl = c.TessellateArcWithArc(c.Length / 100);
                var res = index.SelectCrossingPolygon(pl.Vertices());
                if (res.Count > 0)
                    BreakArcByLine(res, c, dicLine2Curve, arcLines);
                else
                    RecordArcLine(c, dicLine2Curve, arcLines);
            }
            foreach (Line l in arcLines)
                lines.Add(l);
        }
    }
}
