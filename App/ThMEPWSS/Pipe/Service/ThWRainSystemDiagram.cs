namespace ThMEPWSS.FlatDiagramNs
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
    public class MLeaderInfo
    {
        public Point3d BasePoint;
        public string Text;
        public static MLeaderInfo Create(Point2d pt, string text) => Create(pt.ToPoint3d(), text);
        public static MLeaderInfo Create(Point3d pt, string text)
        {
            return new MLeaderInfo() { BasePoint = pt, Text = text };
        }
    }
    public static class FlatDiagramService
    {
        public static void DrawRainFlatDiagram(RainSystemDiagramViewModel vm)
        {
            var range = CadCache.TryGetRange();
            if (range == null)
            {
                Active.Editor.WriteMessage(THESAURUSPOWERLESS);
                return;
            }
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
            {
                if (adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer).Any())
                {
                    var r = MessageBox.Show(QUOTATIONISOPHANE, THESAURUSMISADVENTURE, MessageBoxButtons.YesNo);
                    if (r == DialogResult.No) return;
                }
                if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
                var mlPts = new List<Point>(THESAURUSREPERCUSSION);
                foreach (var e in adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer))
                {
                    var pt = e.GetFirstVertex(THESAURUSSTAMPEDE).ToNTSPoint();
                    pt.UserData = e;
                    mlPts.Add(pt);
                }
                ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.CollectRainGeoData(range, adb, out List<StoreyInfo> storeysItems, out ThMEPWSS.ReleaseNs.RainSystemNs.RainGeoData geoData);
                var (drDatas, exInfo) = ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.CreateRainDrawingData(adb, geoData, INTRAVASCULARLY);
                exInfo.drDatas = drDatas;
                exInfo.geoData = geoData;
                exInfo.storeysItems = storeysItems;
                exInfo.vm = vm;
                Dispose();
                var f = GeoFac.CreateIntersectsSelector(geoData.WLines.Select(x => x.Buffer(THESAURUSCOMMUNICATION)).ToList());
                var pts = mlPts.Where(pt => f(pt).Any()).ToList();
                foreach (var pt in pts)
                {
                    adb.Element<Entity>(((Entity)pt.UserData).ObjectId, THESAURUSOBSTINACY).Erase();
                }
                DrawFlatDiagram(exInfo);
                FlushDQ();
            }
        }
        public static void DrawDrainageFlatDiagram(DrainageSystemDiagramViewModel vm)
        {
            var range = CadCache.TryGetRange();
            if (range == null)
            {
                Active.Editor.WriteMessage(THESAURUSPOWERLESS);
                return;
            }
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
            {
                if (adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer).Any())
                {
                    var r = MessageBox.Show(QUOTATIONISOPHANE, THESAURUSMISADVENTURE, MessageBoxButtons.YesNo);
                    if (r == DialogResult.No) return;
                }
                if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
                var mlPts = new List<Point>(THESAURUSREPERCUSSION);
                foreach (var e in adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer))
                {
                    var pt = e.GetFirstVertex(THESAURUSSTAMPEDE).ToNTSPoint();
                    pt.UserData = e;
                    mlPts.Add(pt);
                }
                ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CollectDrainageGeoData(range, adb, out List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> storeysItems, out ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData);
                var (drDatas, exInfo) = ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CreateDrainageDrawingData(geoData, INTRAVASCULARLY);
                exInfo.drDatas = drDatas;
                exInfo.geoData = geoData;
                exInfo.storeysItems = storeysItems;
                exInfo.vm = vm;
                Dispose();
                var f = GeoFac.CreateIntersectsSelector(geoData.DLines.Select(x => x.Buffer(THESAURUSCOMMUNICATION)).ToList());
                var pts = mlPts.Where(pt => f(pt).Any()).ToList();
                foreach (var pt in pts)
                {
                    adb.Element<Entity>(((Entity)pt.UserData).ObjectId, THESAURUSOBSTINACY).Erase();
                }
                DrawFlatDiagram(exInfo);
                FlushDQ();
            }
        }
        public static void DrawFlatDiagram(ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo);
        }
        public static void DrawFlatDiagram(ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo);
        }
        public static void DrawBackToFlatDiagram(ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo.storeysItems, exInfo.geoData, exInfo.drDatas, exInfo, exInfo.vm);
        }
        static List<GLineSegment> Substract(List<GLineSegment> segs, IEnumerable<Geometry> geos)
        {
            static Func<Geometry, List<T>> CreateIntersectsSelector<T>(ICollection<T> geos) where T : Geometry
            {
                if (geos.Count == THESAURUSSTAMPEDE) return r => new List<T>();
                var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > THESAURUSACRIMONIOUS ? geos.Count : THESAURUSACRIMONIOUS);
                foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
                return geo =>
                {
                    if (geo == null) throw new ArgumentNullException();
                    var gf = GeoFac.PreparedGeometryFactory.Create(geo);
                    return engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
                };
            }
            var lines = new HashSet<LineString>(segs.Select(x => x.ToLineString()));
            foreach (var geo in geos)
            {
                var f = CreateIntersectsSelector(lines);
                var _lines = f(geo);
                if (_lines.Count > THESAURUSSTAMPEDE)
                {
                    foreach (var line in _lines)
                    {
                        lines.Remove(line);
                    }
                    lines.AddRange(_lines.SelectMany(line => GeoFac.GetLines(line.Difference(geo)).Select(x => x.ToLineString())));
                }
            }
            return lines.SelectMany(x => GeoFac.GetLines(x)).ToList();
        }
        static GLineSegment GetCenterLine(List<GLineSegment> segs)
        {
            if (segs.Count == THESAURUSSTAMPEDE) throw new ArgumentException();
            if (segs.Count == THESAURUSHOUSING) return segs[THESAURUSSTAMPEDE];
            var angle = segs.Select(seg => seg.SingleAngle).Average();
            var r = Extents2dCalculator.Calc(segs.YieldPoints()).ToGRect();
            var m = Matrix2d.Displacement(-r.Center.ToVector2d()).PreMultiplyBy(Matrix2d.Rotation(-angle, default));
            r = Extents2dCalculator.Calc(segs.Select(seg => seg.TransformBy(m))).ToGRect();
            return new GLineSegment(GetMidPoint(r.LeftButtom, r.LeftTop), GetMidPoint(r.RightButtom, r.RightTop)).TransformBy(m.Inverse());
        }
        public static bool IsToilet(string roomName)
        {
            var roomNameContains = new List<string>
            {
                CIRCUMSTANTIARE,THESAURUSCIPHER,PREMILLENNIALIST,
                THESAURUSINDIGENT,THESAURUSFRAMEWORK,THESAURUSHUMILIATION,
            };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            if (roomName.Equals(THESAURUSSTERLING))
                return THESAURUSOBSTINACY;
            return Regex.IsMatch(roomName, THESAURUSSCREEN);
        }
        public static bool IsKitchen(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSPEDESTRIAN, THESAURUSRECLUSE };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            if (roomName.Equals(THESAURUSINCONTROVERTIBLE))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static bool IsBalcony(string roomName)
        {
            if (roomName == null) return INTRAVASCULARLY;
            var roomNameContains = new List<string> { NATIONALIZATION };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static bool IsCorridor(string roomName)
        {
            if (roomName == null) return INTRAVASCULARLY;
            var roomNameContains = new List<string> { THESAURUSSPECIES };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static void DrawBackToFlatDiagram(List<StoreyInfo> storeysItems, ThMEPWSS.ReleaseNs.RainSystemNs.RainGeoData geoData, List<ThMEPWSS.ReleaseNs.RainSystemNs.RainDrawingData> drDatas, ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo, RainSystemDiagramViewModel vm)
        {
            var mlInfos = new List<MLeaderInfo>(THESAURUSREPERCUSSION);
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<List<Geometry>, Func<Geometry, bool>> T = GeoFac.CreateIntersectsTester;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            {
                var after = new List<ValueTuple<string, Geometry>>();
                var (sankakuptsf, addsankaku) = GeoFac.CreateIntersectsSelectorEngine(mlInfos.Select(x => x.BasePoint.ToNTSPoint(x)));
                void draw(string text, Geometry geo, bool autoCreate = THESAURUSOBSTINACY, bool overWrite = THESAURUSOBSTINACY)
                {
                    Point2d center;
                    if (geo is Point point)
                    {
                        center = point.ToPoint2d();
                        geo = GRect.Create(center, UNCONSEQUENTIAL).ToPolygon();
                    }
                    else
                    {
                        center = geo.GetCenter();
                    }
                    {
                        var ok = INTRAVASCULARLY;
                        foreach (var pt in sankakuptsf(geo))
                        {
                            var info = (MLeaderInfo)pt.UserData;
                            if (string.IsNullOrWhiteSpace(info.Text) || overWrite)
                            {
                                info.Text = text;
                            }
                            ok = THESAURUSOBSTINACY;
                        }
                        if (!ok && autoCreate)
                        {
                            var pt = center;
                            var info = MLeaderInfo.Create(pt, text);
                            mlInfos.Add(info);
                            var p = pt.ToNTSPoint();
                            p.UserData = info;
                            addsankaku(p);
                        }
                    }
                }
                Action lazy = null;
                foreach (var si in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count))
                {
                    var item = cadDatas[si];
                    var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, DINOFLAGELLATES).ToList();
                    var wlinesGeosf = F(wlinesGeos);
                    var fdst = T(item.FloorDrains.Select(x => x.Buffer(THESAURUSPERMUTATION)).ToList());
                    var portst = T(item.RainPortSymbols.Select(x => x.Buffer(THESAURUSENTREPRENEUR)).ToList());
                    var wellst = T(item.WaterWells.Select(x => x.Buffer(MISAPPREHENSIVE)).ToList());
                    var fdsf = F(item.FloorDrains);
                    var ppsf = F(item.VerticalPipes);
                    var ppst = T(item.VerticalPipes);
                    var cpst = T(item.CondensePipes);
                    var cpsf = F(item.CondensePipes);
                    foreach (var wlinesGeo in wlinesGeos)
                    {
                        var lines = Substract(GeoFac.GetLines(wlinesGeo).ToList(), item.VerticalPipes.Select(x => x.Buffer(-SUPERLATIVENESS)).Concat(item.CondensePipes.Select(x => x.Buffer(-SUPERLATIVENESS))));
                        lines = GeoFac.ToNodedLineSegments(lines).Where(x => x.Length >= DISPROPORTIONAL).ToList();
                        lines = GeoFac.GroupParallelLines(lines, THESAURUSAPPARATUS, THESAURUSHOUSING).Select(g => GetCenterLine(g)).ToList();
                        lines = GeoFac.ToNodedLineSegments(lines.Select(x => x.Extend(THESAURUSHOUSING)).ToList()).Where(x => x.Length > THESAURUSMORTUARY).Select(x => new GLineSegment(new Point2d(Convert.ToInt64(x.StartPoint.X), Convert.ToInt64(x.StartPoint.Y)), new Point2d(Convert.ToInt64(x.EndPoint.X), Convert.ToInt64(x.EndPoint.Y)))).Distinct().ToList();
                        foreach (var seg in lines)
                        {
                            if (seg.Length < DINOFLAGELLATES) continue;
                            var pt = seg.Center;
                            var info = MLeaderInfo.Create(pt, THESAURUSDEPLORE);
                            mlInfos.Add(info);
                            var p = pt.ToNTSPoint();
                            p.UserData = info;
                            addsankaku(p);
                        }
                        lazy += () =>
                        {
                            var linesf = GeoFac.CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList());
                            {
                                foreach (var cp in item.CondensePipes)
                                {
                                    foreach (var line in linesf(cp))
                                    {
                                        var dn = vm.Params.CondensePipeHorizontalDN;
                                        draw(dn, line.GetCenter().Expand(THESAURUSHOUSING).ToGRect().ToPolygon());
                                    }
                                }
                                foreach (var fd in item.FloorDrains)
                                {
                                    foreach (var line in linesf(fd))
                                    {
                                        var dn = vm.Params.BalconyFloorDrainDN;
                                        draw(dn, line.GetCenter().Expand(THESAURUSHOUSING).ToGRect().ToPolygon());
                                    }
                                }
                            }
                            if (cpst(wlinesGeo))
                            {
                                var r = GeoFac.CreateGeometry(ppsf(wlinesGeo).Concat(cpsf(wlinesGeo))).ToGRect();
                                var dn = vm.Params.CondensePipeHorizontalDN;
                                after.Add(new(dn, r.ToPolygon()));
                            }
                            if (fdst(wlinesGeo))
                            {
                                var r = GeoFac.CreateGeometry(ppsf(wlinesGeo).Concat(fdsf(wlinesGeo))).ToGRect().Expand(-VÖLKERWANDERUNG);
                                var dn = vm.Params.BalconyFloorDrainDN;
                                after.Add(new(dn, r.ToPolygon()));
                            }
                            var _pts = lines.SelectMany(seg => new Point2d[] { seg.StartPoint, seg.EndPoint }).GroupBy(x => x).Select(x => x.Key).Distinct().ToList();
                            var pts = _pts.Select(x => x.ToNTSPoint()).ToList();
                            var ptsf = GeoFac.CreateIntersectsSelector(pts);
                            var stPts = pts.Where(pt => fdst(pt) || cpst(pt)).ToList();
                            var edPts = pts.Where(pt => portst(pt)).ToList();
                            if (edPts.Count == THESAURUSSTAMPEDE)
                            {
                                var pps = ppsf(wlinesGeo);
                                if (pps.Count == THESAURUSHOUSING)
                                {
                                    edPts = pts.Where(pt => ppst(pt)).ToList();
                                }
                                else
                                {
                                }
                            }
                            foreach (var edPt in edPts)
                            {
                                var mdPts = pts.Except(stPts).Except(edPts).ToList();
                                var nodes = pts.Select(x => new GraphNode<Point>(x)).ToList();
                                {
                                    var kvs = new HashSet<KeyValuePair<int, int>>();
                                    foreach (var seg in lines)
                                    {
                                        var i = pts.IndexOf(seg.StartPoint.ToNTSPoint());
                                        var j = pts.IndexOf(seg.EndPoint.ToNTSPoint());
                                        if (i != j)
                                        {
                                            if (i > j)
                                            {
                                                ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.Swap(ref i, ref j);
                                            }
                                            kvs.Add(new KeyValuePair<int, int>(i, j));
                                        }
                                    }
                                    foreach (var kv in kvs)
                                    {
                                        nodes[kv.Key].AddNeighbour(nodes[kv.Value], THESAURUSHOUSING);
                                    }
                                }
                                var dijkstra = new Dijkstra<Point>(nodes);
                                {
                                    var paths = new List<IList<GraphNode<Point>>>(stPts.Count);
                                    var areaDict = new Dictionary<GLineSegment, double>();
                                    foreach (var stPt in stPts)
                                    {
                                        var path = dijkstra.FindShortestPathBetween(nodes[pts.IndexOf(stPt)], nodes[pts.IndexOf(edPt)]);
                                        paths.Add(path);
                                    }
                                    foreach (var path in paths)
                                    {
                                        for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                        {
                                            areaDict[new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d())] = THESAURUSSTAMPEDE;
                                        }
                                    }
                                    foreach (var path in paths)
                                    {
                                        for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                        {
                                            var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                            var sel = seg.Buffer(VÖLKERWANDERUNG);
                                            foreach (var pt in sankakuptsf(sel))
                                            {
                                                var info = (MLeaderInfo)pt.UserData;
                                                if (!string.IsNullOrEmpty(info.Text))
                                                {
                                                    var r = parseDn(info.Text);
                                                    areaDict[seg] += r * r;
                                                }
                                            }
                                        }
                                    }
                                    foreach (var path in paths)
                                    {
                                        double area = THESAURUSSTAMPEDE;
                                        for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                        {
                                            var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                            var sel = seg.Buffer(VÖLKERWANDERUNG);
                                            foreach (var pt in sankakuptsf(sel))
                                            {
                                                var info = (MLeaderInfo)pt.UserData;
                                                if (!string.IsNullOrEmpty(info.Text))
                                                {
                                                    var r = parseDn(info.Text);
                                                    if (area < r * r) area = r * r;
                                                }
                                            }
                                            if (areaDict[seg] < area) areaDict[seg] = area;
                                        }
                                    }
                                    {
                                        var dict = areaDict.ToDictionary(x => x.Key, x => QUOTATIONTRANSFERABLE);
                                        foreach (var path in paths)
                                        {
                                            for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                            {
                                                var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                                dict[seg] += areaDict[seg];
                                            }
                                        }
                                        areaDict = dict;
                                    }
                                    foreach (var kv in areaDict)
                                    {
                                        var sel = kv.Key.Buffer(VÖLKERWANDERUNG);
                                        foreach (var pt in sankakuptsf(sel))
                                        {
                                            var info = (MLeaderInfo)pt.UserData;
                                            info.Text = getDn(kv.Value);
                                        }
                                    }
                                }
                            }
                            if (portst(wlinesGeo))
                            {
                                foreach (var line in lines)
                                {
                                    if (line.Length > THESAURUSNOTORIETY)
                                    {
                                        draw(IRRESPONSIBLENESS, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                    }
                                }
                            }
                            if (wellst(wlinesGeo))
                            {
                                foreach (var line in lines)
                                {
                                    if (line.Length > THESAURUSNOTORIETY)
                                    {
                                        draw(IRRESPONSIBLENESS, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                    }
                                }
                            }
                        };
                    }
                }
                lazy?.Invoke();
                foreach (var tp in after.OrderByDescending(tp => tp.Item2.Area))
                {
                    draw(tp.Item1, tp.Item2, INTRAVASCULARLY);
                }
            }
            foreach (var info in mlInfos)
            {
                if (info.Text == THESAURUSDEPLORE) info.Text = IRRESPONSIBLENESS;
            }
            foreach (var info in mlInfos)
            {
                if (!string.IsNullOrWhiteSpace(info.Text)) DrawMLeader(info.Text, info.BasePoint, info.BasePoint.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
            }
        }
        public const int THESAURUSHOUSING = 1;
        public const int THESAURUSCOMMUNICATION = 5;
        public const int THESAURUSPERMUTATION = 2;
        public const int THESAURUSSTAMPEDE = 0;
        public const int THESAURUSACRIMONIOUS = 10;
        public const bool INTRAVASCULARLY = false;
        public const bool THESAURUSOBSTINACY = true;
        public const int DISPENSABLENESS = 40;
        public const int SUPERLATIVENESS = 6;
        public const int THESAURUSREPERCUSSION = 4096;
        public const int HYPERDISYLLABLE = 100;
        public const string THESAURUSDEPLORE = "";
        public const int THESAURUSENTREPRENEUR = 50;
        public const int THESAURUSQUAGMIRE = 90;
        public const int QUOTATIONWITTIG = 500;
        public const int THESAURUSCAVERN = 180;
        public const string THESAURUSDISREPUTABLE = "DN25";
        public const string IRRESPONSIBLENESS = "DN100";
        public const double QUOTATIONTRANSFERABLE = .0;
        public const int THESAURUSINHERIT = 2000;
        public const int MISAPPREHENSIVE = 200;
        public const int VÖLKERWANDERUNG = 30;
        public const int THESAURUSNOTORIETY = 3000;
        public const string CONTROVERSIALLY = "TH-STYLE3";
        public const double UNCONSEQUENTIAL = .01;
        public const int DINOFLAGELLATES = 15;
        public const string QUOTATIONDOPPLER = "DN75";
        public const string CIRCUMSTANTIARE = "卫生间";
        public const string THESAURUSCIPHER = "主卫";
        public const string PREMILLENNIALIST = "公卫";
        public const string THESAURUSINDIGENT = "次卫";
        public const string THESAURUSFRAMEWORK = "客卫";
        public const string THESAURUSHUMILIATION = "洗手间";
        public const string THESAURUSSTERLING = "卫";
        public const string THESAURUSSCREEN = @"^[卫]\d$";
        public const string THESAURUSPEDESTRIAN = "厨房";
        public const string THESAURUSRECLUSE = "西厨";
        public const string THESAURUSINCONTROVERTIBLE = "厨";
        public const string NATIONALIZATION = "阳台";
        public const string THESAURUSSPECIES = "连廊";
        public const string QUOTATIONBREWSTER = "DN50";
        public const int THESAURUSMORTUARY = 12;
        public const int PHOTOSYNTHETICALLY = 270;
        public const int THESAURUSNEGATE = 76;
        public const string THESAURUSRESIGNED = "多通道";
        public const string PHOTOAUTOTROPHIC = "洗衣机";
        public const string THESAURUSPOWERLESS = "\n处理中断。未找到有效楼层。";
        public const string QUOTATIONISOPHANE = "已有的管径标注将被覆盖，是否继续？";
        public const string THESAURUSMISADVENTURE = "Quetion";
        public const double DISPROPORTIONAL = 5.01;
        public const double THESAURUSAPPARATUS = 10.01;
        public const string ARGENTIMACULATUS = "W-辅助-管径";
        public const int THESAURUSSYMMETRICAL = 26;
        public const int THESAURUSDRAGOON = 33;
        public const string ALLITERATIVENESS = "DN32";
        public const int THESAURUSMORTALITY = 51;
        public const string SUPERNATURALIZE = @"\d+";
        public const string THESAURUSCIRCUMSTANTIAL = "DN15";
        public const int THESAURUSINNOCUOUS = 99;
        public const double THESAURUSPLAYGROUND = .00001;
        public const double ULTRASONOGRAPHY = .0001;
        const string MLeaderLayer = ARGENTIMACULATUS;
        public static MLeader DrawMLeader(string content, Point2d p1, Point2d p2)
        {
            var e = new MLeader();
            e.MText = new MText() { Contents = content, TextHeight = HYPERDISYLLABLE, ColorIndex = DISPENSABLENESS, };
            e.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
            e.ArrowSize = THESAURUSENTREPRENEUR;
            e.DoglegLength = THESAURUSSTAMPEDE;
            e.LandingGap = THESAURUSSTAMPEDE;
            e.ExtendLeaderToText = INTRAVASCULARLY;
            e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
            e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
            e.AddLeaderLine(p1.ToPoint3d());
            var bd = e.MText.Bounds.ToGRect();
            var p3 = p2.OffsetY(bd.Height + HYPERDISYLLABLE).ToPoint3d();
            if (p2.X < p1.X)
            {
                p3 = p3.OffsetX(-bd.Width);
            }
            e.TextLocation = p3;
            e.Layer = MLeaderLayer;
            DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
            return e;
        }
        public static MLeader DrawMLeader(string content, Point3d p1, Point3d p2)
        {
            var e = new MLeader();
            e.MText = new MText() { Contents = content, TextHeight = HYPERDISYLLABLE, ColorIndex = DISPENSABLENESS, };
            e.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
            e.ArrowSize = THESAURUSENTREPRENEUR;
            e.DoglegLength = THESAURUSSTAMPEDE;
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
            e.Layer = MLeaderLayer;
            DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
            return e;
        }
        private static void ClearMLeader()
        {
            var adb = _DrawingTransaction.Current.adb;
            LayerTools.AddLayer(adb.Database, MLeaderLayer);
            foreach (var e in adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer))
            {
                adb.Element<Entity>(e.ObjectId, THESAURUSOBSTINACY).Erase();
            }
        }
        static string getDn(double area)
        {
            return DnToString(Math.Sqrt(area));
        }
        static double calcDn(double dn1, double dn2)
        {
            return Math.Sqrt(dn1 * dn1 + dn2 * dn2);
        }
        static string DnToString(double dn)
        {
            if (dn < THESAURUSSYMMETRICAL) return THESAURUSDISREPUTABLE;
            if (dn < THESAURUSDRAGOON) return ALLITERATIVENESS;
            if (dn < THESAURUSMORTALITY) return QUOTATIONBREWSTER;
            if (dn < THESAURUSNEGATE) return QUOTATIONDOPPLER;
            if (dn < THESAURUSINNOCUOUS) return QUOTATIONDOPPLER;
            return IRRESPONSIBLENESS;
        }
        static double parseDn(string dn)
        {
            var m = Regex.Match(dn, SUPERNATURALIZE);
            if (m.Success) return double.Parse(m.Value);
            return THESAURUSSTAMPEDE;
        }
        public static Geometry CreateXGeoRect(GRect r)
        {
            return new MultiLineString(new LineString[] {
                r.ToLinearRing(),
                new LineString(new Coordinate[] { r.LeftTop.ToNTSCoordinate(), r.RightButtom.ToNTSCoordinate() }),
                new LineString(new Coordinate[] { r.LeftButtom.ToNTSCoordinate(), r.RightTop.ToNTSCoordinate() })
            });
        }
        public static GRect GetBounds(params Geometry[] geos) => new GeometryCollection(geos).ToGRect();
        public static void DrawBackToFlatDiagram(List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> storeysItems, ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData, List<ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageDrawingData> drDatas, ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo, DrainageSystemDiagramViewModel vm)
        {
            var mlInfos = new List<MLeaderInfo>(THESAURUSREPERCUSSION);
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<List<Geometry>, Func<Geometry, bool>> T = GeoFac.CreateIntersectsTester;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            var (sankakuptsf, addsankaku) = GeoFac.CreateIntersectsSelectorEngine(mlInfos.Select(x => x.BasePoint.ToNTSPoint(x)));
            var roomData = geoData.RoomData;
            var _kitchens = roomData.Where(x => IsKitchen(x.Key)).Select(x => x.Value).ToList();
            var _toilets = roomData.Where(x => IsToilet(x.Key)).Select(x => x.Value).ToList();
            var _nonames = roomData.Where(x => x.Key is THESAURUSDEPLORE).Select(x => x.Value).ToList();
            var _balconies = roomData.Where(x => IsBalcony(x.Key)).Select(x => x.Value).ToList();
            var kitchenst = T(_kitchens);
            void draw(string text, Geometry geo, bool autoCreate = THESAURUSOBSTINACY, bool overWrite = THESAURUSOBSTINACY)
            {
                Point2d center;
                if (geo is Point point)
                {
                    center = point.ToPoint2d();
                    geo = GRect.Create(center, UNCONSEQUENTIAL).ToPolygon();
                }
                else
                {
                    center = geo.GetCenter();
                }
                {
                    var ok = INTRAVASCULARLY;
                    foreach (var pt in sankakuptsf(geo))
                    {
                        var info = (MLeaderInfo)pt.UserData;
                        if (string.IsNullOrWhiteSpace(info.Text) || overWrite)
                        {
                            info.Text = text;
                        }
                        ok = THESAURUSOBSTINACY;
                    }
                    if (!ok && autoCreate)
                    {
                        var pt = center;
                        var info = MLeaderInfo.Create(pt, text);
                        mlInfos.Add(info);
                        var p = pt.ToNTSPoint();
                        p.UserData = info;
                        addsankaku(p);
                    }
                }
            }
            var shooters = geoData.FloorDrainTypeShooter.SelectNotNull(kv =>
            {
                var name = kv.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (name.Contains(THESAURUSRESIGNED) || name.Contains(PHOTOAUTOTROPHIC))
                    {
                        return GRect.Create(kv.Key, HYPERDISYLLABLE).ToPolygon();
                    }
                }
                return null;
            }).ToList();
            var shooterst = GeoFac.CreateIntersectsTester(shooters);
            var zbqst = GeoFac.CreateIntersectsTester(geoData.zbqs.Select(x => x.ToPolygon()).ToList());
            var xstst = GeoFac.CreateIntersectsTester(geoData.xsts.Select(x => x.ToPolygon()).ToList());
            var queue1 = new Queue<Action>();
            var queue2 = new Queue<Action>();
            var queue3 = new Queue<Action>();
            foreach (var si in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count))
            {
                var item = cadDatas[si];
                var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, DINOFLAGELLATES).ToList();
                dlinesGeos = dlinesGeos.SelectMany(dlinesGeo =>
                {
                    var lines = Substract(GeoFac.GetLines(dlinesGeo).ToList(), item.VerticalPipes.Select(x => x.Buffer(-SUPERLATIVENESS)).Concat(item.DownWaterPorts.Select(x => x.Buffer(-SUPERLATIVENESS))));
                    lines = GeoFac.ToNodedLineSegments(lines).Where(x => x.Length >= DISPROPORTIONAL).ToList();
                    lines = GeoFac.GroupParallelLines(lines, THESAURUSAPPARATUS, THESAURUSHOUSING).Select(g => GetCenterLine(g)).ToList();
                    lines = GeoFac.ToNodedLineSegments(lines.Select(x => x.Extend(DISPROPORTIONAL)).ToList()).Where(x => x.Length > THESAURUSCOMMUNICATION).ToList();
                    return GeoFac.GroupGeometries(lines.Select(x => x.Extend(THESAURUSPLAYGROUND).ToLineString()).ToList()).Select(x =>
                    {
                        var lsArr= x.Cast<LineString>().SelectMany(x => GeoFac.GetLines(x)).Distinct(new GLineSegment.EqualityComparer(ULTRASONOGRAPHY)).Select(x=> new GLineSegment(new Point2d(Convert.ToInt64(x.StartPoint.X), Convert.ToInt64(x.StartPoint.Y)), new Point2d(Convert.ToInt64(x.EndPoint.X), Convert.ToInt64(x.EndPoint.Y))).ToLineString()).ToArray();
                        return new MultiLineString(lsArr);
                    });
                }).Cast<Geometry>().ToList();
                var dlinesGeosf = F(dlinesGeos);
                var wrappingPipesf = F(item.WrappingPipes);
                var dpsf = F(item.DownWaterPorts);
                var dpst = T(item.DownWaterPorts);
                var portst = T(item.WaterPorts.Select(x => x.Buffer(MISAPPREHENSIVE)).ToList());
                var fldrs = item.FloorDrains;
                var fdst = T(fldrs.Select(x => x.Buffer(THESAURUSPERMUTATION)).ToList());
                var fdsf = F(fldrs);
                var ppsf = F(item.VerticalPipes);
                var ppst = T(item.VerticalPipes);
                queue1.Enqueue(() =>
                {
                    var dlscf = GeoFac.CreateContainsSelector(dlinesGeos);
                    foreach (var sel in GeoFac.GroupGeometries(fldrs.Select(x => CreateXGeoRect(x.Buffer(THESAURUSPERMUTATION).ToGRect())).Concat(dlinesGeos).ToList()).Select(x => GeoFac.CreateGeometry(x)))
                    {
                        var fds = fdsf(sel);
                        if (fds.Count == THESAURUSPERMUTATION)
                        {
                            var r = GetBounds(fds.ToArray());
                            var dls = dlscf(r.ToPolygon());
                            if (dls.Count == THESAURUSHOUSING)
                            {
                                queue1.Enqueue(() =>
                                {
                                    string dn;
                                    if (shooterst(GeoFac.CreateGeometryEx(fds)))
                                    {
                                        dn = vm.Params.WashingMachineFloorDrainDN;
                                    }
                                    else
                                    {
                                        dn = vm.Params.OtherFloorDrainDN;
                                    }
                                    foreach (var dl in dlscf(r.ToPolygon()))
                                    {
                                        draw(dn, dl.Buffer(THESAURUSPERMUTATION));
                                    }
                                    var fdst = T(fds);
                                    foreach (var dl in dlinesGeosf(r.ToPolygon()).Except(dls).SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).Where(x => fdst(x)))
                                    {
                                        var _dn = dn;
                                        if (_dn == QUOTATIONBREWSTER) _dn = QUOTATIONDOPPLER;
                                        draw(_dn, dl.Buffer(THESAURUSPERMUTATION));
                                    }
                                    {
                                    }
                                });
                            }
                        }
                    }
                });
                foreach (var dlinesGeo in dlinesGeos)
                {
                    var lines = GeoFac.GetLines(dlinesGeo);
                    var linesf = GeoFac.CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList());
                    foreach (var seg in lines)
                    {
                        if (seg.Length < DINOFLAGELLATES) continue;
                        var pt = seg.Center;
                        var info = MLeaderInfo.Create(pt, THESAURUSDEPLORE);
                        mlInfos.Add(info);
                        var p = pt.ToNTSPoint();
                        p.UserData = info;
                        addsankaku(p);
                    }
                    queue2.Enqueue(() =>
                    {
                        var dlinesGeoBuf = dlinesGeo.Buffer(THESAURUSACRIMONIOUS);
                        var dlbufgf = dlinesGeoBuf.ToIPreparedGeometry();
                        if (fdst(dlinesGeo) || ppst(dlinesGeo))
                        {
                            foreach (var line in lines)
                            {
                                foreach (var dp in dpsf(line.ToLineString()))
                                {
                                    if (zbqst(dp))
                                    {
                                        var dn = IRRESPONSIBLENESS;
                                        draw(dn, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                    }
                                    else if (xstst(dp))
                                    {
                                        var dn = QUOTATIONBREWSTER;
                                        draw(dn, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                    }
                                    else
                                    {
                                        var pt = line.Center;
                                        if (kitchenst(pt.ToNTSPoint()))
                                        {
                                            var dn = QUOTATIONDOPPLER;
                                            draw(dn, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                        }
                                        else
                                        {
                                            var dn = QUOTATIONBREWSTER;
                                            draw(dn, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                        }
                                    }
                                }
                                foreach (var fd in fdsf(line.ToLineString()))
                                {
                                    string dn;
                                    if (shooterst(fd))
                                    {
                                        dn = vm.Params.WashingMachineFloorDrainDN;
                                    }
                                    else
                                    {
                                        dn = vm.Params.OtherFloorDrainDN;
                                    }
                                    draw(dn, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                }
                            }
                        }
                        queue3.Enqueue(() =>
                        {
                            var _pts = lines.SelectMany(seg => new Point2d[] { seg.StartPoint, seg.EndPoint }).GroupBy(x => x).Select(x => x.Key).Distinct().Where(pt => dlbufgf.Intersects(pt.ToNTSPoint())).ToList();
                            var pts = _pts.Select(x => x.ToNTSPoint()).ToList();
                            var ptsf = GeoFac.CreateIntersectsSelector(pts);
                            var stPts = pts.Where(pt => fdst(pt) || dpst(pt)).ToList();
                            var edPts = pts.Where(pt => portst(pt)).ToList();
                            if (edPts.Count == THESAURUSSTAMPEDE)
                            {
                                var pps = ppsf(dlinesGeo);
                                if (pps.Count == THESAURUSHOUSING)
                                {
                                    edPts = pts.Where(pt => ppst(pt)).ToList();
                                }
                                else if (pps.Count > THESAURUSHOUSING)
                                {
                                    if (!portst(dlinesGeo))
                                    {
                                        foreach (var line in lines)
                                        {
                                            if (line.Length >= THESAURUSINHERIT && line.IsHorizontalOrVertical(THESAURUSCOMMUNICATION))
                                            {
                                                var ls = line.ToLineString();
                                                foreach (var pp in pps)
                                                {
                                                    if (ls.Intersects(pp))
                                                    {
                                                        edPts.AddRange(ptsf(pp));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (edPts.Count == THESAURUSHOUSING)
                            {
                                var edPt = edPts[THESAURUSSTAMPEDE];
                                var mdPts = pts.Except(stPts).Except(edPts).ToList();
                                var nodes = pts.Select(x => new GraphNode<Point>(x)).ToList();
                                {
                                    var kvs = new HashSet<KeyValuePair<int, int>>();
                                    foreach (var seg in lines)
                                    {
                                        var i = pts.IndexOf(seg.StartPoint.ToNTSPoint());
                                        var j = pts.IndexOf(seg.EndPoint.ToNTSPoint());
                                        if (i != j)
                                        {
                                            if (i > j)
                                            {
                                                ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.Swap(ref i, ref j);
                                            }
                                            kvs.Add(new KeyValuePair<int, int>(i, j));
                                        }
                                    }
                                    foreach (var kv in kvs)
                                    {
                                        nodes[kv.Key].AddNeighbour(nodes[kv.Value], THESAURUSHOUSING);
                                    }
                                }
                                var dijkstra = new Dijkstra<Point>(nodes);
                                {
                                    var paths = new List<IList<GraphNode<Point>>>(stPts.Count);
                                    var areaDict = new Dictionary<GLineSegment, double>();
                                    foreach (var stPt in stPts)
                                    {
                                        var path = dijkstra.FindShortestPathBetween(nodes[pts.IndexOf(stPt)], nodes[pts.IndexOf(edPt)]);
                                        paths.Add(path);
                                    }
                                    foreach (var path in paths)
                                    {
                                        for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                        {
                                            areaDict[new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d())] = THESAURUSSTAMPEDE;
                                        }
                                    }
                                    foreach (var path in paths)
                                    {
                                        for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                        {
                                            var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                            var sel = seg.Buffer(VÖLKERWANDERUNG);
                                            foreach (var pt in sankakuptsf(sel))
                                            {
                                                var info = (MLeaderInfo)pt.UserData;
                                                if (!string.IsNullOrEmpty(info.Text))
                                                {
                                                    var r = parseDn(info.Text);
                                                    areaDict[seg] += r * r;
                                                }
                                            }
                                        }
                                    }
                                    foreach (var path in paths)
                                    {
                                        double area = THESAURUSSTAMPEDE;
                                        for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                        {
                                            var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                            var sel = seg.Buffer(VÖLKERWANDERUNG);
                                            foreach (var pt in sankakuptsf(sel))
                                            {
                                                var info = (MLeaderInfo)pt.UserData;
                                                if (!string.IsNullOrEmpty(info.Text))
                                                {
                                                    var r = parseDn(info.Text);
                                                    if (area < r * r) area = r * r;
                                                }
                                            }
                                            if (areaDict[seg] < area) areaDict[seg] = area;
                                        }
                                    }
                                    {
                                        var dict = areaDict.ToDictionary(x => x.Key, x => QUOTATIONTRANSFERABLE);
                                        foreach (var path in paths)
                                        {
                                            for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                            {
                                                var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                                dict[seg] += areaDict[seg];
                                            }
                                        }
                                        areaDict = dict;
                                    }
                                    foreach (var kv in areaDict)
                                    {
                                        var sel = kv.Key.Buffer(VÖLKERWANDERUNG);
                                        foreach (var pt in sankakuptsf(sel))
                                        {
                                            var info = (MLeaderInfo)pt.UserData;
                                            info.Text = getDn(kv.Value);
                                        }
                                    }
                                }
                            }
                            else
                            {
                            }
                            if (portst(dlinesGeo))
                            {
                                foreach (var line in lines)
                                {
                                    if (line.Length > THESAURUSNOTORIETY)
                                    {
                                        var dn = IRRESPONSIBLENESS;
                                        draw(dn, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                    }
                                }
                            }
                        });
                    });
                }
            }
            while (queue1.Count + queue2.Count + queue3.Count > THESAURUSSTAMPEDE)
            {
                while (queue1.Count > THESAURUSSTAMPEDE) queue1.Dequeue()();
                if (queue2.Count > THESAURUSSTAMPEDE)
                {
                    queue2.Dequeue()();
                    continue;
                }
                if (queue3.Count > THESAURUSSTAMPEDE)
                {
                    queue3.Dequeue()();
                    continue;
                }
            }
            foreach (var info in mlInfos)
            {
                if (info.Text == THESAURUSDEPLORE) info.Text = IRRESPONSIBLENESS;
            }
            foreach (var info in mlInfos)
            {
                if (!string.IsNullOrWhiteSpace(info.Text)) DrawMLeader(info.Text, info.BasePoint, info.BasePoint.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
            }
        }
        private static void NewMethod2(ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo, RainSystemDiagramViewModel vm)
        {
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            for (int si = THESAURUSSTAMPEDE; si < cadDatas.Count; si++)
            {
                var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                foreach (var kv in lbdict)
                {
                }
                var item = cadDatas[si];
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, DINOFLAGELLATES).ToList();
                var wlinesGeosf = F(wlinesGeos);
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var wlines = item.WLines.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).ToList();
                    var wlf = GeoFac.CreateIntersectsSelector(wlines);
                    foreach (var fd in item.FloorDrains)
                    {
                        foreach (var wl in wlf(fd))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var kv in lbdict)
                    {
                        var pp = kv.Key;
                        foreach (var wl in wlf(pp))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var cp in item.CondensePipes)
                    {
                        foreach (var wl in wlf(cp))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                drawMLeader(vm?.Params?.CondensePipeHorizontalDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                drawMLeader(vm?.Params?.CondensePipeHorizontalDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var ws in item.WaterSealingWells)
                    {
                        foreach (var wl in wlf(ws.Buffer(QUOTATIONWITTIG)))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                drawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                drawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var well in item.WaterWells)
                    {
                        foreach (var wl in wlf(well.Buffer(QUOTATIONWITTIG)))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                drawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                drawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var ws in item.RainPortSymbols)
                    {
                        foreach (var wl in wlf(ws))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                drawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                drawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                }
            }
            void drawMLeader(string content, Point2d p1, Point2d p2)
            {
                var e = new MLeader();
                e.ColorIndex = DISPENSABLENESS;
                e.MText = new MText() { Contents = content, TextHeight = MISAPPREHENSIVE, ColorIndex = DISPENSABLENESS, };
                e.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
                e.ArrowSize = THESAURUSENTREPRENEUR;
                e.DoglegLength = THESAURUSSTAMPEDE;
                e.LandingGap = THESAURUSSTAMPEDE;
                e.ExtendLeaderToText = INTRAVASCULARLY;
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
                e.AddLeaderLine(p1.ToPoint3d());
                var bd = e.MText.Bounds.ToGRect();
                var p3 = p2.OffsetY(bd.Height + HYPERDISYLLABLE).ToPoint3d();
                if (p2.X < p1.X)
                {
                    p3 = p3.OffsetX(-bd.Width);
                }
                e.TextLocation = p3;
                DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
            }
        }
        public static void DrawBackToFlatDiagram(ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo.storeysItems, exInfo.geoData, exInfo.drDatas, exInfo, exInfo.vm);
        }
        private static void NewMethod1(ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo, DrainageSystemDiagramViewModel vm)
        {
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            for (int si = THESAURUSSTAMPEDE; si < cadDatas.Count; si++)
            {
                var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                foreach (var kv in lbdict)
                {
                }
                var item = cadDatas[si];
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, DINOFLAGELLATES).ToList();
                var dlinesGeosf = F(dlinesGeos);
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var dlines = item.DLines.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).ToList();
                    var dlf = GeoFac.CreateIntersectsSelector(dlines);
                    foreach (var fd in item.FloorDrains)
                    {
                        foreach (var dl in dlf(fd))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var fd in item.DownWaterPorts)
                    {
                        foreach (var dl in dlf(fd))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                DrawMLeader(QUOTATIONDOPPLER, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                DrawMLeader(QUOTATIONDOPPLER, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var kv in lbdict)
                    {
                        var pp = kv.Key;
                        foreach (var dl in dlf(pp))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? QUOTATIONBREWSTER, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                    foreach (var port in item.WaterPorts)
                    {
                        foreach (var dl in dlf(port.Buffer(QUOTATIONWITTIG)))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (THESAURUSSTAMPEDE <= dg && dg <= THESAURUSQUAGMIRE || THESAURUSCAVERN <= dg && dg <= PHOTOSYNTHETICALLY)
                            {
                                DrawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                            else
                            {
                                DrawMLeader(THESAURUSCIRCUMSTANTIAL, p, p.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
                            }
                        }
                    }
                }
            }
        }
        public class Ref<T>
        {
            public T Value;
            public Ref() { }
            public Ref(T v) { Value = v; }
        }
        public class BlockInfo
        {
            public string LayerName;
            public string BlockName;
            public Point3d BasePoint;
            public double Rotate;
            public double Scale;
            public Dictionary<string, string> PropDict;
            public Dictionary<string, object> DynaDict;
            public BlockInfo(string blockName, string layerName, Point3d basePoint)
            {
                this.LayerName = layerName;
                this.BlockName = blockName;
                this.BasePoint = basePoint;
                this.PropDict = new Dictionary<string, string>();
                this.DynaDict = new Dictionary<string, object>();
                this.Rotate = THESAURUSSTAMPEDE;
                this.Scale = THESAURUSHOUSING;
            }
        }
        public class LineInfo
        {
            public GLineSegment Line;
            public string LayerName;
            public LineInfo(GLineSegment line, string layerName)
            {
                this.Line = line;
                this.LayerName = layerName;
            }
        }
        public class DBTextInfo
        {
            public string LayerName;
            public string TextStyle;
            public Point3d BasePoint;
            public string Text;
            public double Rotation;
            public DBTextInfo(Point3d point, string text, string layerName, string textStyle)
            {
                text ??= THESAURUSDEPLORE;
                this.LayerName = layerName;
                this.TextStyle = textStyle;
                this.BasePoint = point;
                this.Text = text;
            }
        }
    }
    public class Dijkstra<T>
    {
        private readonly List<GraphNode<T>> _graph;
        private IPriorityQueue<GraphNode<T>> _unvistedNodes;
        public Dijkstra(IEnumerable<GraphNode<T>> graph)
        {
            _graph = graph.ToList();
        }
        public IList<GraphNode<T>> FindShortestPathBetween(GraphNode<T> start, GraphNode<T> finish)
        {
            PrepareGraphForDijkstra();
            start.TentativeDistance = THESAURUSSTAMPEDE;
            var current = start;
            while (THESAURUSOBSTINACY)
            {
                foreach (var neighbour in current.Neighbours.Where(x => !x.GraphNode.Visited))
                {
                    var newTentativeDistance = current.TentativeDistance + neighbour.Distance;
                    if (newTentativeDistance < neighbour.GraphNode.TentativeDistance)
                    {
                        neighbour.GraphNode.TentativeDistance = newTentativeDistance;
                    }
                }
                current.Visited = THESAURUSOBSTINACY;
                var next = _unvistedNodes.Pop();
                if (next == null || next.TentativeDistance == int.MaxValue)
                {
                    if (finish.TentativeDistance == int.MaxValue)
                    {
                        return new List<GraphNode<T>>();
                    }
                    finish.Visited = THESAURUSOBSTINACY;
                    break;
                }
                var smallest = next;
                current = smallest;
            }
            return DeterminePathFromWeightedGraph(start, finish);
        }
        private static List<GraphNode<T>> DeterminePathFromWeightedGraph(GraphNode<T> start, GraphNode<T> finish)
        {
            var current = finish;
            var path = new List<GraphNode<T>> { current };
            var currentTentativeDistance = finish.TentativeDistance;
            while (THESAURUSOBSTINACY)
            {
                if (current == start)
                {
                    break;
                }
                foreach (var neighbour in current.Neighbours.Where(x => x.GraphNode.Visited))
                {
                    if (currentTentativeDistance - neighbour.Distance == neighbour.GraphNode.TentativeDistance)
                    {
                        current = neighbour.GraphNode;
                        path.Add(current);
                        currentTentativeDistance -= neighbour.Distance;
                        break;
                    }
                }
            }
            path.Reverse();
            return path;
        }
        private void PrepareGraphForDijkstra()
        {
            _unvistedNodes = new PriorityQueue<GraphNode<T>>(new CompareNeighbour<T>());
            _graph.ForEach(x =>
            {
                x.Visited = INTRAVASCULARLY;
                x.TentativeDistance = int.MaxValue;
                _unvistedNodes.Push(x);
            });
        }
    }
    internal class CompareNeighbour<T> : IComparer<GraphNode<T>>
    {
        public int Compare(GraphNode<T> x, GraphNode<T> y)
        {
            if (x.TentativeDistance > y.TentativeDistance)
            {
                return THESAURUSHOUSING;
            }
            if (x.TentativeDistance < y.TentativeDistance)
            {
                return -THESAURUSHOUSING;
            }
            return THESAURUSSTAMPEDE;
        }
    }
    public class GraphNode<T>
    {
        public readonly List<Neighbour> Neighbours;
        public bool Visited = INTRAVASCULARLY;
        public T Value;
        public int TentativeDistance;
        public GraphNode(T value)
        {
            Value = value;
            Neighbours = new List<Neighbour>();
        }
        public void AddNeighbour(GraphNode<T> graphNode, int distance)
        {
            Neighbours.Add(new Neighbour(graphNode, distance));
            graphNode.Neighbours.Add(new Neighbour(this, distance));
        }
        public struct Neighbour
        {
            public int Distance;
            public GraphNode<T> GraphNode;
            public Neighbour(GraphNode<T> graphNode, int distance)
            {
                GraphNode = graphNode;
                Distance = distance;
            }
        }
    }
    public interface IPriorityQueue<T>
    {
        void Push(T item);
        T Pop();
        bool Contains(T item);
    }
    public class PriorityQueue<T> : IPriorityQueue<T>
    {
        private readonly List<T> _innerList = new List<T>();
        private readonly IComparer<T> _comparer;
        public int Count
        {
            get { return _innerList.Count; }
        }
        public PriorityQueue(IComparer<T> comparer = null)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }
        public void Push(T item)
        {
            _innerList.Add(item);
        }
        public T Pop()
        {
            if (_innerList.Count <= THESAURUSSTAMPEDE)
            {
                return default(T);
            }
            Sort();
            var item = _innerList[THESAURUSSTAMPEDE];
            _innerList.RemoveAt(THESAURUSSTAMPEDE);
            return item;
        }
        public bool Contains(T item)
        {
            return _innerList.Contains(item);
        }
        private void Sort()
        {
            _innerList.Sort(_comparer);
        }
    }
}