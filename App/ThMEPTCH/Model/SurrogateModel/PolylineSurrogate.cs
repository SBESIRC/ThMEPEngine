using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using NFox.Cad;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPTCH.Model.SurrogateModel
{
    [ProtoContract]
    public struct PolylineSurrogate
    {
        public PolylineSurrogate(List<Point3DCollectionSurrogate> pts, bool closed) : this()
        {
            this.Points = pts;
            this.IsClosed = closed;
        }

        [ProtoMember(1)]
        public List<Point3DCollectionSurrogate> Points { get; set; }

        [ProtoMember(2)]
        public bool IsClosed { get; set; }

        public static implicit operator Polyline(PolylineSurrogate surrogate)
        {
            if (surrogate.Points.Count == 0)
            {
                return null;
            }
            Polyline p = new Polyline();
            for (int i = 0; i < surrogate.Points.Count; i++)
            {
                if (surrogate.Points[i].Points.Count == 1)
                {
                    p.AddVertexAt(i, new Point2d(surrogate.Points[i].Points[0].X, surrogate.Points[i].Points[0].Y), 0, 0, 0);
                }
                else
                {
                    var arc = ThArcExtension.CreateArcWith3PointsOrder(new Point3d(surrogate.Points[i].Points[0].X, surrogate.Points[i].Points[0].Y, 0), new Point3d(surrogate.Points[i].Points[1].X, surrogate.Points[i].Points[1].Y, 0), new Point3d(surrogate.Points[i + 1].Points[0].X, surrogate.Points[i + 1].Points[0].Y, 0));
                    p.AddVertexAt(i, new Point2d(surrogate.Points[i].Points[0].X, surrogate.Points[i].Points[0].Y), arc.GetArcBulge(arc.StartPoint), 0, 0);
                }
            }
            p.Closed = surrogate.IsClosed;
            return p;
        }

        public static implicit operator PolylineSurrogate(Polyline polyline)
        {
            if (polyline.IsNull())
                return new PolylineSurrogate(new List<Point3DCollectionSurrogate>(), false);
            var pts = new List<Point3DCollectionSurrogate>();
            var segments = new PolylineSegmentCollection(polyline);
            for (int k = 0; k < segments.Count(); k++)
            {
                var segment = segments[k];
                if (segment.IsLinear)
                {
                    // 直线段
                    var line = segment.ToLineSegment();
                    pts.Add(new Point3DCollectionSurrogate(new List<Point3DSurrogate>() { new Point3DSurrogate(line.StartPoint.X, line.StartPoint.Y, 0) }));

                    if (!polyline.Closed && k == segments.Count() - 1)
                    {
                        pts.Add(new Point3DCollectionSurrogate(new List<Point3DSurrogate> { new Point3DSurrogate(line.EndPoint.X, line.EndPoint.Y, 0) }));
                    }
                }
                else
                {
                    // 圆弧段
                    var arc = segment.ToCircularArc();
                    var points = new Point3DCollectionSurrogate(new List<Point3DSurrogate>() { new Point3DSurrogate(arc.StartPoint.X, arc.StartPoint.Y, 0) });

                    // 圆弧中点
                    double p1 = arc.GetParameterOf(arc.StartPoint);
                    double p2 = arc.GetParameterOf(arc.EndPoint);
                    var midPoint = arc.EvaluatePoint(p1 + (p2 - p1) / 2.0);
                    points.Points.Add(new Point3DSurrogate(midPoint.X, midPoint.Y, 0));

                    pts.Add(points);

                    if (!polyline.Closed && k == segments.Count() - 1)
                    {
                        pts.Add(new Point3DCollectionSurrogate(new List<Point3DSurrogate> { new Point3DSurrogate(arc.EndPoint.X, arc.EndPoint.Y, 0) }));
                    }
                }
            }
            return new PolylineSurrogate(pts, polyline.Closed);
        }
    }
}
