using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Linemerge;
using NetTopologySuite.Operation.Polygonize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LineCleaner;
using ThParkingStall.Core.Tools;

namespace ThParkingStall.Core.FireZone
{
    //实现图形数据（网格）到防火分区图的数据转译
    public class FireZoneTranslator
    {
        #region 源数据
        private Polygon _InputBasement;//地库
        private LinearRing _InputShell;//地库边界
        private LinearRing[] _InputHoles;//障碍物
        private List<LineSegment> InputLines;//防火分区线
        #endregion

        #region 处理后数据
        private LinearRing Shell;
        private List<LinearRing> Holes;
        public List<LineString> Paths;//处理后的所有线
        //private STRtree<int> LineIdxEngine;
        #endregion
        private FireZoneMap _Map;
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public Serilog.Core.Logger Logger = null;
        public FireZoneMap Map
        {
            get
            {
                if (_Map == null) CreateMap();
                return _Map;
            }
        }
        public FireZoneTranslator(Polygon basement, List<LineSegment> fireLines,Serilog.Core.Logger logger = null)
        {
            Logger = logger;
            _InputBasement = basement;
            _InputShell = basement.Shell;
            _InputHoles = basement.Holes;

            var MLstr = new MultiLineString(fireLines.ToLineStrings().ToArray());
            InputLines = basement.Intersection(MLstr).Get<LineString>().ToLineSegments();//求交集


        }

        private FireZoneMap CreateMap()
        {
            Clean();
            var root = new FireZoneNode(Shell, -1);
            _Map = new FireZoneMap(root,Logger);
            foreach (var hole in Holes)
            {
                var node = new FireZoneNode(hole, 1);
                _Map.Add(node);
            }
            UpdateCrossNodes();
            Paths.ForEach(p => _Map.Add(new FireZoneEdge(p)));
            return null;
        }
        private void Clean()//线清理 + 去除cutedge
        {
            var lines = new List<LineSegment>();
            lines.AddRange(InputLines);
            lines.AddRange(_InputShell.ToLineSegments());
            lines.AddRange(_InputHoles.ToLineSegments());
            var cleaner = new LineService(lines);
            var lstrs = cleaner.Clean(false).ToLineStrings().ToHashSet();

            var polygonizer = new Polygonizer();
            foreach (var l in lstrs) polygonizer.Add(l);
            var polygons = polygonizer.GetPolygons().OfType<Polygon>().Select(p => new Polygon(p.Shell));

            var geo = new MultiLineString(polygons.Select(p => p.Shell).ToArray()).Union();
            Shell = ((Polygon)new MultiPolygon(polygons.ToArray()).Union()).Shell;
            Holes = _InputHoles.Select(h => polygons.Where(p => p.Contains(h.Centroid)).First().Shell).ToList();

            geo = geo.Difference(Shell);
            Holes.ForEach(h => geo = geo.Difference(h));
            var merger = new LineMerger();
            merger.Add(geo);
            Paths = merger.GetMergedLineStrings().OfType<LineString>().ToList();
        }
        private void UpdateCrossNodes()
        {
            var PtDic = new Dictionary<Point, List<int>>();
            for(int i = 0; i < Paths.Count; i++)
            {
                var path = Paths[i];
                var p0 = path.StartPoint;
                var p1 = path.EndPoint;
                if (PtDic.ContainsKey(p0))PtDic[p0].Add(i);
                else PtDic.Add(p0, new List<int> { i });
                if(PtDic.ContainsKey(p1))PtDic[p1].Add(i);
                else PtDic.Add(p1 , new List<int> { i });
            }
            var nodePts = PtDic.Keys.Where(k => PtDic[k].Count > 2);
            foreach (var coor in nodePts)
            {
                Map.Add(new FireZoneNode(coor));
            }
        }
    }
}
