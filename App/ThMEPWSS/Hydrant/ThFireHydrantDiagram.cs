namespace ThMEPWSS.FireNumFlatDiagramNs
{
    using AcHelper;
    using AcHelper.Commands;
    using Autodesk.AutoCAD.Colors;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using DotNetARX;
    using Dreambuild.AutoCAD;
    using Linq2Acad;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ThCADExtension;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.ViewModel;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.Runtime;
    using NetTopologySuite.Geometries;
    using NFox.Cad;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using ThMEPEngineCore.Algorithm;
    using ThMEPEngineCore.Engine;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Diagram.ViewModel;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Service;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using static ThMEPWSS.Assistant.DrawUtils;
    using ThMEPEngineCore.Model.Common;
    using NetTopologySuite.Operation.Buffer;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using Newtonsoft.Json.Linq;
    using Exception = System.Exception;
    using NetTopologySuite.Geometries.Prepared;
    using static FlatDiagramService;
    using NetTopologySuite.Algorithm;
    using ThMEPWSS.UndergroundFireHydrantSystem.Service;
    using ThMEPWSS.UndergroundFireHydrantSystem.Model;
    using ThMEPWSS.UndergroundFireHydrantSystem.Method;
    using System.Runtime.Serialization;
    using System.Linq.Expressions;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    public class FireHydrantSystem
    {
        public Dictionary<Point3dEx, List<Point3dEx>> BranchDic;
        public Dictionary<Point3dEx, List<Point3dEx>> ValveDic;
        public List<List<Point3dEx>> MainPathList;
        public List<List<Point3dEx>> SubPathList;
        public FireHydrantSystemIn FireHydrantSystemIn;
        public FireHydrantSystemOut FireHydrantSystemOut;
    }
    public static class FlatDiagramService
    {
        public const string LeaderLayer = THESAURUSBLEMISH;
        public static Geometry GetGeometry(Entity entity)
        {
            if (entity is null) return null;
            if (entity is DBText dbText) return GetGeometry(dbText);
            if (entity is Line line) return GetGeometry(line);
            if (entity is Circle circle) return GetGeometry(circle);
            if (entity is MText mText) return GetGeometry(mText);
            if (entity is Polyline polyline) return GetGeometry(polyline);
            if (entity is MLeader mLeader) return GetGeometry(mLeader);
            if (entity is Leader leader) return GetGeometry(leader);
            throw new NotSupportedException();
        }
        public static Geometry GetGeometry(Circle circle)
        {
            var geo = circle.ToGCircle().ToCirclePolygon(SUPERLATIVENESS);
            geo.UserData = circle;
            return geo;
        }
        public static Geometry GetGeometry(DBText dBText)
        {
            var geo = dBText.Bounds.ToGRect().ToPolygon();
            geo.UserData = dBText;
            return geo;
        }
        public static Geometry GetGeometry(MText mText)
        {
            return GeoFac.CreateGeometry(mText, mText.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static Geometry GetGeometry(Line line)
        {
            var geo = line.ToGLineSegment().ToLineString();
            geo.UserData = line;
            return geo;
        }
        public static Geometry GetGeometry(Polyline polyline)
        {
            return GeoFac.CreateGeometry(polyline, polyline.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static Geometry GetGeometry(Leader leader)
        {
            return GeoFac.CreateGeometry(leader, leader.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static Geometry GetGeometry(MLeader mLeader)
        {
            return GeoFac.CreateGeometry(mLeader, mLeader.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static void DrawDBTextInfoLazy(DBTextInfo dbtInfo)
        {
            if (string.IsNullOrWhiteSpace(dbtInfo.Text)) return;
            var dbt = new DBText()
            {
                TextString = dbtInfo.Text,
                Position = dbtInfo.BasePoint,
                Rotation = dbtInfo.Rotation,
                Height = dbtInfo.Height,
                TextStyleId = GetTextStyleId(dbtInfo.TextStyle),
                WidthFactor = dbtInfo.WidthFactor,
            };
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(dbt));
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, Action f)
        {
            if (seg.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(seg.ToLineString(), f));
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, Action f)
        {
            if (r.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(r.ToPolygon(), f));
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GCircle gc, Action f)
        {
            if (gc.Radius > THESAURUSSTAMPEDE)
            {
                fs.Add(new KeyValuePair<Geometry, Action>(gc.ToCirclePolygon(SUPERLATIVENESS), f));
            }
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GCircle gc, ICollection<GCircle> lst)
        {
            reg(fs, gc, () => lst.Add(gc));
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, DBTextInfo info, ICollection<DBTextInfo> lst)
        {
            reg(fs, info, () => lst.Add(info));
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, DBTextInfo info, Action f)
        {
            if (info.Height <= THESAURUSSTAMPEDE) return;
            var lr = GetObbRing(info);
            if (lr != null)
            {
                fs.Add(new KeyValuePair<Geometry, Action>(lr, f));
            }
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, Action f)
        {
            reg(fs, ct.Boundary, f);
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, ICollection<CText> lst)
        {
            reg(fs, ct, () => { lst.Add(ct); });
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, ICollection<GLineSegment> lst)
        {
            if (seg.IsValid) reg(fs, seg, () => { lst.Add(seg); });
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, ICollection<GRect> lst)
        {
            if (r.IsValid) reg(fs, r, () => { lst.Add(r); });
        }
        public static void cbGenerate()
        {
            try
            {
                Generate(FireHydrantSystemUIViewModel.Singleton);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static Polygon GetObbPolygon(DBTextInfo dt)
        {
            var lr = GetObbRing(dt);
            if (lr is null) return null;
            return new Polygon(lr);
        }
        public static LinearRing GetObbRing(DBTextInfo dt)
        {
            if (string.IsNullOrEmpty((dt.Text))) return null;
            var dbt = new DBText()
            {
                TextString = dt.Text,
                Position = dt.BasePoint,
                Rotation = THESAURUSSTAMPEDE,
                Height = dt.Height,
                TextStyleId = GetTextStyleId(dt.TextStyle),
                WidthFactor = dt.WidthFactor,
            };
            var bd = dbt.Bounds.ToGRect();
            var lr = bd.ToLinearRing2(Matrix3d.Rotation(dt.Rotation, new Vector3d(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE, THESAURUSHOUSING), dt.BasePoint));
            return lr;
        }
        public static HydrantData CollectData(AcadDatabase adb, GRect range)
        {
            FocusMainWindow();
            var hehe = new HydrantData();
            hehe.Range = range;
            var segs = new List<GLineSegment>(THESAURUSREPERCUSSION * THESAURUSTOLERATION);
            var dtexts = new List<DBTextInfo>(THESAURUSREPERCUSSION * THESAURUSTOLERATION);
            hehe.redlines = new HashSet<GLineSegment>(THESAURUSREPERCUSSION);
            hehe.hatches = new HashSet<Geometry>(THESAURUSREPERCUSSION);
            var mi = typeof(ThCADCore.NTS.ThCADCoreNTSHatchExtension).GetMethod(THESAURUSCONDOM, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var pr = Expression.Parameter(typeof(Hatch), ADMINISTRATIONS);
            var ToNTSGeometry = Expression.Lambda<Func<Hatch, Geometry>>(Expression.Call(null, mi, pr), pr).Compile();
            bool isInXref = INTRAVASCULARLY;
            foreach (var entity in adb.ModelSpace.OfType<Entity>())
            {
                if (entity is BlockReference br)
                {
                    if (!br.BlockTableRecord.IsValid) continue;
                    var btr = adb.Blocks.Element(br.BlockTableRecord);
                    var _fs = new List<KeyValuePair<Geometry, Action>>();
                    Action f = null;
                    try
                    {
                        isInXref = btr.XrefStatus != XrefStatus.NotAnXref;
                        handleBlockReference(br, Matrix3d.Identity, _fs);
                    }
                    finally
                    {
                        isInXref = INTRAVASCULARLY;
                    }
                    {
                        var info = br.XClipInfo();
                        if (info.IsValid)
                        {
                            info.TransformBy(br.BlockTransform);
                            var gf = info.PreparedPolygon;
                            foreach (var kv in _fs)
                            {
                                if (gf.Intersects(kv.Key))
                                {
                                    f += kv.Value;
                                }
                            }
                        }
                        else
                        {
                            foreach (var kv in _fs)
                            {
                                f += kv.Value;
                            }
                        }
                        f?.Invoke();
                    }
                }
                else
                {
                    var _fs = new List<KeyValuePair<Geometry, Action>>();
                    handleEntity(entity, Matrix3d.Identity, _fs);
                    foreach (var kv in _fs)
                    {
                        kv.Value();
                    }
                }
            }
            hehe.segs = segs.ToHashSet();
            hehe.dtexts = dtexts;
            return hehe;
            void handle_entity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
            {
                if (entity is Hatch hatch)
                {
                    try
                    {
                        var geo = ToNTSGeometry(hatch).TransformBy(matrix);
                        if (geo is null || geo.IsEmpty) return;
                        var r = hatch.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, r, () =>
                        {
                            hehe.hatches.Add(geo);
                        });
                    }
                    catch { }
                    return;
                }
                {
                    var dxfName = entity.GetRXClass().DxfName.ToUpper();
                    if (dxfName.Contains(QUOTATIONCHROMIC) && dxfName.Contains(QUOTATIONPURKINJE))
                    {
                        var bd = entity.Bounds.ToGRect();
                        if (bd.IsValid)
                        {
                            var r = bd.Expand(HYPERDISYLLABLE);
                            reg(fs, r, () => { segs.AddRange(GeoFac.GetLines(r.ToPolygon().Shell)); });
                        }
                        return;
                    }
                }
                foreach (var ent in explode(entity))
                {
                    if (ent is Line ln && ln.Length > THESAURUSSTAMPEDE && ln.Visible)
                    {
                        var seg = ln.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, segs);
                        reg(fs, seg, () =>
                        {
                            if (UndergroundFireHydrantSystem.Extract.ThExtractHYDTPipeService.IsHYDTPipeLayer(entity.Layer))
                            {
                                hehe.redlines.AddRange(GetGLineSegments(ln).Where(x => x.Length > THESAURUSACRIMONIOUS));
                            }
                        });
                    }
                    else if (ent is DBText dbt && dbt.Visible && !string.IsNullOrWhiteSpace(dbt.TextString))
                    {
                        var info = new DBTextInfo()
                        {
                            Text = dbt.TextString,
                            Height = get_ratio(matrix) * dbt.Height,
                            BasePoint = dbt.Position.TransformBy(matrix),
                            Rotation = dbt.Rotation,
                            TextStyle = dbt.TextStyleName,
                            WidthFactor = dbt.WidthFactor,
                            LayerName = dbt.Layer,
                        };
                        reg(fs, info, dtexts);
                    }
                    else if (ent is Circle circle && circle.Radius > THESAURUSSTAMPEDE)
                    {
                        var gc = new GCircle(circle.Center.TransformBy(matrix).ToPoint2d(), get_ratio(matrix) * circle.Radius);
                        reg(fs, gc, () => { segs.AddRange(GeoFac.GetLines(gc.ToCirclePolygon(SUPERLATIVENESS).Shell)); });
                    }
                    else if (ent is Arc arc)
                    {
                        static IEnumerable<GLineSegment> ConvertToGLineSegments(GArc arc, double tolAngle)
                        {
                            if (tolAngle <= Math.PI / THESAURUSTROUPE) throw new ArgumentOutOfRangeException();
                            int n = THESAURUSHOUSING;
                            double step;
                            arc.StartAngle = GeoFac.FixAngle(arc.StartAngle);
                            arc.EndAngle = GeoFac.FixAngle(arc.EndAngle);
                            var delta = arc.EndAngle - arc.StartAngle;
                            if (arc.IsClockWise)
                            {
                                if (arc.StartAngle < arc.EndAngle)
                                {
                                    delta -= Math.PI * THESAURUSPERMUTATION;
                                }
                            }
                            else
                            {
                                if (arc.StartAngle > arc.EndAngle)
                                {
                                    delta += Math.PI * THESAURUSPERMUTATION;
                                }
                            }
                            do
                            {
                                step = delta / ++n;
                                if (Math.Abs(step) <= tolAngle) break;
                            } while (THESAURUSOBSTINACY);
                            IEnumerable<Point2d> yieldPts()
                            {
                                var st = arc.StartAngle;
                                for (int i = THESAURUSSTAMPEDE; i <= n; i++)
                                {
                                    var angle = st + step * i;
                                    yield return new Point2d(arc.Center.X + arc.Radius * Math.Cos(angle), arc.Center.Y + arc.Radius * Math.Sin(angle));
                                }
                            }
                            return GeoFac.YieldGLineSegments(yieldPts());
                        }
                        segs.AddRange(ConvertToGLineSegments(arc.ToGArc(), Math.PI / THESAURUSBACKER * PROCRASTINATION).Select(seg => seg.TransformBy(matrix)).Where(seg => seg.IsValid));
                    }
                    else if (ent is Curve curve)
                    {
                        var chordHeight = THESAURUSACRIMONIOUS;
                        try
                        {
                            segs.AddRange(GeoFac.YieldGLineSegments(curve.GetGeCurve()?.GetNewSamplePoints(curve.StartParam, curve.EndParam, chordHeight).Select(o => o.Point).Distinct()));
                        }
                        catch { }
                    }
                }
            }
            static double get_ratio(Matrix3d matrix) => new GLineSegment(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE, THESAURUSHOUSING).TransformBy(matrix).Length;
            static bool canExplode(Entity entity)
            {
                var type = entity.GetType();
                return !(type == typeof(Line) || type == typeof(Circle) || type == typeof(DBPoint) || type == typeof(DBText) || type == typeof(Arc));
            }
            static IEnumerable<Entity> _explode(Entity entity)
            {
                try
                {
                    return entity.ExplodeToDBObjectCollection().OfType<Entity>();
                }
                catch
                {
                    return Enumerable.Empty<Entity>();
                }
            }
            static IEnumerable<Entity> explode(Entity entity)
            {
                if (canExplode(entity))
                {
                    foreach (var ent in _explode(entity))
                    {
                        foreach (var e in explode(ent))
                        {
                            yield return e;
                        }
                    }
                }
                else
                {
                    yield return entity;
                }
            }
            static string GetEffectiveLayer(string entityLayer)
            {
                return GetEffectiveName(entityLayer);
            }
            static string GetEffectiveName(string str)
            {
                str ??= THESAURUSDEPLORE;
                var i = str.LastIndexOf(THESAURUSCONTEND);
                if (i >= THESAURUSSTAMPEDE && !str.EndsWith(MULTIPROCESSING))
                {
                    str = str.Substring(i + THESAURUSHOUSING);
                }
                i = str.LastIndexOf(SUPERREGENERATIVE);
                if (i >= THESAURUSSTAMPEDE && !str.EndsWith(THESAURUSCOURIER))
                {
                    str = str.Substring(i + THESAURUSHOUSING);
                }
                return str;
            }
            static string GetEffectiveBRName(string brName)
            {
                return GetEffectiveName(brName);
            }
            static bool IsWantedBlock(BlockTableRecord blockTableRecord)
            {
                if (blockTableRecord.IsDynamicBlock)
                {
                    return INTRAVASCULARLY;
                }
                if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
                {
                    return INTRAVASCULARLY;
                }
                if (!blockTableRecord.Explodable)
                {
                    return INTRAVASCULARLY;
                }
                return THESAURUSOBSTINACY;
            }
            void handleEntity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
            {
                if (!IsLayerVisible(entity) || !entity.Visible) return;
                handle_entity(entity, matrix, fs);
            }
            void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
            {
                if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
                if (!br.Visible) return;
                var _fs = new List<KeyValuePair<Geometry, Action>>();
                void explode_br()
                {
                    foreach (var ent in explode(br))
                    {
                        handleEntity(ent, matrix, _fs);
                    }
                    {
                        var lst = new List<KeyValuePair<Geometry, Action>>();
                        var info = br.XClipInfo();
                        if (info.IsValid)
                        {
                            info.TransformBy(br.BlockTransform.PreMultiplyBy(matrix));
                            var gf = info.PreparedPolygon;
                            foreach (var kv in _fs)
                            {
                                if (gf.Intersects(kv.Key))
                                {
                                    lst.Add(kv);
                                }
                            }
                        }
                        else
                        {
                            foreach (var kv in _fs)
                            {
                                lst.Add(kv);
                            }
                        }
                        fs.AddRange(lst);
                    }
                }
                if (IsLayerVisible(br))
                {
                    var _name = br.GetEffectiveName() ?? THESAURUSDEPLORE;
                    var name = GetEffectiveBRName(_name);
                    if (br.IsDynamicBlock)
                    {
                        explode_br();
                        return;
                    }
                }
                var btr = adb.Element<BlockTableRecord>(br.BlockTableRecord);
                if (btr.Explodable && (btr.IsDynamicBlock || btr.IsAnonymous))
                {
                    explode_br();
                    return;
                }
                if (!IsWantedBlock(btr)) return;
                foreach (var objId in btr)
                {
                    var dbObj = adb.Element<Entity>(objId);
                    if (dbObj is BlockReference b)
                    {
                        handleBlockReference(b, br.BlockTransform.PreMultiplyBy(matrix), _fs);
                    }
                    else
                    {
                        handleEntity(dbObj, br.BlockTransform.PreMultiplyBy(matrix), _fs);
                    }
                }
                {
                    var lst = new List<KeyValuePair<Geometry, Action>>();
                    var info = br.XClipInfo();
                    if (info.IsValid)
                    {
                        info.TransformBy(br.BlockTransform.PreMultiplyBy(matrix));
                        var gf = info.PreparedPolygon;
                        foreach (var kv in _fs)
                        {
                            if (gf.Intersects(kv.Key))
                            {
                                lst.Add(kv);
                            }
                        }
                    }
                    else
                    {
                        foreach (var kv in _fs)
                        {
                            lst.Add(kv);
                        }
                    }
                    fs.AddRange(lst);
                }
            }
        }
        public static IEnumerable<GLineSegment> GetGLineSegments(Entity entity)
        {
            if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
            {
                yield return line.ToGLineSegment();
                yield break;
            }
            if (entity is Polyline pl)
            {
                foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>().Where(x => x.Length > THESAURUSSTAMPEDE))
                {
                    yield return ln.ToGLineSegment();
                }
                yield break;
            }
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            if (dxfName is DISORGANIZATION)
            {
                foreach (Entity e in entity.ExplodeToDBObjectCollection())
                {
                    foreach (var seg in GetGLineSegments(e))
                    {
                        yield return seg;
                    }
                }
            }
        }
        public const int THESAURUSHOUSING = 1;
        public const int THESAURUSPERMUTATION = 2;
        public const int THESAURUSSTAMPEDE = 0;
        public const int THESAURUSACRIMONIOUS = 10;
        public const bool INTRAVASCULARLY = false;
        public const int THESAURUSDISINGENUOUS = 36;
        public const bool THESAURUSOBSTINACY = true;
        public const int DISPENSABLENESS = 40;
        public const int SUPERLATIVENESS = 6;
        public const int THESAURUSREPERCUSSION = 4096;
        public const string DISORGANIZATION = "TCH_PIPE";
        public const int HYPERDISYLLABLE = 100;
        public const string THESAURUSWINDFALL = "TCH_VPIPEDIM";
        public const string THESAURUSSPECIFICATION = "-";
        public const string THESAURUSDURESS = "TCH_TEXT";
        public const string THESAURUSFACILITATE = "TCH_MTEXT";
        public const string THESAURUSINHARMONIOUS = "TCH_MULTILEADER";
        public const string THESAURUSDEPLORE = "";
        public const int THESAURUSENTREPRENEUR = 50;
        public const char THESAURUSCONTEND = '|';
        public const string MULTIPROCESSING = "|";
        public const char SUPERREGENERATIVE = '$';
        public const string THESAURUSCOURIER = "$";
        public const int THESAURUSHYPNOTIC = 300;
        public const int QUOTATIONEDIBLE = 4;
        public const int INTROPUNITIVENESS = 3;
        public const int DOCTRINARIANISM = 1200;
        public const int THESAURUSDIFFICULTY = 360;
        public const int THESAURUSNOTORIETY = 3000;
        public const double THESAURUSDISPASSIONATE = .7;
        public const string CONTROVERSIALLY = "TH-STYLE3";
        public const string THESAURUSBLEMISH = "W-FRPT-NOTE";
        public const string BACTERIOLOGICAL = "\n请指定环管标记起点: ";
        public const string THESAURUSINAPPOSITE = "\n请指定一个起点: ";
        public const string QUOTATIONPERISTALTIC = "([a-zA-Z])'?";
        public const string SEMITRANSPARENT = "1:150";
        public const double THESAURUSSOMBRE = 525.0;
        public const double THESAURUSCOMPULSIVE = 350.0;
        public const string DISINTEGRATIVELY = "消火栓环管节点标记";
        public const string PHENYLHYDRAZONE = "\n请使用最近的消火栓环管节点图块\n";
        public const string TELECOMMUNICATIONS = "没找到主环";
        public const int THESAURUSPARAGRAPH = 5000;
        public const double PROCRASTINATION = 30.0;
        public const string INTELLECTUALNESS = "\n";
        public const int THESAURUSTOLERATION = 16;
        public const string THESAURUSCONDOM = "ToNTSGeometry";
        public const string ADMINISTRATIONS = "hatch";
        public const string QUOTATIONCHROMIC = "TCH";
        public const string QUOTATIONPURKINJE = "DIMENSION";
        public const double THESAURUSTROUPE = 1e6;
        public const double THESAURUSBACKER = 180.0;
        public const string CHESTERFIELDIAN = "W-FRPT-HYDT-DIMS";
        public const string THESAURUSAFFECTATION = "X";
        public const string THESAURUSREALLY = "L";
        public const string QUOTATION1BFIRST = "标注线图层";
        public static void Generate(FireHydrantSystemUIViewModel vm)
        {
            int @case = THESAURUSSTAMPEDE;
            if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Whole && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Whole)
            {
                @case = THESAURUSHOUSING;
            }
            else if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Whole && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Single)
            {
                @case = THESAURUSPERMUTATION;
            }
            else if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Single && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Whole)
            {
                @case = INTROPUNITIVENESS;
            }
            else if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Single && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Single)
            {
                @case = QUOTATIONEDIBLE;
            }
            else
            {
                throw new NotSupportedException();
            }
            Point3d selPt = default;
            Polygon selectArea = default;
            List<Polygon> extraAreas = default;
            FireHydrantSystem GetCtx()
            {
                FocusMainWindow();
                var _vm = new FireHydrantSystemViewModel();
                Point3d loopStartPt;
                {
                    var opt = new PromptPointOptions(BACTERIOLOGICAL);
                    var propPtRes = Active.Editor.GetPoint(opt);
                    if (propPtRes.Status != PromptStatus.OK) return null;
                    loopStartPt = propPtRes.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                }
                var _selectArea = TrySelectRangeEx();
                if (_selectArea == null) return null;
                selectArea = _selectArea.ToGRect().ToPolygon();
                if (@case is THESAURUSPERMUTATION or QUOTATIONEDIBLE)
                {
                    var opt = new PromptPointOptions(THESAURUSINAPPOSITE);
                    var propPtRes = Active.Editor.GetPoint(opt);
                    if (propPtRes.Status != PromptStatus.OK) return null;
                    selPt = propPtRes.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                }
                var fireHydrantSysIn = new FireHydrantSystemIn(_vm.SetViewModel.FloorLineSpace);
                var fireHydrantSysOut = new FireHydrantSystemOut();
                var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();
                var valveDic = new Dictionary<Point3dEx, List<Point3dEx>>();
                var subPathList = new List<List<Point3dEx>>();
                var mainPathList = new List<List<Point3dEx>>();
                var ctx = new FireHydrantSystem()
                {
                    BranchDic = branchDic,
                    ValveDic = valveDic,
                    FireHydrantSystemIn = fireHydrantSysIn,
                    FireHydrantSystemOut = fireHydrantSysOut,
                };
                using (DocLock)
                using (var adb = AcadDatabase.Active())
                {
                    var ok = GetInput.GetFireHydrantSysInput(adb, ref fireHydrantSysIn, _selectArea, loopStartPt);
                    if (!ok) return null;
                    var _mainPathList = MainLoop.Get(ref fireHydrantSysIn);
                    if (_mainPathList.Count == THESAURUSSTAMPEDE)
                    {
                        throw new Exception(TELECOMMUNICATIONS);
                    }
                    var _subPathList = SubLoop.Get(ref fireHydrantSysIn, _mainPathList);
                    var visited = new HashSet<Point3dEx>();
                    visited.AddVisit(_mainPathList);
                    visited.AddVisit(_subPathList);
                    PtDic.CreateBranchDic(ref branchDic, ref valveDic, _mainPathList, fireHydrantSysIn, visited);
                    PtDic.CreateBranchDic(ref branchDic, ref valveDic, _subPathList, fireHydrantSysIn, visited);
                    GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, _mainPathList[THESAURUSSTAMPEDE], fireHydrantSysIn, branchDic);
                    GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, _subPathList, fireHydrantSysIn, branchDic);
                    GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, valveDic, fireHydrantSysIn);
                    mainPathList = _mainPathList;
                    subPathList = _subPathList;
                }
                ctx.MainPathList = mainPathList;
                ctx.SubPathList = subPathList;
                return ctx;
            }
            var TEXTHEIGHT = vm.CurrentDwgRatio is SEMITRANSPARENT ? THESAURUSSOMBRE : THESAURUSCOMPULSIVE;
            MLeader DrawMLeader(string content, Point3d p1, Point3d p2)
            {
                var e = new MLeader();
                var mt = new MText() { Contents = content, TextHeight = TEXTHEIGHT, ColorIndex = DISPENSABLENESS, };
                ByLayer(mt);
                e.MText = mt;
                e.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
                e.ArrowSize = THESAURUSSTAMPEDE;
                e.DoglegLength = THESAURUSENTREPRENEUR;
                e.LandingGap = THESAURUSSTAMPEDE;
                e.ExtendLeaderToText = INTRAVASCULARLY;
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
                e.AddLeaderLine(p1);
                var bd = e.MText.Bounds.ToGRect();
                var p3 = p2.OffsetY(bd.Height + HYPERDISYLLABLE);
                if (p2.X < p1.X)
                {
                    p3 = p3.OffsetX(-bd.Width);
                }
                e.TextLocation = p3;
                e.Layer = LeaderLayer;
                DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
                return e;
            }
            var ctx = GetCtx();
            if (ctx == null) return;
{ ThRainSystemService.ImportElementsFromStdDwg(); }
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            {
                var tokills = new HashSet<Entity>();
                foreach (var entity in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer is THESAURUSBLEMISH or CHESTERFIELDIAN))
                {
                    var dxfName = entity.GetRXClass().DxfName.ToUpper();
                    if (dxfName is THESAURUSWINDFALL)
                    {
                        dynamic o = entity.AcadObject;
                        string DimStyleText = o.DimStyleText;
                        string VPipeNum = o.VPipeNum;
                        var str = (DimStyleText + THESAURUSSPECIFICATION + VPipeNum).ToUpper();
                        if (str.Contains(THESAURUSAFFECTATION) && str.Contains(THESAURUSREALLY))
                        {
                            tokills.Add(entity);
                            continue;
                        }
                    }
                    if (dxfName is THESAURUSINHARMONIOUS)
                    {
                        dynamic o = entity.AcadObject;
                        string UpText = o.UpText;
                        string DownText = o.DownText;
                        var str = THESAURUSDEPLORE + UpText + DownText;
                        if (str.Contains(THESAURUSAFFECTATION) && str.Contains(THESAURUSREALLY))
                        {
                            tokills.Add(entity);
                            continue;
                        }
                    }
                }
                tokills.AddRange(adb.ModelSpace.OfType<Entity>().Where(x => x.Layer is QUOTATION1BFIRST or LeaderLayer));
                foreach (var e in tokills)
                {
                    e.Erase(adb);
                }
            }
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                string GetEffectiveName(BlockReference br) => br.BlockTableRecord.IsNull ? string.Empty : (br.IsDynamicBlock || br.DynamicBlockTableRecord.IsValid ? adb.Element<BlockTableRecord>(br.DynamicBlockTableRecord).Name : br.Name);
                if (adb.ModelSpace.OfType<BlockReference>().Where(br => GetEffectiveName(br) is DISINTEGRATIVELY).Any())
                {
                    Active.Editor.WriteMessage(PHENYLHYDRAZONE);
                }
                LayerThreeAxes(new string[] { THESAURUSBLEMISH });
                var mlInfos = new List<MLeaderInfo>(THESAURUSREPERCUSSION);
                mlInfos.AddRange(ctx.FireHydrantSystemIn.TermPointDic.Values.Where(x => x.Type is THESAURUSHOUSING).Select(x => x.PtEx).Distinct().Select(x => x._pt).Select(pt => MLeaderInfo.Create(pt, THESAURUSDEPLORE)));
                var pts = mlInfos.Select(x => x.BasePoint.ToNTSPoint(x)).Distinct().ToList();
                var ptsf = GeoFac.CreateIntersectsSelector(pts);
                switch (@case)
                {
                    case THESAURUSHOUSING:
                        {
                            var mainVisited = new List<Point3dEx>();
                            var k = vm.StartNum - THESAURUSHOUSING;
                            foreach (var mpt in ctx.MainPathList)
                            {
                                foreach (var ppt in mpt)
                                {
                                    mainVisited.Add(ppt);
                                    foreach (var kv in ctx.BranchDic)
                                    {
                                        if (kv.Key.Equals(ppt))
                                        {
                                            foreach (var v in kv.Value)
                                            {
                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                {
                                                    var info = (MLeaderInfo)pt.UserData;
                                                    if (info.Text == THESAURUSDEPLORE)
                                                    {
                                                        info.Text = vm.Prefix + ++k;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            foreach (var _mpt in ctx.SubPathList)
                            {
                                var mpt = _mpt.ToList();
                                {
                                    if (mpt.Count > THESAURUSHOUSING)
                                    {
                                        var first = mpt.First();
                                        var last = mpt.Last();
                                        var i = mainVisited.IndexOf(first);
                                        var j = mainVisited.IndexOf(last);
                                        var shouldReverse = INTRAVASCULARLY;
                                        if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                        {
                                            if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                        }
                                        else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                        {
                                            shouldReverse = THESAURUSOBSTINACY;
                                        }
                                        if (shouldReverse)
                                        {
                                            mpt.Reverse();
                                        }
                                    }
                                }
                                foreach (var ppt in mpt)
                                {
                                    foreach (var kv in ctx.BranchDic)
                                    {
                                        if (kv.Key.Equals(ppt))
                                        {
                                            foreach (var v in kv.Value)
                                            {
                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                {
                                                    var info = (MLeaderInfo)pt.UserData;
                                                    if (info.Text == THESAURUSDEPLORE)
                                                    {
                                                        info.Text = vm.Prefix + ++k;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case THESAURUSPERMUTATION:
                        {
                            void main()
                            {
                                var mainVisited = new List<Point3dEx>();
                                var k = vm.StartNum - THESAURUSHOUSING;
                                foreach (var _mpt in ctx.MainPathList)
                                {
                                    var mpt = _mpt.ToList();
                                    if (mpt.Count > THESAURUSSTAMPEDE)
                                    {
                                        var last = mpt.Last();
                                        if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                        {
                                            mpt.Reverse();
                                        }
                                    }
                                    mainVisited.AddRange(mpt);
                                    if (mpt.Count > THESAURUSSTAMPEDE)
                                    {
                                        if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                        {
                                            foreach (var ppt in mpt)
                                            {
                                                foreach (var kv in ctx.BranchDic)
                                                {
                                                    if (kv.Key.Equals(ppt))
                                                    {
                                                        foreach (var v in kv.Value)
                                                        {
                                                            foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                            {
                                                                var info = (MLeaderInfo)pt.UserData;
                                                                if (info.Text == THESAURUSDEPLORE)
                                                                {
                                                                    info.Text = vm.Prefix + ++k;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            return;
                                        }
                                    }
                                }
                                foreach (var _mpt in ctx.SubPathList)
                                {
                                    var mpt = _mpt.ToList();
                                    {
                                        if (mpt.Count > THESAURUSHOUSING)
                                        {
                                            var first = mpt.First();
                                            var last = mpt.Last();
                                            var i = mainVisited.IndexOf(first);
                                            var j = mainVisited.IndexOf(last);
                                            var shouldReverse = INTRAVASCULARLY;
                                            if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            if (first._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                shouldReverse = INTRAVASCULARLY;
                                            }
                                            else if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            if (shouldReverse)
                                            {
                                                mpt.Reverse();
                                            }
                                        }
                                    }
                                    if (mpt.Count > THESAURUSSTAMPEDE)
                                    {
                                        if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                        {
                                            foreach (var ppt in mpt)
                                            {
                                                foreach (var kv in ctx.BranchDic)
                                                {
                                                    if (kv.Key.Equals(ppt))
                                                    {
                                                        foreach (var v in kv.Value)
                                                        {
                                                            foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                            {
                                                                var info = (MLeaderInfo)pt.UserData;
                                                                if (info.Text == THESAURUSDEPLORE)
                                                                {
                                                                    info.Text = vm.Prefix + ++k;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                            main();
                        }
                        break;
                    case INTROPUNITIVENESS:
                        {
                            var mainVisited = new List<Point3dEx>();
                            {
                                var k = vm.StartNum - THESAURUSHOUSING;
                                foreach (var mpt in ctx.MainPathList)
                                {
                                    foreach (var ppt in mpt)
                                    {
                                        mainVisited.Add(ppt);
                                        foreach (var kv in ctx.BranchDic)
                                        {
                                            if (kv.Key.Equals(ppt))
                                            {
                                                foreach (var v in kv.Value)
                                                {
                                                    foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                    {
                                                        var info = (MLeaderInfo)pt.UserData;
                                                        if (info.Text == THESAURUSDEPLORE)
                                                        {
                                                            info.Text = vm.Prefix + ++k;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            {
                                var prefix = THESAURUSDEPLORE;
                                foreach (var _mpt in ctx.SubPathList)
                                {
                                    var mpt = _mpt.ToList();
                                    {
                                        if (mpt.Count > THESAURUSHOUSING)
                                        {
                                            var first = mpt.First();
                                            var last = mpt.Last();
                                            var i = mainVisited.IndexOf(first);
                                            var j = mainVisited.IndexOf(last);
                                            var shouldReverse = INTRAVASCULARLY;
                                            if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            if (shouldReverse)
                                            {
                                                mpt.Reverse();
                                            }
                                        }
                                    }
                                    var k = THESAURUSSTAMPEDE;
                                    if (mpt.Count > THESAURUSHOUSING)
                                    {
                                        var first = mpt.First();
                                        var last = mpt.Last();
                                        foreach (var ppt in new Point3dEx[] { first, last })
                                        {
                                            ctx.FireHydrantSystemIn.MarkList.TryGetValue(ppt, out string v);
                                            if (v != null)
                                            {
                                                var m = Regex.Match(v, QUOTATIONPERISTALTIC);
                                                if (m.Success)
                                                {
                                                    prefix = m.Groups[THESAURUSHOUSING].Value.ToLower();
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    foreach (var ppt in mpt)
                                    {
                                        foreach (var kv in ctx.BranchDic)
                                        {
                                            if (kv.Key.Equals(ppt))
                                            {
                                                foreach (var v in kv.Value)
                                                {
                                                    foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                    {
                                                        var info = (MLeaderInfo)pt.UserData;
                                                        if (info.Text == THESAURUSDEPLORE)
                                                        {
                                                            info.Text = vm.Prefix + prefix + ++k;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case QUOTATIONEDIBLE:
                        {
                            void main()
                            {
                                var mainVisited = new List<Point3dEx>();
                                {
                                    var k = vm.StartNum - THESAURUSHOUSING;
                                    foreach (var _mpt in ctx.MainPathList)
                                    {
                                        var mpt = _mpt.ToList();
                                        if (mpt.Count > THESAURUSSTAMPEDE)
                                        {
                                            var last = mpt.Last();
                                            if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                mpt.Reverse();
                                            }
                                        }
                                        mainVisited.AddRange(mpt);
                                        if (mpt.Count > THESAURUSSTAMPEDE)
                                        {
                                            if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                foreach (var ppt in mpt)
                                                {
                                                    foreach (var kv in ctx.BranchDic)
                                                    {
                                                        if (kv.Key.Equals(ppt))
                                                        {
                                                            foreach (var v in kv.Value)
                                                            {
                                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                                {
                                                                    var info = (MLeaderInfo)pt.UserData;
                                                                    if (info.Text == THESAURUSDEPLORE)
                                                                    {
                                                                        info.Text = vm.Prefix + ++k;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                                {
                                    var prefix = THESAURUSDEPLORE;
                                    foreach (var _mpt in ctx.SubPathList)
                                    {
                                        var mpt = _mpt.ToList();
                                        {
                                            if (mpt.Count > THESAURUSHOUSING)
                                            {
                                                var first = mpt.First();
                                                var last = mpt.Last();
                                                var i = mainVisited.IndexOf(first);
                                                var j = mainVisited.IndexOf(last);
                                                var shouldReverse = INTRAVASCULARLY;
                                                if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                                {
                                                    if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                                }
                                                else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                                {
                                                    shouldReverse = THESAURUSOBSTINACY;
                                                }
                                                if (first._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                                {
                                                    shouldReverse = INTRAVASCULARLY;
                                                }
                                                else if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                                {
                                                    shouldReverse = THESAURUSOBSTINACY;
                                                }
                                                if (shouldReverse)
                                                {
                                                    mpt.Reverse();
                                                }
                                            }
                                        }
                                        if (mpt.Count > THESAURUSHOUSING)
                                        {
                                            var first = mpt.First();
                                            var last = mpt.Last();
                                            foreach (var ppt in new Point3dEx[] { first, last })
                                            {
                                                ctx.FireHydrantSystemIn.MarkList.TryGetValue(ppt, out string v);
                                                if (v != null)
                                                {
                                                    var m = Regex.Match(v, QUOTATIONPERISTALTIC);
                                                    if (m.Success)
                                                    {
                                                        prefix = m.Groups[THESAURUSHOUSING].Value.ToLower();
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (mpt.Count > THESAURUSSTAMPEDE)
                                        {
                                            if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                var k = vm.StartNum - THESAURUSHOUSING;
                                                foreach (var ppt in mpt)
                                                {
                                                    foreach (var kv in ctx.BranchDic)
                                                    {
                                                        if (kv.Key.Equals(ppt))
                                                        {
                                                            foreach (var v in kv.Value)
                                                            {
                                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                                {
                                                                    var info = (MLeaderInfo)pt.UserData;
                                                                    if (info.Text == THESAURUSDEPLORE)
                                                                    {
                                                                        info.Text = vm.Prefix + prefix + ++k;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            main();
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }
                var stepAngle = THESAURUSACRIMONIOUS;
                var stepRadius = HYPERDISYLLABLE;
                var minRadius = DOCTRINARIANISM;
                var maxRadius = THESAURUSPARAGRAPH;
                var toDraw = new List<Point>();
                foreach (var info in mlInfos)
                {
                    if (!string.IsNullOrWhiteSpace(info.Text))
                    {
                        toDraw.Add(info.BasePoint.ToNTSPoint().Tag(info.Text));
                    }
                }
                var toDrawf = GeoFac.CreateIntersectsSelector(toDraw);
                {
                    var lst = new List<Geometry>(THESAURUSREPERCUSSION);
                    var lst2 = new List<Geometry>(THESAURUSREPERCUSSION);
                    foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer is LeaderLayer).ToList())
                    {
                        Geometry geo = null;
                        if (ent is Line ln && ln.Length > THESAURUSSTAMPEDE)
                        {
                            geo = ln.ToLineString();
                        }
                        else if (ent is Polyline pl)
                        {
                            geo = pl.ToLineString();
                        }
                        else if (ent is DBText dbt)
                        {
                            geo = dbt.Bounds.ToGRect().ToPolygon();
                        }
                        if (geo != null)
                        {
                            if (toDraw.Select(x => x.ToPoint2d().ToGRect(maxRadius).ToPolygon()).Any(x => x.Contains(geo)))
                            {
                                ent.Erase(adb);
                            }
                        }
                    }
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        var dxfName = ent.GetRXClass().DxfName.ToUpper();
                        if (!IsLayerVisible(ent)) continue;
                        if (ent is Line ln && ln.Length > THESAURUSSTAMPEDE)
                        {
                            lst.Add(ln.ToLineString().Buffer(THESAURUSACRIMONIOUS).Tag(() => { ent.Erase(adb); }));
                        }
                        else if (ent is Polyline pl)
                        {
                            lst.Add(pl.ToLineString().Buffer(THESAURUSACRIMONIOUS).Tag(() => { ent.Erase(adb); }));
                        }
                        else if (ent is DBText dbt)
                        {
                            lst.Add(dbt.Bounds.ToGRect().ToPolygon().Buffer(THESAURUSACRIMONIOUS).Tag(() => { ent.Erase(adb); }));
                        }
                        else if (dxfName is THESAURUSDURESS or THESAURUSFACILITATE or THESAURUSINHARMONIOUS)
                        {
                            lst.Add(ent.Bounds.ToGRect().ToPolygon().Buffer(THESAURUSACRIMONIOUS).Tag(() => { ent.Erase(adb); }));
                        }
                    }
                    lst = GeoFac.CreateIntersectsSelector(lst)(GeoFac.CreateGeometryEx(toDraw.Select(x => x.ToPoint2d().ToGRect(maxRadius).ToPolygon()).ToList()));
                    if (selectArea != null)
                    {
                        lst = GeoFac.CreateIntersectsSelector(lst)(selectArea);
                    }
                    var lstf = GeoFac.CreateCoveredBySelector(lst);
                    foreach (var pt in ctx.FireHydrantSystemIn.TermPointDic.Values)
                    {
                        if (pt.StartLine is null) continue;
                        var seg = pt.StartLine.ToGLineSegment();
                        foreach (var geo in lstf(seg.ToLineString()))
                        {
                            (geo.UserData as Action)?.Invoke();
                            geo.UserData = null;
                        }
                    }
                    foreach (var pt in ctx.FireHydrantSystemIn.TermPointDic.Values)
                    {
                        if (pt.TextLine is null) continue;
                        var seg = pt.TextLine.ToGLineSegment();
                        foreach (var geo in lstf(seg.ToLineString()))
                        {
                            (geo.UserData as Action)?.Invoke();
                            geo.UserData = null;
                        }
                        foreach (var geo in lstf(seg.Center.OffsetY(THESAURUSHYPNOTIC).ToNTSPoint()))
                        {
                            (geo.UserData as Action)?.Invoke();
                            geo.UserData = null;
                        }
                    }
                }
                var data = CollectData(adb, selectArea.ToGRect());
                {
                    var t0 = DateTime.Now;
                    var targets = ctx.FireHydrantSystemIn.TermPointDic.Values.Where(x => x.Type is THESAURUSHOUSING).Select(x => x.PtEx._pt).ToHashSet();
                    var range = data.Range.ToPolygon().ToIPreparedGeometry();
                    range = new MultiPoint(targets.Select(x => x.ToNTSPoint()).Where(x => range.Contains(x)).ToArray()).ToGRect().Expand(maxRadius + THESAURUSNOTORIETY).ToPolygon().ToIPreparedGeometry();
                    var rdls = GeoFac.GetManyLineStrings(data.redlines.Select(x => x.ToLineString()).Where(x => range.Intersects(x))).ToList();
                    var textpls = data.dtexts.SelectNotNull(GetObbPolygon).ToHashSet();
                    var obstcs = data.segs.Except(data.redlines).Select(x => x.ToLineString()).OfType<Geometry>().Concat(textpls).Where(x => range.Intersects(x)).ToHashSet();
                    {
                        var tmp = GeoFac.GetNodedLineStrings(rdls).ToList();
                        var kills = targets.Select(x => x.ToPoint2d().ToGCircle(HYPERDISYLLABLE - THESAURUSACRIMONIOUS).ToCirclePolygon(SUPERLATIVENESS, INTRAVASCULARLY)).ToList();
                        var breaks = GeoFac.GetNodedLineStrings(targets.Select(x => x.ToPoint2d().ToGCircle(HYPERDISYLLABLE).ToCirclePolygon(THESAURUSDISINGENUOUS, INTRAVASCULARLY))).ToList();
                        tmp = GeoFac.GetNodedLineStrings(tmp.Concat(breaks)).ToList();
                        rdls = tmp.Except(breaks).Except(GeoFac.CreateIntersectsSelector(tmp)(GeoFac.CreateGeometryEx(kills))).ToList();
                    }
                    {
                        rdls.AddRange(GeoFac.GetManyLines(rdls.ToList(), INTRAVASCULARLY).SelectMany(x => GeoFac.GetLines(x.Buffer(THESAURUSENTREPRENEUR), INTRAVASCULARLY)).Select(x => x.ToLineString()));
                    }
                    var hatches = data.hatches.Where(x => range.Intersects(x)).ToHashSet();
                    var t1 = GeoFac.CreateIntersectsTester(rdls.Concat(obstcs).ToList());
                    var t2 = GeoFac.CreateIntersectsTester(rdls);
                    var t3 = GeoFac.CreateIntersectsTester(rdls.OfType<Geometry>().Concat(textpls).ToList());
                    var (t4, addsankaku) = GeoFac.CreateIntersectsTesterEngine<Geometry>();
                    foreach (var tg in targets)
                    {
                        var text = toDrawf(tg.ToGRect(HYPERDISYLLABLE).ToPolygon()).FirstOrDefault()?.UserData as string;
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        var (w, h) = GetDBTextSize(text, TEXTHEIGHT, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
                        void test()
                        {
                            for (int state = THESAURUSSTAMPEDE; state < INTROPUNITIVENESS; state++)
                            {
                                for (double radius = minRadius; radius < maxRadius; radius += stepRadius)
                                {
                                    var n = THESAURUSDIFFICULTY / stepAngle;
                                    var step = Math.PI * THESAURUSPERMUTATION / n;
                                    for (int k = THESAURUSSTAMPEDE; k < n; k++)
                                    {
                                        const double text_gap = THESAURUSENTREPRENEUR;
                                        const double bd_gap = THESAURUSENTREPRENEUR;
                                        var angle = step * k + PROCRASTINATION.AngleFromDegree();
                                        var pt1 = tg.OffsetXY(radius * Math.Cos(angle), radius * Math.Sin(angle));
                                        var pt2 = pt1.OffsetXY(w + THESAURUSPERMUTATION * text_gap, h + THESAURUSPERMUTATION * text_gap);
                                        var labelline1 = new GLineSegment(pt1, tg);
                                        if (state < THESAURUSPERMUTATION)
                                        {
                                            if (t2(labelline1.ToLineString())) continue;
                                        }
                                        var r = new GRect(pt1, pt2);
                                        if ((pt1 - tg).X < THESAURUSSTAMPEDE)
                                        {
                                            r = r.OffsetXY(-(w + THESAURUSPERMUTATION * text_gap), THESAURUSSTAMPEDE);
                                        }
                                        var rpl = r.Expand(bd_gap).ToPolygon();
                                        if (state < THESAURUSPERMUTATION)
                                        {
                                            if (t4(rpl)) continue;
                                        }
                                        if (state == THESAURUSSTAMPEDE)
                                        {
                                            if (t1(rpl)) continue;
                                        }
                                        else if (state == THESAURUSHOUSING)
                                        {
                                            if (t3(rpl)) continue;
                                        }
                                        var _r = r.Expand(-text_gap);
                                        var t = DrawTextLazy(text, TEXTHEIGHT, _r.LeftButtom);
                                        t.Layer = LeaderLayer;
                                        t.WidthFactor = THESAURUSDISPASSIONATE;
                                        DrawingQueue.Enqueue(adb =>
                                        {
                                            t.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
                                        });
                                        DrawLineSegmentLazy(labelline1).Layer = LeaderLayer;
                                        DrawLineSegmentLazy(new GLineSegment(r.LeftButtom, r.RightButtom)).Layer = LeaderLayer;
                                        addsankaku(rpl);
                                        return;
                                    }
                                }
                            }
                        }
                        test();
                    }
                    Active.Editor.Write(INTELLECTUALNESS + (DateTime.Now - t0).ToString() + INTELLECTUALNESS);
                }
            }
        }
    }
    public class MLeaderInfo
    {
        public Point3d BasePoint;
        public string Text;
        public string Note;
        public static MLeaderInfo Create(Point2d pt, string text) => Create(pt.ToPoint3d(), text);
        public static MLeaderInfo Create(Point3d pt, string text)
        {
            return new MLeaderInfo() { BasePoint = pt, Text = text };
        }
    }
    public class HydrantData
    {
        public HashSet<GLineSegment> segs;
        public HashSet<GCircle> circles;
        public List<DBTextInfo> dtexts;
        public HashSet<GLineSegment> redlines;
        public HashSet<Geometry> hatches;
        public HashSet<Point3d> targets;
        public GRect Range;
    }
    public class DBTextInfo
    {
        public string LayerName;
        public string TextStyle;
        public Point3d BasePoint;
        public string Text;
        public double Height;
        public double Rotation;
        public double WidthFactor;
        public DBTextInfo() { }
        public DBTextInfo(Point3d point, string text, string layerName, string textStyle)
        {
            this.LayerName = layerName;
            this.TextStyle = textStyle;
            this.BasePoint = point;
            this.Text = text;
        }
    }
}