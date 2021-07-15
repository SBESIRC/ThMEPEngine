//this file is for debugging only by Feng

//#if DEBUG
#pragma warning disable

using System;
using System.Text;

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
                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(e=> e.GetEffectiveName()==""))
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
                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(e=> e.GetEffectiveName()==""))
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
                var lst = adb.ModelSpace.OfType<BlockReference>().Where(x => x.ObjectId.IsValid).Where(x => x.GetEffectiveName() == "地漏系统").ToList();
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
                var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(pls.ToCollection());
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
        public class DebugTools
        {
            [Feng]
            public static void CollectEntitiesCodegen()
            {
                Dbg.FocusMainWindow();
                using (var adb = AcadDatabase.Active())
                {
                    while (true)
                    {
                        var e = Dbg.TrySelectEntity<Entity>(adb);
                        if (e == null) break;
                        {
                            NewMethod3(e);
                        }
                    }
                }
            }
            [Feng("收集范围内的图层")]
            public static void qw4kwo()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var r = Dbg.SelectGRect();
                    var h = new HashSet<string>();
                    foreach (var e in adb.ModelSpace.OfType<Entity>())
                    {
                        if (e.Bounds.HasValue)
                        {
                            if (r.ContainsRect(e.Bounds.ToGRect()))
                            {
                                h.Add(e.Layer);
                            }
                        }
                    }
                    Dbg.PrintLine(h.ToJson());

                }
            }
            [Feng("天正信息提取")]
            public static void NewMethod3()
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
                    Dbg.PrintLine(System.ComponentModel.TypeDescriptor.GetClassName(e.AcadObject));
                    var dict = new Dictionary<string, object>();
                    foreach (System.ComponentModel.PropertyDescriptor pi in System.ComponentModel.TypeDescriptor.GetProperties(e.AcadObject))
                    {
                        var t = pi.PropertyType;
                        if (t == typeof(string) || t == typeof(double) || t == typeof(float) || t == typeof(int) || t == typeof(long) || t == typeof(Point2d) || t == typeof(Point3d))
                        {
                            var v = pi.GetValue(e.AcadObject);
                            dict[pi.Name] = v;
                        }
                    }
                    Dbg.PrintText(dict.ToCadJson());
                }
            }
            [Feng]
            public static void GetMText()
            {
                Dbg.FocusMainWindow();
                using (var adb = AcadDatabase.Active())
                {
                    var t = Dbg.SelectEntity<MText>(adb);
                    Dbg.PrintText(t.Text);
                    Dbg.PrintText(t.Contents);
                }
            }
            [Feng]
            public static void ShowBoundary()
            {
                Dbg.FocusMainWindow();
                using (var adb = AcadDatabase.Active())
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);
                    var e = Dbg.TrySelectEntity<Entity>(adb);
                    DU.DrawRectLazy(e.Bounds.ToGRect());
                    DU.Draw(adb);
                }
            }
            [Feng]
            public static void GetAllBlocks()
            {
                Dbg.FocusMainWindow();
                using (var adb = AcadDatabase.Active())
                {
                    Dbg.SetText(adb.Blocks.Select(x => x.Name));
                }
            }
            private static void NewMethod3(Entity e)
            {
                //var source = "adb.ModelSpace";
                var source = "entities";
                if (ThRainSystemService.IsTianZhengElement(e))
                {
                    Dbg.PrintLine($"{source}.OfType<Entity>().Where( e=>e.Layer=={e.Layer.ToJson()} && e.GetRXClass().DxfName.ToUpper()=={e.GetRXClass().DxfName.ToUpper().ToJson()})");
                }
                else if (e is Line)
                {
                    Dbg.PrintLine($"{source}.OfType<Line>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                }
                else if (e is Polyline)
                {
                    Dbg.PrintLine($"{source}.OfType<Polyline>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                }
                else if (e is Circle)
                {
                    Dbg.PrintLine($"{source}.OfType<Circle>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                }
                else if (e is DBText)
                {
                    Dbg.PrintLine($"{source}.OfType<DBText>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                }
                else if (e is MText)
                {
                    Dbg.PrintLine($"{source}.OfType<MText>().Where( e=>e.Layer=={e.Layer.ToJson()})");
                }
                else if (e is BlockReference br)
                {
                    Dbg.PrintLine($"{source}.OfType<BlockReference>().Where( e=>e.Layer=={e.Layer.ToJson()} && e.ObjectId.IsValid && e.GetEffectiveName()=={br.GetEffectiveName().ToJson()}) ");
                }
            }
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
            public static void GetEntityBlockEffectiveName()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e = Dbg.SelectEntity<BlockReference>(adb);
                    Dbg.PrintLine(e.GetEffectiveName());
                }
            }
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
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
            [Feng]
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
                DU.DrawBlockReference(blkName: "*U349", basePt: pt, cb: br => DU.SetLayerAndByLayer("W-BUSH", br));
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
                DU.DrawBlockReference(blkName: "*U348", basePt: pt, scale: 2, cb: br => DU.SetLayerAndByLayer("W-RAIN-EQPM", br));
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
                DU.SetLayerAndByLayer("W-RAIN-DIMS", t);
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
                DU.SetLayerAndByLayer("W-RAIN-EQPM", c1);
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
                DU.SetLayerAndByLayer("W-RAIN-EQPM", c1, c2);
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
                Dbg.ShowString(e.Layer + " " + e.GetEffectiveName());
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
        public class FengDbgTesting
        {
            public static void InitButtons()
            {
                register(); qt8ddf();
            }
            public static void AddButton(string name, Action f)
            {
                if (!Dbg.isDebugging) return;
                ((Action<object, string, Action>)ctx["addBtn"])(ctx["currentPanel"], name, f);
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
            private static void register()
            {
                THDrainageService.Register(AddButton);
                RegisterNonPublic(typeof(THDrainageService));
                RegisterWithAttribute(typeof(Test1));
                Register(typeof(DrainageTest));
                RegisterWithAttribute(typeof(Test1.DrainageTest));
                RegisterWithAttribute(typeof(DebugNs.FengDbgTesting));
            }

            private static void RegisterWithAttribute(Type t)
            {
                if (t.GetCustomAttribute<FengAttribute>() != null)
                {
                    Register(t);
                }
            }
            public static void RegisterNonPublic(Type targetType)
            {
                var attrType = ((Assembly)ctx["currentAsm"]).GetType(typeof(FengAttribute).FullName);
                foreach (var mi in ((Assembly)ctx["currentAsm"]).GetType(targetType.FullName).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
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
            public static void Register(Type targetType)
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
                        FengDbgTesting.InitButtons();
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
        public static T LoadFromTempJsonFile<T>(string name)
        {
            return LoadFromJsonFile<T>(@"Y:\" + name + ".json");
        }
        public static T LoadFromJsonFile<T>(string file)
        {
            return File.ReadAllText(file).FromCadJson<T>();
        }
        public static void AddButton(string name, Action f)
        {
            FengDbgTesting.AddButton(name, f);
        }
        public static void AddLazyAction(string name, Action<AcadDatabase> f)
        {
            FengDbgTesting.AddLazyAction(name, f);
        }
        public static void SaveToTempJsonFile(object obj, string filename = null)
        {
            filename ??= DateTime.Now.Ticks.ToString();
            var file = @"Y:\" + filename + ".json";
            File.WriteAllText(file, obj.ToCadJson());
            Dbg.PrintLine(file);
        }
        public static void SaveToJsonFile(object obj, string filename = null)
        {
            filename ??= DateTime.Now.Ticks.ToString();
            var file = @"D:\DATA\temp\" + filename + ".json";
            File.WriteAllText(file, obj.ToCadJson());
            Dbg.PrintLine(file);
        }
        public static void UnHighLight(IEnumerable<Entity> ents)
        {
            HighlightHelper.UnHighLight(ents);
        }
        public static void HighLight(IEnumerable<Entity> ents)
        {
            HighlightHelper.HighLight(ents);
        }
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
        public static void Log(object obj)
        {
            ((Action<string>)Dbg.ctx["thape_logger"])(obj.ToCadJson());
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
        public static bool _;
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
        public static void LayerThreeAxes(List<string> layers)
        {
            static void EnsureLayerOn(string layerName)
            {
                var id = DbHelper.GetLayerId(layerName);
                id.QOpenForWrite<LayerTableRecord>(layer =>
                {
                    layer.IsLocked = false;
                    layer.IsFrozen = false;
                    layer.IsHidden = false;
                    layer.IsOff = false;
                });
            }
            foreach (var layer in layers)
            {
                try
                {
                    //Dreambuild.AutoCAD.DbHelper.EnsureLayerOn(layer);
                    EnsureLayerOn(layer);
                }
                catch { }
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
            targetLayerName ??= TEST_GEO_PREFIX + Guid.NewGuid().ToString("N");
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
        public static void UnlockCurrentLayer()
        {
            using var adb = AcadDatabase.Active();
            var db = adb.Database;
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            foreach (var layerId in lt)
            {
                if (db.Clayer == layerId)
                {
                    LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
                    if (ltr != null)
                    {
                        if (ltr.IsFrozen)
                        {
                            ltr.IsFrozen = false;
                        }
                        if (ltr.IsLocked)
                        {
                            ltr.IsLocked = false;
                        }
                        if (ltr.IsPlottable)
                        {
                            ltr.IsPlottable = false;
                        }
                        if (ltr.IsOff)
                        {
                            ltr.IsOff = false;
                        }
                    }
                    return;
                }
            }
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

        public static List<Entity> SelectEntitiesEx(AcadDatabase adb)
        {
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择",
                };
                var result = Active.Editor.GetSelection(options);
                if (result.Status == PromptStatus.OK)
                {
                    var selectedIds = result.Value.GetObjectIds();
                    return selectedIds.Select(id => adb.Element<Entity>(id)).ToList();
                }
                return null;
            }
            if (Dbg._)
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择楼层框线",
                    //RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                        RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
            }
        }
        public static T SelectEntity<T>(AcadDatabase adb) where T : DBObject
        {
            return ThDebugDrawer.GetEntity<T>(adb);
        }
        public static T TrySelectEntity<T>(AcadDatabase adb) where T : DBObject
        {
            var ed = Active.Editor;
            var opt = new PromptEntityOptions("请选择");
            var ret = ed.GetEntity(opt);
            if (ret.Status != PromptStatus.OK) return null;
            return adb.Element<T>(ret.ObjectId);
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
                ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName().Contains("地漏")));
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
                ents.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name == "CYSD" || x.GetEffectiveName() == "CYSD"));
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
                 .Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == blockNameOfVerticalPipe));
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
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
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
        public static ObjectIdCollection VisibleEntities(this ThBlockReferenceData blockReference, string propName = "可见性1")
        {
            var objs = new ObjectIdCollection();
            var visibilityStates = DynablockVisibilityStates(blockReference);
            var properties = blockReference.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(o => o.PropertyName == propName);
            foreach (var property in properties)
            {
                visibilityStates.Where(o => o.Key == property.Value as string)
                    .ForEach(o => objs.Add(o.Value));
            }
            return objs;
        }

        public static void AddButton(string name, Action f)
        {
            FengDbgTest.FengDbgTesting.AddButton(name, f);
        }
        public static void AddLazyAction(string name, Action<AcadDatabase> f)
        {
            FengDbgTest.FengDbgTesting.AddButton(name, () =>
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
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() == "TCH_TEXT"))
                    {
                        foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                        {
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





        #region UTIL1



        public static T LoadCadData<T>(string name)
        {
            return LoadData<T>(name, cvt4);
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
                     .Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == blockNameOfVerticalPipe));
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

            FengDbgTest.FengDbgTesting.AddButton("--", () =>
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.GetEffectiveName().Contains("地漏")));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
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
                     .Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == blockNameOfVerticalPipe));
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

        #endregion
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.GetEffectiveName().Contains("地漏")));
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
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
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
                     .Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == blockNameOfVerticalPipe));
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
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName().Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
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

                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector() { adb = adb, geoData = geoData };
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
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.GetEffectiveName().Contains("地漏")));
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
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
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
                                     .Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == blockNameOfVerticalPipe));
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



//this file is for debugging only by Feng

//#if DEBUG




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
    using NetTopologySuite.Operation.OverlayNG;
    using NetTopologySuite.Operation.Overlay;
    using NetTopologySuite.Algorithm;
#pragma warning disable
    public static class HighlightHelper
    {
        public static Point2d GetCurrentViewSize()
        {
            double h = (double)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWSIZE");
            Point2d screen = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");
            double w = h * (screen.X / screen.Y);
            return new Point2d(w, h);
        }
        public static Extents2d GetCurrentViewBound(double shrinkScale = 1.0)
        {
            Point2d vSize = GetCurrentViewSize();
            Point3d center = ((Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR")).
                    TransformBy(Active.Editor.CurrentUserCoordinateSystem);
            double w = vSize.X * shrinkScale;
            double h = vSize.Y * shrinkScale;
            Point2d minPoint = new Point2d(center.X - w / 2.0, center.Y - h / 2.0);
            Point2d maxPoint = new Point2d(center.X + w / 2.0, center.Y + h / 2.0);
            return new Extents2d(minPoint, maxPoint);
        }
        public static void HighLight(IEnumerable<Entity> ents)
        {
            //var extents = ThAuxiliaryUtils.GetCurrentViewBound();
            var extents = GetCurrentViewBound();
            foreach (var e in ents)
            {
                if (!e.IsErased && !e.IsDisposed && e.Bounds is Extents3d ext)
                {
                    if (IsInActiveView(ext.MinPoint,
                        extents.MinPoint.X, extents.MaxPoint.X,
                        extents.MinPoint.Y, extents.MaxPoint.Y) ||
                    IsInActiveView(ext.MaxPoint,
                    extents.MinPoint.X, extents.MaxPoint.X,
                    extents.MinPoint.Y, extents.MaxPoint.Y))
                    {
                        e.Highlight();
                    }
                }
            }
        }
        public static void UnHighLight(IEnumerable<Entity> ents)
        {
            foreach (var e in ents)
            {
                if (!e.IsErased && !e.IsDisposed)
                {
                    e.Unhighlight();
                }
            }
        }
        private static bool IsInActiveView(Point3d pt, double minX, double maxX, double minY, double maxY)
        {
            return pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY;
        }
    }
    public static class CloneHelper
    {
        public static readonly MethodInfo ObjectMemberwiseCloneMethodInfo = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly MethodInfo CopyListMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyList));
        public static readonly MethodInfo CopyHashSetMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyHashSet));
        public static readonly MethodInfo CopyQueueMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyQueue));
        public static readonly MethodInfo CopyStackMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyStack));
        public static readonly MethodInfo CopyObservableCollectionMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyObservableCollection));
        public static readonly MethodInfo CopyArrayMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyArray));
        public static readonly MethodInfo CopyDictionaryMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyDictionary));
        public static readonly MethodInfo CopySortedDictionaryMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopySortedDictionary));
        public static readonly MethodInfo CopySortedListMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopySortedList));
        public static object GetDefaultValue(Type type)
        {
            return type.IsClass ? null : Activator.CreateInstance(type);
        }
        public static List<T> CopyList<T>(List<T> src) => new List<T>(src);
        public static HashSet<T> CopyHashSet<T>(HashSet<T> src) => new HashSet<T>(src);
        public static Queue<T> CopyQueue<T>(Queue<T> src) => new Queue<T>(src);
        public static Stack<T> CopyStack<T>(Stack<T> src) => new Stack<T>(src);
        public static System.Collections.ObjectModel.ObservableCollection<T> CopyObservableCollection<T>(System.Collections.ObjectModel.ObservableCollection<T> src) => new System.Collections.ObjectModel.ObservableCollection<T>(src);
        public static T[] CopyArray<T>(T[] src)
        {
            var dst = new T[src.Length];
            Array.Copy(src, dst, src.Length);
            return dst;
        }
        public static Dictionary<K, V> CopyDictionary<K, V>(Dictionary<K, V> src) => new Dictionary<K, V>(src);
        public static SortedDictionary<K, V> CopySortedDictionary<K, V>(SortedDictionary<K, V> src) => new SortedDictionary<K, V>(src);
        public static SortedList<K, V> CopySortedList<K, V>(SortedList<K, V> src) => new SortedList<K, V>(src);
        public static Func<T, T> MemberwiseCloneF<T>()
        {
            var pe = Expression.Parameter(typeof(T), "v");
            if (typeof(T).IsValueType) return Expression.Lambda<Func<T, T>>(Expression.Block(pe), pe).Compile();
            return Expression.Lambda<Func<T, T>>(Expression.Block(Expression.Convert(Expression.Call(pe, CloneHelper.ObjectMemberwiseCloneMethodInfo), typeof(T))), pe).Compile();
        }

        public static Action<T, T> CopyFieldsMemberwiseF<T>()
        {
            var src = Expression.Parameter(typeof(T), "src");
            var dst = Expression.Parameter(typeof(T), "dst");
            return Expression.Lambda<Action<T, T>>(Expression.Block(typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(fi => Expression.Assign(Expression.Field(dst, fi), Expression.Field(src, fi)))), src, dst).Compile();
        }

        public static Action<T, T> CopyCollectionFieldsF<T>()
        {
            var src = Expression.Parameter(typeof(T), "src");
            var dst = Expression.Parameter(typeof(T), "dst");
            var exprs = new List<Expression>();
            foreach (var fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var mi = TryGetCollectionCloneMethodInfo(fi.FieldType);
                if (mi != null)
                {
                    exprs.Add(Expression.Assign(Expression.Field(dst, fi), Expression.Call(null, mi, Expression.Field(src, fi))));
                }
            }
            return Expression.Lambda<Action<T, T>>(Expression.Block(exprs), src, dst).Compile();
        }
        public static MethodInfo TryGetCollectionCloneMethodInfo(Type type)
        {
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                return CopyArrayMethodInfo.MakeGenericMethod(itemType);
            }
            if (type.IsGenericType)
            {
                var gtypes = type.GetGenericArguments();
                if (gtypes.Length == 1)
                {
                    var itemType = gtypes[0];
                    var gtypeDef = type.GetGenericTypeDefinition();
                    if (gtypeDef == typeof(List<>))
                    {
                        return CopyListMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(System.Collections.ObjectModel.ObservableCollection<>))
                    {
                        return CopyObservableCollectionMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(HashSet<>))
                    {
                        return CopyHashSetMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(Queue<>))
                    {
                        return CopyQueueMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(Stack<>))
                    {
                        return CopyStackMethodInfo.MakeGenericMethod(itemType);
                    }
                }
                else if (gtypes.Length == 2)
                {
                    var gtypeDef = type.GetGenericTypeDefinition();
                    if (gtypeDef == typeof(Dictionary<string, string>).GetGenericTypeDefinition())
                    {
                        var ktype = gtypes[0];
                        var vtype = gtypes[1];
                        return CopyDictionaryMethodInfo.MakeGenericMethod(ktype, vtype);
                    }
                    if (gtypeDef == typeof(SortedDictionary<string, string>).GetGenericTypeDefinition())
                    {
                        var ktype = gtypes[0];
                        var vtype = gtypes[1];
                        return CopySortedDictionaryMethodInfo.MakeGenericMethod(ktype, vtype);
                    }
                    if (gtypeDef == typeof(SortedList<string, string>).GetGenericTypeDefinition())
                    {
                        var ktype = gtypes[0];
                        var vtype = gtypes[1];
                        return CopySortedListMethodInfo.MakeGenericMethod(ktype, vtype);
                    }
                }
            }
            return null;
        }
    }
    public class ReturnBlockBuilder
    {
        public LabelTarget returnTarget;
        public LabelExpression returnLabel;
        public List<Expression> expressions { get; } = new List<Expression>();
        public ReturnBlockBuilder(Type type, object dftValue)
        {
            returnTarget = Expression.Label(type);
            returnLabel = Expression.Label(returnTarget, Expression.Constant(dftValue, type));
        }
        public ReturnBlockBuilder(Type type) : this(type, CloneHelper.GetDefaultValue(type)) { }
        public BlockExpression BuildBlockExpression()
        {
            try
            {
                expressions.Add(returnLabel);
                return Expression.Block(expressions);
            }
            finally
            {
                expressions.Clear();
            }
        }
        public void AddReturnExpression(Expression expr)
        {
            expressions.Add(Expression.Return(returnTarget, expr));
        }
    }



    [Feng]
    public class Test1
    {
        static string ReadString()
        {
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return null;
            return rst.StringResult;
        }
        [Feng("👀")]
        public static void qus5uz()
        {
            Util1.FindText();
        }
        [Feng("❌")]
        public static void qus63i()
        {
            Dbg.DeleteTestGeometries();
        }
        [Feng("DrawDrainageSystemDiagram2")]
        public static void qus6ak()
        {
            DrainageService.DrawDrainageSystemDiagram2();
        }
        [Feng("保存geoData")]
        public static void qutpmu()
        {
            var geoData = DrainageService.CollectGeoData();
            Dbg.SaveToJsonFile(geoData);
        }



        [Feng("直接从geoData生成")]
        public static void qutpt9()
        {
            var file = @"D:\DATA\temp\637595412925029309.json";
            var geoData = Dbg.LoadFromJsonFile<DrainageGeoData>(file);
            DrainageService.TestDrawingDatasCreation(geoData);
        }
        [Feng("直接从drawingDatas draw8")]
        public static void qv92ji()
        {
            var file = @"D:\DATA\temp\637602373354770648.json";
            var drDatas = Dbg.LoadFromJsonFile<List<DrainageDrawingData>>(file);

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var pt = Dbg.SelectPoint().ToPoint2d();
                DrainageSystemDiagram.draw8(drDatas, pt);
            }
        }
        [Feng("draw9")]
        public static void qveh3t()
        {
            var file = @"D:\DATA\temp\637602373354770648.json";
            var drDatas = Dbg.LoadFromJsonFile<List<DrainageDrawingData>>(file);

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var pt = Dbg.SelectPoint().ToPoint2d();
                //DrainageSystemDiagram.draw9(drDatas, pt);
            }
        }
        [Feng("从图纸提取geoData")]
        public static void qv6yoh()
        {
            var geoData = DrainageService.CollectGeoData();
            DrainageService.TestDrawingDatasCreation(geoData);
        }
        [Feng("draw10")]
        public static void qvemj1()
        {
            DrainageSystemDiagram.draw10();
        }

        [Feng("UnlockCurrentLayer")]
        public static void qvenc4()
        {
            using (Dbg.DocumentLock)
                Dbg.UnlockCurrentLayer();
        }
        public static List<Point2d> GetAlivePointsByNTS(List<Point2d> points, double radius)
        {
            var pts = points.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToList();
            var flags = new bool[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                if (!flags[i])
                {
                    for (int j = 0; j < pts.Count; j++)
                    {
                        if (!flags[j])
                        {
                            if (i != j)
                            {
                                if (pts[i].Intersects(pts[j]))
                                {
                                    flags[i] = true;
                                    flags[j] = true;
                                }
                            }
                        }
                    }
                }
            }
            IEnumerable<Point2d> f()
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    if (!flags[i])
                    {
                        yield return points[i];
                    }
                }
            }
            var q = f();
            return q.ToList();
        }


        public static void quu77p()
        {
            var file = @"D:\DATA\temp\637595412925029309.json";
            var geoData = File.ReadAllText(file).FromCadJson<DrainageGeoData>();
            {
                for (int i = 0; i < geoData.DLines.Count; i++)
                {
                    geoData.DLines[i] = geoData.DLines[i].Extend(5);
                }
            }
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var cadData = DrainageCadData.Create(geoData);
                var killer = GeoFac.CreateGeometryEx(cadData.VerticalPipes.Concat(cadData.WaterPorts).Concat(cadData.FloorDrains).ToList());
                var maxDis = 8000;
                var angleTolleranceDegree = 1;
                var lines = geoData.DLines.Where(x => x.Length > 0).Distinct().ToList();
                geoData.DLines.AddRange(GeoFac.AutoConn(lines, killer, maxDis, angleTolleranceDegree));
                DrainageService.DrawGeoData(geoData);
            }
        }

        public class DrainageTest
        {

            [Feng("水管井")]
            public static void quqgdc()
            {
                //6.3.8	水管井的FL
                //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                //水管井的判断：
                //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var list = DrainageService.CollectRoomData(adb);
                    foreach (var kv in list)
                    {
                        if (kv.Key == "水" || kv.Key.Contains("水井") || kv.Key.Contains("水管井"))
                        {
                            Dbg.PrintLine(kv.Key);
                        }
                    }
                }
            }
            [Feng("quqfmg")]
            public static void quqfmg()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var circles = adb.ModelSpace.OfType<Circle>().Select(x => x.ToGCircle()).ToList();
                    var lines = adb.ModelSpace.OfType<Line>().Select(x => x.ToGLineSegment()).ToList();
                    var cs = circles.Select(x => x.ToCirclePolygon(36)).ToList();
                    var ls = lines.Select(x => x.ToLineString()).ToList();
                    var gs = GeoFac.GroupGeometries(ToGeometries(cs, ls));
                    var _gs = new List<List<Geometry>>();
                    foreach (var g in gs)
                    {
                        var _circles = g.Where(x => cs.Contains(x)).ToList();
                        var _lines = g.Where(x => ls.Contains(x)).ToList();
                        var segs = GeoFac.CreateGeometry(_lines).Difference(GeoFac.CreateGeometry(_circles)).ToDbObjects().OfType<Polyline>().SelectMany(x => x.ExplodeToDBObjectCollection().OfType<Line>()).Select(x => x.ToGLineSegment()).Where(x => x.IsValid).ToList();
                        var lst = new List<Geometry>();
                        lst.AddRange(_circles);
                        lst.AddRange(segs.Select(x => x.Extend(.1).ToLineString()));//延长一点点！
                        _gs.Add(lst);
                    }
                    foreach (var g in _gs)
                    {
                        var _circles = g.Where(x => cs.Contains(x)).ToList();
                        foreach (var c in _circles)
                        {
                            var lst = g.ToList();
                            lst.Remove(c);
                            var f = GeoFac.CreateIntersectsSelector(lst);
                            Dbg.PrintLine(f(c).Count);//OK,如果有地漏是串联的，那么这里会等于2，否则等于1
                        }

                    }
                }
            }
            [Feng("在交点处打碎")]
            public static void quqqrp()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var seg1 = Dbg.SelectEntity<Line>(adb).ToGLineSegment();
                    var seg2 = Dbg.SelectEntity<Line>(adb).ToGLineSegment();
                    var geo = seg1.ToLineString().Union(seg2.ToLineString());//MultiLineString
                    var segs = geo.ToDbCollection().OfType<Polyline>().SelectMany(x => x.ExplodeToDBObjectCollection().OfType<Line>()).Select(x => x.ToGLineSegment()).ToList();

                    FengDbgTesting.AddLazyAction("", adb =>
                    {
                        foreach (var seg in segs)
                        {
                            DU.DrawLineSegmentLazy(seg);
                        }
                    });
                }
            }
            [Feng("qurx6s")]
            public static void qurx6s()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var circle = Dbg.SelectEntity<Circle>(adb);
                    //DU.DrawGeometryLazy(circle.ToGCircle().ToCirclePolygon(6));
                    DU.DrawGeometryLazy(circle.ToGCircle().ToCirclePolygon(6, false));
                    //DU.DrawGeometryLazy(circle.ToGCircle().ToCirclePolygon(36));
                }
            }
            [Feng("quqdxf")]
            public static void quqdxf()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var circles = adb.ModelSpace.OfType<Circle>().Select(x => x.ToGCircle()).ToList();
                    var lines = adb.ModelSpace.OfType<Line>().Select(x => x.ToGLineSegment()).ToList();
                    var cs = circles.Select(x => x.ToCirclePolygon(36)).ToList();
                    var ls = lines.Select(x => x.ToLineString()).ToList();
                    var gs = GeoFac.GroupGeometries(ToGeometries(cs, ls));
                    //Dbg.PrintLine(gs.Count);
                    foreach (var g in gs)
                    {
                        var _circles = g.Where(x => cs.Contains(x)).ToList();
                        var _lines = g.Where(x => ls.Contains(x)).ToList();
                        var segs = GeoFac.CreateGeometry(_lines).Difference(GeoFac.CreateGeometry(_circles)).ToDbObjects().OfType<Polyline>().SelectMany(x => x.ExplodeToDBObjectCollection().OfType<Line>()).Select(x => x.ToGLineSegment()).Where(x => x.IsValid).ToList();
                        FengDbgTesting.AddLazyAction("", adb =>
                        {
                            foreach (var c in _circles)
                            {
                                DU.DrawGeometryLazy(c);
                            }
                            foreach (var seg in segs)
                            {
                                DU.DrawLineSegmentLazy(seg);
                            }
                        });
                    }
                }
            }
            public static List<Geometry> ToGeometries(IEnumerable<Geometry> geos1, IEnumerable<Geometry> geos2)
            {
                return geos1.Concat(geos2).ToList();
            }
            public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
            {
                return source1.Concat(source2).ToList();
            }
            public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
            {
                return source1.Concat(source2).Concat(source3).ToList();
            }
            [Feng("quqcqu")]
            public static void quqcqu()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var e1 = Dbg.SelectEntity<Line>(adb);
                    var e2 = Dbg.SelectEntity<Circle>(adb);
                    var g1 = e1.ToGLineSegment().ToLineString();
                    var g2 = e2.ToGCircle().ToCirclePolygon(36);
                    var g3 = g1.Difference(g2);
                    FengDbgTesting.AddLazyAction("", _adb =>
                    {
                        DU.DrawGeometryLazy(g3);
                        Dbg.PrintLine(g3.Intersects(g2));
                    });
                }
            }
            [Feng("quqb3e")]
            public static void quqb3e()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    //Dbg.BuildAndSetCurrentLayer(db);
                    var circles = adb.ModelSpace.OfType<Circle>().Select(x => x.ToGCircle()).ToList();
                    var lines = adb.ModelSpace.OfType<Line>().Select(x => x.ToGLineSegment()).ToList();
                    //[{'X':1744.5169050298846,'Y':2095.5695398475646,'Radius':656.30028012291064,'Center':{'Y':2095.5695398475646,'X':1744.5169050298846}}]
                    Dbg.PrintText(circles.ToCadJson());
                    //[{'type':'GLineSegment','values':[1823.533911180044,2017.6666691268156,6103.4608013348316,1256.789502112395]},{'type':'GLineSegment','values':[6103.4608013348316,1256.789502112395,6360.6679478744718,2249.0144581879167]},{'type':'GLineSegment','values':[6360.6679478744718,2249.0144581879167,5038.6232134830043,2326.1303835595991]},{'type':'GLineSegment','values':[5038.6232134830043,2326.1303835595991,4915.1637896213415,2819.6723318304757]},{'type':'GLineSegment','values':[4915.1637896213415,2819.6723318304757,6525.2805081162569,2819.6723318304757]}]
                    Dbg.PrintText(lines.ToCadJson());
                }
            }
            [Feng("quqbet")]
            public static void quqbet()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var circles = "[{'X':1744.5169050298846,'Y':2095.5695398475646,'Radius':656.30028012291064,'Center':{'Y':2095.5695398475646,'X':1744.5169050298846}}]".FromCadJson<List<GCircle>>();
                    var lines = "[{'type':'GLineSegment','values':[1823.533911180044,2017.6666691268156,6103.4608013348316,1256.789502112395]},{'type':'GLineSegment','values':[6103.4608013348316,1256.789502112395,6360.6679478744718,2249.0144581879167]},{'type':'GLineSegment','values':[6360.6679478744718,2249.0144581879167,5038.6232134830043,2326.1303835595991]},{'type':'GLineSegment','values':[5038.6232134830043,2326.1303835595991,4915.1637896213415,2819.6723318304757]},{'type':'GLineSegment','values':[4915.1637896213415,2819.6723318304757,6525.2805081162569,2819.6723318304757]}]".FromCadJson<List<GLineSegment>>();
                    foreach (var e in circles)
                    {
                        DU.DrawGeometryLazy(e);
                    }
                    foreach (var e in lines)
                    {
                        DU.DrawLineSegmentLazy(e);
                    }
                    var nothing = nameof(FengDbgTesting.GetSegsToConnect);
                    var h = GeoFac.LineGrouppingHelper.Create(lines);
                    h.InitPointGeos(radius: 2.5);
                    h.DoGroupingByPoint();
                    h.CalcAlonePoints();
                    h.DistinguishAlonePoints();
                    foreach (var geo in h.GetAlonePoints())
                    {
                        DU.DrawGeometryLazy(geo);
                    }
                    //去掉起点剩下的全是终点
                }
            }


            [Feng("qupz46")]
            public static void qupz46()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var e = new Leader();
                    e.HasArrowHead = true;
                    for (int i = 0; i < 3; i++)
                    {
                        e.AppendVertex(Dbg.SelectPoint());
                    }
                    //e.Layer = "H-DIMS-DUCT";
                    e.Dimasz = 200;
                    //e.Dimtxt = 1000;
                    //e.SetDimstyleData(AcHelper.Collections.Tables.GetDimStyle("TH-DIM100"));

                    //Dbg.PrintLine(AcHelper.Collections.Tables.GetDimStyle("TH-DIM100").ObjectId.ToString());
                    //e.SetDatabaseDefaults(db);
                    DU.DrawEntityLazy(e);

                }
            }
            [Feng("quq0kg")]
            public static void quq0kg()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var e1 = Dbg.SelectEntity<Leader>(adb);
                    Debugger.Break();
                }
            }
            [Feng("qupzpe")]
            public static void qupzpe()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    //var db = adb.Database;
                    //Dbg.BuildAndSetCurrentLayer(db);

                    var e1 = Dbg.SelectEntity<Leader>(adb);
                    var e2 = Dbg.SelectEntity<Leader>(adb);
                    //e2.Dimldrblk = e1.Dimldrblk;//boom
                    //e2.DimensionStyle = e1.DimensionStyle;//boom

                    //var e2=(Leader)e1.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(100,0,0)));
                    //e2.AppendVertex(Dbg.SelectPoint());
                    //DU.DrawEntityLazy(e2);
                }
            }
            [Feng("qupz57")]
            public static void qupz57()
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    //Dbg.BuildAndSetCurrentLayer(db);
                    var e = Dbg.SelectEntity<Leader>(adb);
                    Dbg.PrintLine(e.DimensionStyleName);
                }
            }

        }

    }
    public class ListDict<K, V> : Dictionary<K, List<V>>
    {
        public void Add(K key, V value)
        {
            var d = this;
            if (!d.TryGetValue(key, out List<V> lst))
            {
                lst = new List<V>() { value };
                d[key] = lst;
            }
            else
            {
                lst.Add(value);
            }
        }
    }
    public class CountDict<K> : IEnumerable<KeyValuePair<K, int>>
    {
        Dictionary<K, int> d = new Dictionary<K, int>();
        public int this[K key]
        {
            get
            {
                d.TryGetValue(key, out int value); return value;
            }
            set
            {
                d[key] = value;
            }
        }

        public IEnumerator<KeyValuePair<K, int>> GetEnumerator()
        {
            foreach (var kv in d)
            {
                if (kv.Value > 0) yield return kv;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    public static class CadJsonExtension
    {
        public static string ToCadJson(this object obj)
        {
            return Util1.ToJson(obj);
        }
        public static T FromCadJson<T>(this string json)
        {
            return Util1.FromJson<T>(json);
        }
    }
    public class FengAttribute : Attribute
    {
        public string Title;
        public FengAttribute() { }
        public FengAttribute(string title) { this.Title = title; }
    }

    public static class Matrix2dUtil
    {
        public static readonly Matrix2d Identity = new Matrix2d(new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 });
    }
    public static class DrainageTest
    {





        public static void quizl8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var _segs = NewMethod4();

                foreach (var seg in _segs)
                {
                    DU.DrawLineSegmentLazy(seg);
                }

                var sv = new ThMEPEngineCore.Service.ThLaneLineCleanService();
                sv.ExtendDistance = 1;
                var colle = _segs.Select(x => x.ToCadLine()).ToCollection();
                var ret = sv.Clean(colle);
                foreach (Line e in ret)
                {
                    DU.DrawLineSegmentLazy(e.ToGLineSegment());
                }
            }
        }

        private static List<GLineSegment> NewMethod4()
        {
            var r = "{'type':'GRect','values':[521552.78763576248,867324.05193330813,533133.08130046073,876100.43981294858]}".FromCadJson<GRect>();
            var segs = loadsegs();
            var dlines = segs.Select(x => x.ToLineString()).ToGeometryList();
            var f = GeoFac.CreateContainsSelector(dlines);
            var list = f(r.ToPolygon());
            var ext = new Extents3d();
            foreach (var dline in list)
            {
                var seg = segs[dlines.IndexOf(dline)];
                ext.AddPoint(seg.StartPoint.ToPoint3d());
                ext.AddPoint(seg.EndPoint.ToPoint3d());
            }
            var w = ext.ToGRect();
            var targetWorld = GRect.Create(800, 600);

            //var m=Matrix2d.Displacement(-ext.MinPoint.ToPoint2d().ToVector2d());
            var v = -ext.MinPoint.ToPoint2d().ToVector2d();

            var p1 = ext.MaxPoint.ToPoint2D() + v;
            var p2 = ext.MinPoint.ToPoint2D() + v;
            var kx = targetWorld.Width / (p1.X - p2.X);
            var ky = targetWorld.Height / (p1.Y - p2.Y);

            var _segs = new List<GLineSegment>(segs.Count);
            foreach (var seg in segs)
            {
                //var sp=seg.StartPoint.TransformBy(m);
                //var ep = seg.EndPoint.TransformBy(m);
                var sp = seg.StartPoint + v;
                sp = new Point2d(sp.X * kx, sp.Y * ky);
                var ep = seg.EndPoint + v;
                ep = new Point2d(ep.X * kx, ep.Y * ky);
                _segs.Add(new GLineSegment(sp, ep));
            }

            return _segs;
        }


        public static void quirdk()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                var ee = (Entity)e.Clone();
                DU.DrawRectLazy(ee.Bounds.ToGRect());
            }
        }

        public static void quim6x()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var segs = new List<GLineSegment>();
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-DRAI-DOME-PIPE" && ThRainSystemService.IsTianZhengElement(x)))
                {
                    if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                    {
                        segs.Add(seg);
                    }
                }
                var dlines = segs.Select(x => x.ToLineString()).ToList();
                Dbg.PrintText(segs.ToCadJson());
            }
        }

        public static void quizgv()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            {
                var r = Dbg.SelectGRect();
                Dbg.PrintLine(r.ToCadJson());
            }
        }
        public static List<GLineSegment> loadsegs()
        {
            return File.ReadAllText(@"Y:\xxx.txt").FromCadJson<List<GLineSegment>>().Distinct().ToList();
        }





        public static void quha91()
        {
            //光胜给的提取代码
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var range = Dbg.SelectRange();
                var roomBuilder = new ThRoomBuilderEngine();
                var rooms = roomBuilder.BuildFromMS(db, range);

                foreach (var room in rooms)
                {
                    //内容是空的，算了，还是自己写吧
                    Dbg.PrintLine(room.Name);
                    Dbg.PrintLine(room.Tags.ToJson());
                }
            }
        }
        private static void qugqvl()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //var list=adb.ModelSpace.OfType<DBText>().Where(x => x.Layer == "W-DRAI-EQPM").ToList();
                //Dbg.PrintText(list.Select(x => x.TextString).ToJson());

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName()=="立管编号").ToList();
                //Dbg.PrintLine(list.Count);

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName() == "清扫口系统").ToList();
                //Dbg.PrintLine(list.Count);

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName() == "污废合流井编号").ToList();
                //Dbg.PrintLine(list.Count);
                //foreach (var e in list)
                //{
                //    Dbg.PrintLine(e.GetAttributesStrValue("-"));
                //}


                //Dbg.PrintLine(RXClass.GetClass(Dbg.SelectEntity<Entity>(adb).GetType()).DxfName);//tch_pipe是空的
                //Dbg.PrintLine(RXClass.GetClass(Dbg.SelectEntity<Entity>(adb).AcadObject.GetType()).DxfName);//报错

                //不管了，就这么判断吧
                //var e = Dbg.SelectEntity<Entity>(adb);
                //Dbg.PrintLine(System.ComponentModel.TypeDescriptor.GetClassName(e.AcadObject));//IComPipe
                //Dbg.PrintLine(TypeDescriptor.GetReflectionType(e.AcadObject).ToString());//System.__ComObject
                //Dbg.PrintLine(e.AcadObject.GetType().Assembly.FullName);
                //Dbg.PrintLine(TypeDescriptor.GetComponentName(e.AcadObject));//空的
                //Dbg.PrintLine(e.GetType().Assembly.FullName);//Acdbmgd,...

                //var list=adb.ModelSpace.OfType<Entity>().Where(x => ThRainSystemService.IsTianZhengElement(x)).ToList();
                //Dbg.PrintLine(list.Count);

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR" && x.ObjectId.IsValid).ToList();
                //Dbg.PrintLine(list.Count);


            }
        }



        private static void NewMethod2(Database db)
        {
            var texts = new List<CText>();
            var visitor = new Visitor();
            visitor.ExtractCb = (e, m) =>
            {
                if (e is MText mt)
                {
                    var bd = mt.Bounds;
                    if (bd is Extents3d ext)
                    {
                        ext.TransformBy(m);
                        var r = ext.ToGRect();
                        if (r.IsValid)
                        {
                            var text = mt.Contents;
                            //长这样
                            //"\\A1;Ah2","\\A1;Ah1"
                            if (text.ToLower().Contains("ah1") || text.ToLower().Contains("ah2"))
                            {
                                var ct = new CText() { Text = text, Boundary = r };
                                texts.Add(ct);
                            }
                        }
                    }
                }
            };
            Execute(db, visitor);
            foreach (var ct in texts)
            {
                DU.DrawRectLazy(ct.Boundary);
            }
            //Dbg.PrintLine(texts.Select(x => x.Text).ToJson());
        }
        [Feng("💰准备打开多张图纸")]
        public static void qtjr2w()
        {
            var files = Util1.getFiles();
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                AddButton((i + 1) + " " + Path.GetFileName(file), () =>
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
                });
            }
            AddButton("地上给水排水平面图模板_20210125", () =>
            {
                var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\地上给水排水平面图模板_20210125.dwg";
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
            });
            AddButton("绘图说明_20210326", () =>
            {
                var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\绘图说明_20210326（反馈）.dwg";
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
            });

        }
        [Feng("💰准备打开多张图纸2")]
        public static void quhakv()
        {
            var root = @"E:\thepa_workingSpace\任务资料\任务3\图纸";
            var files = new string[]
            {
$@"{root}\01_蓝光钰泷府二期_框线\FS59P2BC_W20-地上给水排水平面图-副本.dwg",
$@"{root}\02_湖北交投颐和华府_框线\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg",
$@"{root}\03_佳兆业滨江新城_框线\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg",
$@"{root}\04_蓝光未来阅璟_框线\FS5F8704_W20-地上给水排水平面图-送审版.dwg",
$@"{root}\05_清江山水四期_框线\FS55TMPH_W20-地上给水排水平面图.dwg",
$@"{root}\06_庭瑞君越观澜三期_框线\fs57grhn_w20-地上给水排水平面图.dwg",
$@"{root}\07_武汉二七滨江商务区南一片住宅地块_框线\FS5747SS_W20-地上给水排水平面图.dwg",
$@"{root}\08_合景红莲湖项目_框线\FS55TD78_W20-73#-地上给水排水平面图.dwg",
$@"{root}\09_长征村K2地块\FS5F46QE_W20-地上给水排水平面图-Z.dwg",
            };
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var _i = i;
                AddButton((i + 1) + " " + Path.GetFileName(file), () =>
                {
                    Console.WriteLine("图纸" + (_i + 1));
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
                });
            }
            AddButton("全部打开", () =>
            {
                foreach (var file in files)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
                }
            });
        }
        public static void AddButton(string name, Action f)
        {
            Util1.AddButton(name, f);
        }
        private static void Execute(Database db, Visitor visitor)
        {
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(db);
        }

        private static void NewMethod1(Database db)
        {
            var texts = new List<CText>();
            var mtexts = new List<CText>();

            var visitor = new Visitor();
            visitor.ExtractCb = (e, m) =>
            {
                if (e is DBText dbt)
                {
                    //if (string.IsNullOrEmpty(dbt.TextString) || !dbt.TextString.ToLower().Contains("ah2")) return;
                    //var dbt2 = (DBText)dbt.Clone();
                    //dbt2.TransformBy(m);//eCannotScaleNonUniformly
                    //texts.Add(dbt);

                    var bd = dbt.Bounds;
                    if (bd is Extents3d ext)
                    {
                        ext.TransformBy(m);
                        var ct = new CText() { Text = dbt.TextString, Boundary = ext.ToGRect() };
                        texts.Add(ct);
                    }
                }
                else if (e is MText mt)
                {
                    //if (string.IsNullOrEmpty(mt.Contents) || !mt.Contents.ToLower().Contains("ah2")) return;
                    //var mt2 = (MText)mt.Clone();
                    //mt2.TransformBy(m);
                    //mtexts.Add(mt2);

                    var bd = mt.Bounds;
                    if (bd is Extents3d ext)
                    {
                        ext.TransformBy(m);
                        var ct = new CText() { Text = mt.Contents, Boundary = ext.ToGRect() };
                        mtexts.Add(ct);
                    }
                }
            };

            var ranges = new List<Geometry>();
            visitor.DoXClipCb = (xclip) =>
            {
                ranges.Add(xclip.Polygon.ToNTSPolygon());
            };

            Execute(db, visitor);


            Dbg.PrintLine(texts.Count);
            Dbg.PrintLine(mtexts.Count);


            if (ranges.Count > 0)
            {
                var _geo = ranges[0];
                for (int i = 1; i < ranges.Count; i++)
                {
                    //_geo = _geo.Intersection(ranges[i]);
                    _geo = _geo.Union(ranges[i]);
                }
                var bds = texts.Select(x => x.Boundary).Concat(mtexts.Select(x => x.Boundary)).Distinct().ToList();
                var geos = bds.Where(x => x.IsValid).Select(x => x.ToPolygon()).Cast<Geometry>().ToList();
                Dbg.PrintLine(geos.Count);
                var geo = GeoFac.CreateGeometry(geos);
                var f = GeoFac.CreateIntersectsSelector(geos);
                var results = f(_geo);
                Dbg.PrintLine(results.Count);
            }
        }

        public class Visitor : ThBuildingElementExtractionVisitor
        {
            public Action<Entity, Matrix3d> ExtractCb;
            public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
            {
                ExtractCb?.Invoke(dbObj, matrix);
                //if (dbObj is DBText dbText)
                //{
                //    elements.AddRange(HandleDbText(dbText, matrix));
                //}
                //else if (dbObj is MText mText)
                //{
                //    elements.AddRange(HandleMText(mText, matrix));
                //}
            }
            public Action<ThMEPXClipInfo> DoXClipCb;
            public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
            {
                var xclip = blockReference.XClipInfo();
                if (xclip.IsValid)
                {
                    xclip.TransformBy(matrix);
                    //elements.RemoveAll(o => !xclip.Contains(GetTextPosition(o.Geometry)));
                    DoXClipCb?.Invoke(xclip);
                }
            }

        }
        private static void NewMethod(AcadDatabase adb, Database db)
        {
            if (false)
            {
                var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                gravityBucketEngine.Recognize(adb.Database, Dbg.SelectRange());
            }
            var list = new List<string>();
            var visitor = new BlockReferenceVisitor();
            visitor.IsTargetBlockReferenceCb = (br) =>
            {
                //__覆盖_A10-8地上平面_SEN23WUB$0$厨房250X250洞口
                var name = br.GetEffectiveName();
                //list.Add(name);
                if (name.Contains("厨房")) return true;
                return false;
            };
            var rs = new List<GRect>();
            visitor.HandleBlockReferenceCb = (br, m) =>
            {
                var e = br.GetTransformedCopy(m);
                rs.Add(e.Bounds.ToGRect());
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(db);
            //File.WriteAllText(@"Y:\xxxx.json", list.ToJson());
            foreach (var r in rs)
            {
                //Dbg.ShowWhere(r);
                DU.DrawRectLazy(r);
            }
        }
    }
    public class BlockReferenceVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                _HandleBlockReference(elements, blkref, matrix);
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }
        public Func<BlockReference, bool> IsTargetBlockReferenceCb;
        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference blkref)
            {
                return IsTargetBlockReferenceCb(blkref);
                var name = blkref.GetEffectiveName();
                return (ThMEPEngineCore.Service.ThGravityWaterBucketLayerManager.IsGravityWaterBucketBlockName(name));
            }
            return false;
        }
        public Action<BlockReference, Matrix3d> HandleBlockReferenceCb;
        public bool SupportDynamicBlock;
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            //// 暂时不支持动态块，外部参照，覆盖
            //if (blockTableRecord.IsDynamicBlock)
            //{
            //    return false;
            //}

            if (!SupportDynamicBlock)
            {
                if (blockTableRecord.IsDynamicBlock)
                {
                    return false;
                }
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
        private void _HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (!blkref.ObjectId.IsValid) return;
            HandleBlockReferenceCb(blkref, matrix);
        }

        private bool IsContain(ThMEPEngineCore.Algorithm.ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

}

//#endif