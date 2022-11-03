using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Index.Strtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.LaneDeformation
{
    public class BNSpatialIndex : IDisposable
    {
        private STRtree<BlockNode> Engine { get; set; }
        private HashSet<BlockNode> Nodes = new HashSet<BlockNode>();//去重
        public bool AllowDuplicate { get; set; }
        public bool PrecisionReduce { get; set; }
        private BNSpatialIndex() { }
        private PreparedGeometryFactory geometryFactory = new PreparedGeometryFactory();

        public BNSpatialIndex(IEnumerable<BlockNode> nodes, bool precisionReduce = false, bool allowDuplicate = false)
        {
            // 默认使用固定精度
            PrecisionReduce = precisionReduce;
            // 默认忽略重复图元
            AllowDuplicate = allowDuplicate;
            Update(nodes, new List<BlockNode>());
        }
        public void Update(IEnumerable<BlockNode> adds, IEnumerable<BlockNode> removals)
        {
            // 添加新的对象
            foreach (var o in adds)
                if (!Nodes.Contains(o)) Nodes.Add(o);
            foreach (var o in removals)
                if (Nodes.Contains(o)) Nodes.Remove(o);
            Engine = new STRtree<BlockNode>();
            foreach (var no in Nodes) Engine.Insert(no.Obb.EnvelopeInternal, no);
        }
        public List<BlockNode> SelectAll()
        {
            return Nodes.ToList();
        }
        public void Dispose()
        {
            Nodes.Clear();
            Nodes = null;
            Engine = null;
        }
        public List<BlockNode> SelectCrossingGeometry(Geometry geo)
        {
            return CrossingFilter(
              Query(geo.EnvelopeInternal), geometryFactory.Create(geo)).ToList();
        }
        public List<BlockNode> SelectNOTCrossingGeometry(Geometry geo)
        {
            return Nodes.Except(SelectCrossingGeometry(geo)).ToList();
        }
        private IEnumerable<BlockNode> CrossingFilter(List<BlockNode> nodes, IPreparedGeometry preparedGeometry)
        {
            return nodes.Where(o => Intersects(preparedGeometry, o.Obb));
        }
        private bool Intersects(IPreparedGeometry preparedGeometry, Geometry geo)
        {
            return preparedGeometry.Intersects(geo);
        }
        public List<BlockNode> Query(Envelope envelope)
        {
            var nodes = new List<BlockNode>();
            var results = Engine.Query(envelope).ToList();
            Nodes
                .Where(o => results.Contains(o)).ToList()
                .ForEach(o =>
                {
                    nodes.Add(o);
                });
            return nodes;
        }
    }
}
