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
        public static void JsonTest()
        {
            const string KEY = "qrjq0w";
            var diagram = load<ThWRainSystemDiagram>(KEY);
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
                foreach (var e in adb.ModelSpace.OfType<DBText>().Where(e => e.TextString == "Y1L1-3a").ToList())
                {
                    Dbg.ShowWhere(e);
                }
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
        public static void DeleteTestGeometries()
        {
            Dbg.DeleteTestGeometries();
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
        public static void xx()
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
        public static void xxxxx()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var pt = Dbg.SelectPoint();

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
        public static void DrawCondensePipe()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var pt = Dbg.SelectPoint();
                DU.DrawCircleLazy(pt, 30);
                DU.DrawCircleLazy(pt.OffsetX(500), 30);
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
        public static void NewAndShowLogWindow()
        {
            Dbg.NewLogWindow();
            Dbg.ShowCurrentLogWindow();
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
                var rt = sv.GetOutputType(null, id);
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
                sv.CollectVerticalPipeData2();
                sv.FindShortConverters();
                sv.LabelWRainLinesAndPipes();
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

        public static void RunThRainSystemDiagramCmd_NoRectSelection2()
        {
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
                var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();
                NewMethod(adb, pt1, pt2);
            }
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
        public static void xxxxxxxxxxx()
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

                const string KEY = "qrjq0w";

                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;
                //var basePt = default(Point3d);

                var diagram = new ThWRainSystemDiagram();
                var storeysRecEngine = new ThWStoreysRecognitionEngine();
                storeysRecEngine.Recognize(adb.Database, points);
                //var sw = new Stopwatch();
                //sw.Start();
                diagram.CollectData(adb, points);
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

                save(KEY, diagram);
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
                const string KEY = "qrjq0w";

                var diagram = load<ThWRainSystemDiagram>(KEY);
                diagram.Init();
                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;
                diagram.Draw(basePt);

                DrLazy.Default.DrawLazy();
                DrawUtils.Draw();
                Dbg.FocusMainWindow();
            }

        }


        static void save(string name, object obj)
        {
            File.WriteAllText($@"D:\temptxts\{name}.txt", obj.ToJson());
        }
        static T load<T>(string name)
        {
            return File.ReadAllText($@"D:\temptxts\{name}.txt").FromJson<T>();
        }
        private static void NewMethod(AcadDatabase adb, Point3d pt1, Point3d pt2)
        {
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
            var storeysRecEngine = new ThWStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, points);
            //var sw = new Stopwatch();
            //sw.Start();
            diagram.CollectData(adb, points);
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
            FengDbgTest.processContext["xx"] = 123;
        }
        public static void demo2()
        {
            FengDbgTest.processContext["xx"] = ((int)FengDbgTest.processContext["xx"]) + 1;
        }
        public static void demo3()
        {
            MessageBox.Show(FengDbgTest.processContext["xx"].ToJson());
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
                        if (GeoAlgorithm.IsLineConnected(kv1.Value, kv2.Value, 5) || (kv1.Value.IsHorizontalOrVertical(5) && kv2.Value.IsHorizontalOrVertical(5) && GeoAlgorithm.IsOnSameLine(kv1.Value, kv2.Value, 5) && GeoAlgorithm.GetMinConnectionDistance(kv1.Value, kv2.Value) < 8000))
                        {
                            //Dbg.PrintLine($"{i} {j}");
                            //if (ent == kv1.Key)
                            //{
                            //    Dbg.PrintLine($"xxx {i} {j}");
                            //}

                            //if (j == 686)
                            //{
                            //    DU.DrawCircleLazy(GeoAlgorithm.GetBoundaryRect(kv2.Key));
                            //}
                            //if (i == 153 && j == 686)
                            //{
                            //    DU.DrawCircleLazy(GeoAlgorithm.GetBoundaryRect(kv1.Key));
                            //    //Dbg.ShowWhere(kv1.Key);
                            //}
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
        public static void xxx()
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
                ThRainSystemService.GroupLines(lines, groups);
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
        public static void xx()
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
                        //DU.DrawBoundaryLazy(line, well);
                        DU.DrawBoundaryLazy(line);
                        DU.DrawBoundaryLazy(well);
                        //Dbg.PrintLine("xx");
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
        public static void ThRainSystemServiceTest4_OK()
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
                foreach (var e1 in sv.VerticalPipeDBTexts)
                {
                    foreach (var e2 in sv.VerticalPipeLines)
                    {
                        if (e2 is Line line)
                        {
                            var seg = line.ToGLineSegment();
                            if (seg.IsHorizontal(10))
                            {
                                var c1 = sv.BoundaryDict[e1].Center;
                                var c2 = sv.BoundaryDict[e2].Center;
                                if (c1.Y > c2.Y && GeoAlgorithm.Distance(c1, c2) < 150)
                                {
                                    dbTxtToHLineDict[e1] = e2;
                                    break;
                                }
                            }
                        }
                    }
                }
                {
                    var pairs = new List<KeyValuePair<int, int>>();
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
                foreach (var g in linesGroup)
                {
                    //DU.DrawBoundaryLazy(g.ToArray());
                    //if (GeoAlgorithm.GetBoundaryRect(g.ToArray()).ToJson() == "[-1556.47474123369,398855.075333649,852.092909549596,401109.883025958]")
                    if (Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(g.ToArray()).MinX) == -1556)
                    {
                        foreach (var e in g)
                        {
                            //DU.DrawBoundaryLazy(e);
                            //Dbg.ShowWhere(e);
                            if (e is Line line)
                            {
                                //var l=DU.DrawLineLazy(line.StartPoint, line.EndPoint);
                                //l.ColorIndex = 5;
                            }
                        }
                    }
                }
                //return;
                {
                    var pls1 = new List<Polyline>();
                    var pls2 = new List<Polyline>();
                    foreach (var e in sv.VerticalPipes)
                    {
                        var bd = sv.BoundaryDict[e];
                        var pl = PolylineTools.CreatePolygon(bd.Center, 4, bd.Radius);
                        plDict[e] = pl;
                        pls1.Add(pl);
                    }
                    foreach (var e in sv.VerticalPipeLines)
                    {
                        var pl = (e as Line).Buffer(10);
                        plDict[e] = pl;
                        pls2.Add(pl);
                    }
                    //foreach (var e1 in sv.VerticalPipeLines)
                    //{
                    //    foreach (var e2 in sv.VerticalPipes)
                    //    {
                    //        if (new ThCADCore.NTS.ThCADCoreNTSRelate(plDict[e1], plDict[e2]).IsIntersects)
                    //        {
                    //            lineToPipesDict.Add(e1, e2);
                    //            //DU.DrawBoundaryLazy(e2);
                    //            //DU.DrawRectLazy(sv.BoundaryDict[e2]);
                    //        }
                    //    }
                    //}
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
                }
                //return;
                {
                    var totalList = new List<Entity>();
                    totalList.AddRange(sv.VerticalPipeDBTexts);
                    totalList.AddRange(sv.VerticalPipes);
                    totalList.AddRange(sv.VerticalPipeLines);
                    var pairs = new List<KeyValuePair<Entity, Entity>>();
                    foreach (var kv in dbTxtToHLineDict) pairs.Add(kv);
                    //foreach (var kv in dbTxtToHLineDict)
                    //{
                    //    DU.DrawBoundaryLazy(new Entity[] { kv.Key, kv.Value });
                    //}
                    lineToPipesDict.ForEach((e, l) => { l.ForEach(o => pairs.Add(new KeyValuePair<Entity, Entity>(e, o))); });
                    //lineToPipesDict.ForEach((e, l) =>
                    //{
                    //    DU.DrawBoundaryLazy(e);
                    //    DU.DrawBoundaryLazy(l.ToArray());
                    //});
                    //return;
                    foreach (var g in linesGroup) for (int i = 1; i < g.Count; i++) pairs.Add(new KeyValuePair<Entity, Entity>(g[i - 1], g[i]));
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
                    foreach (var g in groups)
                    {
                        //DU.DrawBoundaryLazy(g.ToArray());
                        sv.SortBy2DSpacePosition(
                            g.Where(e => sv.VerticalPipes.Contains(e)).ToList(),
                            g.Where(e => sv.VerticalPipeDBTexts.Contains(e)).ToList(),
                            out List<Entity> targetPipes,
                            out List<Entity> targetTexts);
                        if (targetPipes.Count == targetTexts.Count && targetTexts.Count > 0)
                        {
                            //Dbg.PrintLine(targetTexts.Select(o => (o as DBText)?.TextString).ToJson());


                            for (int i = 0; i < targetPipes.Count; i++)
                            {
                                var pipe = targetPipes[i];
                                var dbT = targetTexts[i] as DBText;
                                var t = dbT.TextString;
                                Dbg.PrintLine(t);
                                Dbg.PrintLine("has short cvt:" + shortConverters.Contains(pipe));
                                if (t == "Y1L1-2")
                                {
                                    //Dbg.PrintLine(scvts.Contains(pipe));
                                    Dbg.ShowWhere(pipe);
                                }
                            }
                        }
                    }

                }

            }
            Dbg.FocusMainWindow();

        }
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

//#endif