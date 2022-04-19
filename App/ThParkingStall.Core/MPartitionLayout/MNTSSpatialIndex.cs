using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Index.Strtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.MPartitionLayout
{
    public class MNTSSpatialIndex : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        private HashSet<Geometry> Geometries=new HashSet<Geometry>();//去重
        public bool AllowDuplicate { get; set; }
        public bool PrecisionReduce { get; set; }
        private MNTSSpatialIndex() { }
        private PreparedGeometryFactory geometryFactory = new PreparedGeometryFactory();

        public MNTSSpatialIndex(IEnumerable<Geometry> geos, bool precisionReduce = false, bool allowDuplicate = false)
        {
            // 默认使用固定精度
            PrecisionReduce = precisionReduce;
            // 默认忽略重复图元
            AllowDuplicate = allowDuplicate;
            Update(geos, new List<Geometry>());
        }
        public MNTSSpatialIndex(IEnumerable<Polygon> geos, bool precisionReduce = false, bool allowDuplicate = false)
        {
            PrecisionReduce = precisionReduce;
            AllowDuplicate = allowDuplicate;
            Update(geos.Select(e => (Geometry)e).ToList(), new List<Geometry>());
        }
        public void Update(IEnumerable<Geometry> adds, IEnumerable<Geometry> removals)
        {
            // 添加新的对象
            foreach (var o in adds)
                if (!Geometries.Contains(o)) Geometries.Add(o);
            foreach (var o in removals)
                if (Geometries.Contains(o)) Geometries.Remove(o);
            Engine = new STRtree<Geometry>();
            foreach (var geo in Geometries) Engine.Insert(geo.EnvelopeInternal, geo);
        }
        public void Update(List<Polygon> adds, List<Polygon> removals)
        {
            // 添加新的对象
            var a = adds.Cast<Geometry>().ToList();
            var b = removals.Cast<Geometry>().ToList();
            Update(a, b);
        }
        public void Dispose()
        {
            Geometries.Clear();
            Geometries = null;
            Engine = null;
        }
        public List<Geometry> SelectCrossingGeometry(Geometry geo)
        {
            return CrossingFilter(
              Query(geo.EnvelopeInternal), geometryFactory.Create(geo)).ToList();
        }
        private IEnumerable<Geometry> CrossingFilter(List<Geometry> geos, IPreparedGeometry preparedGeometry)
        {
            return geos.Where(o => Intersects(preparedGeometry, o));
        }
        private bool Intersects(IPreparedGeometry preparedGeometry, Geometry geo)
        {
            return preparedGeometry.Intersects(geo);
        }
        public List<Geometry> Query(Envelope envelope)
        {
            var geos = new List<Geometry>();
            var results = Engine.Query(envelope).ToList();
            Geometries
                .Where(o => results.Contains(o)).ToList()
                .ForEach(o =>
                {
                    geos.Add(o);
                });
            return geos;
        }
    }
}
