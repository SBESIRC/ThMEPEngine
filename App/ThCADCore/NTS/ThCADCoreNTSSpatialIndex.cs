using System;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Index.Strtree;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSSpatialIndex : IDisposable
    {
        private STRtree<IGeometry> Engine { get; set; }
        public ThCADCoreNTSSpatialIndex(DBObjectCollection objs)
        {
            Engine = new STRtree<IGeometry>();
            Initialize(objs);
        }

        public void Dispose()
        {
            //
        }

        private void Initialize(DBObjectCollection objs)
        {
            foreach(Curve obj in objs)
            {
                if (obj is Line line)
                {
                    AddGeometry(line.ToNTSLineString());
                }
                else if (obj is Polyline polyline)
                {
                    AddGeometry(polyline.ToNTSLineString());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private void AddGeometry(IGeometry geometry)
        {
            Engine.Insert(geometry.EnvelopeInternal, geometry);
        }

        private void RemoveGeometry(IGeometry geometry)
        {
            Engine.Remove(geometry.EnvelopeInternal, geometry);
        }

        public DBObjectCollection SelectCrossingWindow(Point3d pt1, Point3d pt2)
        {
            var extents = new Extents3d(pt1, pt2);
            return Query(extents.ToEnvelope());
        }

        public DBObjectCollection SelectFence(Curve curve)
        {
            return Query(curve.GeometricExtents.ToEnvelope());
        }

        private DBObjectCollection Query(Envelope envelope)
        {
            var objs = new DBObjectCollection();
            foreach(var geometry in Engine.Query(envelope))
            {
                if (geometry is ILineString lineString)
                {
                    objs.Add(lineString.ToDbPolyline());
                }
                else if (geometry is ILinearRing linearRing)
                {
                    objs.Add(linearRing.ToDbPolyline());
                }
                else if (geometry is IPolygon polygon)
                {
                    objs.Add(polygon.Shell.ToDbPolyline());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }

        /// <summary>
        /// 最近的邻居
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public Curve NearestNeighbour(Curve curve)
        {
            IGeometry geometry = null;
            if (curve is Line line)
            {
                geometry = line.ToNTSLineString();
            }
            else if (curve is Polyline polyline)
            {
                geometry = polyline.ToNTSLineString();
            }
            else
            {
                throw new NotSupportedException();
            }

            var objs = new DBObjectCollection();
            var neighbours = Engine.NearestNeighbour(
                geometry.EnvelopeInternal, 
                geometry, 
                new GeometryItemDistance(), 
                2);
            foreach(var neighbour in neighbours)
            {
                // 从邻居中过滤掉自己
                if (neighbour.EqualsExact(geometry))
                {
                    continue;
                }

                if (neighbour is ILineString lineString)
                {
                    objs.Add(lineString.ToDbPolyline());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs[0] as Curve;
        }
    }
}
