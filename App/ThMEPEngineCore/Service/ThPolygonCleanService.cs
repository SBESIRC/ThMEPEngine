using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThPolygonCleanService
    {
        private double SmallAreaTolerance { get; set; } = 1.0;
        private ThPolygonalElementSimplifier Simplifier { get; set; }
        public ThPolygonCleanService(ThPolygonalElementSimplifier simplifier)
        {
            Simplifier = simplifier;
            SmallAreaTolerance = Simplifier.AREATOLERANCE;
        }
        public ThPolygonCleanService()
        {
            Simplifier = new ThPolygonalElementSimplifier();
            SmallAreaTolerance = Simplifier.AREATOLERANCE;
        }
        public DBObjectCollection Clean(DBObjectCollection polygons)
        {
            var garbages = new DBObjectCollection();
            var polygon1s = Filter(polygons);            
            var polygon2s = MakeValid(polygon1s); //解决自交的Case
            garbages = garbages.Union(polygon2s);

            var polygon3s = Simplify(polygon2s); //简化
            garbages = garbages.Union(polygon2s);

            var results = Normalize(polygon3s); //去除狭长线

            garbages = garbages.Difference(results);
            garbages = garbages.Difference(polygons);

            garbages.MDispose();
            return results;
        }
        private DBObjectCollection Simplify(DBObjectCollection polygons)
        {            
            var results = Simplifier.Simplify(polygons);
            return Filter(results);
        }

        private DBObjectCollection Normalize(DBObjectCollection polygons)
        {            
            var results = Simplifier.Normalize(polygons);
            return Filter(results);
        }

        private DBObjectCollection MakeValid(DBObjectCollection polygons)
        {
            var results = Simplifier.MakeValid(polygons);
            return Filter(results);
        }

        public DBObjectCollection Filter(DBObjectCollection objs)
        {
            var garbages = new DBObjectCollection();
            garbages = garbages.Union(objs);
            var results = RemoveDBpoints(objs); // 过滤 DBPoint
            results = results.FilterSmallArea(SmallAreaTolerance); //清除面积为零
            results = DuplicatedRemove(results); //去重
            garbages = garbages.Difference(results);
            garbages.MDispose();
            return results;
        }

        private DBObjectCollection RemoveDBpoints(DBObjectCollection objs)
        {
            return objs.OfType<Entity>().Where(e => !(e is DBPoint)).ToCollection();
        }

        private DBObjectCollection DuplicatedRemove(DBObjectCollection objs)
        {
            return ThCADCoreNTSGeometryFilter.GeometryEquality(objs);
        }        
    }
}
