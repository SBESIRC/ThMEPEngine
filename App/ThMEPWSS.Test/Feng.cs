

namespace ThMEPWSS.TestNs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
    using System.Reflection;
    using System.Windows.Forms;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Autodesk.AutoCAD.EditorInput;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Engine;
    using Autodesk.AutoCAD.DatabaseServices;
    using System.Diagnostics;
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using Autodesk.AutoCAD.Internal;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.DebugNs;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Runtime.Remoting;
    using PolylineTools = Pipe.Service.PolylineTools;
    using CircleTools = Pipe.Service.CircleTools;
    using System.IO;
    using Autodesk.AutoCAD.Runtime;
    using ThMEPWSS.Pipe;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using System.Collections;
    using ThCADCore.NTS.IO;
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Operation.Linemerge;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using ThMEPEngineCore.Algorithm;
    using ThMEPWSS.DebugNs;
    using static ThMEPWSS.DebugNs.Util1;
    public class Feng
    {
        public static void Main(string[] args)
        {
            //Console.WriteLine("hello");
            //Console.WriteLine(1/Math.Tan(90.0.AngleFromDegree()));
            Console.WriteLine(1 / Math.Tan(Math.PI / 2));
        }
        [Feng("雨水管径100")]
        public static void quexpp()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt:Dbg.SelectPoint(), layer: "W-NOTE", props: new Dictionary<string, string>() { { "可见性", /*Dr.GetFloorDrainDN()*/"DN25" }, { "角度1", "90" } });
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                //{
                //    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                //});

                //DU.DrawBlockReference(blkName: "雨水管径100",scale:.5, basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                DU.DrawBlockReference(blkName: "雨水管径100", scale: 1, basePt: Dbg.SelectPoint().OffsetX(600), layer: "W-NOTE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                    //br.ObjectId.SetDynBlockValue("角度1", Math.PI / 2);
                    br.ObjectId.SetDynBlockValue("角度1", Math.PI);
                });
            }
        }
        [Feng]
        public static void test2()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint();
                var dn = "DN25";
                //var angle = Math.PI / 2;
                var angle = Math.PI;
                DU.DrawBlockReference(blkName: "雨水管径100", scale: 1, basePt: basePt, layer: "W-NOTE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("可见性", dn);
                    br.ObjectId.SetDynBlockValue("角度1", Math.PI);
                });
            }
        }
        [Feng]
        public static void test()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawingTransaction.CreateWithFbk(adb))
            {
                var pt = Dbg.SelectPoint();
                var fbk = FastBlock.Create(adb);
                for (int i = 0; i < 1; i++)
                //for (int i = 0; i < 1; i++)
                {
                    var dn = "DN25";
                    var angle = Math.PI / 2;
                    //var angle = Math.PI;
                    //var angle = 0;
                    Dr.InsetDNBlock(pt, dn, angle, .5);
                }

            }
        }
        public static void quf6ek()
        {

            //for (int i = 0; i < 1000; i++)
            //{
            //    var dn = "DN25";
            //    var angle = Math.PI / 2;
            //    NewMethod9(pt, fbk, dn, angle);
            //}
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
                        NewMethod8(attNameValues, db, br, attDef);
                    }
                }
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            return br.ObjectId;//返回添加的块参照的Id
        }

        private static void NewMethod8(Dictionary<string, string> d, Database db, BlockReference br, AttributeDefinition attDef)
        {
            var attribute = FastBlock.CreateAttribute(db, br, attDef);
            //判断是否包含指定的属性名称
            {
                var s = attDef.Tag.ToUpper();
                if (d.ContainsKey(s))
                {
                    //设置属性值
                    attribute.TextString = d[s].ToString();
                }
            }
            FastBlock.AppendAttribute(db, br, attribute);
        }


        //雨水斗对位研究
        private static void NewMethod2()
        {
            var r1 = "{'type':'GRect','values':[1292293.678676707,581253.83417658461,1392293.678676707,699247.960999981]}".FromCadJson<GRect>();
            var r2 = "{'type':'GRect','values':[1292293.678676707,431253.83417658461,1392293.678676707,549247.960999981]}".FromCadJson<GRect>();
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "重力流雨水斗"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "GYYR"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                //foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e=>e.Layer=="W-RAIN-EQPM"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                var pts1 = new List<Point2d>();
                var pts2 = new List<Point2d>();
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "GYYR"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r1.ContainsRect(bd))
                    {
                        pts1.Add(bd.Center);
                    }
                }
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "重力流雨水斗"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r1.ContainsRect(bd))
                    {
                        pts1.Add(bd.Center);
                    }
                }
                //foreach (var pt in pts1)
                //{
                //    Dbg.ShowWhere(pt);
                //}

                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r2.ContainsRect(bd))
                    {
                        pts2.Add(bd.Center);
                    }
                }
                //foreach (var pt in pts2)
                //{
                //    Dbg.ShowWhere(pt);
                //}

                var p1 = r1.LeftTop;
                var p2 = r2.LeftTop;
                var pts3 = pts1.Select(p => (p - p1).ToPoint2d()).ToList();
                var pts4 = pts2.Select(p => (p - p2).ToPoint2d()).ToList();
                for (int i = 0; i < pts3.Count; i++)
                {
                    for (int j = 0; j < pts4.Count; j++)
                    {
                        if (pts3[i].GetDistanceTo(pts4[j]) < 50)
                        {
                            Dbg.ShowWhere(pts1[i]);
                            Dbg.ShowWhere(pts2[j]);
                        }
                    }
                }
            }
        }

        private static void NewMethod1()
        {
            var p1 = new Point2d(0, 10);
            var p2 = new Point2d(10, 0);
            var p3 = new Point2d(10, 10);
            var p4 = Point2d.Origin;
            var line1 = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p3.ToNTSCoordinate(), p4.ToNTSCoordinate() });
            var line2 = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p1.ToNTSCoordinate(), p2.ToNTSCoordinate() });
            var geo = line1.Intersection(line2) as Point;
            Dbg.PrintLine(geo.ToString());
        }

        private static void NewMethod()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                {
                    var p1 = new Point2d(0, 10);
                    var p2 = new Point2d(10, 0);
                    var p3 = new Point2d(20, 0);
                    var line = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p2.ToNTSCoordinate(), p3.ToNTSCoordinate() });
                    var p = p1.ToNTSPoint();
                    Dbg.PrintLine(p.Distance(line));
                }

                {
                    var p1 = new Point2d(0, 10);
                    var p2 = new Point2d(10, 0);
                    var p3 = new Point2d(-10, 0);
                    var line = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p2.ToNTSCoordinate(), p3.ToNTSCoordinate() });
                    var p = p1.ToNTSPoint();
                    Dbg.PrintLine(p.Distance(line));
                }
            }
        }

        private static void qu0lsq()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //var r1 = new GRect(0, 0, 10, 10);
                //var r2 = new GRect(5, 5, 20, 20);
                //var geo = GeometryFac.CreateGeometry(new Geometry[] { r1.ToPolygon(), r2.ToPolygon() });
                //DU.DrawGeometryLazy(geo);
                //Dbg.ChangeCadScreenTo(geo.EnvelopeInternal.ToGRect());

                var r1 = new GRect(0, 0, 10, 10).ToPolygon();
                var r2 = new GRect(5, 5, 20, 20).ToPolygon();
                //var geo = r1.Union(r2);
                var geo = r1.Intersection(r2);
                //var geo = r1.Difference(r2);
                DU.DrawGeometryLazy(geo);
                //Dbg.ChangeCadScreenTo(geo.EnvelopeInternal.ToGRect());

                Dbg.FocusMainWindow();
            }
        }

        [Feng("qvjrf9")]
        public static void qvjrf9()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Console.WriteLine(Dbg.SelectEntity<Entity>(adb).Bounds.ToGRect().ToCadJson());
            }
        }
        [Feng("qvjs5p")]
        public static void qvjs5p()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "地漏平面"))
                {
                    Dbg.ShowWhere(e);
                }
                DU.Draw(adb);
            }
        }
        [Feng("qvjptc")]
        public static void qvjptc()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            {
                var r = "{'type':'GRect','values':[1320824.8471009475,784824.70226323092,1323311.1004554592,787140.50510722329]}".FromCadJson<GRect>();
                using (var trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    var ed = Active.Editor;
                    var opt = new PromptEntityOptions("请选择");
                    var ret = ed.GetEntity(opt);
                    if (ret.Status == PromptStatus.OK)
                    {
                        var br = (BlockReference)trans.GetObject(ret.ObjectId, OpenMode.ForWrite, false);
                        br.ExplodeToOwnerSpace();
                        using (var adb = AcadDatabase.Use(br.Database))
                        {
                            foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "地漏平面"))
                            {
                                Dbg.ShowWhere(e);
                            }
                            DU.Draw(adb);
                        }
                    }

                    trans.Abort();
                    //trans.Commit();
                }
            }
            //using (Dbg.DocumentLock)
            //using (var adb = AcadDatabase.Active())
            //using (var tr = new DrawingTransaction(adb))
            //{
            //    var db = adb.Database;
            //    Dbg.BuildAndSetCurrentLayer(db);
            //    //var brs = Dbg.SelectEntity<BlockReference>(adb).ExplodeToDBObjectCollection().OfType<BlockReference>().ToList();
            //    ////Console.WriteLine(brs.Select(x => x.IsDynamicBlock).ToCadJson());
            //    ////Console.WriteLine(brs.Select(x => x.GetBlockName()).ToCadJson());               
            //    //foreach (var br in brs)
            //    //{
            //    //    br.ExplodeToOwnerSpace();
            //    //}

            //    //Dbg.SelectEntity<BlockReference>(adb).ExplodeToOwnerSpace();

            //    //using (var trans = adb.Database.TransactionManager.StartTransaction())
            //    //{
            //    //    var ed = Active.Editor;
            //    //    var opt = new PromptEntityOptions("请选择");
            //    //    var ret = ed.GetEntity(opt);
            //    //    if (ret.Status == PromptStatus.OK)
            //    //    {
            //    //        var br=(BlockReference)trans.GetObject(ret.ObjectId, OpenMode.ForWrite, false);
            //    //        br.ExplodeToOwnerSpace();
            //    //    }
            //    //    trans.Abort();
            //    //}
            //}
        }
        [Feng("qvjo7o")]
        public static void qvjo7o()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                //Console.WriteLine(Dbg.SelectEntity<BlockReference>(adb).ExplodeToDBObjectCollection().OfType<BlockReference>().Select(x => x.Name).ToCadJson()); ;
                //Console.WriteLine(Dbg.SelectEntity<BlockReference>(adb).ExplodeToDBObjectCollection().OfType<BlockReference>().Select(x => x.IsDynamicBlock).ToCadJson()); 

                //foreach (var br in Dbg.SelectEntity<BlockReference>(adb).ExplodeToDBObjectCollection().OfType<BlockReference>())
                //{
                //    adb.ModelSpace.Add(br);
                //}

            }
        }
        //[Feng("db.SetCurrentLayer")]
        public static void qvjnyy()
        {
            using (var adb = AcadDatabase.Active())
            {
                adb.Database.SetCurrentLayer("0");
            }
        }

        [Feng("ToObb")]
        public static void quiwqs()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<BlockReference>(adb);
                var pl = e.ToObb();
                DU.DrawEntityLazy(pl);
            }
        }

        [Feng("xxx")]
        public static void qu5x6r()
        {
            NewMethod3();
            //Dbg.FocusMainWindow();
            //using (Dbg.DocumentLock)
            //using (var adb = AcadDatabase.Active())
            //using (var tr = new DrawingTransaction(adb))
            //{
            //    var br=Dbg.SelectEntity<BlockReference>(adb);
            //    br.ObjectId.SetDynBlockValue("距离1", 1000.0);
            //    br.ObjectId.SetDynBlockValue("距离1", 2000.0);
            //    //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
            //}
        }

        private static void NewMethod7()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var range = Dbg.SelectRange();
                var geoData = new RainSystemGeoData();
                geoData.Init();
                ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                foreach (var r in geoData.GravityWaterBuckets)
                {
                    DU.DrawRectLazy(r);
                }
            }
        }
        [Feng]
        public static void xx()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint();
                DU.DrawBlockReference(blkName: "通气帽系统", basePt: basePt, layer: "W-DRAI-DOME-PIPE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("距离1", 2000.0);
                    br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                });
            }
        }

        //好奇怪。。。目前搞不定了
        //也可能是图块本身有问题
        //还真是。。。
        private static void NewMethod3()
        {
            ObjectId id = default;
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint();
                DU.DrawBlockReference(blkName: "通气帽系统", basePt: basePt, layer: "W-DRAI-DOME-PIPE", cb: br =>
                {
                    //if (br.IsDynamicBlock)
                    //{
                    //    var props = br.DynamicBlockReferencePropertyCollection;
                    //    foreach (DynamicBlockReferenceProperty prop in props)
                    //    {
                    //        if (prop.ReadOnly) continue;
                    //        if (prop.PropertyName == "距离1") prop.Value = 2000.0;
                    //        //if (prop.PropertyName == "可见性1") prop.Value = "伸顶通气管";
                    //    }
                    //}
                    //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                    //br.ObjectId.SetDynBlockValue("距离1", 2000.0);
                    //br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");

                    br.ResetBlock();

                    br.ObjectId.SetDynBlockValue("距离1", (double)1000.0);
                    //br.ResetBlock();
                    //br.ObjectId.SetDynBlockValue("距离1", (double)1000.0);
                    //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                    //br.ResetBlock();
                    //br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
                    //br.ResetBlock();
                    //br.ObjectId.SetDynBlockValue("距离1", (double)3000.0);
                    id = br.ObjectId;
                    Dbg.UpdateScreen();
                });
            }

            //using (Dbg.DocumentLock)
            //using (var adb = AcadDatabase.Active())
            //using (var tr = new DrawingTransaction(adb))
            //{
            //    var br = adb.Element<BlockReference>(id);
            //    br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
            //    br.ObjectId.SetDynBlockValue("距离1", 2000.0);
            //    br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
            //    Dbg.UpdateScreen();
            //}
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var br = adb.Element<BlockReference>(id);
                var o = adb.Element<BlockTableRecord>(br.BlockTableRecord);

                o.UpdateAnonymousBlocks();
                //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                Dbg.UpdateScreen();
            }
            //using (Dbg.DocumentLock)
            //using (var adb = AcadDatabase.Active())
            //using (var tr = new DrawingTransaction(adb))
            //{
            //    var br = adb.Element<BlockReference>(id);
            //    //br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
            //    Dbg.UpdateScreen();
            //}

        }
        [Feng("xx")]
        public static void qvkflv()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                //var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                //var lines = adb.ModelSpace.OfType<Line>().Where(e => e.Layer == "W-FRPT-HYDT-PIPE").Select(x => x.ToGLineSegment()).Where(x => x.IsValid).ToList();
                //foreach (var line in AutoConn(lines))
                //{
                //    DU.DrawLineSegmentBufferLazy(line, 10).ColorIndex = 4;
                //}

                //var v1=Dbg.SelectEntity<Line>(adb).ToGLineSegment().ToVector2d();
                //var v2 = Dbg.SelectEntity<Line>(adb).ToGLineSegment().ToVector2d();
                //Console.WriteLine(v1.GetAngleTo(v2).AngleToDegree());
                //Console.WriteLine(v2.GetAngleTo(v1).AngleToDegree());

                //var cl = new ThDrainageSystemServiceGeoCollector() { adb = adb, };
                //cl.PreExplode();
                //foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && (x.GetEffectiveName()?.Contains("地漏") ?? false)))
                //{
                //Dbg.ShowWhere(br);

                //}
            }
        }
        private static void NewMethod4()
        {
            var OFFSET_X = 2500;
            var SPAN_X = 5500;
            var HEIGHT = 1800;
            var COUNT = 20;
            var basePt = Dbg.SelectPoint();
            var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
            var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            for (int i = 0; i < storeys.Count; i++)
            {
                var storey = storeys[i];
                var bsPt1 = basePt.OffsetY(HEIGHT * i);
                DrainageSystemDiagram.DrawStoreyLine(storey, bsPt1, lineLen);
                for (int j = 0; j < COUNT; j++)
                {
                    var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);
                    Dbg.ShowXLabel(bsPt2);
                }
            }
        }
        [Feng("draw8#1")]
        public static void qvg1qe()
        {
            DrainageSystemDiagram.qvg1qe();
        }
        [Feng("draw8#2")]
        public static void qvg1vf()
        {
            DrainageSystemDiagram.qvg1vf();
        }
        [Feng("01")]
        public static void qvg7cd()
        {
            DrainageSystemDiagram.qvg7cd();
        }
        [Feng("02")]
        public static void qvg7cs()
        {
            DrainageSystemDiagram.qvg7cs();
        }
        [Feng("draw5")]
        public static void qv3kmr()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint();
                DrainageSystemDiagram.draw5(basePt.ToPoint2d());
            }
        }

        [Feng("排出方式连线")]
        public static void qv1735()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                {
                    //480 1080 
                    {
                        var bsPt = Dbg.SelectPoint();
                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-2000, -121) };
                        var segs = points.ToGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                    {
                        var bsPt = Dbg.SelectPoint();
                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-5300, -121) };
                        var segs = points.ToGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                    {
                        var bsPt = Dbg.SelectPoint();
                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(0, -1379), new Point2d(-121, -1500), new Point2d(-5900, -1500) };
                        var segs = points.ToGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                }
            }
        }
        [Feng("立管检查口")]
        public static void qv17o4()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference(blkName: "立管检查口", basePt: bsPt,
               cb: br =>
               {
                   br.ScaleFactors = new Scale3d(-1, 1, 1);
                   br.Layer = "W-DRAI-EQPM";
               });
                //right
                DU.DrawBlockReference(blkName: "立管检查口", basePt: bsPt,
               cb: br =>
               {
                   br.Layer = "W-DRAI-EQPM";
               });
            }

        }
        [Feng("污废合流井编号")]
        public static void qv16xv()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
               scale: 0.5,
               props: new Dictionary<string, string>() { { "-", "666" } },
               cb: br =>
               {
                   br.Layer = "W-DRAI-EQPM";
               });
            }
        }
        [Feng("阳台支管块")]
        public static void qv16vz()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                DU.DrawBlockReference("阳台支管块", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(270);
                });
            }
        }
        [Feng("套管系统")]
        public static void qv16k0()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                DU.DrawBlockReference("套管系统", bsPt, br =>
                {
                    br.Layer = "W-BUSH";
                });
            }
        }
        [Feng("P型存水弯")]
        public static void qv77sl()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                //left
                {
                    var v = new Vector3d(383875.8169, -250561.9571, 0);
                    DU.DrawBlockReference("P型存水弯", bsPt - v, br =>
                    {
                        br.Layer = "W-DRAI-EQPM";
                        br.ScaleFactors = new Scale3d(2, 2, 2);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "板上P弯");
                        }
                    });
                }
                //right
                {
                    var v = new Vector3d(-383875.8169, -250561.9571, 0);
                    DU.DrawBlockReference("P型存水弯", bsPt - v, br =>
                    {
                        br.Layer = "W-DRAI-EQPM";
                        br.ScaleFactors = new Scale3d(-2, 2, 2);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "板上P弯");
                        }
                    });
                }
            }
        }
        [Feng("qvgbqf")]
        public static void qvgbqf()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Console.WriteLine(adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-DRAI-WAST-PIPE").Count());
            }
        }
        [Feng("侧排地漏")]
        public static void qvgb8o()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                DU.DrawBlockReference("侧排地漏", bsPt - new Vector3d(295694.822273396, 289462.973599816, 0));
            }
        }
        [Feng("侧排地漏test")]
        public static void qvgb5e()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                DU.DrawBlockReference("侧排地漏", bsPt);
                Dbg.AddButton("获取修正量", () =>
                {
                    var pt = Dbg.SelectPoint();
                    var v = pt - bsPt;
                    Console.WriteLine($"new Vector3d({v.X},{v.Y},0)");
                });
            }
        }
        [Feng("P型存水弯test")]
        public static void qv7791()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                //哇，这玩意基点太远了
                //left
                DU.DrawBlockReference("P型存水弯", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上P弯");
                    }
                });
                Dbg.AddButton("获取修正量", () =>
                {
                    var pt = Dbg.SelectPoint();
                    //Console.WriteLine((pt - bsPt).ToCadJson());
                    var v = pt - bsPt;
                    Console.WriteLine($"new Vector3d({v.X},{v.Y},0)");
                });
            }
        }
        [Feng("DN100")]
        public static void qvyyfh()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint();
                Dbg.ShowXLabel(basePt);
                var t = DU.DrawTextLazy("DN100", 350, basePt);
                t.Rotate(basePt, 90.0.AngleFromDegree());
                Dr.SetLabelStylesForRainDims(t);
            }
        }
        [Feng("管底H+X.XX")]
        public static void qvyxpq()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                DU.DrawBlockReference(blkName: "标高", basePt: bsPt,
               props: new Dictionary<string, string>() { { "标高", "管底H+X.XX" } },
               cb: br =>
               {
                   br.Layer = "W-DRAI-NOTE";
               });
            }
        }
        [Feng("地漏系统")]
        public static void qv1o5m()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference("地漏系统", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                    }
                });
                //right
                DU.DrawBlockReference("地漏系统", bsPt,
                    br =>
                    {
                        br.Layer = "W-DRAI-EQPM";
                        br.ScaleFactors = new Scale3d(-2, 2, 2);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                        }
                    });
            }
        }
        [Feng("S型存水弯")]
        public static void qv1mr0()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference("S型存水弯", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                    }
                });
                //right
                DU.DrawBlockReference("S型存水弯", bsPt,
                    br =>
                    {
                        br.Layer = "W-DRAI-EQPM";
                        br.ScaleFactors = new Scale3d(-2, 2, 2);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                        }
                    });
            }
        }
        [Feng("洗涤盆排水")]
        public static void qv15o8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference("洗涤盆排水", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
                //right
                DU.DrawBlockReference("双格洗涤盆排水", bsPt,
                    br =>
                    {
                        br.Layer = "W-DRAI-EQPM";
                        br.ScaleFactors = new Scale3d(-2, 2, 2);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                        }
                    });
            }
        }
        [Feng("清扫口系统")]
        public static void qv15je()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                //left
                DU.DrawBlockReference("清扫口系统", bsPt, scale: 2, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                });
                //right
                DU.DrawBlockReference("清扫口系统", bsPt, scale: 2, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90 + 180);
                });
            }
        }
        [Feng("翻转测试")]
        public static void qv147n()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                {
                    var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-1379, -121), new Point2d(-1500, -241) };
                    {
                        var segs = points.ToGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                    {
                        var m = Matrix2dFac.YAxisMirroring;
                        var segs = points.ToGLineSegments(bsPt, m);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                }

            }
        }
        [Feng("雨水管径100")]
        public static void quev3n()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt:Dbg.SelectPoint(), layer: "W-NOTE", props: new Dictionary<string, string>() { { "可见性", /*Dr.GetFloorDrainDN()*/"DN25" }, { "角度1", "90" } });
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                //{
                //    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                //});

                //DU.DrawBlockReference(blkName: "雨水管径100",scale:.5, basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                DU.DrawBlockReference(blkName: "雨水管径100", scale: 1, basePt: Dbg.SelectPoint().OffsetX(600), layer: "W-NOTE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                    //br.ObjectId.SetDynBlockValue("角度1", Math.PI / 2);
                    br.ObjectId.SetDynBlockValue("角度1", Math.PI);
                });
            }

        }
        [Feng("画标高")]
        public static void NewMethod6()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var pt = Dbg.SelectPoint();
                ThRainSystemService.ImportElementsFromStdDwg();
                DU.DrawBlockReference(blkName: "标高", basePt: pt, layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", "666" } });
            }
        }
        [Feng("画系统图草稿1")]
        public static void qu600o()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var OFFSET_X = 2500.0;
                var SPAN_X = 5500.0;
                var HEIGHT = 1800.0;
                var COUNT = 20;
                var basePt = Dbg.SelectPoint();
                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePt.OffsetY(HEIGHT * i);
                    DrainageSystemDiagram.DrawStoreyLine(storey, bsPt1, lineLen);
                    for (int j = 0; j < COUNT; j++)
                    {
                        var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);

                        if (j == 0)
                        {
                            Dr.DrawCheckPoint(bsPt2);
                        }
                        else if (j == 1)
                        {
                            DU.DrawBlockReference("清扫口系统", bsPt2.OffsetY(HEIGHT - 300), scale: 2, cb: br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                            });
                        }
                        else if (j == 2)
                        {
                            DU.DrawBlockReference("洗涤盆排水", bsPt2.OffsetXY(-1000, HEIGHT - 1720 - 80 + 200), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                if (br.IsDynamicBlock)
                                {
                                    br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                                }
                            });
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, 200), bsPt2.OffsetXY(0, 200));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 3)
                        {
                            Dr.DrawFloorDrain(bsPt2.OffsetX(180 - 1000));
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, -550), bsPt2.OffsetXY(0, -550));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 4)
                        {
                            DU.DrawBlockReference("阳台支管块", bsPt2.OffsetXY(-328257, 35827), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(270);
                            });
                        }
                        else if (j == 5)
                        {
                            var line = DU.DrawLineLazy(bsPt2.OffsetXY(-200, HEIGHT - 550), bsPt2.OffsetXY(200, HEIGHT - 550));
                            line.Layer = "W-DRAI-NOTE";
                            line.ColorIndex = 256;
                        }
                    }
                }
                for (int j = 0; j < COUNT; j++)
                {
                    var x = basePt.X + OFFSET_X + (j + 1) * SPAN_X;
                    var y1 = basePt.Y;
                    var y2 = y1 + HEIGHT * (storeys.Count - 1);
                    {
                        var line = DU.DrawLineLazy(x, y1, x, y2);
                        line.Layer = "W-RAIN-PIPE";
                        line.ColorIndex = 256;
                    }
                }

                {
                    var bsPt = basePt.OffsetY(-1000);
                    DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-RAIN-EQPM";
                   });
                }
                {
                    var bsPt = basePt.OffsetXY(500, -1000);
                    DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                   });
                }
            }
        }
        [Feng("画系统图草稿2")]
        public static void qu5x6k()
        {
            NewMethod5();
        }
        private static void NewMethod5()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var OFFSET_X = 2500.0;
                var SPAN_X = 5500.0;
                var HEIGHT = 1800.0;
                var COUNT = 20;
                var basePt = Dbg.SelectPoint();
                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePt.OffsetY(HEIGHT * i);
                    DrainageSystemDiagram.DrawStoreyLine(storey, bsPt1, lineLen);
                    for (int j = 0; j < COUNT; j++)
                    {
                        var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);

                        if (j == 0)
                        {
                            Dr.DrawCheckPoint(bsPt2);
                        }
                        else if (j == 1)
                        {
                            DU.DrawBlockReference("清扫口系统", bsPt2.OffsetY(HEIGHT - 300), scale: 2, cb: br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                            });
                        }
                        else if (j == 2)
                        {
                            DU.DrawBlockReference("洗涤盆排水", bsPt2.OffsetXY(-1000, HEIGHT - 1720 - 80 + 200), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                if (br.IsDynamicBlock)
                                {
                                    br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                                }
                            });
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, 200), bsPt2.OffsetXY(0, 200));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 3)
                        {
                            Dr.DrawFloorDrain(bsPt2.OffsetX(180 - 1000));
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, -550), bsPt2.OffsetXY(0, -550));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 4)
                        {
                            DU.DrawBlockReference("阳台支管块", bsPt2.OffsetXY(-328257, 35827), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(270);
                            });
                        }
                        else if (j == 5)
                        {
                            var line = DU.DrawLineLazy(bsPt2.OffsetXY(-200, HEIGHT - 550), bsPt2.OffsetXY(200, HEIGHT - 550));
                            line.Layer = "W-DRAI-NOTE";
                            line.ColorIndex = 256;
                        }
                    }
                }
                for (int j = 0; j < COUNT; j++)
                {
                    var x = basePt.X + OFFSET_X + (j + 1) * SPAN_X;
                    var y1 = basePt.Y;
                    var y2 = y1 + HEIGHT * (storeys.Count - 1);
                    {
                        var line = DU.DrawLineLazy(x, y1, x, y2);
                        line.Layer = "W-DRAI-DOME-PIPE";
                        line.ColorIndex = 256;
                    }
                    {
                        var line = DU.DrawLineLazy(x + 300, y1, x + 300, y2);
                        line.Layer = "W-DRAI-VENT-PIPE";
                        line.ColorIndex = 256;
                    }
                }

                {
                    var bsPt = basePt.OffsetY(-1000);
                    DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-RAIN-EQPM";
                   });
                }
                {
                    var bsPt = basePt.OffsetXY(500, -1000);
                    DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                   });
                }
            }
        }

        [Feng("qv744d")]
        public static void qv744d()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                Console.WriteLine(e.GetRXClass().DxfName.ToUpper());
                Console.WriteLine(e.Bounds.ToGRect().ToCadJson());
                Console.WriteLine(e.Bounds.ToGRect().Width);
                Dbg.ShowXLabel(e.Bounds.ToGRect().Center);
                Console.WriteLine(e.Bounds.ToGRect().Height);
                Console.WriteLine(GeoAlgorithm.GetBoundaryRect(e.ExplodeToDBObjectCollection().OfType<Entity>().ToArray()).Width);
            }
        }
        [Feng("qv7011")]
        public static void qv7011()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var br = Dbg.SelectEntity<BlockReference>(adb);
                var basePt = br.Bounds.Value.MinPoint;
                var targetPt = Dbg.SelectPoint();
                var blkData = new ThBlockReferenceData(br.ObjectId);
                var ids = Util1.VisibleEntities(blkData, "可见性");
                //Console.WriteLine(ids.Count);
                foreach (ObjectId id in ids)
                {
                    var e = adb.Element<Entity>(id);
                    //Console.WriteLine(e.GetType().ToString());
                    var ltr = adb.Layers.Element(e.Layer);
                    if (Util1.IsVisibleLayer(ltr))
                    {
                        var ee = e.GetTransformedCopy(blkData.BlockTransform);
                        if (ee is Circle circle)
                        {
                            var circle1 = new Circle(circle.Center, Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, circle.Radius);
                            circle1.ColorIndex = 3;
                            circle1.TransformBy(Matrix3d.Displacement(targetPt - basePt));
                            circle1.SetDatabaseDefaults();
                            adb.ModelSpace.Add(circle1);
                        }
                        else if (ee is Line line)
                        {
                            var line1 = new Line() { StartPoint = line.StartPoint, EndPoint = line.EndPoint };
                            line1.ColorIndex = 3;
                            line1.TransformBy(Matrix3d.Displacement(targetPt - basePt));
                            line1.SetDatabaseDefaults();
                            adb.ModelSpace.Add(line1);
                        }
                    }
                }
            }
        }

        [Feng("qvkdxw")]
        public static void qvkdxw()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                DU.DrawLineSegmentBufferLazy(GeoFac.GetCenterLine(Dbg.SelectEntities(adb).OfType<Line>().Select(x => x.ToGLineSegment()).ToList()), 10);
            }
        }
        [Feng("qvvcmm")]
        public static void qvvcmm()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var offsetY = 2000.0;
                var pt = Dbg.SelectPoint();
                DrainageSystemDiagram.DrawAiringSymbol(pt.ToPoint2d(), offsetY);
            }
        }
        [Feng]
        public static void qw6kl8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                {
                    var line = Dbg.SelectEntity<Line>(adb).ToGLineSegment().ToLineString();
                    var po = Dbg.SelectEntity<Circle>(adb).ToGCircle().ToCirclePolygon(36).Shell;
                    //注意去重
                    var polygonizer = new NetTopologySuite.Operation.Polygonize.Polygonizer();
                    polygonizer.Add(new MultiLineString(GeoFac.ToNodedLineSegments(line.ToGLineSegments().Concat(po.ToGLineSegments()).Distinct().ToList()).Select(x => x.ToLineString()).ToArray()));
                    foreach (Polygon poly in polygonizer.GetPolygons())
                    {
                        DU.DrawGeometryLazy(poly.Shell, ents => ents.ForEach(e => e.ColorIndex = 3));
                        Console.WriteLine(poly.Area);
                    }
                }


                {
                    //var line1 = Dbg.SelectEntity<Line>(adb);
                    //var c = Dbg.SelectEntity<Circle>(adb);
                    //var ret = ThCADCore.NTS.ThCADCoreNTSPolygonizer.Polygonize(new DBObjectCollection() { line1, c });
                    //foreach (var g in ret)
                    //{
                    //    DU.DrawGeometryLazy(g);
                    //}
                }
            }
        }
        [Feng("quhax8")]
        public static void quhax8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //立管
                {
                    var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName() == "A$C58B12E6E").ToList();
                    Dbg.PrintLine(list.Count);
                }
                //label
                {
                    var list = adb.ModelSpace.OfType<DBText>().Where(x => x.Layer == "W-DRAI-NOTE").ToList();
                    //Dbg.PrintText(list.Select(x => x.TextString).ToJson());
                }
                //labelline
                {
                    var list = adb.ModelSpace.OfType<Line>().Where(x => x.Layer == "W-DRAI-NOTE").ToList();
                    Dbg.PrintLine(list.Count);
                }
                //dline
                {
                    var list = adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-DRAI-DOME-PIPE" && ThRainSystemService.IsTianZhengElement(x)).ToList();
                    Dbg.PrintLine(list.Count);
                }
            }
        }





        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\湖北交投颐和华府_框线(1)\湖北交投颐和华府_框线\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg";
        public static void qtbzkf()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    //从可见性里拿
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                }

                {
                    var pps = new List<Entity>();
                    //pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));
                    pps.AddRange(entities.Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM") && ThRainSystemService.IsTianZhengElement(x))
                        .Where(x =>
                        {
                            return x.ExplodeToDBObjectCollection().OfType<Circle>().Any();
                        })
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
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

            }
            Util1.AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var wp in wrappingPipes)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });

        }
        //这个看来不太好弄
        //有超大图块
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\庭瑞君越观澜三期\庭瑞君越观澜三期\fs57grhn_w20-地上给水排水平面图.dwg";
        private static void qtaw1x()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "块")
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    //此处的“地漏”字样被放在了“可见性”里
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-DIMS") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                }

                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect();
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

            }
            Util1.AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var wp in wrappingPipes)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });
        }

        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\清江山水四期\清江山水四期\FS55TMPH_W20-地上给水排水平面图.dwg";
        private static void qtas85()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 50000 && r.Height > 10000 && r.Height < 25000)
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR" || x.ObjectId.IsValid && x.GetEffectiveName().Contains("地漏")));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-DIMS") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                }

                {
                    var pps = new List<Entity>();
                    var q = entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100);
                    pps.AddRange(q);
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect();
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(Util1.GetWRainLines(entities));

            }
            Util1.AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });

        }

        //有大图块
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\澳海黄州府（二期）\澳海黄州府（二期）\FS5GMBXU_W20-地上给水排水平面图.dwg";
        private static void qtar8a()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 50000 && r.Height > 10000 && r.Height < 25000)
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE" || e.Layer == "W-FRPT-NOTE") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }
                    //处理label线混用问题
                    //foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-DRAI-NOTE" && e.Length > 0))
                    //{
                    //    labelLines.Add(e.ToGLineSegment());
                    //}

                    foreach (var ent in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                    foreach (var e in entities.OfType<DBText>().Where(x => x.Layer == "W-RAIN-NOTE" || x.Layer == "W-FRPT-NOTE"))
                    {
                        //if (e.TextString.Contains("雨水口") || ThRainSystemService.IsWantedLabelText(e.TextString))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                }

                {
                    var pps = new List<Entity>();
                    var q = entities.OfType<Entity>().Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")).Where(e =>
                    {
                        if (e is Circle) return true;
                        if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            return e.ExplodeToDBObjectCollection().OfType<Circle>().Any();
                        }
                        return false;
                    });
                    pps.AddRange(q);
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        //var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                        //var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                        //if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                        //return GeoAlgorithm.GetBoundaryRect(ent);
                        return GRect.Create(ent.Bounds.ToGRect().Center, 50);
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(Util1.GetWRainLines(entities));
            }
            Util1.AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                }
            });
        }

        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\湖北交投颐和华府\湖北交投颐和华府\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg";
        private static void qtaorg()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);
                {
                    var ents = new List<Entity>();
                    ents.AddRange(adb.ModelSpace.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName().Contains("地漏")));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(adb.ModelSpace.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in adb.ModelSpace.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }
                    //处理label线混用问题
                    foreach (var e in adb.ModelSpace.OfType<Line>().Where(e => e.Layer == "W-DRAI-NOTE" && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }
                    //处理天正单行文字
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                    foreach (var e in adb.ModelSpace.OfType<DBText>().Where(x => x.Layer == "W-RAIN-NOTE"))
                    {
                        if (e.TextString.Contains("雨水口"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                }

                {
                    var pps = new List<Entity>();
                    var q = adb.ModelSpace.OfType<Entity>().Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")).Where(e =>
                    {
                        if (e is Circle) return true;
                        if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            return e.ExplodeToDBObjectCollection().OfType<Circle>().Any();
                        }
                        return false;
                    });
                    pps.AddRange(q);
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        //var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                        //var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                        //if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                        //return GeoAlgorithm.GetBoundaryRect(ent);
                        return GRect.Create(ent.Bounds.ToGRect().Center, 50);
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(Util1.GetWRainLines(adb.ModelSpace.OfType<Entity>()));
            }
            Util1.AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                }
            });
        }

        //有大图块
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\佳兆业滨江新城\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
        private static void qtam9c()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);


                IEnumerable<Entity> GetEntitiesFromLargeBlock()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 50000 && r.Height > 10000 && r.Height < 25000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                    }
                }
                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 50000 && r.Height > 10000 && r.Height < 25000)
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    foreach (var ent in entities.OfType<Entity>().Where(x => x.Layer == "W-RAIN-DIMS" && ThRainSystemService.IsTianZhengElement(x)))
                    {
                        foreach (var br in ent.ExplodeToDBObjectCollection().OfType<BlockReference>())
                        {
                            foreach (var e in br.ExplodeToDBObjectCollection().OfType<Polyline>().Where(x => x.Layer == "0"))
                            {
                                ents.Add(e);
                            }
                        }
                    }
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    //foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                    //{
                    //    labelLines.Add(e.ToGLineSegment());
                    //}

                    //foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    //{
                    //    cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    //}

                    Util1.CollectTianzhengVerticalPipes(labelLines, cts, entities);
                }

                {
                    //ok
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<BlockReference>()
                        .Where(x => x.Layer == "W-RAIN-EQPM")
                     //.Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == "$LIGUAN")//图块炸开的时候就失效了
                     );
                    foreach (var pp in pps)
                    {
                        pipes.Add(GRect.Create(pp.Bounds.ToGRect().Center, 55));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(Util1.GetWRainLines(entities));
            }
            Util1.AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                }
            });
        }


        public static void qtchqu()
        {
            var files = getFiles();
            var lst = new List<RainSystemGeoData>();
            var d = getRangeDict();
            var dict = new Dictionary<string, List<ThStoreysData>>();
            foreach (var file in files)
            {
                var sideWaterBuckets = new List<GRect>();
                var gravityWaterBuckets = new List<GRect>();
                using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                {
                    var range = d[file].ToPoint3dCollection();
                    {
                        var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
                        sidebucketEngine.Recognize(adb.Database, range);
                        var ents = sidebucketEngine.Elements.Select(e => e.Outline).ToList();
                        foreach (var e in ents)
                        {
                            sideWaterBuckets.Add(e.Bounds.ToGRect());
                        }
                    }
                    {
                        var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                        gravityBucketEngine.Recognize(adb.Database, range);
                        var ents = gravityBucketEngine.Elements.Select(g => g.Outline).ToList();
                        foreach (var e in ents)
                        {
                            gravityWaterBuckets.Add(e.Bounds.ToGRect());
                        }
                    }
                }
                var data = new RainSystemGeoData()
                {
                    SideWaterBuckets = sideWaterBuckets,
                    GravityWaterBuckets = gravityWaterBuckets,
                };
                lst.Add(data);
            }
            Dbg.PrintText(ToJson(lst));
        }
        private static void qtchqs()
        {
            var d = getRangeDict();
            var files = getFiles();
            var dict = new Dictionary<string, List<ThStoreysData>>();
            foreach (var file in files)
            {
                using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                {
                    var range = d[file].ToPoint3dCollection();
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    storeysRecEngine.Recognize(adb.Database, range);
                    var list = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().ToList();
                    var lst = new List<ThStoreysData>();
                    foreach (var item in list)
                    {
                        var e = adb.Element<Entity>(item.ObjectId);
                        var data = new ThStoreysData()
                        {
                            Boundary = e.Bounds.ToGRect(),
                            Storeys = item.Storeys,
                            StoreyType = item.StoreyType,
                        };
                        lst.Add(data);
                    }
                    dict[file] = lst;
                }
            }
            Dbg.PrintText(ToJson(dict));
        }

        private static void qtch5n()
        {
            var files = getFiles();
            var file = files.First();
            var d = getRangeDict();
            using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
            {
                var range = d[file].ToPoint3dCollection();
                var storeysRecEngine = new ThStoreysRecognitionEngine();
                storeysRecEngine.Recognize(adb.Database, range);
                var list = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().ToList();
                foreach (var item in list)
                {
                    var e = adb.Element<Entity>(item.ObjectId);
                    //Dbg.PrintLine(e.Bounds.ToGRect().ToJson());
                    //Dbg.PrintLine(item.StoreyType.ToString());
                    //Dbg.PrintLine(item.Storeys.ToJson());
                    var data = new ThStoreysData()
                    {
                        Boundary = e.Bounds.ToGRect(),
                        Storeys = item.Storeys,
                    };
                    Dbg.PrintLine(ToJson(data));
                    Dbg.PrintLine(FromJson<ThStoreysData>(ToJson(data)).ToString());
                    Dbg.PrintLine(ToJson(FromJson<ThStoreysData>(ToJson(data))));
                }
            }

        }

        private static void qtcawk()
        {
            var file1 = @"E:\thepa_workingSpace\任务资料\任务2\210517\蓝光钰泷府二期_框线(1)\蓝光钰泷府二期_框线\FS59P2BC_W20-地上给水排水平面图-副本.dwg";
            var file2 = @"E:\thepa_workingSpace\任务资料\任务2\210517\清江山水四期_框线(1)\清江山水四期_框线\FS55TMPH_W20-地上给水排水平面图.dwg";
            using (var adb1 = AcadDatabase.Open(file1, DwgOpenMode.ReadOnly))
            using (var adb2 = AcadDatabase.Open(file2, DwgOpenMode.ReadOnly))
            {
                Dbg.PrintLine(adb1.ModelSpace.OfType<Entity>().Count());
                Dbg.PrintLine(adb2.ModelSpace.OfType<Entity>().Count());
            }
        }
        private static void qtc5li()
        {
            var storeys = new List<GRect>();
            var gravityWaterBuckets = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                var range = Dbg.SelectRange();
                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }
                {
                    var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                    gravityBucketEngine.Recognize(adb.Database, range);
                    var ents = gravityBucketEngine.Elements.Select(g => g.Outline).ToList();
                    foreach (var e in ents)
                    {
                        gravityWaterBuckets.Add(e.Bounds.ToGRect());
                    }
                }
            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in gravityWaterBuckets)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
            });
        }
        private static void qtc5gl()
        {
            var storeys = new List<GRect>();
            var sideWaterBuckets = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                var range = Dbg.SelectRange();
                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }
                {
                    var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
                    sidebucketEngine.Recognize(adb.Database, range);
                    var ents = sidebucketEngine.Elements.Select(e => e.Outline).ToList();
                    foreach (var e in ents)
                    {
                        sideWaterBuckets.Add(e.Bounds.ToGRect());
                    }
                }
            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in sideWaterBuckets)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
            });
        }
        //下面那堆的模板
        private static void qtc4p1()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    //从可见性里拿
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                }

                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect();
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(GetWRainLines(entities.OfType<Entity>()));

            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var wp in wrappingPipes)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });

        }

        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\清江山水四期_框线(1)\清江山水四期_框线\FS55TMPH_W20-地上给水排水平面图.dwg";
        private static void qtc3yg()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    //从可见性里拿
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-DIMS") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                }

                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<Circle>().Where(c => c.Layer == "W-RAIN-DIMS" && 40 < c.Radius && c.Radius < 100));
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect();
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(GetWRainLines(entities.OfType<Entity>()));

            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var wp in wrappingPipes)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });

        }
        //雨水井不是图块，不支持
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\蓝光钰泷府二期_框线(1)\蓝光钰泷府二期_框线\FS59P2BC_W20-地上给水排水平面图-副本.dwg";
        private static void qtc30n()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-NOTE" && x.ObjectId.IsValid && x.GetEffectiveName().Contains("合流")));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    //从可见性里拿
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                }

                {
                    var pps = new List<Entity>();
                    //pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));//后面这里最好带上图层来判断
                    pps.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-RAIN-PIPE-RISR" || x.Layer == "W-RAIN-EQPM" || (x.Layer == "W-DRAI-NOTE" && x.ObjectId.IsValid && x.GetEffectiveName().StartsWith("A$"))));
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect();
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(GetWRainLines(entities.OfType<Entity>()));

            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var wp in wrappingPipes)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });

        }
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\蓝光未来阅璟_框线(1)\蓝光未来阅璟_框线\FS5F8704_W20-地上给水排水平面图-送审版.dwg";
        private static void qtc0tu()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    //从可见性里拿
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                }

                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect();
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(GetWRainLines(entities.OfType<Entity>()));

            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var wp in wrappingPipes)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });

        }
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\佳兆业滨江新城_框线(1)\佳兆业滨江新城_框线\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
        private static void qtc0cz()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
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
                }
                var entities = GetEntities().ToList();

                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    //从可见性里拿
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }

                {
                    foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-DIMS" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                    CollectTianzhengVerticalPipes(labelLines, cts, entities);
                }

                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<BlockReference>()
                     //.Where(x => x.Layer == "W-RAIN-EQPM")
                     //.Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == "$LIGUAN")//图块炸开的时候就失效了
                     .Where(x => x.ObjectId.IsValid ? x.GetEffectiveName() == "$LIGUAN" : x.Layer == "W-RAIN-EQPM")
                     );
                    foreach (var pp in pps)
                    {
                        pipes.Add(GRect.Create(pp.Bounds.ToGRect().Center, 55));
                    }
                }

                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    var range = Dbg.SelectRange();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                wLines.AddRange(GetWRainLines(entities.OfType<Entity>()));

            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var seg in labelLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 1;
                }
                foreach (var seg in wLines)
                {
                    var e = DU.DrawLineSegmentLazy(seg);
                    e.ColorIndex = 4;
                }
                foreach (var item in cts)
                {
                    var e = DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var pl = DU.DrawRectLazy(item.Boundary);
                    pl.ColorIndex = 2;
                }
                foreach (var pp in pipes)
                {
                    var e = DU.DrawRectLazy(pp);
                    e.ColorIndex = 3;
                }
                foreach (var s in storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                foreach (var pp in condensePipes)
                {
                    var e = DU.DrawCircleLazy(pp);
                    e.ColorIndex = 2;
                }
                foreach (var fd in floorDrains)
                {
                    var e = DU.DrawRectLazy(fd);
                    e.ColorIndex = 6;
                }
                foreach (var ww in waterWells)
                {
                    var e = DU.DrawRectLazy(ww);
                    e.ColorIndex = 7;
                }
                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var ww in waterPortSymbols)
                    {
                        var e = DU.DrawRectLazy(ww);
                        //e.ColorIndex = 8;
                        e.Color = cl;
                    }
                    foreach (var wp in waterPort13s)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var wp in wrappingPipes)
                    {
                        var e = DU.DrawRectLazy(wp);
                        e.Color = cl;
                    }
                }
            });

        }




    }
}
