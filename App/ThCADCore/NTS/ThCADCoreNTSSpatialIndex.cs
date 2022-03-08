using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Geometries.Prepared;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    /// <summary>
    /// 空间索引DB图元（去除空间重合的DB图元）
    /// </summary>
    public class ThCADCoreNTSSpatialIndex : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        private Dictionary<DBObject, Geometry> Geometries { get; set; }
        private Lookup<Geometry, DBObject> GeometryLookup { get; set; }
        public bool AllowDuplicate { get; set; }
        public bool PrecisionReduce { get; set; }
        private ThCADCoreNTSSpatialIndex() { }
        public ThCADCoreNTSSpatialIndex(DBObjectCollection objs)
        {
            Reset(objs);
            // 默认使用固定精度
            PrecisionReduce = true;
            // 默认忽略重复图元
            AllowDuplicate = false;
        }
        public void Dispose()
        {
            Geometries.Clear();
            Geometries = null;
            GeometryLookup = null;
            Engine = null;
        }
        private DBObjectCollection CrossingFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Intersects(preparedGeometry, o)).ToCollection();
        }

        private DBObjectCollection FenceFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Intersects(preparedGeometry, o)).ToCollection();
        }

        private DBObjectCollection WindowFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Contains(preparedGeometry, o)).ToCollection();
        }

        private bool Contains(IPreparedGeometry preparedGeometry, Entity entity)
        {
            return preparedGeometry.Contains(ToNTSGeometry(entity));
        }

        public bool Intersects(Entity entity, bool precisely = false)
        {
            var geometry = ToNTSPolygonalGeometry(entity);
            var queriedObjs = Query(geometry.EnvelopeInternal);

            if (precisely == false)
                return queriedObjs.Count > 0;

            var preparedGeometry = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry);
            var hasIntersection = queriedObjs.Cast<Entity>().Any(o => Intersects(preparedGeometry, o));
            return hasIntersection;
        }

        private bool Intersects(IPreparedGeometry preparedGeometry, Entity entity)
        {
            return preparedGeometry.Intersects(ToNTSGeometry(entity));
        }

        private Geometry ToNTSGeometry(DBObject obj)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision(PrecisionReduce))
            {
                if (obj is DBPoint dbPoint)
                {
                    return dbPoint.ToNTSPoint();
                }
                else if (obj is Line line)
                {
                    return line.ToNTSLineString();
                }
                else if (obj is Polyline polyline)
                {
                    return polyline.ToNTSLineString();
                }
                else if (obj is Arc arc)
                {
                    return arc.ToNTSGeometry();
                }
                else if (obj is Circle circle)
                {
                    return circle.ToNTSGeometry();
                }
                else if (obj is MPolygon mPolygon)
                {
                    return mPolygon.ToNTSPolygon();
                }
                else if (obj is AlignedDimension dimension)
                {
                    var distanceLine = new Line(dimension.XLine1Point, dimension.XLine2Point);
                    return distanceLine.ToNTSLineString();
                }
                else if (obj is Hatch hatch)
                {
                    return hatch.GeometricExtents.ToNTSPolygon();
                }
                else if (obj is Ellipse ellipse)
                {
                    return ellipse.ToNTSPolygon();
                }
                else if (obj is Entity entity)
                {
                    try
                    {
                        return entity.GeometricExtents.ToNTSPolygon();
                    }
                    catch
                    {
                        // 若异常抛出，则返回一个“空”的Polygon
                        return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private Polygon ToNTSPolygonalGeometry(DBObject obj)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision(PrecisionReduce))
            {
                if (obj is Polyline poly)
                {
                    return poly.ToNTSPolygon();
                }
                else if (obj is MPolygon mPolygon)
                {
                    return mPolygon.ToNTSPolygon();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// 更新索引
        /// </summary>
        /// <param name="adds"></param>
        /// <param name="removals"></param>
        public void Update(DBObjectCollection adds, DBObjectCollection removals)
        {
            // 添加新的对象
            adds.OfType<DBObject>().ForEach(o =>
            {
                if (!Geometries.ContainsKey(o))
                {
                    Geometries[o] = ToNTSGeometry(o);
                }
            });

            // 移除删除对象
            removals.OfType<DBObject>().ForEach(o =>
            {
                if (Geometries.ContainsKey(o))
                {
                    Geometries.Remove(o);
                }
            });

            // 创建新的索引
            Engine = new STRtree<Geometry>();
            GeometryLookup = (Lookup<Geometry, DBObject>)Geometries.ToLookup(p => p.Value, p => p.Key);
            GeometryLookup.Select(o => o.Key).ForEach(o => Engine.Insert(o.EnvelopeInternal, o));
        }

        /// <summary>
        /// 重置索引
        /// </summary>
        public void Reset(DBObjectCollection objs)
        {
            Geometries = new Dictionary<DBObject, Geometry>();
            Update(objs, new DBObjectCollection());
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingPolygon(Entity entity)
        {
            var geometry = ToNTSPolygonalGeometry(entity);
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public DBObjectCollection SelectCrossingPolygon(Point3dCollection polygon)
        {
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(polygon);
            return SelectCrossingPolygon(pline);
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingWindow(Point3d pt1, Point3d pt2)
        {
            var extents = new Extents3d(pt1, pt2);
            return SelectCrossingPolygon(extents.ToRectangle());
        }

        /// <summary>
        /// Window selection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public DBObjectCollection SelectWindowPolygon(Entity entity)
        {
            var geometry = ToNTSPolygonalGeometry(entity);
            return WindowFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public DBObjectCollection SelectFence(Entity entity)
        {
            var geometry = ToNTSGeometry(entity);
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public DBObjectCollection SelectAll()
        {
            var objs = new DBObjectCollection();
            GeometryLookup.ForEach(o =>
            {
                if (AllowDuplicate)
                {
                    o.ForEach(e => objs.Add(e));
                }
                else
                {
                    objs.Add(o.First());
                }
            });
            return objs;
        }

        public void AddTag(DBObject obj, object tag)
        {
            if (Geometries.ContainsKey(obj))
            {
                Geometries[obj].UserData = tag;
            }
        }

        public object Tag(DBObject obj)
        {
            if (Geometries.ContainsKey(obj))
            {
                return Geometries[obj].UserData;
            }
            return null;
        }

        public DBObjectCollection Query(Envelope envelope)
        {
            var objs = new DBObjectCollection();
            var results = Engine.Query(envelope).ToList();
            GeometryLookup
                .Where(o => results.Contains(o.Key))
                .ForEach(o =>
                {
                    if (AllowDuplicate)
                    {
                        o.ForEach(e => objs.Add(e));
                    }
                    else
                    {
                        objs.Add(o.First());
                    }
                });
            return objs;
        }

        /// <summary>
        /// 最近的几个邻居
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public DBObjectCollection NearestNeighbours(Curve curve, int num)
        {
            return NearestGeometries(ToNTSGeometry(curve), num);
        }

        /// <summary>
        /// 最近的几个邻居
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public DBObjectCollection NearestNeighbours(Point3d point, int num)
        {
            return NearestGeometries(point.ToNTSPoint(), num);
        }

        private DBObjectCollection NearestGeometries(Geometry geometry, int k)
        {
            var neighbours = Engine.NearestNeighbour(
                geometry.EnvelopeInternal,
                geometry,
                new GeometryItemDistance(),
                k)
                .Where(o => !o.EqualsExact(geometry))
                .ToList();
            var objs = new DBObjectCollection();
            GeometryLookup
                .Where(o => neighbours.Contains(o.Key))
                .ForEach(o =>
                {
                    if (AllowDuplicate)
                    {
                        o.ForEach(e => objs.Add(e));
                    }
                    else
                    {
                        objs.Add(o.First());
                    }
                });
            return objs;
        }
    }
}
