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
        public string Note;
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
            var angles = segs.Select(seg => seg.SingleAngle).ToList();
            if (angles.Max() - angles.Min() >= Math.PI / THESAURUSPERMUTATION)
            {
                for (int i = THESAURUSSTAMPEDE; i < angles.Count; i++)
                {
                    if (angles[i] > Math.PI / THESAURUSPERMUTATION)
                    {
                        angles[i] -= Math.PI;
                    }
                }
            }
            var angle = angles.Average();
            var r = Extents2dCalculator.Calc(segs.YieldPoints()).ToGRect();
            var m = Matrix2d.Displacement(-r.Center.ToVector2d()).PreMultiplyBy(Matrix2d.Rotation(-angle, default));
            r = Extents2dCalculator.Calc(segs.Select(seg => seg.TransformBy(m))).ToGRect();
            return new GLineSegment(GetMidPoint(r.LeftButtom, r.LeftTop), GetMidPoint(r.RightButtom, r.RightTop)).TransformBy(m.Inverse());
        }
        public static bool IsY1L(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(CHRISTIANIZATION) || label.StartsWith(CONSTRUCTIONIST) || label.StartsWith(CONSTRUCTIONIST);
        }
        public static bool IsY2L(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(UNPREMEDITATEDNESS);
        }
        public static bool IsNL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(THESAURUSFINICKY);
        }
        public static bool IsYL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(THESAURUSUNBEATABLE);
        }
        public static bool IsWL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSANTIDOTE);
        }
        public static bool IsFL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSDISSOLVE);
        }
        public static bool IsDraiPipeLabel(string label) => IsFL(label) || IsPL(label) || IsTL(label) || IsWL(label);
        public static bool IsRainPipeLabel(string label) => IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label);
        public static bool IsDraiFL(string label) => IsFL(label) && !IsFL0(label);
        public static bool IsFL0(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            if (label == THESAURUSBEATITUDE) return THESAURUSOBSTINACY;
            return IsFL(label) && label.Contains(AUTOLITHOGRAPHIC);
        }
        public static bool IsPL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSABUNDANT);
        }
        public static bool IsTL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSOPTIONAL);
        }
        public static bool IsDL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, DIASTEREOISOMER);
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
        static double FixValue(double v)
        {
            return Math.Floor(v + THESAURUSCONFECTIONERY);
        }
        public static void DrawBackToFlatDiagram(List<StoreyInfo> storeysItems, ThMEPWSS.ReleaseNs.RainSystemNs.RainGeoData geoData, List<ThMEPWSS.ReleaseNs.RainSystemNs.RainDrawingData> drDatas, ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo, RainSystemDiagramViewModel vm)
        {
            static string getDn(int dn)
            {
                return DnToString(dn);
            }
            static string DnToString(int dn)
            {
                if (dn < THESAURUSREVERSE) return PHOTOFLUOROGRAM;
                if (dn < THESAURUSSYMMETRICAL) return THESAURUSDISREPUTABLE;
                if (dn < THESAURUSDRAGOON) return ALLITERATIVENESS;
                if (dn < THESAURUSMORTALITY) return QUOTATIONBREWSTER;
                if (dn < THESAURUSNEGATE) return QUOTATIONDOPPLER;
                if (dn < THESAURUSINNOCUOUS) return QUOTATIONDOPPLER;
                return IRRESPONSIBLENESS;
            }
            int parseDn(string dn)
            {
                if (dn.StartsWith(THESAURUSVICTORIOUS)) return THESAURUSENTREPRENEUR;
                if (dn is THESAURUSJOURNAL) return THESAURUSEVERLASTING;
                if (dn is CHRISTIANIZATION or UNPREMEDITATEDNESS) return HYPERDISYLLABLE;
                if (dn is THESAURUSFINICKY) return parseDn(vm.Params.CondensePipeVerticalDN);
                var m = Regex.Match(dn, SUPERNATURALIZE);
                if (m.Success) return int.Parse(m.Value);
                return THESAURUSSTAMPEDE;
            }
            var vp2fdpts = new HashSet<Point2d>();
            var y1lpts = new HashSet<Point2d>();
            var y2lpts = new HashSet<Point2d>();
            var nlpts = new HashSet<Point2d>();
            var fl0pts = new HashSet<Point2d>();
            {
                var toCmp = new HashSet<KeyValuePair<int, int>>();
                {
                    var items = storeysItems.Where(x => x.Numbers.Any()).ToList();
                    var minlst = items.Select(x => x.Numbers.Min()).ToList();
                    var maxlst = items.Select(x => x.Numbers.Max()).ToList();
                    for (int i = THESAURUSSTAMPEDE; i < maxlst.Count; i++)
                    {
                        var max = maxlst[i];
                        for (int j = THESAURUSSTAMPEDE; j < maxlst.Count; j++)
                        {
                            if (j == i) continue;
                            var min = minlst[j];
                            if (min + THESAURUSHOUSING == max)
                            {
                                toCmp.Add(new KeyValuePair<int, int>(storeysItems.IndexOf(items[j]), storeysItems.IndexOf(items[i])));
                            }
                        }
                    }
                }
                foreach (var kv in toCmp)
                {
                    var low = storeysItems[kv.Key];
                    var high = storeysItems[kv.Value];
                    var fds = geoData.FloorDrains.Select(x => x.ToGCircle(INTRAVASCULARLY).ToCirclePolygon(SUPERLATIVENESS, THESAURUSOBSTINACY)).ToList();
                    var fdsf = GeoFac.CreateEnvelopeSelector(fds);
                    fds = fdsf(high.Boundary.ToPolygon());
                    var pps = geoData.VerticalPipes.Select(x => x.ToPolygon()).ToList();
                    var ppsf = GeoFac.CreateEnvelopeSelector(pps);
                    pps = ppsf(low.Boundary.ToPolygon());
                    var vhigh = -high.ContraPoint.ToVector2d();
                    var vlow = -low.ContraPoint.ToVector2d();
                    {
                        var v = low.ContraPoint - high.ContraPoint;
                        var si = kv.Value;
                        var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                        var y1ls = lbdict.Where(x => IsY1L(x.Value)).Select(x => x.Key).ToList();
                        var y2ls = lbdict.Where(x => IsY2L(x.Value)).Select(x => x.Key).ToList();
                        var nls = lbdict.Where(x => IsNL(x.Value)).Select(x => x.Key).ToList();
                        var fl0s = lbdict.Where(x => IsFL0(x.Value)).Select(x => x.Key).ToList();
                        y1lpts.AddRange(y1ls.Select(x => x.GetCenter() + v));
                        y2lpts.AddRange(y2ls.Select(x => x.GetCenter() + v));
                        nlpts.AddRange(nls.Select(x => x.GetCenter() + v));
                        fl0pts.AddRange(fl0s.Select(x => x.GetCenter() + v));
                    }
                    var _fds = fds.Select(x => x.Offset(vhigh)).ToList();
                    var _pps = pps.Select(x => x.Offset(vlow)).ToList();
                    var _ppsf = GeoFac.CreateIntersectsSelector(_pps);
                    foreach (var fd in _fds)
                    {
                        var pp = _ppsf(fd.GetCenter().ToNTSPoint()).FirstOrDefault();
                        if (pp != null)
                        {
                            vp2fdpts.Add(pps[_pps.IndexOf(pp)].GetCenter());
                        }
                    }
                }
            }
            var vp2fdptst = GeoFac.CreateIntersectsTester(vp2fdpts.Select(x => x.ToNTSPoint()).ToList());
            var vp2fdptrgs = vp2fdpts.Select(x => GRect.Create(x, THESAURUSHESITANCY).ToPolygon()).ToList();
            var vp2fdptrgst = GeoFac.CreateIntersectsTester(vp2fdptrgs);
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
                void modify(Geometry range, Action<MLeaderInfo> cb)
                {
                    foreach (var pt in sankakuptsf(range))
                    {
                        cb((MLeaderInfo)pt.UserData);
                    }
                }
                void draw(string text, Geometry geo, bool autoCreate = THESAURUSOBSTINACY, bool overWrite = THESAURUSOBSTINACY, string note = null)
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
                            if (!string.IsNullOrWhiteSpace(note)) info.Note = note;
                            ok = THESAURUSOBSTINACY;
                        }
                        if (!ok && autoCreate)
                        {
                            var pt = center;
                            var info = MLeaderInfo.Create(pt, text);
                            mlInfos.Add(info);
                            var p = pt.ToNTSPoint();
                            p.UserData = info;
                            if (!string.IsNullOrWhiteSpace(note)) info.Note = note;
                            addsankaku(p);
                        }
                    }
                }
                {
                    using var prq = new PriorityQueue(THESAURUSINCOMPLETE);
                    var precisePts = new HashSet<Point2d>();
                    {
                        foreach (var si in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count))
                        {
                            var djPts = new HashSet<Point>();
                            var _linesGroup = new HashSet<HashSet<GLineSegment>>();
                            var storey = geoData.Storeys[si].ToPolygon();
                            var gpGeos = GeoFac.CreateEnvelopeSelector(geoData.Groups.Select(GeoFac.CreateGeometry).ToList())(storey);
                            var wlsegs = geoData.OWLines.ToList();
                            var vertices = GeoFac.CreateEnvelopeSelector(wlsegs.Select(x => x.StartPoint.ToNTSPoint().Tag(x)).Concat(wlsegs.Select(x => x.EndPoint.ToNTSPoint().Tag(x))).ToList())(storey);
                            var verticesf = GeoFac.CreateIntersectsSelector(vertices);
                            wlsegs = vertices.Select(x => x.UserData).Cast<GLineSegment>().Distinct().ToList();
                            prq.Enqueue(THESAURUSOCCASIONALLY, () =>
                            {
                                var linesGeos = _linesGroup.Select(lines => new MultiLineString(lines.Select(x => x.ToLineString()).ToArray())).ToList();
                                var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
                                foreach (var lines in _linesGroup)
                                {
                                    var kvs = new List<KeyValuePair<GLineSegment, MLeaderInfo>>();
                                    foreach (var line in lines.Where(x => x.Length > QUOTATIONLUCANIAN))
                                    {
                                        var pts = sankakuptsf(line.Buffer(THESAURUSPERMUTATION));
                                        if (pts.Count == THESAURUSHOUSING)
                                        {
                                            var pt = pts[THESAURUSSTAMPEDE];
                                            var info = (MLeaderInfo)pt.UserData;
                                            kvs.Add(new KeyValuePair<GLineSegment, MLeaderInfo>(line, info));
                                        }
                                    }
                                    {
                                        var segs = kvs.Select(kv => kv.Key).Distinct().ToList();
                                        var lns = segs.Select(x => x.ToLineString()).ToList();
                                        var vertexs = segs.YieldPoints().Distinct().ToList();
                                        var lnsf = GeoFac.CreateIntersectsSelector(lns);
                                        var lnscf = GeoFac.CreateContainsSelector(segs.Select(x => x.Extend(-THESAURUSPERMUTATION).ToLineString()).ToList());
                                        var opts = new List<Ref<RegexOptions>>();
                                        foreach (var vertex in vertexs)
                                        {
                                            var lst = lnsf(GeoFac.CreateCirclePolygon(vertex, THESAURUSCOMMUNICATION, SUPERLATIVENESS));
                                            RegexOptions opt;
                                            if (lst.Count == THESAURUSHOUSING)
                                            {
                                                opt = RegexOptions.IgnoreCase;
                                            }
                                            else if (lst.Count == THESAURUSPERMUTATION)
                                            {
                                                opt = RegexOptions.Multiline;
                                            }
                                            else if (lst.Count > THESAURUSPERMUTATION)
                                            {
                                                opt = RegexOptions.ExplicitCapture;
                                            }
                                            else
                                            {
                                                opt = RegexOptions.None;
                                            }
                                            opts.Add(new Ref<RegexOptions>(opt));
                                        }
                                        foreach (var geo in GeoFac.GroupLinesByConnPoints(GeoFac.GetLines(GeoFac.CreateGeometry(lns).Difference(GeoFac.CreateGeometryEx(opts.Where(x => x.Value == RegexOptions.ExplicitCapture).Select(opts).ToList(vertexs).Select(x => x.ToGCircle(THESAURUSPERMUTATION).ToCirclePolygon(SUPERLATIVENESS)).ToList()))).Select(x => x.ToLineString()).ToList(), UNCONSEQUENTIAL))
                                        {
                                            var bf = geo.Buffer(THESAURUSPERMUTATION);
                                            var pts = sankakuptsf(bf);
                                            var infos = pts.Select(pt => (MLeaderInfo)pt.UserData).ToList();
                                            if (infos.Select(x => x.Text).Distinct().Count() == THESAURUSHOUSING)
                                            {
                                                var text = infos[THESAURUSSTAMPEDE];
                                                var _segs = lnscf(bf).SelectMany(x => GeoFac.GetLines(x)).ToList();
                                                if (_segs.Count > THESAURUSHOUSING)
                                                {
                                                    const double LEN = PHOTOCONDUCTION;
                                                    if (_segs.Any(x => x.Length >= LEN))
                                                    {
                                                        foreach (var seg in _segs)
                                                        {
                                                            if (seg.Length < LEN)
                                                            {
                                                                draw(null, seg.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var max = _segs.Max(x => x.Length);
                                                        foreach (var seg in _segs)
                                                        {
                                                            if (seg.Length != max)
                                                            {
                                                                draw(null, seg.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                }
                                            }
                                        }
                                    }
                                }
                            });
                            var item = cadDatas[si];
                            var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, DINOFLAGELLATES).ToList();
                            var wlinesGeosf = F(wlinesGeos);
                            var portst = T(item.RainPortSymbols.Select(x => x.Buffer(THESAURUSENTREPRENEUR)).ToList());
                            var portsf = F(item.RainPortSymbols);
                            var wellst = T(item.WaterWells.Select(x => x.Buffer(MISAPPREHENSIVE)).ToList());
                            var wellsf = F(item.WaterWells);
                            var swellst = T(item.WaterSealingWells.Select(x => x.Buffer(MISAPPREHENSIVE)).ToList());
                            var swellsf = F(item.WaterSealingWells);
                            var ditchest = T(item.Ditches.Select(x => x.Buffer(THESAURUSENTREPRENEUR)).ToList());
                            var ditchesf = F(item.Ditches);
                            var fldrs = item.FloorDrains;
                            var vps = item.VerticalPipes;
                            var _vps = vps.ToList();
                            {
                                var fds = vps.Where(vp2fdptst).ToList();
                                fldrs = fldrs.Concat(fds).Distinct().ToList();
                                vps = vps.Except(fds).ToList();
                            }
                            var fdsf = F(fldrs);
                            var fdst = T(fldrs.Select(x => x.Buffer(THESAURUSPERMUTATION)).ToList());
                            var fdrings = geoData.FloorDrainRings.Select(x => x.ToCirclePolygon(THESAURUSDISINGENUOUS).Shell.Buffer(THESAURUSACRIMONIOUS)).ToList();
                            var fdringsf = F(fdrings);
                            var fdringst = T(fdrings);
                            var ppsf = F(vps);
                            var ppst = T(vps);
                            var cpst = T(item.CondensePipes);
                            var cpsf = F(item.CondensePipes);
                            var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                            var y1ls = lbdict.Where(x => IsY1L(x.Value)).Select(x => x.Key).ToList();
                            var y2ls = lbdict.Where(x => IsY2L(x.Value)).Select(x => x.Key).ToList();
                            var nls = lbdict.Where(x => IsNL(x.Value)).Select(x => x.Key).ToList();
                            var fl0s = lbdict.Where(x => IsFL0(x.Value)).Select(x => x.Key).ToList();
                            {
                                var vpsf = F(vps);
                                var _y1ls = vpsf(GeoFac.CreateGeometry((y1lpts.Select(x => x.ToNTSPoint()))));
                                var _y2ls = vpsf(GeoFac.CreateGeometry((y2lpts.Select(x => x.ToNTSPoint()))));
                                var _nls = vpsf(GeoFac.CreateGeometry((nlpts.Select(x => x.ToNTSPoint()))));
                                var _fl0s = vpsf(GeoFac.CreateGeometry((fl0pts.Select(x => x.ToNTSPoint()))));
                                {
                                    var gf = G(item.LabelLines).ToIPreparedGeometry();
                                    y1ls = y1ls.Where(x => gf.Intersects(x)).ToList();
                                    y2ls = y2ls.Where(x => gf.Intersects(x)).ToList();
                                    nls = nls.Where(x => gf.Intersects(x)).ToList();
                                    fl0s = fl0s.Where(x => gf.Intersects(x)).ToList();
                                }
                                {
                                    y1ls = y1ls.Concat(_y1ls).Distinct().ToList();
                                    y2ls = y2ls.Concat(_y2ls).Distinct().ToList();
                                    nls = nls.Concat(_nls).Distinct().ToList();
                                    fl0s = fl0s.Concat(_fl0s).Distinct().ToList();
                                }
                            }
                            var fldrst = T(fldrs);
                            var nlst = T(nls);
                            var y1lst = T(y1ls);
                            var y2lst = T(y2ls);
                            var fl0st = T(fl0s);
                            var fl0sf = F(fl0s);
                            PipeType getPipeType(Point2d pt)
                            {
                                var p = pt.ToNTSPoint();
                                if (nlst(p)) return PipeType.NL;
                                if (y1lst(p)) return PipeType.Y1L;
                                if (y2lst(p)) return PipeType.Y2L;
                                if (fl0st(p)) return PipeType.FL0;
                                return PipeType.Unknown;
                            }
                            string getPipeDn(PipeType type)
                            {
                                return type switch
                                {
                                    PipeType.FL0 => vm.Params.WaterWellPipeVerticalDN,
                                    PipeType.Y2L => vm.Params.BalconyRainPipeDN,
                                    PipeType.NL => vm.Params.CondensePipeVerticalDN,
                                    PipeType.Unknown => null,
                                    _ => IRRESPONSIBLENESS,
                                };
                            }
                            string getDN(Geometry shooter)
                            {
                                if (nlst(shooter)) return vm.Params.CondensePipeVerticalDN;
                                if (y2lst(shooter)) return vm.Params.BalconyRainPipeDN;
                                if (fl0st(shooter)) return vm.Params.WaterWellFloorDrainDN;
                                if (y1lst(shooter)) return IRRESPONSIBLENESS;
                                if (fldrst(shooter)) return vm.Params.BalconyFloorDrainDN;
                                return null;
                            }
                            var nlfdpts = nls.SelectMany(nl => wlinesGeosf(nl)).Distinct().SelectMany(x => fdsf(x))
                                    .Concat(nls.SelectMany(x => fdsf(x.Buffer(THESAURUSHYPNOTIC))))
                                    .Select(x => x.GetCenter()).Distinct().ToList();
                            foreach (var wlinesGeo in wlinesGeos)
                            {
                                var wlinesGeoBuf = wlinesGeo.Buffer(THESAURUSACRIMONIOUS);
                                var wlbufgf = wlinesGeoBuf.ToIPreparedGeometry();
                                prq.Enqueue(THESAURUSCENSURE, () =>
                                {
                                    foreach (var pt in sankakuptsf(wlinesGeo.Buffer(THESAURUSHOUSING)))
                                    {
                                        var info = (MLeaderInfo)pt.UserData;
                                        if (info.Text is THESAURUSDEPLORE)
                                        {
                                            if (nlst(wlinesGeo))
                                            {
                                                info.Text = vm.Params.CondensePipeVerticalDN;
                                            }
                                            else if (y1lst(wlinesGeo) || y2lst(wlinesGeo) || fl0st(wlinesGeo))
                                            {
                                                info.Text = IRRESPONSIBLENESS;
                                            }
                                        }
                                    }
                                });
                                prq.Enqueue(THESAURUSSTAMPEDE, () =>
                                {
                                    var lines = Substract(GeoFac.GetLines(wlinesGeo).ToList(), vps.Select(x => x.Buffer(-SUPERLATIVENESS)).Concat(item.CondensePipes.Select(x => x.Buffer(-SUPERLATIVENESS))));
                                    lines = GeoFac.ToNodedLineSegments(lines).Where(x => x.Length >= DISPROPORTIONAL).ToList();
                                    lines = GeoFac.GroupParallelLines(lines, THESAURUSAPPARATUS, THESAURUSHOUSING).Select(g => GetCenterLine(g)).ToList();
                                    precisePts.AddRange(lines.Select(x => x.Center));
                                    lines = GeoFac.ToNodedLineSegments(lines.Select(x => x.Extend(THESAURUSHOUSING)).ToList()).Where(x => x.Length > THESAURUSMORTUARY).Select(x => new GLineSegment(new Point2d(FixValue(x.StartPoint.X), FixValue(x.StartPoint.Y)), new Point2d(FixValue(x.EndPoint.X), FixValue(x.EndPoint.Y)))).Distinct().ToList();
                                    var linesf = GeoFac.CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList());
                                    {
                                        var _pts = lines.SelectMany(seg => new Point2d[] { seg.StartPoint, seg.EndPoint }).GroupBy(x => x).Select(x => x.Key).Distinct().Where(pt => wlbufgf.Intersects(pt.ToNTSPoint())).ToList();
                                        var pts = _pts.Select(x => x.ToNTSPoint()).ToList();
                                        djPts.AddRange(pts);
                                        _linesGroup.Add(lines.ToHashSet());
                                    }
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
                                    prq.Enqueue(THESAURUSHOUSING, () =>
                                    {
                                        prq.Enqueue(THESAURUSPERMUTATION, () =>
                                            {
                                                foreach (var cp in item.CondensePipes)
                                                {
                                                    foreach (var line in linesf(cp))
                                                    {
                                                        var dn = vm.Params.CondensePipeHorizontalDN;
                                                        draw(dn, line.GetCenter().Expand(THESAURUSHOUSING).ToGRect().ToPolygon());
                                                    }
                                                }
                                            }
);
                                        prq.Enqueue(THESAURUSPERMUTATION, () =>
                                        {
                                            if (cpst(wlinesGeo))
                                            {
                                                var r = GeoFac.CreateGeometry(ppsf(wlinesGeo).Concat(cpsf(wlinesGeo))).ToGRect();
                                                var dn = vm.Params.CondensePipeHorizontalDN;
                                                after.Add(new(dn, r.ToPolygon()));
                                            }
                                            if (fdst(wlinesGeo))
                                            {
                                                var r = GeoFac.CreateGeometry(ppsf(wlinesGeo).Concat(fdsf(wlinesGeo))).ToGRect().Expand(-DISPENSABLENESS);
                                                if (!vp2fdptst(r.ToPolygon()))
                                                {
                                                    string dn;
                                                    {
                                                        if (fl0st(wlinesGeo))
                                                        {
                                                            dn = vm.Params.WaterWellFloorDrainDN;
                                                        }
                                                        else if (nlst(wlinesGeo))
                                                        {
                                                            dn = vm.Params.CondenseFloorDrainDN;
                                                        }
                                                        else
                                                        {
                                                            dn = vm.Params.BalconyFloorDrainDN;
                                                        }
                                                    }
                                                    after.Add(new(dn, r.ToPolygon()));
                                                }
                                            }
                                        });
                                        prq.Enqueue(INTROPUNITIVENESS, () =>
                                        {
                                            foreach (var wlinesGeo in GeoFac.GroupGeometries(lines.Select(x => x.Extend(THESAURUSHOUSING)).Select(x => x.ToLineString()).ToList()).Select(GeoFac.CreateGeometry))
                                            {
                                                if (fldrst(wlinesGeo))
                                                {
                                                    foreach (var seg in GeoFac.GetLines(wlinesGeo))
                                                    {
                                                        string dn;
                                                        {
                                                            if (fl0st(wlinesGeo))
                                                            {
                                                                dn = vm.Params.WaterWellFloorDrainDN;
                                                            }
                                                            else if (nlst(wlinesGeo))
                                                            {
                                                                dn = vm.Params.CondenseFloorDrainDN;
                                                            }
                                                            else
                                                            {
                                                                dn = vm.Params.BalconyFloorDrainDN;
                                                            }
                                                        }
                                                        draw(dn, seg.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                    }
                                                }
                                                else if (nlst(wlinesGeo))
                                                {
                                                    foreach (var seg in GeoFac.GetLines(wlinesGeo))
                                                    {
                                                        {
                                                            draw(vm.Params.CondensePipeVerticalDN, seg.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                        }
                                                    }
                                                }
                                                else if (y1lst(wlinesGeo))
                                                {
                                                    foreach (var seg in GeoFac.GetLines(wlinesGeo))
                                                    {
                                                        {
                                                            draw(IRRESPONSIBLENESS, seg.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                        }
                                                    }
                                                }
                                                else if (y2lst(wlinesGeo))
                                                {
                                                    foreach (var seg in GeoFac.GetLines(wlinesGeo))
                                                    {
                                                        {
                                                            draw(vm.Params.BalconyRainPipeDN, seg.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                        }
                                                    }
                                                }
                                            }
                                        });
                                    });
                                });
                            }
                            prq.Enqueue(ECCLESIASTICISM, () =>
                            {
                                foreach (var gpGeo in gpGeos)
                                {
                                    var pps = ppsf(gpGeo);
                                    foreach (var pp in pps)
                                    {
                                        var dn = getPipeDn(getPipeType(pp.GetCenter()));
                                        if (dn is not null)
                                        {
                                            modify(GeoFac.GetLines(gpGeo, skipPolygon: THESAURUSOBSTINACY).Select(x => x.Buffer(THESAURUSHOUSING)).ToGeometry(), info => info.Text = dn);
                                        }
                                    }
                                    if (pps.Count == THESAURUSHOUSING)
                                    {
                                        var pp = pps[THESAURUSSTAMPEDE];
                                        if (y1ls.Contains(pp) || y2ls.Contains(pp))
                                        {
                                            var buf = gpGeo.Buffer(THESAURUSPERMUTATION);
                                            var oksegs = new HashSet<GLineSegment>();
                                            foreach (var seg in wlsegs)
                                            {
                                                if (buf.Contains(seg.Center.ToNTSPoint()))
                                                {
                                                    foreach (var pt in sankakuptsf(seg.Buffer(THESAURUSHOUSING)))
                                                    {
                                                        ((MLeaderInfo)pt.UserData).Text = IRRESPONSIBLENESS;
                                                    }
                                                    oksegs.Add(seg);
                                                }
                                            }
                                            for (int i = THESAURUSSTAMPEDE; i < THESAURUSCOMMUNICATION; i++)
                                            {
                                                var segs = verticesf(oksegs.YieldPoints().Select(x => x.ToGRect(THESAURUSPERMUTATION).ToPolygon()).ToGeometry()).Select(x => (GLineSegment)x.UserData).Except(oksegs).ToList();
                                                foreach (var seg in segs)
                                                {
                                                    foreach (var pt in sankakuptsf(seg.Buffer(THESAURUSHOUSING)))
                                                    {
                                                        ((MLeaderInfo)pt.UserData).Text = IRRESPONSIBLENESS;
                                                    }
                                                    oksegs.Add(seg);
                                                }
                                                if (segs.Count == THESAURUSSTAMPEDE) break;
                                            }
                                        }
                                    }
                                }
                                {
                                    var wls = GeoFac.CreateEnvelopeSelector(wlsegs.Select(x => x.ToLineString()).ToList())(storey);
                                    foreach (var geo in GeoFac.GroupLinesByConnPoints(wls, THESAURUSPERMUTATION))
                                    {
                                        var buf = geo.Buffer(THESAURUSHOUSING);
                                        if (cpst(buf) && nlst(buf))
                                        {
                                            var pts = sankakuptsf(buf);
                                            if (pts.All(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE or THESAURUSDISREPUTABLE or QUOTATIONBREWSTER or QUOTATIONDOPPLER))
                                            {
                                                foreach (var pt in pts)
                                                {
                                                    ((MLeaderInfo)pt.UserData).Text = THESAURUSDISREPUTABLE;
                                                }
                                            }
                                        }
                                    }
                                    foreach (var wl in wls)
                                    {
                                        if (y1lst(wl))
                                        {
                                            foreach (var pt in sankakuptsf(wl.Buffer(THESAURUSPERMUTATION)))
                                            {
                                                ((MLeaderInfo)pt.UserData).Text = IRRESPONSIBLENESS;
                                            }
                                        }
                                    }
                                }
                                foreach (var geo in GeoFac.GroupLinesByConnPoints(item.WLines, THESAURUSCOMMUNICATION))
                                {
                                    var segs = GeoFac.GetLines(geo).ToList();
                                    if (segs.Count == THESAURUSSTAMPEDE) continue;
                                    if (segs.Count == THESAURUSHOUSING)
                                    {
                                        var seg = segs[THESAURUSSTAMPEDE];
                                        var buf = seg.Buffer(THESAURUSPERMUTATION);
                                        var pts = sankakuptsf(buf);
                                        if (pts.Count == THESAURUSHOUSING)
                                        {
                                            var pt = pts[THESAURUSSTAMPEDE];
                                            if (ppst(seg.StartPoint.ToNTSPoint()) || ppst(seg.EndPoint.ToNTSPoint()))
                                            {
                                                if (fdringst(seg.StartPoint.ToNTSPoint()) || fdringst(seg.EndPoint.ToNTSPoint()))
                                                {
                                                    modify(seg.Buffer(THESAURUSHOUSING), info =>
                                                    {
                                                        if (info.Text is THESAURUSDISREPUTABLE)
                                                        {
                                                            info.Text = QUOTATIONBREWSTER;
                                                        }
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                                {
                                    var lnsGeos = _linesGroup.Select(x => GeoFac.CreateGeometry(x.Where(x => x.Length >= THESAURUSINCOMPLETE).Select(x => x.ToLineString()))).ToList();
                                    var lnsGeosf = GeoFac.CreateIntersectsSelector(lnsGeos);
                                    foreach (var _segs in _linesGroup)
                                    {
                                        var segs = _segs.Where(x => x.Length >= THESAURUSINCOMPLETE).ToList();
                                        var geo = GeoFac.CreateGeometry(segs.Select(x => x.ToLineString()));
                                        var buf = geo.Buffer(THESAURUSPERMUTATION);
                                        if (segs.Count > THESAURUSSTAMPEDE)
                                        {
                                            var pts = sankakuptsf(buf);
                                            if (pts.Count > THESAURUSSTAMPEDE)
                                            {
                                                if (pts.Any(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE) && pts.Any(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDISREPUTABLE) && pts.All(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE or THESAURUSDISREPUTABLE))
                                                {
                                                    foreach (var pt in pts)
                                                    {
                                                        ((MLeaderInfo)pt.UserData).Text = THESAURUSDEPLORE;
                                                    }
                                                }
                                                if (pts.All(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE))
                                                {
                                                    void patch()
                                                    {
                                                        foreach (var gpgeo in gpGeos)
                                                        {
                                                            foreach (var vp in vps)
                                                            {
                                                                if (vp.Intersects(gpgeo))
                                                                {
                                                                    {
                                                                        var dn = getDN(vp.GetCenter().ToNTSPoint());
                                                                        if (dn is null) continue;
                                                                        foreach (var geo in lnsGeosf(new MultiLineString(GeoFac.GetLines(gpgeo, skipPolygon: THESAURUSOBSTINACY).Select(x => x.ToLineString()).ToArray()).Buffer(THESAURUSPERMUTATION)))
                                                                        {
                                                                            draw(dn, geo.Buffer(THESAURUSPERMUTATION), overWrite: INTRAVASCULARLY);
                                                                        }
                                                                    }
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    patch();
                                                }
                                            }
                                        }
                                    }
                                }
                            });
                            prq.Enqueue(THESAURUSCOMMUNICATION, () =>
                              {
                                  var points = djPts.ToList();
                                  var pointsf = GeoFac.CreateIntersectsSelector(points);
                                  var linesGeos = _linesGroup.Select(lines => new MultiLineString(lines.Select(x => x.ToLineString()).ToArray())).ToList();
                                  var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
                                  foreach (var bufGeo in GroupGeometries(_linesGroup.Select(x => GeoFac.CreateGeometry(x.Select(x => x.ToLineString()))).ToList(), vps.Concat(fldrs).ToList()).Select(GeoFac.CreateGeometry).Select(x => x.Buffer(THESAURUSPERMUTATION)))
                                  {
                                      if (portst(bufGeo) || ditchest(bufGeo) || wellst(bufGeo) || swellst(bufGeo))
                                      {
                                          prq.Enqueue(SUPERLATIVENESS, () =>
                                          {
                                              var target = portsf(bufGeo).FirstOrDefault() ?? ditchesf(bufGeo).FirstOrDefault() ?? wellsf(bufGeo).FirstOrDefault() ?? swellsf(bufGeo).FirstOrDefault();
                                              if (target is null) return;
                                              var edPts = GeoFac.CreateIntersectsSelector(pointsf(target))(bufGeo);
                                              foreach (var edPt in edPts)
                                              {
                                                  var fds = fdsf(bufGeo);
                                                  if (fds.Count == THESAURUSSTAMPEDE) return;
                                                  var pps = ppsf(bufGeo);
                                                  if (pps.Count == THESAURUSSTAMPEDE) return;
                                                  var lines = linesGeosk(bufGeo);
                                                  if (pps.All(pp => !pp.Intersects(GeoFac.CreateGeometry(lines)))) return;
                                                  draw(THESAURUSDEPLORE, bufGeo, overWrite: THESAURUSOBSTINACY);
                                                  {
                                                      var lnsf = GeoFac.CreateIntersectsSelector(GeoFac.GetManyLineStrings(lines).ToList());
                                                      if (pps.Any(pp => fl0s.Contains(pp)))
                                                      {
                                                          foreach (var fd in fds)
                                                          {
                                                              foreach (var ln in lnsf(fd))
                                                              {
                                                                  var dn = vm.Params.WaterWellFloorDrainDN;
                                                                  var pt = ln.GetCenter();
                                                                  draw(dn, pt.ToGRect(INTROPUNITIVENESS).ToPolygon());
                                                              }
                                                          }
                                                      }
                                                      else
                                                      {
                                                          foreach (var fd in fds)
                                                          {
                                                              var lns = lnsf(fd);
                                                              if (lns.Count == THESAURUSHOUSING)
                                                              {
                                                                  var ln = lns[THESAURUSSTAMPEDE];
                                                                  string dn;
                                                                  if (fl0st(ln))
                                                                  {
                                                                      dn = vm.Params.WaterWellFloorDrainDN;
                                                                  }
                                                                  else if (nlst(ln))
                                                                  {
                                                                      dn = vm.Params.CondenseFloorDrainDN;
                                                                  }
                                                                  else
                                                                  {
                                                                      dn = vm.Params.BalconyFloorDrainDN;
                                                                  }
                                                                  var pt = ln.GetCenter();
                                                                  draw(dn, pt.ToGRect(INTROPUNITIVENESS).ToPolygon());
                                                              }
                                                          }
                                                      }
                                                      foreach (var pp in pps)
                                                      {
                                                          if (nls.Contains(pp) || y1ls.Contains(pp) || y2ls.Contains(pp) || fl0s.Contains(pp))
                                                          {
                                                              var lns = lnsf(pp);
                                                              if (lns.Count == THESAURUSHOUSING)
                                                              {
                                                                  var ln = lns[THESAURUSSTAMPEDE];
                                                                  string dn;
                                                                  if (nls.Contains(pp))
                                                                  {
                                                                      dn = vm.Params.CondensePipeVerticalDN;
                                                                  }
                                                                  else
                                                                  {
                                                                      dn = vm.Params.BalconyRainPipeDN;
                                                                  }
                                                                  var pt = ln.GetCenter();
                                                                  draw(dn, pt.ToGRect(INTROPUNITIVENESS).ToPolygon());
                                                              }
                                                          }
                                                      }
                                                  }
                                                  static Func<Geometry, bool> CreateContainsTester<T>(List<T> geos) where T : Geometry
                                                  {
                                                      if (geos.Count == THESAURUSSTAMPEDE) return r => INTRAVASCULARLY;
                                                      var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > THESAURUSACRIMONIOUS ? geos.Count : THESAURUSACRIMONIOUS);
                                                      foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
                                                      return geo =>
                                                      {
                                                          if (geo == null) throw new ArgumentNullException();
                                                          var gf = GeoFac.PreparedGeometryFactory.Create(geo);
                                                          return engine.Query(geo.EnvelopeInternal).Any(g => gf.Contains(g));
                                                      };
                                                  }
                                                  prq.Enqueue(THESAURUSSCARCE, () =>
                                                  {
                                                      var t = CreateContainsTester(after.Select(x => x.Item2).ToList());
                                                      var _lines = linesGeosk(bufGeo).SelectMany(x => GeoFac.GetLines(x)).Distinct().ToList();
                                                      var pts = pointsf(bufGeo);
                                                      var ptsf = GeoFac.CreateIntersectsSelector(pts);
                                                      var stPts = new HashSet<Point>();
                                                      var addPts = new HashSet<Point>();
                                                      {
                                                          foreach (var c in vps.Concat(fldrs).Distinct())
                                                          {
                                                              var _pts = ptsf(c.Buffer(THESAURUSPERMUTATION));
                                                              if (_pts.Count == THESAURUSSTAMPEDE) continue;
                                                              if (_pts.Count == THESAURUSHOUSING)
                                                              {
                                                                  stPts.Add(_pts[THESAURUSSTAMPEDE]);
                                                              }
                                                              else
                                                              {
                                                                  var bd = GetBounds(_pts.ToArray());
                                                                  var center = bd.Center.ToNTSPoint();
                                                                  addPts.Add(center);
                                                                  foreach (var seg in _pts.Select(x => new GLineSegment(x.ToPoint2d(), center.ToPoint2d())))
                                                                  {
                                                                      _lines.Add(seg);
                                                                  }
                                                              }
                                                          }
                                                          stPts.Remove(edPt);
                                                      }
                                                      if (stPts.Count == THESAURUSSTAMPEDE)
                                                      {
                                                          _lines = _lines.Except(GeoFac.CreateIntersectsSelector(_lines.Select(x => x.ToLineString()).ToList())(GeoFac.CreateGeometry(addPts.Select(x => x.ToPoint2d().ToGRect(UNCONSEQUENTIAL).ToPolygon()))).SelectMany(geo => GeoFac.GetLines(geo))).ToList();
                                                          addPts.Clear();
                                                          var lines = _lines.Select(x => x.ToLineString()).ToList();
                                                          var linesf = GeoFac.CreateIntersectsSelector(lines);
                                                          foreach (var pp in y1ls.Concat(y2ls).Concat(nls).Concat(fl0s))
                                                          {
                                                              stPts.AddRange(ptsf(pp));
                                                              var lns = linesf(pp).Where(ln => after.Select(x => x.Item2).All(x => !x.Contains(ln))).ToList();
                                                              foreach (var ln in lns)
                                                              {
                                                                  string dn;
                                                                  if (nls.Contains(pp))
                                                                  {
                                                                      dn = vm.Params.CondensePipeVerticalDN;
                                                                  }
                                                                  else
                                                                  {
                                                                      dn = IRRESPONSIBLENESS;
                                                                  }
                                                                  draw(dn, ln.GetCenter().ToNTSPoint(), overWrite: INTRAVASCULARLY);
                                                              }
                                                          }
                                                          foreach (var fd in fldrs)
                                                          {
                                                              stPts.AddRange(ptsf(fd));
                                                              var lns = linesf(fd).Where(ln => after.Select(x => x.Item2).All(x => !x.Contains(ln))).ToList();
                                                              foreach (var ln in lns)
                                                              {
                                                                  string dn;
                                                                  var buf = fd.Buffer(MISAPPREHENSIVE);
                                                                  if (fl0st(buf))
                                                                  {
                                                                      dn = vm.Params.WaterWellFloorDrainDN;
                                                                  }
                                                                  else if (nlst(buf))
                                                                  {
                                                                      dn = vm.Params.CondenseFloorDrainDN;
                                                                  }
                                                                  else
                                                                  {
                                                                      dn = vm.Params.BalconyFloorDrainDN;
                                                                  }
                                                                  draw(dn, ln.GetCenter().ToNTSPoint(), overWrite: INTRAVASCULARLY);
                                                              }
                                                          }
                                                      }
                                                      pts = pts.Concat(addPts).Distinct().ToList();
                                                      var mdPts = pts.Except(stPts).Except(edPts).ToHashSet();
                                                      var nodes = pts.Select(x => new GraphNode<Point>(x)).ToList();
                                                      {
                                                          var kvs = new HashSet<KeyValuePair<int, int>>();
                                                          foreach (var seg in _lines)
                                                          {
                                                              var i = pts.IndexOf(seg.StartPoint.ToNTSPoint());
                                                              if (i < THESAURUSSTAMPEDE) continue;
                                                              var j = pts.IndexOf(seg.EndPoint.ToNTSPoint());
                                                              if (j < THESAURUSSTAMPEDE) continue;
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
                                                          var dnDict = new Dictionary<GLineSegment, int>();
                                                          foreach (var stPt in stPts)
                                                          {
                                                              var path = dijkstra.FindShortestPathBetween(nodes[pts.IndexOf(stPt)], nodes[pts.IndexOf(edPt)]);
                                                              paths.Add(path);
                                                          }
                                                          foreach (var path in paths)
                                                          {
                                                              for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                                              {
                                                                  dnDict[new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d())] = THESAURUSSTAMPEDE;
                                                              }
                                                          }
                                                          foreach (var path in paths)
                                                          {
                                                              for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                                              {
                                                                  var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                                                  var sel = seg.Buffer(THESAURUSHOUSING);
                                                                  foreach (var pt in sankakuptsf(sel))
                                                                  {
                                                                      var info = (MLeaderInfo)pt.UserData;
                                                                      if (!string.IsNullOrEmpty(info.Text))
                                                                      {
                                                                          var r = parseDn(info.Text);
                                                                          dnDict[seg] = Math.Max(r, dnDict[seg]);
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                          foreach (var path in paths)
                                                          {
                                                              int dn = THESAURUSSTAMPEDE;
                                                              for (int i = THESAURUSSTAMPEDE; i < path.Count - THESAURUSHOUSING; i++)
                                                              {
                                                                  var seg = new GLineSegment(path[i].Value.ToPoint2d(), path[i + THESAURUSHOUSING].Value.ToPoint2d());
                                                                  var sel = seg.Buffer(THESAURUSHOUSING);
                                                                  foreach (var pt in sankakuptsf(sel))
                                                                  {
                                                                      var info = (MLeaderInfo)pt.UserData;
                                                                      if (!string.IsNullOrEmpty(info.Text))
                                                                      {
                                                                          var r = parseDn(info.Text);
                                                                          if (dn < r) dn = r;
                                                                      }
                                                                  }
                                                                  if (dnDict[seg] < dn) dnDict[seg] = dn;
                                                              }
                                                          }
                                                          foreach (var kv in dnDict)
                                                          {
                                                              var sel = kv.Key.Buffer(THESAURUSHOUSING);
                                                              foreach (var pt in sankakuptsf(sel))
                                                              {
                                                                  var info = (MLeaderInfo)pt.UserData;
                                                                  info.Text = getDn(kv.Value);
                                                              }
                                                          }
                                                      }
                                                  });
                                              }
                                          });
                                      }
                                  }
                              });
                        }
                    }
                    prq.Enqueue(THESAURUSDESTITUTE, () =>
                    {
                        foreach (var tp in after.OrderByDescending(tp => tp.Item2.Area))
                        {
                            draw(tp.Item1, tp.Item2, INTRAVASCULARLY);
                        }
                    });
                    static List<List<Geometry>> GroupGeometries(List<Geometry> lines, List<Geometry> polys)
                    {
                        var geosGroup = new List<List<Geometry>>();
                        GroupGeometries();
                        return geosGroup;
                        void GroupGeometries()
                        {
                            var geos = lines.Concat(polys).Distinct().ToList();
                            if (geos.Count == THESAURUSSTAMPEDE) return;
                            lines = lines.Distinct().ToList();
                            polys = polys.Distinct().ToList();
                            if (lines.Count + polys.Count != geos.Count) throw new ArgumentException();
                            var lineshs = lines.ToHashSet();
                            var polyhs = polys.ToHashSet();
                            var pairs = _GroupGeometriesToKVIndex(geos).Where(kv =>
                            {
                                if (lineshs.Contains(geos[kv.Key]) && lineshs.Contains(geos[kv.Value])) return INTRAVASCULARLY;
                                if (polyhs.Contains(geos[kv.Key]) && polyhs.Contains(geos[kv.Value])) return INTRAVASCULARLY;
                                return THESAURUSOBSTINACY;
                            }).ToArray();
                            var dict = new ListDict<int>();
                            var h = new BFSHelper()
                            {
                                Pairs = pairs,
                                TotalCount = geos.Count,
                                Callback = (g, i) => dict.Add(g.root, i),
                            };
                            h.BFS();
                            dict.ForEach((_i, l) =>
                            {
                                geosGroup.Add(l.Select(i => geos[i]).ToList());
                            });
                        }
                        static IEnumerable<KeyValuePair<int, int>> _GroupGeometriesToKVIndex<T>(List<T> geos) where T : Geometry
                        {
                            if (geos.Count == THESAURUSSTAMPEDE) yield break;
                            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > THESAURUSACRIMONIOUS ? geos.Count : THESAURUSACRIMONIOUS);
                            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
                            for (int i = THESAURUSSTAMPEDE; i < geos.Count; i++)
                            {
                                var geo = geos[i];
                                var gf = GeoFac.PreparedGeometryFactory.Create(geo);
                                foreach (var j in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Select(g => geos.IndexOf(g)).Where(j => i < j))
                                {
                                    yield return new KeyValuePair<int, int>(i, j);
                                }
                            }
                        }
                    }
                    prq.Enqueue(THESAURUSACRIMONIOUS, () =>
                    {
                        foreach (var info in mlInfos)
                        {
                            if (info.Text == PHOTOFLUOROGRAM) info.Text = THESAURUSDEPLORE;
                        }
                    });
                    prq.Enqueue(THESAURUSOCCASIONALLY, () =>
                    {
                        var points = precisePts.Select(x => x.ToNTSPoint()).ToList();
                        var pointsf = GeoFac.CreateIntersectsSelector(points);
                        foreach (var info in mlInfos)
                        {
                            if (info.Text is not null)
                            {
                                var pts = pointsf(info.BasePoint.ToGRect(THESAURUSHOUSING).ToPolygon());
                                var pt = pts.FirstOrDefault();
                                if (pts.Count > THESAURUSHOUSING)
                                {
                                    pt = GeoFac.NearestNeighbourGeometryF(pts)(info.BasePoint.ToNTSPoint());
                                }
                                if (pt is not null)
                                {
                                    info.BasePoint = pt.ToPoint3d();
                                }
                            }
                        }
                    });
                }
            }
            foreach (var info in mlInfos)
            {
                if (info.Text == THESAURUSDEPLORE) info.Text = THESAURUSIMPETUOUS;
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
        public const double ASSOCIATIONISTS = .1;
        public const bool INTRAVASCULARLY = false;
        public const int THESAURUSDISINGENUOUS = 36;
        public const bool THESAURUSOBSTINACY = true;
        public const int DISPENSABLENESS = 40;
        public const int SUPERLATIVENESS = 6;
        public const int THESAURUSREPERCUSSION = 4096;
        public const int HYPERDISYLLABLE = 100;
        public const int THESAURUSINCOMPLETE = 20;
        public const string THESAURUSDEPLORE = "";
        public const int THESAURUSENTREPRENEUR = 50;
        public const int THESAURUSQUAGMIRE = 90;
        public const int QUOTATIONWITTIG = 500;
        public const int THESAURUSCAVERN = 180;
        public const string THESAURUSDISREPUTABLE = "DN25";
        public const string IRRESPONSIBLENESS = "DN100";
        public const double QUOTATIONTRANSFERABLE = .0;
        public const int THESAURUSHYPNOTIC = 300;
        public const int THESAURUSREVERSE = 24;
        public const int QUOTATIONEDIBLE = 4;
        public const int INTROPUNITIVENESS = 3;
        public const int THESAURUSINHERIT = 2000;
        public const int MISAPPREHENSIVE = 200;
        public const int VÖLKERWANDERUNG = 30;
        public const int ACANTHOCEPHALANS = 18;
        public const string CONTROVERSIALLY = "TH-STYLE3";
        public const double UNCONSEQUENTIAL = .01;
        public const int THESAURUSDESTITUTE = 7;
        public const int DINOFLAGELLATES = 15;
        public const int THESAURUSHESITANCY = 60;
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
        public const string CHRISTIANIZATION = "Y1L";
        public const string UNPREMEDITATEDNESS = "Y2L";
        public const string THESAURUSFINICKY = "NL";
        public const string THESAURUSUNBEATABLE = "YL";
        public const string THESAURUSANTIDOTE = @"^W\d?L";
        public const string THESAURUSDISSOLVE = @"^F\d?L";
        public const string THESAURUSBEATITUDE = "FL-O";
        public const string AUTOLITHOGRAPHIC = "-0";
        public const string THESAURUSABUNDANT = @"^P\d?L";
        public const string THESAURUSOPTIONAL = @"^T\d?L";
        public const string DIASTEREOISOMER = @"^D\d?L";
        public const string QUOTATIONBREWSTER = "DN50";
        public const int THESAURUSSCARCE = 8;
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
        public const string THESAURUSVICTORIOUS = "FLDR";
        public const string THESAURUSJOURNAL = "CP";
        public const int THESAURUSEVERLASTING = 25;
        public const string QUOTATIONPELVIC = "PipeDN100";
        public const int THESAURUSFINALITY = 10000;
        public const string THESAURUSBOTTOM = "PL-";
        public const int THESAURUSCENSURE = 11;
        public const int THESAURUSBEATIFIC = 13;
        public const string PHOTOFLUOROGRAM = "DN0";
        public const int PERICARDIOCENTESIS = 49;
        public const double QUOTATIONLUCANIAN = 5.1;
        public const double PHOTOCONDUCTION = 400.0;
        public const string THESAURUSIMPETUOUS = "DNXXX";
        public const string CONSTRUCTIONIST = "YIL";
        public const int THESAURUSOCCASIONALLY = 19;
        public const int ECCLESIASTICISM = 17;
        public const double THESAURUSCONFECTIONERY = .5;
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
        public static Geometry CreateXGeoRect(GRect r)
        {
            return new MultiLineString(new LineString[] {
                r.ToLinearRing(),
                new LineString(new Coordinate[] { r.LeftTop.ToNTSCoordinate(), r.RightButtom.ToNTSCoordinate() }),
                new LineString(new Coordinate[] { r.LeftButtom.ToNTSCoordinate(), r.RightTop.ToNTSCoordinate() })
            });
        }
        public static GRect GetBounds(params Geometry[] geos) => new GeometryCollection(geos).ToGRect();
        public static void DrawBackToFlatDiagram(List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> _storeysItems, ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData, List<ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageDrawingData> drDatas, ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo, DrainageSystemDiagramViewModel vm)
        {
            static string getDn(double area)
            {
                return DnToString(Math.Sqrt(area));
            }
            static string DnToString(double dn)
            {
                if (dn < THESAURUSSYMMETRICAL) return PHOTOFLUOROGRAM;
                if (dn < THESAURUSDRAGOON) return PHOTOFLUOROGRAM;
                if (dn < PERICARDIOCENTESIS) return PHOTOFLUOROGRAM;
                if (dn < THESAURUSMORTALITY) return QUOTATIONBREWSTER;
                if (dn < THESAURUSNEGATE) return QUOTATIONDOPPLER;
                if (dn < THESAURUSINNOCUOUS) return QUOTATIONDOPPLER;
                return IRRESPONSIBLENESS;
            }
            static double parseDn(string dn)
            {
                if (dn.StartsWith(THESAURUSVICTORIOUS)) return THESAURUSENTREPRENEUR;
                if (dn is THESAURUSJOURNAL) return THESAURUSEVERLASTING;
                if (dn is CHRISTIANIZATION or UNPREMEDITATEDNESS) return HYPERDISYLLABLE;
                if (dn is THESAURUSFINICKY) return THESAURUSENTREPRENEUR;
                var m = Regex.Match(dn, SUPERNATURALIZE);
                if (m.Success) return double.Parse(m.Value);
                return THESAURUSSTAMPEDE;
            }
            var mlInfos = new List<MLeaderInfo>(THESAURUSREPERCUSSION);
            var tk = DateTime.Now.Ticks % THESAURUSFINALITY;
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
            void draw(string text, Geometry geo, bool autoCreate = THESAURUSOBSTINACY, bool overWrite = THESAURUSOBSTINACY, string note = null)
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
                        if (!string.IsNullOrWhiteSpace(note)) info.Note = note;
                        ok = THESAURUSOBSTINACY;
                    }
                    if (!ok && autoCreate)
                    {
                        var pt = center;
                        var info = MLeaderInfo.Create(pt, text);
                        mlInfos.Add(info);
                        var p = pt.ToNTSPoint();
                        p.UserData = info;
                        if (!string.IsNullOrWhiteSpace(note)) info.Note = note;
                        addsankaku(p);
                    }
                }
            }
            var zbqst = GeoFac.CreateIntersectsTester(geoData.zbqs.Select(x => x.ToPolygon()).ToList());
            var xstst = GeoFac.CreateIntersectsTester(geoData.xsts.Select(x => x.ToPolygon()).ToList());
            var dnInfectors = new List<KeyValuePair<Point2d, string>>();
            var vpInfectors = new HashSet<Point2d>();
            var flpts = new HashSet<Point2d>();
            var plpts = new HashSet<Point2d>();
            var vp2fdpts = new HashSet<Point2d>();
            var shooters = new HashSet<Point>();
            {
                var circleshooters = new HashSet<KeyValuePair<Point2d, Point2d>>();
                var storeysItems = ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext.StoreyContext.StoreyInfos;
                var toCmp = new HashSet<KeyValuePair<int, int>>();
                {
                    var items = storeysItems.Where(x => x.Numbers.Any()).ToList();
                    var minlst = items.Select(x => x.Numbers.Min()).ToList();
                    var maxlst = items.Select(x => x.Numbers.Max()).ToList();
                    for (int i = THESAURUSSTAMPEDE; i < maxlst.Count; i++)
                    {
                        var max = maxlst[i];
                        for (int j = THESAURUSSTAMPEDE; j < maxlst.Count; j++)
                        {
                            if (j == i) continue;
                            var min = minlst[j];
                            if (min + THESAURUSHOUSING == max)
                            {
                                toCmp.Add(new KeyValuePair<int, int>(storeysItems.IndexOf(items[j]), storeysItems.IndexOf(items[i])));
                            }
                        }
                    }
                }
                var _shooters = geoData.FloorDrainTypeShooter.SelectNotNull(kv =>
                {
                    var name = kv.Value;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (name.Contains(THESAURUSRESIGNED) || name.Contains(PHOTOAUTOTROPHIC))
                        {
                            return kv.Key.ToNTSPoint();
                        }
                    }
                    return null;
                }).ToList();
                shooters.AddRange(_shooters);
                var _shootersf = GeoFac.CreateIntersectsSelector(_shooters);
                foreach (var _kv in toCmp)
                {
                    var lbdict = exInfo.Items[_kv.Value].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                    var low = storeysItems[_kv.Key];
                    var high = storeysItems[_kv.Value];
                    var highBound = high.Boundary.ToPolygon();
                    var fds = geoData.FloorDrains.Select(x => x.ToGCircle(INTRAVASCULARLY).ToCirclePolygon(SUPERLATIVENESS, THESAURUSOBSTINACY)).ToList();
                    var fdsf = GeoFac.CreateEnvelopeSelector(fds);
                    fds = fdsf(high.Boundary.ToPolygon());
                    var pps = geoData.VerticalPipes.Select(x => x.ToPolygon()).ToList();
                    var ppsf = GeoFac.CreateEnvelopeSelector(pps);
                    pps = ppsf(low.Boundary.ToPolygon());
                    var dps = geoData.DownWaterPorts.Select(x => x.Center.ToGRect(HYPERDISYLLABLE).ToPolygon()).ToList();
                    var dpsf = GeoFac.CreateEnvelopeSelector(dps);
                    dps = dpsf(high.Boundary.ToPolygon());
                    var circles = geoData.VerticalPipes.Concat(geoData.DownWaterPorts).Select(x => x.ToPolygon()).ToList();
                    var circlesf = GeoFac.CreateEnvelopeSelector(circles);
                    circles = circlesf(low.Boundary.ToPolygon());
                    var vhigh = -high.ContraPoint.ToVector2d();
                    var vlow = -low.ContraPoint.ToVector2d();
                    var _dps = dps.Select(x => x.Offset(vhigh)).ToList();
                    var _circles = circles.Select(x => x.Offset(vlow)).ToList();
                    var _circlesf = GeoFac.CreateIntersectsSelector(_circles);
                    var _fds = fds.Select(x => x.Offset(vhigh)).ToList();
                    var _pps = pps.Select(x => x.Offset(vlow)).ToList();
                    var _ppsf = GeoFac.CreateIntersectsSelector(_pps);
                    var v = low.ContraPoint - high.ContraPoint;
                    {
                        shooters.AddRange(_shootersf(highBound).Select(x => x.Offset(v)));
                    }
                    {
                        var fls = lbdict.Where(x => IsDraiFL(x.Value)).Select(x => x.Key).ToList();
                        var pls = lbdict.Where(x => IsPL(x.Value)).Select(x => x.Key).ToList();
                        var tls = lbdict.Where(x => IsTL(x.Value)).Select(x => x.Key).ToList();
                        foreach (var pp in fls.Concat(pls).Concat(tls))
                        {
                            var pt = pp.GetCenter().Offset(v);
                            dnInfectors.Add(new KeyValuePair<Point2d, string>(pt, IRRESPONSIBLENESS));
                            vpInfectors.Add(pt);
                            if (fls.Contains(pp)) flpts.Add(pp.GetCenter() + v);
                            else if (pls.Contains(pp)) plpts.Add(pp.GetCenter() + v);
                        }
                    }
                    foreach (var pt in dps.Where(dp => kitchenst(dp)).Select(dp => dp.Offset(v).GetCenter()))
                    {
                        dnInfectors.Add(new KeyValuePair<Point2d, string>(pt, vm.Params.BasinDN));
                    }
                    foreach (var pt in dps.Where(dp => zbqst(dp)).Select(dp => dp.Offset(v).GetCenter()))
                    {
                        dnInfectors.Add(new KeyValuePair<Point2d, string>(pt, IRRESPONSIBLENESS));
                    }
                    foreach (var pt in dps.Where(dp => xstst(dp)).Select(dp => dp.Offset(v).GetCenter()))
                    {
                        dnInfectors.Add(new KeyValuePair<Point2d, string>(pt, QUOTATIONBREWSTER));
                    }
                    foreach (var fd in _fds)
                    {
                        var pp = _ppsf(fd.GetCenter().ToNTSPoint()).FirstOrDefault();
                        if (pp != null)
                        {
                            vp2fdpts.Add(pps[_pps.IndexOf(pp)].GetCenter());
                        }
                    }
                    foreach (var dp in _dps)
                    {
                        var circle = _circlesf(dp).FirstOrDefault();
                        if (circle != null)
                        {
                            circleshooters.Add(new KeyValuePair<Point2d, Point2d>(dps[_dps.IndexOf(dp)].GetCenter(), circles[_circles.IndexOf(circle)].GetCenter()));
                        }
                    }
                }
            }
            var shooterst = GeoFac.CreateIntersectsTester(shooters.ToList());
            var vp2fdptst = GeoFac.CreateIntersectsTester(vp2fdpts.Select(x => x.ToNTSPoint()).ToList());
            var vp2fdptrgs = vp2fdpts.Select(x => GRect.Create(x, THESAURUSHESITANCY).ToPolygon()).ToList();
            var vp2fdptrgst = GeoFac.CreateIntersectsTester(vp2fdptrgs);
            {
                using var prq = new PriorityQueue(THESAURUSINCOMPLETE);
                var cleaningPortPtRgs = geoData.CleaningPortBasePoints.Select(x => x.ToGRect(THESAURUSINCOMPLETE).ToPolygon()).ToList();
                var cleaningPortPtsRgst = GeoFac.CreateIntersectsTester(cleaningPortPtRgs);
                foreach (var si in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count))
                {
                    var item = cadDatas[si];
                    var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                    var labelLinesGroup = GG(item.LabelLines);
                    var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                    var labellinesGeosf = F(labelLinesGeos);
                    var storey = geoData.Storeys[si].ToPolygon();
                    var gpGeos = GeoFac.CreateEnvelopeSelector(geoData.Groups.Select(GeoFac.CreateGeometry).ToList())(storey);
                    var dlsegs = geoData.DLines;
                    var vertices = GeoFac.CreateEnvelopeSelector(dlsegs.Select(x => x.StartPoint.ToNTSPoint().Tag(x)).Concat(dlsegs.Select(x => x.EndPoint.ToNTSPoint().Tag(x))).ToList())(storey);
                    var verticesf = GeoFac.CreateIntersectsSelector(vertices);
                    dlsegs = vertices.Select(x => x.UserData).Cast<GLineSegment>().Distinct().ToList();
                    var killer = item.VerticalPipes.Select(x => x.Buffer(-UNCONSEQUENTIAL)).Concat(item.DownWaterPorts.Select(x => x.Buffer(-UNCONSEQUENTIAL))).Concat(item.FloorDrains.Select(x => x.Buffer(-UNCONSEQUENTIAL)));
                    var dlinesGeos = GeoFac.GroupLinesByConnPoints(Substract(item.DLines.SelectMany(x => GeoFac.GetLines(x)).ToList(), killer).Select(x => x.ToLineString()).ToList(), DINOFLAGELLATES).ToList();
                    var precisePts = new HashSet<Point2d>();
                    dlinesGeos = dlinesGeos.SelectMany(dlinesGeo =>
                    {
                        var lines = Substract(GeoFac.GetLines(dlinesGeo).ToList(), killer);
                        lines = GeoFac.ToNodedLineSegments(lines).Where(x => x.Length >= DISPROPORTIONAL).ToList();
                        lines = GeoFac.GroupParallelLines(lines, THESAURUSAPPARATUS, THESAURUSHOUSING).Select(g => GetCenterLine(g)).ToList();
                        lines = GeoFac.ToNodedLineSegments(lines.Select(x => x.Extend(DISPROPORTIONAL)).ToList()).Where(x => x.Length > THESAURUSCOMMUNICATION).ToList();
                        precisePts.AddRange(lines.Select(x => x.Center));
                        return GeoFac.GroupGeometries(lines.Select(x => x.Extend(THESAURUSPLAYGROUND).ToLineString()).ToList()).Select(x =>
                        {
                            var lsArr = x.Cast<LineString>().SelectMany(x => GeoFac.GetLines(x)).Distinct(new GLineSegment.EqualityComparer(ULTRASONOGRAPHY)).Select(x => new GLineSegment(new Point2d(FixValue(x.StartPoint.X), FixValue(x.StartPoint.Y)), new Point2d(FixValue(x.EndPoint.X), FixValue(x.EndPoint.Y))).ToLineString()).ToArray();
                            return new MultiLineString(lsArr);
                        });
                    }).Cast<Geometry>().ToList();
                    var dlinesGeosf = F(dlinesGeos);
                    var wrappingPipesf = F(item.WrappingPipes);
                    var dps = item.DownWaterPorts;
                    var ports = item.WaterPorts;
                    var portst = T(ports.Select(x => x.Buffer(MISAPPREHENSIVE)).ToList());
                    var portsf = F(ports.Select(x => x.Buffer(MISAPPREHENSIVE)).ToList());
                    var fldrs = item.FloorDrains;
                    var vps = item.VerticalPipes;
                    {
                        var _vps = vps.ToHashSet();
                        foreach (var vp in _vps.ToList())
                        {
                            lbdict.TryGetValue(vp, out string lb);
                            if (!IsDraiPipeLabel(lb))
                            {
                                if (kitchenst(vp))
                                {
                                    dps.Add(vp);
                                    _vps.Remove(vp);
                                }
                            }
                        }
                        dps = dps.Distinct().ToList();
                        vps = _vps.ToList();
                    }
                    {
                        var fds = vps.Where(vp2fdptst).ToList();
                        fldrs = fldrs.Concat(fds).Distinct().ToList();
                        vps = vps.Except(fds).ToList();
                    }
                    var fdsf = F(fldrs);
                    var fdst = T(fldrs.Select(x => x.Buffer(THESAURUSPERMUTATION)).ToList());
                    var ppsf = F(vps);
                    var ppst = T(vps);
                    var dpsf = F(dps);
                    var dpst = T(dps);
                    var fls = lbdict.Where(x => IsDraiFL(x.Value)).Select(x => x.Key).ToList();
                    var pls = lbdict.Where(x => IsPL(x.Value)).Select(x => x.Key).ToList();
                    var tls = lbdict.Where(x => IsTL(x.Value)).Select(x => x.Key).ToList();
                    {
                        var vpsf = F(vps);
                        var _fls = vpsf(GeoFac.CreateGeometry((flpts.Select(x => x.ToNTSPoint()))));
                        var _pls = vpsf(GeoFac.CreateGeometry((plpts.Select(x => x.ToNTSPoint()))));
                        {
                            var gf = G(item.LabelLines).ToIPreparedGeometry();
                            fls = fls.Where(x => gf.Intersects(x)).ToList();
                            pls = pls.Where(x => gf.Intersects(x)).ToList();
                        }
                        {
                            fls = fls.Concat(_fls).Distinct().ToList();
                            pls = pls.Concat(_pls).Distinct().ToList();
                        }
                    }
                    if (cleaningPortPtRgs.Count > THESAURUSSTAMPEDE)
                    {
                        foreach (var pp in vps)
                        {
                            if (cleaningPortPtsRgst(pp))
                            {
                                pls.Add(pp);
                                if (!lbdict.ContainsKey(pp))
                                {
                                    lbdict[pp] = THESAURUSBOTTOM + ++tk;
                                }
                            }
                        }
                        pls = pls.Distinct().ToList();
                    }
                    foreach (var seg in item.VLines.SelectMany(x => GeoFac.GetLines(x)).Where(x => x.Length > THESAURUSCOMMUNICATION))
                    {
                        draw(IRRESPONSIBLENESS, seg.ToLineString());
                    }
                    var djPts = new HashSet<Point>();
                    var _linesGroup = new HashSet<HashSet<GLineSegment>>();
                    prq.Enqueue(THESAURUSPERMUTATION, () =>
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
                                    prq.Enqueue(THESAURUSPERMUTATION, () =>
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
                        _linesGroup.Add(lines.ToHashSet());
                        prq.Enqueue(THESAURUSCENSURE, () =>
                        {
                            var flst = T(fls);
                            var plst = T(pls);
                            foreach (var pt in sankakuptsf(dlinesGeo.Buffer(THESAURUSHOUSING)))
                            {
                                var info = (MLeaderInfo)pt.UserData;
                                if (info.Text is THESAURUSDEPLORE)
                                {
                                    if (flst(dlinesGeo) || plst(dlinesGeo))
                                    {
                                        info.Text = IRRESPONSIBLENESS;
                                    }
                                }
                            }
                        });
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
                        prq.Enqueue(INTROPUNITIVENESS, () =>
                        {
                            var dlinesGeoBuf = dlinesGeo.Buffer(THESAURUSACRIMONIOUS);
                            var dlbufgf = dlinesGeoBuf.ToIPreparedGeometry();
                            if (fdst(dlinesGeo) || ppst(dlinesGeo) || dpst(dlinesGeo))
                            {
                                foreach (var line in lines)
                                {
                                    foreach (var dp in dpsf(line.ToLineString()))
                                    {
                                        {
                                            var dn = dnInfectors.FirstOrDefault(kv => dp.Intersects(kv.Key.ToGRect(HYPERDISYLLABLE).ToPolygon())).Value;
                                            if (!string.IsNullOrEmpty(dn))
                                            {
                                                draw(dn, line.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                                                continue;
                                            }
                                        }
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
                                            if (kitchenst(dp))
                                            {
                                                var dn = vm.Params.BasinDN;
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
                            prq.Enqueue(QUOTATIONEDIBLE, () =>
                            {
                                var _pts = lines.SelectMany(seg => new Point2d[] { seg.StartPoint, seg.EndPoint }).GroupBy(x => x).Select(x => x.Key).Distinct().Where(pt => dlbufgf.Intersects(pt.ToNTSPoint())).ToList();
                                var pts = _pts.Select(x => x.ToNTSPoint()).ToList();
                                djPts.AddRange(pts);
                                _linesGroup.Add(lines.ToHashSet());
                                return;
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
                                prq.Enqueue(THESAURUSCOMMUNICATION, () =>
                                {
                                    return;
                                });
                                prq.Enqueue(SUPERLATIVENESS, () =>
                                {
                                    return;
                                    foreach (var pt in pts)
                                    {
                                    }
                                });
                                prq.Enqueue(THESAURUSDESTITUTE, () =>
                                {
                                    return;
                                    if (edPts.Count == THESAURUSHOUSING)
                                    {
                                        var edPt = edPts[THESAURUSSTAMPEDE];
                                        var mdPts = pts.Except(stPts).Except(edPts).ToList();
                                    }
                                    else
                                    {
                                    }
                                });
                            });
                        });
                    }
                    static List<List<Geometry>> GroupGeometries(List<Geometry> lines, List<Geometry> polys)
                    {
                        var geosGroup = new List<List<Geometry>>();
                        GroupGeometries();
                        return geosGroup;
                        void GroupGeometries()
                        {
                            var geos = lines.Concat(polys).Distinct().ToList();
                            if (geos.Count == THESAURUSSTAMPEDE) return;
                            lines = lines.Distinct().ToList();
                            polys = polys.Distinct().ToList();
                            if (lines.Count + polys.Count != geos.Count) throw new ArgumentException();
                            var lineshs = lines.ToHashSet();
                            var polyhs = polys.ToHashSet();
                            var pairs = _GroupGeometriesToKVIndex(geos).Where(kv =>
                            {
                                if (lineshs.Contains(geos[kv.Key]) && lineshs.Contains(geos[kv.Value])) return INTRAVASCULARLY;
                                if (polyhs.Contains(geos[kv.Key]) && polyhs.Contains(geos[kv.Value])) return INTRAVASCULARLY;
                                return THESAURUSOBSTINACY;
                            }).ToArray();
                            var dict = new ListDict<int>();
                            var h = new BFSHelper()
                            {
                                Pairs = pairs,
                                TotalCount = geos.Count,
                                Callback = (g, i) => dict.Add(g.root, i),
                            };
                            h.BFS();
                            dict.ForEach((_i, l) =>
                            {
                                geosGroup.Add(l.Select(i => geos[i]).ToList());
                            });
                        }
                        static IEnumerable<KeyValuePair<int, int>> _GroupGeometriesToKVIndex<T>(List<T> geos) where T : Geometry
                        {
                            if (geos.Count == THESAURUSSTAMPEDE) yield break;
                            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > THESAURUSACRIMONIOUS ? geos.Count : THESAURUSACRIMONIOUS);
                            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
                            for (int i = THESAURUSSTAMPEDE; i < geos.Count; i++)
                            {
                                var geo = geos[i];
                                var gf = GeoFac.PreparedGeometryFactory.Create(geo);
                                foreach (var j in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Select(g => geos.IndexOf(g)).Where(j => i < j))
                                {
                                    yield return new KeyValuePair<int, int>(i, j);
                                }
                            }
                        }
                    }
                    prq.Enqueue(THESAURUSCOMMUNICATION, () =>
                    {
                        var t = GeoFac.CreateIntersectsTester(vpInfectors.Select(x => x.ToNTSPoint()).ToList());
                        var lines = _linesGroup.SelectMany(x => x).Select(x => x.ToLineString()).ToList();
                        var linesf = GeoFac.CreateIntersectsSelector(lines);
                        {
                            foreach (var pp in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count).Select(si => exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2)).SelectMany(x => x)
                            .Where(x => IsDraiFL(x.Value) || IsPL(x.Value) || IsTL(x.Value)).Select(x => x.Key).Concat(vps.Where(t)).Distinct())
                            {
                                var segs = linesf(pp.Buffer(THESAURUSHOUSING)).SelectMany(ls => GeoFac.GetLines(ls)).Distinct().ToList();
                                if (segs.Count == THESAURUSHOUSING)
                                {
                                    var dn = IRRESPONSIBLENESS;
                                    draw(dn, segs[THESAURUSSTAMPEDE].Center.ToGRect(INTROPUNITIVENESS).ToPolygon(), INTRAVASCULARLY, INTRAVASCULARLY, QUOTATIONPELVIC);
                                }
                            }
                        }
                    });
                    prq.Enqueue(SUPERLATIVENESS, () =>
                    {
                        var points = djPts.ToList();
                        var pointsf = GeoFac.CreateIntersectsSelector(points);
                        var linesGeos = _linesGroup.Select(lines => new MultiLineString(lines.Select(x => x.ToLineString()).ToArray())).ToList();
                        var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
                        foreach (var bufGeo in GroupGeometries(_linesGroup.Select(x => GeoFac.CreateGeometry(x.Select(x => x.ToLineString()))).ToList(), vps.Concat(fldrs).Concat(dps).ToList()).Select(GeoFac.CreateGeometry).Select(x => x.Buffer(THESAURUSPERMUTATION)))
                        {
                            if (portst(bufGeo))
                            {
                                prq.Enqueue(THESAURUSDESTITUTE, () =>
                                {
                                    var target = portsf(bufGeo).FirstOrDefault();
                                    if (target is null) return;
                                    var edPts = GeoFac.CreateIntersectsSelector(pointsf(target))(bufGeo);
                                    var edPt = edPts.FirstOrDefault();
                                    if (edPt is null) return;
                                    var _lines = linesGeosk(bufGeo).SelectMany(x => GeoFac.GetLines(x)).Distinct().ToList();
                                    var pts = pointsf(bufGeo);
                                    var ptsf = GeoFac.CreateIntersectsSelector(pts);
                                    var stPts = new HashSet<Point>();
                                    var addPts = new HashSet<Point>();
                                    {
                                        foreach (var c in vps.Concat(fldrs).Concat(dps).Distinct())
                                        {
                                            var _pts = ptsf(c.Buffer(THESAURUSPERMUTATION));
                                            if (_pts.Count == THESAURUSSTAMPEDE) continue;
                                            if (_pts.Count == THESAURUSHOUSING)
                                            {
                                                stPts.Add(_pts[THESAURUSSTAMPEDE]);
                                            }
                                            else
                                            {
                                                var bd = GetBounds(_pts.ToArray());
                                                var center = bd.Center.ToNTSPoint();
                                                addPts.Add(center);
                                                foreach (var seg in _pts.Select(x => new GLineSegment(x.ToPoint2d(), center.ToPoint2d())))
                                                {
                                                    _lines.Add(seg);
                                                    if (fls.Contains(c) || pls.Contains(c))
                                                    {
                                                        draw(IRRESPONSIBLENESS, seg.Center.ToNTSPoint());
                                                        prq.Enqueue(THESAURUSBEATIFIC, () =>
                                                        {
                                                            draw(null, seg.Center.ToNTSPoint());
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                        stPts.Remove(edPt);
                                    }
                                    pts = pts.Concat(addPts).Distinct().ToList();
                                    var mdPts = pts.Except(stPts).Except(edPts).ToHashSet();
                                    foreach (var dp in dpsf(bufGeo).Where(kitchenst))
                                    {
                                    }
                                    var nodes = pts.Select(x => new GraphNode<Point>(x)).ToList();
                                    {
                                        var kvs = new HashSet<KeyValuePair<int, int>>();
                                        foreach (var seg in _lines)
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
                                            if (path.Count == THESAURUSSTAMPEDE) continue;
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
                                                var sel = seg.Buffer(THESAURUSHOUSING);
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
                                                var sel = seg.Buffer(THESAURUSHOUSING);
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
                                            var sel = kv.Key.Buffer(THESAURUSHOUSING);
                                            foreach (var pt in sankakuptsf(sel))
                                            {
                                                var info = (MLeaderInfo)pt.UserData;
                                                info.Text = getDn(kv.Value);
                                            }
                                        }
                                    }
                                });
                            }
                            else
                            {
                                prq.Enqueue(THESAURUSSCARCE, () =>
                                {
                                    var fds = fdsf(bufGeo);
                                    var pps = ppsf(bufGeo);
                                    if (pps.Count == THESAURUSHOUSING && fds.Count == THESAURUSPERMUTATION)
                                    {
                                        var target = pps[THESAURUSSTAMPEDE];
                                        var edPts = GeoFac.CreateIntersectsSelector(pointsf(target))(bufGeo);
                                        var edPt = edPts.FirstOrDefault();
                                        if (edPt is null) return;
                                        var _lines = linesGeosk(bufGeo).SelectMany(x => GeoFac.GetLines(x)).Distinct().ToList();
                                        foreach (var pt in sankakuptsf(bufGeo))
                                        {
                                            var info = (MLeaderInfo)pt.UserData;
                                            if (info.Note is QUOTATIONPELVIC)
                                            {
                                                info.Text = THESAURUSDEPLORE;
                                                info.Note = null;
                                            }
                                        }
                                        prq.Enqueue(THESAURUSACRIMONIOUS, () =>
                                        {
                                            foreach (var pt in sankakuptsf(bufGeo))
                                            {
                                                var info = (MLeaderInfo)pt.UserData;
                                                if (info.Text == IRRESPONSIBLENESS)
                                                {
                                                    info.Text = QUOTATIONDOPPLER;
                                                }
                                            }
                                        });
                                        var pts = pointsf(bufGeo);
                                        var ptsf = GeoFac.CreateIntersectsSelector(pts);
                                        var stPts = new HashSet<Point>();
                                        foreach (var c in fldrs)
                                        {
                                            var _pts = ptsf(c.Buffer(THESAURUSPERMUTATION));
                                            if (_pts.Count == THESAURUSSTAMPEDE) continue;
                                            if (_pts.Count == THESAURUSHOUSING)
                                            {
                                                stPts.Add(_pts[THESAURUSSTAMPEDE]);
                                            }
                                        }
                                        var mdPts = pts.Except(stPts).Except(edPts).ToHashSet();
                                        var nodes = pts.Select(x => new GraphNode<Point>(x)).ToList();
                                        {
                                            var kvs = new HashSet<KeyValuePair<int, int>>();
                                            foreach (var seg in _lines)
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
                                                var sel = kv.Key.Buffer(THESAURUSHOUSING);
                                                foreach (var pt in sankakuptsf(sel))
                                                {
                                                    var info = (MLeaderInfo)pt.UserData;
                                                    info.Text = getDn(kv.Value);
                                                }
                                            }
                                        }
                                    }
                                });
                            }
                        }
                        prq.Enqueue(ECCLESIASTICISM, () =>
                        {
                            foreach (var _segs in _linesGroup)
                            {
                                var segs = _segs.Where(x => x.Length >= THESAURUSINCOMPLETE).ToList();
                                if (segs.Count == THESAURUSSTAMPEDE) continue;
                                var geo = GeoFac.CreateGeometry(segs.Select(x => x.ToLineString()));
                                var buf = geo.Buffer(THESAURUSPERMUTATION);
                                void changeDn()
                                {
                                    foreach (var cleaningPort in geoData.CleaningPorts)
                                    {
                                        if (buf.Intersects(cleaningPort))
                                        {
                                            var dn = IRRESPONSIBLENESS;
                                            if (kitchenst(buf)) dn = vm.Params.BasinDN;
                                            var pts = sankakuptsf(buf);
                                            foreach (var pt in pts.Where(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE))
                                            {
                                                ((MLeaderInfo)pt.UserData).Text = dn;
                                            }
                                        }
                                    }
                                    if (segs.Count == THESAURUSPERMUTATION)
                                    {
                                        var pts = sankakuptsf(buf);
                                        if (pts.Count(x => ((MLeaderInfo)x.UserData).Text is IRRESPONSIBLENESS) == THESAURUSHOUSING && pts.Count(x => ((MLeaderInfo)x.UserData).Text is QUOTATIONBREWSTER or QUOTATIONDOPPLER) == THESAURUSHOUSING)
                                        {
                                            foreach (var pt in pts.Where(x => ((MLeaderInfo)x.UserData).Text is IRRESPONSIBLENESS))
                                            {
                                                ((MLeaderInfo)pt.UserData).Text = (pts.Where(x => ((MLeaderInfo)x.UserData).Text is QUOTATIONBREWSTER or QUOTATIONDOPPLER).First().UserData as MLeaderInfo).Text;
                                            }
                                        }
                                        else if (pts.Count(x => ((MLeaderInfo)x.UserData).Text is QUOTATIONBREWSTER) == THESAURUSHOUSING && pts.Count(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE) == THESAURUSHOUSING)
                                        {
                                            foreach (var pt in pts.Where(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE))
                                            {
                                                ((MLeaderInfo)pt.UserData).Text = QUOTATIONBREWSTER;
                                            }
                                        }
                                        else if (pts.Count(x => ((MLeaderInfo)x.UserData).Text is QUOTATIONDOPPLER) == THESAURUSHOUSING && pts.Count(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE) == THESAURUSHOUSING)
                                        {
                                            foreach (var pt in pts.Where(x => ((MLeaderInfo)x.UserData).Text is THESAURUSDEPLORE))
                                            {
                                                ((MLeaderInfo)pt.UserData).Text = QUOTATIONDOPPLER;
                                            }
                                        }
                                        return;
                                    }
                                }
                                changeDn();
                            }
                        });
                        prq.Enqueue(ACANTHOCEPHALANS, () =>
                        {
                            foreach (var line in _linesGroup.SelectMany(x => x).Where(x => x.Length < THESAURUSINCOMPLETE))
                            {
                                var pts = sankakuptsf(line.Buffer(ASSOCIATIONISTS));
                                if (pts.Count == THESAURUSHOUSING)
                                {
                                    var info = (MLeaderInfo)pts[THESAURUSSTAMPEDE].UserData;
                                    if (info.Text == THESAURUSDEPLORE)
                                    {
                                        info.Text = null;
                                    }
                                }
                            }
                        });
                        prq.Enqueue(THESAURUSOCCASIONALLY, () =>
                        {
                            var linesGeos = _linesGroup.Select(lines => new MultiLineString(lines.Select(x => x.ToLineString()).ToArray())).ToList();
                            var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
                            foreach (var lines in _linesGroup)
                            {
                                var kvs = new List<KeyValuePair<GLineSegment, MLeaderInfo>>();
                                foreach (var line in lines.Where(x => x.Length > QUOTATIONLUCANIAN))
                                {
                                    var pts = sankakuptsf(line.Buffer(THESAURUSPERMUTATION));
                                    if (pts.Count == THESAURUSHOUSING)
                                    {
                                        var pt = pts[THESAURUSSTAMPEDE];
                                        var info = (MLeaderInfo)pt.UserData;
                                        kvs.Add(new KeyValuePair<GLineSegment, MLeaderInfo>(line, info));
                                    }
                                }
                                {
                                    var segs = kvs.Select(kv => kv.Key).Distinct().ToList();
                                    var lns = segs.Select(x => x.ToLineString()).ToList();
                                    var vertexs = segs.YieldPoints().Distinct().ToList();
                                    var lnsf = GeoFac.CreateIntersectsSelector(lns);
                                    var lnscf = GeoFac.CreateContainsSelector(segs.Select(x => x.Extend(-THESAURUSPERMUTATION).ToLineString()).ToList());
                                    var opts = new List<Ref<RegexOptions>>();
                                    foreach (var vertex in vertexs)
                                    {
                                        var lst = lnsf(GeoFac.CreateCirclePolygon(vertex, THESAURUSCOMMUNICATION, SUPERLATIVENESS));
                                        RegexOptions opt;
                                        if (lst.Count == THESAURUSHOUSING)
                                        {
                                            opt = RegexOptions.IgnoreCase;
                                        }
                                        else if (lst.Count == THESAURUSPERMUTATION)
                                        {
                                            opt = RegexOptions.Multiline;
                                        }
                                        else if (lst.Count > THESAURUSPERMUTATION)
                                        {
                                            opt = RegexOptions.ExplicitCapture;
                                        }
                                        else
                                        {
                                            opt = RegexOptions.None;
                                        }
                                        opts.Add(new Ref<RegexOptions>(opt));
                                    }
                                    foreach (var geo in GeoFac.GroupLinesByConnPoints(GeoFac.GetLines(GeoFac.CreateGeometry(lns).Difference(GeoFac.CreateGeometryEx(opts.Where(x => x.Value == RegexOptions.ExplicitCapture).Select(opts).ToList(vertexs).Select(x => x.ToGCircle(THESAURUSPERMUTATION).ToCirclePolygon(SUPERLATIVENESS)).ToList()))).Select(x => x.ToLineString()).ToList(), UNCONSEQUENTIAL))
                                    {
                                        var bf = geo.Buffer(THESAURUSPERMUTATION);
                                        var pts = sankakuptsf(bf);
                                        var infos = pts.Select(pt => (MLeaderInfo)pt.UserData).ToList();
                                        if (infos.Select(x => x.Text).Distinct().Count() == THESAURUSHOUSING)
                                        {
                                            var text = infos[THESAURUSSTAMPEDE];
                                            var _segs = lnscf(bf).SelectMany(x => GeoFac.GetLines(x)).ToList();
                                            if (_segs.Count > THESAURUSHOUSING)
                                            {
                                                const double LEN = PHOTOCONDUCTION;
                                                if (_segs.Any(x => x.Length >= LEN))
                                                {
                                                    foreach (var seg in _segs)
                                                    {
                                                        if (seg.Length < LEN)
                                                        {
                                                            draw(null, seg.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var max = _segs.Max(x => x.Length);
                                                    foreach (var seg in _segs)
                                                    {
                                                        if (seg.Length != max)
                                                        {
                                                            draw(null, seg.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    });
                    prq.Enqueue(THESAURUSOCCASIONALLY, () =>
                    {
                        var points = precisePts.Select(x => x.ToNTSPoint()).ToList();
                        var pointsf = GeoFac.CreateIntersectsSelector(points);
                        foreach (var info in mlInfos)
                        {
                            if (info.Text is not null)
                            {
                                var pts = pointsf(info.BasePoint.ToGRect(THESAURUSHOUSING).ToPolygon());
                                var pt = pts.FirstOrDefault();
                                if (pts.Count > THESAURUSHOUSING)
                                {
                                    pt = GeoFac.NearestNeighbourGeometryF(pts)(info.BasePoint.ToNTSPoint());
                                }
                                if (pt is not null)
                                {
                                    info.BasePoint = pt.ToPoint3d();
                                }
                            }
                        }
                    });
                }
                prq.Enqueue(THESAURUSACRIMONIOUS, () =>
                {
                    foreach (var info in mlInfos)
                    {
                        if (info.Text == PHOTOFLUOROGRAM) info.Text = THESAURUSDEPLORE;
                    }
                });
            }
            foreach (var info in mlInfos)
            {
                if (info.Text == THESAURUSDEPLORE) info.Text = THESAURUSIMPETUOUS;
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
    public class PriorityQueue : IDisposable
    {
        Queue<Action>[] queues;
        public PriorityQueue(int queuesCount)
        {
            queues = new Queue<Action>[queuesCount];
            for (int i = THESAURUSSTAMPEDE; i < queuesCount; i++)
            {
                queues[i] = new Queue<Action>();
            }
        }
        public void Dispose()
        {
            Execute();
        }
        public void Enqueue(int priority, Action f)
        {
            queues[priority].Enqueue(f);
        }
        public void Execute()
        {
            while (queues.Any(queue => queue.Count > THESAURUSSTAMPEDE))
            {
                foreach (var queue in queues)
                {
                    if (queue.Count > THESAURUSSTAMPEDE)
                    {
                        queue.Dequeue()();
                        break;
                    }
                }
            }
        }
    }
    public enum PipeType
    {
        Unknown, Y1L, Y2L, NL, YL, FL0
    }
}