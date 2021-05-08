﻿using System;
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
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using DU = ThMEPWSS.Assistant.DrawUtils;
using LS = System.Collections.Generic.List<string>;

namespace ThMEPWSS.Pipe.Service
{
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Internal;
    using Autodesk.AutoCAD.Runtime;
    using DotNetARX;
    using Dreambuild.AutoCAD;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Index.Strtree;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.DebugNs;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe.Engine;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThUtilExtensionsNs;
    #region Tools
    public static class PolylineTools
    {
        public static Polyline CreatePolyline(IList<Point3d> pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                //pdf page61
                pline.AddVertexAt(i, pts[i].ToPoint2d(), 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreatePolyline(Point2dCollection pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                //pdf page61
                pline.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreatePolyline(params Point2d[] pts)
        {
            //return CreatePolyline(new Point2dCollection(pts));
            var pline = new Polyline();
            for (int i = 0; i < pts.Length; i++)
            {
                //pdf page61
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
    public static class EllipseTools
    {
        public static Ellipse CreateEllipse(Point3d pt1, Point3d pt2)
        {
            var center = GeTools.MidPoint(pt1, pt2);
            var normal = Vector3d.ZAxis;
            var majorAxis = new Vector3d(Math.Abs(pt1.X - pt2.X) / 2, 0, 0);
            var ratio = Math.Abs((pt1.Y - pt2.Y) / (pt1.X - pt2.X));
            var ellipse = new Ellipse();
            ellipse.Set(center, normal, majorAxis, ratio, 0, 2 * Math.PI);
            return ellipse;
        }
        public static Point3d MidPoint(Point3d pt1, Point3d pt2)
        {
            return new Point3d((pt1.X + pt2.X) / 2,
      (pt1.Y + pt2.Y) / 2,
      (pt1.Z + pt2.Z) / 2);
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
    public static class ThBlockTools
    {
        public static ObjectId AddBlockTableRecord(Database db, string blkName, params Entity[] ents)
        {
            var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blkName))
            {
                var btr = new BlockTableRecord();
                btr.Name = blkName;
                ents.ForEach(ent => btr.AppendEntity(ent));
                bt.UpgradeOpen();
                bt.Add(btr);
                db.TransactionManager.AddNewlyCreatedDBObject(btr, true);
                bt.DowngradeOpen();
            }
            return bt[blkName];
        }
        public static ObjectId InsertBlockReference(ObjectId spaceId, string layer,
            string blkName, Point3d position, Scale3d scale, double rotateAngle)
        {
            ObjectId blkRefId;
            var db = spaceId.Database;
            var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blkName)) return ObjectId.Null;
            var space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            var br = new BlockReference(position, bt[blkName]);
            br.ScaleFactors = scale;
            br.Layer = layer;
            br.Rotation = rotateAngle;
            blkRefId = space.AppendEntity(br);
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            space.DowngradeOpen();
            return blkRefId;
        }

        public static void AddAttsToBlock(ObjectId blkId, params AttributeDefinition[] atts)
        {
            var db = blkId.Database;
            var btr = (BlockTableRecord)blkId.GetObject(OpenMode.ForWrite);
            foreach (AttributeDefinition att in atts)
            {
                btr.AppendEntity(att);
                db.TransactionManager.AddNewlyCreatedDBObject(att, true);
            }
            btr.DowngradeOpen();
        }
        public static ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blkName,
            Point3d position, Scale3d scale, double rotateAngle, IDictionary<string, string> attrNameValues)
        {
            var db = spaceId.Database;
            var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blkName)) return ObjectId.Null;
            var space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            var btrId = bt[blkName];
            var record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
            var br = new BlockReference(position, btrId);
            br.ScaleFactors = scale;
            br.Layer = layer;
            br.Rotation = rotateAngle;
            space.AppendEntity(br);
            if (record.HasAttributeDefinitions)
            {
                foreach (ObjectId id in record)
                {
                    if (id.GetObject(OpenMode.ForRead) is AttributeDefinition attDef)
                    {
                        var attribute = new AttributeReference();
                        attribute.SetAttributeFromBlock(attDef, br.BlockTransform);
                        attribute.Position = attDef.Position.TransformBy(br.BlockTransform);
                        attribute.Rotation = attDef.Rotation;
                        attribute.AdjustAlignment(db);
                        if (attrNameValues.ContainsKey(attDef.Tag.ToUpper()))
                        {
                            attribute.TextString = attrNameValues[attDef.Tag.ToUpper()];
                        }
                        br.AttributeCollection.AppendAttribute(attribute);
                        db.TransactionManager.AddNewlyCreatedDBObject(attribute, true);
                    }
                }
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            return br.ObjectId;
        }
        public static void UpdateAttributesInBlock(ObjectId blkRefId, IDictionary<string, string> attNameValues)
        {
            if (blkRefId.GetObject(OpenMode.ForRead) is BlockReference blkRef)
            {
                foreach (ObjectId id in blkRef.AttributeCollection)
                {
                    var attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    if (attNameValues.ContainsKey(attRef.Tag.ToUpper()))
                    {
                        attRef.UpgradeOpen();
                        attRef.TextString = attNameValues[attRef.Tag.ToUpper()];
                        attRef.DowngradeOpen();
                    }
                }
            }
        }
        public static string GetDynBlockValue(ObjectId blockId, string propName)
        {
            return blockId.GetDynProperties().Cast<DynamicBlockReferenceProperty>().FirstOrDefault(prop => prop.PropertyName == propName)?.Value?.ToString();
        }
        public static DynamicBlockReferencePropertyCollection GetDynProperties(ObjectId blockId)
        {
            var br = blockId.GetObject(OpenMode.ForRead) as BlockReference;
            if (br == null || !br.IsDynamicBlock) return null;
            return br.DynamicBlockReferencePropertyCollection;
        }
        public static void SetDynBlockValue(ObjectId blockId, string propName, object value)
        {
            var props = blockId.GetDynProperties();
            foreach (DynamicBlockReferenceProperty prop in props)
            {
                if (!prop.ReadOnly && prop.PropertyName == propName)
                {
                    switch (prop.PropertyTypeCode)
                    {
                        case (short)DynBlockPropTypeCode.Short:
                            prop.Value = Convert.ToInt16(value);
                            break;
                        case (short)DynBlockPropTypeCode.Long:
                            prop.Value = Convert.ToInt64(value);
                            break;
                        case (short)DynBlockPropTypeCode.Real:
                            prop.Value = Convert.ToDouble(value);
                            break;
                        default:
                            prop.Value = value;
                            break;
                    }
                }
            }
        }
        public static string GetBlockName(BlockReference bref)
        {
            if (bref == null) return null;
            if (bref.IsDynamicBlock)
            {
                var idDyn = bref.DynamicBlockTableRecord;
                var btr = (BlockTableRecord)idDyn.GetObject(OpenMode.ForRead);
                return btr.Name;
            }
            else
            {
                return bref.Name;
            }
        }
        public static AnnotationScale AddScale(string scaleName, double paperUnits, double drawingUnits)
        {
            var db = Active.Database;
            var ocm = db.ObjectContextManager;
            var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            if (!occ.HasContext(scaleName))
            {
                var scale = new AnnotationScale();
                scale.Name = scaleName;
                scale.PaperUnits = paperUnits;
                scale.DrawingUnits = drawingUnits;
                return scale;
            }
            return null;
        }
        public static void AttachScale(ObjectId entId, params string[] scaleNames)
        {
            var db = entId.Database;
            var obj = entId.GetObject(OpenMode.ForRead);
            if (obj.Annotative != AnnotativeStates.NotApplicable)
            {
                if (obj is BlockReference br)
                {
                    var btr = (BlockTableRecord)br.BlockTableRecord.GetObject(OpenMode.ForWrite);
                    btr.Annotative = AnnotativeStates.True;
                }
                else if (obj.Annotative == AnnotativeStates.False)
                {
                    obj.Annotative = AnnotativeStates.True;
                    obj.UpgradeOpen();
                    var ocm = db.ObjectContextManager;
                    var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                    foreach (var scaleName in scaleNames)
                    {
                        var scale = occ.GetContext(scaleName);
                        if (scale == null) continue;
                        ObjectContexts.AddContext(obj, scale);
                    }
                    obj.DowngradeOpen();
                }

            }
        }
    }
    #endregion
    public class Ref<T>
    {
        public T Value;
        public Ref(T value)
        {
            this.Value = value;
        }
        public Ref() { }
    }
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
    public class ListDict<K, V>
    {
        Dictionary<K, List<V>> dict = new Dictionary<K, List<V>>();
        public void Add(K item, IEnumerable<V> items)
        {
            if (dict.TryGetValue(item, out List<V> list))
            {
                list.AddRange(items);
            }
            else
            {
                dict[item] = items.ToList();
            }
        }
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
        public void ForEach(Action<K, List<V>> cb)
        {
            foreach (var kv in this.dict) cb(kv.Key, kv.Value);
        }
        public List<V> this[K item]
        {
            get
            {
                dict.TryGetValue(item, out List<V> list);
                return list;
            }
        }
        public IEnumerable<V> Get(K item)
        {
            dict.TryGetValue(item, out List<V> list);
            return list == null ? Enumerable.Empty<V>() : list;
        }
        public Dictionary<K, List<V>>.KeyCollection Keys => dict.Keys;
        public Dictionary<K, List<V>>.ValueCollection Values => dict.Values;
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
            //return rst;

            var selected = AllGravityWaterBucketSpatialIndex.SelectCrossingPolygon(range);
            foreach (Entity e in selected)
            {
                rst.Add(e.GeometricExtents);
            }
            return rst;
        }

        public Pipe.Model.WaterBucketEnum GetRelatedSideWaterBucket(Point3d centerOfPipe)
        {
            //return Pipe.Model.WaterBucketEnum.None;

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
        public Dictionary<Entity, ThWGRect> BoundaryDict = new Dictionary<Entity, ThWGRect>();
        public Dictionary<Entity, string> VerticalPipeDBTextDict = new Dictionary<Entity, string>();
        public List<Entity> VerticalPipeLines = new List<Entity>();
        public List<DBText> VerticalPipeDBTexts = new List<DBText>();
        public List<BlockReference> VerticalPipes = new List<BlockReference>();
        public List<Entity> VerticalFakePipes = new List<Entity>();
        public Dictionary<string, string> VerticalPipeLabelToDNDict = new Dictionary<string, string>();
        public Dictionary<Entity, string> VerticalPipeToLabelDict = new Dictionary<Entity, string>();
        public List<Tuple<Entity, Entity>> ShortConverters = new List<Tuple<Entity, Entity>>();
        public List<Entity> LongConverterLines = new List<Entity>();
        public ListDict<Entity> LongConverterToPipesDict = new ListDict<Entity>();
        public ListDict<Entity> LongConverterToLongConvertersDict = new ListDict<Entity>();
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
                foreach (var item in ShortConverters)
                {
                    yield return item.Item1;
                    yield return item.Item2;
                }
            }
        }

        bool inited;
        public class Context
        {
            public LS VerticalPipes = new LS();
            public Dictionary<string, SRect> BoundaryDict = new Dictionary<string, SRect>();
            public LS WRainLines = new LS();
            public Dictionary<string, SLine> WRainLinesDict = new Dictionary<string, SLine>();
        }
        public Context GetCurrentContext()
        {
            var c = new Context(); var d = new GuidDict(4096);
            d.AddObjs(VerticalPipes); foreach (var e in VerticalPipes) c.VerticalPipes.Add(d[e]);
            d.AddObjs(BoundaryDict.Keys); foreach (var kv in BoundaryDict) c.BoundaryDict[d[kv.Key]] = kv.Value.ToSRect();
            d.AddObjs(WRainLines);
            foreach (var line in WRainLines) if (GeoAlgorithm.TryConvertToLineSegment(line, out ThWGLineSegment seg)) c.WRainLinesDict[d[line]] = seg.ToSLine();
            return c;
        }
        public bool HasBrokenCondensePipe(Point3dCollection range, string id)
        {
            return FiltByRect(range, brokenCondensePipes.Where(kv => kv.Value == id).Select(kv => kv.Key)).Any();
        }
        HashSet<Entity> wrappingEnts = new HashSet<Entity>();
        public bool HasDrivePipe(Entity e)
        {
            return wrappingEnts.Contains(e);
        }

        public bool HasShortConverters(Entity ent)
        {
            //pipe only
            return AllShortConverters.Contains(ent);
        }
        public bool HasLongConverters(Entity ent)
        {
            //pipe only!
            return LongConverterPipes.Contains(ent);
            //pipe or nearby line
            //return WRainLinesToVerticalPipes.Any(kv => kv.Key == ent || kv.Value == ent);
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
            //Dbg.PrintLine("_GetTranslatorType " + verticalPipeID + " " + ret);
            return ret;

            //var rect = range.ToRect();
            //return GetTranslatorType(verticalPipeID, rect);
        }
        private bool HasGravityConverters(Entity pipe)
        {
            foreach (var g in LongConverterLineToWaterBucketsGroups)
            {
                if (g.Count < 3) continue;
                if (!g.Any(e => WaterBuckets.Contains(e))) continue;
                if (g.Contains(pipe)) return true;
            }
            return false;
        }
        private TranslatorTypeEnum _GetTranslatorType(Point3dCollection range, string verticalPipeID)
        {
            List<Entity> pipes = GetVerticalPipes(range);
            var pipe = _GetVerticalPipe(pipes, verticalPipeID);
            if (pipe == null) return TranslatorTypeEnum.None;
            if (HasShortConverters(pipe)) return TranslatorTypeEnum.Short;
            if (HasLongConverters(pipe)) return TranslatorTypeEnum.Long;
            if (HasGravityConverters(pipe)) return TranslatorTypeEnum.Gravity;

            return TranslatorTypeEnum.None;
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
            //var range = pts.ToRect();
            //if (pts.Count >= 3)
            //{
            //    var rst = VerticalPipeToLabelDict.Keys.ToList();
            //    var spacialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(rst.ToCollection());
            //    rst = spacialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();

            //    var pipe = VerticalPipeToLabelDict.FirstOrDefault(kv => kv.Value == pipeId && range.ContainsRect(BoundaryDict[kv.Key])).Key;
            //    Dbg.PrintLine(pipeId + " " + 123);
            //    if (pipe != null)
            //    {
            //        {
            //            Dbg.PrintLine(pipeId + " " + 456);
            //            Entity targetCvt = null;
            //            LongConverterToPipesDict.ForEach((cvt, pipes) =>
            //            {
            //                if (targetCvt == null && pipes.Contains(pipe)) targetCvt = cvt;
            //            });
            //            Dbg.PrintLine(pipeId + " " + 789);
            //            if (targetCvt != null)
            //            {
            //                Dbg.PrintLine("find targetCvt " + pipeId);
            //                {
            //                    var sym = ConnectToRainPortSymbolToLongConverterLineDict.FirstOrDefault(kv => kv.Value == targetCvt).Key;
            //                    if (sym != null) return RainOutputTypeEnum.RainPort;
            //                }
            //                {
            //                    var ok = GeoAlgorithm.TryConvertToLineSegment(targetCvt, out ThWGLineSegment lineSeg);
            //                    if (ok)
            //                    {
            //                        foreach (var pt in new Point2d[] { lineSeg.Point1, lineSeg.Point2 })
            //                        {
            //                            foreach (var rainDrains in WaterWells)
            //                            {
            //                                var bd = BoundaryDict[rainDrains];
            //                                if (bd.ContainsPoint(pt)) return RainOutputTypeEnum.WaterWell;
            //                            }
            //                        }
            //                        double minDis = double.MaxValue;
            //                        foreach (var pt in new Point2d[] { lineSeg.Point1, lineSeg.Point2 })
            //                        {
            //                            foreach (var waterwell in WaterWells)
            //                            {
            //                                var dis = GeoAlgorithm.Distance(pt, BoundaryDict[waterwell].Center);
            //                                if (minDis > dis) minDis = dis;
            //                                if (dis <= 1000)
            //                                {
            //                                    return RainOutputTypeEnum.WaterWell;
            //                                }
            //                            }
            //                        }
            //                        return RainOutputTypeEnum.DrainageDitch;
            //                    }
            //                }
            //            }
            //        }
            //        {
            //            var db = adb.Database;
            //            foreach (var item in ShortConverters)
            //            {
            //                Entity targetPipe = null;
            //                if (item.Item1 == pipe) targetPipe = item.Item2;
            //                else if (item.Item2 == pipe) targetPipe = item.Item1;
            //                if (targetPipe != null)
            //                {
            //                    Entity targetCvt = null;
            //                    foreach (var line in VerticalPipeLines)
            //                    {
            //                        if (GeoAlgorithm.IsRectCross(BoundaryDict[line], BoundaryDict[targetPipe]))
            //                        {
            //                            targetCvt = line;
            //                            break;
            //                        }
            //                    }
            //                    if (targetCvt != null)
            //                    {
            //                        {
            //                            var sym = ConnectToRainPortSymbolToLongConverterLineDict.FirstOrDefault(kv => kv.Value == targetCvt).Key;
            //                            if (sym != null) return RainOutputTypeEnum.RainPort;
            //                        }
            //                        {
            //                            var ok = GeoAlgorithm.TryConvertToLineSegment(targetCvt, out ThWGLineSegment lineSeg);
            //                            if (ok)
            //                            {
            //                                foreach (var pt in new Point2d[] { lineSeg.Point1, lineSeg.Point2 })
            //                                {
            //                                    foreach (var rainDrains in WaterWells)
            //                                    {
            //                                        var bd = BoundaryDict[rainDrains];
            //                                        if (bd.ContainsPoint(pt)) return RainOutputTypeEnum.WaterWell;
            //                                    }
            //                                }
            //                                double minDis = double.MaxValue;
            //                                foreach (var pt in new Point2d[] { lineSeg.Point1, lineSeg.Point2 })
            //                                {
            //                                    foreach (var waterwell in WaterWells)
            //                                    {
            //                                        var dis = GeoAlgorithm.Distance(pt, BoundaryDict[waterwell].Center);
            //                                        if (minDis > dis) minDis = dis;
            //                                        if (dis <= 1000)
            //                                        {
            //                                            return RainOutputTypeEnum.WaterWell;
            //                                        }
            //                                    }
            //                                }
            //                                return RainOutputTypeEnum.DrainageDitch;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            //return RainOutputTypeEnum.None;
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
            //return WaterWells.Where(e => GetLabel(e) == pipeId).Any(e => wrappingEnts.Contains(e));
            return GetWrappingPipes(range).Any(e => GetLabel(e) == pipeId);
        }

        public static bool IsIntersects(Polyline p1, Polyline p2)
        {
            return new ThCADCore.NTS.ThCADCoreNTSRelate(p1.MinimumBoundingBox(), p2.MinimumBoundingBox()).IsIntersects;
        }
        public Polyline CreatePolygon(Entity e, int num = 4, double expand = 0)
        {
            var bd = BoundaryDict[e];
            var pl = PolylineTools.CreatePolygon(bd.Center, num, bd.Radius + expand);
            return pl;
        }
        const double tol = 800;
        public static Polyline expandLine(Line line, double distance)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint - lineDir * tol + moveDir * distance;
            Point3d p2 = line.EndPoint + lineDir * tol + moveDir * distance;
            Point3d p3 = line.EndPoint + lineDir * tol - moveDir * distance;
            Point3d p4 = line.StartPoint - lineDir * tol - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
        public ThGravityService thGravityService;
        public void InitCache()
        {
            if (inited) return;
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
            inited = true;
        }

        public List<Entity> FloorDrains = new List<Entity>();
        public void CollectFloorDrains()
        {
            const string NAME = "地漏平面";
            FloorDrains.AddRange(adb.ModelSpace.OfType<BlockReference>()
             .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
             .Where(x => x.ToDataItem().EffectiveName == NAME));
            foreach (var e in FloorDrains)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
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
        Dictionary<Point3dCollection, List<Entity>> RangeToCondensePipesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetCondensePipes(Point3dCollection range)
        {
            if (!RangeToCondensePipesDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetCondensePipes(range);
                RangeToCondensePipesDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _CondensePipesSpatialIndex;
        private List<Entity> _GetCondensePipes(Point3dCollection pts)
        {
            _CondensePipesSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(CondensePipes);
            return _CondensePipesSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToFloorDrainsDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetFloorDrains(Point3dCollection range)
        {
            if (!RangeToFloorDrainsDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetFloorDrains(range);
                RangeToFloorDrainsDict[range] = ents;
            }
            return ents;
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
        Dictionary<Point3dCollection, List<Entity>> RangeToLongConverterLinesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetLongConverterLines(Point3dCollection range)
        {
            if (!RangeToLongConverterLinesDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetLongConverterLines(range);
                RangeToLongConverterLinesDict[range] = ents;
            }
            return ents;
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
                if (GeoAlgorithm.TryConvertToLineSegment(e, out ThWGLineSegment seg))
                {
                    var line = new Line() { StartPoint = seg.Point1.ToPoint3d(), EndPoint = seg.Point2.ToPoint3d() };
                    WRainLines.Add(line);
                    WRainLinesMapping[e] = line;
                    BoundaryDict[line] = new ThWGRect(seg.Point1, seg.Point2);
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
            public EntitiesCollector Add<T>(IEnumerable<T> ents)where T : Entity
            {
                Entities.AddRange(ents);
                return this;
            }
        }
        public static EntitiesCollector CollectEnts()=> new EntitiesCollector();
        public static ThCADCoreNTSSpatialIndex BuildSpatialIndex<T>(IList<T> ents) where T : Entity
        {
            var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(ents.ToCollection());
            return si;
        }
        public void BuildConnectToRainDrainSymbolToConnectToRainDrainDBTextDict()
        {
            foreach (var ent in ConnectToRainPortSymbols)
            {
                var r = BoundaryDict[ent];
                foreach (var e1 in VerticalPipeLines.Where(e => GeoAlgorithm.IsRectCross(BoundaryDict[e], r)))
                {
                    foreach (var e2 in VerticalPipeLines.Where(e => GeoAlgorithm.IsRectCross(BoundaryDict[e], BoundaryDict[e1])))
                    {
                        if (e2 != e1)
                        {
                            foreach (var e3 in ConnectToRainPortDBTexts.Where(e => GeoAlgorithm.IsRectCross(BoundaryDict[e], BoundaryDict[e2].Expand(200))))
                            {
                                ConnectToRainPortSymbolToConnectToRainDrainDBTextDict[ent] = e3;
                                break;
                            }
                        }
                    }
                }
            }

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
            ConnectToRainPortSymbols.AddRange(adb.ModelSpace.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
            foreach (var e in ConnectToRainPortSymbols)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWaterWell13s()
        {
            RainDrain13s.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "13#雨水口"));
            foreach (var e in RainDrain13s)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWaterWells()
        {
            WaterWells.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
            WaterWells.ForEach(e => WaterWellDNs[e] = e.GetAttributesStrValue("-"));
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
        public void CollectData()
        {
            InitCache();
            CollectVerticalPipesData();
            FindShortConverters();
            LabelEnts();
            //PatchForSomeCase();
            FindOutBrokenCondensePipes();
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
            pairs.AddRange(sv.EnumerateEntities(sv.LongConverterLines, sv.VerticalPipeList, 10));
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
                var r = ThWGRect.Create(ext);
                var pl = EntityFactory.CreatePolyline(r.ToPoint3dCollection());
                pls.Add(pl);
                BoundaryDict[pl] = r;
            }
            this.WaterBuckets = pls;
            var si = new NTSSpatialIndex1(pls.ToCollection());
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
            //var sv = this;
            //var lines = sv.WRainLines;
            //var wells = sv.WaterWells;
            //var mps1 = lines.Select(e => new KeyValuePair<Entity, Polyline>(e, (e as Line)?.Buffer(10))).ToList();
            //var mps2 = wells.Select(e => new KeyValuePair<Entity, Polyline>(e, sv.CreatePolygon(e, expand: 50))).ToList();
            //var bfs = mps1.Select(kv => kv.Value).ToList();
            //var pls = mps2.Select(kv => kv.Value).ToList();
            //var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            //var dict = new Dictionary<Entity, Entity>();
            //foreach (var pl in pls)
            //{
            //    foreach (var bf in si.SelectCrossingPolygon(pl).Cast<Polyline>().ToList())
            //    {
            //        var line = mps1.First(kv => kv.Value == bf).Key;
            //        var well = mps2.First(kv => kv.Value == pl).Key;
            //        dict[line] = well;
            //    }
            //}
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
             .Where(x => x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
            foreach (var e in WrappingPipes)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        //void PatchForSomeCase()
        //{
        //    var re = new Regex(@"接(\d+)楼屋面雨水斗");
        //    foreach (var e in VerticalPipes)
        //    {
        //        if (VerticalPipeToLabelDict.TryGetValue(e, out string lb))
        //        {
        //            var m = re.Match(lb);
        //            if (m.Success)
        //            {
        //                var targetFloor = m.Groups[1].Value;

        //            }
        //        }
        //    }
        //}
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
        public bool HasGravityLabelConnected(Point3dCollection range, string pipeId)
        {
            //get long translator of pipeId
            //get connected pipe's label
            //match by regex
            //if macth, return true, else return false
            var re = new Regex(@"接(\d+F)屋面雨水斗");
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

            ////get e
            //if (VerticalPipeToLabelDict.TryGetValue(e, out string lb))
            //{
            //    var m = re.Match(pipei);
            //    if (m.Success)
            //    {
            //        var targetFloor = m.Groups[1].Value;

            //    }
            //}
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
        public IEnumerable<Entity> FiltByRect(Point3dCollection range, IEnumerable<Entity> ents)
        {
            var rg = range.ToRect();
            foreach (var e in ents)
            {
                if (BoundaryDict.TryGetValue(e, out ThWGRect r))
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



        public TranslatorTypeEnum GetTranslatorType(string verticalPipeID, ThWGRect rect)
        {
            var ret = _GetTrans(verticalPipeID, rect);
            //Dbg.PrintLine("GetTranslatorType " + verticalPipeID + " " + ret);
            return ret;
        }

        private TranslatorTypeEnum _GetTrans(string verticalPipeID, ThWGRect rect)
        {
            var shortCvts = FiltEntityByRange(rect, ShortConverters.SelectMany(x => new Entity[] { x.Item1, x.Item2 })).ToList();
            foreach (var pipe in FiltEntityByRange(rect, VerticalPipeToLabelDict.Keys))
            {
                VerticalPipeToLabelDict.TryGetValue(pipe, out string label);
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
            {
                //var pairs = new List<KeyValuePair<int, int>>();
                //for (int i = 0; i < lines.Count; i++)
                //{
                //    for (int j = i + 1; j < lines.Count; j++)
                //    {
                //        var line1 = lines[i] as Line;
                //        var line2 = lines[j] as Line;
                //        if (line1 != null && line2 != null)
                //        {
                //            var pline1 = line1.Buffer(10);
                //            var pline2 = line2.Buffer(10);
                //            if (new ThCADCore.NTS.ThCADCoreNTSRelate(pline1, pline2).IsIntersects)
                //            {
                //                pairs.Add(new KeyValuePair<int, int>(i, j));
                //            }
                //        }
                //    }
                //}
                GroupLines(lines, linesGroup, 10);
            }
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
            //foreach (var e1 in this.VerticalPipeLines)
            //{
            //    foreach (var e2 in this.VerticalPipes)
            //    {
            //        if (new ThCADCore.NTS.ThCADCoreNTSRelate(plDict[e1], plDict[e2]).IsIntersects)
            //        {
            //            lineToPipesDict.Add(e1, e2);
            //        }
            //    }
            //}
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
                    var targetPipes = SortBy2DSpacePosition(g.Where(e => this.VerticalPipes.Contains(e))).ToList();
                    var targetTexts = SortBy2DSpacePosition(g.Where(e => this.VerticalPipeDBTexts.Contains(e))).ToList();
                    if (targetPipes.Count == targetTexts.Count && targetTexts.Count > 0)
                    {
                        setVisibilities(targetPipes.Cast<BlockReference>().ToList(), targetTexts.Cast<DBText>().ToList());
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
                            if (c1.Y > c2.Y && GeoAlgorithm.Distance(c1, c2) < 150)
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

        public static List<KeyValuePair<Entity, Entity>> GroupLinesBySpatialIndex(List<Entity> lines)
        {
            //var pairs = new List<KeyValuePair<int, int>>();
            var pairs = new List<KeyValuePair<Entity, Entity>>();

            //for (int i = 0; i < lines.Count; i++)
            //{
            //    for (int j = i + 1; j < lines.Count; j++)
            //    {
            //        var line1 = lines[i] as Line;
            //        var line2 = lines[j] as Line;
            //        if (line1 != null && line2 != null)
            //        {
            //            var pline1 = line1.Buffer(10);
            //            var pline2 = line2.Buffer(10);
            //            if (new ThCADCore.NTS.ThCADCoreNTSRelate(pline1, pline2).IsIntersects)
            //            {
            //                pairs.Add(new KeyValuePair<int, int>(i, j));
            //            }
            //        }
            //    }
            //}

            var bfs = lines.Select(e => (e as Line)?.Buffer(10)).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            for (int i = 0; i < bfs.Count; i++)
            {
                Polyline bf = bfs[i];
                if (bf != null)
                {
                    var lst = si.SelectCrossingPolygon(bf).Cast<Polyline>().Select(e => bfs.IndexOf(e)).Where(j => i < j).ToList();
                    //lst.ForEach(j => pairs.Add(new KeyValuePair<int, int>(i, j)));
                    lst.ForEach(j => pairs.Add(new KeyValuePair<Entity, Entity>(lines[i], lines[j])));
                }
            }
            return pairs;
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
        public string GetLabelFromList(IEnumerable<Entity> ents)
        {
            foreach (var e in ents) if (VerticalPipeToLabelDict.TryGetValue(e, out string lb)) return lb;
            return null;
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
                        //DU.DrawBoundaryLazy(g.ToArray());
                        {
                            foreach (var kv in enumerateEnts(new List<Entity>() { e }, 10))
                            {
                                //DU.DrawBoundaryLazy(kv.Key);
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
        public void LabelWRainLinesAndVerticalPipes()
        {
            //var sv = new ThRainSystemService() { adb = adb };
            //sv.InitCache();
            //sv.CollectVerticalPipeData2();
            //sv.CollectShortConverters();
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.VerticalPipes);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToVerticalPipes.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.VerticalPipeList, 100));
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
                        //if (sv.VerticalPipes.Contains(e))
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
                var pipes = g.Where(e => VerticalPipes.Contains(e)).ToList();
                if (pipes.Count > 1) LongConverterPipes.AddRange(pipes);
            }

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
                        if (GeoAlgorithm.TryConvertToLineSegment(line1, out ThWGLineSegment seg1) && GeoAlgorithm.TryConvertToLineSegment(line2, out ThWGLineSegment seg2))
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
        public List<Entity> VerticalPipeList => VerticalPipes.Cast<Entity>().ToList();
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
            return VerticalPipeToLabelDict.Any(kv => kv.Value == pipeId && ConnectToRainPortDBTexts.Contains(kv.Key));
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
        public List<Entity> ConvertToPolylines(List<Entity> ents)
        {
            return ents.Select(e => BoundaryDict[e].CreatePolygon(6)).Cast<Entity>().ToList();
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
                GeoAlgorithm.TryConvertToLineSegment(e, out ThWGLineSegment seg);
                return new Line() { StartPoint = seg.Point1.ToPoint3d(), EndPoint = seg.Point2.ToPoint3d() };
            }
            return r;
        }

        public static void GroupLines(List<Entity> lines, List<List<Entity>> linesGroup, double bufferDistance)
        {
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

        private void setVisibilities(List<BlockReference> targetPipes, List<DBText> targetTexts)
        {
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
                    VerticalPipeToLabelDict[pipeEnt] = label;
                }
            }
        }

        public IEnumerable<T> SortBy2DSpacePosition<T>(IEnumerable<T> list) where T : Entity
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
            var d = VerticalPipeToLabelDict;
            foreach (var item in ShortConverters)
            {
                string v;
                if (!d.TryGetValue(item.Item1, out v) && d.TryGetValue(item.Item2, out v))
                {
                    d[item.Item1] = v;
                }
                if (!d.TryGetValue(item.Item2, out v) && d.TryGetValue(item.Item1, out v))
                {
                    d[item.Item2] = v;
                }
            }
        }
        public void CollectVerticalPipes()
        {
            var blockNameOfVerticalPipe = "带定位立管";
            VerticalPipes.AddRange(adb.ModelSpace.OfType<BlockReference>()
             .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
             .Where(x => x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
            VerticalFakePipes.AddRange(ConvertToPolylines(VerticalPipes.Cast<Entity>().ToList()));
        }
        //public void CollectVerticalPipes()
        //{
        //    var blockNameOfVerticalPipe = "带定位立管";
        //    VerticalPipes.AddRange(adb.ModelSpace.OfType<BlockReference>()
        //     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
        //     .Where(x => x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
        //    VerticalPipes.AddRange(adb.ModelSpace.OfType<Circle>()
        //     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM));
        //图块和圆圈都要支持。图块的名称为“带定位立管”。圆圈的半径不大于200。
        //    ThWGRect getRealBoundaryForPipe(Entity ent)
        //    {
        //        if (ent is BlockReference)
        //        {
        //            var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
        //            var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
        //            if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
        //        }
        //        if (ent is Circle) return GeoAlgorithm.GetBoundaryRect(ent);
        //        return default;
        //    }
        //    foreach (var e in VerticalPipes)
        //    {
        //        BoundaryDict[e] = getRealBoundaryForPipe(e);
        //    }
        //}
        public void CollectVerticalPipeDBTexts()
        {
            VerticalPipeDBTexts.AddRange(adb.ModelSpace.OfType<DBText>()
             .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE)
             //.Where(x=>x.TextString.StartsWith("Y1L"))
             );

            foreach (var e in VerticalPipeDBTexts)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                VerticalPipeDBTextDict[e] = e.TextString;
            }
        }

        public void CollectVerticalPipeLines()
        {
            VerticalPipeLines.AddRange(adb.ModelSpace.OfType<Line>().Cast<Entity>()
             .Union(adb.ModelSpace.OfType<Polyline>().Cast<Entity>())
             .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE));
            foreach (var e in VerticalPipeLines)
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
        public static DBObjectCollection ExplodeToDBObjectCollection(this IList<Entity> ents)
        {
            var entitySet = new DBObjectCollection();
            foreach (var ent in ents) ent.Explode(entitySet);
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
            if (!(e is BlockReference)) return null;
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
