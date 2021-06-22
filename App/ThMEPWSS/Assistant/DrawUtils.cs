using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetARX;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Uitl;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Geom;
using Autodesk.AutoCAD.Internal;
using ThMEPWSS.Uitl.ExtensionsNs;
using System.Windows.Forms;
using NetTopologySuite.Geometries;
using ThMEPWSS.DebugNs;
using ThCADCore.NTS;

namespace ThMEPWSS.Assistant
{
    public class Cache<K, V>
    {
        Dictionary<K, V> d = new Dictionary<K, V>();
        Func<K, V> f;
        public Cache(Func<K, V> f)
        {
            this.f = f;
        }
        public V this[K k]
        {
            get
            {
                if (!d.TryGetValue(k, out V v))
                {
                    v = f(k);
                    d[k] = v;
                }
                return v;
            }
        }
    }
    public class DrawingTransaction : IDisposable
    {
        public static DrawingTransaction Cur { get; private set; }
        public AcadDatabase adb { get; }
        public FastBlock fbk { get; }
        public DrawingTransaction(AcadDatabase adb) : this()
        {
            this.adb = adb;
        }
        public DrawingTransaction(AcadDatabase adb, bool createFbk)
        {
            this.adb = adb;
            if (createFbk) this.fbk = FastBlock.Create(adb);
        }
        public DrawingTransaction()
        {
            DrawUtils.DrawingQueue.Clear();
            Cur = this;
        }
        public void Dispose()
        {
            try
            {
                if (adb != null)
                {
                    DrawUtils.Draw(adb);
                }
                else
                {
                    DrawUtils.Draw();
                }
            }
            finally
            {
                Cur = null;
            }
        }
    }
    public class FastBlock
    {
        BlockTable bt;
        BlockTableRecord btr;
        AcadDatabase adb;
        Database db;
        Dictionary<string, ObjectId> d1 = new Dictionary<string, ObjectId>();
        Dictionary<string, BlockTableRecord> d2 = new Dictionary<string, BlockTableRecord>();
        Dictionary<BlockTableRecord, List<AttributeDefinition>> d3 = new Dictionary<BlockTableRecord, List<AttributeDefinition>>();
        Dictionary<BlockTableRecord, Action<BlockReference, Dictionary<string, string>>> d4 = new Dictionary<BlockTableRecord, Action<BlockReference, Dictionary<string, string>>>();
        private FastBlock() { }
        public static FastBlock Create(AcadDatabase adb)
        {
            var o = new FastBlock();
            var spaceId = adb.ModelSpace.ObjectId;
            var db = spaceId.Database;
            o.adb = adb;
            o.db = db;
            var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            o.bt = bt;
            o.btr = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            foreach (var blockName in adb.Blocks.Select(x => x.Name))
            {
                var btrId = bt[blockName];
                var record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
                o.d1[blockName] = btrId;
                o.d2[blockName] = record;
                if (record.HasAttributeDefinitions)
                {
                    var attrDefs = new List<AttributeDefinition>();
                    foreach (ObjectId id in record)
                    {
                        if (id.GetObject(OpenMode.ForRead) is AttributeDefinition attr)
                            attrDefs.Add(attr);
                    }
                    o.d3[record] = attrDefs;
                    var attrDefKeys = attrDefs.Select(x => x.Tag.ToUpper()).ToList();
                    o.d4[record] = (br, d) =>
                    {
                        for (int i = 0; i < attrDefs.Count; i++)
                        {
                            var k = attrDefKeys[i];
                            var attrDef = attrDefs[i];
                            if (d.TryGetValue(k, out string value))
                            {
                                var attr = CreateAttribute(db, br, attrDef);
                                attr.TextString = value;
                                AppendAttribute(db, br, attr);
                            }
                        }
                    };
                }
            }
            return o;
        }
        public bool HasBlock(string name) => d1.ContainsKey(name);
        public BlockReference InsertBlockReference(Point3d position, string blockName,
            Dictionary<string, string> attNameValue = null,
            Action<BlockReference> before = null,
            Action<BlockReference> after = null)
        {
            if (!d1.TryGetValue(blockName, out ObjectId id)) return null;
            var br = new BlockReference(position, id);
            before?.Invoke(br);
            btr.AppendEntity(br);
            if (attNameValue != null)
            {
                var record = d2[blockName];
                d4[record](br, attNameValue);
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            after?.Invoke(br);
            return br;
        }
        public static void AppendAttribute(Database db, BlockReference br, AttributeReference attribute)
        {
            //向块参照添加属性对象
            br.AttributeCollection.AppendAttribute(attribute);
            db.TransactionManager.AddNewlyCreatedDBObject(attribute, true);
        }

        public static AttributeReference CreateAttribute(Database db, BlockReference br, AttributeDefinition attDef)
        {
            //创建一个新的属性对象
            AttributeReference attribute = new AttributeReference();
            //从属性定义获得属性对象的对象特性
            attribute.SetAttributeFromBlock(attDef, br.BlockTransform);
            //设置属性对象的其它特性
            attribute.Position = attDef.Position.TransformBy(br.BlockTransform);
            attribute.Rotation = attDef.Rotation;
            attribute.AdjustAlignment(db);
            return attribute;
        }
    }
    public static class DrawUtils
    {
        public static DrawingTransaction DrawingTransaction => new DrawingTransaction();
        public static Queue<Action<AcadDatabase>> DrawingQueue { get; } = new Queue<Action<AcadDatabase>>(4096);
        static readonly Dictionary<string, ObjectId> d = new Dictionary<string, ObjectId>();
        public static void Dispose()
        {
            DrawingQueue.Clear();
        }
        public static List<Action<AcadDatabase>> TakeAllDrawingActions()
        {
            var lst = DrawingQueue.ToList();
            DrawingQueue.Clear();
            return lst;
        }
        public static void Draw(IEnumerable<Action<AcadDatabase>> fs, AcadDatabase adb, bool notifyOnException = true)
        {
            foreach (var f in fs)
            {
                try
                {
                    f(adb);
                }
                catch (System.Exception ex)
                {
                    if (notifyOnException)
                    {
                        MessageBox.Show((ex.InnerException ?? ex).Message);
                    }
                    break;
                }
            }
        }
        public static ObjectId GetTextStyleId(string textStyleName)
        {
            if (!d.TryGetValue(textStyleName, out ObjectId id))
            {
                id = DbHelper.GetTextStyleId(textStyleName);
                d[textStyleName] = id;
            }
            return id;
        }
        public static void SetTextStyle(DBText t, string textStyleName)
        {
            if (!t.ObjectId.IsValid) return;
            var textStyleId = GetTextStyleId(textStyleName);
            if (!textStyleId.IsValid) return;
            t.TextStyleId = textStyleId;
        }
        public static void SetTextStyleLazy(DBText t, string textStyleName)
        {
            DrawingQueue.Enqueue(adb =>
            {
                SetTextStyle(t, textStyleName);
            });
        }
        public static void Draw()
        {
            if (DrawingQueue.Count == 0) return;
            using var adb = AcadDatabase.Active();
            Draw(adb);
        }
        public static void Draw(AcadDatabase adb)
        {
            try
            {
                while (DrawingQueue.Count > 0)
                {
                    DrawingQueue.Dequeue()(adb);
                }
            }
            finally
            {
                if (DrawingQueue.Count > 0) DrawingQueue.Clear();
            }
        }
        public static void SetLayerAndColorIndex(string layer, int colorIndex, params Entity[] ents)
        {
            foreach (var ent in ents)
            {
                ent.Layer = layer;
                ent.ColorIndex = colorIndex;
            }
        }
        public static Circle DrawGeometryLazy(GCircle circle)
        {
            var c = new Circle() { Center = circle.Center.ToPoint3d(), Radius = circle.Radius };
            DrawEntityLazy(c);
            return c;
        }
        public static void DrawGeometryLazy(Geometry geo, Action<List<Entity>> cb = null)
        {
            var ents = geo.ToDbObjects().OfType<Entity>().ToList();
            cb?.Invoke(ents);
            DrawEntitiesLazy(ents);
        }
        public static void DrawEntityLazy(Entity ent)
        {
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(ent));
        }
        public static void DrawEntitiesLazy<T>(IList<T> ents) where T : Entity
        {
            DrawingQueue.Enqueue(adb => ents.ForEach(ent => adb.ModelSpace.Add(ent)));
        }
        public static ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blockName, Point3d position, Scale3d scale, double rotateAngle)
        {
            ObjectId blockRefId;//存储要插入的块参照的Id
            Database db = spaceId.Database;//获取数据库对象
            //以读的方式打开块表
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            //如果没有blockName表示的块，则程序返回
            if (!bt.Has(blockName)) return ObjectId.Null;
            //以写的方式打开空间（模型空间或图纸空间）
            BlockTableRecord space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            //创建一个块参照并设置插入点
            BlockReference br = new BlockReference(position, bt[blockName]);
            br.ScaleFactors = scale;//设置块参照的缩放比例
            if (layer != null) br.Layer = layer;//设置块参照的层名
            br.Rotation = rotateAngle;//设置块参照的旋转角度
            ObjectId btrId = bt[blockName];//获取块表记录的Id
            //打开块表记录
            BlockTableRecord record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
            //添加可缩放性支持
            if (record.Annotative == AnnotativeStates.True)
            {
                ObjectContextCollection contextCollection = db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");
                ObjectContexts.AddContext(br, contextCollection.GetContext("1:1"));
            }
            blockRefId = space.AppendEntity(br);//在空间中加入创建的块参照
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);//通知事务处理加入创建的块参照
            space.DowngradeOpen();//为了安全，将块表状态改为读
            return blockRefId;//返回添加的块参照的Id
        }
        public static void DrawBlockReference(string blkName, Point3d basePt)
        {
            DrawingQueue.Enqueue(adb =>
            {
                var id = adb.ModelSpace.ObjectId;
                if (!id.IsValid) return;
                InsertBlockReference(id, null, blkName, basePt, new Scale3d(1), 0);
            });
        }
        public static ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blockName, Point3d position, Scale3d scale, double rotateAngle, Dictionary<string, string> attNameValues)
        {
            Database db = spaceId.Database;//获取数据库对象
            //以读的方式打开块表
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            //如果没有blockName表示的块，则程序返回
            if (!bt.Has(blockName)) return ObjectId.Null;
            //以写的方式打开空间（模型空间或图纸空间）
            BlockTableRecord space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            ObjectId btrId = bt[blockName];//获取块表记录的Id
            //打开块表记录
            BlockTableRecord record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
            //创建一个块参照并设置插入点
            BlockReference br = new BlockReference(position, bt[blockName]);
            br.ScaleFactors = scale;//设置块参照的缩放比例
            if (layer != null) br.Layer = layer;//设置块参照的层名
            br.Rotation = rotateAngle;//设置块参照的旋转角度
            space.AppendEntity(br);//为了安全，将块表状态改为读 
            //判断块表记录是否包含属性定义
            if (record.HasAttributeDefinitions)
            {
                //若包含属性定义，则遍历属性定义
                foreach (ObjectId id in record)
                {
                    //检查是否是属性定义
                    AttributeDefinition attDef = id.GetObject(OpenMode.ForRead) as AttributeDefinition;
                    if (attDef != null)
                    {
                        //创建一个新的属性对象
                        AttributeReference attribute = new AttributeReference();
                        //从属性定义获得属性对象的对象特性
                        attribute.SetAttributeFromBlock(attDef, br.BlockTransform);
                        //设置属性对象的其它特性
                        attribute.Position = attDef.Position.TransformBy(br.BlockTransform);
                        attribute.Rotation = attDef.Rotation;
                        attribute.AdjustAlignment(db);
                        //判断是否包含指定的属性名称
                        if (attNameValues.ContainsKey(attDef.Tag.ToUpper()))
                        {
                            //设置属性值
                            attribute.TextString = attNameValues[attDef.Tag.ToUpper()].ToString();
                        }
                        //向块参照添加属性对象
                        br.AttributeCollection.AppendAttribute(attribute);
                        db.TransactionManager.AddNewlyCreatedDBObject(attribute, true);
                    }
                }
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            return br.ObjectId;//返回添加的块参照的Id
        }
        public static void DrawBlockReference(string blkName, Point3d basePt, Action<BlockReference> cb = null, Dictionary<string, string> props = null, string layer = null, double scale = 1, double rotateDegree = 0)
        {
            DrawingQueue.Enqueue(adb =>
            {
                var id = InsertBlockReference(adb.ModelSpace.ObjectId, layer, blkName, basePt, new Scale3d(scale), GeoAlgorithm.AngleFromDegree(rotateDegree), props);
                if (!id.IsValid) return;
                if (cb != null)
                {
                    var br = adb.Element<BlockReference>(id);
                    cb(br);
                }
            });
        }
        public static void DrawBlockReference(string blkName, Point3d basePt, Action<BlockReference> cb)
        {
            DrawingQueue.Enqueue(adb =>
            {
                var id = InsertBlockReference(adb.ModelSpace.ObjectId, null, blkName, basePt, new Scale3d(1), 0);
                if (!id.IsValid) return;
                if (cb != null)
                {
                    var br = adb.Element<BlockReference>(id);
                    cb(br);
                }
            });
        }
        public static void DrawBlockReference(string blkName, Point3d basePt, string layerName)
        {
            DrawingQueue.Enqueue(adb =>
            {
                var id = adb.ModelSpace.ObjectId;
                if (!id.IsValid) return;
                id.InsertBlockReference(layerName, blkName, basePt, new Scale3d(1), 0);
            });
        }
        public static Polyline DrawBoundaryLazy(params Entity[] ents)
        {
            return DrawBoundaryLazy(ents, 2);
        }
        public static Polyline DrawBoundaryLazy(Entity[] ents, double thickness)
        {
            if (ents.Length == 0) return null;
            var lst = ents.Select(e => GeoAlgorithm.GetBoundaryRect(e)).ToList();
            var minx = lst.Select(r => r.MinX).Min();
            var miny = lst.Select(r => r.MinY).Min();
            var maxx = lst.Select(r => r.MaxX).Max();
            var maxy = lst.Select(r => r.MaxY).Max();
            var pl = DrawRectLazy(new GRect(minx, miny, maxx, maxy));
            pl.ConstantWidth = thickness;
            return pl;
        }
        public static void DrawBoundaryLazy(Entity e, double thickness = 2)
        {
            DrawingQueue.Enqueue(adb => { _DrawBoundary(adb.Database, e, thickness); });
        }
        public static void _DrawBoundary(Database db, Entity e, double thickness)
        {
            //if (e is BlockReference br)
            //{
            //    var colle = br.ExplodeToDBObjectCollection();
            //    ThMEPWSS.Uitl.DebugNs.DebugTool.DrawBoundary(db, thickness, colle.OfType<Entity>().ToArray());
            //    foreach (Entity ent in colle)
            //    {
            //        ThMEPWSS.Uitl.DebugNs.DebugTool.DrawBoundary(db, thickness, ent);
            //    }
            //}
            //else
            //{
            //    ThMEPWSS.Uitl.DebugNs.DebugTool.DrawBoundary(db, thickness, e);
            //}

            ThMEPWSS.Uitl.DebugNs.DebugTool.DrawBoundary(db, thickness, e);
        }
        public static Polyline DrawRectLazyFromLeftButtom(Point3d leftButtom, double width, double height)
        {
            return DrawRectLazy(leftButtom, new Point3d(leftButtom.X + width, leftButtom.Y + height, leftButtom.Z));
        }
        public static Polyline DrawRectLazyFromLeftTop(Point3d leftButtom, double width, double height)
        {
            return DrawRectLazy(leftButtom, new Point3d(leftButtom.X + width, leftButtom.Y - height, leftButtom.Z));
        }
        public static void DrawGVectorLazy(GVector gv, Action<Entity> cb = null)
        {
            DrawingQueue.Enqueue(adb =>
            {
                if (gv.Vector.Length > 0)
                {
                    //var e = new Leader();
                    //e.HasArrowHead = true;
                    //var v = gv.Vector.Length / 2;
                    //if (v > 200) v = 200;
                    //e.Dimasz = v;
                    //e.AppendVertex((gv.EndPoint).ToPoint3d());
                    //e.AppendVertex(gv.StartPoint.ToPoint3d());
                    //cb?.Invoke(e);
                    //DrawEntityLazy(e);

                    DrawLineLazy(gv.StartPoint, gv.EndPoint);
                    var v = gv.Vector.Length / 4;
                    if (v > 200) v = 200;
                    DrawCircleLazy(gv.EndPoint.ToPoint3d(), v);
                }
            });
        }
        public static Line DrawLineSegmentLazy(GLineSegment seg)
        {
            return DrawLineLazy(seg.StartPoint, seg.EndPoint);
        }
        public static List<Line> DrawLineSegmentsLazy(IEnumerable<GLineSegment> segs)
        {
            var lines = segs.Select(seg=> new Line() { StartPoint = seg.StartPoint.ToPoint3d(), EndPoint = seg.EndPoint.ToPoint3d() }).ToList();
            DrawingQueue.Enqueue(adb =>
            {
                foreach (var line in lines)
                {
                    adb.ModelSpace.Add(line);
                }
            });
            return lines;
        }
        public static Polyline DrawLineSegmentLazy(GLineSegment seg, double width)
        {
            var pl = DrawPolyLineLazy(new Point2d[] { seg.StartPoint, seg.EndPoint });
            pl.ConstantWidth = width;
            return pl;
        }
        public static Polyline DrawLineSegmentBufferLazy(GLineSegment seg, double bufSize)
        {
            var pl = ThCADCoreNTSOperation.Buffer(seg.ToCadLine(), bufSize);
            DrawEntityLazy(pl);
            return pl;
        }
        public static Polyline DrawPolyLineLazy(Coordinate[] coordinates)
        {
            return DrawPolyLineLazy(coordinates.Select(c => c.ToPoint3d()).ToArray());
        }
        public static Polyline DrawPolyLineLazy(GLineSegment seg)
        {
            var c = new Point2dCollection() { seg.StartPoint, seg.EndPoint };
            var pl = new Polyline();
            PolylineTools.CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
        public static Polyline DrawRectLazy(GRect rect)
        {
            return DrawRectLazyFromLeftTop(new Point2d(rect.MinX, rect.MaxY).ToPoint3d(), rect.Width, rect.Height);
        }
        public static Polyline DrawRectLazy(Point3d pt1, Point3d pt2)
        {
            var polyline = ThMEPWSS.Uitl.DebugNs.DebugTool.CreateRectangle(pt1.ToPoint2D(), pt2.ToPoint2D());
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(polyline);
            });
            return polyline;
        }
        public static Point3d GetMidPoint(Point3d first, Point3d second)
        {
            var x = (first.X + second.X) / 2;
            var y = (first.Y + second.Y) / 2;

            return new Point3d(x, y, 0);
        }
        public static Circle DrawCircleLazy(GRect rect)
        {
            var p1 = new Point3d(rect.MinX, rect.MinY, 0);
            var p2 = new Point3d(rect.MaxX, rect.MaxY, 0);
            var center = GetMidPoint(p1, p2);
            var radius = GeoAlgorithm.Distance(p1, p2) / 2;
            return DrawCircleLazy(center, radius);
        }
        public static Circle DrawCircleLazy(Point2d center, double radius)
        {
            return DrawCircleLazy(center.ToPoint3d(), radius);
        }
        public static Circle DrawCircleLazy(Point3d center, double radius)
        {
            if (radius <= 0) radius = 1;
            var circle = new Circle() { Center = center, Radius = radius };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(circle);
            });
            return circle;
        }
        public static Polyline DrawPolyLineLazy(params Point2d[] pts)
        {
            var c = new Point2dCollection();
            foreach (var pt in pts)
            {
                c.Add(pt);
            }
            var pl = new Polyline();
            PolylineTools.CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
        public static Polyline DrawPolyLineLazy(params Point3d[] pts)
        {
            var c = new Point2dCollection();
            foreach (var pt in pts)
            {
                c.Add(pt.ToPoint2d());
            }
            var pl = new Polyline();
            PolylineTools.CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
        public static List<Line> DrawLinesLazy(params Point2d[] pts)
        {
            return DrawLinesLazy((IList<Point2d>)pts);
        }
        public static List<Line> DrawLinesLazy(params Point3d[] pts)
        {
            return DrawLinesLazy((IList<Point3d>)pts);
        }
        public static List<Line> DrawLinesLazy(IList<Point3d> pts)
        {
            var ret = new List<Line>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var line = DrawLineLazy(pts[i], pts[i + 1]);
                ret.Add(line);
            }
            return ret;
        }
        public static List<Line> DrawLinesLazy(IList<Point2d> pts)
        {
            var ret = new List<Line>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var line = DrawLineLazy(pts[i], pts[i + 1]);
                ret.Add(line);
            }
            return ret;
        }
        public static Line DrawLineLazy(double x1, double y1, double x2, double y2)
        {
            return DrawLineLazy(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
        }
        public static Line DrawLineLazy(Point2d start, Point2d end)
        {
            return DrawLineLazy(start.ToPoint3d(), end.ToPoint3d());
        }
        public static Line DrawLineLazy(Point3d start, Point3d end)
        {
            var line = new Line() { StartPoint = start, EndPoint = end };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(line);
            });
            return line;
        }
        public static Line DrawTextAndLinesLazy(DBText t1, DBText t2, double extH, double extV)
        {
            var r1 = GeoAlgorithm.GetBoundaryRect(t1);
            var r2 = GeoAlgorithm.GetBoundaryRect(t2);
            var pts = new Point3d[]
            {
                r1.LeftButtom.OffsetXY(-extH, -extV).ToPoint3d(), r1.RightButtom.OffsetXY(extH, -extV).ToPoint3d(),
                r2.LeftButtom.OffsetXY(-extH, -extV).ToPoint3d(), r2.RightButtom.OffsetXY(extH, -extV).ToPoint3d(),
            };
            var r = GeoAlgorithm.GetGRect(pts);
            var pt1 = GeoAlgorithm.MidPoint(r.LeftTop, r.LeftButtom).ToPoint3d();
            var pt2 = GeoAlgorithm.MidPoint(r.RightTop, r.RightButtom).ToPoint3d();
            return DrawLineLazy(pt1, pt2);
        }

        public static Line DrawTextUnderlineLazy(DBText t, double extH, double extV)
        {
            var r = GeoAlgorithm.GetBoundaryRect(t);
            return DrawLineLazy(r.LeftButtom.OffsetXY(-extH, -extV).ToPoint3d(), r.RightButtom.OffsetXY(extH, -extV).ToPoint3d());
        }
        public static DBText DrawTextLazy(string text, double height, Point2d position) => DrawTextLazy(text, height, position.ToPoint3d());
        public static DBText DrawTextLazy(string text, Point2d position)
        {
            return DrawTextLazy(text, position.ToPoint3d());
        }
        public static DBText DrawTextLazy(string text, Point3d position)
        {
            return DrawTextLazy(text, 100, position);
        }
        public static DBText DrawTextLazy(string text, double height, Point3d position, Action<DBText> cb = null)
        {
            var dbText = new DBText
            {
                TextString = text,
                Position = position,
                Height = height,
            };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(dbText);
                cb?.Invoke(dbText);
            });
            return dbText;
        }
        public static List<ObjectId> DrawProfile(List<Curve> curves, string LayerName, Color color = null)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                if (color == null)
                    CreateLayer(LayerName, Color.FromRgb(255, 0, 0));
                else
                    CreateLayer(LayerName, color);

                foreach (var curve in curves)
                {
                    var clone = curve.Clone() as Curve;
                    clone.Layer = LayerName;
                    objectIds.Add(db.ModelSpace.Add(clone));
                }
            }

            return objectIds;
        }
        public static Polyline DrawLineString(LineString lineString)
        {
            var points = new Point3d[lineString.NumPoints];
            for (int i = 0; i < lineString.NumPoints; i++)
            {
                var pt = lineString.GetPointN(i);
                var p = new Point3d(pt.X, pt.Y, pt.Z);
                points[i] = p;
            }
            return DrawPolyLineLazy(points);
        }
        public static Polyline DrawLinearRing(LinearRing ring)
        {
            var points = new Point3d[ring.NumPoints];
            for (int i = 0; i < ring.NumPoints; i++)
            {
                var pt = ring.GetPointN(i);
                var p = new Point3d(pt.X, pt.Y, pt.Z);
                points[i] = p;
            }
            return DrawPolyLineLazy(points);
        }

        /// <summary>
        /// 创建新的图层
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        public static void CreateLayer(string aimLayer, Color color)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.Equals(aimLayer))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    layerRecord.Color = color;
                    layerRecord.IsPlottable = false;
                }
            }
        }
    }
    public static class ThBlock
    {
        public static bool IsSupportedBlock(BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
            if (blockTableRecord.IsDynamicBlock) return false;
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous) return false;
            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable) return false;
            return true;
        }
    }
}
