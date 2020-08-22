using ThCADCore.NTS;
using NetTopologySuite.Simplify;
using NetTopologySuite.Operation.Union;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Linemerge;

namespace ThCADCore.Geometry
{
    public static class ThCADCorePolylineExtension
    {
        public static DBObjectCollection Preprocess(this Polyline polyline)
        {
            // 剔除重复点（在一定公差范围内）
            // 鉴于主要的使用场景是建筑底图，选择1毫米作为公差
            var result = TopologyPreservingSimplifier.Simplify(polyline.ToNTSLineString(), 1.0);

            // 合并线段
            var merger = new LineMerger();
            merger.Add(UnaryUnionOp.Union(result));

            // 返回结果
            return merger.GetMergedLineStrings().ToDBCollection();
        }
    }
}
