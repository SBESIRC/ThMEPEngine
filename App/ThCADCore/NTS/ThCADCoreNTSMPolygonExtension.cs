using System;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSMPolygonExtension
    {
        public static MPolygon ToDbMPolygon(this Polygon polygon)
        {
            List<Curve> holes = new List<Curve>();
            var shell = polygon.Shell.ToDbPolyline();
            polygon.Holes.ForEach(o => holes.Add(o.ToDbPolyline()));
            return ThMPolygonTool.CreateMPolygon(shell, holes);
        }

        public static Geometry ToNTSGeometry(this MPolygon mPolygon)
        {
            var exteriorLoops = new IntegerCollection();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                MPolygonLoop loop = mPolygon.GetMPolygonLoopAt(i);
                if (mPolygon.GetLoopDirection(i) == LoopDirection.Exterior)
                {
                    exteriorLoops.Add(i);
                }
            }
            if (exteriorLoops.Count == 1)
            {
                return mPolygon.ToNTSPolygon(exteriorLoops[0]);
            }
            else if (exteriorLoops.Count > 0)
            {
                var ploygons = new List<Polygon>();
                exteriorLoops.Cast<int>().ForEach(i => ploygons.Add(mPolygon.ToNTSPolygon(i)));
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(ploygons.ToArray());
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static Polygon ToNTSPolygon(this MPolygon mPolygon, int loopIndex)
        {
            if (mPolygon.GetLoopDirection(loopIndex) == LoopDirection.Exterior)
            {
                var holes = new List<LinearRing>();
                foreach (int index in mPolygon.GetChildLoops(loopIndex))
                {
                    holes.Add(mPolygon.GetMPolygonLoopAt(index).ToDbPolyline().ToNTSLineString() as LinearRing);
                }
                var shell = mPolygon.GetMPolygonLoopAt(loopIndex).ToDbPolyline().ToNTSLineString() as LinearRing;
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(shell, holes.ToArray());
            }
            throw new ArgumentException();
        }

        private static Polyline ToDbPolyline(this MPolygonLoop loop)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            for (int i = 0; i < loop.Count; i++)
            {
                var bulgeVertex = loop[i];
                polyline.AddVertexAt(i, bulgeVertex.Vertex, bulgeVertex.Bulge, 0, 0);
            }
            return polyline;
        }

        public static Polygon ToNTSPolygon(this MPolygon mPolygon)
        {
            Polyline shell = null;
            List<Polyline> holes = new List<Polyline>();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                LoopDirection direction = mPolygon.GetLoopDirection(i);
                MPolygonLoop mPolygonLoop = mPolygon.GetMPolygonLoopAt(i);
                Polyline polyline = ToDbPolyline(mPolygonLoop);
                if (LoopDirection.Exterior == direction)
                {
                    shell = polyline;
                }
                else if (LoopDirection.Interior == direction)
                {
                    holes.Add(polyline);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            if (shell == null && holes.Count == 1)
            {
                return holes[0].ToNTSPolygon();
            }
            else if (shell != null && holes.Count == 0)
            {
                return shell.ToNTSPolygon();
            }
            else if (shell != null && holes.Count > 0)
            {
                List<LinearRing> holeRings = new List<LinearRing>();
                holes.ForEach(o =>
                {
                    holeRings.Add(o.ToNTSLineString() as LinearRing);
                });
                LinearRing shellLinearRing = shell.ToNTSLineString() as LinearRing;
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(shellLinearRing, holeRings.ToArray());
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static MPolygon ToDbMPolygon(this MultiPolygon multiPolygon)
        {
            var loops = multiPolygon.Geometries.Cast<Polygon>().Select(o => o.ToDbCollection()).ToList();
            return ThMPolygonTool.CreateMPolygon(loops);
        }

        public static Point3d GetMaximumInscribedCircleCenter(this MPolygon shell)
        {
            return shell.ToNTSPolygon().GetMaximumInscribedCircleCenter();
        }
    }
}
