//this file is for debugging only by Feng

//#if DEBUG

using System;
using System.Text;

#pragma warning disable
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
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using System.Linq.Expressions;

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
        public class qt8czw
        {
            public static void InitButtons()
            {
                qt8f54(); qt8ddf();
            }
            public static void AddButton(string name, Action f)
            {
                ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], name, () =>
                {
                    try
                    {
                        f?.Invoke();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show((ex.InnerException ?? ex).Message);
                    }
                });
            }
            private static void qt8ddf()
            {
                var targetType = typeof(Util1);
                ((Action<Assembly, string>)ctx["pushAcadActions"])((Assembly)ctx["currentAsm"], targetType.FullName);
                var fs = ((List<Action>)ctx["actions"]).ToList();
                var names = ((List<string>)ctx["names"]).ToList();
                ((Action<Assembly>)ctx["clearAcadActions"])((Assembly)ctx["currentAsm"]);
                for (int i = 0; i < fs.Count; i++)
                {
                    var f = fs[i];
                    var name = names[i];
                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], name, f);
                }
            }
            private static void qt8f54()
            {
                AddButtons3(typeof(Sankaku2));
                AddButtons3(typeof(Sankaku1));

                AddButtons2(typeof(DrainageTest));
                AddButtons2(typeof(Sankaku));

                  
                if (false)
                {
                    AddButtons2(typeof(quj50y));
                    AddButtons2(typeof(quin3c));
                    AddButtons2(typeof(FengDbgTesting));
                }
            }

            private static void AddButtons3(Type t)
            {
                if (t.GetCustomAttribute<FengAttribute>() != null)
                {
                    AddButtons2(t);
                }
            }

            public static void AddButtons2(Type targetType)
            {
                var attrType = ((Assembly)ctx["currentAsm"]).GetType(typeof(FengAttribute).FullName);
                foreach (var mi in ((Assembly)ctx["currentAsm"]).GetType(targetType.FullName).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var attr = mi.GetCustomAttribute(attrType);
                    if (attr == null) continue;
                    var name = (string)attrType.GetField("Title").GetValue(attr);
                    if (string.IsNullOrEmpty(name)) name = mi.Name;
                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], name, () =>
                    {
                        try
                        {
                            mi.Invoke(null, null);
                        }
                        catch (System.Exception ex)
                        {
                            MessageBox.Show((ex.InnerException ?? ex).Message);
                        }
                    });
                }
            }
            public static void AddButtons1(Type targetType)
            {
                foreach (var mi in ((Assembly)ctx["currentAsm"]).GetType(targetType.FullName).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], mi.Name, () =>
                    {
                        try
                        {
                            mi.Invoke(null, null);
                        }
                        catch (System.Exception ex)
                        {
                            MessageBox.Show((ex.InnerException ?? ex).Message);
                        }
                    });
                }
            }
        }
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
                    var fs = ((List<Action>)ctx["actions"]).ToList();
                    var names = ((List<string>)ctx["names"]).ToList();
                    ((Action<Assembly>)ctx["clearAcadActions"])((Assembly)ctx["currentAsm"]);
                    ((Action<object>)ctx["clearBtns"])(ctx["currentPanel"]);
                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], "initMethod", initMethod);
                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], "reloadMe", () =>
                    {
                        var asm = ((Func<string, Assembly>)ctx["loadAsm"])((string)ctx["asmDllFullPath"]);
                        asm.GetType(typeof(FengDbgTest).FullName).GetField(nameof(processContext)).SetValue(null, processContext);
                        initMethod();
                    });

                    {
                        qt8czw.InitButtons();
                    }
                    {
                        var _names = File.ReadLines(@"E:\xx.txt")
             .Select(x => x.Trim())
             .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("//"))
             .ToList();
                        foreach (var _name in _names)
                        {
                            var name = _name;
                            string __name = null;
                            if (name.Contains(" "))
                            {
                                var j = name.IndexOf(" ");
                                var tmp = name;
                                name = tmp.Substring(0, j);
                                __name = tmp.Substring(j).Trim();
                                if (string.IsNullOrWhiteSpace(__name)) __name = null;
                            }
                            var i = names.IndexOf(name);
                            if (i >= 0)
                            {
                                var f = fs[i];
                                var name_ = names[i];
                                if (__name != "xx")
                                {
                                    ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], __name ?? name, f);
                                }
                            }
                        }
                    }

                    if (false)
                    {
                        for (int i = 0; i < fs.Count; i++)
                        {
                            var f = fs[i];
                            var name = names[i];
                            ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], name, f);
                        }
                    }
                });
                ctx["initMethod"] = initMethod;
                entryMethod();
            }
            else
            {
                MessageBox.Show("entryMethod not set!");
            }
            return;
            Origin(ctx);
        }


        private static void Origin(Dictionary<string, object> ctx)
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
        public static T LoadData<T>(string name, JsonConverter cvt)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText($@"D:\temptxts\{name}.txt"), cvt);
        }
    }
    public class ThDebugTool
    {

        public static GeometryFactory GeometryFactory => ThCADCoreNTSService.Instance.GeometryFactory;
        public static DocumentLock DocumentLock => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
        public static Editor Editor => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        public static Document MdiActiveDocument => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        public static Dictionary<string, object> ctx => ThDebugClass.ctx;
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
        public static bool __showXLabel => false;
        //public static bool __showXLabel => true;
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
        public class TextBoxWriter : System.IO.TextWriter
        {
            //重载string那个方法没用的
            StringBuilder sb = new StringBuilder(8192);
            bool r;
            public override void Write(char value)
            {
                //Dbg.PrintLine(value.ToJson());
                if (value == '\r')
                {
                    r = true;
                    return;
                }
                if (value == '\n')
                {
                    if (r)
                    {
                        var s = sb.ToString();
                        if (s.Length > 100 || s.Contains('\n'))
                        {
                            Dbg.PrintText(s);
                        }
                        else
                        {
                            Dbg.PrintLine(s);
                        }
                        r = false;
                        sb.Clear();
                        return;
                    }
                    else
                    {
                        sb.Append(value);
                        return;
                    }
                }
                r = false;
                sb.Append(value);
            }
            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        }
        static ThDebugTool()
        {
            Console.SetOut(new TextBoxWriter());
        }
        const double DEFAULT_WIDTH = 100;
        public static void ShowLine(Line line, double width = DEFAULT_WIDTH)
        {
            var pl = DU.DrawPolyLineLazy(line.StartPoint, line.EndPoint);
            pl.ConstantWidth = width;
        }
        public static void ShowLine(Polyline pline, double width = DEFAULT_WIDTH)
        {
            var pl = DU.DrawPolyLineLazy(pline.ToPoint3dCollection().Cast<Point3d>().ToArray());
            pl.ConstantWidth = width;
        }
        public static void ShowLine(Entity ent, double width = DEFAULT_WIDTH)
        {
            if (ThRainSystemService.IsTianZhengElement(ent))
            {
                foreach (var e in ent.ExplodeToDBObjectCollection().OfType<Entity>())
                {
                    ShowLine(e, width);
                }
            }
            else if (ent is Line line)
            {
                ShowLine(line, width);
            }
            else if (ent is Polyline pl)
            {
                ShowLine(pl, width);
            }
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
        public static void ShowWhere(Point2d pt, double delta = DEFAULT_DELTA)
        {
            ShowWhere(pt.ToPoint3d(), delta);
        }
        public static void ShowWhere(Point3d pt, double delta = DEFAULT_DELTA)
        {
            ShowWhere(new GRect(pt.ToPoint2d(), pt.ToPoint2d()), delta);
        }
        public static void ShowXLabel(Point2d pt, double size = 500)
        {
            ShowXLabel(pt.ToPoint3d(), size);
        }
        public static void ShowXLabel(Point3d pt, double size = 500)
        {
            DrawUtils.DrawingQueue.Enqueue(adb =>
            {
                var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                var r = new GRect(pt.X - size / 2, pt.Y - size / 2, pt.X + size / 2, pt.Y + size / 2);
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
        public static void ShowWhere(Geometry geo, double delta = DEFAULT_DELTA)
        {
            ShowWhere(geo.ToGRect(), delta);
        }
        public static void ShowWhere(GRect r, double delta = DEFAULT_DELTA)
        {
            DrawUtils.DrawingQueue.Enqueue(adb =>
            {
                var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                var rect = "[-334718.142328821,1366616.99129695,635160.253054206,1868196.71202574]".JsonToGRect();
                GRect r3 = default;
                {
                    var circle = DrawUtils.DrawCircleLazy(r.Expand(800));
                    circle.Thickness = 500;
                    circle.ColorIndex = 3;
                }
                for (int i = 0; i < 4; i++)
                {
                    var _delta = delta * i;
                    //if (_delta > rect.Width / 2 && _delta > rect.Height / 2) break;
                    var _r = new GRect(r.MinX - _delta, r.MaxY + _delta, r.MaxX + _delta, r.MinY - _delta);
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
                if (!Equals(r3, default(GRect)))
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
        public static void ChangeCadScreenTo(GRect r)
        {
            if (r.IsValid)
            {
                AcHelper.Commands.CommandHandlerBase.ExecuteFromCommandLine("ZOOM", "W", $"{r.LeftTop.X},{r.LeftTop.Y}", $"{r.RightButtom.X},{r.RightButtom.Y}");
            }
        }
        public static void ZoomAll()
        {
            AcHelper.Commands.CommandHandlerBase.ExecuteFromCommandLine("ZOOM", "A");
        }
        public static void FocusMainWindow()
        {
            ThMEPWSS.Common.Utils.FocusMainWindow();
        }
        public static void OpenCadDwgFile(string file, bool readOnly = false)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, readOnly);
        }
        public static void Print(string str, params object[] objs)
        {
            var dt = DateTime.Now.ToString("HH:mm:ss.fff");
            if (objs.Length == 0) Editor.WriteMessage($"\n[{dt}] " + str + "\n");
            else Editor.WriteMessage($"\n[{dt}] " + str + "\n", objs);
        }

        const string TEST_GEO_PREFIX = "feng_test_";
        public static string BuildAndSetCurrentLayer(Database db, string targetLayerName = null)
        {
            //直接打开或新建，然后解冻、解锁
            targetLayerName ??= TEST_GEO_PREFIX + CtGuid().ToString();
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
        public static void UpdateScreen()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
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
        public static DBObjectCollection SelectEntities(AcadDatabase adb)
        {
            IEnumerable<ObjectId> f()
            {
                var ed = Active.Editor;
                var opt = new PromptEntityOptions("请选择");
                while (true)
                {
                    var ret = ed.GetEntity(opt);
                    if (ret.Status == PromptStatus.OK) yield return ret.ObjectId;
                    else yield break;
                }
            }
            return f().Select(id => adb.Element<DBObject>(id)).ToCollection();
        }
        public static Point3d SelectPoint()
        {
            var basePtOptions = new PromptPointOptions("\n选择图纸基点");
            var rst = Active.Editor.GetPoint(basePtOptions);
            if (rst.Status != PromptStatus.OK) return default;
            var basePt = rst.Value;
            return basePt;
        }
        public static bool TrySelectPoint(out Point3d pt)
        {
            var basePtOptions = new PromptPointOptions("\n选择图纸基点");
            var rst = Active.Editor.GetPoint(basePtOptions);
            if (rst.Status != PromptStatus.OK)
            {
                pt = default;
                return false;
            }
            pt = rst.Value;
            return true;
        }
        public static Point3dCollection SelectRange()
        {
            return SelectGRect().ToPoint3dCollection();
        }
        public static Point3dCollection TrySelectRange()
        {
            return TrySelectRect()?.ToPoint3dCollection();
        }
        public static Tuple<Point3d, Point3d> TrySelectRect()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            if (ptLeftRes.Status != PromptStatus.OK) return null;
            Point3d leftDownPt = ptLeftRes.Value;
            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status != PromptStatus.OK) return null;
            return new Tuple<Point3d, Point3d>(leftDownPt, ptRightRes.Value);
        }
        public static GRect SelectGRect()
        {
            var t = SelectRect();
            return new GRect(t.Item1.ToPoint2d(), t.Item2.ToPoint2d());
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
        static _nzm __nzm;
        public static _nzm nzm
        {
            get
            {
                __nzm ??= _nzm.nzm;
                return __nzm;
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
        public static void DeleteTestGeometries()
        {
            Dbg.DeleteTestGeometries();
        }
        public static void CadJsonBaseTest()
        {
            var sv = CadJsonBase.Create();

            //Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 10;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = adb.ModelSpace.OfType<Line>().ToList();
                var l = new List<string>();
                foreach (var e in ents)
                {
                    var j = sv.Serialize(e.StartPoint);
                    l.Add(j);
                    var o = sv.Deserialize<Point3d>(j);
                }
                var str = l.JoinWith("\n");
                File.WriteAllText(@"Y:\test.json", str);
            }
        }
        public static void CadJsonExtendTest()
        {
            var sv1 = CadJsonBase.Create();
            var sv2 = CadJsonExtend.Create(sv1);

            //Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 10;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = adb.ModelSpace.OfType<MText>().ToList();
                var l = new List<string>();
                foreach (var e in ents)
                {
                    var j = sv2.Serialize(e);
                    l.Add(j);
                    var o = sv2.Deserialize<MText>(j);
                }
                var str = l.JoinWith("\n");
                File.WriteAllText(@"Y:\test.json", str);
            }
        }
        public abstract class CadJsonAbstract
        {
            protected Func<Type, bool> _canConvert;
            protected Func<object, Type, Dictionary<string, object>> _serialize;
            protected Func<JObject, Type, object> _deserialize;
            public bool CanConvert(Type type)
            {
                return _canConvert(type);
            }
            public string Serialize<T>(T obj)
            {
                return SerializeToDict(obj).ToJson();
            }
            public Dictionary<string, object> SerializeToDict<T>(T obj)
            {
                return _serialize(obj, typeof(T));
            }
            public T Deserialize<T>(string json)
            {
                return DeserialzeFromJObject<T>(json.FromJson<JObject>());
            }
            public T DeserialzeFromJObject<T>(JObject jo)
            {
                return (T)_deserialize(jo, typeof(T));
            }
            public JObject ToJObject<T>(T obj)
            {
                return Serialize(obj).FromJson<JObject>();
            }
        }
        public class CadJsonBase : CadJsonAbstract
        {
            private CadJsonBase() { }
            public static CadJsonBase Create()
            {
                var r = new CadJsonBase();
                {
                    var hs = new HashSet<Type>();
                    hs.Add(typeof(Point2d));
                    hs.Add(typeof(Point3d));
                    r._canConvert = type => hs.Contains(type);
                }
                {
                    var d = new Dictionary<Type, Func<object, Dictionary<string, object>>>();
                    d[typeof(Point2d)] = o =>
                    {
                        var jo = new Dictionary<string, object>();
                        jo["type"] = typeof(Point2d).Name;
                        var e = (Point2d)o;
                        jo["data"] = new double[] { e.X, e.Y };
                        return jo;
                    };
                    d[typeof(Point3d)] = o =>
                    {
                        var jo = new Dictionary<string, object>();
                        jo["type"] = typeof(Point3d).Name;
                        var e = (Point3d)o;
                        jo["data"] = new double[] { e.X, e.Y, e.Z };
                        return jo;
                    };
                    r._serialize = (o, t) => d[t](o);
                }
                {
                    var d = new Dictionary<Type, Func<JObject, object>>();
                    d[typeof(Point2d)] = jo =>
                    {
                        var ja = (JArray)jo["data"];
                        return new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>());
                    };
                    d[typeof(Point3d)] = jo =>
                    {
                        var ja = (JArray)jo["data"];
                        return new Point3d(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>());
                    };
                    r._deserialize = (jo, t) => d[t](jo);
                }
                return r;
            }
        }
        public class CadJsonExtend : CadJsonAbstract
        {
            private CadJsonExtend() { }
            public static CadJsonExtend Create(CadJsonBase cb)
            {
                var r = new CadJsonExtend();
                {
                    var hs = new HashSet<Type>();
                    hs.Add(typeof(Line));
                    r._canConvert = type => hs.Contains(type);
                }
                {
                    var d = new Dictionary<Type, Func<object, Dictionary<string, object>>>();
                    d[typeof(Line)] = o =>
                    {
                        var jo = new Dictionary<string, object>();
                        jo["type"] = typeof(Line).Name;
                        var e = (Line)o;
                        jo["startPt"] = cb.ToJObject(e.StartPoint);
                        jo["endPt"] = cb.ToJObject(e.EndPoint);
                        return jo;
                    };
                    d[typeof(Polyline)] = o =>
                    {
                        var jo = new Dictionary<string, object>();
                        jo["type"] = typeof(Polyline).Name;
                        var e = (Polyline)o;
                        jo["closed"] = e.Closed;
                        try
                        {
                            jo["constantWidth"] = e.ConstantWidth;
                        }
                        catch
                        {
                            jo["constantWidth"] = 0;
                        }
                        var points = new Point3d[e.NumberOfVertices];
                        for (int i = 0; i < points.Length; i++) points[i] = e.GetPoint3dAt(i);
                        jo["points"] = points.Select(pt => cb.ToJObject(pt)).ToArray();
                        return jo;
                    };
                    d[typeof(Circle)] = o =>
                    {
                        var jo = new Dictionary<string, object>();
                        jo["type"] = typeof(Circle).Name;
                        var e = (Circle)o;
                        jo["center"] = cb.ToJObject(e.Center);
                        jo["radius"] = e.Radius;
                        return jo;
                    };
                    d[typeof(DBText)] = o =>
                    {
                        var jo = new Dictionary<string, object>();
                        jo["type"] = typeof(DBText).Name;
                        var e = (DBText)o;
                        jo["position"] = cb.ToJObject(e.Position);
                        jo["text"] = e.TextString;
                        return jo;
                    };
                    d[typeof(MText)] = o =>
                    {
                        var jo = new Dictionary<string, object>();
                        jo["type"] = typeof(MText).Name;
                        var e = (MText)o;
                        jo["location"] = cb.ToJObject(e.Location);
                        //jo["text"] = e.Text;//跟Contents一样，目前没见过不一样的
                        jo["contents"] = e.Contents;
                        return jo;
                    };
                    r._serialize = (o, t) => d[t](o);
                }
                {
                    var d = new Dictionary<Type, Func<JObject, object>>();
                    d[typeof(Line)] = jo =>
                    {
                        var e = new Line();
                        e.StartPoint = cb.DeserialzeFromJObject<Point3d>((JObject)jo["startPt"]);
                        e.EndPoint = cb.DeserialzeFromJObject<Point3d>((JObject)jo["endPt"]);
                        return e;
                    };
                    d[typeof(Polyline)] = jo =>
                    {
                        var e = new Polyline();
                        e.Closed = jo["closed"].ToObject<bool>();
                        e.ConstantWidth = jo["constantWidth"].ToObject<double>();
                        var ja = (JArray)jo["points"];
                        for (int i = 0; i < ja.Count; i++)
                        {
                            JToken item = ja[i];
                            var pt = cb.DeserialzeFromJObject<Point3d>((JObject)item);
                            e.AddVertexAt(i, pt.ToPoint2d(), 0, 0, 0);
                        }
                        return e;
                    };
                    d[typeof(Circle)] = jo =>
                    {
                        var e = new Circle();
                        e.Center = cb.DeserialzeFromJObject<Point3d>((JObject)jo["center"]);
                        e.Radius = jo["radius"].ToObject<double>();
                        return e;
                    };
                    d[typeof(DBText)] = jo =>
                    {
                        var e = new DBText();
                        e.Position = cb.DeserialzeFromJObject<Point3d>((JObject)jo["position"]);
                        e.TextString = jo["text"].ToObject<string>();
                        return e;
                    };
                    d[typeof(MText)] = jo =>
                    {
                        var e = new MText();
                        e.Location = cb.DeserialzeFromJObject<Point3d>((JObject)jo["location"]);
                        e.Contents = jo["contents"].ToObject<string>();
                        return e;
                    };
                    r._deserialize = (jo, t) => d[t](jo);
                }
                return r;
            }
        }
        //Autodesk.AutoCAD.DatabaseServices.DBPoint
        //Autodesk.AutoCAD.DatabaseServices.DBText
        //Autodesk.AutoCAD.DatabaseServices.Line
        //Autodesk.AutoCAD.DatabaseServices.BlockReference
        //Autodesk.AutoCAD.DatabaseServices.Polyline
        //Autodesk.AutoCAD.DatabaseServices.RotatedDimension
        //Autodesk.AutoCAD.DatabaseServices.MText
        //Autodesk.AutoCAD.DatabaseServices.Circle
        //Autodesk.AutoCAD.DatabaseServices.ImpCurve
        //Autodesk.AutoCAD.DatabaseServices.ImpEntity
        //Autodesk.AutoCAD.DatabaseServices.Spline
        //Autodesk.AutoCAD.DatabaseServices.Arc
        //Autodesk.AutoCAD.DatabaseServices.Ellipse
        //Autodesk.AutoCAD.DatabaseServices.Ole2Frame


        //找立管1
        //FS59OCRA_W20-3#-地上给排水及消防平面图.dwg
        public static void qss737()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 10;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(e=> e.ToDataItem().EffectiveName==""))
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "WP_KTN_LG"))
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        //找立管2
        //FS59OCRA_W20-3#-地上给排水及消防平面图.dwg
        public static void qss77o()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 10;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(e=> e.ToDataItem().EffectiveName==""))
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    Dbg.ShowWhere(e);
                }
            }
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
                var storeysRecEngine = new ThStoreysRecognitionEngine();
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
                var storeysRecEngine = new ThStoreysRecognitionEngine();
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
                DU.DrawEntityLazy(Dbg.SelectPoint().Expand(100).ToGRect().ToCadPolyline());
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


        public static void ImportFromDwgTest()
        {
            var file = Path.Combine(ThCADCommon.SupportPath(), "地上给水排水平面图模板_20210125.dwg");
            if (File.Exists(file))
            {
                using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (AcadDatabase adb = AcadDatabase.Active())
                using (AcadDatabase blockDb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly, false))
                {
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("侧排雨水斗系统"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("重力流雨水井编号"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("$TwtSys$00000132"));
                    //adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("*U349"));//failed
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("*U348"));
                    adb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault("TH-STYLE1"), false);
                    //adb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault("TH-STYLE2"), false);//failed
                    adb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault("TH-STYLE3"), false);
                    //adb.Layers.Import(blockDb.Layers.ElementOrDefault(""));
                }
            }
        }
        public static void AhTest2()
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
                            //Dbg.ShowWhere(ah);
                            if (ah is MText mt)
                            {
                                if (mt.Contents.ToLower() == "ah1")
                                {
                                    isLow = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void AhTest()
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
                var si = ThRainSystemService.BuildSpatialIndex(sv.AhTexts);
                foreach (var cp in sv.CondensePipes)
                {
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
                            Dbg.ShowWhere(ah);
                        }
                    }
                }
            }
        }

        private static void NewMethod1()
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
                foreach (Entity e in si.NearestNeighbours(Dbg.SelectPoint().Expand(1).ToGRect().ToCadPolyline(), 3))
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
                    var pl = r.ToCadPolyline();
                    DU.DrawEntityLazy(pl);
                }

            }
        }


        public static void RunThRainSystemDiagramCmd_NoRectSelection2()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                Point3d pt1, pt2;
                SelectAllStoreys(out pt1, out pt2);
                NewMethod(adb, pt1, pt2);
            }
        }
        public static void hhh()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                var lst = e.ExplodeToDBObjectCollection().Cast<Entity>().ToList();
                Debugger.Break();
            }
        }
        public static void kkk()
        {
            var name = File.ReadLines(@"E:\xx.txt").First().Trim();
            typeof(ThDebugClass).GetMethod(name).Invoke(null, null);
        }
        public static void test6()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                Debugger.Break();
            }
        }
        public static void test5()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                var tp = e.GetType();
                var fds = tp.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var ls = new List<string>();
                foreach (var fd in fds)
                {
                    if (fd.FieldType == typeof(string))
                    {
                        ls.Add(fd.Name);
                    }
                }
                Dbg.ShowString(ls.JoinWith("\n"));
            }
        }
        //判断天正元素
        public static void test1()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => x.GetType().IsNotPublic && x.GetType().Name.StartsWith("Imp") && x.GetType().Namespace == "Autodesk.AutoCAD.DatabaseServices"))
                {
                    DU.DrawBoundaryLazy(e);
                }
            }
        }
        //炸开天正元素
        public static void test2()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => x.GetType().IsNotPublic && x.GetType().Name.StartsWith("Imp") && x.GetType().Namespace == "Autodesk.AutoCAD.DatabaseServices"))
                {
                    foreach (var ee in e.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                    {
                        DU.DrawBoundaryLazy(ee);
                    }
                }
            }
        }
        public static void test3()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(e);
                    DU.DrawRectLazy(r);
                }
            }
        }
        public static void ExplodePolylineTest()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                MessageBox.Show(adb.ModelSpace.OfType<Polyline>().Select(e => e.ExplodeToDBObjectCollection().Count).Max().ToString());
            }
        }
        public static void qsw7rc()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = adb.ModelSpace.OfType<BlockReference>()
          .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
          .Where(x => x.ToDataItem().EffectiveName == "$LIGUAN")
          .ToList();
                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }
                //var e =Dbg.SelectEntity<BlockReference>(adb);
                //Dbg.ShowString(e.ToDataItem().EffectiveName);
            }

        }
        public static void qsw8d6()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = adb.ModelSpace.OfType<BlockReference>()
          .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
          .Where(x => x.Name == "*U398")
          .ToList();
                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void qsxip5()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
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
                                    .OfType<Line>()
                                    .ToList();
                                foreach (var t in lst3)
                                {
                                    Dbg.ShowWhere(t);
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
                            Dbg.ShowWhere(t);
                        }
                    }
                }
            }
        }
        public static void qsxk6p()
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
                var txts = sv.GetDBText(Dbg.SelectRange());
                Dbg.PrintText(txts.ToJson());
            }
        }
        public static void qsxz9s()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                Dbg.PrintLine(e.ExplodeToDBObjectCollection().Count);
            }
        }
        public static void qsxzxx()
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
                foreach (var e in sv.VerticalPipeLines)
                {
                    DU.DrawBoundaryLazy(e);
                }
            }
        }
        public static void qsxynk()
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
                foreach (var g in sv.RainPortsGroups)
                {
                    //DU.DrawBoundaryLazy(g.ToArray());
                    foreach (var e in g)
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
                        {
                            if (!string.IsNullOrEmpty(lb))
                            {
                                DU.DrawTextLazy(lb, sv.BoundaryDict[e].LeftTop.ToPoint3d());
                            }
                        }
                    }
                }
            }
        }
        public static void qt8n7z()
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

                if (false) NewMethod2(adb, sv);
                if (false) ThRainSystemService.TempPatch(adb, sv);
            }
        }

        private static void NewMethod2(AcadDatabase adb, ThRainSystemService sv)
        {
            var txts = new List<Entity>();
            foreach (var ent in sv.EnumerateTianzhengElements().ToList())
            {
                var lst = ent.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                if (lst.Count == 1)
                {
                    var e = lst.First();
                    txts.Add(e);
                    //if (e.TextString == "Y1L1-2")
                    //{
                    //    Dbg.ShowWhere(e);
                    //}
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
                        //Dbg.ShowWhere(ent);
                    }
                }
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
                //DU.DrawBoundaryLazy(g.ToArray());
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
                                    DU.DrawTextLazy(lb, bd.Center.ToPoint3d());
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
                                DU.DrawTextLazy(lb, bd.Center.ToPoint3d());
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void qsxmgz()
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
                //foreach (var g in sv.RainPortsGroups)
                //{
                //    //DU.DrawBoundaryLazy(g.ToArray());
                //    foreach (var e in g)
                //    {
                //        if(sv.VerticalPipeToLabelDict.TryGetValue(e,out string lb))
                //        {
                //            if (string.IsNullOrEmpty(lb)) lb = "???";
                //            {
                //                DU.DrawTextLazy(lb, sv.BoundaryDict[e].LeftTop.ToPoint3d());
                //            }
                //        }
                //    }
                //}
                foreach (var e in sv.VerticalPipes)
                {
                    if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
                    {
                        //if (string.IsNullOrEmpty(lb)) lb = "???";
                        //{
                        //    DU.DrawTextLazy(lb, sv.BoundaryDict[e].LeftTop.ToPoint3d());
                        //}
                        if (string.IsNullOrEmpty(lb)) Dbg.ShowWhere(e);
                    }
                }
            }
        }
        public static void qsxhgn()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lst = new List<DBText>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(br);
                    if (r.Width > 10000 && r.Width < 60000)
                    {
                        foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                        {
                            //var r2 = GeoAlgorithm.GetBoundaryRect(e);
                            //DU.DrawRectLazy(r2);
                            //if (e is BlockReference br2)
                            //{
                            //    DU.DrawTextLazy(br2.Name, r2.RightTop.ToPoint3d());
                            //}
                            //if (e is BlockReference br2)
                            //{
                            //    if (br2.Name == "*U398")
                            //    {
                            //        vps.Add(br2);
                            //    }
                            //}
                            //else
                            if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                var lst3 = e.ExplodeToDBObjectCollection()
                                    .OfType<Entity>()
                                    .Where(x => ThRainSystemService.IsTianZhengElement(x))
                                    .SelectMany(x => x.ExplodeToDBObjectCollection().OfType<DBText>())
                                    .ToList();
                                foreach (var t in lst3)
                                {
                                    //txts.Add(t);
                                    Dbg.ShowWhere(t);
                                }
                            }

                            //if(e is DBText)
                            //{
                            //    Dbg.ShowWhere(e);
                            //}

                            //ExplodedEntities.Add(e);
                        }
                    }
                }
            }
        }
        public static void qsw8n3()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.CollectTianZhengEntities();
                sv.ExplodeSingleTianZhengElements();
                sv.CollectExplodedEntities();
                sv.CollectVerticalPipes();
                foreach (var e in sv.VerticalPipes)
                {
                    Dbg.ShowWhere(e);
                    //DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                }
                //foreach (var e in sv.ExplodedEntities)
                //{
                //    DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                //}
            }
        }
        public static void qsxbgn()
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
                foreach (var e in sv.VerticalPipes)
                {
                    Dbg.ShowWhere(e);
                    //DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                }
                //foreach (var e in sv.ExplodedEntities)
                //{
                //    DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                //}
            }
        }
        public static void qsxnsd()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var points = Dbg.SelectRange();
                var storeysRecEngine = new ThStoreysRecognitionEngine();
                storeysRecEngine.Recognize(adb.Database, points);
                Dbg.ShowString(storeysRecEngine.Elements.Count.ToString());
            }

        }
        public static void qsxc3b()
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
                foreach (var e in sv.VerticalPipeDBTexts)
                {
                    if (e is DBText) Dbg.ShowWhere(e);
                    //DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                }

                //foreach (var e in sv.ExplodedEntities)
                //{
                //    DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                //}
            }
        }
        public static void qsxsdp()
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
                foreach (var e in sv.ConnectToRainPortSymbols)
                {
                    Dbg.ShowWhere(e);
                }
            }
        }

        public static void qsxbif()
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
                foreach (var e in sv.VerticalPipeDBTexts)
                {
                    Dbg.ShowWhere(e);
                    //DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                }
                //foreach (var e in sv.ExplodedEntities)
                //{
                //    DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                //}
            }
        }
        public static void qsxbv3()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var e in adb.ModelSpace.OfType<Entity>().ToList())
                {
                    if (ThRainSystemService.IsTianZhengElement(e))
                    {
                        var lst = e.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                        foreach (var t in lst)
                        {
                            Dbg.ShowWhere(t);
                        }
                    }
                }
            }
        }
        public static void qt8n3e()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var ents = adb.ModelSpace.OfType<Entity>()
        .Where(x => ThRainSystemService.IsTianZhengElement(x))
        .Where(x => x.Layer == "W-RAIN-EQPM")
        .SelectMany(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Where(c => c.Radius > 40 && c.Radius < 80));

                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }


                //var br = Dbg.SelectEntity<BlockReference>(adb);
                ////Dbg.PrintLine(br.ExplodeToDBObjectCollection().Count);
                //foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                //{
                //    DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                //}

                //foreach (var br in adb.ModelSpace.OfType<BlockReference>())
                //{
                //    var r = GeoAlgorithm.GetBoundaryRect(br);
                //    if (r.Width > 10000 && r.Width < 60000)
                //    {
                //        foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                //        {
                //            DU.DrawRectLazy(GeoAlgorithm.GetBoundaryRect(e));
                //        }
                //    }
                //}
            }
        }
        public static void qsxmvb()
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
                foreach (var e in sv.VerticalPipes)
                {
                    DU.DrawRectLazy(sv.BoundaryDict[e]);
                }
            }
        }
        public static void qsxmqt()
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
                foreach (var e in sv.VerticalPipes)
                {
                    DU.DrawBoundaryLazy(e);
                }
            }
        }
        public static void qsx97t()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var br = Dbg.SelectEntity<BlockReference>(adb);
                foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                {
                    DU.DrawBoundaryLazy(e);
                }
            }
        }
        public static void qt8mkj()
        {
        }

        public static void qt8mkk()
        {
        }

        public static void qt8mkl()
        {
        }

        public static void qt8mkm()
        {
        }

        public static void qt8mkn()
        {
        }

        public static void qt8mko()
        {
        }

        public static void qt8mkp()
        {
        }

        public static void qt8mkq()
        {
        }

        public static void qt8mkr()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var e in adb.ModelSpace.OfType<DBText>())
                {
                    if (e.TextString == "NL1-5")
                    {
                        Dbg.ShowWhere(e);
                    }
                }
            }
        }

        public static void qt8mks()
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
                foreach (var e in sv.VerticalPipeDBTexts)
                {
                    if (e.TextString == "Y2L1-3")
                        Dbg.ShowWhere(e);
                }
                //foreach (var e in adb.ModelSpace.OfType<DBText>().Where(x => x.TextString == "Y1L1-1"))
                //{
                //    Dbg.ShowWhere(e);
                //}
            }
        }

        public static void qu0jef()
        {
            Dbg.FocusMainWindow();
            using (Lock)
            using (var adb = AcadDatabase.Active())
            using (DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };

                //var pl = DU.DrawPolyLineLazy(new Point3d[] { Dbg.SelectPoint(), Dbg.SelectPoint() });
                //pl.ConstantWidth = 100;

                //sv.InitCache();
                //foreach (var e in sv.VerticalPipeLines)
                //{
                //    Dbg.ShowLine(e);
                //}

                //foreach (var e in adb.ModelSpace.OfType<Entity>())
                //{
                //    if (ThRainSystemService.IsTianZhengElement(e))
                //    {
                //        foreach (var ee in e.ExplodeToDBObjectCollection().OfType<DBText>())
                //        {
                //            if (ee.TextString == "NL2-1")
                //            {
                //                Dbg.ShowWhere(ee);
                //            }
                //        }
                //    }
                //}

                //string strFloorDrain = "地漏";
                //var q = adb.ModelSpace.OfType<BlockReference>()
                //   .Where(e => e.ObjectId.IsValid)
                //.Where(x =>
                //{
                //    if (x.IsDynamicBlock)
                //    {
                //        return x.ObjectId.GetDynBlockValue("可见性")?.Contains(strFloorDrain) ?? false;

                //    }
                //    else
                //    {
                //        return x.ToDataItem().EffectiveName.Contains(strFloorDrain);
                //    }
                //});
                //foreach (var e in q)
                //{
                //    //Dbg.ShowWhere(e);
                //    //DU.DrawBoundaryLazy(e, 10);
                //}

                //sv.InitCache();
                //foreach (var e in sv.VerticalPipes)
                //{
                //    //DU.DrawBoundaryLazy(e, 10);
                //    var bd = sv.BoundaryDict[e];
                //    DU.DrawRectLazy(bd);
                //}

                //sv.InitCache();
                //sv.CollectData();

                //foreach (var pipe in sv.VerticalPipes)
                //{
                //    sv.VerticalPipeToLabelDict.TryGetValue(pipe, out string lb);
                //    if (ThRainSystemService.IsWantedLabelText(lb))
                //    {
                //        Dbg.ShowWhere(pipe);
                //    }
                //    else
                //    {
                //        DU.DrawTextLazy(lb ?? "???", sv.BoundaryDict[pipe].Center.ToPoint3d());
                //    }
                //    //DU.DrawBoundaryLazy(e, 10);
                //}

                //if (false)
                //qt344d.qt3457(adb);

                //var storeysRecEngine = new ThStoreysRecognitionEngine();
                //storeysRecEngine.Recognize(adb.Database, Dbg.SelectRange());
                //foreach (var e in storeysRecEngine.Elements)
                //{
                //    //DU.DrawBoundaryLazy(e.Boundary,100);
                //    var r = GeoAlgorithm.GetBoundaryRect(e.Boundary);
                //    Dbg.PrintLine(r.ToJson());
                //    //Dbg.PrintLine(e.Boundary.Bounds.ToJson());
                //    if (e.Boundary == null)
                //    {
                //        //Dbg.ShowString("!!!");
                //    }
                //    //var pl = DU.DrawRectLazy(r);
                //    //pl.ConstantWidth = 100;
                //           DU.DrawBoundaryLazy(adb.Element<Entity>(e.ObjectId), 100);
                //}


            }
        }



        public static void qsx5z7()
        {
            //var file = @"D:\DATA\Git\ThMEPEngine\AutoLoader\Contents\Support\地上给水排水平面图模板_20210125.dwg";

            //绘图说明
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\绘图说明_20210409.dwg";

            //一开始的图纸
            //第01张图纸
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\FL1ASTSB_W20-8#楼-给排水及消防平面图.dwg";
            //图画出来了
            //地漏和立管无连线的情况不识别
            //Y1L1-2 has problem for check point and label
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\佳兆业滨江新城\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
            //提取楼层失败
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\05_测试图纸（新武汉）(2)\05_测试图纸（新武汉）\湖北交投颐和华府\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg";
            //提取楼层失败
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\05_测试图纸（新武汉）(2)\05_测试图纸（新武汉）\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
            //提取楼层失败
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\05_测试图纸（新武汉）(2)\05_测试图纸（新武汉）\蓝光未来阅璟\FS5F8704_W20-地上给水排水平面图-送审版.dwg";
            //提取楼层失败
            //提取立管失败（注意跟GAS的引线的共用问题）
            //雨水口也很奇怪
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\05_测试图纸（新武汉）(2)\05_测试图纸（新武汉）\蓝光钰泷府二期\FS59P2BC_W20-地上给水排水平面图-副本.dwg";
            //提取楼层失败
            //这里的文本带了星号，被过滤掉了
            //不管
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\05_测试图纸（新武汉）(2)\05_测试图纸（新武汉）\万科花园\FS56Y37Y_W20-地上给水排水平面图2017-3-14.dwg";
            //提取楼层失败
            //这里的文本也带有前缀
            //有部分图块需要炸一下
            //雨水口图层需要修正
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\05_测试图纸（新武汉）(2)\05_测试图纸（新武汉）\武汉二七滨江商务区南一片住宅地块\FS5747SS_W20-地上给水排水平面图.dwg";
            //提取楼层失败
            //雨水口也很奇怪
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\05_测试图纸（新武汉）(2)\05_测试图纸（新武汉）\长征村K2地块\FS5F46QE_W20-地上给水排水平面图-Z.dwg";
            //图画出来了
            //不支持FL开头的立管
            //支持87雨水斗
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\湖北交投颐和华府\湖北交投颐和华府\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg";



            //文本，炸开两次才能拿到，天正 W-RAIN-DIMS
            //标号格式不对，先不管
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\亳州恒大城\亳州恒大城\fs54ba4v_w20-地上给水排水平面图7.26.dwg";

            //文本包含前缀
            //先不管
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\合景红莲湖项目\合景红莲湖项目\FS55TD78_W20-73#-地上给水排水平面图.dwg";
            //图画出来了
            //LN开头的立管不支持
            //87雨水斗问题
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\清江山水四期\清江山水四期\FS55TMPH_W20-地上给水排水平面图.dwg";
            //图画出来了
            //不支持YL开头的立管
            //地漏没有连线
            //NL2-4 先连到雨水口，又连到雨水井
            //一楼套管问题
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\庭瑞君越观澜三期\庭瑞君越观澜三期\fs57grhn_w20-地上给水排水平面图.dwg";
            //图画出来了
            //地漏 转管看看
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210512\澳海黄州府（二期）\澳海黄州府（二期）\FS5GMBXU_W20-地上给水排水平面图.dwg";

            //label不对
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\合景红莲湖项目_框线(1)\合景红莲湖项目_框线\FS55TD78_W20-73#-地上给水排水平面图.dwg";
            //图画出来了
            //RF层不对
            //第03张图纸
            var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\湖北交投颐和华府_框线(1)\湖北交投颐和华府_框线\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg";
            //图画出来了
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\佳兆业滨江新城_框线(1)\佳兆业滨江新城_框线\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
            //图画出来了
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\蓝光未来阅璟_框线(1)\蓝光未来阅璟_框线\FS5F8704_W20-地上给水排水平面图-送审版.dwg";
            //图画出来了
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\蓝光钰泷府二期_框线(1)\蓝光钰泷府二期_框线\FS59P2BC_W20-地上给水排水平面图-副本.dwg";
            //图画出来了
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\清江山水四期_框线(1)\清江山水四期_框线\FS55TMPH_W20-地上给水排水平面图.dwg";
            //label不对
            //var file = @"E:\thepa_workingSpace\任务资料\任务2\210517\武汉二七滨江商务区南一片住宅地块_框线(1)\武汉二七滨江商务区南一片住宅地块_框线\FS5747SS_W20-地上给水排水平面图.dwg";

            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
            //Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();

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
                DrawUtils._DrawBoundary(db, e, 2);
            }
        }
        public static void FindFloorDrain()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //var lst = adb.ModelSpace.OfType<BlockReference>().Where(x => x.ObjectId.IsValid).Where(x => x.Name == "*U348").ToList();
                //var lst = adb.ModelSpace.OfType<BlockReference>().Where(x => x.ObjectId.IsValid).Where(x => x.Name == "地漏系统").ToList();
                var lst = adb.ModelSpace.OfType<BlockReference>().Where(x => x.ObjectId.IsValid).Where(x => x.ToDataItem().EffectiveName == "地漏系统").ToList();
                foreach (var e in lst)
                {
                    Dbg.ShowWhere(e);
                }
            }
        }
        public static void qsz23s()
        {
#if ACAD_ABOVE_2014
            Autodesk.AutoCAD.ApplicationServices.DocumentExtension.CloseAndDiscard(Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument);
#else
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.CloseAndDiscard();
#endif
        }
        static Autodesk.AutoCAD.ApplicationServices.DocumentLock Lock => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
        public static void qsxoxu()
        {
            ThRainSystemService.ImportElementsFromStdDwg();
            Dbg.FocusMainWindow();
            var basePt = Dbg.SelectPoint();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                var db = adb.Database;
                var layerName = Dbg.BuildAndSetCurrentLayer(db);
                //var blkName = "侧排雨水斗系统";
                //var blkName = "屋面雨水斗";
                //var blkName = "*U348";
                var blkName = "地漏系统";
                adb.ModelSpace.ObjectId.InsertBlockReference(layerName, blkName, basePt, new Scale3d(1), 0);
            }
        }
        public static void qsxps4()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                var br = Dbg.SelectEntity<BlockReference>(adb);
                Dbg.ShowString(br.ToDataItem().ToJson());
            }
        }
        public static void SelectEntityAndExplodeTest()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Dbg.FocusMainWindow();
                var e = Dbg.SelectEntity<Entity>(adb);
                var lst = e.ExplodeToDBObjectCollection().Cast<Entity>().ToList();
                Debugger.Break();
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
        public static void qt8n8l()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db, "😀");
                Dbg.BuildAndSetCurrentLayer(db);
                var points = Dbg.SelectRange();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");
                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;

                var diagram = new ThWRainSystemDiagram();
                var storeysRecEngine = new ThStoreysRecognitionEngine();
                storeysRecEngine.Recognize(adb.Database, points);

                diagram.InitServices(adb, points);
                diagram.InitStoreys(storeysRecEngine.Elements);
                diagram.InitVerticalPipeSystems(points);
                diagram.Draw(basePt);
            }

        }
        public static void xx()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                var points = Dbg.SelectRange();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");
                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;

                var diagram = new ThWRainSystemDiagram();
                var storeysRecEngine = new ThStoreysRecognitionEngine();
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
            public static CRect Create(GRect r)
            {
                return new CRect() { MinX = r.MinX, MinY = r.MinY, MaxX = r.MaxX, MaxY = r.MaxY };
            }
            public GRect ToGRect()
            {
                return new GRect(MinX, MinY, MaxX, MaxY);
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
                    var r = GRect.Create(ext);
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
                    var r = GRect.Create(ept);
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
            var storeysRecEngine = new ThStoreysRecognitionEngine();
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
                GeoAlgorithm.TryConvertToLineSegment(e1, out GLineSegment seg1);
                var e2 = Dbg.SelectEntity<Entity>(adb);
                GeoAlgorithm.TryConvertToLineSegment(e2, out GLineSegment seg2);
                Dbg.PrintLine(GeoAlgorithm.IsOnSameLine(seg1, seg2, 5).ToString());
            }
        }
        public class qu0jxf
        {
            public static void CollectorCodegen()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    foreach (var e in Dbg.SelectEntities(adb).OfType<Entity>())
                    {
                        if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            Dbg.PrintLine($"adb.ModelSpace.OfType<Entity>().Where( e=>e.Layer=={e.Layer.ToJson()} && ThRainSystemService.IsTianZhengElement(x))");
                        }
                        else if (e is Line)
                        {
                            Dbg.PrintLine($"adb.ModelSpace.OfType<Line>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                        }
                        else if (e is Polyline)
                        {
                            Dbg.PrintLine($"adb.ModelSpace.OfType<Polyline>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                        }
                        else if (e is Circle)
                        {
                            Dbg.PrintLine($"adb.ModelSpace.OfType<Circle>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                        }
                        else if (e is DBText)
                        {
                            Dbg.PrintLine($"adb.ModelSpace.OfType<DBText>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                        }
                        else if (e is MText)
                        {
                            Dbg.PrintLine($"adb.ModelSpace.OfType<MText>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                        }
                        else if (e is BlockReference br)
                        {
                            Dbg.PrintLine($"adb.ModelSpace.OfType<BlockReference>().Where( e=>e.Layer=={e.Layer.ToJson()}) && e.ObjectId.IsValid && e.GetEffectiveName()=={br.GetEffectiveName().ToJson()})");
                        }
                    }

                }
            }
            public static void BlockReferenceToDataItemToJson()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.PrintLine(Dbg.SelectEntity<BlockReference>(adb).ToDataItem().ToJson());
                }
            }
            public static void GetEntityBoundsGRectCadJson()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.PrintLine(Dbg.SelectEntity<Entity>(adb).Bounds.ToGRect().ToCadJson());
                }
            }
            public static void GetEntityType()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Entity>(adb);
                    Dbg.PrintLine(e.GetType().ToString());
                }
            }
            public static void GetEntityLength()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Entity>(adb);
                    GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg);
                    Dbg.PrintLine(seg.Length.ToString());
                }
            }
            public static void GetEntityBlockName()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<BlockReference>(adb);
                    Dbg.PrintLine(e.Name);
                }
            }
            public static void GetEntityBlockEffectiveName()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<BlockReference>(adb);
                    Dbg.PrintLine(e.ToDataItem().EffectiveName);
                }
            }

            public static void GetCircleRadius()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Circle>(adb);
                    Dbg.PrintLine(e.Radius.ToString());
                }
            }
            public static void GetEntityLayer()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Entity>(adb);
                    Dbg.PrintLine(e.Layer);
                }
            }
            public static void GetTextStyleName()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<DBText>(adb);
                    Dbg.PrintLine(e.TextStyleName);
                }
            }
            public static void ScaleEntityTest()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Entity>(adb);
                    EntTools.Scale(e, Dbg.SelectPoint(), 2);
                }
            }
            public static void RotateEntityTest()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Entity>(adb);
                    EntTools.Rotate(e, Dbg.SelectPoint(), GeoAlgorithm.AngleFromDegree(45));
                }
            }
            public static void MoveEntityTest()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Entity>(adb);
                    EntTools.Move(e, Dbg.SelectPoint(), GeoAlgorithm.GetBoundaryRect(e).Center.ToPoint3d());
                }
            }
            public static void CopyEntityTest()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<Entity>(adb);
                    EntTools.Copy(e, Dbg.SelectPoint(), GeoAlgorithm.GetBoundaryRect(e).Center.ToPoint3d());
                }
            }
            public static void GetScaleFactors()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<BlockReference>(adb);
                    Dbg.PrintLine(e.ScaleFactors.ToString());
                }
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
                    var r = ept.ToGRect();
                    Dbg.ShowWhere(r);
                    foreach (var e in sv.VerticalPipes)
                    {
                        if (GeoAlgorithm.Distance(r.Center, sv.BoundaryDict[e].Center) < 100)
                        {
                            Dbg.ShowWhere(ept.ToGRect());
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
                    DU.DrawRectLazy(ept.ToGRect());
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
                var storeysRecEngine = new ThStoreysRecognitionEngine();
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
            var storeysRecEngine = new ThStoreysRecognitionEngine();
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
                //var list = new List<KeyValuePair<Entity, GLineSegment>>();
                //foreach (var pipe in sv.DraiDomePipes)
                //{
                //    if (GeoAlgorithm.TryConvertToLineSegment(pipe, out GLineSegment seg))
                //    {
                //        list.Add(new KeyValuePair<Entity, GLineSegment>(pipe, seg));
                //    }
                //}
                ////var ent = Dbg.SelectEntity<Entity>(adb);
                //var pairs = new List<KeyValuePair<int, int>>();
                //for (int i = 0; i < list.Count; i++)
                //{
                //    for (int j = i + 1; j < list.Count; j++)
                //    {
                //        var kv1 = list[i];
                //        var kv2 = list[j];
                //        const double dis = 8000;
                //        if (NewMethod(kv1, kv2, dis))
                //        {
                //            pairs.Add(new KeyValuePair<int, int>(i, j));
                //        }
                //    }
                //}
                //var dict = new ListDict<int>();
                //var h = new BFSHelper()
                //{
                //    Pairs = pairs.ToArray(),
                //    TotalCount = list.Count,
                //    Callback = (g, i) =>
                //    {
                //        dict.Add(g.root, i);
                //    },
                //};
                //h.BFS();
                //groups = new List<List<Entity>>();
                //dict.ForEach((_i, l) =>
                //{
                //    //DU.DrawBoundaryLazy(l.Select(i => list[i].Key).ToArray(), 2);
                //    groups.Add(l.Select(i => list[i].Key).ToList());
                //});
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
                    DU._DrawBoundary(db, e1, 3);
                    foreach (var e2 in sv.VerticalPipeLines.Where(e => GeoAlgorithm.IsRectCross(sv.BoundaryDict[e], sv.BoundaryDict[e1])))
                    {
                        if (e2 != e1)
                        {
                            DU._DrawBoundary(db, e2, 3);
                            foreach (var e3 in sv.ConnectToRainPortDBTexts.Where(e => GeoAlgorithm.IsRectCross(sv.BoundaryDict[e], sv.BoundaryDict[e2].Expand(200))))
                            {
                                DU._DrawBoundary(db, e3, 3);
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
                            DU._DrawBoundary(db, o, 2);
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
                GeoAlgorithm.TryConvertToLineSegment(e1, out GLineSegment seg1);
                Dbg.PrintLine(GeoAlgorithm.AngleToDegree((seg1.EndPoint - seg1.StartPoint).Angle).ToString());
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
                var rect = "[-334718.142328821,1366616.99129695,635160.253054206,1868196.71202574]".JsonToGRect();
                const double delta = 1000;
                for (int i = 0; i < 1000; i++)
                {
                    var _delta = delta * i;
                    if (_delta > rect.Width / 2 && _delta > rect.Height / 2) break;
                    var _r = new GRect(r.MinX - _delta, r.MaxY + _delta, r.MaxX + _delta, r.MinY - _delta);
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






    public static class qt344d
    {
        public static void qt3457(AcadDatabase adb)
        {
            var d = new Dictionary<Entity, GRect>();
            var ld = new Dictionary<Entity, string>();
            var txts = new List<Entity>();
            var lines = new List<Entity>();
            var pipes = new List<Entity>();
            foreach (var e in adb.ModelSpace.OfType<Circle>().Where(c => Convert.ToInt32(c.Radius) == 50).ToList())
            {
                //Dbg.ShowWhere(e);
                pipes.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-RAIN-EQPM").Where(x => ThRainSystemService.IsTianZhengElement(x)))
            {
                //Dbg.ShowWhere(e);
                pipes.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            foreach (var e in adb.ModelSpace.OfType<DBText>().ToList())
            {
                if (ThRainSystemService.IsWantedLabelText(e.TextString))
                {
                    //Dbg.ShowWhere(e);
                    txts.Add(e);
                    d[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
            }

            foreach (var e in adb.ModelSpace.OfType<Line>().Where(line => line.Length > 0).ToList())
            {
                //Dbg.ShowLine(e);
                lines.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }

            var gs = ThRainSystemService.GroupLines(lines);
            foreach (var g in gs)
            {
                //DU.DrawBoundaryLazy(g.ToArray());
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
                    DU.DrawTextLazy(lb, d[pipe].Center.ToPoint3d());
                }
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
                    }
                }
            }
            foreach (var pp in longPipes)
            {
                Dbg.ShowWhere(pp);
            }
        }
    }
}
namespace ThMEPWSS.DebugNs
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Linq2Acad;
    using ThMEPWSS.Assistant;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;

    public class quin3c
    {
        public static void quiqni()
        {
            var f = new Form();
            f.Paint += (s, e) =>
            {
                Bitmap bitmap = new Bitmap(@"Y:\xxx.gif");

                // Initialize the color matrix.
                float[][] matrixItems ={
 new float[] {0.4f, 0.2f, 0.4f, 0, 0},
 new float[] {0.4f, 0.2f, 0.4f, 0, 0},
 new float[] {0.4f, 0.2f, 0.4f, 0, 0},
 new float[] {0, 0, 0, 1, 0},
 new float[] {0, 0, 0, 0, 1}};
                ColorMatrix colorMatrix = new ColorMatrix(matrixItems);

                // Create an ImageAttributes object and set its color matrix.
                ImageAttributes imageAtt = new ImageAttributes();
                imageAtt.SetColorMatrix(
                colorMatrix,
                ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap);

                // Now draw the semitransparent bitmap image.
                int iWidth = bitmap.Width;
                int iHeight = bitmap.Height;
                f.Width = iWidth;
                f.Height = iHeight;
                e.Graphics.DrawImage(
                bitmap,
                new Rectangle(0, 0, iWidth, iHeight),
                    0.0f,
                    0.0f,
                    iWidth,
                    iHeight,
                    GraphicsUnit.Pixel,
                imageAtt);
            }; f.ShowDialog();
        }
        public static void quip92()
        {
            var f = new Form();
            f.Paint += (s, e) =>
            {
                Graphics g = f.CreateGraphics();
                Pen p = new Pen(Color.Blue, 1);
                for (int i = 0; i < 90; i++)
                {
                    g.RotateTransform(i);
                    g.DrawLine(p, 0, 0, 100, 0);
                    g.ResetTransform();
                }
                g.TranslateTransform(100, 100);
                g.DrawLine(p, 0, 0, 100, 0);
                g.ResetTransform();
                g.TranslateTransform(100, 200);
                for (int i = 0; i < 8; i++)
                {
                    g.RotateTransform(45);
                    g.DrawLine(p, 0, 0, 100, 0);
                }
                g.Dispose();
            }; f.ShowDialog();
        }
        public static void quinww()
        {
            var f = new Form();
            f.Paint += (s, e) =>
            {
                Graphics g = f.CreateGraphics();
                Rectangle rect = new Rectangle(10, 10, 50, 50);
                SolidBrush b1 = new SolidBrush(Color.Blue);//定义单色画刷
                g.FillRectangle(b1, rect);
                g.DrawString("字符串", new Font("宋体", 10), b1, new PointF(90, 10));
                TextureBrush b2 = new TextureBrush(Image.FromFile(@"Y:\xxx.gif"));
                rect.Location = new Point(10, 70);
                rect.Width = 200;
                rect.Height = 200;
                g.FillRectangle(b2, rect);
                rect.Location = new Point(10, 290);
                LinearGradientBrush b3 = new LinearGradientBrush(rect, Color.Yellow, Color.Black, LinearGradientMode.Horizontal);
                g.FillRectangle(b3, rect);
            }; f.ShowDialog();
        }
        public static void quinwv()
        {
            var f = new Form();
            f.Paint += (s, e) =>
            {
                Pen p = new Pen(Color.Blue, 5);
                Graphics g = f.CreateGraphics();
                p.DashStyle = DashStyle.Dot;
                g.DrawLine(p, 10, 10, 200, 10);
                p.DashPattern = new float[] { 2, 1 };
                g.DrawLine(p, 10, 20, 200, 20);
                p.DashStyle = DashStyle.Solid;
                p.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(p, 10, 30, 200, 30);
                g.Dispose();
                p.Dispose();
            }; f.ShowDialog();
        }
        public static void quinwx()
        {
            var f = new Form();
            f.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                Pen p = new Pen(Color.Blue, 2);
                g.DrawLine(p, 10, 10, 100, 100);
                g.DrawRectangle(p, 10, 10, 100, 100);
                g.DrawEllipse(p, 10, 10, 100, 100);
            }; f.ShowDialog();
        }
        public static void quinwy()
        {
            Bitmap bmp;
            Graphics g;
            string strString;
            bmp = new Bitmap(600, 400);
            g = Graphics.FromImage(bmp);
            g.Clear(Color.DarkBlue);
            Font objFont = new Font("Times", 24);
            strString = "ABCabc123 - AntiAlias";
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.DrawString(strString, objFont, Brushes.White, 10, 10);
            strString = "ABCabc123 - AntiAliasGridFit";
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.DrawString(strString, objFont, Brushes.White, 10, 50);
            strString = "ABCabc123 - ClearTypeGridFit";
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.DrawString(strString, objFont, Brushes.White, 10, 90);
            strString = "ABCabc123 - SingleBitPerPixel";
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
            g.DrawString(strString, objFont, Brushes.White, 10, 130);
            strString = "ABCabc123 - SingleBitPerPixelGridFit";
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
            g.DrawString(strString, objFont, Brushes.White, 10, 170);
            strString = "ABCabc123 - SystemDefault";
            g.TextRenderingHint = TextRenderingHint.SystemDefault;
            g.DrawString(strString, objFont, Brushes.White, 10, 210);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinwz()
        {
            Bitmap bmp;
            Graphics g;
            Pen objPen;
            Brush objBrush;
            Font objFont;
            bmp = new Bitmap(400, 400);
            g = Graphics.FromImage(bmp);
            objPen = new Pen(Color.Yellow);
            objBrush = new SolidBrush(Color.Yellow);
            objFont = new Font("Lucida Sans Unicode", 18);
            g.SmoothingMode = SmoothingMode.Default;
            g.DrawString("Default", objFont, objBrush, 50, 20);
            g.DrawEllipse(objPen, 10, 10, 200, 50);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawString("AntiAlias", objFont, objBrush, 50, 80);
            g.DrawEllipse(objPen, 10, 70, 200, 50);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawString("HighQuality", objFont, objBrush, 50, 140);
            g.DrawEllipse(objPen, 10, 130, 200, 50);
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.DrawString("HighSpeed", objFont, objBrush, 50, 200);
            g.DrawEllipse(objPen, 10, 190, 200, 50);
            g.SmoothingMode = SmoothingMode.None;
            g.DrawString("None", objFont, objBrush, 50, 260);
            g.DrawEllipse(objPen, 10, 250, 200, 50);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinx1()
        {
            Bitmap bmp = new Bitmap(600, 500);
            Graphics gph;
            gph = Graphics.FromImage(bmp);
            gph.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            gph.Clear(Color.Red);
            gph.DrawString("无换行显示:绘制文本换行显示的字符串,应该使用文本外部边界的长方形", new Font("宋体", 16), Brushes.Blue, 10, 10);
            RectangleF rect = new RectangleF(10, 110, 300, 200);
            string str = "绘制文本换行显示的字符串,应该使用文本外部边界的长方形";
            gph.DrawString(str, new Font("宋体", 16), Brushes.Blue, rect);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinx3()
        {
            Bitmap bmp = new Bitmap(600, 500);
            Graphics gph;
            gph = Graphics.FromImage(bmp);
            gph.SmoothingMode = SmoothingMode.HighQuality;
            gph.Clear(Color.Red);
            gph.DrawRectangle(Pens.Blue, 10, 10, 100, 100);
            gph.FillRectangle(Brushes.Blue, 120, 10, 100, 100);
            gph.DrawEllipse(Pens.Blue, 10, 120, 100, 100);
            gph.FillEllipse(Brushes.Blue, 120, 120, 100, 100);
            gph.DrawPie(Pens.Blue, 10, 230, 100, 100, 0, 270);
            gph.FillPie(Brushes.Blue, 120, 230, 100, 100, 0, 270);
            Point[] line = { new Point(10, 340), new Point(60, 30), new Point(110, 30), new Point(160, 340) };
            gph.DrawCurve(Pens.Blue, line);
            gph.DrawBezier(Pens.Blue, new Point(10, 340), new Point(60, 30), new Point(110, 30), new Point(160, 340));
            Point[] line2 = { new Point(10, 340), new Point(340, 100), new Point(190, 340), new Point(10, 340) };
            gph.DrawPolygon(Pens.Blue, line2);
            Bitmap mybit = new Bitmap(@"Y:\xxx.gif");
            gph.DrawImage(mybit, 10, 360, 100, 100);
            gph.DrawLine(Pens.Black, 10, 480, 300, 480);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinx4()
        {
            Bitmap bmp;
            Graphics objGraphics;
            Pen pen;
            bmp = new Bitmap(400, 300);
            objGraphics = Graphics.FromImage(bmp);
            pen = new Pen(Color.White);
            pen.Width = 10;
            pen.LineJoin = LineJoin.Bevel;
            objGraphics.DrawRectangle(pen, 10, 20, 350, 40);
            pen.LineJoin = LineJoin.Miter;
            objGraphics.DrawRectangle(pen, 10, 80, 350, 40);
            pen.LineJoin = LineJoin.MiterClipped;
            objGraphics.DrawRectangle(pen, 10, 140, 350, 40);
            pen.LineJoin = LineJoin.Round;
            objGraphics.DrawRectangle(pen, 10, 200, 350, 40);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinx5()
        {
            Bitmap bmp;
            Graphics objGraphics;
            Pen pen;
            bmp = new Bitmap(400, 200);
            objGraphics = Graphics.FromImage(bmp);
            pen = new Pen(Color.White);
            pen.Width = 6;
            pen.EndCap = LineCap.ArrowAnchor;
            objGraphics.DrawLine(pen, 10, 20, 350, 20);
            pen.EndCap = LineCap.DiamondAnchor;
            objGraphics.DrawLine(pen, 10, 40, 350, 40);
            pen.EndCap = LineCap.Flat;
            objGraphics.DrawLine(pen, 10, 60, 350, 60);
            pen.EndCap = LineCap.Round;
            objGraphics.DrawLine(pen, 10, 80, 350, 80);
            pen.EndCap = LineCap.RoundAnchor;
            objGraphics.DrawLine(pen, 10, 100, 350, 100);
            pen.EndCap = LineCap.Square;
            objGraphics.DrawLine(pen, 10, 120, 350, 120);
            pen.EndCap = LineCap.SquareAnchor;
            objGraphics.DrawLine(pen, 10, 140, 350, 140);
            pen.EndCap = LineCap.Triangle;
            objGraphics.DrawLine(pen, 10, 160, 350, 160);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinx6()
        {
            Bitmap bmp = new Bitmap(600, 120);
            Graphics gph;
            gph = Graphics.FromImage(bmp);
            gph.Clear(Color.Green);
            Pen mypen = new Pen(Color.Red, 2);
            mypen.DashStyle = DashStyle.Custom;
            gph.DrawLine(mypen, 10, 10, 300, 10);
            mypen.DashStyle = DashStyle.Dash;
            gph.DrawLine(mypen, 10, 30, 300, 30);
            mypen.DashStyle = DashStyle.DashDot;
            gph.DrawLine(mypen, 10, 50, 300, 50);
            mypen.DashStyle = DashStyle.DashDotDot;
            gph.DrawLine(mypen, 10, 70, 300, 70);
            mypen.DashStyle = DashStyle.Dot;
            gph.DrawLine(mypen, 10, 90, 300, 90);
            mypen.DashStyle = DashStyle.Solid;
            gph.DrawLine(mypen, 10, 110, 300, 110);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinap()
        {
            Bitmap bmp = new Bitmap(600, 120);
            Color mycolor;
            mycolor = Color.FromArgb(0, 0, 255);
            Pen mypen = new Pen(Color.Blue, 5);
            Graphics gph;
            gph = Graphics.FromImage(bmp);
            gph.Clear(Color.Red);
            gph.DrawRectangle(Pens.Green, 10, 10, 100, 100);
            gph.DrawRectangle(mypen, 120, 10, 100, 100);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinat()
        {
            Bitmap bmp = new Bitmap(600, 120);
            Bitmap img = new Bitmap(@"Y:\xxx.gif");
            Color mycolor;
            mycolor = Color.FromArgb(0, 0, 255);
            Rectangle rect = new Rectangle(300, 0, 100, 100);
            SolidBrush solid = new SolidBrush(mycolor);
            HatchBrush hatch = new HatchBrush(HatchStyle.Cross, Color.Orange);
            LinearGradientBrush linear;
            linear = new LinearGradientBrush(rect, Color.Red, Color.Yellow, LinearGradientMode.ForwardDiagonal);
            TextureBrush texture = new TextureBrush(img);
            Graphics gph;
            gph = Graphics.FromImage(bmp);
            gph.Clear(Color.Red);
            gph.FillRectangle(Brushes.Green, 0, 0, 100, 100);
            gph.FillRectangle(solid, 100, 0, 100, 100);
            gph.FillRectangle(hatch, 200, 0, 100, 100);
            gph.FillRectangle(linear, rect);
            gph.FillRectangle(texture, 400, 0, 100, 100);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        public static void quinax()
        {
            var bmp = new Bitmap(500, 400);
            var gph = Graphics.FromImage(bmp);
            gph.Clear(Color.Olive);
            gph.DrawRectangle(Pens.Red, 100, 100, 50, 50);
            gph.FillRectangle(Brushes.Green, 200, 200, 50, 50);
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
        }
        static string temp_file => @"Y:\" + DateTime.Now.Ticks;
        public static void quimxe()
        {
            var bmp = new Bitmap(400, 200);
            var rd = new Random();
            for (int i = 0; i < 1000; i++)
            {
                bmp.SetPixel(rd.Next(400), rd.Next(200), System.Drawing.Color.Red);
            }
            bmp.Save(temp_file + ".gif", ImageFormat.Gif);
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







namespace qtc3hs
{
    public class Matrix
    {

        public double[,] Mat;
        private long _m, _n;
        public long M
        {
            get
            {
                return _m;
            }
            private set
            {
                _m = value;
            }
        }
        public long N
        {
            get
            {
                return _n;
            }
            private set
            {
                _n = value;
            }
        }
        protected static Random rand = new Random((int)DateTime.Now.Ticks);

        public Matrix(long m, long n, bool isRandValue = false)
        {
            _m = m;
            _n = n;

            Mat = new double[m, n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    if (isRandValue)
                        Mat[i, j] = rand.NextDouble();
                    else Mat[i, j] = 0;
        }
        public Matrix(double[,] m)
        {
            _m = m.GetLongLength(0);
            _n = m.GetLongLength(1);
            Mat = m;
        }


        public static Matrix operator +(Matrix M1, Matrix M2)
        {
            if (M1.M != M2.M
             || M1.N != M1.N)
                throw new Exception("矩阵不符合运算条件，2个矩阵必须完全一样的行和列");

            Matrix result = new Matrix(M1.M, M1.N);
            for (int i = 0; i < M1.M; i++)
                for (int j = 0; j < M1.N; j++)
                {
                    result.Mat[i, j] = M1.Mat[i, j] + M2.Mat[i, j];
                }
            return result;
        }
        public static Matrix operator -(Matrix M1, Matrix M2)
        {
            if (M1.M != M2.M
             || M1.N != M1.N)
                throw new Exception("矩阵不符合运算条件，2个矩阵必须完全一样的行和列");

            Matrix result = new Matrix(M1.M, M1.N);
            for (int i = 0; i < M1.M; i++)
                for (int j = 0; j < M1.N; j++)
                {
                    result.Mat[i, j] = M1.Mat[i, j] - M2.Mat[i, j];
                }
            return result;
        }
        public static Matrix operator ^(Matrix M1, Matrix M2)
        {
            if (M1.M != M2.M
             || M1.N != M1.N)
                throw new Exception("矩阵不符合运算条件，2个矩阵必须完全一样的行和列");

            Matrix result = new Matrix(M1.M, M1.N);
            for (int i = 0; i < M1.M; i++)
                for (int j = 0; j < M1.N; j++)
                {
                    result.Mat[i, j] = M1.Mat[i, j] * M2.Mat[i, j];
                }
            return result;
        }

        public static Matrix operator *(Matrix M1, Matrix M2)
        {
            long m = M1.Mat.GetLongLength(0);
            long jW = M1.Mat.GetLongLength(1);

            long iH = M2.Mat.GetLongLength(0);
            long n = M2.Mat.GetLongLength(1);

            if (jW != iH)
                throw new Exception("矩阵不符合运算条件，W的行不等于H的列");
            Matrix result = new Matrix(m, n);

            for (int i = 0; i < m; i++)//W的行数
            {
                for (int j = 0; j < n; j++)//H的列数
                {

                    for (int k = 0; k < jW; k++)
                    {

                        result.Mat[i, j] += M2.Mat[k, j] * M1.Mat[i, k];
                    }
                }
            }

            return result;
        }
        public static Matrix operator *(Matrix M1, double ratio)
        {
            long m = M1.Mat.GetLongLength(0);
            long n = M1.Mat.GetLongLength(1);
            Matrix result = new Matrix(m, n);
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    result.Mat[i, j] = M1.Mat[i, j] * ratio;
            return result;
        }

        public Matrix Nonlin()
        {
            Matrix result = new Matrix(M, N);
            for (int i = 0; i < M; i++)
                for (int j = 0; j < N; j++)
                    result.Mat[i, j] = Sigmoid(Mat[i, j]);
            return result;
        }
        public Matrix Derivative()
        {
            Matrix result = new Matrix(M, N);
            for (int i = 0; i < M; i++)
                for (int j = 0; j < N; j++)
                    result.Mat[i, j] = Derivative(Mat[i, j]);
            return result;
        }
        public double Sigmoid(double x)
        {
            return (1 / (1 + Math.Exp(-3 * x)));
        }

        //求导
        public double Derivative(double x)
        {
            return (3 * x * (1 - x));
        }
        public Matrix T
        {
            get
            {
                Matrix result = new Matrix(N, M);

                //新矩阵生成规则： b[i,j]=a[j,i]
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        result.Mat[i, j] = this.Mat[j, i];
                    }
                }
                return result;
            }
        }
        public override string ToString()
        {
            StringBuilder sbd = new StringBuilder();
            for (int i = 0; i < this.M; i++)
            {
                for (int j = 0; j < this.N; j++)
                {
                    sbd.Append(Mat[i, j].ToString("N10"));
                    sbd.Append(",");
                }
                sbd.AppendLine();
            }
            return sbd.ToString();
        }

        public static void Test()
        {

            double[,] X = new double[4, 3] { { 0, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 0, 1, 1 } };
            double[,] y = new double[4, 1] { { 0 }, { 1 }, { 1 }, { 0 } };
            Matrix MatX = new Matrix(X);
            Matrix MatY = new Matrix(y);
            Matrix syn0 = new Matrix(3, 4, true);
            Matrix syn1 = new Matrix(4, 1, true);
            Matrix L1, L1_Delta;
            Matrix L2 = null, L2_Delta;
            Matrix L2_Err, L1_Err;
            for (long i = 0; i < 9000; i++)
            {
                L1 = (MatX * syn0).Nonlin();         //l1 = nonlin(np.dot(l0,syn0))
                L2 = (L1 * syn1).Nonlin();           //l2 = nonlin(np.dot(l1,syn1))
                L2_Err = MatY - L2;                  //L2_error = y - l2
                L2_Delta = L2_Err ^ L2.Derivative(); //l2_delta = l2_error*nonlin(l2,deriv=True)
                L1_Err = L2_Delta * syn1.T;          //l1_error = l2_delta.dot(syn1.T)
                L1_Delta = L1_Err ^ L1.Derivative(); //l1_delta = l1_error * nonlin(l1,deriv=True)
                syn1 = syn1 + L1.T * L2_Delta * 0.01;//l1.T.dot(l2_delta)
                syn0 = syn0 + MatX.T * L1_Delta * 0.01;//l0.T.dot(l1_delta)
            }
            Console.WriteLine(L2.ToString());
            Console.WriteLine(syn0.ToString());
            Console.WriteLine(syn1.ToString());
        }
    }
}

namespace qtc49j
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// BpNet 的摘要说明。
    /// </summary>
    public class BpNet
    {
        public int inNum;//输入节点数
        int hideNum;//隐层节点数
        public int outNum;//输出层节点数
        public int sampleNum;//样本总数

        Random R;
        double[] x;//输入节点的输入数据
        double[] x1;//隐层节点的输出
        double[] x2;//输出节点的输出

        double[] o1;//隐层的输入
        double[] o2;//输出层的输入
        public double[,] w;//权值矩阵w
        public double[,] v;//权值矩阵V
        public double[,] dw;//权值矩阵w
        public double[,] dv;//权值矩阵V


        public double rate;//学习率
        public double[] b1;//隐层阈值矩阵
        public double[] b2;//输出层阈值矩阵
        public double[] db1;//隐层阈值矩阵
        public double[] db2;//输出层阈值矩阵

        double[] pp;//输出层的误差
        double[] qq;//隐层的误差
        double[] yd;//输出层的教师数据
        public double e;//均方误差
        double in_rate;//归一化比例系数

        public int computeHideNum(int m, int n)
        {
            double s = Math.Sqrt(0.43 * m * n + 0.12 * n * n + 2.54 * m + 0.77 * n + 0.35) + 0.51;
            int ss = Convert.ToInt32(s);
            return ((s - (double)ss) > 0.5) ? ss + 1 : ss;

        }
        public BpNet(double[,] p, double[,] t)
        {

            // 构造函数逻辑
            R = new Random();

            this.inNum = p.GetLength(1);
            this.outNum = t.GetLength(1);
            this.hideNum = computeHideNum(inNum, outNum);
            //      this.hideNum=18;
            this.sampleNum = p.GetLength(0);

            Console.WriteLine("输入节点数目： " + inNum);
            Console.WriteLine("隐层节点数目：" + hideNum);
            Console.WriteLine("输出层节点数目：" + outNum);

            Console.ReadLine();

            x = new double[inNum];
            x1 = new double[hideNum];
            x2 = new double[outNum];

            o1 = new double[hideNum];
            o2 = new double[outNum];

            w = new double[inNum, hideNum];
            v = new double[hideNum, outNum];
            dw = new double[inNum, hideNum];
            dv = new double[hideNum, outNum];

            b1 = new double[hideNum];
            b2 = new double[outNum];
            db1 = new double[hideNum];
            db2 = new double[outNum];

            pp = new double[hideNum];
            qq = new double[outNum];
            yd = new double[outNum];

            //初始化w
            for (int i = 0; i < inNum; i++)
            {
                for (int j = 0; j < hideNum; j++)
                {
                    w[i, j] = (R.NextDouble() * 2 - 1.0) / 2;
                }
            }

            //初始化v
            for (int i = 0; i < hideNum; i++)
            {
                for (int j = 0; j < outNum; j++)
                {
                    v[i, j] = (R.NextDouble() * 2 - 1.0) / 2;
                }
            }

            rate = 0.8;
            e = 0.0;
            in_rate = 1.0;
        }

        //训练函数
        public void train(double[,] p, double[,] t)
        {
            e = 0.0;
            //求p，t中的最大值
            double pMax = 0.0;
            for (int isamp = 0; isamp < sampleNum; isamp++)
            {
                for (int i = 0; i < inNum; i++)
                {
                    if (Math.Abs(p[isamp, i]) > pMax)
                    {
                        pMax = Math.Abs(p[isamp, i]);
                    }
                }

                for (int j = 0; j < outNum; j++)
                {
                    if (Math.Abs(t[isamp, j]) > pMax)
                    {
                        pMax = Math.Abs(t[isamp, j]);
                    }
                }

                in_rate = pMax;
            }//end isamp



            for (int isamp = 0; isamp < sampleNum; isamp++)
            {
                //数据归一化
                for (int i = 0; i < inNum; i++)
                {
                    x[i] = p[isamp, i] / in_rate;
                }
                for (int i = 0; i < outNum; i++)
                {
                    yd[i] = t[isamp, i] / in_rate;
                }

                //计算隐层的输入和输出

                for (int j = 0; j < hideNum; j++)
                {
                    o1[j] = 0.0;
                    for (int i = 0; i < inNum; i++)
                    {
                        o1[j] += w[i, j] * x[i];
                    }
                    x1[j] = 1.0 / (1.0 + Math.Exp(-o1[j] - b1[j]));
                }

                //计算输出层的输入和输出
                for (int k = 0; k < outNum; k++)
                {
                    o2[k] = 0.0;
                    for (int j = 0; j < hideNum; j++)
                    {
                        o2[k] += v[j, k] * x1[j];
                    }
                    x2[k] = 1.0 / (1.0 + Math.Exp(-o2[k] - b2[k]));
                }

                //计算输出层误差和均方差

                for (int k = 0; k < outNum; k++)
                {
                    qq[k] = (yd[k] - x2[k]) * x2[k] * (1.0 - x2[k]);
                    e += (yd[k] - x2[k]) * (yd[k] - x2[k]);
                    //更新V
                    for (int j = 0; j < hideNum; j++)
                    {
                        v[j, k] += rate * qq[k] * x1[j];
                    }
                }

                //计算隐层误差

                for (int j = 0; j < hideNum; j++)
                {
                    pp[j] = 0.0;
                    for (int k = 0; k < outNum; k++)
                    {
                        pp[j] += qq[k] * v[j, k];
                    }
                    pp[j] = pp[j] * x1[j] * (1 - x1[j]);

                    //更新W

                    for (int i = 0; i < inNum; i++)
                    {
                        w[i, j] += rate * pp[j] * x[i];
                    }
                }

                //更新b2
                for (int k = 0; k < outNum; k++)
                {
                    b2[k] += rate * qq[k];
                }

                //更新b1
                for (int j = 0; j < hideNum; j++)
                {
                    b1[j] += rate * pp[j];
                }

            }//end isamp
            e = Math.Sqrt(e);
            //      adjustWV(w,dw);
            //      adjustWV(v,dv);


        }//end train

        public void adjustWV(double[,] w, double[,] dw)
        {
            for (int i = 0; i < w.GetLength(0); i++)
            {
                for (int j = 0; j < w.GetLength(1); j++)
                {
                    w[i, j] += dw[i, j];
                }
            }

        }

        public void adjustWV(double[] w, double[] dw)
        {
            for (int i = 0; i < w.Length; i++)
            {

                w[i] += dw[i];

            }

        }

        //数据仿真函数

        public double[] sim(double[] psim)
        {
            for (int i = 0; i < inNum; i++)
                x[i] = psim[i] / in_rate;

            for (int j = 0; j < hideNum; j++)
            {
                o1[j] = 0.0;
                for (int i = 0; i < inNum; i++)
                    o1[j] = o1[j] + w[i, j] * x[i];
                x1[j] = 1.0 / (1.0 + Math.Exp(-o1[j] - b1[j]));
            }
            for (int k = 0; k < outNum; k++)
            {
                o2[k] = 0.0;
                for (int j = 0; j < hideNum; j++)
                    o2[k] = o2[k] + v[j, k] * x1[j];
                x2[k] = 1.0 / (1.0 + Math.Exp(-o2[k] - b2[k]));

                x2[k] = in_rate * x2[k];

            }

            return x2;
        } //end sim

        //保存矩阵w,v
        public void saveMatrix(double[,] w, string filename)
        {
            StreamWriter sw = File.CreateText(filename);
            for (int i = 0; i < w.GetLength(0); i++)
            {
                for (int j = 0; j < w.GetLength(1); j++)
                {
                    sw.Write(w[i, j] + " ");
                }
                sw.WriteLine();
            }
            sw.Close();

        }

        //保存矩阵b1,b2
        public void saveMatrix(double[] b, string filename)
        {
            StreamWriter sw = File.CreateText(filename);
            for (int i = 0; i < b.Length; i++)
            {
                sw.Write(b[i] + " ");
            }
            sw.Close();
        }

        //读取矩阵W,V
        public void readMatrixW(double[,] w, string filename)
        {

            StreamReader sr;
            try
            {

                sr = new StreamReader(filename, Encoding.GetEncoding("gb2312"));

                String line;
                int i = 0;

                while ((line = sr.ReadLine()) != null)
                {

                    string[] s1 = line.Trim().Split(' ');
                    for (int j = 0; j < s1.Length; j++)
                    {
                        w[i, j] = Convert.ToDouble(s1[j]);
                    }
                    i++;
                }
                sr.Close();

            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

        }




        //读取矩阵b1,b2
        public void readMatrixB(double[] b, string filename)
        {

            StreamReader sr;
            try
            {
                sr = new StreamReader(filename, Encoding.GetEncoding("gb2312"));

                String line;
                int i = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    b[i] = Convert.ToDouble(line);
                    i++;
                }
                sr.Close();

            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

        }



    }
    class Class1
    {
        [STAThread]
        static void qtc4a9(string[] args)
        {
            //0.1399,0.1467,0.1567,0.1595,0.1588,0.1622,0.1611,0.1615,0.1685,0.1789,0.1790

            //      double [,] p1=new double[,]{{0.05,0.02},{0.09,0.11},{0.12,0.20},{0.15,0.22},{0.20,0.25},{0.75,0.75},{0.80,0.83},{0.82,0.80},{0.90,0.89},{0.95,0.89},{0.09,0.04},{0.1,0.1},{0.14,0.21},{0.18,0.24},{0.22,0.28},{0.77,0.78},{0.79,0.81},{0.84,0.82},{0.94,0.93},{0.98,0.99}};
            //      double [,] t1=new double[,]{{1,0},{1,0},{1,0},{1,0},{1,0},{0,1},{0,1},{0,1},{0,1},{0,1},{1,0},{1,0},{1,0},{1,0},{1,0},{0,1},{0,1},{0,1},{0,1},{0,1}};
            double[,] p1 = new double[,] { { 0.1399, 0.1467, 0.1567, 0.1595, 0.1588, 0.1622 }, { 0.1467, 0.1567, 0.1595, 0.1588, 0.1622, 0.1611 }, { 0.1567, 0.1595, 0.1588, 0.1622, 0.1611, 0.1615 }, { 0.1595, 0.1588, 0.1622, 0.1611, 0.1615, 0.1685 }, { 0.1588, 0.1622, 0.1611, 0.1615, 0.1685, 0.1789 } };
            double[,] t1 = new double[,] { { 0.1622 }, { 0.1611 }, { 0.1615 }, { 0.1685 }, { 0.1789 }, { 0.1790 } };
            BpNet bp = new BpNet(p1, t1);
            int study = 0;
            do
            {
                study++;
                bp.train(p1, t1);
                //       bp.rate=0.95-(0.95-0.3)*study/50000;
                //        Console.Write("第 "+ study+"次学习： ");
                //        Console.WriteLine(" 均方差为 "+bp.e);

            } while (bp.e > 0.001 && study < 50000);
            Console.Write("第 " + study + "次学习： ");
            Console.WriteLine(" 均方差为 " + bp.e);
            bp.saveMatrix(bp.w, "w.txt");
            bp.saveMatrix(bp.v, "v.txt");
            bp.saveMatrix(bp.b1, "b1.txt");
            bp.saveMatrix(bp.b2, "b2.txt");

            //      double [,] p2=new double[,]{{0.05,0.02},{0.09,0.11},{0.12,0.20},{0.15,0.22},{0.20,0.25},{0.75,0.75},{0.80,0.83},{0.82,0.80},{0.90,0.89},{0.95,0.89},{0.09,0.04},{0.1,0.1},{0.14,0.21},{0.18,0.24},{0.22,0.28},{0.77,0.78},{0.79,0.81},{0.84,0.82},{0.94,0.93},{0.98,0.99}};
            double[,] p2 = new double[,] { { 0.1399, 0.1467, 0.1567, 0.1595, 0.1588, 0.1622 }, { 0.1622, 0.1611, 0.1615, 0.1685, 0.1789, 0.1790 } };
            int aa = bp.inNum;
            int bb = bp.outNum;
            int cc = p2.GetLength(0);
            double[] p21 = new double[aa];
            double[] t2 = new double[bb];
            for (int n = 0; n < cc; n++)
            {
                for (int i = 0; i < aa; i++)
                {
                    p21[i] = p2[n, i];
                }
                t2 = bp.sim(p21);

                for (int i = 0; i < t2.Length; i++)
                {
                    Console.WriteLine(t2[i] + " ");
                }

            }

            Console.ReadLine();
        }
    }
}


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
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using System.Linq.Expressions;
    public class _nzm
    {
        private _nzm() { }
        static _nzm __nzm;
        public static _nzm nzm
        {
            get
            {
                if (!Dbg.isDebugging) return null;
                if (__nzm == null)
                {
                    var o = new _nzm();
                    var types = ((Assembly)Dbg.ctx["nozomi"]).GetTypes().Where(x => x.IsPublic && !x.IsEnum).ToList();
                    var baseTypeDict = new Dictionary<Type, string>()
                {
                    {typeof(byte),"byte"} ,
                    {typeof(sbyte),"sbyte"} ,
                    {typeof(ushort),"ushort"} ,
                    {typeof(short),"short"} ,
                    {typeof(uint),"uint"} ,
                    {typeof(int),"int"} ,
                    {typeof(ulong),"ulong"} ,
                    {typeof(long),"long"} ,
                    {typeof(string),"string"} ,
                    {typeof(object),"object"} ,
                    {typeof(void),"void"} ,
                    {typeof(byte[]),"byte[]"} ,
                    {typeof(float),"float"} ,
                    {typeof(double),"double"} ,
                    {typeof(decimal),"decimal"} ,
                    {typeof(DateTime),"DateTime"} ,
                };
                    foreach (var type in types)
                    {
                        var d = baseTypeDict;
                        var mis = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                        foreach (var mi in mis)
                        {
                            if (Regex.IsMatch(mi.Name, @"[\+`<>]")) continue;
                            if (!(d.ContainsKey(mi.ReturnType) && mi.GetParameters().All(p => d.ContainsKey(p.ParameterType)))) continue;
                            o.Init(mi);
                        }
                    }
                    __nzm = o;
                }
                return __nzm;
            }
        }
        public int Copy(string sourceFile, string targetFile) => f0(sourceFile, targetFile);
        public int Move(string sourceFile, string targetFile) => f1(sourceFile, targetFile);
        public int Delete(string filePath) => f2(filePath);
        public int GetLastError() => f3();
        public string GetErrorString() => f4();
        public string GetErrorString(int n) => f5(n);
        public void KillProcess(string preciseName) => f6(preciseName);
        public void OpenDir(string dir) => f7(dir);
        public void OpenFile(string file) => f8(file);
        public void Open(string path) => f9(path);
        public string RunAndRead(string fileName, string args) => f10(fileName, args);
        public byte[] AesEncrypt(byte[] bytes, string key) => f11(bytes, key);
        public string AesEncrypt(string str, string key) => f12(str, key);
        public byte[] AesDecrypt(byte[] encryptedBytes, string key) => f13(encryptedBytes, key);
        public string AesDecrypt(string Base64Str, string key) => f14(Base64Str, key);
        public uint GetCrc32(byte[] bytes, int start, int count) => f15(bytes, start, count);
        public byte[] EncryptData(byte[] data, string publickeyXml) => f16(data, publickeyXml);
        public byte[] DecryptData(byte[] data, string privatekeyXml) => f17(data, privatekeyXml);
        public byte[] Sign(byte[] data, string privatekeyXml) => f18(data, privatekeyXml);
        public string GetMD5StringFromFile(string filePath) => f19(filePath);
        public string GetSHA1StringFromFile(string filePath) => f20(filePath);
        public string GetMd5StringFromString(string str) => f21(str);
        public string GetMd5String(byte[] bytes) => f22(bytes);
        public byte[] GetMd5Bytes(byte[] bytes) => f23(bytes);
        public string Base16To10(string colorStr) => f24(colorStr);
        public string Base10To16(string colorStr) => f25(colorStr);
        public void SetHtmlText(string html, string plainText) => f26(html, plainText);
        public string GetText() => f27();
        public void JoinWith(string str) => f28(str);
        public void SetText(string text) => f29(text);
        public void Test() => f30();
        public int memcmp(byte[] b1, byte[] b2, int count) => f31(b1, b2, count);
        public int memcmp(byte[] b1, byte[] b2, uint count) => f32(b1, b2, count);
        public int memcpy(byte[] dst, byte[] src, int count) => f33(dst, src, count);
        public int memcpy(byte[] dst, byte[] src, uint count) => f34(dst, src, count);
        public int memset(byte[] p, int x, int count) => f35(p, x, count);
        public int memset(byte[] p, int x, uint count) => f36(p, x, count);
        public string ReadTextFile(string fileName, int limitedSize) => f37(fileName, limitedSize);
        public void WriteUtf8BOMTextFile(string str, string filePath) => f38(str, filePath);
        public void WriteGbkTextFile(string str, string filePath) => f39(str, filePath);
        public byte[] EncodeUTF8(string str) => f40(str);
        public byte[] EncodeUTF16LE(string str) => f41(str);
        public double ToUtcTime_Double(DateTime time) => f42(time);
        public double ToUtcTime_Double_Milliseconds(DateTime time) => f43(time);
        public int ToUtcTime_Int(DateTime time) => f44(time);
        public long ToUtcTime_Long(DateTime time) => f45(time);
        public long ToUtcTime_Milliseconds(DateTime time) => f46(time);
        public long ToUtfTime_Ticks(DateTime time) => f47(time);
        public DateTime ToDateTimeFromUtc(double time) => f48(time);
        public DateTime ToDateTimeFromUtf_Milliseconds(long time) => f49(time);
        public DateTime ToDateTimeFromUtf_Ticks(long time) => f50(time);
        public DateTime ToDateTimeFrunUtf(int time) => f51(time);
        public DateTime ToDateTimeFromUtf(long time) => f52(time);
        public int get_NowUtcSeconds() => f53();
        public long get_NowUtcMilliSeconds() => f54();
        public long get_NowTicks() => f55();
        public void MkDir(string dir) => f56(dir);
        public string GetFullPath(string path) => f57(path);
        public string GetParentPath(string path) => f58(path);
        public string ReName(string path, string newName) => f59(path, newName);
        public string UrlJoin(string baseUrl, string url) => f60(baseUrl, url);
        public string FromHtmlText(string html) => f61(html);
        public string ToHtmlText(string text) => f62(text);
        public string GetFileMd5(string filePath) => f63(filePath);
        public string CutEnd(string str, int n) => f64(str, n);
        public string ReadFromFile(string filePath, int limitedSize) => f65(filePath, limitedSize);
        public byte[] EncodeUtf8(string str) => f66(str);
        public string DecodeUtf8(byte[] bytes) => f67(bytes);
        public byte[] EncodeUnicode(string str) => f68(str);
        public string DecodeUnicode(byte[] bytes) => f69(bytes);
        public byte[] EncodeGbk(string str) => f70(str);
        public string DecodeGbk(byte[] bytes) => f71(bytes);
        public void AssertNotNull(object obj) => f72(obj);
        public void AssertNotNullOrEmpty(string str) => f73(str);
        public void AssertNotNullOrWhiteSpace(string str) => f74(str);
        public string GetStandardPathString(string path) => f75(path);
        public string EscapeFromFilePathChars(string path) => f76(path);
        public string GetExtension(string name) => f77(name);
        public string GetNameWithoutExtension(string fullname) => f78(fullname);
        public string GetName(string path) => f79(path);
        public string wpj(string str, string str2) => f80(str, str2);
        public string lpj(string str, string str2) => f81(str, str2);
        public string ToWpt(string str) => f82(str);
        public string ToLpt(string str) => f83(str);
        public string ToStdWinFullPath(string path) => f84(path);
        public void AssertNotExists(string path) => f85(path);
        public void AssertIsDir(string dir) => f86(dir);
        public void AssertIsEmptyDir(string dir) => f87(dir);
        public void AssertIsFile(string file) => f88(file);
        public string ToAnsiString(byte[] bytes) => f89(bytes);
        public string ToAnsiString(byte[] bytes, int start, int length) => f90(bytes, start, length);
        public string ToAsciiString(byte[] bytes) => f91(bytes);
        public string ToAsciiString(byte[] bytes, int start, int length) => f92(bytes, start, length);
        public string ToUtf8String(byte[] bytes) => f93(bytes);
        public string ToUtf8String(byte[] bytes, int start, int length) => f94(bytes, start, length);
        public string ToUnicodeString(byte[] bytes) => f95(bytes);
        public string ToUnicodeString(byte[] bytes, int start, int length) => f96(bytes, start, length);
        public string ToHexString(byte[] bytes, int start, int length) => f97(bytes, start, length);
        public string ToHexString(byte[] bytes) => f98(bytes);
        public string ToBase64String(byte[] bytes, int start, int length) => f99(bytes, start, length);
        public string ToBase64String(byte[] bytes) => f100(bytes);
        public byte[] ToByteArrayFromHexString(string hexStr) => f101(hexStr);
        public byte[] Base64Decode(string Base64Str) => f102(Base64Str);

        Func<string, string, int> f0;
        Func<string, string, int> f1;
        Func<string, int> f2;
        Func<int> f3;
        Func<string> f4;
        Func<int, string> f5;
        Action<string> f6;
        Action<string> f7;
        Action<string> f8;
        Action<string> f9;
        Func<string, string, string> f10;
        Func<byte[], string, byte[]> f11;
        Func<string, string, string> f12;
        Func<byte[], string, byte[]> f13;
        Func<string, string, string> f14;
        Func<byte[], int, int, uint> f15;
        Func<byte[], string, byte[]> f16;
        Func<byte[], string, byte[]> f17;
        Func<byte[], string, byte[]> f18;
        Func<string, string> f19;
        Func<string, string> f20;
        Func<string, string> f21;
        Func<byte[], string> f22;
        Func<byte[], byte[]> f23;
        Func<string, string> f24;
        Func<string, string> f25;
        Action<string, string> f26;
        Func<string> f27;
        Action<string> f28;
        Action<string> f29;
        Action f30;
        Func<byte[], byte[], int, int> f31;
        Func<byte[], byte[], uint, int> f32;
        Func<byte[], byte[], int, int> f33;
        Func<byte[], byte[], uint, int> f34;
        Func<byte[], int, int, int> f35;
        Func<byte[], int, uint, int> f36;
        Func<string, int, string> f37;
        Action<string, string> f38;
        Action<string, string> f39;
        Func<string, byte[]> f40;
        Func<string, byte[]> f41;
        Func<DateTime, double> f42;
        Func<DateTime, double> f43;
        Func<DateTime, int> f44;
        Func<DateTime, long> f45;
        Func<DateTime, long> f46;
        Func<DateTime, long> f47;
        Func<double, DateTime> f48;
        Func<long, DateTime> f49;
        Func<long, DateTime> f50;
        Func<int, DateTime> f51;
        Func<long, DateTime> f52;
        Func<int> f53;
        Func<long> f54;
        Func<long> f55;
        Action<string> f56;
        Func<string, string> f57;
        Func<string, string> f58;
        Func<string, string, string> f59;
        Func<string, string, string> f60;
        Func<string, string> f61;
        Func<string, string> f62;
        Func<string, string> f63;
        Func<string, int, string> f64;
        Func<string, int, string> f65;
        Func<string, byte[]> f66;
        Func<byte[], string> f67;
        Func<string, byte[]> f68;
        Func<byte[], string> f69;
        Func<string, byte[]> f70;
        Func<byte[], string> f71;
        Action<object> f72;
        Action<string> f73;
        Action<string> f74;
        Func<string, string> f75;
        Func<string, string> f76;
        Func<string, string> f77;
        Func<string, string> f78;
        Func<string, string> f79;
        Func<string, string, string> f80;
        Func<string, string, string> f81;
        Func<string, string> f82;
        Func<string, string> f83;
        Func<string, string> f84;
        Action<string> f85;
        Action<string> f86;
        Action<string> f87;
        Action<string> f88;
        Func<byte[], string> f89;
        Func<byte[], int, int, string> f90;
        Func<byte[], string> f91;
        Func<byte[], int, int, string> f92;
        Func<byte[], string> f93;
        Func<byte[], int, int, string> f94;
        Func<byte[], string> f95;
        Func<byte[], int, int, string> f96;
        Func<byte[], int, int, string> f97;
        Func<byte[], string> f98;
        Func<byte[], int, int, string> f99;
        Func<byte[], string> f100;
        Func<string, byte[]> f101;
        Func<string, byte[]> f102;

        void Init(MethodInfo mi)
        {
            var k = mi.ToString();
            if (k == "Int32 Copy(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f0 = Expression.Lambda<Func<string, string, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Int32 Move(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f1 = Expression.Lambda<Func<string, string, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Int32 Delete(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f2 = Expression.Lambda<Func<string, int>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Int32 GetLastError()")
            {
                f3 = Expression.Lambda<Func<int>>(Expression.Block(Expression.Call(null, mi))).Compile();
                return;
            }
            if (k == "System.String GetErrorString()")
            {
                f4 = Expression.Lambda<Func<string>>(Expression.Block(Expression.Call(null, mi))).Compile();
                return;
            }
            if (k == "System.String GetErrorString(Int32)")
            {
                var pe0 = Expression.Parameter(typeof(int), "v0");
                f5 = Expression.Lambda<Func<int, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void KillProcess(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f6 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void OpenDir(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f7 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void OpenFile(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f8 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void Open(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f9 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String RunAndRead(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f10 = Expression.Lambda<Func<string, string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Byte[] AesEncrypt(Byte[], System.String)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f11 = Expression.Lambda<Func<byte[], string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String AesEncrypt(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f12 = Expression.Lambda<Func<string, string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Byte[] AesDecrypt(Byte[], System.String)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f13 = Expression.Lambda<Func<byte[], string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String AesDecrypt(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f14 = Expression.Lambda<Func<string, string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "UInt32 GetCrc32(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f15 = Expression.Lambda<Func<byte[], int, int, uint>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "Byte[] EncryptData(Byte[], System.String)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f16 = Expression.Lambda<Func<byte[], string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Byte[] DecryptData(Byte[], System.String)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f17 = Expression.Lambda<Func<byte[], string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Byte[] Sign(Byte[], System.String)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f18 = Expression.Lambda<Func<byte[], string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String GetMD5StringFromFile(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f19 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetSHA1StringFromFile(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f20 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetMd5StringFromString(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f21 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetMd5String(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f22 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Byte[] GetMd5Bytes(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f23 = Expression.Lambda<Func<byte[], byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String Base16To10(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f24 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String Base10To16(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f25 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void SetHtmlText(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f26 = Expression.Lambda<Action<string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String GetText()")
            {
                f27 = Expression.Lambda<Func<string>>(Expression.Block(Expression.Call(null, mi))).Compile();
                return;
            }
            if (k == "Void JoinWith(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f28 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void SetText(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f29 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void Test()")
            {
                f30 = Expression.Lambda<Action>(Expression.Block(Expression.Call(null, mi))).Compile();
                return;
            }
            if (k == "Int32 memcmp(Byte[], Byte[], Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(byte[]), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f31 = Expression.Lambda<Func<byte[], byte[], int, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "Int32 memcmp(Byte[], Byte[], UInt32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(byte[]), "v1");
                var pe2 = Expression.Parameter(typeof(uint), "v2");
                f32 = Expression.Lambda<Func<byte[], byte[], uint, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "Int32 memcpy(Byte[], Byte[], Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(byte[]), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f33 = Expression.Lambda<Func<byte[], byte[], int, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "Int32 memcpy(Byte[], Byte[], UInt32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(byte[]), "v1");
                var pe2 = Expression.Parameter(typeof(uint), "v2");
                f34 = Expression.Lambda<Func<byte[], byte[], uint, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "Int32 memset(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f35 = Expression.Lambda<Func<byte[], int, int, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "Int32 memset(Byte[], Int32, UInt32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(uint), "v2");
                f36 = Expression.Lambda<Func<byte[], int, uint, int>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "System.String ReadTextFile(System.String, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                f37 = Expression.Lambda<Func<string, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Void WriteUtf8BOMTextFile(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f38 = Expression.Lambda<Action<string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Void WriteGbkTextFile(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f39 = Expression.Lambda<Action<string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Byte[] EncodeUTF8(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f40 = Expression.Lambda<Func<string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Byte[] EncodeUTF16LE(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f41 = Expression.Lambda<Func<string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Double ToUtcTime_Double(System.DateTime)")
            {
                var pe0 = Expression.Parameter(typeof(DateTime), "v0");
                f42 = Expression.Lambda<Func<DateTime, double>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Double ToUtcTime_Double_Milliseconds(System.DateTime)")
            {
                var pe0 = Expression.Parameter(typeof(DateTime), "v0");
                f43 = Expression.Lambda<Func<DateTime, double>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Int32 ToUtcTime_Int(System.DateTime)")
            {
                var pe0 = Expression.Parameter(typeof(DateTime), "v0");
                f44 = Expression.Lambda<Func<DateTime, int>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Int64 ToUtcTime_Long(System.DateTime)")
            {
                var pe0 = Expression.Parameter(typeof(DateTime), "v0");
                f45 = Expression.Lambda<Func<DateTime, long>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Int64 ToUtcTime_Milliseconds(System.DateTime)")
            {
                var pe0 = Expression.Parameter(typeof(DateTime), "v0");
                f46 = Expression.Lambda<Func<DateTime, long>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Int64 ToUtfTime_Ticks(System.DateTime)")
            {
                var pe0 = Expression.Parameter(typeof(DateTime), "v0");
                f47 = Expression.Lambda<Func<DateTime, long>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.DateTime ToDateTimeFromUtc(Double)")
            {
                var pe0 = Expression.Parameter(typeof(double), "v0");
                f48 = Expression.Lambda<Func<double, DateTime>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.DateTime ToDateTimeFromUtf_Milliseconds(Int64)")
            {
                var pe0 = Expression.Parameter(typeof(long), "v0");
                f49 = Expression.Lambda<Func<long, DateTime>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.DateTime ToDateTimeFromUtf_Ticks(Int64)")
            {
                var pe0 = Expression.Parameter(typeof(long), "v0");
                f50 = Expression.Lambda<Func<long, DateTime>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.DateTime ToDateTimeFrunUtf(Int32)")
            {
                var pe0 = Expression.Parameter(typeof(int), "v0");
                f51 = Expression.Lambda<Func<int, DateTime>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.DateTime ToDateTimeFromUtf(Int64)")
            {
                var pe0 = Expression.Parameter(typeof(long), "v0");
                f52 = Expression.Lambda<Func<long, DateTime>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Int32 get_NowUtcSeconds()")
            {
                f53 = Expression.Lambda<Func<int>>(Expression.Block(Expression.Call(null, mi))).Compile();
                return;
            }
            if (k == "Int64 get_NowUtcMilliSeconds()")
            {
                f54 = Expression.Lambda<Func<long>>(Expression.Block(Expression.Call(null, mi))).Compile();
                return;
            }
            if (k == "Int64 get_NowTicks()")
            {
                f55 = Expression.Lambda<Func<long>>(Expression.Block(Expression.Call(null, mi))).Compile();
                return;
            }
            if (k == "Void MkDir(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f56 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetFullPath(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f57 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetParentPath(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f58 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ReName(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f59 = Expression.Lambda<Func<string, string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String UrlJoin(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f60 = Expression.Lambda<Func<string, string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String FromHtmlText(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f61 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToHtmlText(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f62 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetFileMd5(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f63 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String CutEnd(System.String, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                f64 = Expression.Lambda<Func<string, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String ReadFromFile(System.String, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                f65 = Expression.Lambda<Func<string, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "Byte[] EncodeUtf8(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f66 = Expression.Lambda<Func<string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String DecodeUtf8(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f67 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Byte[] EncodeUnicode(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f68 = Expression.Lambda<Func<string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String DecodeUnicode(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f69 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Byte[] EncodeGbk(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f70 = Expression.Lambda<Func<string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String DecodeGbk(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f71 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void AssertNotNull(System.Object)")
            {
                var pe0 = Expression.Parameter(typeof(object), "v0");
                f72 = Expression.Lambda<Action<object>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void AssertNotNullOrEmpty(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f73 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void AssertNotNullOrWhiteSpace(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f74 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetStandardPathString(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f75 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String EscapeFromFilePathChars(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f76 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetExtension(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f77 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetNameWithoutExtension(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f78 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String GetName(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f79 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String wpj(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f80 = Expression.Lambda<Func<string, string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String lpj(System.String, System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                var pe1 = Expression.Parameter(typeof(string), "v1");
                f81 = Expression.Lambda<Func<string, string, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
                return;
            }
            if (k == "System.String ToWpt(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f82 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToLpt(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f83 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToStdWinFullPath(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f84 = Expression.Lambda<Func<string, string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void AssertNotExists(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f85 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void AssertIsDir(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f86 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void AssertIsEmptyDir(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f87 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Void AssertIsFile(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f88 = Expression.Lambda<Action<string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToAnsiString(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f89 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToAnsiString(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f90 = Expression.Lambda<Func<byte[], int, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "System.String ToAsciiString(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f91 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToAsciiString(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f92 = Expression.Lambda<Func<byte[], int, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "System.String ToUtf8String(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f93 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToUtf8String(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f94 = Expression.Lambda<Func<byte[], int, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "System.String ToUnicodeString(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f95 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToUnicodeString(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f96 = Expression.Lambda<Func<byte[], int, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "System.String ToHexString(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f97 = Expression.Lambda<Func<byte[], int, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "System.String ToHexString(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f98 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "System.String ToBase64String(Byte[], Int32, Int32)")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                var pe1 = Expression.Parameter(typeof(int), "v1");
                var pe2 = Expression.Parameter(typeof(int), "v2");
                f99 = Expression.Lambda<Func<byte[], int, int, string>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
                return;
            }
            if (k == "System.String ToBase64String(Byte[])")
            {
                var pe0 = Expression.Parameter(typeof(byte[]), "v0");
                f100 = Expression.Lambda<Func<byte[], string>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Byte[] ToByteArrayFromHexString(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f101 = Expression.Lambda<Func<string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }
            if (k == "Byte[] Base64Decode(System.String)")
            {
                var pe0 = Expression.Parameter(typeof(string), "v0");
                f102 = Expression.Lambda<Func<string, byte[]>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
                return;
            }

        }
    }
}

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
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    public static class qtcb9x
    {
        public static void qtcc3d()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var br = Dbg.SelectEntity<BlockReference>(adb);
                var blkData = new ThBlockReferenceData(br.ObjectId);
                var ids = Util1.VisibleEntities(blkData);
                foreach (ObjectId id in ids)
                {
                    var e = adb.Element<Entity>(id);
                    //Dbg.PrintLine(e.GetType().ToString());
                    var ltr = adb.Layers.Element(e.Layer);
                    if (Util1.IsVisibleLayer(ltr))
                    {
                        var ee = e.GetTransformedCopy(blkData.BlockTransform);
                        DU.DrawRectLazy(ee.Bounds.ToGRect());
                        if (ee is Circle circle)
                        {
                            var circle1 = new Circle(circle.Center, Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, circle.Radius);
                            circle1.ColorIndex = 3;
                            //circle1.TransformBy(Matrix3d.Displacement(Dbg.SelectPoint() - Point3d.Origin));
                            circle1.SetDatabaseDefaults();
                            adb.ModelSpace.Add(circle1);
                        }
                    }
                }

            }
        }
        public static void qtcc2b()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var br = Dbg.SelectEntity<BlockReference>(adb);
                var blkData = new ThBlockReferenceData(br.ObjectId);
                var ids = Util1.VisibleEntities(blkData);
                foreach (ObjectId id in ids)
                {
                    var e = adb.Element<Entity>(id);
                    //Dbg.PrintLine(e.GetType().ToString());
                    var ltr = adb.Layers.Element(e.Layer);
                    if (Util1.IsVisibleLayer(ltr))
                    {
                        var ee = (Entity)e.Clone();
                        var eee = ee.GetTransformedCopy(blkData.BlockTransform);
                        //eee.TransformBy(Matrix3d.Displacement(Dbg.SelectPoint()-Point3d.Origin));
                        //eee.ColorIndex = 2;
                        //eee.SetDatabaseDefaults();
                        //adb.ModelSpace.Add(eee);
                        Dbg.PrintLine(eee.GetType().ToString());
                        DU.DrawRectLazy(eee.Bounds.ToGRect());
                        if (eee is Circle circle)
                        {
                            var circle1 = new Circle(circle.Center, Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, circle.Radius);
                            circle1.ColorIndex = 3;
                            //circle1.TransformBy(Matrix3d.Displacement(Dbg.SelectPoint() - Point3d.Origin));
                            circle1.SetDatabaseDefaults();
                            adb.ModelSpace.Add(circle1);
                        }
                    }
                }

            }
        }

        public static void qtcb6s()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var blkData = new ThBlockReferenceData(result.ObjectId);
                var objIds = Util1.VisibleEntities(blkData);

                for (int i = 0; i < objIds.Count; i++)
                {
                    var origin = acadDatabase.Element<Entity>(objIds[i]);
                    var ltr = acadDatabase.Layers.Element(origin.Layer);
                    if (Util1.IsVisibleLayer(ltr))
                    {
                        var copyEnt = ThCurveExtension.WashClone(origin as Curve);
                        var ent = copyEnt.GetTransformedCopy(blkData.BlockTransform);
                        ent.ColorIndex = 2;
                        ent.SetDatabaseDefaults();
                        acadDatabase.ModelSpace.Add(ent);
                        if (ent is Circle circle)
                        {
                            var circle1 = new Circle(circle.Center, Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, circle.Radius);
                            circle1.ColorIndex = 3;
                            circle1.SetDatabaseDefaults();
                            acadDatabase.ModelSpace.Add(circle1);
                        }
                    }
                }
            }
        }


    }
    public static class Util2
    {
        public static void qt8g37()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var line = new Line();
                Dbg.PrintLine(line.Id.ToString());
                Dbg.PrintLine(line.ObjectId.ToString());
                adb.ModelSpace.Add(line);
                Dbg.PrintLine(line.Id.ToString());
                Dbg.PrintLine(line.ObjectId.ToString());
            }
        }
        public static void qt8g38()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var e = Dbg.SelectEntity<Entity>(adb);
                Dbg.PrintLine(e.ObjectId.ToString());
            }
        }
        public static void CollectRainPortSymbol()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = new List<Entity>();
                ents.AddRange(adb.ModelSpace.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }
                foreach (var e in ents)
                {
                    DU.DrawRectLazy(getRealBoundaryForPipe(e));
                }
            }
        }
        public static void CollectFloorDrain()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = new List<Entity>();
                ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }
                foreach (var e in ents)
                {
                    DU.DrawRectLazy(getRealBoundaryForPipe(e));
                }
            }
        }
        public static void CollectSideWaterBuckets()
        {
            //没找到
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = new List<Entity>();
                ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name == "CYSD" || x.ToDataItem().EffectiveName == "CYSD"));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }
                foreach (var e in ents)
                {
                    //DU.DrawRectLazy(getRealBoundaryForPipe(e));
                }
            }
        }
        public static void CollectWaterWells()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = new List<Entity>();
                ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }
                foreach (var e in ents)
                {
                    //DU.DrawRectLazy(getRealBoundaryForPipe(e));
                }
            }
        }
        public static void CollectWrappingPipes()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = new List<Entity>();
                ents.AddRange(adb.ModelSpace.OfType<BlockReference>()
                 .Where(x => x.Layer == "W-BUSH")
                 );
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in ents)
                {
                    Dbg.ShowWhere(e);
                }
                foreach (var e in ents)
                {
                    //DU.DrawRectLazy(getRealBoundaryForPipe(e));
                }
            }
        }
        public static void CollectCondensePipes()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = new List<Entity>();
                ents.AddRange(adb.ModelSpace.OfType<Circle>()
                    .Where(c => c.Layer == "W-RAIN-EQPM")
                    .Where(c => 20 < c.Radius && c.Radius < 40));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in ents)
                {
                    //Dbg.ShowWhere(e);
                }
                foreach (var e in ents)
                {
                    DU.DrawRectLazy(getRealBoundaryForPipe(e));
                }
            }
        }
        public static void CollectVerticalPipes()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var pipes = new List<Entity>();
                var blockNameOfVerticalPipe = "带定位立管";
                pipes.AddRange(adb.ModelSpace.OfType<BlockReference>()
                 .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                 .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                    if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in pipes)
                {
                    //Dbg.ShowWhere(e);
                }
                foreach (var e in pipes)
                {
                    //DU.DrawRectLazy(getRealBoundaryForPipe(e));
                }
            }
        }
    }
    public static class Util1
    {
        public static IEnumerable<Entity> YieldVisibleEntities(AcadDatabase adb, BlockReference br)
        {
            if (br.ObjectId.IsNull) yield break;
            var blkData = new ThBlockReferenceData(br.ObjectId);
            var ids = Util1.VisibleEntities(blkData);
            foreach (ObjectId id in ids)
            {
                var e = adb.Element<Entity>(id);
                var ltr = adb.Layers.Element(e.Layer);
                if (Util1.IsVisibleLayer(ltr))
                {
                    var ee = e.GetTransformedCopy(blkData.BlockTransform);
                    yield return ee;
                }
            }
        }
        // Reference:
        //  https://adndevblog.typepad.com/autocad/2012/05/accessing-visible-entities-in-a-dynamic-block.html
        public static Dictionary<string, ObjectIdCollection> DynablockVisibilityStates(this ThBlockReferenceData blockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.HostDatabase))
            {
                var groups = new Dictionary<string, ObjectIdCollection>();
                var btr = acadDatabase.Blocks.ElementOrDefault(blockReference.EffectiveName);
                if (btr == null)
                {
                    return groups;
                }

                if (!btr.IsDynamicBlock)
                {
                    return groups;
                }

                if (btr.ExtensionDictionary.IsNull)
                {
                    return groups;
                }

                var dict = acadDatabase.Element<DBDictionary>(btr.ExtensionDictionary);
                if (!dict.Contains("ACAD_ENHANCEDBLOCK"))
                {
                    return groups;
                }

                ObjectId graphId = dict.GetAt("ACAD_ENHANCEDBLOCK");
                var parameterIds = graphId.acdbEntGetObjects((short)DxfCode.HardOwnershipId);
                foreach (object parameterId in parameterIds)
                {
                    ObjectId objId = (ObjectId)parameterId;
                    if (objId.ObjectClass.Name == "AcDbBlockVisibilityParameter")
                    {
                        var visibilityParam = objId.acdbEntGetTypedVals();
                        var enumerator = visibilityParam.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.TypeCode == 303)
                            {
                                string group = (string)enumerator.Current.Value;
                                enumerator.MoveNext();
                                int nbEntitiesInGroup = (int)enumerator.Current.Value;
                                var entities = new ObjectIdCollection();
                                for (int i = 0; i < nbEntitiesInGroup; ++i)
                                {
                                    enumerator.MoveNext();
                                    entities.Add((ObjectId)enumerator.Current.Value);
                                }
                                groups.Add(group, entities);
                            }
                        }
                        break;
                    }
                }
                return groups;
            }
        }


        public static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
        /// <summary>
        /// 提取动态块中当前可见性下可见的实体
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static ObjectIdCollection VisibleEntities(this ThBlockReferenceData blockReference)
        {
            var objs = new ObjectIdCollection();
            var visibilityStates = DynablockVisibilityStates(blockReference);
            var properties = blockReference.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(o => o.PropertyName == "可见性1");
            foreach (var property in properties)
            {
                visibilityStates.Where(o => o.Key == property.Value as string)
                    .ForEach(o => objs.Add(o.Value));
            }
            return objs;
        }

        public static void AddButton(string name, Action f)
        {
            FengDbgTest.qt8czw.AddButton(name, f);
        }
        public static void AddLazyAction(string name, Action<AcadDatabase> f)
        {
            FengDbgTest.qt8czw.AddButton(name, () =>
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);
                    f?.Invoke(adb);
                }
                Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
            });
        }
        public static void FindText()
        {
            qu690p((x, y) => x?.ToUpper() == y?.ToUpper());
        }
        public static void FindText_Contains()
        {
            qu690p((x, y) => x?.ToUpper().Contains(y.ToUpper()) ?? false);
        }
        static void qu690p(Func<string, string, bool> f)
        {
            Dbg.FocusMainWindow();
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                foreach (var e in adb.ModelSpace.OfType<DBText>())
                {
                    if (e.TextString.ToUpper() == rst.StringResult.ToUpper())
                    {
                        Dbg.ShowWhere(e);
                    }
                }

                {
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

                    foreach (var e in GetEntities().OfType<DBText>())
                    {
                        if (e.TextString.ToUpper() == rst.StringResult.ToUpper())
                        {
                            Dbg.ShowWhere(e);
                        }
                    }
                }

                {
                    foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => ThRainSystemService.IsTianZhengElement(x)))
                    {
                        foreach (var o in e.ExplodeToDBObjectCollection().OfType<Entity>().Where(x => ThRainSystemService.IsTianZhengElement(x)))
                        {
                            foreach (var j in o.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                if (j.TextString.ToUpper() == rst.StringResult.ToUpper())
                                {
                                    Dbg.ShowWhere(e);
                                }
                            }
                        }
                    }
                }

                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            //if (e.TextString.ToUpper() == rst.StringResult.ToUpper())
                            if (f(e.TextString, rst.StringResult))
                            {
                                Dbg.ShowWhere(e);
                            }
                        }
                    }
                }
            }
        }
        private static void FindText1()
        {
            Dbg.FocusMainWindow();
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var e in adb.ModelSpace.OfType<DBText>())
                {
                    if (e.TextString.ToUpper() == rst.StringResult.ToUpper())
                    {
                        Dbg.ShowWhere(e);
                    }
                }
            }
        }
        private static void FindText2()
        {
            Dbg.FocusMainWindow();
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

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

                foreach (var e in GetEntities().OfType<DBText>())
                {
                    if (e.TextString.ToUpper() == rst.StringResult.ToUpper())
                    {
                        Dbg.ShowWhere(e);
                    }
                }
            }
        }
        //  var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\佳兆业滨江新城\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
        private static void FindText3()
        {
            Dbg.FocusMainWindow();
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => ThRainSystemService.IsTianZhengElement(x)))
                {
                    foreach (var o in e.ExplodeToDBObjectCollection().OfType<Entity>().Where(x => ThRainSystemService.IsTianZhengElement(x)))
                    {
                        foreach (var j in o.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
                            if (j.TextString.ToUpper() == rst.StringResult.ToUpper())
                            {
                                Dbg.ShowWhere(e);
                            }
                        }
                    }
                }
            }
        }
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\FL1ASTSB_W20-8#楼-给排水及消防平面图.dwg";
        private static void FindText4()
        {
            Dbg.FocusMainWindow();
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                {
                    foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                    {
                        if (e.TextString.ToUpper() == rst.StringResult.ToUpper())
                        {
                            Dbg.ShowWhere(e);
                        }
                    }
                }
            }
        }
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\佳兆业滨江新城\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
        private static void qu0jqv()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "块"))
                {
                    var r = e.Bounds.ToGRect();
                    if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                    {
                        var pl = DU.DrawRectLazy(r);
                        pl.ConstantWidth = 100;
                        //Dbg.ShowWhere(r);
                    }
                }
            }
        }
        public static void GetWidthAndHeight()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);
                var r = Dbg.SelectEntity<BlockReference>(adb).Bounds.ToGRect();
                Dbg.ShowString((new { r.Width, r.Height }).ToJson());
            }
        }
        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210508\佳兆业滨江新城\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg";
        private static void qu0jo8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                //Dbg.ShowString(Dbg.SelectEntity<BlockReference>(adb).ExplodeToDBObjectCollection().OfType<BlockReference>().Where(x => x.Layer == "W-RAIN-EQPM" && x.ObjectId.IsValid).Count().ToString());
                foreach (var e in Dbg.SelectEntity<BlockReference>(adb).ExplodeToDBObjectCollection().OfType<BlockReference>().Where(x => x.Layer == "W-RAIN-EQPM"))
                {
                    Dbg.ShowWhere(e);
                }
            }

        }

        public static Dictionary<string, GRect> getRangeDict()
        {
            var s = @"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\FL1ASTSB_W20-8#楼-给排水及消防平面图.dwg
[-86890.0469313123,307099.525864313,291232.022571523,1911982.17383515]
E:\thepa_workingSpace\任务资料\任务2\210508\佳兆业滨江新城\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg
[425098.633234828,480818.177362539,608908.55827485,1031959.02502]
E:\thepa_workingSpace\任务资料\任务2\210508\湖北交投颐和华府\湖北交投颐和华府\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg
[1168984.93136568,263414.893244965,1800906.3291906,1396070.63377467]
E:\thepa_workingSpace\任务资料\任务2\210512\清江山水四期\清江山水四期\FS55TMPH_W20-地上给水排水平面图.dwg
[163408.963038944,299689.075161778,347612.302142453,1054733.96008933]
E:\thepa_workingSpace\任务资料\任务2\210512\庭瑞君越观澜三期\庭瑞君越观澜三期\fs57grhn_w20-地上给水排水平面图.dwg
[544848.592070914,398715.671620322,882239.935258409,1180866.18346052]
E:\thepa_workingSpace\任务资料\任务2\210512\澳海黄州府（二期）\澳海黄州府（二期）\FS5GMBXU_W20-地上给水排水平面图.dwg
[761300,119990,1006650,1041670]
E:\thepa_workingSpace\任务资料\任务2\210517\合景红莲湖项目_框线(1)\合景红莲湖项目_框线\FS55TD78_W20-73#-地上给水排水平面图.dwg
[49706.9100843431,112203.444592767,333792.455727039,845753.114303187]
E:\thepa_workingSpace\任务资料\任务2\210517\湖北交投颐和华府_框线(1)\湖北交投颐和华府_框线\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg
[1253475.1943434,193626.21944266,1841327.20086618,1333582.21300303]
E:\thepa_workingSpace\任务资料\任务2\210517\佳兆业滨江新城_框线(1)\佳兆业滨江新城_框线\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg
[377340.555263862,413410.520618514,627425.352888763,1063241.5321275]
E:\thepa_workingSpace\任务资料\任务2\210517\蓝光未来阅璟_框线(1)\蓝光未来阅璟_框线\FS5F8704_W20-地上给水排水平面图-送审版.dwg
[731430.596681312,230326.852448013,1083896.80211244,856257.035059903]
E:\thepa_workingSpace\任务资料\任务2\210517\蓝光钰泷府二期_框线(1)\蓝光钰泷府二期_框线\FS59P2BC_W20-地上给水排水平面图-副本.dwg
[480051.797006845,644183.416617674,625001.129664848,1170160.73062964]
E:\thepa_workingSpace\任务资料\任务2\210517\清江山水四期_框线(1)\清江山水四期_框线\FS55TMPH_W20-地上给水排水平面图.dwg
[210889.753832759,252675.713925077,344666.901518278,1010766.3099549]
E:\thepa_workingSpace\任务资料\任务2\210517\武汉二七滨江商务区南一片住宅地块_框线(1)\武汉二七滨江商务区南一片住宅地块_框线\FS5747SS_W20-地上给水排水平面图.dwg
[360466.545942936,383904.076920602,622484.468922399,1292530.18108956]";
            var lines = s.Replace("\r", "").Split('\n');
            var d = new Dictionary<string, GRect>();
            for (int i = 0; i < lines.Length / 2; i++)
            {
                d[lines[i * 2]] = lines[i * 2 + 1].JsonToGRect();
            }
            return d;
        }
        public static string[] getFiles()
        {
            return new string[]{
@"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\FL1ASTSB_W20-8#楼-给排水及消防平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210508\佳兆业滨江新城\佳兆业滨江新城\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210508\湖北交投颐和华府\湖北交投颐和华府\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210512\清江山水四期\清江山水四期\FS55TMPH_W20-地上给水排水平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210512\庭瑞君越观澜三期\庭瑞君越观澜三期\fs57grhn_w20-地上给水排水平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210512\澳海黄州府（二期）\澳海黄州府（二期）\FS5GMBXU_W20-地上给水排水平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210517\合景红莲湖项目_框线(1)\合景红莲湖项目_框线\FS55TD78_W20-73#-地上给水排水平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210517\湖北交投颐和华府_框线(1)\湖北交投颐和华府_框线\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210517\佳兆业滨江新城_框线(1)\佳兆业滨江新城_框线\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210517\蓝光未来阅璟_框线(1)\蓝光未来阅璟_框线\FS5F8704_W20-地上给水排水平面图-送审版.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210517\蓝光钰泷府二期_框线(1)\蓝光钰泷府二期_框线\FS59P2BC_W20-地上给水排水平面图-副本.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210517\清江山水四期_框线(1)\清江山水四期_框线\FS55TMPH_W20-地上给水排水平面图.dwg",
@"E:\thepa_workingSpace\任务资料\任务2\210517\武汉二七滨江商务区南一片住宅地块_框线(1)\武汉二七滨江商务区南一片住宅地块_框线\FS5747SS_W20-地上给水排水平面图.dwg",
};
        }
        private static void qtcdqp()
        {
            var files = getFiles();
            foreach (var file in files)
            {
                AddButton(Path.GetFileName(file), () =>
                {
                    Dbg.PrintLine(file);
                    Dbg.OpenCadDwgFile(file);
                    Dbg.FocusMainWindow();
                    Dbg.PrintLine(Dbg.SelectGRect().ToJson());

                    //using (var adb = AcadDatabase.Active())
                    //{
                    //    //Clipboard.SetText(Dbg.SelectGRect().ToJson());
                    //}


                });
            }
        }

        public class JsonConverter4 : JsonConverter
        {
            public override bool CanRead => true;
            public override bool CanWrite => true;
            static readonly HashSet<Type> types = new HashSet<Type>();
            static JsonConverter4()
            {
                types.Add(typeof(GRect));
                types.Add(typeof(GLineSegment));
                types.Add(typeof(GVector));
                types.Add(typeof(Point2d));
                types.Add(typeof(Point3d));
                types.Add(typeof(Vector2d));
                types.Add(typeof(Vector3d));
            }
            public override bool CanConvert(Type objectType)
            {
                return types.Contains(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (typeof(GRect) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new GRect(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>(), ja[3].ToObject<double>());
                }
                if (typeof(GLineSegment) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new GLineSegment(new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>()),
                        new Point2d(ja[2].ToObject<double>(), ja[3].ToObject<double>()));
                }
                if (typeof(GVector) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new GVector(new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>()), new Vector2d(ja[2].ToObject<double>(), ja[3].ToObject<double>()));
                }
                if (typeof(Point2d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>());
                }
                if (typeof(Point3d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Point3d(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>());
                }
                if (typeof(Vector2d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>());
                }
                if (typeof(Vector3d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Point3d(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>());
                }
                throw new NotSupportedException();
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                {
                    if (value is GRect r)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(GRect) }, { "values", new double[] { r.MinX, r.MinY, r.MaxX, r.MaxY } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is GLineSegment seg)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(GLineSegment) }, { "values", new double[] { seg.StartPoint.X, seg.StartPoint.Y, seg.EndPoint.X, seg.EndPoint.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is GVector vec)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(GVector) }, { "values", new double[] { vec.StartPoint.X, vec.StartPoint.Y, vec.Vector.X, vec.Vector.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Point2d pt)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Point2d) }, { "values", new double[] { pt.X, pt.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Point3d pt)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Point3d) }, { "values", new double[] { pt.X, pt.Y, pt.Z } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Vector2d vec)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Vector2d) }, { "values", new double[] { vec.X, vec.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Vector3d vec)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Vector3d) }, { "values", new double[] { vec.X, vec.Y, vec.Z } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                throw new NotSupportedException();
            }
        }
        public static readonly JsonConverter4 cvt4 = new JsonConverter4();
        public class JsonConverter3 : JsonConverter
        {
            public override bool CanRead => false;
            public override bool CanWrite => true;
            public override bool CanConvert(Type objectType)
            {
                return objectType.IsEnum;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }
        }
        static readonly JsonConverter3 cvt3 = new JsonConverter3();
        public static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj, cvt3, cvt4);
        }
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, cvt4);
        }
        private static void qtchqt()
        {
            var k = FengKeys.StoreysJsonData210519;
            var dict = LoadData<Dictionary<string, List<ThStoreysData>>>(k, cvt4);
            var lst = dict.Values.First();

            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var item in lst)
                {
                    DU.DrawRectLazy(item.Boundary);
                    DU.DrawTextLazy(item.StoreyType + item.Storeys.ToJson(), 3000, item.Boundary.LeftTop.ToPoint3d());
                }
            }
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
        public static T LoadCadData<T>(string name)
        {
            return LoadData<T>(name, cvt4);
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
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
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
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
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-NOTE" && x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("合流")));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
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
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                    pps.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-RAIN-PIPE-RISR" || x.Layer == "W-RAIN-EQPM" || (x.Layer == "W-DRAI-NOTE" && x.ObjectId.IsValid && x.ToDataItem().EffectiveName.StartsWith("A$"))));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
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
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
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
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                     //.Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == "$LIGUAN")//图块炸开的时候就失效了
                     .Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName == "$LIGUAN" : x.Layer == "W-RAIN-EQPM")
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
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
                    //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                    waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR" || x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("地漏")));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
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

                wLines.AddRange(GetWRainLines(entities));

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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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

                wLines.AddRange(GetWRainLines(entities));
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
                    ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
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

                wLines.AddRange(GetWRainLines(adb.ModelSpace.OfType<Entity>()));
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

                    CollectTianzhengVerticalPipes(labelLines, cts, entities);
                }

                {
                    //ok
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<BlockReference>()
                        .Where(x => x.Layer == "W-RAIN-EQPM")
                     //.Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == "$LIGUAN")//图块炸开的时候就失效了
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

                wLines.AddRange(GetWRainLines(entities));
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
                }
            });
        }

        public static void CollectTianzhengVerticalPipes(List<GLineSegment> labelLines, List<CText> cts, List<Entity> entities)
        {
            foreach (var ent in entities.Where(e => ThRainSystemService.IsTianZhengElement(e)).ToList())
            {
                void f()
                {
                    var lst = ent.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                    if (lst.OfType<Line>().Any())
                    {
                        foreach (var et in lst)
                        {
                            if (ThRainSystemService.IsTianZhengElement(et))
                            {
                                var l = et.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                                if (l.Count == 1)
                                {
                                    var e = l[0];
                                    var t = e.TextString;
                                    if (!ThRainSystemService.IsWantedLabelText(t)) return;
                                    var bd = e.Bounds.ToGRect();
                                    var ct = new CText() { Text = t, Boundary = bd };
                                    cts.Add(ct);
                                    if (!ct.Boundary.IsValid)
                                    {
                                        var p = e.Position.ToPoint2d();
                                        var h = e.Height;
                                        var w = h * e.WidthFactor * e.WidthFactor * e.TextString.Length;
                                        var r = new GRect(p, p.OffsetXY(w, h));
                                        ct.Boundary = r;
                                    }
                                    labelLines.AddRange(lst.OfType<Line>().Where(e => e.Length > 0).Select(e => e.ToGLineSegment()));
                                    return;
                                }
                            }
                        }
                    }
                }
                f();
            }
        }

        public static IEnumerable<GLineSegment> GetWRainLines(IEnumerable<Entity> entities)
        {
            foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
            {
                if (e is Line line && line.Length > 0)
                {
                    //wLines.Add(line.ToGLineSegment());
                    yield return line.ToGLineSegment();
                }
                else if (ThRainSystemService.IsTianZhengElement(e))
                {
                    //有些天正线炸开是两条，看上去是一条，这里当成一条来处理

                    //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                    //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                    //{
                    //    wLines.Add(ln.ToGLineSegment());
                    //}

                    if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                    {
                        //wLines.Add(seg);
                        if (seg.Length > 0) yield return seg;
                    }
                    else foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            if (ln.Length > 0)
                            {
                                //wLines.Add(ln.ToGLineSegment());
                                yield return ln.ToGLineSegment();
                            }
                        }
                }
            }
        }

        //var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\FL1ASTSB_W20-8#楼-给排水及消防平面图.dwg";
        private static void qtaiag()
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
                    ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
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

                    foreach (var e in adb.ModelSpace.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                }

                {
                    var pps = new List<Entity>();
                    var blockNameOfVerticalPipe = "带定位立管";
                    pps.AddRange(adb.ModelSpace.OfType<BlockReference>()
                     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                     .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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

                {
                    foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                    {
                        if (e is Line line && line.Length > 0)
                        {
                            wLines.Add(line.ToGLineSegment());
                        }
                        else if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                            //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                            //{
                            //    wLines.Add(ln.ToGLineSegment());
                            //}
                            foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                if (ln.Length > 0)
                                {
                                    wLines.Add(ln.ToGLineSegment());
                                }
                            }
                        }
                    }
                }
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
                }
            });
        }

        private static void qta864()
        {
            var lst = new List<CText>();
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);
                foreach (var e in adb.ModelSpace.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                {
                    lst.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                }
            }
            AddLazyAction("draw texts", adb =>
            {
                foreach (var item in lst)
                {
                    DU.DrawTextLazy(item.Text, item.Boundary.LeftButtom.ToPoint3d());
                    DU.DrawRectLazy(item.Boundary);
                }
            });
        }

        private static void qta7om()
        {
            var lines = new List<GLineSegment>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e => ThRainSystemService.IsTianZhengElement(e)).ToList())
                {
                    var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                    if (lst.Count == 1 && lst[0] is Line line && line.Length > 0)
                    {
                        lines.Add(line.ToGLineSegment());
                    }
                }
            }
            AddLazyAction("draw tianzheng", adb =>
            {
                foreach (var seg in lines)
                {
                    DU.DrawLineSegmentLazy(seg);
                }
            });
        }

        private static void qta742()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);
                Dbg.ShowString(Dbg.SelectEntity<BlockReference>(adb).ToDataItem().ToJson());
            }
        }

        private static void qta6qi()
        {
            var lst = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var storeysRecEngine = new ThStoreysRecognitionEngine();
                var range = Dbg.SelectRange();
                storeysRecEngine.Recognize(db, range);
                var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                foreach (var e in ents)
                {
                    lst.Add(e.Bounds.ToGRect());
                }
            }
            AddLazyAction("draw rect", adb =>
            {
                foreach (var r in lst)
                {
                    DU.DrawRectLazy(r);
                }
            });
        }

        private static void qta69w()
        {
            var lst = new List<GRect>();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = adb.ModelSpace.OfType<Entity>().ToList();

                foreach (var e in ents)
                {
                    var bd = GeoAlgorithm.GetBoundaryRect(e);
                    lst.Add(bd);
                }

            }

            FengDbgTest.qt8czw.AddButton("--", () =>
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);

                    foreach (var e in lst)
                    {
                        DU.DrawRectLazy(e);
                    }
                    lst.Clear();
                }

            });
        }

        private static void qta68u()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var br = Dbg.SelectEntity<BlockReference>(adb);
                var objs = GetEntities(br, Matrix3d.Identity);
                //Dbg.ShowString(objs.Count.ToString());

                //var bd = GeoAlgorithm.GetBoundaryRect(br);
                //var center = bd.Center.ToPoint3d();
                var center = Dbg.SelectPoint();
                //DU.DrawEntitiesLazy(objs);
                foreach (var e in objs)
                {
                    Dbg.PrintLine(e.GetType().ToString());
                    e.TransformBy(Matrix3d.Displacement(center - Point3d.Origin));
                    //Dbg.ShowWhere(e);
                    DU.DrawEntityLazy(e);

                    if (e is Circle c)
                    {
                        Dbg.PrintLine(c.Radius);
                    }
                    if (e is Line line)
                    {
                        Dbg.PrintLine(line.Length);
                        Dbg.ShowXLabel(line.StartPoint, 10);
                        Dbg.ShowXLabel(line.EndPoint, 10);
                        Dbg.PrintLine(line.Visible);
                        var ln = new Line() { StartPoint = line.StartPoint, EndPoint = line.EndPoint };
                        DU.DrawEntityLazy(ln);
                    }
                }
            }
        }

        private static List<Entity> GetEntities(BlockReference br, Matrix3d mt)
        {
            Debugger.Break();
            using (var db = AcadDatabase.Use(br.Database))
            {
                var results = new List<Entity>();
                BlockTableRecord btr;
                if (br.IsDynamicBlock)
                {
                    btr = db.Element<BlockTableRecord>(br.DynamicBlockTableRecord);
                }
                else
                {
                    btr = db.Element<BlockTableRecord>(br.BlockTableRecord);
                }
                foreach (var id in btr)
                {
                    var obj = db.Element<Entity>(id);
                    if (obj is BlockReference blkObj)
                    {
                        var newMt = blkObj.BlockTransform.PreMultiplyBy(mt);
                        results.AddRange(GetEntities(blkObj, newMt));
                    }
                    else
                    {
                        if (obj.Visible)
                        {
                            results.Add(obj.GetTransformedCopy(mt));
                        }
                    }
                }
                return results;
            }
        }

        private static void qtcknq()
        {
            var fileToStoreysDataDict = LoadData<Dictionary<string, List<ThStoreysData>>>(FengKeys.StoreysJsonData210519, cvt4);
            var files = getFiles();
            var file = files.First();
            var lst = fileToStoreysDataDict[file];
            var items = LoadData<List<RainSystemGeoData>>(FengKeys.WaterBucketsJsonData210519, cvt4);
            var data = items.First();

            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                foreach (var item in lst)
                {
                    DU.DrawRectLazy(item.Boundary);
                    DU.DrawTextLazy(item.StoreyType + item.Storeys.ToJson(), 3000, item.Boundary.LeftTop.ToPoint3d());
                }
                foreach (var bk in data.SideWaterBuckets)
                {
                    DU.DrawRectLazy(bk);
                }
                foreach (var bk in data.GravityWaterBuckets)
                {
                    DU.DrawRectLazy(bk);
                }
            }
        }

        private static void qtclnt()
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


            var files = getFiles();
            var file = files.First();
            var range = getRangeDict()[file].ToPoint3dCollection();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
            {
                //Dbg.BuildAndSetCurrentLayer(adb.Database);

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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
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
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                }

                {
                    var pps = new List<Entity>();
                    var blockNameOfVerticalPipe = "带定位立管";
                    pps.AddRange(entities.OfType<BlockReference>()
                     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                     .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
                    foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                    {
                        if (e is Line line && line.Length > 0)
                        {
                            wLines.Add(line.ToGLineSegment());
                        }
                        else if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                            //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                            //{
                            //    wLines.Add(ln.ToGLineSegment());
                            //}
                            foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                if (ln.Length > 0)
                                {
                                    wLines.Add(ln.ToGLineSegment());
                                }
                            }
                        }
                    }
                }
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
        private static void qu0jrt()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                ThCADCoreNTSService.Instance.ArcTessellationLength = 10;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var ents = adb.ModelSpace.OfType<Entity>().ToList();
                var s = ents.Select(x => x.GetType()).Distinct().Select(x => x.FullName).ToList().JoinWith("\n");
                Dbg.ShowString(s);
            }
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


    }
    public static class GeoExtensions
    {
        public static void ToLineString(this GRect rect)
        {
            var points = new NetTopologySuite.Geometries.Coordinate[]
            {
                rect.LeftTop.ToNTSCoordinate(),
                rect.RightTop.ToNTSCoordinate(),
                rect.RightButtom.ToNTSCoordinate(),
                rect.LeftButtom.ToNTSCoordinate(),
                rect.LeftTop.ToNTSCoordinate(),
            };
            ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points);
        }
    }
}


namespace ThMEPWSS.DebugNs
{
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
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
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Operation.Linemerge;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using ThMEPEngineCore.Algorithm;
    public class FengDbgTesting
    {
        public static RainSystemGeoData qtdtsl()
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


            var files = Util1.getFiles();
            var file = files.First();
            var range = Util1.getRangeDict()[file].ToPoint3dCollection();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
            {
                //Dbg.BuildAndSetCurrentLayer(adb.Database);

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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
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
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
                }
                {
                    foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                }

                {
                    var pps = new List<Entity>();
                    var blockNameOfVerticalPipe = "带定位立管";
                    pps.AddRange(entities.OfType<BlockReference>()
                     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                     .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
                    foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                    {
                        if (e is Line line && line.Length > 0)
                        {
                            wLines.Add(line.ToGLineSegment());
                        }
                        else if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                            //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                            //{
                            //    wLines.Add(ln.ToGLineSegment());
                            //}
                            foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                if (ln.Length > 0)
                                {
                                    wLines.Add(ln.ToGLineSegment());
                                }
                            }
                        }
                    }
                }
            }
            var geoData = new RainSystemGeoData();
            geoData.Init();
            geoData.Storeys.AddRange(storeys);
            geoData.LabelLines.AddRange(labelLines);
            geoData.WLines.AddRange(wLines);
            geoData.Labels.AddRange(cts);
            geoData.VerticalPipes.AddRange(pipes);
            geoData.CondensePipes.AddRange(condensePipes);
            geoData.FloorDrains.AddRange(floorDrains);
            geoData.WaterWells.AddRange(waterWells);
            geoData.WaterPortSymbols.AddRange(waterPortSymbols);
            geoData.WaterPort13s.AddRange(waterPort13s);
            geoData.WrappingPipes.AddRange(wrappingPipes);
            //geoData.SideWaterBuckets.AddRange(xxx);
            //geoData.GravityWaterBuckets.AddRange(xxx);

            geoData.FixData();
            return geoData;
        }
        const string qtdwh3 = @"D:\DATA\temp\637571012711826922.json";
        [Feng]
        public static void qtntzx()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = DU.DrawingTransaction)
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                //天正立管的bound是空的。。。中心点是对的
                //var e = Dbg.SelectEntity<Entity>(adb);
                ////DU.DrawBoundaryLazy(e);
                ////Dbg.ShowWhere(e);
                //Dbg.PrintLine(e.Bounds.ToGRect().Width);
                //Dbg.PrintLine(e.Bounds.ToGRect().Height);
                ////var pl=DU.DrawRectLazy(e.Bounds.ToGRect());
                ////pl.ConstantWidth = 100;
                ////Dbg.ShowWhere(pl);


            }

        }
        [Feng]
        public static void qtntzs()
        {
            Util1.qtbzkf();
        }
        [Feng("输出图纸分析结果")]
        public static void qtnu0e()
        {


            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                using (var adb = AcadDatabase.Active())
                {
                    //Dbg.BuildAndSetCurrentLayer(adb.Database);

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
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                        //!!!
                        var pps = new List<Entity>();
                        //pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));
                        pps.AddRange(entities.Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM") && ThRainSystemService.IsTianZhengElement(x)));
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                DU.Dispose();
                var range = Dbg.SelectRange();
                var basePt = Dbg.SelectPoint();
                ThRainSystemService.ImportElementsFromStdDwg();
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = getGeoData(range);
                ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                ThRainSystemService.PreFixGeoData(geoData, 150);
                geoData.FixData();
                var cadDataMain = RainSystemCadData.Create(geoData);
                var cadDatas = cadDataMain.SplitByStorey();
                var sv = new RainSystemService()
                {
                    Storeys = storeys,
                    GeoData = geoData,
                    CadDataMain = cadDataMain,
                    CadDatas = cadDatas,
                };
                sv.CreateDrawingDatas();
                if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                DU.Dispose();
            }

        }
        [Feng]
        public static void qtnu08()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Polyline>(adb);
                //Dbg.PrintLine(e.HasBulges);
                //Dbg.PrintLine(e.NumberOfVertices);//3
                //(e.ExplodeToDBObjectCollection()[0] as BlockReference).ToDataItem();
                //Dbg.PrintLine(e.ExplodeToDBObjectCollection()[0].ObjectId.ToString());
            }
        }
        public static bool IsTianZhengWaterPort(Entity e)
        {
            if ((e.Layer == "W-RAIN-EQPM" || e.Layer == "W-RAIN-NOTE" || e.Layer == "W-RAIN-DIMS") && ThRainSystemService.IsTianZhengElement(e))
            {
                var lst = e.ExplodeToDBObjectCollection();
                if (lst.Count == 1)
                {
                    if (lst[0] is BlockReference br)
                    {
                        var lst2 = br.ExplodeToDBObjectCollection();
                        if (lst2.Count == 1)
                        {
                            if (lst2[0] is Polyline pl)
                            {
                                if (pl.HasBulges && pl.NumberOfVertices == 3)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        [Feng("GetLayers")]
        public static void xx()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                Dbg.SetText(adb.Layers.Select(x => x.Name));
            }
        }
        [Feng("test03")]
        public static void qti0pn()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                using (var adb = AcadDatabase.Active())
                {
                    //Dbg.BuildAndSetCurrentLayer(adb.Database);

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
                        ents.AddRange(entities.Where(e => IsTianZhengWaterPort(e)));
                        waterPortSymbols.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                        //!!!
                        var pps = new List<Entity>();
                        pps.AddRange(entities.OfType<Circle>().Where(c => 40 <= c.Radius && c.Radius <= 60));
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
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 350);
        }
        [Feng("标出所有的立管的正确boundary")]
        public static void qtntze()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                    if (c != null)
                    {
                        DU.DrawBoundaryLazy(c);
                    }
                }
            }
        }
        [Feng("试试这个自动连接在test04上的效果")]
        public static void qtp1sh()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Entity>()
                  .Where(e => e.Layer == "W-RAIN-PIPE").OfType<Line>()
                  .Select(e => e.ToGLineSegment())
                  .ToList();
                var rs = new List<GRect>();
                foreach (var e in adb.ModelSpace.OfType<Circle>().Where(e => e.Layer == "W-RAIN-DIMS"))
                {
                    var r = e.Bounds.ToGRect();
                    if (r.IsValid)
                    {
                        rs.Add(r);
                    }
                }
                foreach (var seg in GetSegsToConnect(lines, rs, 200))//这个值不要设置得太大 处理这种情况的时候会有些问题 - --
                {
                    var pl = DU.DrawLineSegmentLazy(seg);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }
        }
        [Feng]
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
        [Feng("再试试...")]
        public static void qtp8xj()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Line>()
                  .Where(e => e.ColorIndex == 4)
                  .Select(e => e.ToGLineSegment())
                  .ToList();

                foreach (var seg in GetSegsToConnect(lines, null, 200, radius: 15))//这个值不要设置得太大 处理这种情况的时候会有些问题 - --
                {
                    var pl = DU.DrawLineSegmentLazy(seg, 50);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }

        }
        [Feng("vec")]
        public static void qtpe9p()
        {
            var v1 = new Point2d(1, 1) - Point2d.Origin;
            var v2 = new Point2d(-1, -1) - Point2d.Origin;
            Dbg.PrintLine(GeoAlgorithm.AngleToDegree(v1.GetAngleTo(v2)));
        }

        [Feng("搞定")]
        public static void qtpbrr()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Entity>()
                  .Where(e => e.Layer == "W-DRAI-DOME-PIPE" && ThRainSystemService.IsTianZhengElement(e))
                  .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                  .Select(e => e.ToGLineSegment())
                  .ToList();
                var rs = new List<GRect>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                    if (c != null)
                    {
                        //DU.DrawBoundaryLazy(c);
                        var r = c.Bounds.ToGRect();
                        if (r.IsValid)
                        {
                            rs.Add(r);
                        }
                    }
                }
                foreach (var seg in GetSegsToConnect(lines, rs, 8000))
                {
                    var pl = DU.DrawLineSegmentLazy(seg, 50);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }

        }
        [Feng("定版吧")]
        public static void qtn8ij()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Entity>()
                  .Where(e => e.Layer == "W-RAIN-PIPE" && ThRainSystemService.IsTianZhengElement(e))
                  .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                  .Select(e => e.ToGLineSegment())
                  .ToList();
                var rs = new List<GRect>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                    if (c != null)
                    {
                        //DU.DrawBoundaryLazy(c);
                        var r = c.Bounds.ToGRect();
                        if (r.IsValid)
                        {
                            rs.Add(r);
                        }
                    }
                }
                foreach (var seg in GetSegsToConnect(lines, rs, 8000))//后面排除下一些值，中间不要穿过别的东西
                {
                    var pl = DU.DrawLineSegmentLazy(seg, 100);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }

        }
        static bool IsContrary(double v1, double v2)
        {
            return v1 < 0 && v2 > 0 || v1 > 0 && v2 < 0;
        }
        //以后有需要再写个支持任意角度的，因为现在没有测试数据，先不深入了
        //使用的时候最好再对结果extend .1
        public static IEnumerable<GLineSegment> GetSegsToConnect(List<GLineSegment> lines, List<GRect> rects, double extends, double bufSize = 2.5, double radius = 10)
        {
            if (extends <= 0) throw new ArgumentException();
            var h = GeoFac.LineGrouppingHelper.Create(lines);
            h.InitPointGeos(radius);
            h.DoGroupingByPoint();
            h.CalcAlonePoints();
            h.DistinguishAlonePoints();
            if (rects != null) h.KillAloneRings(rects.Where(r => r.IsValid).Distinct().Select(r => r.ToPolygon()).ToArray());

            //foreach (var r in h.YieldAloneRings())
            //{
            //    DU.DrawEntitiesLazy(r.ToDbObjects().OfType<Entity>().ToArray());
            //}
            var extSegs = h.GetExtendedGLineSegmentsByFlags(extends).ToList();
            {
                var newSegs = extSegs.Where(seg => seg.IsHorizontal(5)).Distinct().ToList();
                var geos = newSegs.Select(seg => seg.Buffer(bufSize)).ToList();

                foreach (var g in GeoFac.GroupGeometries(geos).Where(g => g.Count == 2))
                {
                    var geo1 = g[0];
                    var geo2 = g[1];
                    var seg1 = newSegs[geos.IndexOf(geo1)];
                    var seg2 = newSegs[geos.IndexOf(geo2)];
                    //if (!IsContrary(seg1.StartPoint.X - seg1.EndPoint.X, seg2.StartPoint.X - seg2.EndPoint.X)) continue;

                    //var xArr = new double[] { seg1.StartPoint.X, seg1.EndPoint.X, seg2.StartPoint.X, seg2.EndPoint.X };
                    //var minX = xArr.Min();
                    //var maxX = xArr.Max();
                    //yield return new GLineSegment(minX, seg1.Y1, maxX, seg1.Y1);

                    //var kv = GetMiddleValue(seg1.StartPoint.X, seg1.EndPoint.X, seg2.StartPoint.X, seg2.EndPoint.X);
                    //yield return new GLineSegment(kv.Key, seg1.Y1, kv.Value, seg1.Y1);

                    //DU.DrawLineSegmentBufferLazy(seg1, 10);
                    //DU.DrawLineSegmentBufferLazy(seg2, 10);

                    var x1 = seg1.StartPoint.X;
                    var x2 = seg1.EndPoint.X;
                    var x3 = seg2.StartPoint.X;
                    var x4 = seg2.EndPoint.X;
                    if (!IsContrary(x1 - x2, x3 - x4)) continue;
                    yield return new GLineSegment(x1, seg1.Y1, x3, seg1.Y1);
                }
            }
            {
                var newSegs = extSegs.Where(seg => seg.IsVertical(5)).Distinct().ToList();
                var geos = newSegs.Select(seg => seg.Buffer(bufSize)).ToList();

                var gs = GeoFac.GroupGeometries(geos).Where(g => g.Count == 2).ToList();
                //foreach (var g in gs)
                //{
                //    //if (g.Count > 2)
                //    {
                //        DU.DrawEntitiesLazy(g.SelectMany(x => x.ToDbObjects().OfType<Entity>()).ToArray());
                //    }
                //}
                foreach (var g in gs)
                {
                    var geo1 = g[0];
                    var geo2 = g[1];
                    var seg1 = newSegs[geos.IndexOf(geo1)];
                    var seg2 = newSegs[geos.IndexOf(geo2)];

                    //DU.DrawLineSegmentBufferLazy(seg1, 10);
                    //DU.DrawLineSegmentBufferLazy(seg2, 10);

                    //if (!IsContrary(seg1.StartPoint.Y - seg1.EndPoint.Y, seg2.StartPoint.Y - seg2.EndPoint.Y)) continue;

                    //var yArr = new double[] { seg1.StartPoint.Y, seg1.EndPoint.Y, seg2.StartPoint.Y, seg2.EndPoint.Y };
                    //var minY = yArr.Min();
                    //var maxY = yArr.Max();
                    //yield return new GLineSegment(seg1.X1, minY, seg1.X1, maxY);

                    //var kv = GetMiddleValue(seg1.StartPoint.Y, seg1.EndPoint.Y, seg2.StartPoint.Y, seg2.EndPoint.Y);
                    //yield return new GLineSegment(seg1.X1, kv.Key, seg1.X1, kv.Value);

                    //DU.DrawLineSegmentBufferLazy(seg1, 10);
                    //DU.DrawLineSegmentBufferLazy(seg2, 10);

                    var y1 = seg1.StartPoint.Y;
                    var y2 = seg1.EndPoint.Y;
                    var y3 = seg2.StartPoint.Y;
                    var y4 = seg2.EndPoint.Y;
                    if (!IsContrary(y1 - y2, y3 - y4)) continue;
                    yield return new GLineSegment(seg1.X1, y1, seg1.X1, y3);
                }
            }
        }
        static void Sort(ref double v1, ref double v2)
        {
            if (v1 > v2)
            {
                Swap(ref v1, ref v2);
            }
        }

        private static void Swap(ref double v1, ref double v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }


        static KeyValuePair<double, double> GetMiddleValue(double v1, double v2, double v3, double v4)
        {
            var lst = new List<double>() { v1, v2, v3, v4 };
            var min = lst.Min();
            var max = lst.Max();
            lst.Remove(min);
            lst.Remove(max);
            min = lst.Min();
            max = lst.Max();
            return new KeyValuePair<double, double>(min, max);
        }
        [Feng("再试试")]
        public static void qtn8ip()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var lines = adb.ModelSpace.OfType<Entity>()
                   .Where(e => e.Layer == "W-RAIN-PIPE" && ThRainSystemService.IsTianZhengElement(e))
                   .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                   .ToList();
                var res = ThMEPEngineCore.Algorithm.ThLineMerger.Merge(lines);
                foreach (var line in res)
                {
                    //em...
                    DU.DrawLineSegmentLazy(line.ToGLineSegment());
                }
            }
        }
        [Feng("继续测试")]
        public static void qtn8it()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var segs = adb.ModelSpace.OfType<Entity>()
                    .Where(e => e.Layer == "W-RAIN-PIPE" && ThRainSystemService.IsTianZhengElement(e))
                    .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                    .Select(e => e.ToGLineSegment()).ToList();
                var h = GeoFac.LineGrouppingHelper.Create(segs);
                h.InitPointGeos(10);
                h.DoGroupingByPoint();
                h.CalcAlonePoints();
                h.DistinguishAlonePoints();
                {
                    var rs = new List<GRect>();
                    foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                    {
                        var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                        if (c != null)
                        {
                            //DU.DrawBoundaryLazy(c);
                            var r = c.Bounds.ToGRect();
                            if (r.IsValid)
                            {
                                rs.Add(r);
                            }
                        }
                    }
                    h.KillAloneRings(rs.Distinct().Select(r => r.ToPolygon()).ToArray());

                }
                Dbg.PrintLine(h.GetAlonePointsCount());
                var lst = new List<GLineSegment>();
                foreach (var seg in h.GetExtendedGLineSegmentsByFlags(200.1))
                {
                    if (!seg.IsHorizontal(5)) continue;
                    //DU.DrawPolyLineLazy(seg);
                    lst.Add(seg);
                }
                {
                    lst = lst.Distinct().ToList();
                    Dbg.PrintLine(lst.Distinct().Count() == lst.Count);
                    var geos = lst.Select(seg => seg.Buffer(2.5)).ToList();
                    //Dbg.PrintLine(lst.Count);
                    //Dbg.PrintLine(geos.Count);
                    foreach (var geo in geos)
                    {
                        //Dbg.PrintLine(geo.ToDbObjects().Count);
                        foreach (var pl in geo.ToDbObjects().OfType<Polyline>())
                        {
                            //DU.DrawEntityLazy(pl);
                        }
                    }

                    {
                        var gs = ThRainSystemService.GroupPolylines(geos.SelectMany(geo => geo.ToDbObjects().OfType<Polyline>()).ToList()).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            //DU.DrawEntitiesLazy(g);
                        }
                        var rs = geos.Select(x => x.EnvelopeInternal.ToGRect()).ToList();
                        foreach (var r in rs)
                        {
                            //DU.DrawRectLazy(r);
                        }

                    }
                    //var gs = GeometryFac.GroupGeometries(geos);
                    {
                        Dbg.PrintLine(geos.Distinct().Count() == geos.Count);
                        //for (int i = 0; i < geos.Count; i++)
                        //{
                        //    for (int j = i + 1; j < geos.Count; j++)
                        //    {
                        //        var o1 = geos[i];
                        //        var o2 = geos[j];
                        //        var m = geos.IndexOf(o1);
                        //        var n = geos.IndexOf(o2);
                        //        if (m != i || n != j)
                        //        {
                        //            Dbg.PrintLine($"{i} {j } {m} {n}");
                        //        }
                        //    }
                        //}
                        //ThRainSystemService.Triangle(geos, (o1, o2) =>
                        //{
                        //    if (o1.ToIPreparedGeometry().Intersects(o2))
                        //    {
                        //        Dbg.ShowWhere(o2.ToGRect());
                        //        var i = geos.IndexOf(o1);
                        //        var j = geos.IndexOf(o2);
                        //        Dbg.PrintLine($"{} {}");
                        //    }
                        //});
                    }
                    {
                        var gs = GeoFac.GroupGeometries(geos).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            var geo1 = g[0];
                            var geo2 = g[1];
                            var seg1 = lst[geos.IndexOf(geo1)];
                            var seg2 = lst[geos.IndexOf(geo2)];

                            //DU.DrawPolyLineLazy(seg1);
                            //DU.DrawPolyLineLazy(seg2);

                            //Dbg.ShowWhere(GRect.Create(seg1.StartPoint,.1));
                            //Dbg.ShowWhere(GRect.Create(seg2.StartPoint, .1));
                            //Dbg.ShowWhere(seg1.StartPoint);

                            //var l = new List<Geometry>() { seg1.ToLineString(), seg2.ToLineString() };
                            //var ret=LineMerge(l);
                            //foreach (var pl in ret.Select(x => x.ToDbObjects()).OfType<Entity>())
                            //{
                            //    DU.DrawEntityLazy(pl);
                            //}

                            var objs = new DBObjectCollection() { seg1.ToCadLine(), seg2.ToCadLine() };
                            objs = objs.LineMerge();
                            //foreach (var x in objs.OfType<Entity>())
                            //{
                            //    DU.DrawEntityLazy(x);
                            //}
                            foreach (var pl in objs.OfType<Polyline>())
                            {
                                //var lines = pl.ExplodeToDBObjectCollection().OfType<Line>();
                                //foreach (var line in lines)
                                //{
                                //    DU.DrawEntityLazy(line);
                                //}
                                var res = ThMEPEngineCore.Algorithm.ThLineMerger.Merge(pl.ExplodeToDBObjectCollection().OfType<Line>().ToList());
                            }

                        }
                    }
                }
            }
        }
        public static IList<Geometry> LineMerge(IEnumerable<Geometry> geos)
        {
            var merger = new LineMerger();
            merger.Add(geos);
            return merger.GetMergedLineStrings();
        }
        [Feng("test01")]
        public static void qtjrmd()
        {
            //这版对标准图纸是OK的
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                using (var adb = AcadDatabase.Active())
                {
                    //Dbg.BuildAndSetCurrentLayer(adb.Database);

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
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
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
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
                    }
                    {
                        foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        var blockNameOfVerticalPipe = "带定位立管";
                        pps.AddRange(entities.OfType<BlockReference>()
                         .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                         .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
                        foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                        {
                            if (e is Line line && line.Length > 0)
                            {
                                wLines.Add(line.ToGLineSegment());
                            }
                            else if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                                //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                                //{
                                //    wLines.Add(ln.ToGLineSegment());
                                //}
                                foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                                {
                                    if (ln.Length > 0)
                                    {
                                        wLines.Add(ln.ToGLineSegment());
                                    }
                                }
                            }
                        }
                    }
                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 150);
        }
        //粗看OK
        [Feng("test02")]
        public static void qtlabu()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
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
                var waterWellDNs = new List<string>();

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
                                if (r.Width > 20000 && r.Width < 80000 && r.Height > 5000)
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
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                        Util1.CollectTianzhengVerticalPipes(labelLines, cts, entities);
                    }

                    {
                        var pps = new List<Entity>();
                        pps.AddRange(entities.OfType<BlockReference>()
                         //.Where(x => x.Layer == "W-RAIN-EQPM")
                         //.Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == "$LIGUAN")//图块炸开的时候就失效了
                         .Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName == "$LIGUAN" : x.Layer == "W-RAIN-EQPM")
                         );
                        foreach (var pp in pps)
                        {
                            pipes.Add(GRect.Create(pp.Bounds.ToGRect().Center, 55));
                        }
                    }

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

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

                }

                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            var files = Util1.getFiles();
            qtjrlj(getGeoData, 150);
        }

        public static void qtm55x()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                //var e = Dbg.SelectEntity<Line>(adb);
                //var si = new ThCADCoreNTSSpatialIndex(new DBObjectCollection() { e });
                //var ret = si.SelectCrossingPolygon(Dbg.SelectRange().ToSRect().ToGRect().ToCadPolyline());
                //foreach (var x in ret.OfType<Entity>())
                //{
                //    Dbg.ShowWhere(x);
                //}

                //var line1=Dbg.SelectEntity<Line>(adb);
                //var line2 = Dbg.SelectEntity<Line>(adb);
                //Dbg.PrintLine(line1.ToGLineSegment().Buffer(100).ToIPreparedGeometry().Intersects(line2.ToGLineSegment().Buffer(100)));
                //var lst = new List<Geometry>() { line1.ToGLineSegment().Buffer(100), line2.ToGLineSegment().Buffer(100) };
                //Dbg.PrintLine(GeometryFac.GroupGeometries(lst)[0].Count);
            }
            //var g1 = GRect.Create(Point3d.Origin, 100).ToLinearRing();
            //var g2 = GRect.Create(Point3d.Origin, 100).OffsetXY(10,0).ToLinearRing();
            //Dbg.PrintText(g1.ToIPreparedGeometry().Intersects(g2).ToString());
            //var g1 = new Polygon(GRect.Create(Point3d.Origin, 100).ToLinearRing());
            //var g2 = new Polygon(GRect.Create(Point3d.Origin, 100).OffsetXY(10, 0).ToLinearRing());
            //Dbg.PrintText(g1.ToIPreparedGeometry().Intersects(g2).ToString());

        }

        public static void qtm55s()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var segs = adb.ModelSpace.OfType<Line>().Where(e => e.Layer == "W-RAIN-PIPE").Select(e => e.ToGLineSegment()).ToList();
                var h = GeoFac.LineGrouppingHelper.Create(segs);
                h.InitPointGeos(10);
                h.DoGroupingByPoint();
                h.CalcAlonePoints();
                h.DistinguishAlonePoints();
                var lst = new List<GLineSegment>();
                foreach (var seg in h.GetExtendedGLineSegmentsByFlags(200.1))
                {
                    if (!seg.IsHorizontal(5)) continue;
                    //DU.DrawPolyLineLazy(seg);
                    lst.Add(seg);
                }
                {
                    lst = lst.Distinct().ToList();
                    Dbg.PrintLine(lst.Distinct().Count() == lst.Count);
                    var geos = lst.Select(seg => seg.Buffer(2.5)).ToList();
                    //Dbg.PrintLine(lst.Count);
                    //Dbg.PrintLine(geos.Count);
                    foreach (var geo in geos)
                    {
                        //Dbg.PrintLine(geo.ToDbObjects().Count);
                        foreach (var pl in geo.ToDbObjects().OfType<Polyline>())
                        {
                            //DU.DrawEntityLazy(pl);
                        }
                    }

                    {
                        var gs = ThRainSystemService.GroupPolylines(geos.SelectMany(geo => geo.ToDbObjects().OfType<Polyline>()).ToList()).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            //DU.DrawEntitiesLazy(g);
                        }
                        var rs = geos.Select(x => x.EnvelopeInternal.ToGRect()).ToList();
                        foreach (var r in rs)
                        {
                            //DU.DrawRectLazy(r);
                        }

                    }
                    //var gs = GeometryFac.GroupGeometries(geos);
                    {
                        Dbg.PrintLine(geos.Distinct().Count() == geos.Count);
                        //for (int i = 0; i < geos.Count; i++)
                        //{
                        //    for (int j = i + 1; j < geos.Count; j++)
                        //    {
                        //        var o1 = geos[i];
                        //        var o2 = geos[j];
                        //        var m = geos.IndexOf(o1);
                        //        var n = geos.IndexOf(o2);
                        //        if (m != i || n != j)
                        //        {
                        //            Dbg.PrintLine($"{i} {j } {m} {n}");
                        //        }
                        //    }
                        //}
                        //ThRainSystemService.Triangle(geos, (o1, o2) =>
                        //{
                        //    if (o1.ToIPreparedGeometry().Intersects(o2))
                        //    {
                        //        Dbg.ShowWhere(o2.ToGRect());
                        //        var i = geos.IndexOf(o1);
                        //        var j = geos.IndexOf(o2);
                        //        Dbg.PrintLine($"{} {}");
                        //    }
                        //});
                    }
                    {
                        var gs = GeoFac.GroupGeometries(geos).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            var geo1 = g[0];
                            var geo2 = g[1];
                            var seg1 = lst[geos.IndexOf(geo1)];
                            var seg2 = lst[geos.IndexOf(geo2)];
                            DU.DrawPolyLineLazy(seg1);
                            DU.DrawPolyLineLazy(seg2);
                            //Dbg.ShowWhere(GRect.Create(seg1.StartPoint,.1));
                            //Dbg.ShowWhere(GRect.Create(seg2.StartPoint, .1));
                        }
                    }
                }
            }
        }
        [Feng("DrawWrappingPipe")]
        public static void qtnu0v()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Dr.DrawWrappingPipe(Dbg.SelectPoint());


            }

        }
        [Feng]
        public static void qtsmf9()
        {
            var file = @"E:\thepa_workingSpace\210525\test01.txt";
            var drs1 = File.ReadAllText(file).FromCadJson<List<RainSystemDrawingData>>();
            var file2 = @"E:\thepa_workingSpace\210526\新建文本文档.txt";
            var drs2 = File.ReadAllText(file2).FromCadJson<List<RainSystemDrawingData>>();
            var sb = new StringBuilder(4096);
            for (int i = 0; i < drs1.Count; i++)
            {
                var d1 = drs1[i];
                var d2 = drs2[i];
                qtpp9s(sb, d1, d2);
            }
            Dbg.PrintText(sb.ToString());
        }
        static string newFileFn()
        {
            var file = $@"Y:\{DateTime.Now.Ticks}.json";
            return file;
        }
        public static List<RainSystemGeoData> LoadWaterBucketGeoDataList()
        {
            var file = @"D:\DATA\temp\637577092816488915.json";
            var list = File.ReadAllText(file).FromCadJson<List<RainSystemGeoData>>();
            return list;
        }
        [Feng("序列化所有图纸的雨水斗位置信息")]
        public static void qu0j7u()
        {
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<RainSystemGeoData>();
            using (Dbg.DocumentLock)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    var range = d[file].ToPoint3dCollection();
                    using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                    {
                        var geoData = new RainSystemGeoData();
                        geoData.Init();
                        ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        list.Add(geoData);
                    }
                }
            }
            {
                var file = newFileFn();
                File.WriteAllText(file, list.ToCadJson());
            }
        }
        public static List<List<RainSystemDrawingData>> LoadStdDrawingDatas()
        {
            var file = @"D:\DATA\temp\637577104526532228.json";
            var list = File.ReadAllText(file).FromCadJson<List<List<RainSystemDrawingData>>>();
            return list;
        }
        public class BenchMark : IDisposable
        {
            Stopwatch sw = new Stopwatch();
            public BenchMark()
            {
                sw.Start();
            }
            public void Dispose()
            {
                sw.Stop();
                Dbg.PrintLine(sw.Elapsed.TotalSeconds);
            }
        }
        [Feng("找东西")]
        public static void llllll()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

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


                //var pps = new List<Entity>();
                //pps.AddRange(entities.OfType<Circle>().Where(c => 40 <= c.Radius && c.Radius <= 60));
                //pps.AddRange(entities
                //    .Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")
                //    && ThRainSystemService.IsTianZhengElement(x.GetType()))
                //     .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Any())
                //    );
                //Dbg.PrintLine(pps.Count);
                //static GRect getRealBoundaryForPipe(Entity ent)
                //{
                //    return ent.Bounds.ToGRect(50);
                //}
                //foreach (var pp in pps.Distinct())
                //{
                //    Dbg.ShowWhere(pp);
                //}
            }
        }
        [Feng]
        public static void ZoomAll()
        {
            Dbg.ZoomAll();
        }



        private static void NewMethod2(int i)
        {
            var stdDrawingDatas = LoadStdDrawingDatas();

            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            using (Dbg.DocumentLock)
            {
                var getGeoData = fs[i];
                if (getGeoData == null)
                {
                    return;
                }
                var file = files[i];
                var range = d[file].ToPoint3dCollection();
                using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                {
                    var geoData = buckets[i];
                    //var labelHeight = hs[i];
                    var labelHeight = 250;
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);

                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    {
                        var bk = buckets[i];
                        geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                        geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                    }
                    geoData.FixData();
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    //DU.Dispose();
                    {
                        using (var _adb = AcadDatabase.Active())
                        {
                            Dbg.BuildAndSetCurrentLayer(_adb.Database);
                        }
                    }
                    DU.Draw();

                    var drDatas = sv.DrawingDatas;

                }
            }
        }
        [Feng]
        public static void temptest()
        {
            //ThRainSystemService.ImportElementsFromStdDwg();
            //using (var adb = AcadDatabase.Active())
            //{
            //    Dbg.PrintText(adb.TextStyles.Names().ToJson());
            //}

            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var ents = Dbg.SelectEntities(adb).OfType<Entity>().ToList();
                foreach (var e in ents)
                {
                    Dbg.PrintLine(e.GetType().FullName);
                }
            }
        }
        //dim示例
        private static void NewMethod4()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var pt1 = Dbg.SelectPoint();
                var pt2 = pt1.OffsetY(99.9);

                //不传string的话就自动跟踪，注意默认的字体超小的。。。
                //var dim = new AlignedDimension(pt1,pt2,GeTools.MidPoint(pt1,pt2).OffsetX(1000),null, adb.Database.StandardDimStyle());

                var dim = new AlignedDimension();
                dim.XLine1Point = pt1;
                dim.XLine2Point = pt2;
                //dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).PolarPoint(Math.PI / 2, 1000);
                dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(20);
                dim.DimensionText = "<>";//也可以这样
                dim.ColorIndex = 4;

                DU.DrawEntityLazy(dim);
                Dbg.ChangeCadScreenTo(dim.Bounds.ToGRect());

            }
        }



        [Feng("什么情况")]
        public static void qtt4vv()
        {
            var testing = new HashSet<int>() { 0, 2, };

            var stdDrawingDatas = LoadStdDrawingDatas();

            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            using (Dbg.DocumentLock)
            {
                var i = 2;
                var getGeoData = fs[i];
                if (getGeoData == null)
                {
                    return;
                }
                var file = files[i];
                var range = d[file].ToPoint3dCollection();
                using (var adb = AcadDatabase.Active())
                {
                    var geoData = buckets[i];
                    var labelHeight = hs[i];
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);

                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    {
                        var bk = buckets[i];
                        geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                        geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                    }
                    geoData.FixData();
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    DU.Dispose();
                    var drDatas = sv.DrawingDatas;
                    Dbg.PrintText(drDatas.ToCadJson());
                }
            }
        }
        [Feng("试试投入使用")]
        public static void qtt4w3()
        {
            try
            {
                //var testing = new HashSet<int>() { 0,1,2,3,4,5 };
                var testing = new HashSet<int>() { 0, 1, 2, 3, 4, 5, 9, 10, 11 };
                //var testing = new HashSet<int>() { 0 };
                //var testing = new HashSet<int>() { 4 };
                //var testing = new HashSet<int>() { 5 };
                //var testing = new HashSet<int>() { 9 };
                //var testing = new HashSet<int>() { 10 };
                //var testing = new HashSet<int>() { 11 };

                var stdDrawingDatas = LoadStdDrawingDatas();

                var buckets = LoadWaterBucketGeoDataList();
                var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
                var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
                var files = Util1.getFiles();
                var d = Util1.getRangeDict();
                var list = new List<List<RainSystemDrawingData>>();

                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        var getGeoData = fs[i];
                        if (getGeoData == null || !testing.Contains(i))
                        {
                            list.Add(null);
                            continue;
                        }

                        var file = files[i];
                        var range = d[file].ToPoint3dCollection();
                        using (Dbg.DocumentLock)
                        using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                        {
                            DU.Dispose();
                            var geoData = buckets[i];
                            var labelHeight = hs[i];
                            var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                            cl.CollectEntities();
                            cl.CollectLabelLines();
                            cl.CollectCTexts();
                            cl.CollectVerticalPipes();
                            cl.CollectWLines();
                            cl.CollectCondensePipes();
                            cl.CollectFloorDrains();
                            cl.CollectWaterWells();
                            cl.CollectWaterPortSymbols();
                            cl.CollectWaterPort13s();
                            cl.CollectWrappingPipes();
                            cl.CollectStoreys(range);


                            var storeys = ThRainSystemService.GetStoreys(range, adb);
                            {
                                var bk = buckets[i];
                                geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                                geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                            }
                            geoData.FixData();
                            ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                            geoData.FixData();
                            var cadDataMain = RainSystemCadData.Create(geoData);
                            var cadDatas = cadDataMain.SplitByStorey();


                            {
                                DU.Dispose();
                                var disList = new List<double>();
                                foreach (var data in cadDatas)
                                {
                                    var lines = data.LabelLines.Select(pl => geoData.LabelLines[cadDataMain.LabelLines.IndexOf(pl)]).Distinct().ToList();
                                    var bds = data.Labels.Select(pl => geoData.Labels[cadDataMain.Labels.IndexOf(pl)]).Select(x => x.Boundary).ToList();
                                    var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
                                    var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
                                    var f1 = GeoFac.CreateGRectContainsSelector(lineHGs);
                                    foreach (var bd in bds)
                                    {
                                        var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1000, 250);
                                        var _lineHGs = f1(g);
                                        var f2 = GeoFac.NearestNeighbourGeometryF(_lineHGs);
                                        var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                                        var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                                        if (geo == null) continue;
                                        {
                                            var line = lineHs[lineHGs.IndexOf(geo)];
                                            var dis = line.Center.GetDistanceTo(bd.Center);
                                            if (100 <= dis && dis <= 400) disList.Add(dis);
                                        }
                                    }
                                }
                                DU.Dispose();
                                Dbg.PrintLine($"avg:{disList.Average():F0} min:{disList.Min():F0} max:{disList.Max():F0}");
                                string xx(int v)
                                {
                                    return $"{disList.Where(dis => dis > v).Count() * 100.0 / disList.Count:F2}%";
                                }
                                Dbg.PrintLine($"test:150,{xx(150)} 200,{xx(200)} 250,{xx(250)} 300,{xx(300)} 350,{xx(350)} {disList.Count}");
                            }

                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Dbg.PrintText(ex.Message);
                Dbg.PrintText(ex.StackTrace);
            }
        }

        [Feng("😎尝试自动计算高度")]
        public static void qtsmrd()
        {
            var testing = new HashSet<int>() { 1, };

            var stdDrawingDatas = LoadStdDrawingDatas();

            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            for (int i = 0; i < files.Length; i++)
            {
                var getGeoData = fs[i];
                if (getGeoData == null || !testing.Contains(i))
                {
                    list.Add(null);
                    continue;
                }
                var file = files[i];
                var range = d[file].ToPoint3dCollection();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                {
                    var geoData = buckets[i];
                    var labelHeight = hs[i];
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    Dbg.PrintText(geoData.ToCadJson());

                    var lines = geoData.LabelLines.Distinct().ToList();
                    var bds = geoData.Labels.Select(x => x.Boundary).ToList();
                    var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
                    var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
                    var f1 = GeoFac.CreateGRectContainsSelector(lineHGs);
                    DU.Dispose();
                    var disList = new List<double>();
                    foreach (var bd in bds)
                    {
                        var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1000, 250);
                        {
                            var e = DU.DrawRectLazy(g);
                            e.ColorIndex = 2;
                        }
                        var _lineHGs = f1(g);
                        var f2 = GeoFac.NearestNeighbourGeometryF(_lineHGs);
                        var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                        var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                        if (geo == null) continue;
                        {
                            var ents = geo.ToDbObjects().OfType<Entity>().ToList();
                            //DU.DrawEntitiesLazy(ents);
                            //ents.ForEach(e => e.ColorIndex = 3);
                            var line = lineHs[lineHGs.IndexOf(geo)];
                            //{
                            //    var e = DU.DrawLineSegmentLazy(line);
                            //    e.ColorIndex = 3;
                            //}
                            //{
                            //    var seg = new GLineSegment(line.Center, bd.Center);
                            //    var e = DU.DrawLineSegmentLazy(seg);
                            //    e.ColorIndex = 3;
                            //}
                            //Dbg.PrintLine(line.GetDistanceTo(bd.Center));
                            var dis = line.Center.GetDistanceTo(bd.Center);
                            //Dbg.PrintLine(dis);
                            if (100 <= dis && dis <= 400) disList.Add(dis);
                        }
                        //if (geo != null)
                        //{
                        //    var line=lineHs[lineHGs.IndexOf(geo)];
                        //    Dbg.PrintLine(line.GetDistanceTo(bd.Center));
                        //}
                        DU.DrawRectLazy(bd);
                    }
                    foreach (var line in lineHs)
                    {
                        DU.DrawLineSegmentLazy(line);
                    }
                    Dbg.PrintLine("avg:" + disList.Average());
                    DU.Draw();
                    DU.Dispose();
                }
            }
        }

        [Feng("😋画单张图纸的骨架")]
        public static void qtsmf1()
        {
            var i = 2;
            NewMethod2(i);
        }
        [Feng("准备画骨架")]
        public static void qu0jtu()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                try
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);
                    DU.Dispose();
                    var range = Dbg.SelectRange();
                    //var basePt = Dbg.SelectPoint();
                    ThRainSystemService.ImportElementsFromStdDwg();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = new RainSystemGeoData();
                    geoData.Init();

                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);

                    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                    ThRainSystemService.PreFixGeoData(geoData, -1);
                    ThRainSystemService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };

                    AddButton("画骨架", () =>
                    {
                        DU.Dispose();
                        sv.CreateDrawingDatas();
                        using (Dbg.DocumentLock)
                        {
                            DU.Draw();
                        }
                    });

                    //if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                    //DU.Dispose();
                    //sv.RainSystemDiagram.Draw(basePt);
                    //DU.Draw(adb);
                }
                //catch (System.Exception ex)
                //{
                //    MessageBox.Show(ex.Message);
                //}
                finally
                {
                    DU.Dispose();
                }
            }
        }
        [Feng("😎开始融合")]
        public static void qtqxus()
        {
            try
            {
                //var testing = new HashSet<int>() { 1, };
                //var testing = new HashSet<int>() { 0, 1, 2, 3 };
                var testing = new HashSet<int>() { 0, 2, 3 };
                //var testing = new HashSet<int>() { 0 };
                //var testing = new HashSet<int>() { 4 };
                //var testing = new HashSet<int>() { 5 };
                //var testing = new HashSet<int>() { 9 };
                //var testing = new HashSet<int>() { 10 };
                //var testing = new HashSet<int>() { 11 };

                var stdDrawingDatas = LoadStdDrawingDatas();

                var buckets = LoadWaterBucketGeoDataList();
                var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
                var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
                var files = Util1.getFiles();
                var d = Util1.getRangeDict();
                var list = new List<List<RainSystemDrawingData>>();

                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        var getGeoData = fs[i];
                        if (getGeoData == null || !testing.Contains(i))
                        {
                            list.Add(null);
                            continue;
                        }

                        var file = files[i];
                        var range = d[file].ToPoint3dCollection();
                        using (Dbg.DocumentLock)
                        using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                        {
                            try
                            {
                                var geoData = buckets[i];
                                geoData.Init();
                                var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                                cl.CollectEntities();
                                cl.CollectLabelLines();
                                cl.CollectCTexts();
                                cl.CollectVerticalPipes();
                                cl.CollectWLines();
                                cl.CollectCondensePipes();
                                cl.CollectFloorDrains();
                                cl.CollectWaterWells();
                                cl.CollectWaterPortSymbols();
                                cl.CollectWaterPort13s();
                                cl.CollectWrappingPipes();
                                cl.CollectStoreys(range);

                                DU.Dispose();
                                var storeys = ThRainSystemService.GetStoreys(range, adb);
                                {
                                    var bk = buckets[i];
                                    geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                                    geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                                }
                                geoData.FixData();
                                //var labelHeight = hs[i];
                                //var labelHeight = 350;
                                //var labelHeight = ThRainSystemService.TryGuessLabelHeight(adb);
                                var labelHeight = -1;
                                ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                                ThRainSystemService.ConnectLabelToLabelLine(geoData);
                                geoData.FixData();
                                var cadDataMain = RainSystemCadData.Create(geoData);
                                var cadDatas = cadDataMain.SplitByStorey();
                                var sv = new RainSystemService()
                                {
                                    Storeys = storeys,
                                    GeoData = geoData,
                                    CadDataMain = cadDataMain,
                                    CadDatas = cadDatas,
                                };
                                sv.CreateDrawingDatas();
                                DU.Dispose();

                                var drDatas = sv.DrawingDatas;
                                list.Add(drDatas);
                            }
                            finally
                            {
                                adb.DiscardChanges();
                            }

                        }
                    }
                }
                {
                    var sb = new StringBuilder(4096);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var drDatas = list[i];
                        if (drDatas == null) continue;
                        var stdDrDatas = stdDrawingDatas[i];
                        if (stdDrDatas == null) continue;
                        sb.AppendLine($"---test{i + 1:D2}");
                        for (int j = 0; j < drDatas.Count; j++)
                        {
                            var d1 = stdDrDatas[j];
                            var d2 = drDatas[j];
                            qtpp9s(sb, d1, d2, $"上面的是框{j}");
                        }
                    }
                    Dbg.PrintText(sb.ToString());
                    var file = newFileFn();
                    File.WriteAllText(file, list.ToCadJson());
                }
            }
            catch (System.Exception ex)
            {
                Dbg.PrintText(ex.Message);
                Dbg.PrintText(ex.StackTrace);
            }
        }
        [Feng("😎直接跑")]
        public static void qtt6el()
        {
            ThRainSystemService.DrawRainSystemDiagram2();
        }
        public static void TestThWRainSystemDiagram_Save()
        {
            ThWRainSystemDiagramTest.Test2();
        }
        [Feng("🔴TestThWRainSystemDiagram_Load")]
        public static void TestThWRainSystemDiagram_Load()
        {
            //ThRainSystemService.ImportElementsFromStdDwg();
            ThWRainSystemDiagramTest.Test1();
        }
        [Feng("🔴cad win")]
        public static void qtytu6()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var r = Dbg.SelectGRect();
                var ret = Dbg.Editor.SelectCrossingWindow(r.LeftTop.ToPoint3d(), r.RightButtom.ToPoint3d());
                if (ret.Status == PromptStatus.OK)
                {
                    var ents = ret.Value.GetObjectIds().Select(id => adb.Element<Entity>(id)).ToList();
                    foreach (var e in ents)
                    {
                        Dbg.ShowWhere(e);
                    }
                }
            }
        }
        [Feng]
        public static void qtppl7()
        {
            using var bm = new BenchMark();
            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            using (Dbg.DocumentLock)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var getGeoData = fs[i];
                    if (getGeoData == null)
                    {
                        list.Add(null);
                        continue;
                    }
                    var file = files[i];
                    var range = d[file].ToPoint3dCollection();
                    using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                    {
                        var geoData = getGeoData(adb, range);
                        var labelHeight = hs[i];

                        DU.Dispose();
                        var storeys = ThRainSystemService.GetStoreys(range, adb);
                        //{
                        //    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        //    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        //}
                        {
                            var bk = buckets[i];
                            geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                            geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                        }
                        ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                        geoData.FixData();
                        var cadDataMain = RainSystemCadData.Create(geoData);
                        var cadDatas = cadDataMain.SplitByStorey();
                        var sv = new RainSystemService()
                        {
                            Storeys = storeys,
                            GeoData = geoData,
                            CadDataMain = cadDataMain,
                            CadDatas = cadDatas,
                        };
                        sv.CreateDrawingDatas();
                        DU.Dispose();

                        var drDatas = sv.DrawingDatas;

                        //Dbg.PrintText(storeys.ToCadJson());
                        //Dbg.PrintText(drDatas.ToCadJson());
                        list.Add(drDatas);
                    }
                }
            }
            {
                var file = newFileFn();
                //重新设置std的时候用下面这行
                //var file = @"Y:\637577104526532228.json";
                File.WriteAllText(file, list.ToCadJson());
            }
        }
        [Feng]
        public static void qtplas()
        {
            var files = Util1.getFiles();

            var d = Util1.getRangeDict();
            using (Dbg.DocumentLock)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    var range = d[file].ToPoint3dCollection();
                    using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                    {
                        RainSystemGeoData getGeoData(Point3dCollection range)
                        {
                            var labelLines = new List<GLineSegment>();
                            var cts = new List<CText>();
                            var pipes = new List<GRect>();
                            var storeys = new List<GRect>();
                            var wLines = new List<GLineSegment>();
                            var condensePipes = new List<GRect>();
                            var floorDrains = new List<GRect>();
                            var waterWells = new List<GRect>();
                            var waterWellDNs = new List<string>();
                            var waterPortSymbols = new List<GRect>();
                            var waterPort13s = new List<GRect>();
                            var wrappingPipes = new List<GRect>();

                            //using (var adb = AcadDatabase.Active())
                            {
                                //Dbg.BuildAndSetCurrentLayer(adb.Database);

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
                                    var ents = new List<BlockReference>();
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                                    waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                                }
                                {
                                    var ents = new List<Entity>();
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
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
                                    var ents = new List<Entity>();
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
                                }
                                {
                                    foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                                    {
                                        labelLines.Add(e.ToGLineSegment());
                                    }

                                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                                    {
                                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                                    }
                                }

                                {
                                    var pps = new List<Entity>();
                                    var blockNameOfVerticalPipe = "带定位立管";
                                    pps.AddRange(entities.OfType<BlockReference>()
                                     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                                     .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
                                    foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                                    {
                                        if (e is Line line && line.Length > 0)
                                        {
                                            wLines.Add(line.ToGLineSegment());
                                        }
                                        else if (ThRainSystemService.IsTianZhengElement(e))
                                        {
                                            //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                                            //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                                            //{
                                            //    wLines.Add(ln.ToGLineSegment());
                                            //}
                                            foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                                            {
                                                if (ln.Length > 0)
                                                {
                                                    wLines.Add(ln.ToGLineSegment());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            var geoData = new RainSystemGeoData();
                            geoData.Init();
                            geoData.Storeys.AddRange(storeys);
                            geoData.LabelLines.AddRange(labelLines);
                            geoData.WLines.AddRange(wLines);
                            geoData.Labels.AddRange(cts);
                            geoData.VerticalPipes.AddRange(pipes);
                            geoData.CondensePipes.AddRange(condensePipes);
                            geoData.FloorDrains.AddRange(floorDrains);
                            geoData.WaterWells.AddRange(waterWells);
                            geoData.WaterWellLabels.AddRange(waterWellDNs);
                            geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                            geoData.WaterPort13s.AddRange(waterPort13s);
                            geoData.WrappingPipes.AddRange(wrappingPipes);
                            //geoData.SideWaterBuckets.AddRange(xxx);
                            //geoData.GravityWaterBuckets.AddRange(xxx);

                            geoData.FixData();
                            return geoData;
                        }
                        double labelHeight = 350;

                        DU.Dispose();
                        var storeys = ThRainSystemService.GetStoreys(range, adb);
                        var geoData = getGeoData(range);
                        ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                        geoData.FixData();
                        var cadDataMain = RainSystemCadData.Create(geoData);
                        var cadDatas = cadDataMain.SplitByStorey();
                        var sv = new RainSystemService()
                        {
                            Storeys = storeys,
                            GeoData = geoData,
                            CadDataMain = cadDataMain,
                            CadDatas = cadDatas,
                        };
                        sv.CreateDrawingDatas();
                        DU.Dispose();

                        var drDatas = sv.DrawingDatas;

                        //Dbg.PrintText(storeys.ToCadJson());
                        Dbg.PrintText(drDatas.ToCadJson());
                    }
                    if (i >= 5) break;
                }
            }

        }
        public static void qtpp9s(StringBuilder sb, RainSystemDrawingData d1, RainSystemDrawingData d2, string comment = null)
        {
            var s = 0;
            s += qtpla8(sb, d1.RoofLabels, d2.RoofLabels, nameof(d1.RoofLabels));
            s += qtpla8(sb, d1.BalconyLabels, d2.BalconyLabels, nameof(d1.BalconyLabels));
            s += qtpla8(sb, d1.CondenseLabels, d2.CondenseLabels, nameof(d1.CondenseLabels));
            s += qtpla8(sb, d1.CommentLabels, d2.CommentLabels, nameof(d1.CommentLabels));
            s += qtpla8(sb, d1.LongTranslatorLabels, d2.LongTranslatorLabels, nameof(d1.LongTranslatorLabels));
            s += qtpla8(sb, d1.ShortTranslatorLabels, d2.ShortTranslatorLabels, nameof(d1.ShortTranslatorLabels));
            s += qtpla8(sb, d1.GravityWaterBucketTranslatorLabels, d2.GravityWaterBucketTranslatorLabels, nameof(d1.GravityWaterBucketTranslatorLabels));
            s += qtpla8(sb, d1.FloorDrains, d2.FloorDrains, nameof(d1.FloorDrains));
            s += qtpla8(sb, d1.FloorDrainsWrappingPipes, d2.FloorDrainsWrappingPipes, nameof(d1.FloorDrainsWrappingPipes));
            s += qtpla8(sb, d1.WaterWellWrappingPipes, d2.WaterWellWrappingPipes, nameof(d1.WaterWellWrappingPipes));
            s += qtpla8(sb, d1.RainPortWrappingPipes, d2.RainPortWrappingPipes, nameof(d1.RainPortWrappingPipes));
            s += qtpla8(sb, d1.CondensePipes, d2.CondensePipes, nameof(d1.CondensePipes));
            s += qtpla8(sb, d1.OutputTypes, d2.OutputTypes, nameof(d1.OutputTypes));
            s += qtpla8(sb, d1.PipeLabelToWaterWellLabels, d2.PipeLabelToWaterWellLabels, nameof(d1.PipeLabelToWaterWellLabels));
            if (comment != null && s > 0) sb.AppendLine(comment);
        }
        public static int qtpla8<T>(StringBuilder sb, IList<T> lst1, IList<T> lst2, string comment)
        {
            var s = 0;
            var l1 = lst1.Except(lst2).ToList();
            var l2 = lst2.Except(lst1).ToList();
            if (l1.Count > 0)
            {
                sb.AppendLine("- " + comment);
                sb.AppendLine(l1.ToCadJson());
                s += l1.Count;
            }
            if (l2.Count > 0)
            {
                sb.AppendLine("+ " + comment);
                sb.AppendLine(l2.ToCadJson());
                s += l2.Count;
            }
            return s;
        }
        [Feng("test05")]
        public static void qtnetd()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
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
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //此处的“地漏”字样被放在了“可见性”里
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }

            qtjrlj(getGeoData, 250);
        }
        [Feng("test12")]
        public static void qtpiuh()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
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
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 350);
        }
        [Feng("test11")]
        public static void qtpgkd()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
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
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 <= c.Radius && c.Radius <= 30));//暂时调成30
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
                        //pps.AddRange(entities.OfType<Circle>().Where(c => 30 < c.Radius && c.Radius < 100));//最好加上图层
                        pps.AddRange(entities.OfType<BlockReference>()
                            .Where(e =>
                            {
                                return e.ObjectId.IsValid && (e.Layer == "W-RAIN-PIPE-RISR" || e.Layer == "W-DRAI-NOTE")
                                 && !e.ToDataItem().EffectiveName.Contains("井");
                            }));
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 250);
        }
        [Feng("test10")]
        public static void qtpi1j()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
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
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 <= c.Radius && c.Radius <= 30));//暂时调成30
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
                        pps.AddRange(entities.OfType<Circle>().Where(c => 30 < c.Radius && c.Radius < 100));//暂时调成30
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 250);
        }
        [Feng("test06")]
        public static void qtpfjy()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
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
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 250);
        }
        [Feng("test04")]
        public static void qtlbfr()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
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
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));

                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR" || x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("地漏")));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
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
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }

            qtjrlj(getGeoData, 350);
        }
        static void qtjrlj(Func<Point3dCollection, RainSystemGeoData> getGeoData, double labelHeight)
        {

            AddButton("直接跑", () =>
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                {
                    try
                    {
                        DU.Dispose();
                        var range = Dbg.SelectRange();
                        var basePt = Dbg.SelectPoint();
                        ThRainSystemService.ImportElementsFromStdDwg();
                        var storeys = ThRainSystemService.GetStoreys(range, adb);
                        var geoData = getGeoData(range);
                        ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                        geoData.FixData();
                        var cadDataMain = RainSystemCadData.Create(geoData);
                        var cadDatas = cadDataMain.SplitByStorey();
                        var sv = new RainSystemService()
                        {
                            Storeys = storeys,
                            GeoData = geoData,
                            CadDataMain = cadDataMain,
                            CadDatas = cadDatas,
                        };
                        sv.CreateDrawingDatas();
                        if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                        DU.Dispose();
                        sv.RainSystemDiagram.Draw(basePt);
                        DU.Draw(adb);
                    }
                    //catch (System.Exception ex)
                    //{
                    //    MessageBox.Show(ex.Message);
                    //}
                    finally
                    {
                        DU.Dispose();
                    }
                }
            });
            AddButton("输出绘图数据", () =>
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                {
                    DU.Dispose();
                    var range = Dbg.SelectRange();
                    //var basePt = Dbg.SelectPoint();
                    ThRainSystemService.ImportElementsFromStdDwg();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = getGeoData(range);
                    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    Dbg.PrintText(sv.DrawingDatas.ToCadJson());
                    DU.Dispose();
                }
            });
            AddLazyAction("准备画骨架", adb =>
            {
                var range = Dbg.SelectRange();

                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = getGeoData(range);
                geoData.FixData();
                DrawSkeletonLazy(geoData);

                AddLazyAction("生成绘图数据并绘制", adb =>
                {
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    BuildDrawingDatas(storeys, geoData);
                });
            });
        }
        [Feng("💰自定义空间索引")]
        public static void qthme9()
        {

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var line1 = Dbg.SelectEntity<Line>(adb);
                var seg1 = line1.ToGLineSegment();
                var si = GLineSegmentConnectionNTSSpacialIndex.Create(new GLineSegment[] { seg1 }, 10);
                var lst = si.SelectCrossingGRect(Dbg.SelectGRect());
                Dbg.PrintLine(lst.Count.ToString());
            }
        }
        public static void qtla9m(IList<Geometry> geos)
        {
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            Polygon polygon = null;
            var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(polygon);
            var q = engine.Query(polygon.EnvelopeInternal).Where(geo => gf.Intersects(geo));
        }
        public static Polygon ToNTSPolygon(Polyline poly)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                return poly.ToNTSPolygon();
            }
        }
        public static List<Polygon> ToNTSPolygons(IList<Polyline> polys)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                return polys.Select(pl => pl.ToNTSPolygon()).ToList();
            }
        }



        public static void qtk54o()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var numPoints = 6;
                // 获取圆的外接矩形
                var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
                {
                    NumPoints = numPoints,
                    Size = 2 * 1000,
                    Centre = Dbg.SelectPoint().ToNTSCoordinate(),
                };
                var ring = shapeFactory.CreateCircle().Shell;
                DU.DrawLinearRing(ring);
            }

        }
        public class GLineSegmentConnectionNTSSpacialIndex : NTSSpacialIndexAB<GLineSegment>
        {
            double radius;
            protected GLineSegmentConnectionNTSSpacialIndex() : base() { }
            public static GLineSegmentConnectionNTSSpacialIndex Create(IEnumerable<GLineSegment> lines, double radius)
            {
                var si = new GLineSegmentConnectionNTSSpacialIndex();
                si.radius = radius;
                foreach (var seg in lines)
                {
                    si.dict[si.ToNTSGeometry(seg)] = seg;
                }
                si.InitEngine();
                return si;
            }
            public override Geometry ToNTSGeometry(GLineSegment seg)
            {
                var points1 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.StartPoint, this.radius));
                var points2 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.EndPoint, this.radius));
                var ring1 = new LinearRing(points1);
                var ring2 = new LinearRing(points2);
                var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
                return geo;
            }
        }
        public class GCircleNTSSpacialIndex : NTSSpacialIndexAB<GCircle>
        {
            protected GCircleNTSSpacialIndex() : base() { }
            public static GCircleNTSSpacialIndex Create(IEnumerable<GCircle> lines)
            {
                var si = new GCircleNTSSpacialIndex();
                foreach (var seg in lines)
                {
                    si.dict[si.ToNTSGeometry(seg)] = seg;
                }
                si.InitEngine();
                return si;
            }
            public override Geometry ToNTSGeometry(GCircle circle)
            {
                var numPoints = 6;
                // 获取圆的外接矩形
                var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
                {
                    NumPoints = numPoints,
                    Size = 2 * circle.Radius,
                    Centre = circle.Center.ToNTSCoordinate(),
                };
                return shapeFactory.CreateCircle().Shell;
            }
        }


        public abstract class NTSSpacialIndexAB<T>
        {
            public NetTopologySuite.Index.Strtree.STRtree<Geometry> Engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            public Dictionary<Geometry, T> dict = new Dictionary<Geometry, T>();
            public void InitEngine()
            {
                if (dict.Keys.Count == 0) throw new System.Exception("索引数组为空");
                dict.Keys.ForEach(g => Engine.Insert(g.EnvelopeInternal, g));
            }
            public static Polygon ToNTSPolygon(Polyline polyLine)
            {
                var geometry = polyLine.ToNTSLineString();
                if (geometry is LinearRing ring)
                {
                    return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(ring);
                }
                else
                {
                    //return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                    return null;
                }
            }
            public List<T> SelectCrossingGRect(GRect gRect)
            {
                var geometry = ConvertToNTSPolygon(gRect);
                if (geometry == null) return new List<T>();
                return CrossingFilter(
                    Query(geometry.EnvelopeInternal),
                    ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry))
                    .ToList();
            }

            public static Polygon ConvertToNTSPolygon(GRect gRect)
            {
                if (!gRect.IsValid) return null;
                Coordinate[] points = GeoNTSConvertion.ConvertToCoordinateArray(gRect);
                var ring = ThCADCoreNTSService.Instance.GeometryFactory.CreateLinearRing(points);
                var geometry = ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(ring);
                return geometry;
            }



            public List<T> SelectCrossingPolygon(Polyline polyline)
            {
                var geometry = ToNTSPolygon(polyline);
                if (geometry == null) return new List<T>();
                return CrossingFilter(
                    Query(geometry.EnvelopeInternal),
                    ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry))
                    .ToList();
            }
            public IEnumerable<T> CrossingFilter(IEnumerable<T> objs, NetTopologySuite.Geometries.Prepared.IPreparedGeometry preparedGeometry)
            {
                return objs.Where(o => Intersects(preparedGeometry, o));
            }
            public bool Intersects(NetTopologySuite.Geometries.Prepared.IPreparedGeometry preparedGeometry, T key)
            {
                return preparedGeometry.Intersects(ToNTSGeometry(key));
            }
            private IEnumerable<T> Query(Envelope envelope)
            {
                foreach (var geometry in Engine.Query(envelope))
                {
                    if (dict.TryGetValue(geometry, out T value)) yield return value;
                }
            }
            public abstract Geometry ToNTSGeometry(T key);
        }

        private static void NewMethod()
        {
            ThWRainSystemDiagram.DrawingTest();
        }




        private static void DrawRainSystemDiagram(ThWRainSystemDiagram dg, Point3d basePt)
        {
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                dg.Draw(basePt);
            }
        }
        [Feng("ThRainSystemService.DrawRainSystemDiagram1();")]
        public static void qthlws()
        {
            ThRainSystemService.DrawRainSystemDiagram1();
        }
        public static void qty6kj()
        {
            var storeys = Util1.LoadCadData<Dictionary<string, List<ThStoreysData>>>(FengKeys.StoreysJsonData210519).Values.First();
            var geoData = File.ReadAllText(qtdwh3).FromCadJson<RainSystemGeoData>();
            {
                //导入雨水斗数据
                var items = LoadData<List<RainSystemGeoData>>(FengKeys.WaterBucketsJsonData210519, Util1.cvt4);
                var data = items.First();
                geoData.SideWaterBuckets.AddRange(data.SideWaterBuckets);
                geoData.GravityWaterBuckets.AddRange(data.GravityWaterBuckets);
            }
            NewMethod1(storeys, geoData);

        }

        private static void NewMethod1(List<ThStoreysData> storeys, RainSystemGeoData geoData)
        {


            AddButton("打印立管label", () =>
            {
                ThRainSystemService.PreFixGeoData(geoData, 150);
                geoData.FixData();
                var cadDataMain = RainSystemCadData.Create(geoData);
                var cadDatas = cadDataMain.SplitByStorey();
                var sb = new StringBuilder(8192);
                for (int i = 0; i < geoData.Storeys.Count; i++)
                {
                    var r = geoData.Storeys[i];
                    var s = storeys[i];
                    sb.AppendLine("楼层");
                    sb.AppendLine(s.Storeys.ToJson());
                    sb.AppendLine(s.StoreyType.ToString());
                    var item = cadDatas[i];

                    var wantedLabels = new List<string>();
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        if (RainSystemService.IsWantedText(m.Text))
                        {
                            wantedLabels.Add(m.Text);
                        }
                    }
                    sb.AppendLine("立管");
                    sb.AppendLine(ThRainSystemService.GetRoofLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetBalconyLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetCondenseLabels(wantedLabels).ToJson());
                    {
                        var lst = wantedLabels.Where(x => ThRainSystemService.HasGravityLabelConnected(x)).ToList();
                        if (lst.Count > 0)
                        {
                            sb.AppendLine("特殊处理的text");
                            sb.AppendLine(lst.ToJson());
                        }
                    }
                }
                Dbg.PrintText(sb.ToString());

            });
            AddLazyAction("生成绘图数据", adb =>
            {
                ThRainSystemService.PreFixGeoData(geoData, 150);
                BuildDrawingDatas(storeys, geoData);
            });

            //qtduqc(geoData);
        }

        private static void BuildDrawingDatas(List<ThStoreysData> storeys, RainSystemGeoData geoData)
        {

            geoData.FixData();
            var cadDataMain = RainSystemCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            var sv = new RainSystemService()
            {
                Storeys = storeys,
                GeoData = geoData,
                CadDataMain = cadDataMain,
                CadDatas = cadDatas,
            };
            sv.CreateDrawingDatas();

            AddButton("画最终输出", () =>
            {
                Dbg.FocusMainWindow();
                var basePt = Dbg.SelectPoint();
                ThRainSystemService.ImportElementsFromStdDwg();
                if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                DrawRainSystemDiagram(sv.RainSystemDiagram, basePt);
            });
        }
        [Feng]
        public static void qtduwv()
        {
            var geoData = qtdtsl();
            var file = @"D:\DATA\temp\" + DateTime.Now.Ticks + ".json";
            File.WriteAllText(file, geoData.ToCadJson());
            Dbg.PrintLine(file);
        }

        private static void qtduvb()
        {
            var geoData = File.ReadAllText(qtdwh3).FromCadJson<RainSystemGeoData>();
            DrawSkeletonLazy(geoData);
        }

        private static void qtdtx9()
        {
            DrawSkeletonLazy(qtdtsl());
        }
        public static void AddButton(string name, Action f)
        {
            Util1.AddButton(name, f);
        }
        public static void AddLazyAction(string name, Action<AcadDatabase> f)
        {
            Util1.AddLazyAction(name, f);
        }
        private static void DrawSkeletonLazy(RainSystemGeoData geoData)
        {
            var cadDataMain = RainSystemCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();

            Util1.AddLazyAction("画骨架", adb =>
            {
                //Dbg.PrintLine(lst.Count);
                for (int i = 0; i < cadDatas.Count; i++)
                {
                    {
                        var s = geoData.Storeys[i];
                        var e = DU.DrawRectLazy(s);
                        e.ColorIndex = 1;
                    }
                    var item = cadDatas[i];
                    foreach (var o in item.LabelLines)
                    {
                        var j = cadDataMain.LabelLines.IndexOf(o);
                        var m = geoData.LabelLines[j];
                        var e = DU.DrawLineSegmentLazy(m);
                        e.ColorIndex = 1;
                    }
                    foreach (var o in item.WLines)
                    {
                        var j = cadDataMain.WLines.IndexOf(o);
                        var m = geoData.WLines[j];
                        var e = DU.DrawLineSegmentLazy(m);
                        e.ColorIndex = 4;
                    }
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                        e.ColorIndex = 2;
                        var _pl = DU.DrawRectLazy(m.Boundary);
                        _pl.ColorIndex = 2;
                    }
                    foreach (var o in item.VerticalPipes)
                    {
                        var j = cadDataMain.VerticalPipes.IndexOf(o);
                        var m = geoData.VerticalPipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 3;
                    }

                    foreach (var o in item.CondensePipes)
                    {
                        var j = cadDataMain.CondensePipes.IndexOf(o);
                        var m = geoData.CondensePipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 2;
                    }
                    foreach (var o in item.FloorDrains)
                    {
                        var j = cadDataMain.FloorDrains.IndexOf(o);
                        var m = geoData.FloorDrains[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 6;
                    }
                    foreach (var o in item.WaterWells)
                    {
                        var j = cadDataMain.WaterWells.IndexOf(o);
                        var m = geoData.WaterWells[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                    }
                    {
                        var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                        foreach (var o in item.WaterPortSymbols)
                        {
                            var j = cadDataMain.WaterPortSymbols.IndexOf(o);
                            var m = geoData.WaterPortSymbols[j];
                            var e = DU.DrawRectLazy(m);
                            e.Color = cl;
                        }
                        foreach (var o in item.WaterPort13s)
                        {
                            var j = cadDataMain.WaterPort13s.IndexOf(o);
                            var m = geoData.WaterPort13s[j];
                            var e = DU.DrawRectLazy(m);
                            e.Color = cl;
                        }
                    }
                    {
                        var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                        foreach (var o in item.WrappingPipes)
                        {
                            var j = cadDataMain.WrappingPipes.IndexOf(o);
                            var m = geoData.WrappingPipes[j];
                            var e = DU.DrawRectLazy(m);
                            e.Color = cl;
                        }
                    }
                }
            });
        }
    }
}


//#endif



