using System;
using System.Collections.Generic;
using System.Linq;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPWSS.Uitl;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.Pipe.Service
{
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Dreambuild.AutoCAD;
    using ThMEPWSS.Pipe.Engine;
    using ThMEPWSS.Pipe.Model;
    using ThUtilExtensionsNs;
    public class Ref<T>
    {
        public T Value;
        public Ref(T value)
        {
            this.Value = value;
        }
        public Ref() { }
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
            while (queue.Any())
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
    public class FlagsArray<T>
    {
        public T[] Items { get; }
        bool[] flags;
        int cur;
        public FlagsArray(T[] items)
        {
            Items = items;
            flags = new bool[items.Length];
        }
        public void Clear()
        {
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = false;
            }
            cur = 0;
        }
        public void SetFlag()
        {
            flags[cur] = true;
        }
        public bool IsVisited(int i)
        {
            return flags[i];
        }
        public IEnumerable<T> Filt(IEnumerable<int> list)
        {
            foreach (var i in list)
            {
                if (!flags[i]) yield return Items[i];
            }
        }
        public IEnumerable<KeyValuePair<int, T>> Yield()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                cur = i;
                if (!flags[i]) yield return new KeyValuePair<int, T>(i, Items[i]);
            }
        }
        public int GetTrueCount()
        {
            return flags.Where(x => x).Count();
        }
        public int GetFalseCount()
        {
            return flags.Where(x => !x).Count();
        }
        public bool TryGetFirstFalseItem(out T value)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                var flag = flags[i];
                if (!flag)
                {
                    value = Items[i];
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
    public class ListDict<T>
    {
        Dictionary<T, List<T>> dict = new Dictionary<T, List<T>>();
        public void Add(T item, IEnumerable<T> items)
        {
            if (dict.TryGetValue(item, out List<T> list))
            {
                list.AddRange(items);
            }
            else
            {
                dict[item] = items.ToList();
            }
        }
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
        public List<T> this[T item]
        {
            get
            {
                dict.TryGetValue(item, out List<T> list);
                return list;
            }
        }
        public IEnumerable<T> Get(T item)
        {
            dict.TryGetValue(item, out List<T> list);
            return list == null ? Enumerable.Empty<T>() : list;
        }
        public Dictionary<T, List<T>>.KeyCollection Keys => dict.Keys;
        public Dictionary<T, List<T>>.ValueCollection Values => dict.Values;
    }
    public class ThRainSystemService
    {

        const string ROOF_RAIN_PIPE_PREFIX = "Y1";
        const string BALCONY_PIPE_PREFIX = "Y2";
        const string CONDENSE_PIPE_PREFIX = "N1";

        public AcadDatabase adb;
        public Dictionary<Entity, ThWGRect> BoundaryDict = new Dictionary<Entity, ThWGRect>();
        public List<Entity> WaterRainPipeLines;
        public List<DBText> VerticalPipeDBTexts;
        public List<BlockReference> VerticalPipes;
        public Dictionary<string, string> VerticalPipeLabelToDNDict = new Dictionary<string, string>();
        public Dictionary<Entity, string> EntityToLabelDict = new Dictionary<Entity, string>();
        public List<Tuple<Entity, Entity>> ShortConverters = new List<Tuple<Entity, Entity>>();
        public List<Entity> LongConverterLines = new List<Entity>();
        public ListDict<Entity> LongConverterToPipesDict = new ListDict<Entity>();
        public ListDict<Entity> LongConverterToLongConvertersDict = new ListDict<Entity>();
        public List<BlockReference> WrappingPipes = new List<BlockReference>();
        public List<Entity> DraiDomePipes = new List<Entity>();
        public Point3dCollection CurrentSelectionExtent { get; set; }
        private ThCADCoreNTSSpatialIndex DbTextSpatialIndex;
        private List<Extents3d> AllGravityWaterBucketExtents;
        private List<Extents3d> AllSideWaterBucketExtents;
        private ThCADCoreNTSSpatialIndex AllGravityWaterBucketSpatialIndex;
        bool inited;
        public void InitCache()
        {
            if (inited) return;
            CollectWaterRainPipeLines();
            CollectVerticalPipeDBTexts();
            CollectVerticalPipes();

            CollectLongConverterLines();
            CollectDraiDomePipes();
            CollectWrappingPipes();
            inited = true;
        }
        public void CollectData()
        {
            InitCache();
            CollectVerticalPipeData();
            CollectShortConverters();
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
            WrappingPipes.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-BUSH"));
            foreach (var e in WrappingPipes)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public List<Extents3d> GetRelatedGravityWaterBucket(Point3dCollection range)
        {
            if (AllGravityWaterBucketExtents == null)
            {
                var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                gravityBucketEngine.Recognize(adb.Database, CurrentSelectionExtent);
                var gravities = gravityBucketEngine.Elements;
                AllGravityWaterBucketExtents = gravities.Select(g => g.Outline.GeometricExtents).ToList();
                AllGravityWaterBucketSpatialIndex = new ThCADCoreNTSSpatialIndex(gravities.Select(g => g.Outline).ToList().ToCollection());
            }

            var selected = AllGravityWaterBucketSpatialIndex.SelectCrossingPolygon(range);

            var rst = new List<Extents3d>();
            foreach (Entity e in selected)
            {
                rst.Add(e.GeometricExtents);
            }

            return rst;
        }

        public Pipe.Model.WaterBucketEnum GetRelatedSideWaterBucket(Point3d centerOfPipe)
        {
            if (AllSideWaterBucketExtents == null)
            {
                var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
                sidebucketEngine.Recognize(adb.Database, CurrentSelectionExtent);
                var sides = sidebucketEngine.Elements;
                AllSideWaterBucketExtents = sides.Select(g => g.Outline.GeometricExtents).ToList();
            }

            foreach (var e in AllSideWaterBucketExtents)
            {
                if (e.IsPointIn(centerOfPipe))
                {
                    return WaterBucketEnum.Side;
                }
            }

            return Pipe.Model.WaterBucketEnum.None;
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
            var textEntities = GetDBTextEntities(pts);
            var texts = textEntities.Select(e => (e as DBText).TextString);
            return texts.ToList();
        }
        public List<Entity> GetDBTextEntities(Point3dCollection pts)
        {
            if (DbTextSpatialIndex != null)
            {
                var rst = DbTextSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
                return rst;
            }
            else
            {
                using (var db = Linq2Acad.AcadDatabase.Use(adb.Database))
                {
                    var rst = new List<Entity>();

                    var tvs = new List<TypedValue>();
                    tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(DBText)).DxfName + "," + RXClass.GetClass(typeof(MText)).DxfName));
                    tvs.Add(new TypedValue((int)DxfCode.LayerName, ThWPipeCommon.W_RAIN_NOTE));
                    var sf = new SelectionFilter(tvs.ToArray());

                    var psr = Active.Editor.SelectAll(sf);
                    if (psr.Status == PromptStatus.OK)
                    {
                        foreach (var id in psr.Value.GetObjectIds())
                            rst.Add(db.Element<Entity>(id));
                    }

                    if (pts.Count >= 3)
                    {
                        DbTextSpatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(rst.ToCollection());
                        rst = DbTextSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
                    }

                    return rst;

                }
            }
        }

        public bool GetCenterOfVerticalPipe(Point3dCollection range, string verticalPipeID, ref Point3d outPt)
        {
            foreach (var pipe in VerticalPipes)
            {
                EntityToLabelDict.TryGetValue(pipe, out string id);
                if (id == verticalPipeID)
                {
                    var bd = BoundaryDict[pipe];
                    if (range.ToRect().ContainsRect(bd))
                    {
                        outPt = bd.Center.ToPoint3d();
                        return true;
                    }
                }
            }
            return false;
        }
        public TranslatorTypeEnum GetTranslatorType(Point3dCollection range, string verticalPipeID)
        {
            var rect = range.ToRect();
            return GetTranslatorType(verticalPipeID, rect);
        }
        //todo: unit test
        public TranslatorTypeEnum GetTranslatorType(string verticalPipeID, ThWGRect rect)
        {
            var shortCvts = FiltEntityByRange(rect, ShortConverters.SelectMany(x => new Entity[] { x.Item1, x.Item2 })).ToList();
            foreach (var pipe in FiltEntityByRange(rect, EntityToLabelDict.Keys))
            {
                EntityToLabelDict.TryGetValue(pipe, out string label);
                if (label == verticalPipeID)
                {
                    if (shortCvts.Contains(pipe)) return TranslatorTypeEnum.Short;
                    if (FiltEntityByRange(rect, LongConverterLines).Any(cvt => LongConverterToPipesDict.Get(cvt).Any(p => p == pipe))) return TranslatorTypeEnum.Long;
                    return TranslatorTypeEnum.None;
                }
            }
            return TranslatorTypeEnum.None;
        }

        private IEnumerable<Entity> FiltEntityByRange(ThWGRect range, IEnumerable<Entity> ents)
        {
            foreach (var e in ents)
            {
                if (BoundaryDict.TryGetValue(e, out ThWGRect r))
                {
                    if (range.ContainsRect(r)) yield return e;
                }
            }
        }



        private void CollectVerticalPipeData()
        {
            var used = new HashSet<Entity>();
            foreach (var pipe in VerticalPipes)
            {
                if (used.Contains(pipe)) continue;
                var group = new HashSet<Entity> { pipe };
                Entity curLine = null;
                {
                    var r = BoundaryDict[pipe];
                    foreach (var line in WaterRainPipeLines)
                    {
                        if (!used.Contains(line) && GeoAlgorithm.IsRectCross(r, BoundaryDict[line]))
                        {
                            group.Add(line);
                            used.Add(line);
                            curLine = line;
                            break;
                        }
                    }
                }
                if (curLine != null)
                {
                    var r = BoundaryDict[curLine];
                    foreach (var line in WaterRainPipeLines)
                    {
                        if (!used.Contains(line) && GeoAlgorithm.IsRectCross(r, BoundaryDict[line]))
                        {
                            group.Add(line);
                            used.Add(line);
                        }
                    }
                    foreach (var e in VerticalPipes)
                    {
                        if (!used.Contains(e) && GeoAlgorithm.IsRectCross(r, BoundaryDict[e]))
                        {
                            group.Add(e);
                            used.Add(e);
                        }
                    }
                    var rect = GeoAlgorithm.GetBoundaryRect(group.ToArray());
                    foreach (var e in VerticalPipeDBTexts)
                    {
                        if (!used.Contains(e))
                        {
                            var ok = GeoAlgorithm.IsRectCross(rect, BoundaryDict[e]);
                            if (!ok)
                            {
                                var r3 = BoundaryDict[e];
                                r3 = GeoAlgorithm.ExpandRect(r3, 100);
                                ok = GeoAlgorithm.IsRectCross(rect, r3);
                            }
                            if (ok)
                            {
                                group.Add(e);
                                used.Add(e);
                            }
                        }
                    }
                    var _targetPipes = group.OfType<BlockReference>().OrderBy(e => BoundaryDict[e].LeftTop.X).ThenByDescending(e => BoundaryDict[e].LeftTop.Y).ToList();
                    var _targetTexts = group.OfType<DBText>().OrderBy(e => BoundaryDict[e].LeftTop.X).ThenByDescending(e => BoundaryDict[e].LeftTop.Y).ToList();
                    if (_targetTexts.Count == 0)
                    {
                        var boundary = GeoAlgorithm.GetBoundaryRect(group.ToArray());
                        foreach (var t in VerticalPipeDBTexts)
                        {
                            var bd = BoundaryDict[t];
                            if (Math.Abs(bd.MinY - boundary.MaxY) < 100 && boundary.MinX <= bd.MaxX && boundary.MaxX >= bd.MaxX)
                            {
                                if (!_targetTexts.Contains(t))
                                {
                                    _targetTexts.Add(t);
                                }
                            }
                        }
                    }
                    if (_targetPipes.Count > 0 && _targetPipes.Count == _targetTexts.Count)
                    {
                        var targetPipes = (from e in _targetPipes
                                           let bd = BoundaryDict[e]
                                           orderby bd.MinX ascending
                                           orderby bd.MaxY descending
                                           select e).ToList();
                        var targetTexts = (from e in _targetTexts
                                           let bd = BoundaryDict[e]
                                           orderby bd.MinX ascending
                                           orderby bd.MaxY descending
                                           select e).ToList();
                        var dnProp = "可见性1";
                        for (int i = 0; i < targetPipes.Count; i++)
                        {
                            var pipeEnt = targetPipes[i];
                            var dbText = targetTexts[i];
                            var label = dbText.TextString;
                            var dnText = pipeEnt.GetCustomPropertiyStrValue(dnProp);
                            if (label != null)
                            {
                                VerticalPipeLabelToDNDict[label] = dnText;
                                EntityToLabelDict[pipeEnt] = label;
                            }
                        }
                    }
                }
                used.Add(pipe);
            }
        }

        private void CollectShortConverters()
        {
            var pipeBoundaries = (from pipe in VerticalPipes
                                  let boundary = BoundaryDict[pipe]
                                  where !Equals(boundary, default(ThWGRect))
                                  select new { pipe, boundary }).ToList();
            for (int i = 0; i < pipeBoundaries.Count; i++)
            {
                for (int j = i + 1; j < pipeBoundaries.Count; j++)
                {
                    var bd1 = pipeBoundaries[i].boundary;
                    var bd2 = pipeBoundaries[j].boundary;
                    if (GeoAlgorithm.Distance(bd1.Center, bd2.Center) <= 5 + (bd1.Width + bd2.Width) / 2)
                    {
                        ShortConverters.Add(new Tuple<Entity, Entity>(pipeBoundaries[i].pipe, pipeBoundaries[j].pipe));
                    }
                }
            }
        }

        public void CollectVerticalPipes()
        {
            var blockNameOfVerticalPipe = "带定位立管";
            VerticalPipes = adb.ModelSpace.OfType<BlockReference>()
             .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
             .Where(x => x.ToDataItem().EffectiveName == blockNameOfVerticalPipe)
             .ToList();
            ThWGRect getRealBoundaryForPipe(Entity ent)
            {
                var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                return default;
            }
            foreach (var e in VerticalPipes)
            {
                BoundaryDict[e] = getRealBoundaryForPipe(e);
            }
        }

        public void CollectVerticalPipeDBTexts()
        {
            VerticalPipeDBTexts = adb.ModelSpace.OfType<DBText>()
             .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE)
             .ToList();
            foreach (var e in VerticalPipeDBTexts)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }

        public void CollectWaterRainPipeLines()
        {
            WaterRainPipeLines = adb.ModelSpace.OfType<Line>().Cast<Entity>()
             .Union(adb.ModelSpace.OfType<Polyline>().Cast<Entity>())
             .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE)
             .ToList();
            foreach (var e in WaterRainPipeLines)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
    }
}
namespace ThUtilExtensionsNs
{
    public static class ThDataItemExtensions
    {
        public static ThWGRect ToRect(this Point3dCollection colle)
        {
            if (colle.Count == 0) return default;
            var arr = colle.Cast<Point3d>().ToArray();
            var x1 = arr.Select(p => p.X).Min();
            var x2 = arr.Select(p => p.X).Max();
            var y1 = arr.Select(p => p.Y).Max();
            var y2 = arr.Select(p => p.Y).Min();
            return new ThWGRect(x1, y1, x2, y2);
        }
        public static ThBlockReferenceData ToDataItem(this Entity ent)
        {
            return new ThBlockReferenceData(ent.ObjectId);
        }
        public static DBObjectCollection ExplodeToDBObjectCollection(this Entity ent)
        {
            var entitySet = new DBObjectCollection();
            ent.Explode(entitySet);
            return entitySet;
        }
        public static DBObject[] ToArray(this DBObjectCollection colle)
        {
            var arr = new DBObject[colle.Count];
            System.Collections.IList list = colle;
            for (int i = 0; i < list.Count; i++)
            {
                var @object = (DBObject)list[i];
                arr[i] = @object;
            }
            return arr;
        }
        public static string GetCustomPropertiyStrValue(this Entity e, string key)
        {
            var d = e.ToDataItem().CustomProperties.ToDict();
            d.TryGetValue(key, out object o);
            return o?.ToString();
        }
        public static Dictionary<string, object> ToDict(this DynamicBlockReferencePropertyCollection colle)
        {
            var ret = new Dictionary<string, object>();
            foreach (var p in colle.ToList())
            {
                ret[p.PropertyName] = p.Value;
            }
            return ret;
        }
        public static List<DynamicBlockReferenceProperty> ToList(this DynamicBlockReferencePropertyCollection colle)
        {
            var ret = new List<DynamicBlockReferenceProperty>();
            foreach (DynamicBlockReferenceProperty item in colle)
            {
                ret.Add(item);
            }
            return ret;
        }
    }
}
