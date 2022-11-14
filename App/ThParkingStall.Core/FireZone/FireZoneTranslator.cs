using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Linemerge;
using NetTopologySuite.Operation.Polygonize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private STRtree<LinearRing> HoleEngine = new STRtree<LinearRing>();
        public List<LineString> Paths;//处理后的所有线
        private STRtree<LineString> PathEngine = new STRtree<LineString>();
        //private STRtree<int> LineIdxEngine;
        #endregion
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public Serilog.Core.Logger Logger = null;
        private Stopwatch _stopWatch = new Stopwatch();
        private double t_pre;

        public FireZoneTranslator(Polygon basement, List<LineSegment> fireLines,Serilog.Core.Logger logger = null)
        {
            _stopWatch.Start();
            t_pre = _stopWatch.Elapsed.TotalSeconds;
            Logger = logger;
            _InputBasement = basement;
            _InputShell = basement.Shell;
            _InputHoles = basement.Holes;

            var MLstr = new MultiLineString(fireLines.ToLineStrings().ToArray());
            InputLines = basement.Intersection(MLstr).Get<LineString>().ToLineSegments();//求交集
            Clean();
        }
        public FireZoneMap CreateMap(Polygon newShell = null)//基于输入边界创建map
        {
            t_pre = _stopWatch.Elapsed.TotalSeconds;
            LinearRing shell;
            List<LinearRing> holes;
            List<LineString> paths;
            if (newShell == null)
            {
                shell = Shell;
                holes = Holes;
                paths = Paths;
            }
            else
            {
                shell = newShell.Shell;
                holes = HoleEngine.Query(shell.EnvelopeInternal).Where(h => newShell.Contains(h)).ToList();
                paths = PathEngine.Query(shell.EnvelopeInternal).Where(p =>newShell.Contains(p)).ToList();
            }
            var root = new FireZoneNode(shell, -1);
            var map = new FireZoneMap(root,Logger);
            
            foreach (var hole in holes)
            {
                var node = new FireZoneNode(hole, 1);
                map.Add(node);
            }

            var PtDic = new Dictionary<Point, List<int>>();
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                var p0 = path.StartPoint;
                var p1 = path.EndPoint;
                if (PtDic.ContainsKey(p0)) PtDic[p0].Add(i);
                else PtDic.Add(p0, new List<int> { i });
                if (PtDic.ContainsKey(p1)) PtDic[p1].Add(i);
                else PtDic.Add(p1, new List<int> { i });
            }
            var nodePts = PtDic.Keys.Where(k => PtDic[k].Count > 2);
            foreach (var coor in nodePts)
            {
                map.Add(new FireZoneNode(coor));
            }
            paths.ForEach(p => map.Add(new FireZoneEdge(p)));
            Logger.Information($"添加节点时:{_stopWatch.Elapsed.TotalSeconds - t_pre}");
            return map;
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
            Holes.ForEach(h => HoleEngine.Insert(h.EnvelopeInternal, h));
            geo = geo.Difference(Shell);
            Holes.ForEach(h => geo = geo.Difference(h));
            var merger = new LineMerger();
            merger.Add(geo);
            Paths = merger.GetMergedLineStrings().OfType<LineString>().ToList();
            Paths.ForEach(p => PathEngine.Insert(p.EnvelopeInternal, p));
        }

    }
}
