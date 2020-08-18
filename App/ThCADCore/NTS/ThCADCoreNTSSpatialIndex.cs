using System;
using System.Linq;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Index.Strtree;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using TianHua.AutoCAD.Utility.ExtensionTools;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSSpatialIndex : IDisposable
    {
        private STRtree<IGeometry> Engine { get; set; }
        private Dictionary<IGeometry, DBObject> Geometries { get; set; }
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
            Geometries = new Dictionary<IGeometry, DBObject>();
            foreach (Entity obj in objs)
            {
                if (obj is Line line)
                {
                    var geometry = line.ToNTSLineString();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, line);
                    }
                }
                else if (obj is Polyline polyline)
                {
                    var geometry = polyline.ToNTSLineString();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, polyline);
                    }
                }
                else if (obj is Circle circle)
                {
                    var geometry = circle.ToNTSPolygon();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, circle);
                    }
                }
                else if (obj is DBText text)
                {
                    var boundary = ThPolylineExtension.CreateRectangle(text.GeometricExtents);
                    var geometry = boundary.ToNTSLineString();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, text);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            foreach (var geometry in Geometries.Keys)
            {
                AddGeometry(geometry);
            }
        }

        private void AddGeometry(IGeometry geometry)
        {
            Engine.Insert(geometry.EnvelopeInternal, geometry);
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

        public DBObjectCollection SelectAll()
        {
            var objs = new DBObjectCollection();
            Geometries.Values.ForEach(o => objs.Add(o));
            return objs;
        }

        private DBObjectCollection Query(Envelope envelope)
        {
            var objs = new DBObjectCollection();
            foreach(var geometry in Engine.Query(envelope))
            {
                if (Geometries.ContainsKey(geometry))
                {
                    objs.Add(Geometries[geometry]);
                }
            }
            return objs;
        }

        /// <summary>
        /// 最近的几个邻居
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public DBObjectCollection NearestNeighbours(Curve curve, int num)
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
            Engine.NearestNeighbour(
                geometry.EnvelopeInternal,
                geometry,
                new GeometryItemDistance(),
                num)
                .Where(o => !o.EqualsExact(geometry))
                .ForEach(o => objs.Add(Geometries[o]));
            return objs;
        }

        /// <summary>
        /// 找到最近的元素，并从集合中剔除该元素
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public DBObject NearestNeighbourRemove(Curve curve)
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

            DBObject obj = null;
            if (Engine.Count > 0)
            {
                var neighbours = Engine.NearestNeighbour(
                                geometry.EnvelopeInternal,
                                geometry,
                                new GeometryItemDistance(),
                                2);
                foreach (var neighbour in neighbours)
                {
                    // 从邻居中过滤掉自己
                    if (neighbour.EqualsExact(geometry))
                    {
                        continue;
                    }

                    if (neighbour is ILineString lineString)
                    {
                        obj =lineString.ToDbPolyline();
                        Engine.Remove(neighbour.EnvelopeInternal, neighbour);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            
            return obj;
        }
    }
}
