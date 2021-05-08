//this file is for debugging only by Feng

//#if DEBUG

namespace ThMEPWSS.DebugNs
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
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
    using static ThMEPWSS.DebugNs.ThPublicMethods;
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
    using static StaticMethods;
    using ThMEPWSS.Pipe;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using System.Collections;
    using ThCADCore.NTS.IO;

    public class FengDbgTest
    {
        public static Dictionary<string, object> processContext;
        public static Dictionary<string, object> ctx
        {
            get
            {
                if (processContext == null) return null;
                return (Dictionary<string, object>)processContext["context"];
            }
        }
        //using (var adb = AcadDatabase.Active())
        //using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
        //{
        //}
        public static void Test(Dictionary<string, object> ctx)
        {
            processContext = (Dictionary<string, object>)ctx["processContext"];
            ctx.TryGetValue("entryMethod", out object o);
            if (o is Action entryMethod)
            {
                Action initMethod = null;
                initMethod = new Action(() =>
                {
                    ((Action<Assembly, string>)ctx["pushAcadActions"])((Assembly)ctx["currentAsm"], typeof(ThDebugClass).FullName);
                    ((Action<object>)ctx["clearBtns"])(ctx["currentPanel"]);
                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], "initMethod", initMethod);
                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], "reloadMe", () =>
                    {
                        var asm = ((Func<string, Assembly>)ctx["loadAsm"])((string)ctx["asmDllFullPath"]);
                        asm.GetType(typeof(FengDbgTest).FullName).GetField(nameof(processContext)).SetValue(null, processContext);
                        initMethod();
                    });
                    var fs = (List<Action>)ctx["actions"];
                    var names = (List<string>)ctx["names"];
                    for (int i = 0; i < fs.Count; i++)
                    {
                        var f = fs[i];
                        var name = names[i];
                        ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], name, f);
                    }
                });
                ctx["initMethod"] = initMethod;
                entryMethod();
            }
            else
            {
                MessageBox.Show("entryMethod not set!");
            }
        }
    }
    public class ThDebugDrawer
    {
        static Database WorkingDatabase => HostApplicationServices.WorkingDatabase;
        static Transaction Transaction => WorkingDatabase.TransactionManager.StartTransaction();
        static ObjectId GetEntity()
        {
            var ed = Active.Editor;
            var opt = new PromptEntityOptions("请选择");
            var ret = ed.GetEntity(opt);
            if (ret.Status != PromptStatus.OK) return ObjectId.Null;
            return ret.ObjectId;
        }
        public static T GetEntity<T>(AcadDatabase adb) where T : DBObject
        {
            var id = GetEntity();
            var ent = adb.Element<T>(id);
            return ent;
        }
        public static ObjectId[] GetSelection()
        {
            var ed = Active.Editor;
            var opt = new PromptSelectionOptions();
            var ret = ed.GetSelection(opt);
            if (ret.Status != PromptStatus.OK) return null;
            return ret.Value.GetObjectIds();
        }
        private static BlockTableRecord GetBlockTableRecord(Database db, Transaction trans)
        {
            var bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            var btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            return btr;
        }
        public static Polyline DrawPolygon(Database db, Transaction trans, Point2d center, int num, double radius)
        {
            var btr = GetBlockTableRecord(db, trans);
            var pline = PolylineTools.CreatePolygon(center, num, radius);
            btr.AppendEntity(pline);
            trans.AddNewlyCreatedDBObject(pline, true);
            return pline;
        }
        public static bool GetCircleBoundary(Point3dCollection points, out Point3d center, out double radius)
        {
            if (points.Count == 0)
            {
                center = default;
                radius = default;
                return false;
            }
            var minX = points.Cast<Point3d>().Select(p => p.X).Min();
            var maxX = points.Cast<Point3d>().Select(p => p.X).Max();
            var minY = points.Cast<Point3d>().Select(p => p.Y).Min();
            var maxY = points.Cast<Point3d>().Select(p => p.Y).Max();
            var minZ = points.Cast<Point3d>().Select(p => p.Z).Min();
            var maxZ = points.Cast<Point3d>().Select(p => p.Z).Max();
            var pt1 = new Point3d(minX, minY, minZ);
            var pt2 = new Point3d(maxX, maxY, maxZ);
            center = GeTools.MidPoint(pt1, pt2);
            radius = (center - pt1).Length;
            return true;
        }
        public static void DrawLabelBox(Database db, params Point2d[] points)
        {
            using (var trans = Transaction)
            {
                var radius = 50;
                foreach (var pt in points)
                {
                    var pline = DrawPolygon(db, trans, pt, 6, radius * 1.5);
                    pline.ConstantWidth = 50;
                    trans.Commit();
                }
            }
        }
        public static void DrawLineByPolar(Database db, Point2d center, double radius, double angle)
        {
            var x = center.X + radius * Math.Cos(angle);
            var y = center.Y + radius * Math.Sin(angle);
            DrawLine(db, center, new Point2d(x, y));
        }
        public static void DrawLineByOffset(Database db, Point2d start, double offsetX, double offsetY)
        {
            DrawLine(db, start, new Point2d(start.X + offsetX, start.Y + offsetY));
        }
        public static void DrawText(Database db, Point2d pt, string text)
        {
            using (var trans = Transaction)
            {
                var btr = GetBlockTableRecord(db, trans);
                var t = new DBText() { Position = pt.ToPoint3d(), TextString = text, Height = 350, Thickness = 10, };
                btr.AppendEntity(t);
                trans.AddNewlyCreatedDBObject(t, true);
                trans.Commit();
            }

        }
        public static void DrawCircle(Database db, Point3d pt1, Point3d pt2, Point3d pt3)
        {
            using (var trans = Transaction)
            {
                var btr = GetBlockTableRecord(db, trans);
                var circle = CircleTools.CreateCircle(pt1, pt2, pt3);
                circle.Thickness = 5;
                btr.AppendEntity(circle);
                trans.AddNewlyCreatedDBObject(circle, true);
                circle.ColorIndex = 32;
                trans.Commit();
            }
        }
        public static void DrawCircle(Database db, Point2d center, double radius)
        {
            using (var trans = Transaction)
            {
                var btr = GetBlockTableRecord(db, trans);
                var circle = new Circle
                {
                    Center = center.ToPoint3d(),
                    Radius = radius,
                    Thickness = 5
                };
                btr.AppendEntity(circle);
                trans.AddNewlyCreatedDBObject(circle, true);
                circle.ColorIndex = 32;
                trans.Commit();
            }
        }
        public static void DrawLine(Database db, Point2d start, Point2d end)
        {
            using (var trans = Transaction)
            {
                var btr = GetBlockTableRecord(db, trans);
                var pts = new Point2dCollection { start, end, };
                var pline = PolylineTools.CreatePolyline(pts);
                btr.AppendEntity(pline);
                trans.AddNewlyCreatedDBObject(pline, true);
                pline.ConstantWidth = 2;
                pline.ColorIndex = 32;
                trans.Commit();
            }
        }


    }
    public class StaticMethods
    {
        public static void SaveData(string name, object obj)
        {
            File.WriteAllText($@"D:\temptxts\{name}.txt", obj.ToJson());
        }
        public static T LoadData<T>(string name)
        {
            return File.ReadAllText($@"D:\temptxts\{name}.txt").FromJson<T>();
        }
    }
    public class ThDebugTool
    {
        public static Editor Editor => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        public static Document MdiActiveDocument => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        static Dictionary<string, object> ctx => ThDebugClass.ctx;
        public static void ShowString(string str)
        {
            if (!isDebugging) return;
            ((Action<string>)ctx["showString"])(str);
        }
        public static void PrintLine(bool b)
        {
            PrintLine(b.ToString());
        }
        public static void PrintLine(double line)
        {
            PrintLine(line.ToString());
        }
        public static void PrintLine(string line)
        {
            if (!isDebugging) return;
            if (ctx["currentLogWindow"] == null) return;
            ((Action<string>)ctx["addline"])(line);
        }
        public static void PrintText(string text)
        {
            if (!isDebugging) return;
            if (ctx["currentLogWindow"] == null) return;
            ((Action<string>)ctx["addtext"])(text);
        }
        public static void PrintText(string[] lines)
        {
            if (!isDebugging) return;
            if (ctx["currentLogWindow"] == null) return;
            ((Action<string>)ctx["addtext"])(string.Join("\n", lines));
        }
        public static void NewLogWindow()
        {
            if (!isDebugging) return;
            ((Action)ctx["newLogWindow"])();
        }
        public static void ShowCurrentLogWindow()
        {
            if (!isDebugging) return;
            ((Action)ctx["showCurrentLogWindow"])();
        }
        public static bool isDebugging => FengDbgTest.ctx != null;
        public static void SetText(IEnumerable<string> lines)
        {
            if (!isDebugging) return;
            if (lines == null) SetText((string)null);
            SetText(string.Join("\n", lines));
        }
        public static void SetText(string text)
        {
            if (!isDebugging) return;
            ((Action<string>)ctx["setText"])(text);
        }
        const double DEFAULT_DELTA = 10000;
        public static void ShowAll(string text, double delta = DEFAULT_DELTA)
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                foreach (var t in adb.ModelSpace.OfType<DBText>().Where(t => t.TextString == text))
                {
                    var bd = GeoAlgorithm.GetBoundaryRect(t);
                    Dbg.ShowWhere(bd, delta);
                }
            }
        }
        public static void ShowWhere(string text, double delta = DEFAULT_DELTA)
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var t = adb.ModelSpace.OfType<DBText>().FirstOrDefault(x => x.TextString == text);
                if (t != null)
                {
                    var bd = GeoAlgorithm.GetBoundaryRect(t);
                    Dbg.ShowWhere(bd);
                }
            }
        }
        public static void ShowWhere(Entity e, double delta = DEFAULT_DELTA)
        {
            ShowWhere(GeoAlgorithm.GetBoundaryRect(e), delta);
        }
        public static void ShowWhere(Point3d pt, double delta = DEFAULT_DELTA)
        {
            ShowWhere(new ThWGRect(pt.ToPoint2d(), pt.ToPoint2d()), delta);
        }
        public static void ShowXLabel(Point3d pt, double size = 500)
        {
            DrawUtils.DrawingQueue.Enqueue(adb =>
            {
                var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                var r = new ThWGRect(pt.X - size / 2, pt.Y - size / 2, pt.X + size / 2, pt.Y + size / 2);
                var lines = new Line[] {
new Line() { StartPoint = r.LeftTop.ToPoint3d(), EndPoint = r.RightButtom.ToPoint3d()},
new Line() { StartPoint = r.LeftButtom.ToPoint3d(), EndPoint = r.RightTop.ToPoint3d() }
};
                foreach (var line in lines)
                {
                    line.Thickness = 10;
                    line.ColorIndex = 3;
                    adb.ModelSpace.Add(line);
                }
            });
        }
        public static void ShowWhere(ThWGRect r, double delta = DEFAULT_DELTA)
        {
            DrawUtils.DrawingQueue.Enqueue(adb =>
            {
                var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                var rect = "[-334718.142328821,1366616.99129695,635160.253054206,1868196.71202574]".JsonToThWGRect();
                ThWGRect r3 = default;
                {
                    var circle = DrawUtils.DrawCircleLazy(r.Expand(800));
                    circle.Thickness = 500;
                    circle.ColorIndex = 3;
                }
                for (int i = 0; i < 4; i++)
                {
                    var _delta = delta * i;
                    //if (_delta > rect.Width / 2 && _delta > rect.Height / 2) break;
                    var _r = new ThWGRect(r.MinX - _delta, r.MaxY + _delta, r.MaxX + _delta, r.MinY - _delta);
                    r3 = _r;
                    //DrawUtils.DrawRectLazy(_r);
                    var circle = DrawUtils.DrawCircleLazy(_r);
                    if (i == 0)
                    {
                        circle.Thickness = 10;
                        circle.ColorIndex = 3;
                    }
                    else if (i == 2)
                    {
                        circle.Thickness = 1000;
                        circle.ColorIndex = 4;
                    }
                }
                if (!Equals(r3, default(ThWGRect)))
                {
                    var l1 = DU.DrawLineLazy(new Point3d(r3.MinX, r3.MinY, 0), new Point3d(r3.MaxX, r3.MaxY, 0));
                    var l2 = DU.DrawLineLazy(new Point3d(r3.MinX, r3.MaxY, 0), new Point3d(r3.MaxX, r3.MinY, 0));
                    l1.Thickness = 10;
                    l2.Thickness = 10;
                    l1.ColorIndex = 4;
                    l2.ColorIndex = 4;
                }
            });
        }
        public static void FocusMainWindow()
        {
            //Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
            var w = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow;
            var mi = w.GetType().GetMethod("Focus");
            mi?.Invoke(w, null);
        }
        public static void Print(string str, params object[] objs)
        {
            var dt = DateTime.Now.ToString("HH:mm:ss.fff");
            if (objs.Length == 0) Editor.WriteMessage($"\n[{dt}] " + str + "\n");
            else Editor.WriteMessage($"\n[{dt}] " + str + "\n", objs);
        }

        const string TEST_GEO_PREFIX = "feng_test_";
        public static string BuildAndSetCurrentLayer(Database db)
        {
            //直接打开或新建，然后解冻、解锁
            var guid = CtGuid().ToString();
            var targetLayerName = TEST_GEO_PREFIX + guid;
            //var targetLayer = db.GetAllLayers().FirstOrDefault(x => x.Name == targetLayerName);
            var targetLayer = db.AddLayer(targetLayerName);
            if (targetLayer.IsNull) throw new System.Exception();
            //db.UnFrozenLayer(targetLayerName);
            //db.UnLockLayer(targetLayerName);
            //db.UnPrintLayer(targetLayerName);
            //db.UnOffLayer(targetLayerName);
            db.SetCurrentLayer(targetLayerName);
            short targetColorIndex = 1;
            db.SetLayerColor(targetLayerName, targetColorIndex);
            return targetLayerName;
        }
        public static void DeleteTestGeometries()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                adb.Database.SetCurrentLayer("0");

                var lst = adb.ModelSpace.OfType<Entity>().Where(x => x.Layer.Contains(TEST_GEO_PREFIX)).Distinct().ToList();
                foreach (var e in lst)
                {
                    adb.Element<Entity>(e.ObjectId, true).Erase();
                }
            }
            FocusMainWindow();
        }
        public static void BatchDeleteEntities(AcadDatabase adb, Entity[] ents)
        {
            adb.Database.SetCurrentLayer("0");

            foreach (var e in ents.Distinct())
            {
                adb.Element<Entity>(e.ObjectId, true).Erase();
            }
            FocusMainWindow();
        }

        public static T SelectEntity<T>(AcadDatabase adb) where T : DBObject
        {
            return ThDebugDrawer.GetEntity<T>(adb);
        }
        public static Point3d SelectPoint()
        {
            var basePtOptions = new PromptPointOptions("\n选择图纸基点");
            var rst = Active.Editor.GetPoint(basePtOptions);
            if (rst.Status != PromptStatus.OK) return default;
            var basePt = rst.Value;
            return basePt;
        }
        public static Point3dCollection SelectRange()
        {
            return SelectGRect().ToPoint3dCollection();
        }
        public static ThWGRect SelectGRect()
        {
            var t = SelectRect();
            return new ThWGRect(t.Item1.ToPoint2d(), t.Item2.ToPoint2d());
        }
        public static Tuple<Point3d, Point3d> SelectRect()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }
    }
    public class ThPublicMethods
    {
        public static string CtGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
    public class ThCADInput
    {
        public static bool QueryYesOrNo(string text, bool dft = false)
        {
            var separation_key = new PromptKeywordOptions(text);
            separation_key.Keywords.Add("是", "Y", "是(Y)");
            separation_key.Keywords.Add("否", "N", "否(N)");
            separation_key.Keywords.Default = dft ? "是" : "否";
            var result = Active.Editor.GetKeywords(separation_key);
            if (result.Status != PromptStatus.OK) return false;
            return result.StringResult == "是";
        }
        public static bool? QueryYesOrNoOrCanceled(string text, bool dft = false)
        {
            var separation_key = new PromptKeywordOptions(text);
            separation_key.Keywords.Add("是", "Y", "是(Y)");
            separation_key.Keywords.Add("否", "N", "否(N)");
            separation_key.Keywords.Default = dft ? "是" : "否";
            var result = Active.Editor.GetKeywords(separation_key);
            if (result.Status != PromptStatus.OK) return null;
            return result.StringResult == "是";
        }
    }

    public class ThDebugClass
    {
        public static Dictionary<string, object> ctx => ThDebugClass1.ctx;
        public static Dictionary<string, object> processContext => ThDebugClass1.processContext;

        static void nav()
        {
            var _ = new string[]
            {
                nameof(ThWRainPipeRun.Draw),
            };
        }
        public static void NewAndShowLogWindow()
        {
            Dbg.NewLogWindow();
            Dbg.ShowCurrentLogWindow();
        }
        public static void ExplodeTest4()
        {
            static IEnumerable<MText> Explode(Entity entity)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                foreach (var o in colle)
                {
                    if (o is MText t)
                    {
                        yield return t;
                    }
                    else if (o is Entity e && e.ObjectId.IsValid)
                    {
                        foreach (var r in Explode(e))
                        {
                            yield return r;
                        }
                    }
                }
            }

            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            using (var _adb = AcadDatabase.Active())
            {
                Dbg.BuildAndSetCurrentLayer(_adb.Database);
                void DoExtract(BlockReference blockReference, Matrix3d matrix)
                {
                    using var adb = AcadDatabase.Use(blockReference.Database);
                    if (blockReference.BlockTableRecord.IsValid)
                    {
                        var blockTableRecord = adb.Blocks.Element(blockReference.BlockTableRecord);
                        if (ThBlock.IsSupportedBlock(blockTableRecord))
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = adb.Element<Entity>(objId);
                                if (dbObj is BlockReference blk)
                                {
                                    if (blk.BlockTableRecord.IsNull)
                                    {
                                        continue;
                                    }
                                    if (blk.BlockTableRecord.IsValid)
                                    {
                                        //if (false)
                                        //{
                                        //  var blk2 = blk.GetTransformedCopy(matrix);
                                        //  continue;
                                        //}
                                        {
                                            if (Explode(blk).Any(t => t.Contents.ToLower().Contains("ah")))
                                            {
                                                var blk2 = blk.GetTransformedCopy(matrix);
                                                adb.ModelSpace.Add(blk2);
                                                Dbg.ShowWhere(blk2);
                                                continue;
                                            }
                                        }
                                        var mcs2wcs = blk.BlockTransform.PreMultiplyBy(matrix);
                                        DoExtract(blk, mcs2wcs);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var blockReference in _adb.ModelSpace.OfType<BlockReference>())
                {
                    DoExtract(blockReference, Matrix3d.Identity);
                }
            }
        }
        public static void ExplodeTest3()
        {
            static IEnumerable<DBText> Explode(Entity entity)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                foreach (var o in colle)
                {
                    if (o is DBText t)
                    {
                        yield return t;
                    }
                    else if (o is Entity e && e.ObjectId.IsValid)
                    {
                        foreach (var r in Explode(e))
                        {
                            yield return r;
                        }
                    }
                }
            }

            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            using (var _adb = AcadDatabase.Active())
            {
                void DoExtract(BlockReference blockReference, Matrix3d matrix)
                {
                    using var adb = AcadDatabase.Use(blockReference.Database);
                    if (blockReference.BlockTableRecord.IsValid)
                    {
                        var blockTableRecord = adb.Blocks.Element(blockReference.BlockTableRecord);
                        if (ThBlock.IsSupportedBlock(blockTableRecord))
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = adb.Element<Entity>(objId);
                                if (dbObj is BlockReference blk)
                                {
                                    if (blk.BlockTableRecord.IsNull)
                                    {
                                        continue;
                                    }
                                    if (blk.BlockTableRecord.IsValid)
                                    {
                                        //if (false)
                                        //{
                                        //  var blk2 = blk.GetTransformedCopy(matrix);
                                        //  continue;
                                        //}
                                        {
                                            if (Explode(blk).Any(t => t.TextString.StartsWith("Ah1")))
                                            {
                                                var blk2 = blk.GetTransformedCopy(matrix);
                                                adb.ModelSpace.Add(blk2);
                                                Dbg.ShowWhere(blk2);
                                                continue;
                                            }
                                        }
                                        var mcs2wcs = blk.BlockTransform.PreMultiplyBy(matrix);
                                        DoExtract(blk, mcs2wcs);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var blockReference in _adb.ModelSpace.OfType<BlockReference>())
                {
                    DoExtract(blockReference, Matrix3d.Identity);
                }
            }
        }

        public static void ThAHMarkRecognitionEngineTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            using (var adb = AcadDatabase.Active())
            {
                using (var engine = new Engine.ThAHMarkRecognitionEngine())
                {
                    engine.Recognize(adb.Database, Dbg.SelectRange());
                    var texts = engine.Texts;
                    var res1 = engine.Texts.Where(o => o is DBText).Select(o => (o as DBText).TextString).ToList();
                    var res2 = engine.Texts.Where(o => o is MText).Select(o => (o as MText).Text).ToList();
                    //foreach (var e in res1)
                    //{
                    //    Dbg.ShowWhere(e);
                    //}
                    //Dbg.Print(res1.Count.ToString());
                    //Dbg.PrintLine(res2.ToJson());
                    //foreach (var e in engine.Texts.OfType<MText>())
                    //{
                    //    Dbg.ShowWhere(e);
                    //}
                    Dbg.PrintLine(res2.Distinct().ToJson());
                    //foreach (var e in engine.Texts.OfType<MText>())
                    //{
                    //    DU.DrawBoundaryLazy(e);
                    //}
                }
            }

        }
        public static void ExplodeTest2()
        {
            static DBObjectCollection Explode(Entity entity)
            {
                var entitySet = new DBObjectCollection();
                void explode(Entity ent)
                {
                    var obl = new DBObjectCollection();
                    ent.Explode(obl);
                    foreach (Entity e in obl)
                    {
                        if (e is BlockReference br)
                        {
                            explode(br);
                        }
                        else
                        {
                            entitySet.Add(e);
                        }
                    }
                }
                explode(entity);
                return entitySet;
            }

            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            using (var _adb = AcadDatabase.Active())
            {
                void DoExtract(BlockReference blockReference, Matrix3d matrix)
                {
                    using var adb = AcadDatabase.Use(blockReference.Database);
                    if (blockReference.BlockTableRecord.IsValid)
                    {
                        var blockTableRecord = adb.Blocks.Element(blockReference.BlockTableRecord);
                        if (ThBlock.IsSupportedBlock(blockTableRecord))
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = adb.Element<Entity>(objId);
                                if (dbObj is BlockReference blk)
                                {
                                    if (blk.BlockTableRecord.IsNull)
                                    {
                                        continue;
                                    }
                                    if (blk.BlockTableRecord.IsValid)
                                    {
                                        //if (false)
                                        //{
                                        //  var blk2 = blk.GetTransformedCopy(matrix);
                                        //  continue;
                                        //}
                                        {
                                            if (Explode(blk).OfType<DBText>().Any(t => t.TextString.StartsWith("Ah1")))
                                            {
                                                var blk2 = blk.GetTransformedCopy(matrix);
                                                adb.ModelSpace.Add(blk2);
                                                Dbg.ShowWhere(blk2);
                                                continue;
                                            }
                                        }
                                        var mcs2wcs = blk.BlockTransform.PreMultiplyBy(matrix);
                                        DoExtract(blk, mcs2wcs);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var blockReference in _adb.ModelSpace.OfType<BlockReference>())
                {
                    DoExtract(blockReference, Matrix3d.Identity);
                }
            }
        }
        public static void ExplodeTest21()
        {
            var l = new List<string>();
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (DrawUtils.DrawingTransaction)
            using (var adb = AcadDatabase.Active())
            {
                //Dbg.PrintLine(Dbg.MdiActiveDocument.Name);//文件的绝对路径
                //Dbg.PrintLine(adb.Database.Filename);//这是个临时文件
                //Dbg.SetText(adb.ModelSpace.OfType<DBText>().Select(x => x.TextString));

                var root = Dbg.MdiActiveDocument.Name;
                foreach (var xref in adb.XRefs)
                {
                    //using (AcadDatabase xrefAdb = AcadDatabase.Open(xref.FilePath, DwgOpenMode.ReadOnly, false))
                    //{

                    //}
                    //Dbg.PrintLine(xref.FilePath);//有相对路径有绝对路径，注意判断是否存在文件

                    //找到对应文件了
                    //Dbg.PrintLine(Path.GetFullPath(Path.Combine(root, "..", xref.FilePath)));
                    //Dbg.PrintLine(File.Exists(Path.GetFullPath(Path.Combine(root,"..", xref.FilePath))));

                    var file = Path.GetFullPath(Path.Combine(root, "..", xref.FilePath));
                    if (File.Exists(file))
                    {
                        using (var xrefAdb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly, false))
                        {
                            {
                                //var q = xrefAdb.ModelSpace.OfType<DBText>().Select(x => x.TextString);
                                //l.AddRange(q);
                            }
                            {
                                //var q=xrefAdb.ModelSpace.OfType<BlockReference>().SelectMany(x => x.ExplodeToDBObjectCollection().OfType<DBText>().Select(y => y.TextString));
                                var q = xrefAdb.ModelSpace.OfType<BlockReference>().SelectMany(x => x.ExplodeBlockRef().OfType<DBText>().Select(y => y.TextString));
                                l.AddRange(q);
                            }
                        }
                    }

                }
            }
            if (l.Any(x => x?.Trim().ToLower() == "ah1"))
            {
                Dbg.PrintLine("found!");
            }
            File.WriteAllText("Y:\\test.txt", l.JoinWith("\n"));
        }
        public static void BlockReferenceTest_qso8b4()
        {
            var l = new List<string>();
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (DrawUtils.DrawingTransaction)
            using (var _adb = AcadDatabase.Active())
            {
                void DoExtract(BlockReference blockReference, Matrix3d matrix)
                {
                    using var adb = AcadDatabase.Use(blockReference.Database);
                    if (blockReference.BlockTableRecord.IsValid)
                    {
                        var blockTableRecord = adb.Blocks.Element(blockReference.BlockTableRecord);
                        if (ThBlock.IsSupportedBlock(blockTableRecord))
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = adb.Element<Entity>(objId);
                                if (dbObj is BlockReference blk)
                                {
                                    if (blk.BlockTableRecord.IsNull)
                                    {
                                        continue;
                                    }
                                    if (blk.BlockTableRecord.IsValid)
                                    {
                                        l.Add(blk.GetEffectiveName());
                                        var mcs2wcs = blk.BlockTransform.PreMultiplyBy(matrix);
                                        DoExtract(blk, mcs2wcs);
                                    }
                                }
                                else if (dbObj is DBText t)
                                {
                                    //l.Add(t.TextString);
                                }

                            }
                        }
                    }
                }

                foreach (var blockReference in _adb.ModelSpace.OfType<BlockReference>())
                {
                    DoExtract(blockReference, Matrix3d.Identity);
                }

                File.WriteAllText("Y:\\test.txt", l.JoinWith("\n"));
            }
        }
        public static void BlockReferenceTest_qso7q7()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            using (var _adb = AcadDatabase.Active())
            {
                void DoExtract(BlockReference blockReference, Matrix3d matrix)
                {
                    using var adb = AcadDatabase.Use(blockReference.Database);
                    if (blockReference.BlockTableRecord.IsValid)
                    {
                        var blockTableRecord = adb.Blocks.Element(blockReference.BlockTableRecord);
                        if (ThBlock.IsSupportedBlock(blockTableRecord))
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = adb.Element<Entity>(objId);
                                if (dbObj is BlockReference blk)
                                {
                                    if (blk.BlockTableRecord.IsNull)
                                    {
                                        continue;
                                    }
                                    if (blk.BlockTableRecord.IsValid)
                                    {
                                        if (false)
                                        {
                                            var blk2 = blk.GetTransformedCopy(matrix);
                                            continue;
                                        }
                                        var mcs2wcs = blk.BlockTransform.PreMultiplyBy(matrix);
                                        DoExtract(blk, mcs2wcs);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var blockReference in _adb.ModelSpace.OfType<BlockReference>())
                {
                    DoExtract(blockReference, Matrix3d.Identity);
                }
            }
        }
        public static void ThWStoreysRecognitionEngineTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var storeysRecEngine = new ThWStoreysRecognitionEngine();
                var range = Dbg.SelectRange();
                storeysRecEngine.Recognize(db, range);
                var els = storeysRecEngine.Elements;
            }
        }

        static void print(string str)
        {
            Dbg.PrintLine(str);
        }
        public static void ThWGravityWaterBucketRecognitionEngineTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            //using (var tr = DrawUtils.DrawingTransaction)
            {
                var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                gravityBucketEngine.Recognize(adb.Database, Dbg.SelectRange());
                foreach (ThWGravityWaterBucket el in gravityBucketEngine.Elements)
                {
                    var r = GeoAlgorithm.GetBoundaryRect(el.Outline.GeometricExtents.ToRectangle());
                    Dbg.PrintLine(r.ToJson());
                }
            }
        }
        public static void ThWStoreysRecognitionEngineTest2()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var storeysRecEngine = new ThWStoreysRecognitionEngine();
                var range = Dbg.SelectRange();
                storeysRecEngine.Recognize(db, range);
                foreach (ThWStoreys el in storeysRecEngine.Elements)
                {
                    var r = GeoAlgorithm.GetBoundaryRect(adb.Element<Entity>(el.ObjectId).GeometricExtents.ToRectangle());
                    Dbg.PrintLine(r.ToJson());
                }
            }
        }
        static KeyValuePair<K, V> CreateKeyValuePair<K, V>(K k, V v) => new KeyValuePair<K, V>(k, v);
        public static void LoadEntityTest_LineGrouping()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                const string KEY = FengKeys.ThRainSystemServiceContextData;
                var c = LoadData<ThRainSystemService.Context>(KEY);
                var pipes = c.VerticalPipes;
                var d = c.BoundaryDict;
                var lines = c.WRainLines;
                var ld = c.WRainLinesDict;
                var ld2 = ld.Select(kv => new KeyValuePair<string, Entity>(kv.Key, kv.Value.ToLine())).ToDictionary(kv => kv.Value, kv => kv.Key);
                var lst = ThRainSystemService.GroupLines(ld2.Keys.Cast<Entity>().ToList());
                var gs = lst.Select(l => l.Select(e => ld2[e]).ToList()).ToList();
                SaveData(FengKeys.LinesGroupData, gs);
            }
        }
        public static void SaveEntityTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                const string KEY = FengKeys.ThRainSystemServiceContextData;
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                SaveData(KEY, sv.GetCurrentContext());
            }
        }
        public static void qsqesb()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                DU.DrawEntityLazy(Dbg.SelectPoint().Expand(100).ToThWGRect().CreateRect());
            }
        }
        public static void ToGeoJSONTest()
        {
            //{
            //    var j = ThCADCore.NTS.IO.ThCADCoreNTSGeometryWriter.ToGeoJSON(new Line().ToNTSGeometry());
            //    Dbg.PrintText(j);
            //}

            //Dbg.FocusMainWindow();
            //using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            //using (var adb = AcadDatabase.Active())
            //using (var tr = DrawUtils.DrawingTransaction)
            //{
            //    var db = adb.Database;

            //    var j = Dbg.SelectGRect().CreatePolygon(6).ToNTSGeometry().ToGeoJSON();
            //    Dbg.PrintText(j);

            //    var c = new Circle() { Center=Dbg.SelectPoint(),Radius=666};
            //    var j = c.ToNTSGeometry().ToGeoJSON();
            //    Dbg.PrintText(j);
            //}
        }
        public static void xxx()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 10;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.CollectAhTexts(_GetTestPoints());
                foreach (var ah in sv.AhTexts)
                {
                    DU.DrawBoundaryLazy(ah);
                }
                var si = ThRainSystemService.BuildSpatialIndex<Entity>(ThRainSystemService.CollectEnts().Add(sv.CondensePipes).Add(sv.AhTexts).Entities);
                //foreach (Entity e in si.NearestNeighbours(Dbg.SelectGRect().CreateRect(), 3))
                foreach (Entity e in si.NearestNeighbours(Dbg.SelectPoint().Expand(1).ToThWGRect().CreateRect(), 3))
                {
                    Dbg.ShowWhere(e);
                }
            }
        }

        private static Point3dCollection _GetTestPoints()
        {
            var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
            var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();

            var points = new Point3dCollection();
            points.Add(pt1);
            points.Add(new Point3d(pt1.X, pt2.Y, 0));
            points.Add(pt2);
            points.Add(new Point3d(pt2.X, pt1.Y, 0));
            return points;
        }

        public static void qsq8o3()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                if (false)
                {
                    var basePt = Dbg.SelectPoint();
                    Dr.DrawRainPortLabel(basePt);
                }
                if (false)
                {
                    foreach (var e in adb.ModelSpace.OfType<Circle>().Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM).Where(x => x.Radius <= 200 && x.Radius >= 100))
                    {
                        Dbg.ShowWhere(e);
                    }
                }
                //if (false)
                {
                    //var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                    //gravityBucketEngine.Recognize(adb.Database, Dbg.SelectRange());
                    //gravityBucketEngine.Elements
                    var r = Dbg.SelectGRect();
                    var pl = r.CreateRect();
                    DU.DrawEntityLazy(pl);
                }

            }
        }
        public static void DeleteTestGeometries()
        {
            Dbg.DeleteTestGeometries();
        }

        public static void Invoke()
        {
            var rst = AcHelper.Active.Editor.GetString("\n输入method");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return;
            }
            var name = rst.StringResult;
            typeof(ThDebugClass).GetMethod(name).Invoke(null, null);
        }
        public static void TestThWRainSystemDiagram_Save()
        {
            ThWRainSystemDiagramTest.Test2();
        }
        public static void TestThWRainSystemDiagram_Load()
        {
            ThWRainSystemDiagramTest.Test1();
        }
        public static void RunThRainSystemDiagramCmd_NoRectSelection2()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Point3d pt1, pt2;
                SelectAllStoreys(out pt1, out pt2);
                NewMethod(adb, pt1, pt2);
            }
        }

        public static void FiltOutWaterWellsTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.CollectData();
                var rg = Dbg.SelectGRect().ToPoint3dCollection();
                var wells = sv.GetWaterWells(rg);
                foreach (var w in wells)
                {
                    Dbg.ShowWhere(w);
                }
            }
        }
        public static void GetTianZhengDNValueTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var rst = AcHelper.Active.Editor.GetEntity("\nSelect a TianZheng entity");
                if (rst.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    var entity = adb.Element<Entity>(rst.ObjectId);
                    //todo: extract property
                    var properties = System.ComponentModel.TypeDescriptor.GetProperties(entity.AcadObject).Cast<System.ComponentModel.PropertyDescriptor>().ToDictionary(prop => prop.Name);
                    var DNPropName = "DNDiameter";
                    if (properties.ContainsKey(DNPropName))
                    {
                        var DNPropObject = properties[DNPropName];
                        var DNValue = DNPropObject.GetValue(entity.AcadObject);
                        var DNString = DNValue.ToString();
                        Dbg.ShowString(new { DNValue, DNString }.ToJson());
                    }
                }
            }
        }
        public static void GetConstantWidth()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var pl = Dbg.SelectEntity<Polyline>(adb);
                Dbg.ShowString(pl.ConstantWidth.ToString());
            }
        }
        public static void DrawPolylineWithConstantWidthTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var pl = PolylineTools.CreatePolyline(new Point3d[] { Dbg.SelectPoint(), Dbg.SelectPoint() });
                pl.ConstantWidth = 1000;
                DU.DrawEntityLazy(pl);
            }
        }
        public static void SetTextStyleDemo()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Dr.DrawLabelRight(Dbg.SelectPoint(), "xx");

                //var t = DU.DrawTextLazy("测试", 100, Dbg.SelectPoint());
                var dbText = new DBText
                {
                    TextString = "测试",
                    Position = Dbg.SelectPoint(),
                    Height = 100,
                };
                var t = dbText;
                t.Layer = "W-RAIN-NOTE";
                t.ColorIndex = 256;
                adb.ModelSpace.Add(t);
                //t.TextStyleName = "TH-STYLE3";
                //var tb = AcHelper.Collections.Tables.GetTextStyle("TH-STYLE3");
                //Debugger.Break();
                t.ObjectId.SetTextStyle("TH-STYLE3");
            }
        }
        public static void ExecuteFromCommandLineTest1()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                AcHelper.Commands.CommandHandlerBase.ExecuteFromCommandLine("line", "@-50,0", "@0,-50", "@50,0", "c");
            }
        }
        public static void SelectRange()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                var r = Dbg.SelectGRect();
                Dbg.ShowString($"\"{(int)r.MinX},{(int)r.MaxY}\",\"{(int)r.MaxX},{(int)r.MinY}\"");
            }
        }
        public static void GoToStdDemoArea()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                AcHelper.Commands.CommandHandlerBase.ExecuteFromCommandLine("ZOOM", "W", $"25326.7121258527,103689.863619596", $"158235.423945315,93.7820820846366");
            }
            Dbg.FocusMainWindow();
        }
        public static void GoToTestArea()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                AcHelper.Commands.CommandHandlerBase.ExecuteFromCommandLine("ZOOM", "W", $"-133785.366438359,-179320.643293411", $"152482.054875992,-291960.803734254");
            }
            Dbg.FocusMainWindow();
        }
        public static void GoToGeoArea()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                AcHelper.Commands.CommandHandlerBase.ExecuteFromCommandLine("ZOOM", "W", "-86276,1889359", "295587,333368");
            }
            Dbg.FocusMainWindow();
        }
        public static void JsonTest()
        {
            const string KEY = "qrjq0w";
            var diagram = LoadData<ThWRainSystemDiagram>(KEY);
            foreach (var sys in diagram.RoofVerticalRainPipes)
            {
                Dbg.PrintLine(sys.WaterBucket.ToJson());
            }
        }

        public static void FindLabel()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
                if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    return;
                }
                foreach (var e in adb.ModelSpace.OfType<DBText>().Where(e => e.TextString.ToUpper() == rst.StringResult.ToUpper()).ToList())
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void GetDNTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var e = Dbg.SelectEntity<Entity>(adb);
                //Dbg.ShowString(e.ToDataItem().ToString());
                //Dbg.ShowString(e.ObjectId.GetDynProperties()?.Count.ToString()??"");
                Dbg.ShowString(e.ObjectId.GetAttributesInBlockReference().ToString());
            }
        }
        public static void FindPipe()
        {
            Dbg.FocusMainWindow();

            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return;
            }

            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.CollectVerticalPipesData();
                foreach (var e in sv.VerticalPipes)
                {
                    if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
                    {
                        //if (lb == "Y1L1-1a")
                        if (lb.ToUpper() == rst.StringResult.ToUpper())
                        {
                            Dbg.ShowWhere(e);
                        }
                    }
                }
            }
        }

        public static void qs04s2()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(sv.CondensePipes.ToCollection());
                //var range = Dbg.SelectGRect().ToPoint3dCollection();
                //foreach (var e in sv.FiltByRect())
                //{
                //    Dbg.ShowWhere(e);
                //}
            }
        }
        private static Tuple<Point3d, Point3d> SelectPoints()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }
        public static void ThCADCoreNTSServiceTest_ForArc()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 30;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var c = Dbg.SelectEntity<Arc>(adb);
                var pl = c.ToNTSLineString().ToDbPolyline();
                //var e = p.ToDbEntity();
                //e.ColorIndex = 3;
                //DU.DrawLazy(e);
                //Dbg.PrintLine(e.GetType().ToString());
                ////DU.DrawBoundaryLazy(e);
                ////Dbg.ShowWhere(e);
                //var pl = e as Polyline;
                Dbg.PrintLine(pl.NumberOfVertices);
                DU.DrawBoundaryLazy(pl);
            }
        }
        public static void ThCADCoreNTSServiceTest_ForCircle()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 30;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var c = Dbg.SelectEntity<Circle>(adb);
                var p = c.ToNTSPolygon();
                var e = p.ToDbEntity();
                e.ColorIndex = 3;
                DU.DrawEntityLazy(e);
                Dbg.PrintLine(e.GetType().ToString());
                //DU.DrawBoundaryLazy(e);
                //Dbg.ShowWhere(e);
                var pl = e as Polyline;
                Dbg.PrintLine(pl.NumberOfVertices);
            }
        }
        public static void BufferPLTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var pl = Dbg.SelectEntity<Polyline>(adb);
                var objs = pl.BufferPL(100);
                foreach (var o in objs)
                {
                    if (o is Entity e) DU.DrawEntityLazy(e);
                }
            }
        }
        public static void LineMergeTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var line1 = Dbg.SelectEntity<Line>(adb);
                var line2 = Dbg.SelectEntity<Line>(adb);
                var objs = new DBObjectCollection() { line1, line2 };
                var objs2 = objs.LineMerge();
                Dbg.PrintLine(objs2.Count);
                DU.DrawEntityLazy(objs2[0] as Entity);
            }
        }
        public static void BufferTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                //var line = new Line() { StartPoint = Dbg.SelectPoint(), EndPoint = Dbg.SelectPoint() };
                var line = Dbg.SelectEntity<Line>(adb);
                var pl = line.Buffer(100);
                DU.DrawEntityLazy(pl);
            }
        }
        public static void ExecuteCommandDemo()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                AcHelper.Commands.CommandHandlerBase.ExecuteFromCommandLine("zoom", "a");
            }
        }
        public class CRect
        {
            public double MinX;
            public double MinY;
            public double MaxX;
            public double MaxY;
            public static CRect Create(ThWGRect r)
            {
                return new CRect() { MinX = r.MinX, MinY = r.MinY, MaxX = r.MaxX, MaxY = r.MaxY };
            }
            public ThWGRect ToGRect()
            {
                return new ThWGRect(MinX, MinY, MaxX, MaxY);
            }
        }
        public class CLine
        {
            public double X1;
            public double Y1;
            public double X2;
            public double Y2;
            public static CLine Create(Line line)
            {
                return new CLine() { X1 = line.StartPoint.X, Y1 = line.StartPoint.Y, X2 = line.EndPoint.X, Y2 = line.EndPoint.Y };
            }
            public Line ToLine()
            {
                return new Line() { StartPoint = new Point3d(X1, Y1, 0), EndPoint = new Point3d(X2, Y2, 0) };
            }
        }
        private static Point3dCollection AutoSelectPoints()
        {
            var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
            var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();

            var points = new Point3dCollection();
            points.Add(pt1);
            points.Add(new Point3d(pt1.X, pt2.Y, 0));
            points.Add(pt2);
            points.Add(new Point3d(pt2.X, pt1.Y, 0));
            return points;
        }
        static ObjectId SelectEntity()
        {
            var ed = Active.Editor;
            var opt = new PromptEntityOptions("请选择");
            var ret = ed.GetEntity(opt);
            if (ret.Status != PromptStatus.OK) return ObjectId.Null;
            return ret.ObjectId;
        }
        static IEnumerable<ObjectId> SelectEntities()
        {
            var ed = Active.Editor;
            var opt = new PromptEntityOptions("请选择");
            while (true)
            {
                var ret = ed.GetEntity(opt);
                if (ret.Status != PromptStatus.OK) yield break;
                yield return ret.ObjectId;
            }
        }
        public static void SelectEntitiesTest()
        {
            foreach (var id in SelectEntities())
            {

            }
        }
        public static void YesDrawTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();

                var yd = new YesDraw();
                yd.Rotate(170, 180 + 45);
                yd.OffsetX(-1260);
                yd.Rotate(170, 180 + 45);
                var pts = YesDraw.FixLines(yd.GetPoint3ds(bsPt).ToList());
                //var pts = yd.GetPoint3ds(bsPt).ToList();
                //Dbg.PrintLine(pts.Count);
                var pl = EntityFactory.CreatePolyline(pts);
                DU.DrawEntityLazy(pl);

            }
        }
        public static void GetRelatedGravityWaterBucketTest3()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.CollectData(AutoSelectPoints());
                foreach (var g in sv.LongConverterLineToWaterBucketsGroups)
                {
                    DU.DrawBoundaryLazy(g.ToArray());
                }
            }
        }
        public static void GetRelatedGravityWaterBucketDemo()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                //sv.InitCache();
                //sv.CollectData();
                sv.thGravityService = new ThGravityService() { adb = adb };
                var rg = Dbg.SelectGRect().ToPoint3dCollection();
                sv.thGravityService.Init(rg);
                var list = sv.thGravityService.GetRelatedGravityWaterBucket(rg);
                var pls = new List<Polyline>();
                foreach (var ext in list)
                {
                    var r = ThWGRect.Create(ext);
                    var pl = EntityFactory.CreatePolyline(r.ToPoint3dCollection());
                    pls.Add(pl);
                }
                var si = new NTSSpatialIndex1(pls.ToCollection());
                var lst = si.SelectCrossingPolygon(Dbg.SelectGRect().ToPoint3dCollection()).Cast<Entity>().ToList();
                foreach (var e in lst)
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void GetRelatedGravityWaterBucketTest2()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                //sv.InitCache();
                //sv.CollectData();
                sv.thGravityService = new ThGravityService() { adb = adb };
                var rg = Dbg.SelectGRect().ToPoint3dCollection();
                sv.thGravityService.Init(rg);
                var list = sv.thGravityService.GetRelatedGravityWaterBucket(rg);
                foreach (var ept in list)
                {
                    var r = ThWGRect.Create(ept);
                    Dbg.ShowWhere(r);
                }
            }

        }
        static void ExceptionTest(Action f)
        {
            try
            {
                f?.Invoke();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.InnerException.Message);
                MessageBox.Show(ex.InnerException.StackTrace);
            }
        }
        public static void AlignedDimensionTest3()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var pt1 = Dbg.SelectPoint();
                var pt2 = pt1.OffsetY(1000);
                var dim = new AlignedDimension();
                dim.XLine1Point = pt1;
                dim.XLine2Point = pt2;
                //dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).PolarPoint(Math.PI / 2, 1000);
                dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(1000);
                dim.DimensionText = "AAA";
                dim.ColorIndex = 4;
                DU.DrawEntityLazy(dim);
            }
        }
        public static void AlignedDimensionTest2()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            //using (var tr = DrawUtils.DrawingTransaction)
            {
                //Debugger.Break();

                //var db = adb.Database;
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;

                //Dbg.BuildAndSetCurrentLayer(db);
                //var line = new Line() { StartPoint = Dbg.SelectPoint(), EndPoint = Dbg.SelectPoint() };

                //var line = Dbg.SelectEntity<Line>(adb);
                //var dimAligned = new AlignedDimension();
                //dimAligned.XLine1Point = line.StartPoint;
                //dimAligned.XLine2Point = line.EndPoint;

                var pt = Dbg.SelectPoint();
                var dim = new AlignedDimension();
                dim.XLine1Point = pt;
                dim.XLine2Point = pt.OffsetX(150);

                //dimAligned.DimLinePoint = GeTools.MidPoint(line.StartPoint, line.EndPoint).PolarPoint(Math.PI / 2, 10);
                dim.DimLinePoint = GeTools.MidPoint(pt, pt.OffsetX(150)).PolarPoint(Math.PI / 2, 10);
                dim.DimensionText = "AAA"; //
                dim.ColorIndex = 3;

                adb.ModelSpace.Add(dim);
                //AddDim(db, dim);
            }

        }
        public static void AlignedDimensionTest1()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;

                var pt = Dbg.SelectPoint();
                //AlignedDimension dim = new AlignedDimension();
                //dim.XLine1Point = new Point3d(100, 100, 0);
                //dim.XLine2Point = new Point3d(130, 130, 0);
                //dim.DimLinePoint = new Point3d(140, 125, 0);
                //dim.DimensionStyle = db.Dimstyle;
                var dim = new AlignedDimension();
                dim.XLine1Point = pt;
                dim.XLine2Point = pt.OffsetX(150);

                //dimAligned.DimLinePoint = GeTools.MidPoint(line.StartPoint, line.EndPoint).PolarPoint(Math.PI / 2, 10);
                dim.DimLinePoint = GeTools.MidPoint(pt, pt.OffsetX(150)).PolarPoint(Math.PI / 2, 10);
                dim.DimensionText = "AAA"; //
                dim.ColorIndex = 3;

                AddDim(db, dim);
            }

        }

        private static void AddDim(Database db, AlignedDimension dim)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTbl = tr.GetObject(
                    db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(
                    blockTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                modelSpace.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);
                tr.Commit();
            }
        }

        public static void AlignedDimensionTest4()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            //using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            //using (var tr = DrawUtils.DrawingTransaction)
            {
                ////Debugger.Break();
                //var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                //var line = new Line() { StartPoint = Dbg.SelectPoint(), EndPoint = Dbg.SelectPoint() };

                //var line = Dbg.SelectEntity<Line>(adb);
                //var dimAligned = new AlignedDimension();
                //dimAligned.XLine1Point = line.StartPoint;
                //dimAligned.XLine2Point = line.EndPoint;

                var pt = Dbg.SelectPoint();
                try
                {
                    var dimAligned = new AlignedDimension();
                    dimAligned.XLine1Point = pt;
                    dimAligned.XLine2Point = pt.OffsetX(150);

                    //dimAligned.DimLinePoint = GeTools.MidPoint(line.StartPoint, line.EndPoint).PolarPoint(Math.PI / 2, 10);
                    dimAligned.DimLinePoint = GeTools.MidPoint(pt, pt.OffsetX(150)).PolarPoint(Math.PI / 2, 10);
                    dimAligned.DimensionText = "AAA"; //
                                                      //DU.DrawLazy(dimAligned);
                    dimAligned.ColorIndex = 3;
                    adb.ModelSpace.Add(dimAligned);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.InnerException.Message);
                    MessageBox.Show(ex.InnerException.StackTrace);
                }
            }
        }

        public static void ExtendLineTest1()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var line = new Line() { StartPoint = Dbg.SelectPoint(), /*EndPoint = Dbg.SelectPoint()*/ };
                line.Extend(false, Dbg.SelectPoint());
                DU.DrawEntityLazy(line);
            }
        }

        class SpacialIndexHelper
        {
            public void Create<T>(IList<T> ents) where T : Entity
            {

            }
        }
        public static void FixSpacialIndex2()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                var si = new NTSSpatialIndex1(sv.CondensePipes.ToCollection());
                var input = SelectPoints();
                var range = new Point3dCollection();
                range.Add(input.Item1);
                range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                range.Add(input.Item2);
                range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
                foreach (var e in si.SelectCrossingPolygon(range).Cast<Entity>())
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void FixSpacialIndex1()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                ThCADCoreNTSService.Instance.ArcTessellationLength = 30;
                //var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(sv.VerticalPipeLines.ToCollection());
                //var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(sv.ConnectToRainPortSymbols.ToCollection());
                var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(sv.CondensePipes.ToCollection());
                //var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(sv.VerticalPipes.ToCollection());
                //var range = Dbg.SelectGRect().ToPoint3dCollection();
                var input = SelectPoints();
                var range = new Point3dCollection();
                range.Add(input.Item1);
                range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                range.Add(input.Item2);
                range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
                foreach (var e in si.SelectCrossingPolygon(range).Cast<Entity>())
                {
                    Dbg.ShowWhere(e);
                }
                //foreach (var e in sv.FiltByRect(range, sv.CondensePipes))
                //{
                //    Dbg.ShowWhere(e);
                //}
                Dbg.PrintLine(sv.CondensePipes.Count);


            }

        }
        public static void ExplodeTianZhengTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var o = Dbg.SelectEntity<Entity>(adb);
                var objs = new DBObjectCollection();
                o.Explode(objs);
                Dbg.SetText(objs.Cast<Entity>().Select(e => e.GetType().ToString()));
            }
        }
        public static void GetCondensePipesTest_MaybeBug()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();

                var range = Dbg.SelectGRect().ToPoint3dCollection();
                foreach (var e in sv.GetCondensePipes(range))
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void FiltByRectTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();

                var range = Dbg.SelectGRect().ToPoint3dCollection();
                foreach (var e in sv.FiltByRect(range, sv.CondensePipes))
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void FindOutBrokenCondensePipesTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.CollectData();
                //foreach (var e in sv.WRainLines)
                //{
                //    var lb = sv.GetLabel(e);
                //    if (lb != null)
                //    {
                //        //DU.DrawTextLazy(lb, sv.BoundaryDict[e].Center.ToPoint3d());
                //    }
                //    else
                //    {
                //        DU.DrawTextLazy("todo", sv.BoundaryDict[e].Center.ToPoint3d());
                //    }
                //}

                var cps1 = new HashSet<Entity>();
                var cps2 = new HashSet<Entity>();
                foreach (var e in sv.CondensePipes)
                {
                    var lb = sv.GetLabel(e);
                    if (lb != null)
                    {
                        //DU.DrawTextLazy(lb, sv.BoundaryDict[e].Center.ToPoint3d());
                        cps1.Add(e);
                    }
                    else
                    {
                        //DU.DrawTextLazy("todo", sv.BoundaryDict[e].Center.ToPoint3d());
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
                        if (sv.GetLabel(ee) == "NL1-3")
                        {
                            Dbg.ShowWhere(e);
                        }
                    }
                }

            }
        }

        public static void RunThRainSystemDiagramCmd_NoRectSelection3_DrawFromJson_MyDrawing()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.FocusMainWindow();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");


                //const string KEY = "qrjq0v";
                //const string KEY = "qrjq0w";
                const string KEY = "qrjq0x";

                var diagram = LoadData<ThWRainSystemDiagram>(KEY);
                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;
                diagram.Draw(basePt);
                //DrawDiagram(adb, diagram, basePt, points);
                DrLazy.Default.DrawLazy();
                DrawUtils.Draw();
                Dbg.FocusMainWindow();
            }
        }



        public static void DrawDiagram(AcadDatabase adb, ThWRainSystemDiagram diagram, Point3d basePt, Point3dCollection range)
        {
            var storeysRecEngine = new ThWStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            //diagram.CollectData(adb, range);
            //diagram.InitStoreys(storeysRecEngine.Elements);
            //diagram.InitVerticalPipeSystems(range);
        }
        public static void IsOnSameLineTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e1 = Dbg.SelectEntity<Entity>(adb);
                GeoAlgorithm.TryConvertToLineSegment(e1, out ThWGLineSegment seg1);
                var e2 = Dbg.SelectEntity<Entity>(adb);
                GeoAlgorithm.TryConvertToLineSegment(e2, out ThWGLineSegment seg2);
                Dbg.PrintLine(GeoAlgorithm.IsOnSameLine(seg1, seg2, 5).ToString());
            }
        }
        public static void GetEntityType()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                Dbg.ShowString(e.GetType().ToString());
            }
        }
        public static void GetEntityLength()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                GeoAlgorithm.TryConvertToLineSegment(e, out ThWGLineSegment seg);
                Dbg.ShowString(seg.Length.ToString());
            }
        }
        public static void GetEntityBlockName()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<BlockReference>(adb);
                Dbg.ShowString(e.Name);
            }
        }
        public static void GetColorIndex()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                Dbg.ShowString(e.ColorIndex.ToString());
            }
        }
        public static void GetCircleRadius()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Circle>(adb);
                Dbg.ShowString(e.Radius.ToString());
            }
        }
        public static void GetEntityLayer()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                Dbg.ShowString(e.Layer);
            }
        }
        public static void GetTextStyleName()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<DBText>(adb);
                Dbg.ShowString(e.TextStyleName);
            }
        }
        public static void ScaleEntityTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                EntTools.Scale(e, Dbg.SelectPoint(), 2);
            }
        }
        public static void RotateEntityTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                EntTools.Rotate(e, Dbg.SelectPoint(), GeoAlgorithm.AngleFromDegree(45));
            }
        }
        public static void MoveEntityTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                EntTools.Move(e, Dbg.SelectPoint(), GeoAlgorithm.GetBoundaryRect(e).Center.ToPoint3d());
            }
        }
        public static void CopyEntityTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                EntTools.Copy(e, Dbg.SelectPoint(), GeoAlgorithm.GetBoundaryRect(e).Center.ToPoint3d());
            }
        }
        public static void WrappingPipesLabelingTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                //foreach (var e in sv.WrappingPipes)
                //{
                //    DU.DrawBoundaryLazy(e);
                //}
                sv.CollectVerticalPipesData();
                sv.FindShortConverters();
                sv.LabelWRainLinesAndVerticalPipes();
                var groups = sv.LabelWrappingPipes();
                foreach (var g in groups)
                {
                    DU.DrawBoundaryLazy(g.Where(e => sv.WrappingPipes.Contains(e)).ToArray());
                    foreach (var e in g.Where(e => sv.WrappingPipes.Contains(e)))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
                        {
                            var t = DU.DrawTextLazy(lb, sv.BoundaryDict[e].Center.ToPoint3d());
                            t.Height = 50;
                        }
                    }
                }
            }
        }
        public static void WaterDrainLabelingTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                //foreach (var e in sv.WaterDrains)
                //{
                //    DU.DrawBoundaryLazy(e);
                //}
                sv.CollectVerticalPipesData();
                sv.FindShortConverters();
                sv.LabelWRainLinesAndVerticalPipes();
                var groups = sv.LabelFloorDrains();
                foreach (var g in groups)
                {
                    DU.DrawBoundaryLazy(g.Where(e => sv.FloorDrains.Contains(e)).ToArray());
                    foreach (var e in g.Where(e => sv.FloorDrains.Contains(e)))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
                        {
                            DU.DrawTextLazy(lb, sv.BoundaryDict[e].Center.ToPoint3d());
                        }
                    }
                }
            }
        }
        public static void CondensePipeLabelingTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                //foreach (var e in sv.CondensePipes)
                //{
                //    DU.DrawBoundaryLazy(e);
                //}
                sv.CollectVerticalPipesData();
                sv.FindShortConverters();
                sv.LabelWRainLinesAndVerticalPipes();
                var groups = sv.LabelCondensePipes();
                foreach (var g in groups)
                {
                    DU.DrawBoundaryLazy(g.Where(e => sv.CondensePipes.Contains(e)).ToArray());
                    foreach (var e in g.Where(e => sv.CondensePipes.Contains(e)))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
                        {
                            DU.DrawTextLazy(lb, sv.BoundaryDict[e].Center.ToPoint3d());
                        }
                    }
                }

            }
            try
            {

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        public static void CollectCondensePipesTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.CollectCondensePipes();
                foreach (var e in sv.CondensePipes)
                {
                    DU.DrawBoundaryLazy(e);
                }
            }
        }
        public static void DrawWrappingPipe()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var pt = Dbg.SelectPoint();
                DU.DrawBlockReference(blkName: "*U349", basePt: pt, cb: br => DU.SetLayerAndColorIndex("W-BUSH", 256, br));
            }
        }
        public static void DrawFloorDrainTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var pt = Dbg.SelectPoint();
                Dr.DrawFloorDrain(pt);
            }
        }
        public static void DrawFloorDrain()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var pt = Dbg.SelectPoint();
                DU.DrawBlockReference(blkName: "*U348", basePt: pt, scale: 2, cb: br => DU.SetLayerAndColorIndex("W-RAIN-EQPM", 256, br));
            }
        }
        public static void DrLazyTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //var dr = DrLazy.Default;
                //dr.BasePoint = Dbg.SelectPoint();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawLongTranslator();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawLongTranslator();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawLongTranslator();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.Break();
                //dr.DrawLazy();

                //var dr = DrLazy.Default;
                //dr.BasePoint = Dbg.SelectPoint();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawShortTranslator();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawShortTranslator();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawShortTranslator();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.Break();
                //dr.DrawLazy();

                //var dr = DrLazy.Default;
                //dr.BasePoint = Dbg.SelectPoint();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawNormalLine();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawNormalLine();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.DrawNormalLine();
                //Dbg.ShowWhere(dr.BasePoint);
                //dr.Break();
                //dr.DrawLazy();

                var dr = DrLazy.Default;
                dr.BasePoint = Dbg.SelectPoint();
                dr.DrawNormalLine();
                dr.DrawNormalLine();
                dr.DrawNormalLine();
                dr.DrawShortTranslator();
                dr.DrawShortTranslator();
                dr.DrawShortTranslator();
                dr.DrawLongTranslator();
                dr.DrawLongTranslator();
                dr.DrawLongTranslator();
                dr.DrawNormalLine();
                dr.DrawNormalLine();
                dr.DrawNormalLine();
                dr.Break();
                dr.DrawLazy();

            }
        }
        public static void SelectionDemo()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var tvs = new List<TypedValue>();
                tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(DBText)).DxfName + "," + RXClass.GetClass(typeof(MText)).DxfName));
                tvs.Add(new TypedValue((int)DxfCode.LayerName, Pipe.ThWPipeCommon.W_RAIN_NOTE));
                var sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {
                    foreach (var id in psr.Value.GetObjectIds())
                    {
                        var e = adb.Element<Entity>(id);
                        DU.DrawBoundaryLazy(e);
                    }
                }
            }
        }
        public static void RelatedGravityWaterBucketTest2()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Point3d pt1, pt2;
                SelectAllStoreys(out pt1, out pt2);
                var pts = GeoAlgorithm.GetPoint3dCollection(pt1, pt2);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.thGravityService.Init(pts);
                sv.CollectData();
                foreach (var ept in sv.thGravityService.GetRelatedGravityWaterBucket(pts))
                {
                    //Dbg.ShowWhere(ept.ToThWGRect());
                    //DU.DrawRectLazy(ept.ToThWGRect());
                    var r = ept.ToThWGRect();
                    Dbg.ShowWhere(r);
                    foreach (var e in sv.VerticalPipes)
                    {
                        if (GeoAlgorithm.Distance(r.Center, sv.BoundaryDict[e].Center) < 100)
                        {
                            Dbg.ShowWhere(ept.ToThWGRect());
                            break;
                        }
                    }
                }
            }
        }
        public static void GetRelatedGravityWaterBucketTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Point3d pt1, pt2;
                SelectAllStoreys(out pt1, out pt2);
                var pts = GeoAlgorithm.GetPoint3dCollection(pt1, pt2);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.thGravityService.Init(pts);
                sv.CollectData();
                foreach (var ept in sv.thGravityService.GetRelatedGravityWaterBucket(pts))
                {
                    //Dbg.ShowWhere(ept.ToThWGRect());
                    DU.DrawRectLazy(ept.ToThWGRect());
                }
            }
        }
        public static void DrawWaterWellTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: Dbg.SelectPoint(),
                    scale: 0.5,
                    props: new Dictionary<string, string>() { { "-", "666" } },
                    cb: br =>
                    {
                        br.Layer = "W-RAIN-EQPM";
                        var arr = br.DynamicBlockReferencePropertyCollection;
                        for (int i = 0; i < arr.Count; i++)
                        {
                            Dbg.PrintLine(arr[i].PropertyName);
                        }
                        //arr.SetValue("-", "666");
                    });
            }

        }
        public static void GetTextHeight()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<DBText>(adb);
                Dbg.ShowString(e.Height.ToString());
            }
        }
        public static void GetLineThickness()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Line>(adb);
                Dbg.ShowString(e.Thickness.ToString());
            }
        }
        public static void DrawDnText()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var pt = Dbg.SelectPoint();
                var t = new DBText() { TextString = "DN25", Position = pt, Height = 200 };
                DU.DrawEntityLazy(t);
                DU.SetLayerAndColorIndex("W-RAIN-DIMS", 256, t);
            }
        }
        public static void DrawCondensePipe()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var pt = Dbg.SelectPoint();
                var c1 = DU.DrawCircleLazy(pt, 30);
                DU.SetLayerAndColorIndex("W-RAIN-EQPM", 256, c1);
            }
        }
        public static void DrawDoubleCondensePipe()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var pt = Dbg.SelectPoint();
                var c1 = DU.DrawCircleLazy(pt, 30);
                var c2 = DU.DrawCircleLazy(pt.OffsetX(500), 30);
                DU.SetLayerAndColorIndex("W-RAIN-EQPM", 256, c1, c2);
            }
        }
        public static void DrawTextUnderlineTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var pt = Dbg.SelectPoint();
                var h = 100;
                var t = DU.DrawTextLazy("我是标注", h, pt);
                t.Layer = "W-RAIN-NOTE";
                t.ColorIndex = 256;
                //t.TextStyleName = "TH-STYLE3";
                //var tb = AcHelper.Collections.Tables.GetTextStyle("TH-STYLE3");
                //t.ObjectId.SetTextStyle("TH-STYLE3");
                var line = DU.DrawTextUnderlineLazy(t, 10, 10);
                line.Layer = "W-RAIN-NOTE";
                line.ColorIndex = 256;
            }
        }


        public static void GetColor()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<BlockReference>(adb);
                Dbg.ShowString(e.Color.ToString());
            }
        }

        public static void DrawRoofWaterBucket()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var layerName = Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint();
                DU.DrawBlockReference("屋面雨水斗", basePt);
            }
        }
        public static void DrawCheckPoint()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var layerName = Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint();
                DU.DrawBlockReference("立管检查口", basePt, br =>
                {
                    br.Layer = "W-RAIN-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(180);
                });

            }
        }

        public static void DrawSideWaterBucketBlock()
        {
            Dbg.FocusMainWindow();
            var basePt = Dbg.SelectPoint();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var db = adb.Database;
                var layerName = Dbg.BuildAndSetCurrentLayer(db);
                var blkName = "侧排雨水斗系统";
                //var blkName = "屋面雨水斗";
                adb.ModelSpace.ObjectId.InsertBlockReference(layerName, blkName, basePt, new Scale3d(1), 0);
            }
        }
        public static void LabelWaterWells2()
        {
            //若雨水管的端点什么都没有连接，则找到离端点1000范围内直线距离最近的雨水井图块。
            //1000不够，暂时改成1500
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.CollectData();
                //foreach (var e in sv.WaterWells)
                //{
                //    if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                //    {
                //        Dbg.ShowWhere(e);
                //    }
                //}
                var lines = sv.WRainLines;
                var f = ThRainSystemService.BuildSpatialIndexLazy(lines);
                foreach (var e in sv.WaterWells.Where(e => !sv.VerticalPipeToLabelDict.ContainsKey(e)).ToList())
                {
                    var pl = sv.CreatePolygon(e, 6, 1500);
                    foreach (var ee in f(pl))
                    {
                        Dbg.ShowWhere(ee);
                    }
                }
            }
        }
        public static void GetOutputTypeTest()
        {
            Dbg.FocusMainWindow();
            using var adb = AcadDatabase.Active();
            using var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            using var tr = DrawUtils.DrawingTransaction;
            var db = adb.Database;
            Dbg.BuildAndSetCurrentLayer(db);
            var sv = new ThRainSystemService() { adb = adb };
            sv.CollectData();
            foreach (var id in sv.VerticalPipeToLabelDict.Values.Distinct())
            {
                var rt = sv.GetOutputType(null, id, out _);
                Dbg.PrintLine("GetOutputType " + id + " " + rt.ToString());
            }
        }





        public static void FindAllWaterPorts()
        {
            Dbg.FocusMainWindow();
            using var adb = AcadDatabase.Active();
            using var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            using var tr = DrawUtils.DrawingTransaction;
            var db = adb.Database;
            Dbg.BuildAndSetCurrentLayer(db);
            var sv = new ThRainSystemService() { adb = adb };
            sv.InitCache();

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

            foreach (var g in groups)
            {
                foreach (var e in g)
                {
                    if (sv.ConnectToRainPortSymbols.Contains(e))
                    {
                        DU.DrawBoundaryLazy(g.ToArray());
                        break;
                    }
                }
            }
        }
        public static void ShowAllLongConvertersAndPipesGroups()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.CollectVerticalPipesData();
                sv.FindShortConverters();
                sv.LabelWRainLinesAndVerticalPipes();
                sv.LabelWaterPorts();
                foreach (var e in sv.ConnectToRainPortSymbols)
                {
                    if (sv.VerticalPipeToLabelDict.ContainsKey(e))
                    {
                        DU.DrawBoundaryLazy(e);
                    }
                }
                foreach (var e in sv.ConnectToRainPortDBTexts)
                {
                    if (sv.VerticalPipeToLabelDict.ContainsKey(e))
                    {
                        DU.DrawBoundaryLazy(e);
                    }
                }
                return;
                //totalList.AddRange(sv.WRainLines);
                //totalList.AddRange(sv.VerticalPipes);
                //ThRainSystemService.MakePairs(ThRainSystemService.GroupLines(sv.WRainLines), pairs);
                //foreach (var item in sv.ShortConverters) pairs.Add(item.ToKV());
                //pairs.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.VerticalPipeList, 50));
                //ThRainSystemService.GroupByBFS(groups, totalList, pairs);
                //foreach (var g in groups)
                //{
                //    DU.DrawBoundaryLazy(g.ToArray());
                //}
            }
        }



        private static void SelectAllStoreys(out Point3d pt1, out Point3d pt2)
        {
            pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
            pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();
        }

        public static void RunThRainSystemDiagramCmd_NoRectSelection()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var pt1 = "{x:-51259.1494784583,y:914935.416475339,z:0}".JsonToPoint3d();
                var pt2 = "{x:214660.913246393,y:791487.904818842,z:0}".JsonToPoint3d();
                NewMethod(adb, pt1, pt2);
            }
        }
        public static void qs04qa()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
                var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();

                var points = new Point3dCollection();
                points.Add(pt1);
                points.Add(new Point3d(pt1.X, pt2.Y, 0));
                points.Add(pt2);
                points.Add(new Point3d(pt2.X, pt1.Y, 0));
                Dbg.FocusMainWindow();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");

                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;

                var diagram = new ThWRainSystemDiagram();
                //var storeysRecEngine = new ThWStoreysRecognitionEngine();
                //storeysRecEngine.Recognize(adb.Database, points);
                ////var sw = new Stopwatch();
                ////sw.Start();
                //diagram.InitCacheData(adb, points);
                ////Dbg.PrintLine("InitCacheData:" + sw.Elapsed.TotalSeconds.ToString());
                //diagram.InitStoreys(storeysRecEngine.Elements);
                ////Dbg.PrintLine("InitStoreys:" + sw.Elapsed.TotalSeconds.ToString());
                //diagram.InitVerticalPipeSystems(points);
                ////Dbg.PrintLine("InitVerticalPipeSystems:" + sw.Elapsed.TotalSeconds.ToString());
                //diagram.Draw(basePt);
                ////Dbg.PrintLine(" diagram.Draw(basePt):" + sw.Elapsed.TotalSeconds.ToString());
                ////sw.Stop();





                DrawUtils.Draw();
            }
        }

        public static void RunThRainSystemDiagramCmd_NoRectSelection3_CollectData()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
                var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();

                var points = new Point3dCollection();
                points.Add(pt1);
                points.Add(new Point3d(pt1.X, pt2.Y, 0));
                points.Add(pt2);
                points.Add(new Point3d(pt2.X, pt1.Y, 0));
                Dbg.FocusMainWindow();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");

                //const string KEY = "qrjq0w";
                const string KEY = "qrjq0x";

                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;
                //var basePt = default(Point3d);

                var diagram = new ThWRainSystemDiagram();
                var storeysRecEngine = new ThWStoreysRecognitionEngine();
                storeysRecEngine.Recognize(adb.Database, points);
                //var sw = new Stopwatch();
                //sw.Start();
                diagram.InitServices(adb, points);
                //Dbg.PrintLine("InitCacheData:" + sw.Elapsed.TotalSeconds.ToString());
                diagram.InitStoreys(storeysRecEngine.Elements);
                //Dbg.PrintLine("InitStoreys:" + sw.Elapsed.TotalSeconds.ToString());
                diagram.InitVerticalPipeSystems(points);
                //Dbg.PrintLine("InitVerticalPipeSystems:" + sw.Elapsed.TotalSeconds.ToString());
                diagram.Draw(basePt);
                //Dbg.PrintLine(" diagram.Draw(basePt):" + sw.Elapsed.TotalSeconds.ToString());
                //sw.Stop();
                DrLazy.Default.DrawLazy();
                DrawUtils.Draw();

                SaveData(KEY, diagram);
            }
        }
        public static void LabelFloorDrainsWrappingPipeTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.InitCache();
                sv.CollectVerticalPipesData();
                sv.FindShortConverters();
                sv.LabelWRainLinesAndVerticalPipes();
                sv.LabelCondensePipes();
                sv.LabelFloorDrains();
                sv.LabelWrappingPipes();
                var gs = sv.LabelFloorDrainsWrappingPipe();
                foreach (var g in gs)
                {
                    if (g.Count < 3) continue;
                    if (!g.Any(e => sv.WRainLines.Contains(e)) || !g.Any(e => sv.WrappingPipes.Contains(e)) || !g.Any(e => sv.FloorDrains.Contains(e))) continue;
                    foreach (var e in g)
                    {
                        if (sv.WrappingPipes.Contains(e))
                        {
                            DU.DrawBoundaryLazy(e);
                        }
                    }
                }
            }
        }
        public static void ExecuteThRainSystemDiagramCmd()
        {
            Dbg.FocusMainWindow();
            using (var cmd = new ThMEPWSS.Command.ThRainSystemDiagramCmd())
            {
                cmd.Execute();
            }
        }
        public static void RunThRainSystemDiagramCmd_NoRectSelection3_DrawFromJson()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
                var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();

                var points = new Point3dCollection();
                points.Add(pt1);
                points.Add(new Point3d(pt1.X, pt2.Y, 0));
                points.Add(pt2);
                points.Add(new Point3d(pt2.X, pt1.Y, 0));
                Dbg.FocusMainWindow();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");


                //const string KEY = "qrjq0v";
                //const string KEY = "qrjq0w";
                const string KEY = "qrjq0x";

                var diagram = LoadData<ThWRainSystemDiagram>(KEY);
                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;
                diagram.Draw(basePt);

                DrLazy.Default.DrawLazy();
                DrawUtils.Draw();
                Dbg.FocusMainWindow();
            }

        }



        private static void NewMethod(AcadDatabase adb, Point3d pt1, Point3d pt2)
        {
            Point3dCollection points = GeoAlgorithm.GetPoint3dCollection(pt1, pt2);
            Dbg.FocusMainWindow();
            var basePtOptions = new PromptPointOptions("\n选择图纸基点");
            var rst = Active.Editor.GetPoint(basePtOptions);
            if (rst.Status != PromptStatus.OK) return;
            var basePt = rst.Value;

            var diagram = new ThWRainSystemDiagram();
            var storeysRecEngine = new ThWStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, points);
            //var sw = new Stopwatch();
            //sw.Start();
            diagram.InitServices(adb, points);
            //Dbg.PrintLine("InitCacheData:" + sw.Elapsed.TotalSeconds.ToString());
            diagram.InitStoreys(storeysRecEngine.Elements);
            //Dbg.PrintLine("InitStoreys:" + sw.Elapsed.TotalSeconds.ToString());
            diagram.InitVerticalPipeSystems(points);
            //Dbg.PrintLine("InitVerticalPipeSystems:" + sw.Elapsed.TotalSeconds.ToString());
            diagram.Draw(basePt);
            //Dbg.PrintLine(" diagram.Draw(basePt):" + sw.Elapsed.TotalSeconds.ToString());
            //sw.Stop();
            DrLazy.Default.DrawLazy();
            DrawUtils.Draw();
        }



        public static void ThRainSystemService_test()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.CollectData();
            }

        }
    }

    public class ThDebugClass1
    {
        public static Dictionary<string, object> ctx => FengDbgTest.ctx;
        public static Dictionary<string, object> processContext => (Dictionary<string, object>)ctx["processContext"];

        public static void Test1()
        {
            MessageBox.Show("test1");
        }
        public static void Test2()
        {
            MessageBox.Show("test2");
        }
        //todo:wrap into data class
        public static void demo1()
        {
            FengDbgTest.processContext["qs04qm"] = 123;
        }
        public static void demo2()
        {
            FengDbgTest.processContext["qs04qm"] = ((int)FengDbgTest.processContext["qs04qm"]) + 1;
        }
        public static void demo3()
        {
            MessageBox.Show(FengDbgTest.processContext["qs04qm"].ToJson());
        }
        public static void ShowString()
        {
            Dbg.ShowString("hello Feng");
        }
        public static void CollectSelectionTest()
        {
            Dbg.FocusMainWindow();
            var json = ThMEPWSS.Command.ThRainSystemDiagramCmd.CollectSelectionTest();
            Dbg.ShowString(json);
        }
        public static void CollectRect()
        {
            Dbg.FocusMainWindow();
            var r = Dbg.SelectGRect();
            Dbg.ShowString(r.ToJson());
            //1F
            //[-29893.7484479847,365130.744432027,84957.5857769808,466578.676778659]
            //4-31F
            //[-38594.7998397839,819403.971286154,194315.671562288,899867.801990787]
            //RF
            //[-56272.1246186205,1550739.4095195,99367.9063746997,1803726.6518944]
            //all
            //[-101961.01485372,362865.950722739,236385.64048681,1890656.88403184]
        }
        public static void DrawLargeCircleTest()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.FocusMainWindow();
                new Circle() { Center = Dbg.SelectPoint(), Radius = 1000000, ColorIndex = 6, Thickness = 100000 }.AddToCurrentSpace();
            }
        }
        public static void DrawTextLazyTest()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                DrawUtils.DrawTextLazy("ThWRainPipeRun ", 100, Dbg.SelectPoint());
                DrawUtils.Draw();
            }
        }
        public static void RunThRainSystemDiagramCmd()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var cmd = new Command.ThRainSystemDiagramCmd())
            {
                var sw = new Stopwatch();
                sw.Start();
                cmd.Execute();
                Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());
                sw.Stop();
            }
        }
        public static void PrintTest()
        {
            Dbg.Print("hello?");
        }

        public static void PrintLineTest()
        {
            Dbg.PrintLine("PrintLineTest");
        }
        public static void PrintTextTest()
        {
            Dbg.PrintText("PrintTextTest\nPrintTextTest");
        }
        public static void DisplayColorIndexes()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint();
                for (int j = 0; j < 10; j++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        var i = j * 10 + k;
                        var c = new Circle() { Center = basePt.OffsetXY(j * 200, k * 200), Radius = 100, ColorIndex = i, Thickness = 10 };
                        adb.ModelSpace.Add(c);
                    }
                }
            }
        }
        public static void DisplayCircleColors()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var radius = 100;
                var pt = Dbg.SelectPoint();
                for (int i = 3; i < 10; i++)
                {
                    new Circle() { Center = pt, Thickness = 5, ColorIndex = i, Radius = radius }.AddToCurrentSpace();
                    radius += 50;
                }
            }
        }


        public static void ExplodeTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.FocusMainWindow();
                var e = Dbg.SelectEntity<Entity>(adb);
                if (e is BlockReference blk)
                {
                    var colle = e.ExplodeToDBObjectCollection();
                    Debugger.Break();
                }
            }
        }
        public static void SelectEntityTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.FocusMainWindow();
                var e = Dbg.SelectEntity<Entity>(adb);
                Debugger.Break();
            }
        }
        public static void FindAllWrappingPipe()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var pipes = adb.ModelSpace.OfType<BlockReference>().Where(x => x.BlockName.Contains("普通套管")).ToList();
                foreach (var p in pipes)
                {
                    Dbg.ShowWhere(p);
                }
            }
        }
        public static void VectorTest()
        {
            var pt1 = default(Point2d);
            var pt2 = new Point2d(1, 1);
            var v = pt2 - pt1;
            //MessageBox.Show(GeoAlgorithm.AngleToDegree(v.Angle).ToString());
            MessageBox.Show(v.Length.ToString());
        }
        public static void CollectRainDrainData()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")).ToList();
                //Dbg.SetText(list.Select(x => x.Name));
                Dbg.SetText(list.Select(x => x.GetAttributesStrValue("-")));
            }
        }
        public static void FindAllRainDrain13s()
        {
            Dbg.FocusMainWindow();
            using var adb = AcadDatabase.Active();
            using var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            using var tr = DrawUtils.DrawingTransaction;
            var db = adb.Database;
            Dbg.BuildAndSetCurrentLayer(db);
            var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "13#雨水口").ToList();
            foreach (var e in list)
            {
                Dbg.ShowWhere(e);
            }
        }

        public static void FindAllConnectToRainDrainSymbol()
        {
            Dbg.FocusMainWindow();
            using var adb = AcadDatabase.Active();
            using var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            using var tr = DrawUtils.DrawingTransaction;
            var db = adb.Database;
            Dbg.BuildAndSetCurrentLayer(db);
            var list = adb.ModelSpace.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS").ToList();
            foreach (var e in list)
            {
                Dbg.ShowWhere(e);
            }
        }
        public static void ShowVerticalPipeToLabelDict()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService();
                sv.adb = adb;
                sv.CollectData();
                foreach (var item in sv.VerticalPipeToLabelDict)
                {
                    Dbg.ShowWhere(item.Key);
                }
            }
        }
        public static void ShowBlockReferenceData()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<BlockReference>(adb);
                Dbg.ShowString(e.Layer + " " + e.ToDataItem().EffectiveName);
            }
        }
        const double tol = 1;
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

        public static void ExpandLineTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var line = Dbg.SelectEntity<Line>(adb);
                var p1 = expandLine(line, 10);
                adb.ModelSpace.Add(p1);
            }
        }
        public static void ThCADCoreNTSRelate_IsIntersects_Test4()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Polyline p1, p2;
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    p1 = line.Buffer(10);
                    //adb.ModelSpace.Add(p1);
                }
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    p2 = line.Buffer(10);
                    //adb.ModelSpace.Add(p2);
                }
                //adb.ModelSpace.Add(p1.MinimumBoundingBox());
                //adb.ModelSpace.Add(p2.MinimumBoundingBox());
                //var o = new ThCADCore.NTS.ThCADCoreNTSRelate(p1.MinimumBoundingBox(), p2.MinimumBoundingBox());
                var o = new ThCADCore.NTS.ThCADCoreNTSRelate(p1, p2);

                Dbg.ShowString(o.IsIntersects.ToString());
            }
        }
        public static void ThCADCoreNTSRelate_IsIntersects_Test3()
        {
            var t = typeof(ThCADCore.NTS.ThCADCoreNTSRelate);
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Polyline p1, p2;
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    p1 = expandLine(line, 10);
                }
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    p2 = expandLine(line, 10);
                }
                var o = new ThCADCore.NTS.ThCADCoreNTSRelate(p1.MinimumBoundingBox(), p2.MinimumBoundingBox());
                Dbg.ShowString(o.IsIntersects.ToString());
            }
        }
        public static void ThCADCoreNTSRelate_IsIntersects_Test2()
        {
            var t = typeof(ThCADCore.NTS.ThCADCoreNTSRelate);
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Polyline p1, p2;
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    var deltax = line.EndPoint.X - line.StartPoint.X;
                    var deltay = line.EndPoint.Y - line.StartPoint.Y;
                    var pts = new Point2dCollection() { line.StartPoint.ToPoint2D().OffsetXY(-deltax, -deltay), line.EndPoint.ToPoint2D().OffsetXY(deltax, deltay) };
                    var pline = new Polyline();
                    for (int i = 0; i < pts.Count; i++)
                    {
                        pline.AddVertexAt(i, pts[i], 0, 0, 0);
                    }
                    p1 = pline;
                }
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    var deltax = line.EndPoint.X - line.StartPoint.X;
                    var deltay = line.EndPoint.Y - line.StartPoint.Y;
                    var pts = new Point2dCollection() { line.StartPoint.ToPoint2D().OffsetXY(-deltax, -deltay), line.EndPoint.ToPoint2D().OffsetXY(deltax, deltay) };
                    var pline = new Polyline();
                    for (int i = 0; i < pts.Count; i++)
                    {
                        pline.AddVertexAt(i, pts[i], 0, 0, 0);
                    }
                    p2 = pline;
                }
                var o = new ThCADCore.NTS.ThCADCoreNTSRelate(p1.MinimumBoundingBox(), p2.MinimumBoundingBox());
                Dbg.ShowString(o.IsIntersects.ToString());
            }
        }
        public static void ThCADCoreNTSRelate_IsIntersects_Test()
        {
            var t = typeof(ThCADCore.NTS.ThCADCoreNTSRelate);
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Polyline p1, p2;
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    var pts = new Point2dCollection() { line.StartPoint.ToPoint2D(), line.EndPoint.ToPoint2D() };
                    var pline = new Polyline();
                    for (int i = 0; i < pts.Count; i++)
                    {
                        pline.AddVertexAt(i, pts[i], 0, 0, 0);
                    }
                    p1 = pline;
                }
                {
                    var line = Dbg.SelectEntity<Line>(adb);
                    var pts = new Point2dCollection() { line.StartPoint.ToPoint2D(), line.EndPoint.ToPoint2D() };
                    var pline = new Polyline();
                    for (int i = 0; i < pts.Count; i++)
                    {
                        pline.AddVertexAt(i, pts[i], 0, 0, 0);
                    }
                    p2 = pline;
                }
                var o = new ThCADCore.NTS.ThCADCoreNTSRelate(p1.MinimumBoundingBox(), p2.MinimumBoundingBox());
                Dbg.ShowString(o.IsIntersects.ToString());
            }
        }
        public static void ThRainSystemServiceTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.CollectData();
                //sv.Pipes.ForEach(o => DrawUtils.DrawBoundaryLazy(o));
                //sv.Lines.ForEach(o => DrawUtils.DrawBoundaryLazy(o));

                //sv.ShortConverters.ForEach(t =>
                //{
                //    DrawUtils.DrawBoundaryLazy(t.Item1, 10);
                //    DrawUtils.DrawBoundaryLazy(t.Item2, 10);
                //    Dbg.ShowWhere(GeoAlgorithm.GetBoundaryRect(t.Item2));
                //});

                //sv.CollectLongConverterLines();
                //sv.LongConverterLines.ForEach(e =>
                //{
                //    DrawUtils.DrawBoundary(db, e, 2);
                //});

                //foreach (var pipe in sv.Pipes)
                //{
                //    //var r1 = GeoAlgorithm.GetBoundaryRect(pipe).Expand(10);
                //    var r1 = sv.BoundaryDict[pipe].Expand(10);
                //    foreach (var cvt in sv.LongConverterLines)
                //    {
                //        //var r2 = GeoAlgorithm.GetBoundaryRect(cvt);
                //        var r2 = sv.BoundaryDict[cvt];
                //        if (GeoAlgorithm.IsRectCross(r1, r2))
                //        {
                //            sv.LongConverterToPipesDict.Add(cvt, pipe);
                //        }
                //    }
                //}
                //sv.LongConverterToPipesDict.ForEach((k, v) =>
                //{
                //    //DrawUtils.DrawBoundary(db, k, 4);
                //    //v.ForEach(e => DrawUtils.DrawBoundary(db, e, 2));

                //    //DU.DrawRectLazy(sv.BoundaryDict[k]);
                //    //v.ForEach(e => DU.DrawRectLazy(sv.BoundaryDict[e]));
                //});

                //for (int i = 0; i < sv.LongConverterLines.Count; i++)
                //{
                //    for (int j = i + 1; j < sv.LongConverterLines.Count; j++)
                //    {
                //        var cvt1 = sv.LongConverterLines[i];
                //        var cvt2 = sv.LongConverterLines[j];
                //        //if (GeoAlgorithm.IsRectCross(sv.BoundaryDict[cvt1].Expand(1), sv.BoundaryDict[cvt2].Expand(1)))
                //        if (GeoAlgorithm.IsLineConnected(cvt1, cvt2))
                //        {
                //            sv.LongConverterToLongConvertersDict.Add(cvt1, cvt2);
                //            sv.LongConverterToLongConvertersDict.Add(cvt2, cvt1);
                //        }
                //    }
                //}

                //var ent = Dbg.SelectEntity<Entity>(adb);
                //sv.LongConverterToLongConvertersDict[ent].ForEach(e => DU.DrawRectLazy(sv.BoundaryDict[e]));

                //sv.Pipes.ForEach(p => DU.DrawRectLazy(sv.BoundaryDict[p]));
                sv.CollectDraiDomePipes();
                //sv.DraiDomePipes.ForEach(e => DU.DrawRectLazy(sv.BoundaryDict[e]));
                var list = new List<KeyValuePair<Entity, ThWGLineSegment>>();
                foreach (var pipe in sv.DraiDomePipes)
                {
                    if (GeoAlgorithm.TryConvertToLineSegment(pipe, out ThWGLineSegment seg))
                    {
                        list.Add(new KeyValuePair<Entity, ThWGLineSegment>(pipe, seg));
                    }
                }
                //var ent = Dbg.SelectEntity<Entity>(adb);
                var pairs = new List<KeyValuePair<int, int>>();
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var kv1 = list[i];
                        var kv2 = list[j];
                        const double dis = 8000;
                        if (NewMethod(kv1, kv2, dis))
                        {
                            pairs.Add(new KeyValuePair<int, int>(i, j));
                        }
                    }
                }
                var dict = new ListDict<int>();
                var h = new BFSHelper()
                {
                    Pairs = pairs.ToArray(),
                    TotalCount = list.Count,
                    Callback = (g, i) =>
                    {
                        dict.Add(g.root, i);
                    },
                };
                h.BFS();
                groups = new List<List<Entity>>();
                dict.ForEach((_i, l) =>
                {
                    //DU.DrawBoundaryLazy(l.Select(i => list[i].Key).ToArray(), 2);
                    groups.Add(l.Select(i => list[i].Key).ToList());
                });
                //var ent = Dbg.SelectEntity<Entity>(adb);

                //for (int i = 0; i < list.Count; i++)
                //{
                //    for (int j = i + 1; j < list.Count; j++)
                //    {
                //        var kv1 = list[i];
                //        var kv2 = list[j];
                //        if (GeoAlgorithm.IsLineConnected(kv1.Value, kv2.Value, 5) || (kv1.Value.IsHorizontalOrVertical(5) && kv2.Value.IsHorizontalOrVertical(5) && GeoAlgorithm.IsOnSameLine(kv1.Value, kv2.Value, 5) && GeoAlgorithm.GetMinConnectionDistance(kv1.Value, kv2.Value) < 8000))
                //        {
                //            if(kv1.Key==ent||kv2.Key==ent)
                //            {
                //                 DU.DrawBoundaryLazy(kv1.Key, 2);
                //            DU.DrawBoundaryLazy(kv2.Key, 2);
                //            }
                //        }
                //    }
                //}
                Dbg.FocusMainWindow();
            }
        }

        private static bool NewMethod(KeyValuePair<Entity, ThWGLineSegment> kv1, KeyValuePair<Entity, ThWGLineSegment> kv2, double dis)
        {
            var seg1 = kv1.Value;
            var seg2 = kv2.Value;
            return GeoAlgorithm.CanConnect(seg1, seg2, dis);
        }



        public static void ThRainSystemServiceTest2()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                var targetEnt = Dbg.SelectEntity<Entity>(adb);
                var r = sv.BoundaryDict[targetEnt];
                //foreach (var e in sv.LongConverterLines.Where(e => GeoAlgorithm.IsRectCross(sv.BoundaryDict[e], r)))
                //{
                //    DU.DrawBoundary(db, e, 3);
                //}
                foreach (var e1 in sv.VerticalPipeLines.Where(e => GeoAlgorithm.IsRectCross(sv.BoundaryDict[e], r)))
                {
                    DU.DrawBoundary(db, e1, 3);
                    foreach (var e2 in sv.VerticalPipeLines.Where(e => GeoAlgorithm.IsRectCross(sv.BoundaryDict[e], sv.BoundaryDict[e1])))
                    {
                        if (e2 != e1)
                        {
                            DU.DrawBoundary(db, e2, 3);
                            foreach (var e3 in sv.ConnectToRainPortDBTexts.Where(e => GeoAlgorithm.IsRectCross(sv.BoundaryDict[e], sv.BoundaryDict[e2].Expand(200))))
                            {
                                DU.DrawBoundary(db, e3, 3);
                            }
                        }
                    }
                }
                Dbg.FocusMainWindow();
            }
        }
        public static void GetConnectedPipes()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                var plDict = new Dictionary<Entity, Polyline>();
                foreach (var e in sv.VerticalPipes)
                {
                    var bd = sv.BoundaryDict[e];
                    var pl = PolylineTools.CreatePolygon(bd.Center, 4, bd.Radius);
                    plDict[e] = pl;
                }

                var ent = Dbg.SelectEntity<Line>(adb);
                var pline = ent.Buffer(10);
                foreach (var kv in plDict)
                {
                    var e = kv.Key; var pl = kv.Value;
                    if (new ThCADCore.NTS.ThCADCoreNTSRelate(pline, pl).IsIntersects)
                    {
                        DU.DrawBoundaryLazy(e);
                    }
                }
            }
        }
        public static void ThRainSystemServiceTest3_OK()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                var dbTxtToHLineDict = new Dictionary<Entity, Entity>();
                foreach (var e1 in sv.VerticalPipeDBTexts)
                {
                    //Dbg.ShowWhere(e);
                    foreach (var e2 in sv.VerticalPipeLines)
                    {
                        if (e2 is Line line)
                        {
                            var seg = line.ToGLineSegment();
                            if (seg.IsHorizontal(10))
                            {
                                //Dbg.ShowWhere(e);
                                var c1 = sv.BoundaryDict[e1].Center;
                                var c2 = sv.BoundaryDict[e2].Center;
                                if (c1.Y > c2.Y && GeoAlgorithm.Distance(c1, c2) < 150)
                                {
                                    //Dbg.ShowWhere(e1);
                                    //Dbg.ShowWhere(e2);
                                    dbTxtToHLineDict[e1] = e2;
                                    break;
                                }
                            }
                        }
                    }
                }
                {
                    var list = sv.VerticalPipeLines;
                    var pairs = new List<KeyValuePair<int, int>>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        for (int j = i + 1; j < list.Count; j++)
                        {
                            var line1 = list[i] as Line;
                            var line2 = list[j] as Line;
                            if (line1 != null && line2 != null)
                            {
                                //var pline1 = ThRainSystemService.expandLine(line1, 10);
                                //var pline2 = ThRainSystemService.expandLine(line2, 10);
                                var pline1 = line1.Buffer(10);
                                var pline2 = line2.Buffer(10);
                                //if (ThRainSystemService.IsIntersects(pline1, pline2))
                                if (new ThCADCore.NTS.ThCADCoreNTSRelate(pline1, pline2).IsIntersects)
                                {
                                    pairs.Add(new KeyValuePair<int, int>(i, j));
                                }
                            }
                        }
                    }

                    var dict = new ListDict<int>();
                    var h = new BFSHelper()
                    {
                        Pairs = pairs.ToArray(),
                        TotalCount = list.Count,
                        Callback = (g, i) =>
                        {
                            dict.Add(g.root, i);
                        },
                    };
                    h.BFS();
                    groups = new List<List<Entity>>();
                    dict.ForEach((_i, l) =>
                    {
                        groups.Add(l.Select(i => list[i]).ToList());
                    });
                    foreach (var g in groups)
                    {
                        DU.DrawBoundaryLazy(g.ToArray());
                    }
                }

            }
        }
        public static void ThCADCoreNTSSpatialIndexTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(sv.VerticalPipes.ToCollection());
                var r = Dbg.SelectGRect();
                var rst = si.SelectCrossingPolygon(r.ToPoint3dCollection()).Cast<Entity>().ToList();
                foreach (var e in rst)
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void GroupWRainLinesTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                var lines = sv.WRainLines;
                var groups = new List<List<Entity>>();
                ThRainSystemService.GroupLines(lines, groups, 10);
                foreach (var g in groups)
                {
                    DU.DrawBoundaryLazy(g.ToArray());
                }
            }
        }

        public static void ShowAllLongConverters()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                foreach (var e in sv.LongConverterLines)
                {
                    DU.DrawBoundaryLazy(e);
                }
            }
        }

        public static void qrwq0q()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();

                //foreach (var e in sv.WaterWells)
                //{
                //    DU.DrawBoundaryLazy(e);
                //}

                //foreach (var e in sv.WRainLines)
                //{
                //    DU.DrawBoundaryLazy(e);
                //}

                var lines = sv.WRainLines;
                var wells = sv.WaterWells;
                var mps1 = lines.Select(e => new KeyValuePair<Entity, Polyline>(e, (e as Line)?.Buffer(10))).ToList();
                var mps2 = wells.Select(e => new KeyValuePair<Entity, Polyline>(e, sv.CreatePolygon(e, expand: 50))).ToList();
                var bfs = mps1.Select(kv => kv.Value).ToList();
                var pls = mps2.Select(kv => kv.Value).ToList();
                var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
                var dict = new Dictionary<Entity, Entity>();
                foreach (var pl in pls)
                {
                    foreach (var bf in si.SelectCrossingPolygon(pl).Cast<Polyline>().ToList())
                    {
                        var line = mps1.First(kv => kv.Value == bf).Key;
                        var well = mps2.First(kv => kv.Value == pl).Key;
                        DU.DrawBoundaryLazy(line);
                        DU.DrawBoundaryLazy(well);
                        dict[line] = well;
                    }
                }

            }
        }
        public static void spacial_test()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();

                var lines = sv.VerticalPipeLines;
                ThRainSystemService.GroupLinesBySpatialIndex(lines);
            }
        }



        public static void spacial_test2()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.FindShortConverters();
                var lines = sv.VerticalPipeLines;
                var pipes = sv.VerticalPipes;
                var shortConverters = sv.AllShortConverters.ToList();
                var dbTxtToHLineDict = new Dictionary<Entity, Entity>();
                var linesGroup = new List<List<Entity>>();
                var groups = new List<List<Entity>>();
                var plDict = new Dictionary<Entity, Polyline>();
                var lineToPipesDict = new ListDict<Entity>();
                var pls1 = new List<Polyline>();
                var pls2 = new List<Polyline>();
                foreach (var e in sv.VerticalPipes)
                {
                    var bd = sv.BoundaryDict[e];
                    var pl = PolylineTools.CreatePolygon(bd.Center, 4, bd.Radius);
                    pls1.Add(pl);
                    plDict[e] = pl;
                }
                foreach (var e in sv.VerticalPipeLines)
                {
                    var pl = (e as Line).Buffer(10);
                    pls2.Add(pl);
                    plDict[e] = pl;
                }
                var si = ThRainSystemService.BuildSpatialIndex(pls1);
                foreach (var pl2 in pls2)
                {
                    foreach (var pl1 in si.SelectCrossingPolygon(pl2).Cast<Polyline>().ToList())
                    {
                        var pipe = sv.VerticalPipes[pls1.IndexOf(pl1)];
                        var line = sv.VerticalPipeLines[pls2.IndexOf(pl2)];
                        lineToPipesDict.Add(line, pipe);
                    }
                }
                lineToPipesDict.ForEach((line, pipes) =>
                {
                    DU.DrawBoundaryLazy(line);
                    DU.DrawBoundaryLazy(pipes.ToArray());
                });
                if (false)
                {
                    foreach (var e1 in sv.VerticalPipeLines)
                    {
                        foreach (var e2 in sv.VerticalPipes)
                        {
                            if (new ThCADCore.NTS.ThCADCoreNTSRelate(plDict[e1], plDict[e2]).IsIntersects)
                            {
                                lineToPipesDict.Add(e1, e2);
                                //DU.DrawBoundaryLazy(e2);
                                //DU.DrawRectLazy(sv.BoundaryDict[e2]);
                            }
                        }
                    }
                }
            }
        }
        //public static void ThRainSystemServiceTest4_OK()
        //{
        //    Dbg.FocusMainWindow();
        //    using (var adb = AcadDatabase.Active())
        //    using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
        //    using (var tr = DrawUtils.DrawingTransaction)
        //    {
        //        var db = adb.Database;
        //        Dbg.BuildAndSetCurrentLayer(db);
        //        var sv = new ThRainSystemService() { adb = adb };
        //        sv.InitCache();
        //        sv.FindShortConverters();
        //        var lines = sv.VerticalPipeLines;
        //        var pipes = sv.VerticalPipes;
        //        var shortConverters = sv.AllShortConverters.ToList();
        //        var dbTxtToHLineDict = new Dictionary<Entity, Entity>();
        //        var linesGroup = new List<List<Entity>>();
        //        var groups = new List<List<Entity>>();
        //        var plDict = new Dictionary<Entity, Polyline>();
        //        var lineToPipesDict = new ListDict<Entity>();
        //        foreach (var e1 in sv.VerticalPipeDBTexts)
        //        {
        //            foreach (var e2 in sv.VerticalPipeLines)
        //            {
        //                if (e2 is Line line)
        //                {
        //                    var seg = line.ToGLineSegment();
        //                    if (seg.IsHorizontal(10))
        //                    {
        //                        var c1 = sv.BoundaryDict[e1].Center;
        //                        var c2 = sv.BoundaryDict[e2].Center;
        //                        if (c1.Y > c2.Y && GeoAlgorithm.Distance(c1, c2) < 150)
        //                        {
        //                            dbTxtToHLineDict[e1] = e2;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        {
        //            var pairs = new List<KeyValuePair<int, int>>();
        //            //for (int i = 0; i < lines.Count; i++)
        //            //{
        //            //    for (int j = i + 1; j < lines.Count; j++)
        //            //    {
        //            //        var line1 = lines[i] as Line;
        //            //        var line2 = lines[j] as Line;
        //            //        if (line1 != null && line2 != null)
        //            //        {
        //            //            var pline1 = line1.Buffer(10);
        //            //            var pline2 = line2.Buffer(10);
        //            //            if (new ThCADCore.NTS.ThCADCoreNTSRelate(pline1, pline2).IsIntersects)
        //            //            {
        //            //                pairs.Add(new KeyValuePair<int, int>(i, j));
        //            //            }
        //            //        }
        //            //    }
        //            //}
        //            var bfs = lines.Select(e => (e as Line)?.Buffer(10)).ToList();
        //            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
        //            for (int i = 0; i < bfs.Count; i++)
        //            {
        //                Polyline bf = bfs[i];
        //                if (bf != null)
        //                {
        //                    var lst = si.SelectCrossingPolygon(bf).Cast<Polyline>().Select(e => bfs.IndexOf(e)).Where(j => i < j).ToList();
        //                    lst.ForEach(j => pairs.Add(new KeyValuePair<int, int>(i, j)));
        //                }
        //            }

        //            var dict = new ListDict<int>();
        //            var h = new BFSHelper()
        //            {
        //                Pairs = pairs.ToArray(),
        //                TotalCount = lines.Count,
        //                Callback = (g, i) =>
        //                {
        //                    dict.Add(g.root, i);
        //                },
        //            };
        //            h.BFS();
        //            dict.ForEach((_i, l) =>
        //            {
        //                linesGroup.Add(l.Select(i => lines[i]).ToList());
        //            });
        //        }
        //        foreach (var g in linesGroup)
        //        {
        //            //DU.DrawBoundaryLazy(g.ToArray());
        //            //if (GeoAlgorithm.GetBoundaryRect(g.ToArray()).ToJson() == "[-1556.47474123369,398855.075333649,852.092909549596,401109.883025958]")
        //            if (Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(g.ToArray()).MinX) == -1556)
        //            {
        //                foreach (var e in g)
        //                {
        //                    //DU.DrawBoundaryLazy(e);
        //                    //Dbg.ShowWhere(e);
        //                    if (e is Line line)
        //                    {
        //                        //var l=DU.DrawLineLazy(line.StartPoint, line.EndPoint);
        //                        //l.ColorIndex = 5;
        //                    }
        //                }
        //            }
        //        }
        //        //return;
        //        {
        //            var pls1 = new List<Polyline>();
        //            var pls2 = new List<Polyline>();
        //            foreach (var e in sv.VerticalPipes)
        //            {
        //                var bd = sv.BoundaryDict[e];
        //                var pl = PolylineTools.CreatePolygon(bd.Center, 4, bd.Radius);
        //                plDict[e] = pl;
        //                pls1.Add(pl);
        //            }
        //            foreach (var e in sv.VerticalPipeLines)
        //            {
        //                var pl = (e as Line).Buffer(10);
        //                plDict[e] = pl;
        //                pls2.Add(pl);
        //            }
        //            //foreach (var e1 in sv.VerticalPipeLines)
        //            //{
        //            //    foreach (var e2 in sv.VerticalPipes)
        //            //    {
        //            //        if (new ThCADCore.NTS.ThCADCoreNTSRelate(plDict[e1], plDict[e2]).IsIntersects)
        //            //        {
        //            //            lineToPipesDict.Add(e1, e2);
        //            //            //DU.DrawBoundaryLazy(e2);
        //            //            //DU.DrawRectLazy(sv.BoundaryDict[e2]);
        //            //        }
        //            //    }
        //            //}
        //            var si = ThRainSystemService.BuildSpatialIndex(pls1);
        //            foreach (var pl2 in pls2)
        //            {
        //                foreach (var pl1 in si.SelectCrossingPolygon(pl2).Cast<Polyline>().ToList())
        //                {
        //                    var pipe = sv.VerticalPipes[pls1.IndexOf(pl1)];
        //                    var line = sv.VerticalPipeLines[pls2.IndexOf(pl2)];
        //                    lineToPipesDict.Add(line, pipe);
        //                }
        //            }
        //        }
        //        //return;
        //        {
        //            var totalList = new List<Entity>();
        //            totalList.AddRange(sv.VerticalPipeDBTexts);
        //            totalList.AddRange(sv.VerticalPipes);
        //            totalList.AddRange(sv.VerticalPipeLines);
        //            var pairs = new List<KeyValuePair<Entity, Entity>>();
        //            foreach (var kv in dbTxtToHLineDict) pairs.Add(kv);
        //            //foreach (var kv in dbTxtToHLineDict)
        //            //{
        //            //    DU.DrawBoundaryLazy(new Entity[] { kv.Key, kv.Value });
        //            //}
        //            lineToPipesDict.ForEach((e, l) => { l.ForEach(o => pairs.Add(new KeyValuePair<Entity, Entity>(e, o))); });
        //            //lineToPipesDict.ForEach((e, l) =>
        //            //{
        //            //    DU.DrawBoundaryLazy(e);
        //            //    DU.DrawBoundaryLazy(l.ToArray());
        //            //});
        //            //return;
        //            foreach (var g in linesGroup) for (int i = 1; i < g.Count; i++) pairs.Add(new KeyValuePair<Entity, Entity>(g[i - 1], g[i]));
        //            var dict = new ListDict<Entity>();
        //            var h = new BFSHelper2<Entity>()
        //            {
        //                Pairs = pairs.ToArray(),
        //                Items = totalList.ToArray(),
        //                Callback = (g, i) =>
        //                {
        //                    dict.Add(g.root, i);
        //                },
        //            };
        //            h.BFS();
        //            dict.ForEach((_start, ents) =>
        //            {
        //                groups.Add(ents);
        //            });
        //            foreach (var g in groups)
        //            {
        //                //DU.DrawBoundaryLazy(g.ToArray());
        //                sv.SortBy2DSpacePosition(
        //                    g.Where(e => sv.VerticalPipes.Contains(e)).ToList(),
        //                    g.Where(e => sv.VerticalPipeDBTexts.Contains(e)).ToList(),
        //                    out List<Entity> targetPipes,
        //                    out List<Entity> targetTexts);
        //                if (targetPipes.Count == targetTexts.Count && targetTexts.Count > 0)
        //                {
        //                    //Dbg.PrintLine(targetTexts.Select(o => (o as DBText)?.TextString).ToJson());


        //                    for (int i = 0; i < targetPipes.Count; i++)
        //                    {
        //                        var pipe = targetPipes[i];
        //                        var dbT = targetTexts[i] as DBText;
        //                        var t = dbT.TextString;
        //                        Dbg.PrintLine(t);
        //                        Dbg.PrintLine("has short cvt:" + shortConverters.Contains(pipe));
        //                        if (t == "Y1L1-2")
        //                        {
        //                            //Dbg.PrintLine(scvts.Contains(pipe));
        //                            Dbg.ShowWhere(pipe);
        //                        }
        //                    }
        //                }
        //            }

        //        }

        //    }
        //    Dbg.FocusMainWindow();

        //}
        public static void SelectCrossingPolygonDemo(Point3dCollection pts, List<Entity> rst)
        {
            if (pts.Count >= 3)
            {
                var spacialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(rst.ToCollection());
                rst = spacialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
            }
        }
        public static void DisplayGroup()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                foreach (var lst in groups)
                {
                    if (lst.Contains(e))
                    {
                        //DU.DrawBoundaryLazy(lst.ToArray(), 2);
                        foreach (var o in lst)
                        {
                            DU.DrawBoundary(db, o, 2);
                        }
                    }
                }
            }
        }
        static List<List<Entity>> groups;

        public static void ShowLineLength()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var db = adb.Database;
                var line = Dbg.SelectEntity<Line>(adb);
                MessageBox.Show(line.Length.ToString());
            }
        }
        public static void ShowDegreeAngle()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var db = adb.Database;
                var e1 = Dbg.SelectEntity<Entity>(adb);
                GeoAlgorithm.TryConvertToLineSegment(e1, out ThWGLineSegment seg1);
                Dbg.PrintLine(GeoAlgorithm.AngleToDegree((seg1.Point2 - seg1.Point1).Angle).ToString());
            }
        }

        public static void ShowAllWaterBuckets()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                foreach (var t in adb.ModelSpace.OfType<DBText>().Where(t => t.TextString.Contains("雨水斗")))
                {
                    var bd = GeoAlgorithm.GetBoundaryRect(t);
                    Dbg.ShowWhere(bd);
                }
            }
        }

        public static void DrawBoundaryTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                DrawUtils.DrawBoundary(db, e, 2);
            }
        }
        public static void DrawBoundaryTest2()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                DrawUtils.DrawBoundaryLazy(e);
            }
        }
        public static void DrawBoundaryTest3()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                DrawUtils.DrawBoundaryLazy(e, 100);
            }
        }
        public static void DrawBoundaryTest4()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                var r = GeoAlgorithm.GetBoundaryRect(e);
                DrawUtils.DrawRectLazy(r);
            }
        }

        public static void DrawBoundaryTest5()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                var r = GeoAlgorithm.GetBoundaryRect(e);
                var rect = "[-334718.142328821,1366616.99129695,635160.253054206,1868196.71202574]".JsonToThWGRect();
                const double delta = 1000;
                for (int i = 0; i < 1000; i++)
                {
                    var _delta = delta * i;
                    if (_delta > rect.Width / 2 && _delta > rect.Height / 2) break;
                    var _r = new ThWGRect(r.MinX - _delta, r.MaxY + _delta, r.MaxX + _delta, r.MinY - _delta);
                    DrawUtils.DrawRectLazy(_r);
                }
            }
        }
        public static void FindY1L1_3()
        {
            Dbg.ShowWhere("Y1L1-3");
        }
        public static void FindAllY1L1_3()
        {
            Dbg.ShowAll("Y1L1-3");
        }
        public static void FindAllWb3()
        {
            Dbg.ShowAll("Wb3", 10000 * 5);
        }
        public static void FindAllWb3WrappingPipes2()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-BUSH").ForEach(e => Dbg.ShowWhere(e));
            }
        }
        public static void FindAllFL1_2()
        {
            Dbg.ShowAll("FL1-2", 10000 * 5);
        }
        public static void GetBoundaryData()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                var r = GeoAlgorithm.GetBoundaryRect(e);
                Dbg.ShowString(r.ToJson());
            }
        }
        public static void DrawLinesDemo()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var rect = Dbg.SelectGRect();
                var x1 = rect.MinX;
                var x2 = rect.MaxX;
                var y1 = rect.MaxY;
                var y2 = rect.MinY;
                DebugTool.DrawRect(db, rect.LeftTop, rect.RightButtom);
                var delta = (y2 - y1) / 10;
                for (int i = 0; i < 10; i++)
                {
                    var y = y1 + delta * i;
                    ThDebugDrawer.DrawLine(db, new Point2d(x1, y), new Point2d(x2, y));
                }
                {
                    var x3 = x1 + 5000;
                    ThDebugDrawer.DrawLine(db, new Point2d(x3, y1), new Point2d(x3, y2));
                }
                {
                    var x3 = x1 + 5000;
                    var lenY = Math.Abs(y2 - y1);
                    var lenX = lenY;
                    x3 += 5000;
                    ThDebugDrawer.DrawLine(db, new Point2d(x3, y1), new Point2d(x3 - lenX, y1 + lenY));
                    x3 += 5000;
                    ThDebugDrawer.DrawLine(db, new Point2d(x3, y1), new Point2d(x3 - lenX, y1 - lenY));
                    x3 += 5000;
                    ThDebugDrawer.DrawLine(db, new Point2d(x3, y1), new Point2d(x3 + lenX, y1 + lenY));
                    x3 += 5000;
                    ThDebugDrawer.DrawLine(db, new Point2d(x3, y1), new Point2d(x3 + lenX, y1 - lenY));
                    x3 += 10000;
                    ThDebugDrawer.DrawLineByOffset(db, new Point2d(x3, y1), lenX, lenY);
                    x3 += 5000;
                    ThDebugDrawer.DrawLineByPolar(db, new Point2d(x3, y1), 5000, GeoAlgorithm.AngleFromDegree(45));
                }
            }
        }
        public static void GetAllBlockReferenceEffectiveNames()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.SetText(adb.ModelSpace.OfType<BlockReference>().Select(x => x.GetEffectiveName()));
            }
        }
        public static void GetAllBlockNames()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.SetText(adb.Blocks.Select(x => x.Name));
            }
        }
        public static void GetAllDBTexts()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.SetText(adb.ModelSpace.OfType<DBText>().Select(x => x.TextString));
            }
        }
        public static void GetAllLayers()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.SetText(adb.Layers.Select(o => o.Name));
            }
        }
    }
}


















namespace ThMEPWSS.DebugNs
{
    using System;
    using NFox.Cad;
    using DotNetARX;
    using Linq2Acad;
    using System.Linq;
    using ThCADExtension;
    using Dreambuild.AutoCAD;
    using System.Collections.Generic;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Index.Strtree;
    using NetTopologySuite.Geometries.Prepared;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.DatabaseServices;
    using ThCADCore.NTS;

    /// <summary>
    /// 空间索引DB图元（去除空间重合的DB图元）
    /// </summary>
    public class NTSSpatialIndex1 : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        public Dictionary<Geometry, DBObject> Geometries { get; set; }
        public NTSSpatialIndex1(DBObjectCollection objs)
        {
            Geometries = new Dictionary<Geometry, DBObject>();
            Update(objs, new DBObjectCollection());
        }

        public void Dispose()
        {
            //
        }

        private DBObjectCollection CrossingFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Intersects(preparedGeometry, o)).ToCollection();
        }

        private DBObjectCollection FenceFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Intersects(preparedGeometry, o)).ToCollection();
        }

        private DBObjectCollection WindowFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Contains(preparedGeometry, o)).ToCollection();
        }

        private bool Contains(IPreparedGeometry preparedGeometry, Entity entity)
        {
            return preparedGeometry.Contains(ToNTSGeometry(entity));
        }

        private bool Intersects(IPreparedGeometry preparedGeometry, Entity entity)
        {
            return preparedGeometry.Intersects(ToNTSGeometry(entity));
        }

        private Geometry ToNTSGeometry(DBObject obj)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                if (obj is Line line)
                {
                    return line.ToNTSLineString();
                }
                else if (obj is Polyline polyline)
                {
                    return polyline.ToNTSLineString();
                }
                else if (obj is Arc arc)
                {
                    return arc.ToNTSGeometry();
                }
                else if (obj is Circle circle)
                {
                    //return circle.ToNTSGeometry();

                    var length = ThCADCoreNTSService.Instance.ArcTessellationLength;
                    var circum = 2 * Math.PI * circle.Radius;
                    int num = (int)Math.Ceiling(circum / length);
                    return circle.ToNTSPolygon(num < 6 ? 6 : num);

                    //if (num >= 3)
                    //{
                    //    return circle.ToNTSPolygon(num);
                    //}
                    //else
                    //{
                    //    return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                    //}
                }
                else if (obj is MPolygon mPolygon)
                {
                    return mPolygon.ToNTSPolygon();
                }
                else if (obj is Entity entity)
                {
                    try
                    {
                        return entity.GeometricExtents.ToNTSPolygon();
                    }
                    catch
                    {
                        // 若异常抛出，则返回一个“空”的Polygon
                        return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private Polygon ToNTSPolygon(DBObject obj)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                if (obj is Polyline poly)
                {
                    return poly.ToNTSPolygon();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// 更新索引
        /// </summary>
        /// <param name="adds"></param>
        /// <param name="removals"></param>
        public void Update(DBObjectCollection adds, DBObjectCollection removals)
        {
            // 添加新的对象
            adds.Cast<DBObject>().ForEachDbObject(o =>
            {
                var geometry = ToNTSGeometry(o);
                if (!Geometries.Keys.Contains(geometry))
                {
                    Geometries.Add(geometry, o);
                }
            });
            // 移除删除对象
            Geometries.RemoveAll((k, v) => removals.Contains(v));

            // 创建新的索引
            Engine = new STRtree<Geometry>();
            Geometries.Keys.ForEach(g => Engine.Insert(g.EnvelopeInternal, g));
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingPolygon(Polyline polyline)
        {
            var geometry = ToNTSPolygon(polyline);
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public DBObjectCollection SelectCrossingPolygon(MPolygon mPolygon)
        {
            /*
             * 线获取MPolygon外圈内所有的物体
             * 减去洞内包括的物体
             */
            var loops = mPolygon.Loops();
            var objs = SelectCrossingPolygon(loops[0]);
            for (int i = 1; i < loops.Count; i++)
            {
                foreach (DBObject innerObj in SelectWindowPolygon(loops[i]))
                {
                    objs.Remove(innerObj);
                }
            }
            return objs;
        }

        public DBObjectCollection SelectCrossingPolygon(Point3dCollection polygon)
        {
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(polygon);
            return SelectCrossingPolygon(pline);
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingWindow(Point3d pt1, Point3d pt2)
        {
            var extents = new Extents3d(pt1, pt2);
            return SelectCrossingPolygon(extents.ToRectangle());
        }

        /// <summary>
        /// Window selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectWindowPolygon(Polyline polyline)
        {
            var geometry = ToNTSPolygon(polyline);
            return WindowFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectFence(Polyline polyline)
        {
            var geometry = ToNTSGeometry(polyline);
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public DBObjectCollection SelectFence(Line line)
        {
            var geometry = ToNTSGeometry(line);
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public DBObjectCollection SelectAll()
        {
            var objs = new DBObjectCollection();
            foreach (var item in Geometries.Values)
            {
                objs.Add(item);
            }
            return objs;
        }

        public void AddTag(DBObject obj, object tag)
        {
            if (Geometries.ContainsValue(obj))
            {
                Geometries.Where(o => o.Value == obj).First().Key.UserData = tag;
            }
        }

        public object Tag(DBObject obj)
        {
            if (!Geometries.ContainsValue(obj))
            {
                return null;
            }
            return Geometries.Where(o => o.Value == obj).First().Key.UserData;
        }

        private DBObjectCollection Query(Envelope envelope)
        {
            var objs = new DBObjectCollection();
            foreach (var geometry in Engine.Query(envelope))
            {
                if (Geometries.ContainsKey(geometry))
                {
                    objs.Add(Geometries[geometry]);
                }
            }
            return objs;
        }

        /// <summary>
        /// 最近的几个邻居
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public DBObjectCollection NearestNeighbours(Curve curve, int num)
        {
            var geometry = ToNTSGeometry(curve);
            var neighbours = Engine.NearestNeighbour(
                geometry.EnvelopeInternal,
                geometry,
                new GeometryItemDistance(),
                num)
                .Where(o => !o.EqualsExact(geometry));
            var objs = new DBObjectCollection();
            foreach (var neighbour in neighbours)
            {
                objs.Add(Geometries[neighbour]);
            }
            return objs;
        }
    }
}





//#endif



