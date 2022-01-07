using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using ThMEPWSS.JsonExtensionsNs;
using static ThMEPWSS.Assistant.DrawUtils;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Uitl;
using ThMEPWSS.Pipe.Service;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Colors;
using System.Text.RegularExpressions;
using ThCADExtension;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using System.IO;
using System.Windows.Forms;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Assistant;
using ThMEPWSS.ReleaseNs;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.ReleaseNs.RainSystemNs;

namespace ThMEPWSS.Pipe.Service
{
    #region Tools
    public static class PolylineTools
    {
        public static Polyline CreatePolyline(IList<Point3d> pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                pline.AddVertexAt(i, pts[i].ToPoint2d(), 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreatePolyline(Point2dCollection pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                pline.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreatePolyline(params Point2d[] pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Length; i++)
            {
                pline.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreateRectangle(Point2d pt1, Point2d pt2)
        {
            var minX = Math.Min(pt1.X, pt2.X);
            var maxX = Math.Max(pt1.X, pt2.X);
            var minY = Math.Min(pt1.Y, pt2.Y);
            var maxY = Math.Max(pt1.Y, pt2.Y);
            var pts = new Point2dCollection
{
new Point2d(minX, minY),
new Point2d(minX, maxY),
new Point2d(maxX, maxY),
new Point2d(maxX, minY)
};
            var pline = CreatePolyline(pts);
            pline.Closed = true;
            return pline;
        }
        public static Polyline CreatePolygon(Point2d centerPoint, int num, double radius)
        {
            var pts = new Point2dCollection(num);
            double angle = 2 * Math.PI / num;
            for (int i = 0; i < num; i++)
            {
                var pt = new Point2d(centerPoint.X + radius * Math.Cos(i * angle),
                centerPoint.Y + radius * Math.Sin(i * angle));
                pts.Add(pt);
            }
            var pline = CreatePolyline(pts);
            pline.Closed = true;
            return pline;
        }
        public static Polyline CreatePolyCircle(Point2d centerPoint, double radius)
        {
            var pt1 = new Point2d(centerPoint.X + radius, centerPoint.Y);
            var pt2 = new Point2d(centerPoint.X - radius, centerPoint.Y);
            var pline = new Polyline();
            pline.AddVertexAt(0, pt1, 1, 0, 0);
            pline.AddVertexAt(1, pt2, 1, 0, 0);
            pline.AddVertexAt(2, pt1, 1, 0, 0);
            pline.Closed = true;
            return pline;
        }
        public static Polyline CreatePolyArc(Point2d centerPoint, double radius, double startAngle, double endAngle)
        {
            var pt1 = new Point2d(centerPoint.X + radius * Math.Cos(startAngle),
            centerPoint.Y + radius * Math.Sin(startAngle));
            var pt2 = new Point2d(centerPoint.X + radius * Math.Cos(endAngle),
            centerPoint.Y + radius * Math.Sin(endAngle));
            var pline = new Polyline();
            pline.AddVertexAt(0, pt1, Math.Tan((endAngle - startAngle) / 4), 0, 0);
            pline.AddVertexAt(1, pt2, 0, 0, 0);
            return pline;
        }
        public static double DegreeToRadian(double angle)
        {
            return angle * (Math.PI / 180.0);
        }
    }
    public static class CircleTools
    {
        public static Circle CreateCircle(Point3d pt1, Point3d pt2, Point3d pt3)
        {
            var va = pt1.GetVectorTo(pt2);
            var vb = pt1.GetVectorTo(pt3);
            var angle = va.GetAngleTo(vb);
            if (angle == 0 || angle == Math.PI)
            {
                return null;
            }
            else
            {
                var circle = new Circle();
                var geArc = new CircularArc3d(pt1, pt2, pt3);
                circle.Center = geArc.Center;
                circle.Radius = geArc.Radius;
                return circle;
            }
        }
        public static double AngleFromXAxis(Point3d pt1, Point3d pt2)
        {
            var vec = new Vector2d(pt1.X - pt2.X, pt1.Y - pt2.Y);
            return vec.Angle;
        }
        public static void AddFan(Point3d startPoint, Point3d pointOnArc, Point3d endPoint,
        out Arc arc, out Line line1, out Line line2)
        {
            var db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                arc = new Arc();
                arc.CreateArc(startPoint, pointOnArc, endPoint);
                line1 = new Line(arc.Center, startPoint);
                line2 = new Line(arc.Center, endPoint);
                db.AddToModelSpace(line1, line2, arc);
                trans.Commit();
            }
        }
    }
    #endregion

    public class BFSHelper2<T> where T : class
    {
        public KeyValuePair<T, T>[] Pairs;
        public T[] Items;
        public Action<BFSHelper2<T>, T> Callback;
        public HashSet<T> visited = new HashSet<T>();
        public Queue<T> queue = new Queue<T>();
        public T root;
        public T GetFirstNotVisited()
        {
            foreach (var item in Items) if (!visited.Contains(item)) return item;
            return null;
        }
        T[] getChildren(T item)
        {
            IEnumerable<T> f()
            {
                foreach (var kv in Pairs)
                {
                    if (kv.Key == item) yield return kv.Value;
                    if (kv.Value == item) yield return kv.Key;
                }
            }
            return f().Where(x => x != item).Distinct().ToArray();
        }
        public void BFS()
        {
            while (true)
            {
                var item = GetFirstNotVisited();
                if (item == null) break;
                BFS(item);
            }
        }
        public void BFS(T start)
        {
            root = start;
            visit(start);
            while (queue.Count > 0)
            {
                var sz = queue.Count;
                for (int i = 0; i < sz; i++)
                {
                    var cur = queue.Dequeue();
                    var children = getChildren(cur);
                    foreach (var c in children)
                    {
                        if (!visited.Contains(c))
                        {
                            visit(c);
                        }
                    }
                }
            }
        }
        private void visit(T i)
        {
            queue.Enqueue(i);
            visited.Add(i);
            Callback?.Invoke(this, i);
        }
    }
    public class BFSHelper
    {
        public KeyValuePair<int, int>[] Pairs;
        public int TotalCount;
        public Action<BFSHelper, int> Callback;
        public HashSet<int> visited = new HashSet<int>();
        public Queue<int> queue = new Queue<int>();
        public int root;
        public int GetFirstNotVisited()
        {
            for (int i = 0; i < TotalCount; i++)
            {
                if (!visited.Contains(i)) return i;
            }
            return -1;
        }
        int[] getChildren(int i)
        {
            IEnumerable<int> f()
            {
                foreach (var kv in Pairs)
                {
                    if (kv.Key == i) yield return kv.Value;
                    if (kv.Value == i) yield return kv.Key;
                }
            }
            return f().Where(x => x != i).Distinct().ToArray();
        }
        public void BFS()
        {
            while (true)
            {
                var start = GetFirstNotVisited();
                if (start < 0) break;
                BFS(start);
            }
        }
        public void BFS(int start)
        {
            root = start;
            visit(start);
            while (queue.Count > 0)
            {
                var sz = queue.Count;
                for (int i = 0; i < sz; i++)
                {
                    var cur = queue.Dequeue();
                    var children = getChildren(cur);
                    foreach (var c in children)
                    {
                        if (!visited.Contains(c))
                        {
                            visit(c);
                        }
                    }
                }
            }
        }
        private void visit(int i)
        {
            queue.Enqueue(i);
            visited.Add(i);
            Callback?.Invoke(this, i);
        }
    }

    public class ListDict<T>
    {
        Dictionary<T, List<T>> dict = new Dictionary<T, List<T>>();
 
        public void Add(T item, T value)
        {
            if (dict.TryGetValue(item, out List<T> list))
            {
                list.Add(value);
            }
            else
            {
                dict[item] = new List<T>() { value };
            }
        }
        public void ForEach(Action<T, List<T>> cb)
        {
            foreach (var kv in this.dict) cb(kv.Key, kv.Value);
        }
    }
    public class ListDict<K, V> : IEnumerable<KeyValuePair<K, List<V>>>
    {
        Dictionary<K, List<V>> dict = new Dictionary<K, List<V>>();
       
        public void Add(K item, V value)
        {
            if (dict.TryGetValue(item, out List<V> list))
            {
                list.Add(value);
            }
            else
            {
                dict[item] = new List<V>() { value };
            }
        }
    
        public List<V> this[K item]
        {
            get
            {
                dict.TryGetValue(item, out List<V> list);
                return list;
            }
        }

        public IEnumerator<KeyValuePair<K, List<V>>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return dict.GetEnumerator();
        }
    }
    public class ThGravityService
    {
        public AcadDatabase adb;
        public Point3dCollection CurrentSelectionExtent { get; private set; }
        private List<Entity> _Gravities = null;
        private List<Entity> Gravities
        {
            get
            {
                if (_Gravities == null)
                {
                    var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                    gravityBucketEngine.Recognize(adb.Database, CurrentSelectionExtent);
                    _Gravities = gravityBucketEngine.Elements.Select(g => g.Outline).ToList();
                }
                return _Gravities;
            }
        }
        public Func<Point3dCollection, List<Entity>> GetGravityWaterBuckets;
        private ThCADCoreNTSSpatialIndex _AllGravityWaterBucketSpatialIndex = null;
        private ThCADCoreNTSSpatialIndex AllGravityWaterBucketSpatialIndex
        {
            get
            {
                if (_AllGravityWaterBucketSpatialIndex == null)
                    _AllGravityWaterBucketSpatialIndex = new ThCADCoreNTSSpatialIndex(Gravities.ToCollection());
                return _AllGravityWaterBucketSpatialIndex;
            }
        }
        private List<Entity> _Sides = null;
        private List<Entity> Sides
        {
            get
            {
                if (_Sides == null)
                {
                    var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
                    sidebucketEngine.Recognize(adb.Database, CurrentSelectionExtent);
                    _Sides = sidebucketEngine.Elements.Select(e => e.Outline).ToList();
                }
                return _Sides;
            }
        }
        private ThCADCoreNTSSpatialIndex AllSideWaterBucketSpatialIndex
        {
            get
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Sides.ToCollection());
                return spatialIndex;
            }
        }
        private List<Extents3d> _AllSideWaterBucketExtents = null;
        private List<Extents3d> AllSideWaterBucketExtents
        {
            get
            {
                if (_AllSideWaterBucketExtents == null)
                    _AllSideWaterBucketExtents = Sides.Select(g => g.GeometricExtents).ToList();
                return _AllSideWaterBucketExtents;
            }
        }
        public void Init(Point3dCollection baseRange)
        {
            CurrentSelectionExtent = baseRange;
        }
        public List<Extents3d> GetRelatedGravityWaterBucket(Point3dCollection range)
        {
            var rst = new List<Extents3d>();
            var selected = AllGravityWaterBucketSpatialIndex.SelectCrossingPolygon(range);
            foreach (Entity e in selected)
            {
                rst.Add(e.GeometricExtents);
            }
            return rst;
        }
        public Pipe.Model.WaterBucketEnum GetRelatedSideWaterBucket(Point3d centerOfPipe)
        {
            foreach (var e in AllSideWaterBucketExtents)
            {
                if (e.IsPointIn(centerOfPipe))
                {
                    return WaterBucketEnum.Side;
                }
            }
            return Pipe.Model.WaterBucketEnum.None;
        }
    }
    public class ThRainSystemService
    {
        const string ROOF_RAIN_PIPE_PREFIX = "Y1";
        const string BALCONY_PIPE_PREFIX = "Y2";
        const string CONDENSE_PIPE_PREFIX = "NL";
        public AcadDatabase adb;
        public Dictionary<Entity, GRect> BoundaryDict = new Dictionary<Entity, GRect>();
        public Dictionary<Entity, string> VerticalPipeDBTextDict = new Dictionary<Entity, string>();
        public List<Entity> VerticalPipeLines = new List<Entity>();
        public List<DBText> VerticalPipeDBTexts = new List<DBText>();
        public List<Entity> VerticalPipes = new List<Entity>();
        public Dictionary<string, string> VerticalPipeLabelToDNDict = new Dictionary<string, string>();
        public Dictionary<Entity, string> VerticalPipeToLabelDict = new Dictionary<Entity, string>();
        public List<Tuple<Entity, Entity>> ShortConverters = new List<Tuple<Entity, Entity>>();
        public List<Entity> LongConverterLines = new List<Entity>();
        public List<BlockReference> WrappingPipes = new List<BlockReference>();
        public List<Entity> DraiDomePipes = new List<Entity>();
        public List<Entity> WaterWells = new List<Entity>();
        public Dictionary<Entity, string> WaterWellDNs = new Dictionary<Entity, string>();
        public List<Entity> RainDrain13s = new List<Entity>();
        public List<Entity> ConnectToRainPortSymbols = new List<Entity>();
        public List<DBText> ConnectToRainPortDBTexts = new List<DBText>();
        public List<Entity> WRainLines = new List<Entity>();
        public List<Entity> WRainRealLines = new List<Entity>();
        public Dictionary<Entity, Entity> WRainLinesMapping = new Dictionary<Entity, Entity>();
        public Dictionary<Entity, Entity> ConnectToRainPortSymbolToLongConverterLineDict = new Dictionary<Entity, Entity>();
        public Dictionary<Entity, DBText> ConnectToRainPortSymbolToConnectToRainDrainDBTextDict = new Dictionary<Entity, DBText>();
        public List<Entity> CondensePipes = new List<Entity>();
        private ThCADCoreNTSSpatialIndex DbTextSpatialIndex;
        public IEnumerable<Entity> AllShortConverters
        {
            get
            {
                IEnumerable<Entity> f()
                {
                    foreach (var item in ShortConverters)
                    {
                        yield return item.Item1;
                        yield return item.Item2;
                    }
                }
                return f().Distinct();
            }
        }
        public bool IsCondensePipeLow(Entity cp)
        {
            cpIsLowDict.TryGetValue(cp, out bool r);
            return r;
        }
        bool inited;

        public bool HasBrokenCondensePipe(Point3dCollection range, string id)
        {
            return FiltByRect(range, brokenCondensePipes.Where(kv => kv.Value == id).Select(kv => kv.Key)).Any();
        }
        HashSet<Entity> wrappingEnts = new HashSet<Entity>();
        public bool HasDrivePipe(Entity e)
        {
            return wrappingEnts.Contains(e);
        }
        public IEnumerable<Entity> EnumerateTianzhengElements()
        {
            return adb.ModelSpace.OfType<Entity>().Where(x => IsTianZhengElement(x));
        }
        public bool HasShortConverters(Entity ent)
        {
            return AllShortConverters.Contains(ent);
        }
        public bool HasLongConverters(Entity ent)
        {
            var ret = LongConverterPipes.Contains(ent) || LongPipes.Contains(ent);
            return ret;
        }
        public RainOutputTypeEnum GetOutputType(Point3dCollection pts, string pipeId, out bool hasDrivePipe)
        {
            var rt = _GetOutputType(pts, pipeId, out hasDrivePipe);
            return rt;
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToPipesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public TranslatorTypeEnum GetTranslatorType(Point3dCollection range, string verticalPipeID)
        {
            if (HasGravityLabelConnected(range, verticalPipeID)) return TranslatorTypeEnum.None;
            var ret = _GetTranslatorType(range, verticalPipeID);
            return ret;
        }

        private TranslatorTypeEnum _GetTranslatorType(Point3dCollection range, string verticalPipeID)
        {
            List<Entity> pipes = GetVerticalPipes(range);
            var pipe = _GetVerticalPipe(pipes, verticalPipeID);
            if (pipe != null)
            {
                if (HasShortConverters(pipe)) return TranslatorTypeEnum.Short;
                if (HasLongConverters(pipe)) return TranslatorTypeEnum.Long;
            }
            foreach (var pp in _GetVerticalPipes(pipes, verticalPipeID))
            {
                if (HasLongConverters(pp)) return TranslatorTypeEnum.Long;
            }
            if (hasLongConverter?.Invoke(range.ToRect(), verticalPipeID) ?? false)
            {
                return TranslatorTypeEnum.Long;
            }
            return TranslatorTypeEnum.None;
        }
        public static void SortByY(ref Point2d pt1, ref Point2d pt2)
        {
            if (pt1.Y > pt2.Y)
            {
                var tmp = pt1;
                pt1 = pt2;
                pt2 = tmp;
            }
        }
        public static Point2d GetTargetPoint(GLineSegment line1, GLineSegment line2)
        {
            var y1 = line1.Center.Y;
            var y2 = line2.Center.Y;
            var pt1 = line2.StartPoint;
            var pt2 = line2.EndPoint;
            SortByY(ref pt1, ref pt2);
            if (y1 > y2)
            {
                return pt1;
            }
            else if (y1 < y2)
            {
                return pt2;
            }
            else
            {
                var xArr = new double[] { line1.StartPoint.X, line1.EndPoint.X, line2.StartPoint.X, line2.EndPoint.X };
                var minx = xArr.Min();
                var maxx = xArr.Max();
                if (line1.Center.X < line2.Center.X)
                {
                    return new Point2d(maxx, y1);
                }
                else
                {
                    return new Point2d(minx, y1);
                }
            }
        }
        public static void TempPatch(AcadDatabase adb, ThRainSystemService sv)
        {
            var txts = new List<Entity>();
            foreach (var ent in sv.EnumerateTianzhengElements().ToList())
            {
                var lst = ent.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                if (lst.Count == 1)
                {
                    var e = lst.First();
                    txts.Add(e);
                }
            }
            var pipes = new List<Entity>();
            foreach (var ent in sv.EnumerateTianzhengElements().ToList())
            {
                if (ent.Layer == "W-RAIN-EQPM" || ent.Layer == "WP_KTN_LG")
                {
                    var lst = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    if (lst.Count == 1)
                    {
                        var e = lst.First();
                        pipes.Add(ent);
                    }
                    else
                    {
                        pipes.Add(ent);
                    }
                }
            }
            {
                var ps = sv.VerticalPipeToLabelDict.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
                pipes = pipes.Except(ps).ToList();
            }
            var lines = adb.ModelSpace.OfType<Line>().Where(x => x.Length > 0 && x.Layer == "W-RAIN-NOTE").Cast<Entity>().ToList();
            var d = new Dictionary<Entity, GRect>();
            foreach (var e in pipes.Concat(lines).Concat(txts).Distinct())
            {
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            var gs = ThRainSystemService.GroupLines(lines);
            foreach (var g in gs)
            {
                string lb = null;
                foreach (Line e in g)
                {
                    var s = e.ToGLineSegment();
                    if (s.IsHorizontal(10))
                    {
                        foreach (var t in txts)
                        {
                            var bd = d[t];
                            if (bd.CenterY > d[e].Center.Y)
                            {
                                if (GeoAlgorithm.Distance(bd.Center, d[e].Center) < 500)
                                {
                                    lb = ((DBText)t).TextString;
                                    goto xx;
                                }
                            }
                        }
                    }
                }
                xx:
                if (lb != null)
                {
                    var pts = new List<Point2d>(8);
                    foreach (Line line in g)
                    {
                        var s = line.ToGLineSegment();
                        pts.Add(s.StartPoint);
                        pts.Add(s.EndPoint);
                    }
                    foreach (var p in pipes)
                    {
                        var bd = d[p];
                        foreach (var pt in pts)
                        {
                            if (bd.ContainsPoint(pt))
                            {
                                if (!sv.VerticalPipeToLabelDict.ContainsKey(p))
                                {
                                    sv.VerticalPipeToLabelDict[p] = lb;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
        public static void Triangle<T>(IList<T> lst, Action<T, T> cb)
        {
            for (int i = 0; i < lst.Count; i++)
            {
                for (int j = i + 1; j < lst.Count; j++)
                {
                    cb(lst[i], lst[j]);
                }
            }
        }
        public static void Triangle(int count, Action<int, int> cb)
        {
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    cb(i, j);
                }
            }
        }

        public List<Entity> GetVerticalPipes(Point3dCollection range)
        {
            if (!RangeToPipesDict.TryGetValue(range, out List<Entity> pipes))
            {
                pipes = _GetVerticalPipes(range);
                RangeToPipesDict[range] = pipes;
            }
            return pipes;
        }
        private IEnumerable<Entity> _GetVerticalPipes(List<Entity> pipes, string id)
        {
            return pipes.Where(p =>
            {
                VerticalPipeToLabelDict.TryGetValue(p, out string lb); return lb == id;
            });
        }
        private Entity _GetVerticalPipe(List<Entity> pipes, string id)
        {
            return pipes.FirstOrDefault(p =>
            {
                VerticalPipeToLabelDict.TryGetValue(p, out string lb); return lb == id;
            });
        }
        ThCADCoreNTSSpatialIndex _verticalPipesSpatialIndex;
        private List<Entity> _GetVerticalPipes(Point3dCollection pts)
        {
            _verticalPipesSpatialIndex ??= BuildSpatialIndex(VerticalPipes);
            return _verticalPipesSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        private RainOutputTypeEnum _GetOutputType(Point3dCollection pts, string pipeId, out bool hasDrivePipe)
        {
            if (IsRainPort(pipeId))
            {
                hasDrivePipe = false;
                return RainOutputTypeEnum.RainPort;
            }
            if (IsWaterWell(pipeId))
            {
                hasDrivePipe = HasOutputDrivePipeForWaterWell(pts, pipeId);
                return RainOutputTypeEnum.WaterWell;
            }
            {
                hasDrivePipe = false;
                return RainOutputTypeEnum.None;
            }
        }
        Dictionary<Point3dCollection, List<Entity>> WrappingPipesRangeDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetWrappingPipes(Point3dCollection range)
        {
            if (!WrappingPipesRangeDict.TryGetValue(range, out List<Entity> ret))
            {
                ret = FiltEntityByRange(range.ToRect(), WrappingPipes).ToList();
                WrappingPipesRangeDict[range] = ret;
            }
            return ret;
        }
        public bool HasOutputDrivePipeForWaterWell(Point3dCollection range, string pipeId)
        {
            return GetWrappingPipes(range).Any(e => GetLabel(e) == pipeId);
        }
        public static void ConnectLabelToLabelLine(RainSystemGeoData geoData)
        {
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1500, 250);
                {
                    var e = DrawRectLazy(g);
                    e.ColorIndex = 2;
                }
                var _lineHGs = f1(g.ToPolygon());
                var f2 = GeoFac.NearestNeighbourGeometryF(_lineHGs);
                var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                if (geo == null) continue;
                {
                    var ents = geo.ToDbObjects().OfType<Entity>().ToList();
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(100, 400) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(.1, 400))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(.1));
                    }
                }
            }
        }

        public static void AppendSideWaterBuckets(AcadDatabase adb, Point3dCollection range, RainSystemGeoData geoData)
        {
            var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
            sidebucketEngine.Recognize(adb.Database, range);
            geoData.SideWaterBuckets.AddRange(sidebucketEngine.Elements.Select(e => e.Outline.Bounds.ToGRect()).Where(r => r.IsValid));
        }
        public static void AppendGravityWaterBuckets(AcadDatabase adb, Point3dCollection range, RainSystemGeoData geoData)
        {
            var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
            gravityBucketEngine.Recognize(adb.Database, range);
            geoData.GravityWaterBuckets.AddRange(gravityBucketEngine.Elements.Select(e => e.Outline.Bounds.ToGRect()).Where(r => r.IsValid));
        }
        public class ThRainSystemServiceGeoCollector
        {
            public AcadDatabase adb;
            public List<Entity> entities;
            public RainSystemGeoData geoData;
            List<GLineSegment> labelLines => geoData.LabelLines;
            List<CText> cts => geoData.Labels;
            List<GRect> pipes => geoData.VerticalPipes;
            List<GRect> storeys => geoData.Storeys;
            List<GLineSegment> wLines => geoData.WLines;
            List<GLineSegment> wLinesAddition => geoData.WLinesAddition;
            List<GRect> condensePipes => geoData.CondensePipes;
            List<GRect> floorDrains => geoData.FloorDrains;
            List<GRect> waterWells => geoData.WaterWells;
            List<string> waterWellLabels => geoData.WaterWellLabels;
            List<GRect> waterPortSymbols => geoData.WaterPortSymbols;
            List<GRect> waterPort13s => geoData.WaterPort13s;
            List<GRect> wrappingPipes => geoData.WrappingPipes;
            public void CollectEntities()
            {
                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br)
                        {
                            if (br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (br.GetEffectiveName() == "11" || (r.Width > 20000 && r.Width < 80000 && r.Height > 5000))
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else if (br.Layer == "块")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                        else
                        {
                            yield return ent;
                        }
                    }
                }
                var entities = GetEntities().ToList();
                this.entities = entities;
            }
            public void CollectLabelLines()
            {
                foreach (var e in entities.OfType<Line>()
                .Where(e => (e.Layer == "W-RAIN-NOTE" || e.Layer == "W-RAIN-DIMS" || e.Layer == "W-FRPT-NOTE")
                && e.Length > 0))
                {
                    labelLines.Add(e.ToGLineSegment());
                }
            }
            public void CollectCTexts()
            {
                foreach (var e in entities.OfType<DBText>()
                .Where(e => (e.Layer == "W-RAIN-NOTE" || e.Layer == "W-RAIN-DIMS" || e.Layer == "W-FRPT-NOTE")))
                {
                    cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                }
                foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                {
                    foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                    {
                        var ct = new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() };
                        if (!ct.Boundary.IsValid)
                        {
                            var p = e.Position.ToPoint2d();
                            var h = e.Height;
                            var w = h * e.WidthFactor * e.WidthFactor * e.TextString.Length;
                            var r = new GRect(p, p.OffsetXY(w, h));
                            ct.Boundary = r;
                        }
                        cts.Add(ct);
                    }
                }
            }
            int distinguishDiameter = 35;
            public void CollectVerticalPipes()
            {
                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<BlockReference>()
                    .Where(x => x.Layer == "W-RAIN-EQPM")
                    .Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName() == "带定位立管"));
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                        var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                        if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                        return GeoAlgorithm.GetBoundaryRect(ent);
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }
                {
                    var pps = new List<Circle>();
                    pps.AddRange(entities.OfType<Circle>()
                    .Where(x => x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM" || x.Layer == "W-RAIN-DIMS")
                    .Where(c => distinguishDiameter <= c.Radius && c.Radius <= 100));
                    static GRect getRealBoundaryForPipe(Circle c)
                    {
                        return c.Bounds.ToGRect();
                    }
                    foreach (var pp in pps.Distinct())
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }
                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities
                    .Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")
                    && ThRainSystemService.IsTianZhengElement(x))
                    .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Any())
                    );
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect(50);
                    }
                    foreach (var pp in pps.Distinct())
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }
                {
                    DrawUtils.CollectTianzhengVerticalPipes(labelLines, cts, entities);
                }
                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<BlockReference>()
                    .Where(x => x.ObjectId.IsValid ? x.Layer == "W-RAIN-EQPM" && x.GetBlockEffectiveName() == "$LIGUAN" : x.Layer == "W-RAIN-EQPM")
                    );
                    pps.AddRange(entities.OfType<BlockReference>()
                    .Where(e =>
                    {
                        return e.ObjectId.IsValid && (e.Layer == "W-RAIN-PIPE-RISR" || e.Layer == "W-DRAI-NOTE")
                                                && !e.GetBlockEffectiveName().Contains("井");
                    }));
                    foreach (var pp in pps)
                    {
                        pipes.Add(GRect.Create(pp.Bounds.ToGRect().Center, 55));
                    }
                }
            }
            public void CollectWLines()
            {
                    {
                        foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                        {
                            if (e is Line line && line.Length > 0)
                            {
                                wLines.Add(line.ToGLineSegment());
                            }
                            else if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                                {
                                    if (seg.Length > 0)
                                    {
                                        var lst = e.ExplodeToDBObjectCollection().OfType<Line>().Where(ln => ln.Length > 0).ToList();
                                        if (lst.Count == 1)
                                        {
                                            wLines.Add(lst[0].ToGLineSegment());
                                        }
                                        else if (lst.Count > 1)
                                        {
                                            wLines.Add(lst[0].ToGLineSegment());
                                            Point3d p1 = default, p2 = default;
                                            var tmp = new List<GLineSegment>();
                                            for (int i = 1; i < lst.Count; i++)
                                            {
                                                wLines.Add(lst[i].ToGLineSegment());
                                                p1 = lst[i - 1].EndPoint;
                                                p2 = lst[i].StartPoint;
                                                var sg = new GLineSegment(p1, p2);
                                                if (sg.Length > 0)
                                                    tmp.Add(sg);
                                            }
                                            wLinesAddition.AddRange(tmp);
                                        }
                                    }
                                }
                            }
                        }
                    }
            }

            public void CollectCondensePipes()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<Circle>()
                .Where(c => c.Layer == "W-RAIN-EQPM")
                .Where(c => 20 < c.Radius && c.Radius < distinguishDiameter));
                condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
            }
            public void CollectFloorDrains()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName().Contains("地漏")));
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                floorDrains.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
            }
            public void CollectWaterWells()
            {
                var ents = new List<BlockReference>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                waterWellLabels.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
            }
            public void CollectWaterPortSymbols()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                ents.AddRange(entities.Where(e => DrawUtils.IsTianZhengRainPort(e)));
                waterPortSymbols.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
            }
            public void CollectWaterPort13s()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName().Contains("雨水口")));
                waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
            }
            public void CollectWrappingPipes()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetBlockEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
            }
            public void CollectStoreys(Point3dCollection range)
            {
                var storeysRecEngine = new ThStoreysRecognitionEngine();
                storeysRecEngine.Recognize(adb.Database, range);
                foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                {
                    var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                    storeys.Add(bd);
                }
            }
        }
        public static List<ThStoreysData> GetStoreys(Geometry range, AcadDatabase adb, CommandContext ctx)
        {
            return ctx.StoreyContext.thStoreysDatas;
        }
        public static List<ThStoreysData> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            var storeys = new List<ThStoreysData>();
            foreach (var s in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
            {
                var e = adb.Element<Entity>(s.ObjectId);
                var data = new ThStoreysData()
                {
                    Boundary = e.Bounds.ToGRect(),
                    Storeys = s.Storeys,
                    StoreyType = s.StoreyType,
                };
                storeys.Add(data);
            }
            FixStoreys(storeys);
            return storeys;
        }

        public static void FixStoreys(List<ThStoreysData> storeys)
        {
            var lst1 = storeys.Where(s => s.Storeys.Count == 1).Select(s => s.Storeys[0]).ToList();
            foreach (var s in storeys.Where(s => s.Storeys.Count > 1).ToList())
            {
                var hs = new HashSet<int>(s.Storeys);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Storeys.Clear();
                s.Storeys.AddRange(hs.OrderBy(i => i));
            }
        }
        public class ThStoreysData
        {
            public GRect Boundary;
            public List<int> Storeys;
            public ThMEPEngineCore.Model.Common.StoreyType StoreyType;
        }
        public class StoreyContext
        {
            public List<ThStoreysData> thStoreysDatas;
            public List<ThMEPEngineCore.Model.Common.ThStoreys> thStoreys;
            public List<ObjectId> GetObjectIds()
            {
                return thStoreys.Select(o => o.ObjectId).ToList();
            }
        }
        public class CommandContext
        {
            public Point3dCollection range;
            public StoreyContext StoreyContext;
            public RainSystemDiagramViewModel rainSystemDiagramViewModel;
            public System.Windows.Window window;
        }
        public static CommandContext commandContext;
        public static void InitFloorListDatas()
        {
            ThMEPWSS.Common.Utils.FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return;
            var ctx = commandContext;
            ctx.range = range;
            using var adb = AcadDatabase.Active();
            ctx.StoreyContext = GetStoreyContext(range, adb);
            InitFloorListDatas(adb);
        }
        public static StoreyContext GetStoreyContext(Point3dCollection range, AcadDatabase adb)
        {
            var ctx = new StoreyContext();
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            var storeys = new List<ThStoreysData>();
            ctx.thStoreys = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().ToList();
            foreach (var s in ctx.thStoreys)
            {
                var e = adb.Element<Entity>(s.ObjectId);
                var data = new ThStoreysData()
                {
                    Boundary = e.Bounds.ToGRect(),
                    Storeys = s.Storeys,
                    StoreyType = s.StoreyType,
                };
                storeys.Add(data);
            }
            FixStoreys(storeys);
            ctx.thStoreysDatas = storeys;
            return ctx;
        }
        public static void InitFloorListDatas(AcadDatabase adb)
        {
            var ctx = commandContext.StoreyContext;
            var storeys = ctx.GetObjectIds()
            .Select(o => adb.Element<BlockReference>(o))
            .Where(o => o.GetBlockEffectiveName() == ThWPipeCommon.STOREY_BLOCK_NAME)
            .Select(o => o.ObjectId)
            .ToObjectIdCollection();
            var service = new ThReadStoreyInformationService();
            service.Read(storeys);
            commandContext.rainSystemDiagramViewModel.FloorListDatas = service.StoreyNames.Select(o => o.Item2).ToList();
        }
        public Polyline CreatePolygon(Entity e, int num = 4, double expand = 0)
        {
            var bd = BoundaryDict[e];
            var pl = PolylineTools.CreatePolygon(bd.Center, num, bd.Radius + expand);
            return pl;
        }
        const double tol = 800;

        public ThGravityService thGravityService;
        public static bool IsTianZhengElement(Entity ent)
        {
            return ThMEPEngineCore.Algorithm.ThMEPTCHService.IsTCHElement(ent);
        }

        public List<Entity> TianZhengEntities = new List<Entity>();
        public List<Entity> SingleTianzhengElements = new List<Entity>();
        public void CollectTianZhengEntities()
        {
            TianZhengEntities.AddRange(adb.ModelSpace.OfType<Entity>().Where(x => IsTianZhengElement(x)));
        }
        public void ExplodeSingleTianZhengElements()
        {
            foreach (var e in TianZhengEntities)
            {
                var colle = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                if (colle.Count == 1)
                {
                    SingleTianzhengElements.Add(colle[0]);
                }
            }
        }
        public List<Entity> ExplodedEntities = new List<Entity>();
        public List<Entity> vps = new List<Entity>();
        public List<DBText> txts = new List<DBText>();
        public void CollectExplodedEntities()
        {
            foreach (var br in adb.ModelSpace.OfType<BlockReference>())
            {
                var r = GeoAlgorithm.GetBoundaryRect(br);
                if (r.Width > 10000 && r.Width < 60000)
                {
                    foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                    {
                        if (e is BlockReference br2)
                        {
                            if (br2.Name == "*U398")
                            {
                                vps.Add(br2);
                            }
                        }
                        else if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            var lst = e.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                            foreach (var t in lst)
                            {
                                txts.Add(t);
                            }
                        }
                    }
                }
            }
            foreach (var br in adb.ModelSpace.OfType<BlockReference>())
            {
                var r = GeoAlgorithm.GetBoundaryRect(br);
                if (r.Width > 1000 && r.Width < 60000)
                {
                    foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                    {
                        if (e is DBText t && t.Layer == "W-RAIN-NOTE")
                        {
                            txts.Add(t);
                        }
                        else if (e is Circle c && e.Layer == "W-RAIN-EQPM")
                        {
                            vps.Add(e);
                        }
                    }
                }
            }
            foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
            {
                var r = GeoAlgorithm.GetBoundaryRect(br);
                if (r.Width > 10000 && r.Width < 100000)
                {
                    foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                    {
                        if (e is Circle && e.Layer == "W-RAIN-EQPM")
                        {
                            vps.Add(e);
                        }
                        else if (e is DBText t)
                        {
                            txts.Add(t);
                        }
                    }
                }
            }
            foreach (var br1 in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "C-SHET-SHET"))
            {
                foreach (var br2 in br1.ExplodeToDBObjectCollection().OfType<BlockReference>())
                {
                    foreach (var e in br2.ExplodeToDBObjectCollection().Cast<Entity>())
                    {
                        if (e is DBText t)
                        {
                            txts.Add(t);
                        }
                        else if (e is Circle)
                        {
                            vps.Add(e);
                        }
                    }
                }
            }
        }
        public static bool ImportElementsFromStdDwg()
        {
            var file = ThCADCommon.WSSDwgPath();
            if (!File.Exists(file))
            {
                MessageBox.Show($"\"{file}\"不存在");
                return false;
            }
            {
                using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (AcadDatabase adb = AcadDatabase.Active())
                using (AcadDatabase blockDb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly, false))
                {
                    var fs = new Dictionary<Action, string>();
                    {
                        var blocks = blockDb.Blocks.Select(x => x.Name).ToList();
                        foreach (var blk in blocks)
                        {
                            fs.Add(() => adb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blk)), blk);
                        }
                    }
                    {
                        blockDb.DimStyles.ForEach(x => adb.DimStyles.Import(x));
                        foreach (var txtStyle in blockDb.TextStyles)
                        {
                            adb.TextStyles.Import(txtStyle);
                        }
                    }
                    {
                        var layers = blockDb.Layers.Select(x => x.Name).ToList();
                        foreach (var layer in layers)
                        {
                            fs.Add(() => adb.Layers.Import(blockDb.Layers.ElementOrDefault(layer)), layer);
                        }
                    }
                    foreach (var kv in fs)
                    {
                        try
                        {
                            kv.Key();
                        }
                        catch (System.Exception ex)
                        {
                        }
                    }
                }
            }
            return true;
        }
        public List<Entity> SideWaterBuckets = new List<Entity>();
        public void InitCache()
        {
            if (inited) return;
            CollectTianZhengEntities();
            ExplodeSingleTianZhengElements();
            CollectExplodedEntities();
            CollectVerticalPipeLines();
            CollectVerticalPipeDBTexts();
            CollectVerticalPipes();
            CollectLongConverterLines();
            CollectDraiDomePipes();
            CollectWrappingPipes();
            CollectWaterWells();
            CollectWaterWell13s();
            CollectConnectToRainPortDBTexts();
            CollectConnectToRainPortSymbols();
            CollectWRainLines();
            CollectCondensePipes();
            CollectFloorDrains();
            CollectSideWaterBuckets();
            inited = true;
        }
        public void CollectSideWaterBuckets()
        {
            SideWaterBuckets.AddRange(EnumerateEntities<BlockReference>().Where(x => x.Name == "CYSD" || x.GetBlockEffectiveName() == "CYSD"));
        }
        public List<Entity> FloorDrains = new List<Entity>();
        public void CollectFloorDrains()
        {
            IEnumerable<Entity> q;
            {
                string strFloorDrain = "地漏";
                q = adb.ModelSpace.OfType<BlockReference>()
                .Where(e => e.ObjectId.IsValid)
                .Where(x =>
                {
                    if (x.IsDynamicBlock)
                    {
                        return x.ObjectId.GetDynBlockValue("可见性")?.Contains(strFloorDrain) ?? false;
                    }
                    else
                    {
                        return x.GetBlockEffectiveName().Contains(strFloorDrain);
                    }
                }
                );
            }
            {
                static bool IsFloorDrawin(BlockReference br)
                {
                    return br.Name == "*U400";
                }
                var lst = new List<Entity>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(br);
                    if (r.Width > 10000 && r.Width < 60000)
                    {
                        foreach (var e in br.ExplodeToDBObjectCollection().OfType<BlockReference>().ToList())
                        {
                            if (IsFloorDrawin(e))
                            {
                                lst.Add(e);
                            }
                        }
                    }
                }
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    if (IsFloorDrawin(e))
                    {
                        lst.Add(e);
                    }
                }
                q = q.Concat(lst);
            }
            FloorDrains.AddRange(q.Distinct());
            static GRect getRealBoundary(Entity ent)
            {
                var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                var et = ents.FirstOrDefault(e =>
                {
                    var m = Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width);
                    return m == 120;
                });
                if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                return GeoAlgorithm.GetBoundaryRect(ent);
            }
            foreach (var e in FloorDrains)
            {
                BoundaryDict[e] = getRealBoundary(e);
            }
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToWRainLinesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetWRainLines(Point3dCollection range)
        {
            if (!RangeToWRainLinesDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetWRainLines(range);
                RangeToWRainLinesDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _WRainLinessSpatialIndex;
        private List<Entity> _GetWRainLines(Point3dCollection pts)
        {
            _WRainLinessSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(WRainLines);
            return _WRainLinessSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }

        ThCADCoreNTSSpatialIndex _CondensePipesSpatialIndex;
        private List<Entity> _GetCondensePipes(Point3dCollection pts)
        {
            _CondensePipesSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(CondensePipes);
            return _CondensePipesSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }

        ThCADCoreNTSSpatialIndex _FloorDrainsSpatialIndex;
        private List<Entity> _GetFloorDrains(Point3dCollection pts)
        {
            _FloorDrainsSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(FloorDrains);
            return _FloorDrainsSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        public void CollectCondensePipes()
        {
            CondensePipes.AddRange(adb.ModelSpace.OfType<Circle>().Where(e => e.Layer == "W-RAIN-EQPM"));
            foreach (var e in CondensePipes)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }

        ThCADCoreNTSSpatialIndex _LongConverterLinesSpatialIndex;
        private List<Entity> _GetLongConverterLines(Point3dCollection pts)
        {
            _LongConverterLinesSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(LongConverterLines);
            return _LongConverterLinesSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToLWaterWellsDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetWaterWells(Point3dCollection range)
        {
            if (!RangeToLWaterWellsDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetWaterWells(range);
                RangeToLWaterWellsDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _WaterWellSpatialIndex;
        private List<Entity> _GetWaterWells(Point3dCollection pts)
        {
            _WaterWellSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(WaterWells);
            return _WaterWellSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        public void CollectWRainLines()
        {
            WRainRealLines.AddRange(adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE"));
            foreach (var e in WRainRealLines)
            {
                if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                {
                    var line = new Line() { StartPoint = seg.StartPoint.ToPoint3d(), EndPoint = seg.EndPoint.ToPoint3d() };
                    WRainLines.Add(line);
                    WRainLinesMapping[e] = line;
                    BoundaryDict[line] = new GRect(seg.StartPoint, seg.EndPoint);
                }
            }
        }
        public static Func<Polyline, List<Entity>> BuildSpatialIndexLazy(IList<Entity> ents)
        {
            var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(ents.ToCollection());
            List<Entity> f(Polyline pline)
            {
                return si.SelectCrossingPolygon(pline).Cast<Entity>().ToList();
            }
            return f;
        }
        public class EntitiesCollector
        {
            public List<Entity> Entities = new List<Entity>();
            public EntitiesCollector Add<T>(IEnumerable<T> ents) where T : Entity
            {
                Entities.AddRange(ents);
                return this;
            }
        }
        public static EntitiesCollector CollectEnts() => new EntitiesCollector();
        public static ThCADCoreNTSSpatialIndex BuildSpatialIndex<T>(IList<T> ents) where T : Entity
        {
            var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(ents.ToCollection());
            return si;
        }

        public void CollectConnectToRainPortDBTexts()
        {
            ConnectToRainPortDBTexts.AddRange(adb.ModelSpace.OfType<DBText>().Where(x => x.TextString == "接至雨水口"));
            foreach (var e in ConnectToRainPortDBTexts)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectConnectToRainPortSymbols()
        {
            IEnumerable<Entity> q = adb.ModelSpace.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS");
            q = q.Concat(adb.ModelSpace.OfType<Entity>().Where(x => IsTianZhengElement(x)).Where(x =>
            {
                return x.ExplodeToDBObjectCollection().OfType<BlockReference>().Any(x => x.Name == "$TwtSys$00000132");
            }));
            ConnectToRainPortSymbols.AddRange(q.Distinct());
            foreach (var e in ConnectToRainPortSymbols)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWaterWell13s()
        {
            RainDrain13s.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetBlockEffectiveName() == "13#雨水口"));
            foreach (var e in RainDrain13s)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWaterWells()
        {
            WaterWells.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
            WaterWells.ForEach(e => WaterWellDNs[e] = (e as BlockReference)?.GetAttributesStrValue("-") ?? "");
            foreach (var e in WaterWells)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public List<MText> AhTexts = new List<MText>();
        public void CollectAhTexts(Point3dCollection range)
        {
            var engine = new ThMEPWSS.Engine.ThAHMarkRecognitionEngine();
            engine.Recognize(adb.Database, range);
            AhTexts.AddRange(engine.Texts.OfType<MText>());
            foreach (var e in AhTexts)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void FixVPipes()
        {
            var d = new Dictionary<Entity, GRect>();
            var ld = new Dictionary<Entity, string>();
            var txts = new List<Entity>();
            var lines = new List<Entity>();
            var pipes = new List<Entity>();
            foreach (var e in adb.ModelSpace.OfType<Circle>().Where(c => Convert.ToInt32(c.Radius) == 50).ToList())
            {
                pipes.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-RAIN-EQPM").Where(x => ThRainSystemService.IsTianZhengElement(x)))
            {
                pipes.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            foreach (var e in adb.ModelSpace.OfType<DBText>().ToList())
            {
                if (ThRainSystemService.IsWantedLabelText(e.TextString))
                {
                    txts.Add(e);
                    d[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
            }
            foreach (var e in adb.ModelSpace.OfType<Line>().Where(line => line.Length > 0).ToList())
            {
                lines.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            var gs = ThRainSystemService.GroupLines(lines);
            foreach (var g in gs)
            {
                void f()
                {
                    foreach (var line in g.OfType<Line>())
                    {
                        var seg = line.ToGLineSegment();
                        if (seg.IsHorizontal(10))
                        {
                            foreach (var t in txts.OfType<DBText>())
                            {
                                var bd = d[t];
                                var dt = bd.CenterY - seg.StartPoint.Y;
                                if (dt > 0 && dt < 250)
                                {
                                    var x1 = Math.Min(seg.StartPoint.X, seg.EndPoint.X);
                                    var x2 = Math.Max(seg.StartPoint.X, seg.EndPoint.X);
                                    if (x1 < bd.CenterX && x2 > bd.CenterX)
                                    {
                                        var pts = g.OfType<Line>().SelectMany(line => new Point2d[] { line.StartPoint.ToPoint2d(), line.EndPoint.ToPoint2d() }).ToList();
                                        foreach (var p in pipes)
                                        {
                                            foreach (var pt in pts)
                                            {
                                                if (d[p].ContainsPoint(pt))
                                                {
                                                    var lb = t.TextString;
                                                    ld[p] = lb;
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                f();
            }
            foreach (var pipe in pipes)
            {
                ld.TryGetValue(pipe, out string lb);
                if (lb != null)
                {
                    VerticalPipeToLabelDict[pipe] = lb;
                }
            }
            foreach (var pipe in pipes)
            {
                if (!VerticalPipes.Contains(pipe)) VerticalPipes.Add(pipe);
            }
            foreach (var txt in txts.OfType<DBText>())
            {
                if (!VerticalPipeDBTexts.Contains(txt)) VerticalPipeDBTexts.Add(txt);
            }
            foreach (var line in lines)
            {
                if (!VerticalPipeLines.Contains(line)) VerticalPipeLines.Add(line);
            }
            foreach (var e in pipes.Concat(txts).Concat(lines))
            {
                BoundaryDict[e] = d[e];
            }
            var longPipes = new List<Entity>();
            var lines2 = new List<Entity>();
            string getLabel(Entity e)
            {
                ld.TryGetValue(e, out string v);
                return v;
            }
            foreach (var line in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-RAIN-PIPE").Where(x => ThRainSystemService.IsTianZhengElement(x)))
            {
                if (GeoAlgorithm.TryConvertToLineSegment(line, out GLineSegment seg))
                {
                    var pts = new Point2d[] { seg.StartPoint, seg.EndPoint };
                    var ps = pipes.Where(pipe => pts.Any(pt => d[pipe].ContainsPoint(pt)));
                    var pp1 = ps.FirstOrDefault(p => getLabel(p) != null);
                    var pp2 = ps.FirstOrDefault(p => getLabel(p) == null);
                    if (pp1 != null && pp2 != null)
                    {
                        longPipes.Add(pp1);
                        longPipes.Add(pp2);
                        if (!VerticalPipes.Contains(pp1)) VerticalPipes.Add(pp1);
                        if (!VerticalPipes.Contains(pp2)) VerticalPipes.Add(pp2);
                        VerticalPipeToLabelDict[pp2] = getLabel(pp1);
                    }
                }
            }
            foreach (var pp in longPipes)
            {
                LongPipes.Add(pp);
            }
            IEnumerable<Entity> getLongPipes(GRect range)
            {
                foreach (var pp in longPipes)
                {
                    if (range.ContainsRect(d[pp]))
                    {
                        yield return pp;
                    }
                }
            }
            IEnumerable<Entity> getPipes(GRect range)
            {
                foreach (var pp in pipes)
                {
                    if (range.ContainsRect(d[pp]))
                    {
                        yield return pp;
                    }
                }
            }
            bool hasLongConverter(GRect range, string lb)
            {
                return getLongPipes(range).Any(pp => getLabel(pp) == lb);
            }
            GRect getVPipeBoundary(GRect range, string lb)
            {
                var pp = getPipes(range).FirstOrDefault(pp => getLabel(pp) == lb);
                if (pp != null) return d[pp];
                return default;
            }
            this.getVPipeBoundary = getVPipeBoundary;
            getDbTexts = r =>
            {
                var ret = getPipes(r).Select(pp => GetLabel(pp)).Where(lb => lb != null).Distinct().ToList();
                return ret;
            };
            this.hasLongConverter = hasLongConverter;
        }
        Func<GRect, string, GRect> getVPipeBoundary;
        Func<GRect, string, bool> hasLongConverter;
        public List<Entity> LongPipes = new List<Entity>();
        public void CollectData()
        {
            InitCache();
            CollectVerticalPipesData();
            FindShortConverters();
            LabelEnts();
            FindOutBrokenCondensePipes();
            CalcCondensePipeIsLow();
            TempPatch(adb, this);
            FixVPipes();
        }
        Dictionary<Entity, bool> cpIsLowDict = new Dictionary<Entity, bool>();
        public void CalcCondensePipeIsLow()
        {
            var sv = this;
            var si = ThRainSystemService.BuildSpatialIndex(sv.AhTexts);
            foreach (var cp in sv.CondensePipes)
            {
                var isLow = false;
                var center = sv.BoundaryDict[cp].Center.ToPoint3d();
                var r = center.Expand(1000).ToGRect();
                var pl = r.ToCadPolyline();
                var ahs = si.SelectCrossingPolygon(pl).Cast<Entity>().ToList();
                if (ahs.Count > 0)
                {
                    var si2 = ThRainSystemService.BuildSpatialIndex(ahs);
                    var ah = si2.NearestNeighbours(center.Expand(.1).ToGRect().ToCadPolyline(), 1).Cast<Entity>().FirstOrDefault();
                    if (ah != null)
                    {
                        if (ah is MText mt)
                        {
                            if (mt.Contents.ToLower() == "ah1")
                            {
                                isLow = true;
                            }
                        }
                    }
                }
                cpIsLowDict[cp] = isLow;
            }
        }
        public List<KeyValuePair<Entity, Entity>> LongConverterLineToWaterBuckets = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LongConverterLineToWaterBucketsGroups;
        public void CollectData(Point3dCollection range)
        {
            CollectData();
            InitThGravityService(range);
            CollectLongConverterLineToWaterBucketsData();
            CollectAhTexts(range);
        }
        private void CollectLongConverterLineToWaterBucketsData()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.LongConverterLines);
            totalList.AddRange(sv.WaterBuckets);
            totalList.AddRange(sv.VerticalPipes);
            ThRainSystemService.MakePairs(GetLongConverterLinesGroup(), pairs);
            LongConverterLineToWaterBuckets.AddRange(sv.EnumerateEntities(sv.LongConverterLines, sv.WaterBuckets, 10));
            pairs.AddRange(LongConverterLineToWaterBuckets);
            pairs.AddRange(sv.EnumerateEntities(sv.LongConverterLines, sv.VerticalPipes, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            LongConverterLineToWaterBucketsGroups = groups;
        }
        public List<Entity> WaterBuckets;
        private void InitThGravityService(Point3dCollection range)
        {
            thGravityService = new ThGravityService() { adb = adb };
            thGravityService.Init(range);
            var WaterBuckets = thGravityService.GetRelatedGravityWaterBucket(range);
            var pls = new List<Entity>();
            foreach (var ext in WaterBuckets)
            {
                var r = GRect.Create(ext);
                var pl = EntityFactory.CreatePolyline(r.ToPt3dCollection());
                pls.Add(pl);
                BoundaryDict[pl] = r;
            }
            this.WaterBuckets = pls;
            var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(pls.ToCollection());
            thGravityService.GetGravityWaterBuckets = rg => si.SelectCrossingPolygon(rg).Cast<Entity>().ToList();
        }
        List<KeyValuePair<Entity, string>> brokenCondensePipes = new List<KeyValuePair<Entity, string>>();
        void FindOutBrokenCondensePipes()
        {
            var sv = this;
            var cps1 = new HashSet<Entity>();
            var cps2 = new HashSet<Entity>();
            foreach (var e in sv.CondensePipes)
            {
                var lb = sv.GetLabel(e);
                if (lb != null)
                {
                    cps1.Add(e);
                }
                else
                {
                    cps2.Add(e);
                }
            }
            foreach (var e in cps2)
            {
                var bd = sv.BoundaryDict[e];
                Entity ee = null;
                double dis = double.MaxValue;
                foreach (var c in cps1)
                {
                    var d = GeoAlgorithm.Distance(sv.BoundaryDict[c].Center, bd.Center);
                    if (d < dis)
                    {
                        dis = d;
                        ee = c;
                    }
                }
                if (ee != null && dis < 500)
                {
                    var lb = sv.GetLabel(ee);
                    brokenCondensePipes.Add(new KeyValuePair<Entity, string>(e, lb));
                    sv.SetLabel(e, lb);
                }
            }
        }
        public void CollectLongConverterLines()
        {
            LongConverterLines.AddRange(adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-RAIN-PIPE").Where(x => x is Line || x is Polyline || x.GetType().ToString() == "Autodesk.AutoCAD.DatabaseServices.ImpCurve"));
            foreach (var e in LongConverterLines)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectDraiDomePipes()
        {
            DraiDomePipes.AddRange(adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-DRAI-DOME-PIPE").Where(x => x is Line || x is Polyline || x.GetType().ToString() == "Autodesk.AutoCAD.DatabaseServices.ImpCurve"));
            foreach (var e in DraiDomePipes)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWrappingPipes()
        {
            var blockNameOfVerticalPipe = "套管";
            WrappingPipes.AddRange(adb.ModelSpace.OfType<BlockReference>()
            .Where(x => x.Layer == "W-BUSH")
            .Where(x => x.GetBlockEffectiveName() == blockNameOfVerticalPipe));
            foreach (var e in WrappingPipes)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        Dictionary<KeyValuePair<object, object>, object> cacheDict = new Dictionary<KeyValuePair<object, object>, object>();
        List<T> FiltEntsByRect<T>(Point3dCollection range, IList<T> ents) where T : Entity
        {
            var kv = new KeyValuePair<object, object>(range, ents);
            if (!cacheDict.TryGetValue(kv, out object obj))
            {
                var ret = FiltByRect(range, ents).Cast<T>().ToList();
                cacheDict[kv] = ret;
                return ret;
            }
            return (List<T>)obj;
        }
        static readonly Regex re = new Regex(@"接(\d+F)屋面雨水斗");
        public static bool HasGravityLabelConnected(string text)
        {
            return re.IsMatch(text);
        }
        public static Match TestGravityLabelConnected(string text)
        {
            return re.Match(text);
        }
        public bool HasGravityLabelConnected(Point3dCollection range, string pipeId)
        {
            var e1 = FiltEntsByRect(range, VerticalPipes).FirstOrDefault(e => GetLabel(e) == pipeId);
            if (e1 == null) return false;
            var ents = FiltEntsByRect(range, VerticalPipes).Where(e => re.IsMatch(GetLabel(e) ?? "")).ToList();
            if (ents.Count == 0) return false;
            var gs = GetLongConverterGroup();
            foreach (var g in gs)
            {
                if (g.Count <= 1) continue;
                if (!g.Contains(e1)) continue;
                foreach (var e3 in g)
                {
                    if (ents.Contains(e3)) return true;
                }
            }
            return false;
        }
        public List<string> GetCondenseVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(CONDENSE_PIPE_PREFIX)).ToList();
        }
        public List<string> GetBalconyVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(BALCONY_PIPE_PREFIX)).ToList();
        }
        public List<string> GetRoofVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(ROOF_RAIN_PIPE_PREFIX)).ToList();
        }
        public List<string> GetVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(ROOF_RAIN_PIPE_PREFIX) || t.StartsWith(BALCONY_PIPE_PREFIX) || t.StartsWith(CONDENSE_PIPE_PREFIX)).ToList();
        }
        public List<string> GetDBText(Point3dCollection pts)
        {
            return _CodeByFeng(pts);
        }
        private List<string> _CodeByFeng(Point3dCollection pts)
        {
            if (DbTextSpatialIndex == null)
            {
                DbTextSpatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(VerticalPipeDBTexts.ToCollection());
            }
            var temps = DbTextSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
            var texts = temps.OfType<DBText>().Select(e => e.TextString);
            if (getDbTexts != null)
            {
                texts = texts.Concat(getDbTexts(pts.ToRect()));
            }
            return texts.Distinct().ToList();
        }
        Func<GRect, IEnumerable<string>> getDbTexts;

        public IEnumerable<Entity> FiltByRect(Point3dCollection range, IEnumerable<Entity> ents)
        {
            var rg = range.ToRect();
            foreach (var e in ents)
            {
                if (BoundaryDict.TryGetValue(e, out GRect r))
                {
                    if (rg.ContainsRect(r))
                    {
                        yield return e;
                    }
                }
            }
        }
        public bool GetCenterOfVerticalPipe(Point3dCollection range, string verticalPipeID, ref Point3d outPt)
        {
            var rg = range.ToRect();
            foreach (var pipe in VerticalPipes)
            {
                VerticalPipeToLabelDict.TryGetValue(pipe, out string id);
                if (id == verticalPipeID)
                {
                    var bd = BoundaryDict[pipe];
                    if (rg.ContainsRect(bd))
                    {
                        outPt = bd.Center.ToPoint3d();
                        return true;
                    }
                }
            }
            {
                var bd = getVPipeBoundary(range.ToRect(), verticalPipeID);
                if (bd.IsValid)
                {
                    outPt = bd.Center.ToPoint3d();
                    return true;
                }
            }
            return false;
        }
        public ThWSDOutputType GetPipeOutputType(Point3dCollection range, string verticalPipeID)
        {
            ThWSDOutputType outputType = new ThWSDOutputType();
            outputType.OutputType = GetOutputType(range, verticalPipeID, out bool hasDrivePipe);
            outputType.HasDrivePipe = hasDrivePipe;
            if (outputType.OutputType == RainOutputTypeEnum.WaterWell)
            {
                var dn = GetWaterWellDNValue(verticalPipeID, range);
                if (dn != null)
                {
                    outputType.Label = dn;
                }
            }
            return outputType;
        }


        private IEnumerable<Entity> FiltEntityByRange(GRect range, IEnumerable<Entity> ents)
        {
            foreach (var e in ents)
            {
                if (BoundaryDict.TryGetValue(e, out GRect r))
                {
                    if (range.ContainsRect(r)) yield return e;
                }
            }
        }
        public List<KeyValuePair<Entity, Entity>> CollectVerticalPipesData()
        {
            var lines = this.VerticalPipeLines;
            var pipes = this.VerticalPipes;
            var dbTxtToHLineDict = new Dictionary<Entity, Entity>();
            var linesGroup = new List<List<Entity>>();
            var groups = new List<List<Entity>>();
            var plDict = new Dictionary<Entity, Polyline>();
            var lineToPipesDict = new ListDict<Entity>();
            CollectDbTxtToLbLines(dbTxtToHLineDict, VerticalPipeDBTexts, VerticalPipeLines);
            GroupLines(lines, linesGroup, 10);
            var pls1 = new List<Polyline>();
            var pls2 = new List<Polyline>();
            foreach (var e in this.VerticalPipes)
            {
                var bd = this.BoundaryDict[e];
                var pl = PolylineTools.CreatePolygon(bd.Center, 4, bd.Radius);
                plDict[e] = pl;
                pls1.Add(pl);
            }
            foreach (var e in this.VerticalPipeLines)
            {
                var pl = (e as Line).Buffer(10);
                plDict[e] = pl;
                pls2.Add(pl);
            }
            var si = ThRainSystemService.BuildSpatialIndex(pls1);
            foreach (var pl2 in pls2)
            {
                foreach (var pl1 in si.SelectCrossingPolygon(pl2).Cast<Polyline>().ToList())
                {
                    var pipe = this.VerticalPipes[pls1.IndexOf(pl1)];
                    var line = this.VerticalPipeLines[pls2.IndexOf(pl2)];
                    lineToPipesDict.Add(line, pipe);
                }
            }
            {
                var totalList = new List<Entity>();
                totalList.AddRange(this.VerticalPipeDBTexts);
                totalList.AddRange(this.VerticalPipes);
                totalList.AddRange(this.VerticalPipeLines);
                var pairs = new List<KeyValuePair<Entity, Entity>>();
                foreach (var kv in dbTxtToHLineDict) pairs.Add(kv);
                lineToPipesDict.ForEach((e, l) => { l.ForEach(o => pairs.Add(new KeyValuePair<Entity, Entity>(e, o))); });
                MakePairs(linesGroup, pairs);
                GroupByBFS(groups, totalList, pairs);
                foreach (var g in groups)
                {
                    var targetPipes = SortEntitiesBy2DSpacePosition(g.Where(e => this.VerticalPipes.Contains(e))).ToList();
                    var targetTexts = SortEntitiesBy2DSpacePosition(g.Where(e => this.VerticalPipeDBTexts.Contains(e))).ToList();
                    if (targetPipes.Count == targetTexts.Count && targetTexts.Count > 0)
                    {
                        setVisibilities(targetPipes, targetTexts);
                    }
                }
                return pairs;
            }
        }
        public IEnumerable<KeyValuePair<Entity, Entity>> EnumerateDbTxtToLbLine(List<DBText> dbTxts, List<Entity> lblines)
        {
            foreach (var e1 in dbTxts)
            {
                foreach (var e2 in lblines)
                {
                    if (e2 is Line line)
                    {
                        var seg = line.ToGLineSegment();
                        if (seg.IsHorizontal(10))
                        {
                            var c1 = this.BoundaryDict[e1].Center;
                            var c2 = this.BoundaryDict[e2].Center;
                            if (c1.Y > c2.Y && GeoAlgorithm.Distance(c1, c2) < 500)
                            {
                                yield return new KeyValuePair<Entity, Entity>(e1, e2);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void CollectDbTxtToLbLines(Dictionary<Entity, Entity> dbTxtToHLineDict, List<DBText> dbTxts, List<Entity> lblines)
        {
            foreach (var kv in EnumerateDbTxtToLbLine(dbTxts, lblines)) dbTxtToHLineDict[kv.Key] = kv.Value;
        }
        public static void MakePairs(List<List<Entity>> linesGroup, List<KeyValuePair<Entity, Entity>> pairs)
        {
            foreach (var g in linesGroup) for (int i = 1; i < g.Count; i++) pairs.Add(new KeyValuePair<Entity, Entity>(g[i - 1], g[i]));
        }

        public void LabelEnts()
        {
            LabelWRainLinesAndVerticalPipes();
            LabelCondensePipes();
            LabelFloorDrains();
            LabelWrappingPipes();
            LabelWaterPorts();
            LabelRainPortSymbols();
            LabelRainPortLinesAndTexts();
            LabelWaterWells();
            LabelFloorDrainsWrappingPipe();
            LabelWaterWellsWrappingPipe();
        }
        public List<List<Entity>> LabelFloorDrainsWrappingPipe()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.FloorDrains);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            pairs.AddRange(WRainLinesToFloorDrains);
            pairs.AddRange(WRainLinesToWrappingPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            var lines = new HashSet<Entity>(sv.WRainLines);
            var fds = new HashSet<Entity>(sv.FloorDrains);
            var wps = new HashSet<Entity>(sv.WrappingPipes);
            foreach (var g in groups)
            {
                if (g.Count < 3) continue;
                if (!g.Any(e => lines.Contains(e)) || !g.Any(e => fds.Contains(e)) || !g.Any(e => wps.Contains(e))) continue;
                wrappingEnts.AddRange(g.Where(e => fds.Contains(e)));
            }
            return groups;
        }
        public List<List<Entity>> LabelWaterWellsWrappingPipe()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.WaterWells);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            pairs.AddRange(WRainLinesToWaterWells);
            pairs.AddRange(WRainLinesToWrappingPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            var lines = new HashSet<Entity>(sv.WRainLines);
            var wells = new HashSet<Entity>(sv.WaterWells);
            var wps = new HashSet<Entity>(sv.WrappingPipes);
            foreach (var g in groups)
            {
                if (g.Count < 3) continue;
                if (!g.Any(e => lines.Contains(e)) || !g.Any(e => wells.Contains(e)) || !g.Any(e => wps.Contains(e))) continue;
                wrappingEnts.AddRange(g.Where(e => wells.Contains(e)));
            }
            return groups;
        }
        public List<KeyValuePair<Entity, Entity>> WRainLinesToFloorDrains = new List<KeyValuePair<Entity, Entity>>();
        public List<KeyValuePair<Entity, Entity>> WRainLinesToWaterWells = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LabelFloorDrains()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.FloorDrains);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToFloorDrains.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.FloorDrains, 1));
            pairs.AddRange(WRainLinesToFloorDrains);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.WRainLines.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
            }
            return groups;
        }
        HashSet<string> waterWellLabels = new HashSet<string>();
        List<KeyValuePair<Entity, string>> WaterWellToPipeId = new List<KeyValuePair<Entity, string>>();
        public void LabelWaterWells()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.WaterWells);
            WRainLinesToWaterWells.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.WaterWells, 10));
            pairs.AddRange(WRainLinesToWaterWells);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            sv.LabelGroups(groups);
            var f = ThRainSystemService.BuildSpatialIndexLazy(sv.WRainLines);
            foreach (var well in sv.WaterWells.ToList())
            {
                var pl = sv.CreatePolygon(well, 6, 100);
                foreach (var line in f(pl))
                {
                    var lb = GetLabel(line);
                    if (lb != null)
                    {
                        waterWellLabels.Add(lb);
                        WaterWellToPipeId.Add(new KeyValuePair<Entity, string>(well, lb));
                    }
                }
            }
            foreach (var well in sv.WaterWells.Where(e => !sv.VerticalPipeToLabelDict.ContainsKey(e)).ToList())
            {
                var pl = sv.CreatePolygon(well, 6, 1500);
                foreach (var line in f(pl))
                {
                    var lb = GetLabel(line);
                    if (lb != null)
                    {
                        waterWellLabels.Add(lb);
                        WaterWellToPipeId.Add(new KeyValuePair<Entity, string>(well, lb));
                    }
                }
            }
        }
        public bool SetLabel(Entity e, string lb, bool force = false)
        {
            if (force)
            {
                VerticalPipeToLabelDict[e] = lb;
                return true;
            }
            if (GetLabel(e) == null)
            {
                VerticalPipeToLabelDict[e] = lb;
                return true;
            }
            return false;
        }
        public string GetLabel(Entity e)
        {
            VerticalPipeToLabelDict.TryGetValue(e, out string lb); return lb;
        }

        public List<List<Entity>> RainPortsGroups = new List<List<Entity>>();
        public void LabelWaterPorts()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.VerticalPipeLines);
            totalList.AddRange(sv.ConnectToRainPortDBTexts);
            totalList.AddRange(sv.ConnectToRainPortSymbols);
            ThRainSystemService.MakePairs(ThRainSystemService.GroupLines(sv.VerticalPipeLines), pairs);
            pairs.AddRange(sv.EnumerateDbTxtToLbLine(sv.ConnectToRainPortDBTexts, sv.VerticalPipeLines));
            pairs.AddRange(sv.EnumerateEntities(sv.VerticalPipeLines, sv.ConnectToRainPortSymbols, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            var enumerateEnts = sv.EnumerateEntities(sv.WRainLines);
            RainPortsGroups = groups;
            foreach (var g in groups)
            {
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.ConnectToRainPortSymbols.Contains(e))
                    {
                        {
                            foreach (var kv in enumerateEnts(new List<Entity>() { e }, 10))
                            {
                                if (sv.VerticalPipeToLabelDict.TryGetValue(kv.Key, out lb))
                                {
                                    foreach (var _e in g)
                                    {
                                        if (!sv.VerticalPipeToLabelDict.ContainsKey(_e))
                                        {
                                            sv.VerticalPipeToLabelDict[_e] = lb;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }
        List<List<Entity>> LongConverterGroups;
        public List<List<Entity>> GetLongConverterGroup()
        {
            if (LongConverterGroups == null)
            {
                var sv = this;
                var pairs = new List<KeyValuePair<Entity, Entity>>();
                var groups = new List<List<Entity>>();
                var totalList = new List<Entity>();
                totalList.AddRange(sv.LongConverterLines);
                totalList.AddRange(sv.VerticalPipes);
                ThRainSystemService.MakePairs(GetLongConverterLinesGroup(), pairs);
                pairs.AddRange(sv.EnumerateEntities(sv.LongConverterLines, sv.VerticalPipes.Cast<Entity>().ToList(), 1));
                ThRainSystemService.GroupByBFS(groups, totalList, pairs);
                LongConverterGroups = groups;
            }
            return LongConverterGroups;
        }
        public List<KeyValuePair<Entity, Entity>> WRainLinesToVerticalPipes = new List<KeyValuePair<Entity, Entity>>();
        public List<KeyValuePair<Entity, Entity>> WRainLinesToCondensePipes = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LabelCondensePipes()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.CondensePipes);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToCondensePipes.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.CondensePipes, 1));
            pairs.AddRange(WRainLinesToCondensePipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.WRainLines.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
            }
            return groups;
        }
        public List<KeyValuePair<Entity, Entity>> WRainLinesToWrappingPipes = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LabelWrappingPipes()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.WrappingPipes);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToWrappingPipes.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.WrappingPipes.Cast<Entity>().ToList(), 1));
            pairs.AddRange(WRainLinesToWrappingPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.WRainLines.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
            }
            return groups;
        }
        public List<List<Entity>> LabelWRainLinesAndVerticalPipesGroups = new List<List<Entity>>();
        public void LabelWRainLinesAndVerticalPipes()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.VerticalPipes);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToVerticalPipes.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.VerticalPipes, 100));
            pairs.AddRange(WRainLinesToVerticalPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.VerticalPipes.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
                var pipes = g.Where(e =>
                {
                    if (VerticalPipes.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            if (!string.IsNullOrWhiteSpace(lb)) return true;
                        }
                    }
                    return false;
                })
                .Where(e => BoundaryDict[e].IsValid)
                .ToList();
                if (pipes.Count > 1)
                {
                    LongConverterPipes.AddRange(pipes);
                }
            }
            LabelWRainLinesAndVerticalPipesGroups = groups;
        }
        public HashSet<Entity> LongConverterPipes = new HashSet<Entity>();
        List<List<Entity>> WRainLinesGroup;
        private List<List<Entity>> GetWRainLinesGroup()
        {
            WRainLinesGroup ??= ThRainSystemService.GroupLines_SkipCrossingCases(WRainLines);
            return WRainLinesGroup;
        }
        public static List<List<Entity>> GroupLines_SkipCrossingCases(List<Entity> lines)
        {
            var linesGroup = new List<List<Entity>>();
            GroupLines_SkipCrossingCases(lines, linesGroup);
            return linesGroup;
        }
        public static void GroupLines_SkipCrossingCases(List<Entity> lines, List<List<Entity>> linesGroup)
        {
            var pairs = new List<KeyValuePair<int, int>>();
            var bfs = lines.Select(e => (TryConvertToLine(e))?.Buffer(10)).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            for (int i = 0; i < bfs.Count; i++)
            {
                Polyline bf = bfs[i];
                if (bf != null)
                {
                    var lst = si.SelectCrossingPolygon(bf).Cast<Polyline>().Select(e => bfs.IndexOf(e)).Where(j => i < j).ToList();
                    lst.ForEach(j =>
                    {
                        var line1 = lines[i];
                        var line2 = lines[j];
                        if (GeoAlgorithm.TryConvertToLineSegment(line1, out GLineSegment seg1) && GeoAlgorithm.TryConvertToLineSegment(line2, out GLineSegment seg2))
                        {
                            if (GeoAlgorithm.IsLineConnected(line1, line2))
                            {
                                pairs.Add(new KeyValuePair<int, int>(i, j));
                            }
                        }
                    });
                }
            }
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs.ToArray(),
                TotalCount = lines.Count,
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                linesGroup.Add(l.Select(i => lines[i]).ToList());
            });
        }
        List<List<Entity>> LongConverterLinesGroup;
        private List<List<Entity>> GetLongConverterLinesGroup()
        {
            LongConverterLinesGroup ??= ThRainSystemService.GroupLines(LongConverterLines);
            return LongConverterLinesGroup;
        }
        public Func<List<Entity>, double, IEnumerable<KeyValuePair<Entity, Entity>>> EnumerateEntities(List<Entity> lines)
        {
            var mps1 = lines.Select(e => new KeyValuePair<Entity, Polyline>(e, (TryConvertToLine(e))?.Buffer(10))).ToList();
            var bfs = mps1.Select(kv => kv.Value).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            IEnumerable<KeyValuePair<Entity, Entity>> f(List<Entity> wells, double expand)
            {
                var mps2 = wells.Select(e => new KeyValuePair<Entity, Polyline>(e, this.CreatePolygon(e, expand: expand))).ToList();
                var pls = mps2.Select(kv => kv.Value).ToList();
                foreach (var pl in pls)
                {
                    foreach (var bf in si.SelectCrossingPolygon(pl).Cast<Polyline>().ToList())
                    {
                        var line = mps1.First(kv => kv.Value == bf).Key;
                        var well = mps2.First(kv => kv.Value == pl).Key;
                        yield return new KeyValuePair<Entity, Entity>(line, well);
                    }
                }
            }
            return f;
        }
        public void LabelGroups(List<List<Entity>> groups)
        {
            LabelGroups(this, groups);
        }
        public void LabelRainPortSymbols()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.ConnectToRainPortSymbols);
            pairs.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.ConnectToRainPortSymbols, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            sv.LabelGroups(groups);
        }
        public bool IsRainPort(string pipeId)
        {
            var ok = VerticalPipeToLabelDict.Any(kv => kv.Value == pipeId && ConnectToRainPortDBTexts.Contains(kv.Key));
            if (!ok)
            {
                ok = VerticalPipeToLabelDict.Any(kv => kv.Value == pipeId && ConnectToRainPortSymbols.Contains(kv.Key));
            }
            return ok;
        }
        public bool IsWaterWell(string pipeId)
        {
            return waterWellLabels.Contains(pipeId) || VerticalPipeToLabelDict.Any(kv => kv.Value == pipeId && WaterWells.Contains(kv.Key));
        }
        public Entity GetWaterWell(string pipeId, List<Entity> wells)
        {
            var e = VerticalPipeToLabelDict.FirstOrDefault(kv => kv.Value == pipeId && WaterWells.Contains(kv.Key)).Key;
            return e;
        }
        public string GetWaterWellDNValue(string pipeId, Point3dCollection range)
        {
            string ret = null;
            var wells = GetWaterWells(range);
            var well = GetWaterWell(pipeId, wells);
            if (well != null)
            {
                WaterWellDNs.TryGetValue(well, out ret);
            }
            if (string.IsNullOrEmpty(ret))
            {
                foreach (var kv in WaterWellToPipeId)
                {
                    if (kv.Value == pipeId && wells.Contains(kv.Key))
                    {
                        WaterWellDNs.TryGetValue(kv.Key, out ret);
                        return ret;
                    }
                }
            }
            return ret;
        }
        public void LabelRainPortLinesAndTexts()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.VerticalPipeLines);
            totalList.AddRange(sv.ConnectToRainPortDBTexts);
            totalList.AddRange(sv.ConnectToRainPortSymbols);
            ThRainSystemService.MakePairs(ThRainSystemService.GroupLines(sv.VerticalPipeLines), pairs);
            pairs.AddRange(sv.EnumerateDbTxtToLbLine(sv.ConnectToRainPortDBTexts, sv.VerticalPipeLines));
            pairs.AddRange(sv.EnumerateEntities(sv.VerticalPipeLines, sv.ConnectToRainPortSymbols, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            sv.LabelGroups(groups);
        }
        private static void LabelGroups(ThRainSystemService sv, List<List<Entity>> groups)
        {
            foreach (var g in groups)
            {
                string lb = null;
                foreach (var e in g) if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb)) break;
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                        {
                            sv.VerticalPipeToLabelDict[e] = lb;
                        }
                    }
                }
            }
        }
        public IEnumerable<KeyValuePair<Entity, Entity>> EnumerateEntities(List<Entity> lines, List<Entity> wells, double expand)
        {
            var mps1 = lines.Select(e => new KeyValuePair<Entity, Polyline>(e, (TryConvertToLine(e))?.Buffer(10))).ToList();
            var mps2 = wells.Select(e => new KeyValuePair<Entity, Polyline>(e, this.CreatePolygon(e, expand: expand))).ToList();
            var bfs = mps1.Select(kv => kv.Value).ToList();
            var pls = mps2.Select(kv => kv.Value).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            foreach (var pl in pls)
            {
                foreach (var bf in si.SelectCrossingPolygon(pl).Cast<Polyline>().ToList())
                {
                    var line = mps1.First(kv => kv.Value == bf).Key;
                    var well = mps2.First(kv => kv.Value == pl).Key;
                    yield return new KeyValuePair<Entity, Entity>(line, well);
                }
            }
        }

        public static void GroupByBFS(List<List<Entity>> groups, List<Entity> totalList, List<KeyValuePair<Entity, Entity>> pairs)
        {
            var dict = new ListDict<Entity>();
            var h = new BFSHelper2<Entity>()
            {
                Pairs = pairs.ToArray(),
                Items = totalList.ToArray(),
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_start, ents) =>
            {
                groups.Add(ents);
            });
        }
        public static List<List<Entity>> GroupLines(List<Entity> lines)
        {
            var linesGroup = new List<List<Entity>>();
            GroupLines(lines, linesGroup, 10);
            return linesGroup;
        }
        static Line TryConvertToLine(Entity e)
        {
            var line = _TryConvertToLine(e);
            if (line != null)
            {
                if (line.Length > 0) return line;
            }
            return null;
        }
        static Line _TryConvertToLine(Entity e)
        {
            var r = e as Line;
            if (r != null) return r;
            if (e.GetType().ToString() == "Autodesk.AutoCAD.DatabaseServices.ImpCurve")
            {
                GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg);
                return new Line() { StartPoint = seg.StartPoint.ToPoint3d(), EndPoint = seg.EndPoint.ToPoint3d() };
            }
            return r;
        }
        public static List<List<Polyline>> GroupPolylines(List<Polyline> lines)
        {
            var linesGroup = new List<List<Polyline>>();
            GroupPolylines(lines, linesGroup);
            return linesGroup;
        }
        public static void GroupPolylines(List<Polyline> lines, List<List<Polyline>> linesGroup)
        {
            if (lines.Count == 0) return;
            var pairs = new List<KeyValuePair<int, int>>();
            var si = ThRainSystemService.BuildSpatialIndex(lines);
            for (int i = 0; i < lines.Count; i++)
            {
                var pl = lines[i];
                var lst = si.SelectCrossingPolygon(pl).Cast<Polyline>().Select(e => lines.IndexOf(e)).Where(j => i < j).ToList();
                lst.ForEach(j => pairs.Add(new KeyValuePair<int, int>(i, j)));
            }
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs.ToArray(),
                TotalCount = lines.Count,
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                linesGroup.Add(l.Select(i => lines[i]).ToList());
            });
        }
        public static void GroupLines(List<Entity> lines, List<List<Entity>> linesGroup, double bufferDistance)
        {
            if (lines.Count == 0) return;
            var pairs = new List<KeyValuePair<int, int>>();
            var bfs = lines.Select(e => (TryConvertToLine(e))?.Buffer(bufferDistance)).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            for (int i = 0; i < bfs.Count; i++)
            {
                Polyline bf = bfs[i];
                if (bf != null)
                {
                    var lst = si.SelectCrossingPolygon(bf).Cast<Polyline>().Select(e => bfs.IndexOf(e)).Where(j => i < j).ToList();
                    lst.ForEach(j => pairs.Add(new KeyValuePair<int, int>(i, j)));
                }
            }
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs.ToArray(),
                TotalCount = lines.Count,
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                linesGroup.Add(l.Select(i => lines[i]).ToList());
            });
        }
        private void setVisibilities(List<Entity> targetPipes, List<Entity> targetTexts)
        {
            var dnProp = "可见性1";
            for (int i = 0; i < targetPipes.Count; i++)
            {
                var pipeEnt = targetPipes[i];
                var dbText = targetTexts[i];
                var label = (dbText as DBText)?.TextString;
                if (label != null)
                {
                    var dnText =/* (pipeEnt.ObjectId.IsValid ? pipeEnt.GetCustomPropertiyStrValue(dnProp) : null) ?? */"DN100";
                    VerticalPipeLabelToDNDict[label] = dnText;
                    VerticalPipeToLabelDict[pipeEnt] = label;
                }
            }
        }
        public static IEnumerable<Geometry> SortGeometrysBy2DSpacePosition(IEnumerable<Geometry> list)
        {
            return from e in list
                   let bd = e.EnvelopeInternal
                   orderby bd.MinX ascending
                   orderby bd.MaxY descending
                   select e;
        }


        public IEnumerable<T> SortEntitiesBy2DSpacePosition<T>(IEnumerable<T> list) where T : Entity
        {
            return from e in list
                   let bd = BoundaryDict[e]
                   orderby bd.MinX ascending
                   orderby bd.MaxY descending
                   select e;
        }
        public void FindShortConverters()
        {
            var pipeBoundaries = (from pipe in VerticalPipes
                                  let boundary = BoundaryDict[pipe]
                                  where !Equals(boundary, default(GRect))
                                  select new { pipe, boundary }).ToList();
            var d = VerticalPipeToLabelDict;
            for (int i = 0; i < pipeBoundaries.Count; i++)
            {
                for (int j = i + 1; j < pipeBoundaries.Count; j++)
                {
                    var bd1 = pipeBoundaries[i].boundary;
                    var bd2 = pipeBoundaries[j].boundary;
                    if (!bd1.IsValid || !bd2.IsValid) continue;
                    if (!bd1.EqualsTo(bd2, 5)) continue;
                    var dis = GeoAlgorithm.Distance(bd1.Center, bd2.Center);
                    var dis1 = (bd1.Width + bd2.Width) / 2;
                    if (dis <= 5 + dis1)
                    {
                        var pipe1 = pipeBoundaries[i].pipe;
                        var pipe2 = pipeBoundaries[j].pipe;
                        ShortConverters.Add(new Tuple<Entity, Entity>(pipe1, pipe2));
                    }
                }
            }
            foreach (var item in ShortConverters)
            {
                string v;
                if (!d.TryGetValue(item.Item1, out v) && d.TryGetValue(item.Item2, out v))
                {
                    d[item.Item1] = v;
                }
                else if (!d.TryGetValue(item.Item2, out v) && d.TryGetValue(item.Item1, out v))
                {
                    d[item.Item2] = v;
                }
            }
        }
        IEnumerable<T> EnumerateEntities<T>() where T : Entity
        {
            return adb.ModelSpace.OfType<T>()
            .Concat(SingleTianzhengElements.OfType<T>())
            .Concat(ExplodedEntities.OfType<T>())
            .Where(e => e != null && e.ObjectId.IsValid)
            .Distinct();
        }
        public void CollectVerticalPipes()
        {
            var pipes = new List<Entity>();
            {
                var blockNameOfVerticalPipe = "带定位立管";
                pipes.AddRange(adb.ModelSpace.OfType<BlockReference>()
                .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                .Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName() == blockNameOfVerticalPipe));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                    if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in pipes)
                {
                    BoundaryDict[e] = getRealBoundaryForPipe(e);
                }
            }
            {
                var ents = adb.ModelSpace.OfType<Entity>()
                .Where(x => IsTianZhengElement(x))
                .Where(x => x.Layer == "W-RAIN-EQPM")
                .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Count() == 1);
                foreach (var e in ents)
                {
                    BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
                pipes.AddRange(ents);
            }
            {
                var ents = adb.ModelSpace.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Layer == "W-RAIN-PIPE-RISR").ToList();
                foreach (var e in ents)
                {
                    BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
                pipes.AddRange(ents);
            }
            {
                var lst = adb.ModelSpace.OfType<Entity>()
                .Where(x => IsTianZhengElement(x))
                .Where(x => x.Layer == "WP_KTN_LG").ToList();
                foreach (var c in lst)
                {
                    if (!BoundaryDict.ContainsKey(c)) BoundaryDict[c] = GeoAlgorithm.GetBoundaryRect(c);
                }
                pipes.AddRange(lst);
            }
            {
                var q = EnumerateEntities<Circle>()
                .Where(c => c.Radius >= 50 && c.Radius <= 200
                && (c.Layer.Contains("W-")
                && c.Layer.Contains("-EQPM")
                && c.Layer != "W-EQPM"
                && c.Layer != "W-WSUP-EQPM")
                );
                var lst = q.ToList();
                foreach (var c in lst)
                {
                    if (!BoundaryDict.ContainsKey(c)) BoundaryDict[c] = GeoAlgorithm.GetBoundaryRect(c);
                }
                pipes.AddRange(lst);
            }
            {
                var q = EnumerateEntities<BlockReference>()
                .Where(x => x.Name == "*U398");
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    var et = ents.FirstOrDefault(e =>
                    {
                        var m = Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width);
                        return m == 110 || m == 88;
                    });
                    if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                var lst = q.ToList();
                lst.AddRange(vps.OfType<BlockReference>());
                foreach (var e in lst)
                {
                    if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = getRealBoundaryForPipe(e);
                }
                pipes.AddRange(lst);
                pipes.AddRange(vps);
            }
            {
                var q = EnumerateEntities<BlockReference>()
                .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                .Where(x => x.Name == "$LIGUAN");
                var lst = q.ToList();
                foreach (var e in lst)
                {
                    if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
                pipes.AddRange(lst);
            }
            VerticalPipes.AddRange(pipes.Distinct());
            foreach (var p in VerticalPipes)
            {
                if (!BoundaryDict.ContainsKey(p))
                {
                    BoundaryDict[p] = GeoAlgorithm.GetBoundaryRect(p);
                }
            }
        }
        public static IEnumerable<string> GetRoofLabels(IEnumerable<string> labels)
        {
            return labels.Where(x => x.StartsWith(ROOF_RAIN_PIPE_PREFIX)).OrderBy(x => x);
        }
        public static IEnumerable<string> GetBalconyLabels(IEnumerable<string> labels)
        {
            return labels.Where(x => x.StartsWith(BALCONY_PIPE_PREFIX)).OrderBy(x => x);
        }
        public static IEnumerable<string> GetCondenseLabels(IEnumerable<string> labels)
        {
            return labels.Where(x => x.StartsWith(CONDENSE_PIPE_PREFIX)).OrderBy(x => x);
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return false;
            return label.StartsWith(ROOF_RAIN_PIPE_PREFIX) || label.StartsWith(BALCONY_PIPE_PREFIX) || label.StartsWith(CONDENSE_PIPE_PREFIX);
        }
        public void CollectVerticalPipeDBTexts()
        {
            var q = adb.ModelSpace.OfType<DBText>()
            .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE);
            {
                var lst = new List<DBText>();
                foreach (var e in adb.ModelSpace.OfType<Entity>().ToList())
                {
                    if (IsTianZhengElement(e))
                    {
                        lst.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>());
                    }
                }
                q = q.Concat(lst);
            }
            {
                var lst = new List<DBText>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(br);
                    if (r.Width > 10000 && r.Width < 60000)
                    {
                        foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                        {
                            if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                var lst3 = e.ExplodeToDBObjectCollection()
                                .OfType<Entity>()
                                .Where(x => ThRainSystemService.IsTianZhengElement(x))
                                .SelectMany(x => x.ExplodeToDBObjectCollection().OfType<DBText>())
                                .ToList();
                                lst.AddRange(lst3);
                            }
                        }
                    }
                }
                q = q.Concat(lst);
            }
            {
                var lst = new List<DBText>();
                foreach (var e in adb.ModelSpace.OfType<Entity>().ToList())
                {
                    if (ThRainSystemService.IsTianZhengElement(e))
                    {
                        var lst3 = e.ExplodeToDBObjectCollection()
                        .OfType<Entity>()
                        .Where(x => ThRainSystemService.IsTianZhengElement(x))
                        .SelectMany(x => x.ExplodeToDBObjectCollection().OfType<DBText>())
                        .ToList();
                        lst.AddRange(lst3);
                    }
                }
                q = q.Concat(lst);
            }
            q = q.Concat(txts);
            {
                IEnumerable<DBText> f()
                {
                    foreach (var e in adb.ModelSpace.OfType<DBText>().ToList())
                    {
                        if (ThRainSystemService.IsWantedLabelText(e.TextString))
                        {
                            yield return e;
                        }
                    }
                }
                q = q.Concat(f());
            }
            VerticalPipeDBTexts.AddRange(
            q.Distinct().Where(t => IsWantedLabelText(t.TextString))
            );
            foreach (var e in VerticalPipeDBTexts)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                VerticalPipeDBTextDict[e] = e.TextString;
            }
        }
        public void CollectVerticalPipeLines()
        {
            IEnumerable<Entity> q;
            {
                var lines = new List<Entity>();
                foreach (var e in EnumerateEntities<Entity>())
                {
                    if (e is Line) lines.Add(e);
                    else if (e is Polyline)
                    {
                        lines.AddRange(e.ExplodeToDBObjectCollection().OfType<Line>());
                    }
                }
                lines = lines.Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE).ToList();
                q = lines;
            }
            {
                var lst = new List<Line>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(br);
                    if (r.Width > 10000 && r.Width < 60000)
                    {
                        foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                        {
                            if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                var lst3 = e.ExplodeToDBObjectCollection()
                                .OfType<Line>()
                                .ToList();
                                foreach (var t in lst3)
                                {
                                    lst.Add(t);
                                }
                            }
                        }
                    }
                }
                foreach (var e in adb.ModelSpace.OfType<Entity>().ToList())
                {
                    if (ThRainSystemService.IsTianZhengElement(e))
                    {
                        var lst3 = e.ExplodeToDBObjectCollection()
                        .OfType<Line>()
                        .ToList();
                        foreach (var t in lst3)
                        {
                            lst.Add(t);
                        }
                    }
                }
                q = q.Concat(lst);
            }
            VerticalPipeLines.AddRange(q.OfType<Line>().Where(x => x.Length > 0).Distinct());
            foreach (var e in VerticalPipeLines)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
    }
}