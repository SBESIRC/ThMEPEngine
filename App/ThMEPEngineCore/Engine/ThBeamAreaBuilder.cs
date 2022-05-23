using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamAreaBuilder
    {
        // 用于过滤面积很小的Polygon
        private const double SmallAreaTolerance = 1.0;
        private const double BufferDistance = 300.0;            
        private const double PolylineClosedTolerance = 5.0;        
        private DBObjectCollection Columns { get; set; } //仅支持Polyline
        private DBObjectCollection Beams  { get; set; } //仅支持Polyline,Line
        public DBObjectCollection Results { get; private set; }
        public ThBeamAreaBuilder(DBObjectCollection beams,DBObjectCollection columns)
        {
            Beams = beams;
            Columns = columns;
            Results = new DBObjectCollection();
        }
        public void Build()
        {
            if(Beams.Count==0)
            {
                return;
            }
            // 移动到近原点处
            var transformer = new ThMEPOriginTransformer(Beams);
            transformer.Transform(Beams);
            transformer.Transform(Columns);

            // 扩大
            var garbage = new DBObjectCollection();
            var polygons1 = new DBObjectCollection();
            var newColumns = BufferPolygons(Columns, BufferDistance);
            var newBeams = BufferBeams(Beams, BufferDistance);
            polygons1 = polygons1.Union(newColumns);
            polygons1 = polygons1.Union(newBeams);
            garbage = garbage.Union(polygons1);

            // 对框线处理
            var polygons2 = Clean(polygons1);
            garbage = garbage.Union(polygons2);

            var polygons3 = UnionPolygons(RepeatedRemoved(polygons2));
            garbage = garbage.Union(polygons3);

            var polygons4 = BufferPolygons(polygons3, -1.0 * BufferDistance);
            garbage = garbage.Union(polygons4);

            this.Results = Clean(polygons4);
            garbage.Difference(this.Results);

            // 释放资源
            garbage.MDispose();

            // 还原            
            transformer.Reset(Beams);
            transformer.Reset(Columns);
            transformer.Reset(this.Results);
        }

        private DBObjectCollection UnionPolygons(DBObjectCollection polygons)
        {
            return polygons.UnionPolygons(true);
        }

        private DBObjectCollection RepeatedRemoved(DBObjectCollection objs)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.SelectAll();
        }

        private DBObjectCollection Clean(DBObjectCollection polygons)
        {
            var garbage = new DBObjectCollection();
            var simplifier = new ThPolygonalElementSimplifier();
            var objs = simplifier.Normalize(polygons.FilterSmallArea(SmallAreaTolerance));
            garbage = garbage.Union(objs); // 放入
            objs = simplifier.MakeValid(objs.FilterSmallArea(SmallAreaTolerance));
            garbage = garbage.Union(objs); // 放入
            var results = simplifier.Simplify(objs.FilterSmallArea(SmallAreaTolerance));
            garbage = garbage.Union(results); // 放入
            results = results.FilterSmallArea(SmallAreaTolerance);
            garbage = garbage.Difference(results);
            garbage.MDispose(); // 释放
            return results;
        }
        private DBObjectCollection BufferPolygons(DBObjectCollection polygons,double distance)
        {
            var results = new DBObjectCollection();
            polygons.OfType<Polyline>().ForEach(p=>
            {
                var objs = p.Buffer(distance);
                results = results.Union(objs);
            });
            polygons.OfType<MPolygon>().ForEach(p =>
            {
                var objs = p.Buffer(distance,true);
                results = results.Union(objs);
            });
            return results.FilterSmallArea(SmallAreaTolerance);
        }
        private DBObjectCollection BufferBeams(DBObjectCollection rawBeams,double distance)
        {
            var results = new DBObjectCollection();
            // 只处理Line,Polyline
            rawBeams
                .OfType<Polyline>()
                .Where(o=>o.Length>1.0)
                .ForEach(p =>
            {
                if(ThMEPFrameService.IsClosed(p,PolylineClosedTolerance))
                {
                    var clone = p.Clone() as Polyline;
                    clone.Closed = true;
                    var objs = new DBObjectCollection() { clone };
                    results = results.Union(objs.Buffer(distance));
                    clone.Dispose();
                }
                else
                {
                    var objs = new DBObjectCollection() { p };
                    results = results.Union(objs.Buffer(distance));
                }
            });

            rawBeams
                .OfType<Line>()
                .Where(o => o.Length > 1.0)
                .ForEach(p =>
                {
                    var objs = new DBObjectCollection() { p };
                    results = results.Union(objs.Buffer(distance));
                });
            return results.FilterSmallArea(SmallAreaTolerance);
        }
    }
}
