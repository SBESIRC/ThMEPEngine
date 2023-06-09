namespace ThMEPWSS.FireProtectionSystemNs
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
    public static class ThFireControlSystemCmd
    {
        public static void ExecuteTH(FireControlSystemDiagramViewModel vm)
        {
            ThMEPCommandService.Execute(() => Execute(vm), "THXHSXTT", "生成");
        }
        public static void Execute(FireControlSystemDiagramViewModel vm)
        {
            const double TEXT_HEIGHT = 350;
            const double AIR_VALVE_SCALE = 12.7 / 25.4;
            {
                var _vm = vm.SetHighlevelNozzleAndSemiPlatformNozzleParams;
                var items = _vm.Items.ToList();
                _vm.Items.Clear();
                for (int i = 1; i <= FireControlSystemDiagramViewModel.Singleton.CountsGeneral; i++)
                {
                    _vm.Items.Add(items.FirstOrDefault(x => x.PipeId == i) ?? new SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.Item() { PipeId = i, HasFireHydrant = true, PipeConnectionType = "低位", IsHalfPlatform = false });
                }
            }
            {
                vm.Serialnumber = (vm.Serialnumber ?? "").Trim();
                if (vm.Serialnumber == "0") vm.Serialnumber = "";
            }
            var _generalCount = vm.CountsGeneral;
            var _refugeCount = vm.CountsRefuge;
            if (_generalCount <= 0) return;
            if (_generalCount == 1)
            {
                _generalCount++;
                _refugeCount++;
            }
            FocusMainWindow();
            var opt = new PromptPointOptions("\n请选择消火栓系统图排布的起点");
            var result = Active.Editor.GetPoint(opt);
            if (result.Status != PromptStatus.OK) return;
            var basePoint = result.Value;
            {
                if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            }
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, true))
            {
                LayerThreeAxes(new List<string>() { "W-NOTE", "W-WSUP-NOTE", "W-FRPT-HYDT-PIPE", "W-FRPT-NOTE", "W-FRPT-HYDT-DIMS", "W-FRPT-HYDT-EQPM", "W-FRPT-HYDT" });
                var textInfos = new List<DBTextInfo>();
                var brInfos = new List<BlockInfo>();
                var lineInfos = new List<LineInfo>();
                var floorGroupData = InputDataConvert.SplitFloor(vm);
                var floorDatas = InputDataConvert.FloorDataModels(floorGroupData);
                const double fireOffsetY = 400.0;
                const double fireHeight = 700.0;
                var HEIGHT = vm.FaucetFloor;
                var OFFSET_X1 = 5000.0;
                var OFFSET_X2 = 1000.0;
                var SPAN_X = 2000.0;
                if (vm.SetHighlevelNozzleAndSemiPlatformNozzleParams.Items.Any(x => x.IsHalfPlatform))
                {
                    SPAN_X += 500;
                }
                else
                {
                    SPAN_X += 100;
                }
                var allStoreys = floorDatas.Select(x => $"{x.FloorNum}F").ToList();
                var maxNumStorey = floorDatas.Select(x => x.FloorNum).Max() + "F";
                var hasFullHalfPlatformLine = false;
                var refugeFloors = floorDatas.Where(x => x.IsRefugeFloor).Select(x => $"{x.FloorNum}F").ToList();
                if (refugeFloors.Count == 0) _refugeCount = _generalCount;
                var floorRanges = vm.ZoneConfigs.Where(x => x.IsEffective()).Select(x => new ValueTuple<int?, int?>(x.GetIntStartFloor(), x.GetIntEndFloor()))
                    .Where(x => x.Item1.HasValue && x.Item2.HasValue).Select(x => new ValueTuple<int, int>(x.Item1.Value, x.Item2.Value))
                    .OrderBy(x => x.Item1).ToList();
                var ringPairFloors = new List<ValueTuple<string, string>>();
                var ringPairInts = new List<ValueTuple<int, int>>();
                for (int i = 0; i < floorRanges.Count - 1; i++)
                {
                    if (floorRanges[i + 1].Item1 - floorRanges[i].Item2 == 1)
                    {
                        var v1 = floorRanges[i + 1].Item1 + "F";
                        var v2 = floorRanges[i].Item2 + "F";
                        if (!refugeFloors.Contains(v1) && !refugeFloors.Contains(v2))
                        {
                            ringPairFloors.Add(new(v2, v1));
                            ringPairInts.Add(new(floorRanges[i].Item2, floorRanges[i + 1].Item1));
                        }
                    }
                }
                var ringInts = ringPairInts.SelectMany(x => new int[] { x.Item1, x.Item2 }).OrderBy(x => x).ToList();
                var ringStoreys = ringInts.Select(x => x + "F").ToList();
                var ringsCount = refugeFloors.Count + ringPairFloors.Count;
                const double width_HandPumpConnection = 2500.0;
                bool canHaveHandPumpConnection(string storey)
                {
                    return vm.HaveHandPumpConnection && GetStoreyScore(storey) >= 30 && refugeFloors.Contains(storey);
                }
                var handConnFloors = allStoreys.Where(canHaveHandPumpConnection).ToList();
                var width_HaveHandPumpConnection = handConnFloors.Count * width_HandPumpConnection;
                var vlines = new List<LineString>(4096);
                double getHandConnWidth(int i)
                {
                    var _tmp = allStoreys.ToList();
                    _tmp.Reverse();
                    var sum = .0;
                    foreach (var s in _tmp)
                    {
                        if (handConnFloors.Contains(s))
                        {
                            sum += width_HandPumpConnection;
                        }
                        if (s == allStoreys[i]) return sum;
                    }
                    return .0;
                }
                allStoreys.Add("RF");
                var iRF = allStoreys.IndexOf("RF");
                Point3d getStoreyBsPt(int i) => basePoint.OffsetY(HEIGHT * i);
                Point3d getGeneralBsPt(int i, int j)
                {
                    var dx = ringsCount * SPAN_X;
                    dx += width_HaveHandPumpConnection;
                    var dc = _refugeCount - _generalCount;
                    if (dc > 0)
                    {
                        if (j >= _generalCount / 2)
                        {
                            dx += dc * SPAN_X;
                        }
                    }
                    return basePoint.OffsetXY(j * SPAN_X + OFFSET_X1 + dx, HEIGHT * i);
                }
                Point3d getRefugeBsPt(int i, int j)
                {
                    var dx = (ringsCount + _generalCount / 2) * SPAN_X;
                    dx += width_HaveHandPumpConnection;
                    return basePoint.OffsetXY(j * SPAN_X + OFFSET_X1 + dx, HEIGHT * i);
                }
                {
                    {
                        var heights = new List<int>(allStoreys.Count);
                        var total = 0;
                        var _vm = FloorHeightsViewModel.Instance;
                        static bool test(string x, int t)
                        {
                            var m = Regex.Match(x, @"^(\-?\d+)\-(\-?\d+)$");
                            if (m.Success)
                            {
                                if (int.TryParse(m.Groups[1].Value, out int v1) && int.TryParse(m.Groups[2].Value, out int v2))
                                {
                                    var min = Math.Min(v1, v2);
                                    var max = Math.Max(v1, v2);
                                    for (int i = min; i <= max; i++)
                                    {
                                        if (i == t) return true;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            m = Regex.Match(x, @"^\-?\d+$");
                            if (m.Success)
                            {
                                if (int.TryParse(x, out int v))
                                {
                                    if (v == t) return true;
                                }
                            }
                            return false;
                        }
                        for (int i = 0; i < allStoreys.Count; i++)
                        {
                            heights.Add(total);
                            var v = _vm.GeneralFloor;
                            if (_vm.ExistsSpecialFloor) v = _vm.Items.FirstOrDefault(m => test(m.Floor, GetStoreyScore(allStoreys[i])))?.Height ?? v;
                            total += v;
                        }
                        var lineLen = OFFSET_X1 + width_HaveHandPumpConnection + (_refugeCount + ringsCount * 2) * SPAN_X + OFFSET_X2;
                        var fs = new List<Tuple<int, Action>>();
                        foreach (var i in Enumerable.Range(0, allStoreys.Count))
                        {
                            var storey = allStoreys[i];
                            string getStoreyHeightText()
                            {
                                if (storey is "1F") return "±0.00";
                                var ret = (heights[i] / 1000.0).ToString("0.00"); ;
                                if (ret == "0.00") return "±0.00";
                                return ret;
                            }
                            var bsPt1 = getStoreyBsPt(i);
                            void DrawStoreyLine(string label, Point3d basePt, double lineLen, string text)
                            {
                                var px = basePt;
                                brInfos.Add(new BlockInfo("标高", "W-NOTE", px.OffsetXY(550, 0)) { PropDict = new Dictionary<string, string>() { { "标高", text } } });
                                textInfos.Add(new DBTextInfo(px.OffsetXY(2000, 130), label, "W-NOTE", "TH-STYLE3"));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(lineLen, 0)), "W-NOTE"));
                            }
                            fs.Add(new Tuple<int, Action>(i, () => { DrawStoreyLine(storey, bsPt1, lineLen, getStoreyHeightText()); }));
                        }
                        var pr = vm.SetHighlevelNozzleAndSemiPlatformNozzleParams;
                        {
                            int c1 = 0, c2 = 0;
                            for (int _j = 0; _j < _generalCount; _j++)
                            {
                                var j = getRealJ(_j);
                                var arg = pr.Items.First(x => x.PipeId - 1 == j);
                                if (!arg.HasFireHydrant) continue;
                                c2++;
                                if (arg.IsHalfPlatform)
                                {
                                    c1++;
                                }
                            }
                            if (c1 > 0 && c1 == c2)
                            {
                                hasFullHalfPlatformLine = true;
                            }
                        }
                        if (hasFullHalfPlatformLine)
                        {
                            for (int i = 0; i < iRF; i++)
                            {
                                var px = getStoreyBsPt(i).OffsetY(HEIGHT / 2);
                                brInfos.Add(new BlockInfo("标高", "W-NOTE", px.OffsetXY(550, 0)) { PropDict = new Dictionary<string, string>() { { "标高", "xxx" } } });
                                textInfos.Add(new DBTextInfo(px.OffsetXY(2000, 130), (i + 1) + "F半平台", "W-NOTE", "TH-STYLE3"));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(lineLen, 0)), "W-NOTE"));
                            }
                            foreach (var item in fs)
                            {
                                if (item.Item1 == 0 || item.Item1 == iRF)
                                {
                                    item.Item2();
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in fs)
                            {
                                item.Item2();
                            }
                        }
                    }
                    void drawDomePipe(GLineSegment seg)
                    {
                        if (seg.IsValid)
                        {
                            lineInfos.Add(new LineInfo(seg, "W-FRPT-HYDT-PIPE"));
                        }
                    }
                    void drawDomePipes(IEnumerable<GLineSegment> segs)
                    {
                        foreach (var seg in segs)
                        {
                            drawDomePipe(seg);
                        }
                    }
                    (double, double) getValveSize() => (240.0, 90.0);
                    GRect drawValve(Point3d p)
                    {
                        var bi = new BlockInfo("蝶阀", "W-FRPT-HYDT-EQPM", p);
                        bi.DynaDict["可见性1"] = "蝶阀";
                        brInfos.Add(bi);
                        var (w, h) = getValveSize();
                        var bd = new GRect(p.OffsetY(-h / 2), p.OffsetXY(w, h / 2));
                        return bd;
                    }
                    void drawFire(Point3d p)
                    {
                        var bi = new BlockInfo("室内消火栓系统1", "W-FRPT-HYDT", p);
                        bi.DynaDict["可见性"] = vm.ComBoxFireTypeSelectItem;
                        brInfos.Add(bi);
                    }
                    string getPipeConnectionType(int i, int j)
                    {
                        var pr = vm.SetHighlevelNozzleAndSemiPlatformNozzleParams;
                        var arg = pr.Items.First(x => x.PipeId - 1 == getRealJ(j));
                        return arg.PipeConnectionType;
                    }
                    int getRealJ(int j)
                    {
                        if (vm.CountsGeneral == 1) return 0;
                        return j;
                    }
                    bool getHasHalfPlatform(int i, int j)
                    {
                        if (i == iRF) return false;
                        if (vm.CountsGeneral == 1 && j != 0) return false;
                        var pr = vm.SetHighlevelNozzleAndSemiPlatformNozzleParams;
                        var arg = pr.Items.First(x => x.PipeId - 1 == getRealJ(j));
                        if (!arg.HasFireHydrant) return false;
                        var hasHalfPlatform = arg.IsHalfPlatform;
                        if (!hasHalfPlatform) return false;
                        if (i == 0)
                        {
                            if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.YesNo)
                            {
                                return true;
                            }
                            else if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.NoYes)
                            {
                                return false;
                            }
                            else if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.NoNo)
                            {
                                return false;
                            }
                            else if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.YesYes)
                            {
                                return true;
                            }
                        }
                        else if (i == iRF - 1)
                        {
                            if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.YesNo)
                            {
                                return false;
                            }
                            else if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.NoYes)
                            {
                                return true;
                            }
                            else if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.NoNo)
                            {
                                return false;
                            }
                            else if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.YesYes)
                            {
                                return true;
                            }
                        }
                        return true;
                    }
                    bool getHasExtraFire(int i, int j)
                    {
                        if (vm.CountsGeneral == 1 && j != 0) return false;
                        var pr = vm.SetHighlevelNozzleAndSemiPlatformNozzleParams;
                        var arg = pr.Items.First(x => x.PipeId - 1 == getRealJ(j));
                        return i == 0 && arg.HasFireHydrant && arg.IsHalfPlatform;
                    }
                    bool getHasOrdinalFire(int i, int j)
                    {
                        if (vm.CountsGeneral == 1 && j != 0) return false;
                        var pr = vm.SetHighlevelNozzleAndSemiPlatformNozzleParams;
                        var arg = pr.Items.First(x => x.PipeId - 1 == getRealJ(j));
                        if (arg.HasFireHydrant)
                        {
                            if (arg.IsHalfPlatform)
                            {
                                return getHasHalfPlatform(i, j);
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    for (int i = 0; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        if (IsNumStorey(storey))
                        {
                            for (int j = 0; j < _generalCount; j++)
                            {
                                var bsPt = getGeneralBsPt(i, j);
                                vlines.Add(new GLineSegment(bsPt, bsPt.OffsetY(HEIGHT)).ToLineString());
                            }
                        }
                    }
                    {
                        var p7s = new List<Point3d>();
                        for (int j = 0; j < _generalCount; j++)
                        {
                            var bsPt = getGeneralBsPt(0, j);
                            var p5 = bsPt;
                            var p6 = p5.OffsetY(-300);
                            drawDomePipe(new GLineSegment(p5, p6));
                            brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p6) { Rotate = Math.PI / 2 });
                            var p7 = p6.OffsetXY(0, -600);
                            lineInfos.Add(new LineInfo(new GLineSegment(p6, p7), "W-FRPT-NOTE"));
                            p7s.Add(p7);
                        }
                        var seg = new GLineSegment(p7s[0], p7s.Last());
                        lineInfos.Add(new LineInfo(seg, "W-FRPT-NOTE"));
                        var text = "接自地库低区消火栓环管（入口压力X.XXMPa）";
                        var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                        textInfos.Add(new DBTextInfo(seg.StartPoint.OffsetXY((seg.Length - w) / 2, -h - 100).ToPoint3d(), text, "W-FRPT-NOTE", "TH-STYLE3"));
                    }
                    var basementInfos = new List<Tuple<int, Point3d, Point3d>>();
                    {
                        var airValvePlaceList = new List<Tuple<GRect, Ref<Point3d>, List<Point3d>, Action<Point3d>>>();
                        var obstacles = new List<Geometry>();
                        var vsels = new List<Point3d>();
                        var vkills = new List<Point3d>();
                        var vdrills = new List<GRect>();
                        {
                            var labelList = new List<Tuple<int, int, int, Action>>();
                            var dnList = new List<Tuple<int, int, int, Action>>();
                            var xList = new List<Tuple<int, Action>>();
                            {
                                int k = 1;
                                foreach (var i in Enumerable.Range(0, allStoreys.Count))
                                {
                                    var storey = allStoreys[i];
                                    if (refugeFloors.Contains(storey))
                                    {
                                        ++k;
                                        continue;
                                    }
                                    if (ringPairFloors.Any(x => x.Item2 == storey))
                                    {
                                        ++k;
                                    }
                                    if (IsNumStorey(storey))
                                    {
                                        foreach (var j in Enumerable.Range(0, _generalCount))
                                        {
                                            var bsPt = getGeneralBsPt(i, j);
                                            {
                                                var text = "X" + k;
                                                var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                                var dy = .0;
                                                if (hasFullHalfPlatformLine)
                                                {
                                                    dy += HEIGHT + (HEIGHT - w) / 2;
                                                }
                                                else
                                                {
                                                    dy += (HEIGHT - w) / 2;
                                                    if (getHasHalfPlatform(i, j))
                                                    {
                                                        dy = (HEIGHT / 2 - w) / 2;
                                                        if (getPipeConnectionType(i, j) == "高位-板下")
                                                        {
                                                            dy = (HEIGHT / 2 - 300 - TEXT_HEIGHT - w) / 2;
                                                        }
                                                    }
                                                    if (getHasHalfPlatform(i, j) && HEIGHT < 2300 && (getPipeConnectionType(i, j) == "低位"))
                                                    {
                                                        dy = (HEIGHT / 2 - 400 - w) / 2;
                                                    }
                                                }
                                                var p = bsPt.OffsetXY(h / 2, dy);
                                                if (hasFullHalfPlatformLine) p = p.OffsetY(-HEIGHT / 2);
                                                xList.Add(new Tuple<int, Action>(i, () =>
                                                {
                                                    textInfos.Add(new DBTextInfo(p, text, "W-FRPT-NOTE", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    vdrills.Add(new GRect(p, p.OffsetXY(-h, w)));
                                                }));
                                            }
                                            if (i < iRF - 1)
                                            {
                                                var hasHalfPlatform = getHasHalfPlatform(i, j);
                                                var hasExtraFire = getHasExtraFire(i, j);
                                                var connType = getPipeConnectionType(i, j);
                                                {
                                                    var _k = k;
                                                    var _j = getRealJ(j);
                                                    var isLeft = _refugeCount != _generalCount && (j + 1 > _generalCount / 2);
                                                    labelList.Add(new Tuple<int, int, int, Action>(i, k, j, () =>
                                                    {
                                                        var text = $"X{_k}L{vm.Serialnumber}-{_j + 1}";
                                                        var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                                        var vecs = new List<Vector2d> { new Vector2d(0, -600), new Vector2d(150, 150), new Vector2d(w, 0) };
                                                        var beLeft = !isLeft;
                                                        if (beLeft) vecs = vecs.GetYAxisMirror();
                                                        var segs = vecs.ToGLineSegments(bsPt.OffsetY(HEIGHT + (hasFullHalfPlatformLine ? HEIGHT / 2 : 0))).Skip(1).ToList();
                                                        textInfos.Add(new DBTextInfo((beLeft ? segs[1].EndPoint : segs[1].StartPoint).OffsetY(50).ToPoint3d(), text, "W-FRPT-NOTE", "TH-STYLE3"));
                                                        foreach (var seg in segs)
                                                        {
                                                            lineInfos.Add(new LineInfo(seg, "W-FRPT-NOTE"));
                                                        }
                                                    }));
                                                    dnList.Add(new Tuple<int, int, int, Action>(i, k, j, () =>
                                                    {
                                                        var dn = vm.ZoneConfigs.Where(x => x.ZoneID == _k && x.IsEffective()).FirstOrDefault()?.DNSelectItem;
                                                        if (dn != null)
                                                        {
                                                            var (w, h) = GetDBTextSize(dn, 350, .7, "TH-STYLE3");
                                                            var dy = .0;
                                                            var beRight = isLeft;
                                                            if (hasFullHalfPlatformLine)
                                                            {
                                                                dy += (HEIGHT - w) / 2 + HEIGHT / 2;
                                                            }
                                                            else
                                                            {
                                                                dy += (HEIGHT - w) / 2;
                                                                if (hasHalfPlatform)
                                                                {
                                                                    dy = (HEIGHT / 2 - w) / 2;
                                                                }
                                                                if (hasHalfPlatform && hasExtraFire)
                                                                {
                                                                    dy += HEIGHT / 2;
                                                                }
                                                            }
                                                            var p = beRight ? bsPt.OffsetXY(h + 100, dy) : bsPt.OffsetXY(-50, dy);
                                                            textInfos.Add(new DBTextInfo(p, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                        }
                                                    }));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            var nodraws = new List<int>();
                            var todoX = new HashSet<int>();
                            foreach (var g in dnList.GroupBy(x => x.Item2))
                            {
                                var targets = new HashSet<int>();
                                var _lst = g.Select(x => x.Item1).Distinct().ToList();
                                var lst = _lst.Except(nodraws).Except(ringPairInts.Select(x => x.Item1)).ToList();
                                lst.Sort();
                                if (lst.Count > 0)
                                {
                                    var target = lst.First();
                                    targets.Add(target);
                                    if (lst.Count >= 7)
                                    {
                                        targets.Add(lst.Last() - 2);
                                    }
                                }
                                else
                                {
                                    var target = _lst.First();
                                    targets.Add(target);
                                }
                                foreach (var tg in targets)
                                {
                                    foreach (var m in g)
                                    {
                                        if (m.Item1 == tg)
                                        {
                                            m.Item4();
                                            nodraws.Add(tg);
                                        }
                                    }
                                }
                            }
                            foreach (var g in labelList.GroupBy(x => x.Item2))
                            {
                                var targets = new HashSet<int>();
                                var _lst = g.Select(x => x.Item1).Distinct().ToList();
                                var lst = _lst.Except(nodraws).Except(ringPairInts.Select(x => x.Item1)).ToList();
                                lst.Sort();
                                if (lst.Count > 0)
                                {
                                    var target = lst.First();
                                    if (target == 1 && !nodraws.Contains(target + 1) && _lst.Contains(target + 1))
                                    {
                                        target = 2;
                                    }
                                    targets.Add(target);
                                    if (lst.Count >= 6)
                                    {
                                        targets.Add(lst.Last() - 1);
                                    }
                                }
                                else
                                {
                                    var target = _lst.First();
                                    targets.Add(target);
                                }
                                foreach (var tg in targets)
                                {
                                    todoX.Add(tg);
                                    foreach (var m in g)
                                    {
                                        if (m.Item1 == tg)
                                        {
                                            m.Item4();
                                            nodraws.Add(tg);
                                        }
                                    }
                                }
                            }
                            todoX.Join(xList, x => x, x => x.Item1, (x, y) => { y.Item2(); return 666; }).Count();
                        }
                        {
                            int k = 0;
                            for (int i = 0; i < allStoreys.Count; i++)
                            {
                                var storey = allStoreys[i];
                                if (refugeFloors.Contains(storey))
                                {
                                    var p0 = getStoreyBsPt(i);
                                    var h1 = HEIGHT - 550.0;
                                    var h2 = h1 - 250.0;
                                    var p1 = p0.OffsetXY(OFFSET_X1 + (ringsCount - k - 1) * SPAN_X + getHandConnWidth(i), h1);
                                    var p3 = p0.OffsetXY(OFFSET_X1 + (_refugeCount + k + ringsCount) * SPAN_X + width_HaveHandPumpConnection, h1);
                                    var p2 = new Point2d(p1.X, basePoint.Y).ToPoint3d();
                                    var p4 = new Point2d(p3.X, basePoint.Y).ToPoint3d();
                                    {
                                        var text = "X" + (k + 2);
                                        var y = getStoreyBsPt(2).Y;
                                        var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                        var p = (new Point2d(p1.X, y).ToPoint3d()).OffsetXY(h / 2, (HEIGHT - w) / 2);
                                        if (hasFullHalfPlatformLine) p = p.OffsetY(-HEIGHT / 2);
                                        textInfos.Add(new DBTextInfo(p, text, "W-FRPT-NOTE", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                        vdrills.Add(new GRect(p, p.OffsetXY(-h, w)));
                                    }
                                    if (allStoreys.Contains("3F"))
                                    {
                                        void _drawLabel(double x, string pipeId, bool beLeft)
                                        {
                                            var _k = k;
                                            var text = $"X{_k + 2}L{vm.Serialnumber}-{pipeId}";
                                            var y = getStoreyBsPt(2).Y;
                                            var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                            var vecs = new List<Vector2d> { new Vector2d(0, -600), new Vector2d(150, 150), new Vector2d(w, 0) };
                                            if (beLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(new Point2d(x, y).OffsetY(HEIGHT + (hasFullHalfPlatformLine ? -HEIGHT / 2 : 0))).Skip(1).ToList();
                                            textInfos.Add(new DBTextInfo((beLeft ? segs[1].EndPoint : segs[1].StartPoint).OffsetY(50).ToPoint3d(), text, "W-FRPT-NOTE", "TH-STYLE3"));
                                            foreach (var seg in segs)
                                            {
                                                lineInfos.Add(new LineInfo(seg, "W-FRPT-NOTE"));
                                            }
                                        }
                                        _drawLabel(p1.X, "A", true);
                                        _drawLabel(p3.X, "B", false);
                                    }
                                    var zid = k + 2;
                                    {
                                        var text = "X" + zid;
                                        var y = getStoreyBsPt(2).Y;
                                        var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                        var p = (new Point2d(p3.X, y).ToPoint3d()).OffsetXY(h / 2, (HEIGHT - w) / 2);
                                        if (hasFullHalfPlatformLine) p = p.OffsetY(-HEIGHT / 2);
                                        textInfos.Add(new DBTextInfo(p, text, "W-FRPT-NOTE", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                        vdrills.Add(new GRect(p, p.OffsetXY(-h, w)));
                                    }
                                    {
                                        Point3d p7, p8;
                                        {
                                            var p5 = new Point2d(p1.X, getStoreyBsPt(0).Y);
                                            var p6 = p5.OffsetY(-300);
                                            drawDomePipe(new GLineSegment(p5, p6));
                                            brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p6.ToPoint3d()) { Rotate = Math.PI / 2 });
                                            p7 = p6.ToPoint3d();
                                        }
                                        {
                                            var p5 = new Point2d(p3.X, getStoreyBsPt(0).Y);
                                            var p6 = p5.OffsetY(-300);
                                            drawDomePipe(new GLineSegment(p5, p6));
                                            brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p6.ToPoint3d()) { Rotate = Math.PI / 2 });
                                            p8 = p6.ToPoint3d();
                                        }
                                        basementInfos.Add(new Tuple<int, Point3d, Point3d>(zid, p7, p8));
                                    }
                                    {
                                        var dn = vm.ZoneConfigs.Where(x => x.ZoneID == zid).FirstOrDefault()?.DNSelectItem;
                                        if (dn != null)
                                        {
                                            var (w, h) = GetDBTextSize(dn, 350, .7, "TH-STYLE3");
                                            var dx = (SPAN_X * (k + 1) - w) / 2;
                                            if (refugeFloors.Contains(storey) && vm.SetHighlevelNozzleAndSemiPlatformNozzleParams.Items.First(x => x.PipeId - 1 == 0).PipeConnectionType == "高位-板下")
                                            {
                                                dx -= 300.0;
                                            }
                                            var p9 = p1.OffsetXY(dx, 50);
                                            if (k == 0 && handConnFloors.Contains(storey))
                                            {
                                                p9 = p9.OffsetX(200);
                                            }
                                            textInfos.Add(new DBTextInfo(p9, dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                            if (_refugeCount > _generalCount)
                                            {
                                                var p = GetMidPoint(getRefugeBsPt(i, 0), getRefugeBsPt(i, _refugeCount - _generalCount - 1)).OffsetY(h1);
                                                textInfos.Add(new DBTextInfo(p.OffsetXY(-w / 2, 50), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                            }
                                            {
                                                var i1 = i - 3;
                                                if (i1 >= 0)
                                                {
                                                    var p7 = new Point2d(p1.X, p0.Y - HEIGHT * 3).OffsetXY(-50, (HEIGHT - w) / 2).ToPoint3d();
                                                    var p8 = new Point2d(p3.X, p0.Y - HEIGHT * 3).OffsetXY(h + 100, (HEIGHT - w) / 2).ToPoint3d();
                                                    if (hasFullHalfPlatformLine)
                                                    {
                                                        p7 = p7.OffsetY(HEIGHT / 2);
                                                        p8 = p8.OffsetY(HEIGHT / 2);
                                                    }
                                                    textInfos.Add(new DBTextInfo(p7, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    textInfos.Add(new DBTextInfo(p8, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                }
                                                if (i1 != 0)
                                                {
                                                    var y = getStoreyBsPt(0).Y;
                                                    var p7 = new Point2d(p1.X, y).OffsetXY(-50, (HEIGHT - w) / 2).ToPoint3d();
                                                    var p8 = new Point2d(p3.X, y).OffsetXY(h + 100, (HEIGHT - w) / 2).ToPoint3d();
                                                    if (hasFullHalfPlatformLine)
                                                    {
                                                        p7 = p7.OffsetY(HEIGHT / 2);
                                                        p8 = p8.OffsetY(HEIGHT / 2);
                                                    }
                                                    textInfos.Add(new DBTextInfo(p7, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    textInfos.Add(new DBTextInfo(p8, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                }
                                            }
                                        }
                                    }
                                    var hlines = new List<GLineSegment>();
                                    var hdrills = new List<GRect>();
                                    for (int j = 0; j < _refugeCount - _generalCount; j++)
                                    {
                                        var bsPt = getRefugeBsPt(i, j);
                                        drawDomePipes(new List<Vector2d> { new Vector2d(0, h2), new Vector2d(-300, 0), new Vector2d(0, -300), new Vector2d(300, 0), new Vector2d(0, -(h2 - 300) + fireOffsetY) }.ToGLineSegments(bsPt).Skip(2).ToList());
                                        var p7 = bsPt.OffsetY(400);
                                        var p8 = p7.OffsetX(600);
                                        drawDomePipe(new GLineSegment(p8, p7));
                                        drawFire(p8);
                                        textInfos.Add(new DBTextInfo(p7.OffsetXY(50, -350), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                    }
                                    for (int j = 0; j < _generalCount; j++)
                                    {
                                        var bsPt1 = getGeneralBsPt(i, j);
                                        var bsPt2 = getGeneralBsPt(i + 1, j);
                                        if (j == 0)
                                        {
                                            var p9 = bsPt1.OffsetY(h2);
                                            vsels.Add(p9);
                                            vkills.Add(p9.OffsetY(.1));
                                            brInfos.Add(new BlockInfo("自动排气阀系统1", "W-FRPT-HYDT-PIPE", p9) { Scale = AIR_VALVE_SCALE });
                                            hdrills.Add(new List<Vector2d> { new Vector2d(150, 250), new Vector2d(-300, 0) }.ToGLineSegments(p9).Last().Buffer(.1).ToGRect());
                                        }
                                        var ok = false;
                                        if (vm.SetHighlevelNozzleAndSemiPlatformNozzleParams.Items.First(x => x.PipeId - 1 == getRealJ(j)).PipeConnectionType == "高位-板下")
                                        {
                                            var isLeft = _refugeCount > _generalCount && _refugeCount != _generalCount && (j + 1 > _generalCount / 2);
                                            if (!isLeft)
                                            {
                                                var _segs = new List<Vector2d> { new Vector2d(0, -250), new Vector2d(-600, 0), new Vector2d(0, -300) }.ToGLineSegments(bsPt2);
                                                var s = _segs[1];
                                                var p9 = s.StartPoint;
                                                _segs.RemoveAt(1);
                                                drawDomePipes(GeoFac.GetLines(s.ToLineString().Difference(drawValve(s.EndPoint.OffsetX(150).ToPoint3d()).ToPolygon())));
                                                drawDomePipes(_segs);
                                                ok = true;
                                            }
                                        }
                                        var segs = new List<Vector2d> { new Vector2d(0, -250), new Vector2d(600, 0), new Vector2d(0, -300) }.ToGLineSegments(bsPt2);
                                        if (!ok)
                                        {
                                            {
                                                var s = segs[1];
                                                var p9 = s.StartPoint;
                                                vsels.Add(p9.ToPoint3d());
                                                if (j == _generalCount - 1)
                                                {
                                                    vsels.Add(bsPt1.OffsetY(h2));
                                                }
                                                vkills.Add(p9.OffsetY(-.1).ToPoint3d());
                                                segs.RemoveAt(1);
                                                drawDomePipes(GeoFac.GetLines(s.ToLineString().Difference(drawValve(s.StartPoint.OffsetX(150).ToPoint3d()).ToPolygon())));
                                            }
                                            drawDomePipes(segs);
                                        }
                                        if (_refugeCount == _generalCount)
                                        {
                                            if (j < _generalCount - 1)
                                            {
                                                hdrills.Add(drawValve(segs.Last().EndPoint.OffsetX(250).ToPoint3d()));
                                                hdrills.Add(drawValve(getGeneralBsPt(i, j).OffsetXY(856, h2)));
                                            }
                                            if (j > 0 && j < _generalCount - 1)
                                            {
                                                var _segs = new List<Vector2d> { new Vector2d(-600, 0), new Vector2d(0, -300), new Vector2d(600, 0) }.ToGLineSegments(bsPt1.OffsetY(h2)).Skip(1).ToList();
                                                var p9 = _segs.Last().EndPoint;
                                                vsels.Add(p9.ToPoint3d());
                                                vkills.Add(p9.OffsetY(.1).ToPoint3d());
                                                drawDomePipe(_segs[0]);
                                                drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].StartPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                            }
                                        }
                                        else
                                        {
                                            if (j + 1 != _generalCount / 2 && j != _generalCount - 1)
                                            {
                                                hdrills.Add(drawValve(segs.Last().EndPoint.OffsetX(250).ToPoint3d()));
                                                hdrills.Add(drawValve(getGeneralBsPt(i, j).OffsetXY(856, h2)));
                                            }
                                            if (j != 0 && j + 1 <= _generalCount / 2)
                                            {
                                                var _segs = new List<Vector2d> { new Vector2d(-600, 0), new Vector2d(0, -300), new Vector2d(600, 0) }.ToGLineSegments(bsPt1.OffsetY(h2)).Skip(1).ToList();
                                                var p9 = _segs.Last().EndPoint;
                                                vsels.Add(p9.ToPoint3d());
                                                vkills.Add(p9.OffsetY(.1).ToPoint3d());
                                                drawDomePipe(_segs[0]);
                                                drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].StartPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                            }
                                            if (j + 1 > _generalCount / 2 && j != _generalCount - 1)
                                            {
                                                var _segs = new List<Vector2d> { new Vector2d(600, 0), new Vector2d(0, -300), new Vector2d(-600, 0) }.ToGLineSegments(bsPt1.OffsetY(h2)).Skip(1).ToList();
                                                var p9 = _segs.Last().EndPoint;
                                                vsels.Add(p9.ToPoint3d());
                                                vkills.Add(p9.OffsetY(.1).ToPoint3d());
                                                drawDomePipe(_segs[0]);
                                                drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].EndPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                            }
                                            if (j == _generalCount - 1)
                                            {
                                                var p9 = bsPt1.OffsetY(h2);
                                                vsels.Add(p9);
                                                vkills.Add(p9.OffsetY(.1));
                                            }
                                        }
                                    }
                                    if (_generalCount > 1)
                                    {
                                        var p5 = getGeneralBsPt(i, 0).OffsetY(h2);
                                        var p6 = getGeneralBsPt(i, _generalCount - 1).OffsetY(h2);
                                        hlines.Add(new GLineSegment(p5, p6));
                                    }
                                    vlines.Add(new GLineSegment(p2, p1).ToLineString());
                                    vlines.Add(new GLineSegment(p3, p4).ToLineString());
                                    hlines.Add(new GLineSegment(p1, p3));
                                    if (canHaveHandPumpConnection(storey))
                                    {
                                        double x;
                                        {
                                            var px = p1;
                                            var fixY = -1325 + 300 - (HEIGHT - 3000.0);
                                            brInfos.Add(new BlockInfo("止回阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(300, 0)));
                                            brInfos.Add(new BlockInfo("截止阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(-1500, -250)));
                                            brInfos.Add(new BlockInfo("止回阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(-1150, -250)));
                                            brInfos.Add(new BlockInfo("截止阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(-1885, -1125 + fixY)));
                                            brInfos.Add(new BlockInfo("接驳预留", "W-FRPT-HYDT-EQPM", px.OffsetXY(-1685, -1125 + fixY)));
                                            brInfos.Add(new BlockInfo("止回阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(-1220, -1125 + fixY)));
                                            brInfos.Add(new BlockInfo("安全阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(-845, -1125 + fixY)));
                                            brInfos.Add(new BlockInfo("截止阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(-620, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(300, 0)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(900, 0), px.OffsetXY(900, -250)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(150, -250), px.OffsetXY(900, -250)));
                                            var seg = new GLineSegment(px.OffsetXY(-2500, -250), px.OffsetXY(-1650, -250));
                                            hdrills.Add(new GLineSegment(p1.OffsetX(300), p1.OffsetX(600)).ToLineString().Buffer(.1).ToGRect());
                                            drawDomePipe(seg);
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-1350, -250), px.OffsetXY(-1150, -250)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-850, -250), px.OffsetXY(-150, -250)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-2135, -250), px.OffsetXY(-2135, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-365, -250), px.OffsetXY(-365, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-2135, -1125 + fixY), px.OffsetXY(-2035, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-1735, -1125 + fixY), px.OffsetXY(-1685, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-1270, -1125 + fixY), px.OffsetXY(-1220, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-920, -1125 + fixY), px.OffsetXY(-845, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-845, -1125 + fixY), px.OffsetXY(-770, -1125 + fixY)));
                                            drawDomePipe(new GLineSegment(px.OffsetXY(-470, -1125 + fixY), px.OffsetXY(-365, -1125 + fixY)));
                                            var ed = seg.StartPoint.ReplaceY(p2.Y);
                                            vlines.Add(new GLineSegment(seg.StartPoint, ed).ToLineString());
                                            {
                                                var text = "X0";
                                                var y = getStoreyBsPt(2).Y;
                                                var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                                x = ed.X;
                                                var p = (new Point2d(x, y).ToPoint3d()).OffsetXY(h / 2, (HEIGHT - w) / 2);
                                                textInfos.Add(new DBTextInfo(p, text, "W-FRPT-NOTE", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                vdrills.Add(new GRect(p, p.OffsetXY(-h, w)));
                                            }
                                            {
                                                var p5 = ed;
                                                var p6 = p5.OffsetY(-300);
                                                drawDomePipe(new GLineSegment(p5, p6));
                                                brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p6.ToPoint3d()) { Rotate = Math.PI / 2 });
                                            }
                                        }
                                        if (allStoreys.Contains("3F"))
                                        {
                                            var beLeft = true;
                                            var text = $"X0L{vm.Serialnumber}-{k + 1}";
                                            var y = getStoreyBsPt(2).Y;
                                            var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                            var vecs = new List<Vector2d> { new Vector2d(0, -600), new Vector2d(150, 150), new Vector2d(w, 0) };
                                            if (beLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(new Point2d(x, y).OffsetY(HEIGHT)).Skip(1).ToList();
                                            textInfos.Add(new DBTextInfo((beLeft ? segs[1].EndPoint : segs[1].StartPoint).OffsetY(50).ToPoint3d(), text, "W-FRPT-NOTE", "TH-STYLE3"));
                                            foreach (var seg in segs)
                                            {
                                                lineInfos.Add(new LineInfo(seg, "W-FRPT-NOTE"));
                                            }
                                        }
                                        {
                                            var dn = "DN100";
                                            if (dn != null)
                                            {
                                                var (w, h) = GetDBTextSize(dn, 350, .7, "TH-STYLE3");
                                                {
                                                    var i1 = i - 3;
                                                    var dx = -width_HandPumpConnection;
                                                    if (i1 >= 0)
                                                    {
                                                        textInfos.Add(new DBTextInfo(new Point2d(p1.X, p0.Y - HEIGHT * 3).OffsetXY(dx - 50, (HEIGHT - w) / 2).ToPoint3d(), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    }
                                                    if (i1 != 0)
                                                    {
                                                        var y = getStoreyBsPt(0).Y;
                                                        textInfos.Add(new DBTextInfo(new Point2d(p1.X, y).OffsetXY(dx - 50, (HEIGHT - w) / 2).ToPoint3d(), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    drawDomePipes(GeoFac.GetLines(new MultiLineString(hlines.Select(x => x.ToLineString()).ToArray()).Difference(GeoFac.CreateGeometryEx(hdrills.Select(x => x.ToPolygon()).ToList()))));
                                    ++k;
                                }
                                else if (i >= 1 && ringPairFloors.Any(x => x.Item2 == storey))
                                {
                                    var p0 = getStoreyBsPt(i);
                                    var h1 = HEIGHT - 400.0;
                                    var p1 = p0.OffsetXY(OFFSET_X1 + (ringsCount - k - 1) * SPAN_X + getHandConnWidth(i), h1);
                                    var p3 = p0.OffsetXY(OFFSET_X1 + (_refugeCount + k + ringsCount) * SPAN_X + width_HaveHandPumpConnection, h1);
                                    var p2 = new Point2d(p1.X, basePoint.Y).ToPoint3d();
                                    var p4 = new Point2d(p3.X, basePoint.Y).ToPoint3d();
                                    var hlines = new List<GLineSegment>();
                                    var hdrills = new List<GRect>();
                                    vlines.Add(new GLineSegment(p2, p1).ToLineString());
                                    vlines.Add(new GLineSegment(p3, p4).ToLineString());
                                    hlines.Add(new GLineSegment(p1, p3));
                                    {
                                        var text = "X" + (k + 2);
                                        var y = getStoreyBsPt(2).Y;
                                        var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                        var p = (new Point2d(p1.X, y).ToPoint3d()).OffsetXY(h / 2, (HEIGHT - w) / 2);
                                        if (hasFullHalfPlatformLine) p = p.OffsetY(-HEIGHT / 2);
                                        textInfos.Add(new DBTextInfo(p, text, "W-FRPT-NOTE", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                        vdrills.Add(new GRect(p, p.OffsetXY(-h, w)));
                                    }
                                    var zid = k + 2;
                                    if (allStoreys.Contains("3F"))
                                    {
                                        void _drawLabel(double x, string pipeId, bool beLeft)
                                        {
                                            var text = $"X{zid}L{vm.Serialnumber}-{pipeId}";
                                            var y = getStoreyBsPt(2).Y;
                                            var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                            var vecs = new List<Vector2d> { new Vector2d(0, -600), new Vector2d(150, 150), new Vector2d(w, 0) };
                                            if (beLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(new Point2d(x, y).OffsetY(HEIGHT + (hasFullHalfPlatformLine ? -HEIGHT / 2 : 0))).Skip(1).ToList();
                                            var p = (beLeft ? segs[1].EndPoint : segs[1].StartPoint).OffsetY(50).ToPoint3d();
                                            textInfos.Add(new DBTextInfo(p, text, "W-FRPT-NOTE", "TH-STYLE3"));
                                            foreach (var seg in segs)
                                            {
                                                lineInfos.Add(new LineInfo(seg, "W-FRPT-NOTE"));
                                            }
                                        }
                                        _drawLabel(p1.X, "A", true);
                                        _drawLabel(p3.X, "B", false);
                                    }
                                    {
                                        var text = "X" + zid;
                                        var y = getStoreyBsPt(2).Y;
                                        var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                        var p = (new Point2d(p3.X, y).ToPoint3d()).OffsetXY(h / 2, (HEIGHT - w) / 2);
                                        if (hasFullHalfPlatformLine) p = p.OffsetY(-HEIGHT / 2);
                                        textInfos.Add(new DBTextInfo(p, text, "W-FRPT-NOTE", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                        vdrills.Add(new GRect(p, p.OffsetXY(-h, w)));
                                    }
                                    {
                                        Point3d p7, p8;
                                        {
                                            var p5 = new Point2d(p1.X, getStoreyBsPt(0).Y).ToPoint3d();
                                            var p6 = p5.OffsetY(-300);
                                            drawDomePipe(new GLineSegment(p5, p6));
                                            brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p6) { Rotate = Math.PI / 2 });
                                            p7 = p6;
                                        }
                                        {
                                            var p5 = new Point2d(p3.X, getStoreyBsPt(0).Y).ToPoint3d();
                                            var p6 = p5.OffsetY(-300);
                                            drawDomePipe(new GLineSegment(p5, p6));
                                            brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p6) { Rotate = Math.PI / 2 });
                                            p8 = p6;
                                        }
                                        basementInfos.Add(new Tuple<int, Point3d, Point3d>(zid, p7, p8));
                                    }
                                    if (_generalCount > 0)
                                    {
                                        var p5 = getGeneralBsPt(i - 1, 0);
                                        var p6 = getGeneralBsPt(i - 1, _generalCount - 1);
                                        hlines.Add(new GLineSegment(p5.OffsetY(h1), p6.OffsetY(h1)));
                                        vsels.Add(p5.OffsetY(h1));
                                        vkills.Add(p5.OffsetY(h1).OffsetY(.1));
                                        vsels.Add(p6.OffsetY(h1));
                                        vkills.Add(p6.OffsetY(h1).OffsetY(.1));
                                        {
                                            var dn = vm.ZoneConfigs.Where(x => x.ZoneID == k + 2).FirstOrDefault()?.DNSelectItem;
                                            if (dn != null)
                                            {
                                                var (w, h) = GetDBTextSize(dn, 350, .7, "TH-STYLE3");
                                                textInfos.Add(new DBTextInfo(p1.OffsetXY((SPAN_X * (k + 1) - w) / 2, 50), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                var i1 = i - 3;
                                                if (i1 >= 0)
                                                {
                                                    var p7 = new Point2d(p1.X, p0.Y - HEIGHT * 3).OffsetXY(-50, (HEIGHT - w) / 2).ToPoint3d();
                                                    var p8 = new Point2d(p3.X, p0.Y - HEIGHT * 3).OffsetXY(h + 100, (HEIGHT - w) / 2).ToPoint3d();
                                                    if (hasFullHalfPlatformLine)
                                                    {
                                                        p7 = p7.OffsetY(HEIGHT / 2);
                                                        p8 = p8.OffsetY(HEIGHT / 2);
                                                    }
                                                    textInfos.Add(new DBTextInfo(p7, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    textInfos.Add(new DBTextInfo(p8, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                }
                                                if (i1 != 0)
                                                {
                                                    var y = getStoreyBsPt(0).Y;
                                                    var p7 = new Point2d(p1.X, y).OffsetXY(-50, (HEIGHT - w) / 2).ToPoint3d();
                                                    var p8 = new Point2d(p3.X, y).OffsetXY(h + 100, (HEIGHT - w) / 2).ToPoint3d();
                                                    if (hasFullHalfPlatformLine)
                                                    {
                                                        p7 = p7.OffsetY(HEIGHT / 2);
                                                        p8 = p8.OffsetY(HEIGHT / 2);
                                                    }
                                                    textInfos.Add(new DBTextInfo(p7, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    textInfos.Add(new DBTextInfo(p8, "DN100", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                }
                                            }
                                        }
                                        if (_refugeCount == _generalCount)
                                        {
                                            for (int j = 0; j < _generalCount; j++)
                                            {
                                                {
                                                    var bsPt1 = getGeneralBsPt(i, j);
                                                    if (vm.SetHighlevelNozzleAndSemiPlatformNozzleParams.Items.First(x => x.PipeId - 1 == getRealJ(j)).PipeConnectionType == "低位")
                                                    {
                                                        var p = bsPt1.OffsetY(fireOffsetY);
                                                        vsels.Add(p);
                                                        vkills.Add(p.OffsetY(-.1));
                                                    }
                                                    if (j != _generalCount - 1)
                                                    {
                                                        hdrills.Add(drawValve(bsPt1.OffsetXY(850, h1)));
                                                    }
                                                    {
                                                        var _segs = new List<Vector2d> { new Vector2d(-600, 0), new Vector2d(0, -300), new Vector2d(600, 0) }.ToGLineSegments(bsPt1.OffsetY(h1)).Skip(1).ToList();
                                                        hdrills.Add(new GLineSegment(bsPt1.OffsetY(h1).OffsetX(-150), bsPt1.OffsetY(h1).OffsetX(150)).ToLineString().Buffer(.1).ToGRect());
                                                        drawDomePipe(_segs[0]);
                                                        drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].StartPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                                    }
                                                }
                                                {
                                                    var bsPt1 = getGeneralBsPt(i - 1, j);
                                                    var bsPt3 = getGeneralBsPt(i - 1, _generalCount - 1);
                                                    if (j != _generalCount - 1)
                                                    {
                                                        hdrills.Add(drawValve(bsPt1.OffsetXY(850, h1)));
                                                    }
                                                    if (j > 0 && j < _generalCount - 1)
                                                    {
                                                        var _segs = new List<Vector2d> { new Vector2d(-600, 0), new Vector2d(0, -300), new Vector2d(600, 0) }.ToGLineSegments(bsPt1.OffsetY(h1)).Skip(1).ToList();
                                                        var p9 = _segs.Last().EndPoint;
                                                        vsels.Add(p9.ToPoint3d());
                                                        vkills.Add(p9.OffsetY(.1).ToPoint3d());
                                                        drawDomePipe(_segs[0]);
                                                        drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].StartPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                                    }
                                                    if (j == 0)
                                                    {
                                                        var st1 = bsPt1.OffsetY(h1);
                                                        var st2 = bsPt3.OffsetY(h1);
                                                        var vecs = new List<Vector2d> { new Vector2d(-46, 366), new Vector2d(92, -366) };
                                                        var pts = vecs.ToPoint2ds(st1.ToPoint2D());
                                                        var r = new GRect(pts[1], pts[2]);
                                                        airValvePlaceList.Add(new Tuple<GRect, Ref<Point3d>, List<Point3d>, Action<Point3d>>(r, new Ref<Point3d>(st1), new List<Point3d>() { st1, st2 }, st =>
                                                        {
                                                            brInfos.Add(new BlockInfo("自动排气阀系统1", "W-FRPT-HYDT-PIPE", st) { Scale = AIR_VALVE_SCALE });
                                                        }));
                                                    }
                                                    if (j == 1)
                                                    {
                                                        var dn = vm.ZoneConfigs.Where(x => x.ZoneID == k + 1).FirstOrDefault()?.DNSelectItem;
                                                        if (dn != null)
                                                        {
                                                            var (w, h) = GetDBTextSize(dn, 350, .7, "TH-STYLE3");
                                                            textInfos.Add(new DBTextInfo(bsPt1.OffsetXY(-w - 50, h1 + 50), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int j = 0; j < _generalCount; j++)
                                            {
                                                var bsPt1 = getGeneralBsPt(i, j);
                                                var bsPt2 = getGeneralBsPt(i - 1, j);
                                                var bsPt3 = getGeneralBsPt(i - 1, _generalCount - 1);
                                                if (j > 0 && j < _generalCount - 1)
                                                {
                                                    vsels.Add(bsPt2.OffsetY(h1));
                                                    vkills.Add(bsPt2.OffsetY(h1 + .1));
                                                }
                                                if (j == 0)
                                                {
                                                    var st1 = bsPt2.OffsetY(h1);
                                                    var st2 = bsPt3.OffsetY(h1);
                                                    var vecs = new List<Vector2d> { new Vector2d(-46, 366), new Vector2d(92, -366) };
                                                    var pts = vecs.ToPoint2ds(st1.ToPoint2D());
                                                    var r = new GRect(pts[1], pts[2]);
                                                    airValvePlaceList.Add(new Tuple<GRect, Ref<Point3d>, List<Point3d>, Action<Point3d>>(r, new Ref<Point3d>(st1), new List<Point3d>() { st1, st2 }, st =>
                                                    {
                                                        brInfos.Add(new BlockInfo("自动排气阀系统1", "W-FRPT-HYDT-PIPE", st) { Scale = AIR_VALVE_SCALE });
                                                    }));
                                                }
                                                if (j < _generalCount / 2 - 1)
                                                {
                                                    hdrills.Add(drawValve(bsPt1.OffsetXY(850, h1)));
                                                    hdrills.Add(drawValve(bsPt2.OffsetXY(850, h1)));
                                                }
                                                hdrills.Add(new GLineSegment(bsPt1.OffsetY(h1).OffsetX(-150), bsPt1.OffsetY(h1).OffsetX(150)).ToLineString().Buffer(.1).ToGRect());
                                                if (j < _generalCount / 2)
                                                {
                                                    {
                                                        var _segs = new List<Vector2d> { new Vector2d(-600, 0), new Vector2d(0, -300), new Vector2d(600, 0) }.ToGLineSegments(bsPt1.OffsetY(h1)).Skip(1).ToList();
                                                        var p9 = _segs.Last().EndPoint;
                                                        drawDomePipe(_segs[0]);
                                                        drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].StartPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                                    }
                                                    if (j > 0)
                                                    {
                                                        var _segs = new List<Vector2d> { new Vector2d(-600, 0), new Vector2d(0, -300), new Vector2d(600, 0) }.ToGLineSegments(bsPt2.OffsetY(h1)).Skip(1).ToList();
                                                        var p9 = _segs.Last().EndPoint;
                                                        vsels.Add(p9.ToPoint3d());
                                                        vkills.Add(p9.OffsetY(.1).ToPoint3d());
                                                        drawDomePipe(_segs[0]);
                                                        drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].StartPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                                    }
                                                }
                                                if (j >= _generalCount / 2)
                                                {
                                                    {
                                                        var _segs = new List<Vector2d> { new Vector2d(600, 0), new Vector2d(0, -300), new Vector2d(-600, 0) }.ToGLineSegments(bsPt1.OffsetY(h1)).Skip(1).ToList();
                                                        var p9 = _segs.Last().EndPoint;
                                                        if (_generalCount == 2 && vm.CountsGeneral == 1)
                                                        {
                                                            vsels.Add(p9.ToPoint3d());
                                                            vkills.Add(p9.OffsetY(-.1).ToPoint3d());
                                                        }
                                                        drawDomePipe(_segs[0]);
                                                        drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].EndPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                                    }
                                                    if (j != _generalCount - 1)
                                                    {
                                                        var _segs = new List<Vector2d> { new Vector2d(600, 0), new Vector2d(0, -300), new Vector2d(-600, 0) }.ToGLineSegments(bsPt2.OffsetY(h1)).Skip(1).ToList();
                                                        var p9 = _segs.Last().EndPoint;
                                                        vsels.Add(p9.ToPoint3d());
                                                        vkills.Add(p9.OffsetY(.1).ToPoint3d());
                                                        drawDomePipe(_segs[0]);
                                                        drawDomePipes(GeoFac.GetLines(_segs[1].ToLineString().Difference(drawValve(_segs[1].EndPoint.OffsetX(156).ToPoint3d()).ToPolygon())));
                                                    }
                                                }
                                                if (j == _generalCount / 2 - 1)
                                                {
                                                    var (w, _) = getValveSize();
                                                    hdrills.Add(drawValve(GetMidPoint(bsPt1.OffsetXY(0, h1), getGeneralBsPt(i, j + 1).OffsetXY(0, h1)).OffsetX(-w / 2)));
                                                    hdrills.Add(drawValve(GetMidPoint(bsPt2.OffsetXY(0, h1), getGeneralBsPt(i - 1, j + 1).OffsetXY(0, h1)).OffsetX(-w / 2)));
                                                }
                                                if (j >= _generalCount / 2 && j < _generalCount - 1)
                                                {
                                                    hdrills.Add(drawValve(bsPt1.OffsetXY(850, h1)));
                                                    hdrills.Add(drawValve(bsPt2.OffsetXY(850, h1)));
                                                }
                                            }
                                        }
                                    }
                                    drawDomePipes(GeoFac.GetLines(new MultiLineString(hlines.Select(x => x.ToLineString()).ToArray()).Difference(GeoFac.CreateGeometryEx(hdrills.Select(x => x.ToPolygon()).ToList()))));
                                    ++k;
                                }
                            }
                        }
                        for (int i = 0; i < allStoreys.Count; i++)
                        {
                            var storey = allStoreys[i];
                            var pr = vm.SetHighlevelNozzleAndSemiPlatformNozzleParams;
                            for (int j = 0; j < _generalCount; j++)
                            {
                                var bsPt = getGeneralBsPt(i, j);
                                var arg = pr.Items.First(x => x.PipeId - 1 == getRealJ(j));
                                var isLeft = _refugeCount != _generalCount && (j + 1 > _generalCount / 2);
                                var hasExtraFire = getHasExtraFire(i, j);
                                bool hasHalfPlatform = getHasHalfPlatform(i, j);
                                if (!hasFullHalfPlatformLine)
                                {
                                    if (hasHalfPlatform)
                                    {
                                        lineInfos.Add(new LineInfo(new GLineSegment(bsPt.OffsetXY(-1000, HEIGHT / 2), bsPt.OffsetXY(1000, HEIGHT / 2)), "W-NOTE"));
                                    }
                                }
                                void drawLow(bool isLeft, bool isHalfPlatform)
                                {
                                    var p0 = bsPt;
                                    var txtFixY = .0;
                                    var fixX = .0;
                                    if (isHalfPlatform)
                                    {
                                        p0 = p0.OffsetY(HEIGHT / 2);
                                    }
                                    if (isHalfPlatform && arg.PipeConnectionType == "高位-板上")
                                    {
                                        fixX = 350;
                                        if (isLeft) fixX *= -1;
                                    }
                                    {
                                        var limit = hasHalfPlatform ? HEIGHT / 2 : HEIGHT;
                                        if (refugeFloors.Contains(storey) || ringStoreys.Contains(storey))
                                        {
                                            limit -= 400;
                                        }
                                        if (fireOffsetY + 700 >= limit)
                                        {
                                            p0 = p0.OffsetY(-300);
                                            txtFixY -= 150;
                                            if (!isHalfPlatform && i == 0)
                                            {
                                                fixX = isLeft ? -150 : 150;
                                            }
                                        }
                                    }
                                    var p1 = p0.OffsetY(fireOffsetY);
                                    var p2 = p1.OffsetX(isLeft ? -600 : 600);
                                    if (ringPairFloors.Any(x => x.Item2 == storey))
                                    {
                                        vsels.Add(p1);
                                        vkills.Add(p1.OffsetY(-.1));
                                    }
                                    drawDomePipe(new GLineSegment(p2, p1));
                                    drawFire(p2);
                                    textInfos.Add(new DBTextInfo(p1.OffsetXY(50 + (isLeft ? -600 - 200 : 0) + fixX, -350 + txtFixY), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                }
                                if (i == iRF && hasHalfPlatform)
                                {
                                    if (vm.IsTopLayerRing)
                                    {
                                        var p6 = bsPt.OffsetY(HEIGHT / 2);
                                        var p7 = bsPt.OffsetY(-300);
                                        if (arg.PipeConnectionType == "低位")
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(0, 100), new Vector2d(600, 0) };
                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(p6).ToList();
                                            drawDomePipes(segs);
                                            drawFire(segs.Last().EndPoint.ToPoint3d());
                                            drawDomePipe(new GLineSegment(bsPt.OffsetY(HEIGHT / 2), p7));
                                            textInfos.Add(new DBTextInfo(bsPt.OffsetXY(50, 500), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                        }
                                        else if (arg.PipeConnectionType == "高位-板上")
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(0, 1500), new Vector2d(300, 0), new Vector2d(0, -1100), new Vector2d(300, 0) };
                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(p6).ToList();
                                            drawDomePipes(segs);
                                            drawFire(segs.Last().EndPoint.ToPoint3d());
                                            drawDomePipe(new GLineSegment(bsPt.OffsetY(HEIGHT / 2), p7));
                                            textInfos.Add(new DBTextInfo(bsPt.OffsetXY(50, 950), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                        }
                                        else if (arg.PipeConnectionType == "高位-板下")
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(600, 0), new Vector2d(0, 300) };
                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(p6).Skip(1).ToList();
                                            drawDomePipes(segs);
                                            drawFire(segs.Last().EndPoint.ToPoint3d());
                                            drawDomePipe(new GLineSegment(segs.First().StartPoint.ToPoint3d(), p7));
                                            textInfos.Add(new DBTextInfo(bsPt.OffsetXY(100, 250), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                        }
                                    }
                                    else if (vm.IsRoofRing)
                                    {
                                        var p6 = bsPt.OffsetY(HEIGHT / 2);
                                        var p7 = p6.OffsetY(-300);
                                        if (arg.PipeConnectionType == "低位")
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(0, 100), new Vector2d(600, 0) };
                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(p6).ToList();
                                            drawDomePipes(segs);
                                            drawFire(segs.Last().EndPoint.ToPoint3d());
                                            drawDomePipe(new GLineSegment(p6, p7));
                                            var (w, h) = GetDBTextSize("DN65", 350, .7, "TH-STYLE3");
                                            textInfos.Add(new DBTextInfo(bsPt.OffsetXY(isLeft ? (-50 - w) : 50, 500), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                        }
                                        else if (arg.PipeConnectionType == "高位-板上")
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(0, 600), new Vector2d(300, 0), new Vector2d(0, -600) };
                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(p7).ToList();
                                            drawDomePipes(segs);
                                        }
                                        else if (arg.PipeConnectionType == "高位-板下")
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(600, 0), new Vector2d(0, 300) };
                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                            var segs = vecs.ToGLineSegments(p7).ToList();
                                            drawDomePipes(segs);
                                            drawFire(segs.Last().EndPoint.ToPoint3d());
                                            var (w, h) = GetDBTextSize("DN65", 350, .7, "TH-STYLE3");
                                            textInfos.Add(new DBTextInfo(segs.First().StartPoint.ToPoint3d().OffsetXY(isLeft ? (-50 - w) : 50, -350), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                        }
                                    }
                                }
                                if (IsNumStorey(storey))
                                {
                                    if (getHasOrdinalFire(i, j))
                                    {
                                        if (arg.PipeConnectionType == "低位")
                                        {
                                            drawLow(isLeft, hasHalfPlatform);
                                        }
                                        else if (arg.PipeConnectionType == "高位-板上")
                                        {
                                            if (hasHalfPlatform)
                                            {
                                                if (refugeFloors.Contains(storey) || ringPairFloors.Any(x => x.Item1 == storey))
                                                {
                                                    drawLow(isLeft, hasHalfPlatform);
                                                }
                                                else
                                                {
                                                    if (ringPairFloors.Any(x => x.Item2 == storey))
                                                    {
                                                        var p = bsPt.OffsetY(HEIGHT - 700);
                                                        vsels.Add(p);
                                                        vkills.Add(p.OffsetY(-.1));
                                                    }
                                                    if (storey == maxNumStorey && vm.IsTopLayerRing)
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(0, -(HEIGHT - 300 - fireOffsetY)), new Vector2d(300, 0) };
                                                        if (isLeft) vecs = vecs.GetYAxisMirror();
                                                        var p1 = bsPt.OffsetY(HEIGHT * 1.5 - 300);
                                                        var segs = vecs.ToGLineSegments(p1);
                                                        var pt = segs.Last().EndPoint.ToPoint3d();
                                                        drawFire(pt);
                                                        vecs = new List<Vector2d> { new Vector2d(-300, 0), new Vector2d(0, (HEIGHT / 2 - 300 - fireOffsetY)) };
                                                        if (isLeft) vecs = vecs.GetYAxisMirror();
                                                        segs = vecs.ToGLineSegments(pt);
                                                        drawDomePipes(segs);
                                                        textInfos.Add(new DBTextInfo(bsPt.OffsetY(HEIGHT / 2).OffsetXY(50 + (isLeft ? -600 - 200 : 0), 50), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                    }
                                                    else
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(0, -(HEIGHT - 300 - fireOffsetY)), new Vector2d(300, 0) };
                                                        if (isLeft) vecs = vecs.GetYAxisMirror();
                                                        var p1 = bsPt.OffsetY(HEIGHT * 1.5 - 300);
                                                        var segs = vecs.ToGLineSegments(p1);
                                                        drawDomePipes(segs);
                                                        drawFire(segs.Last().EndPoint.ToPoint3d());
                                                        textInfos.Add(new DBTextInfo(bsPt.OffsetY(HEIGHT / 2).OffsetXY(50 + (isLeft ? -600 - 200 : 0), 50), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var dn = "DN65";
                                                var (w, h) = GetDBTextSize(dn, 350, .7, "TH-STYLE3");
                                                var p1 = bsPt.OffsetY(fireOffsetY);
                                                var p8 = p1.OffsetX(isLeft ? -600 : 600);
                                                var p7 = p1.OffsetXY(50 + (isLeft ? -600 - 200 : 0), -350);
                                                var vecs = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(0, -(HEIGHT - fireOffsetY - 300)), new Vector2d(300, 0) };
                                                var st = bsPt.OffsetY(HEIGHT - 300);
                                                if (refugeFloors.Contains(storey))
                                                {
                                                    st = bsPt.OffsetY(HEIGHT - 800);
                                                    if (isLeft)
                                                    {
                                                        st = st.OffsetXY(-300, 0);
                                                    }
                                                    else
                                                    {
                                                        st = st.OffsetXY(300, 0);
                                                    }
                                                    if (j == _generalCount - 1)
                                                    {
                                                        if (isLeft)
                                                        {
                                                        }
                                                        else
                                                        {
                                                            st = st.OffsetX(-300 - 900);
                                                        }
                                                    }
                                                    vecs = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(300, 0), new Vector2d(0, -(HEIGHT - fireOffsetY - 300 - 800)), new Vector2d(300, 0) };
                                                    if (isLeft) vecs = vecs.GetYAxisMirror();
                                                    p8 = vecs.GetLastPoint(st).ToPoint3d();
                                                    if (isLeft)
                                                    {
                                                        p7 = p8.OffsetXY(-w / 2 + 300 / 2, -h - 100);
                                                    }
                                                    else
                                                    {
                                                        p7 = p8.OffsetXY(-w / 2 - 300 / 2, -h - 100);
                                                    }
                                                    drawDomePipes(vecs.ToGLineSegments(st));
                                                    drawFire(p8);
                                                    textInfos.Add(new DBTextInfo(p7, dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                }
                                                else
                                                {
                                                    var _pr = ringPairFloors.FirstOrDefault(x => x.Item2 == storey);
                                                    if (_pr.Item2 != null)
                                                    {
                                                        st = bsPt.OffsetY(HEIGHT - 300 - 400);
                                                        vsels.Add(st);
                                                        vkills.Add(st.OffsetY(-.1));
                                                        vecs = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(0, -(HEIGHT - fireOffsetY - 300 - 400)), new Vector2d(300, 0) };
                                                        if (isLeft) vecs = vecs.GetYAxisMirror();
                                                        drawDomePipes(vecs.ToGLineSegments(st));
                                                        drawFire(p8);
                                                        textInfos.Add(new DBTextInfo(p7, dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                    }
                                                    else
                                                    {
                                                        _pr = ringPairFloors.FirstOrDefault(x => x.Item1 == storey);
                                                        if (_pr.Item1 != null)
                                                        {
                                                            st = bsPt.OffsetY(HEIGHT - 300 - 400);
                                                            if (isLeft)
                                                            {
                                                                st = st.OffsetXY(-300, 300);
                                                            }
                                                            else
                                                            {
                                                                st = st.OffsetXY(300, 300);
                                                            }
                                                            if (j == _generalCount - 1)
                                                            {
                                                                if (isLeft)
                                                                {
                                                                }
                                                                else
                                                                {
                                                                    st = st.OffsetX(-300 - 900);
                                                                }
                                                            }
                                                            vecs = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(300, 0), new Vector2d(0, -(HEIGHT - fireOffsetY - 300 - 400)), new Vector2d(300, 0) };
                                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                                            p8 = vecs.GetLastPoint(st).ToPoint3d();
                                                            if (isLeft)
                                                            {
                                                                p7 = p8.OffsetXY(-w / 2 + 300 / 2, -h - 100);
                                                            }
                                                            else
                                                            {
                                                                p7 = p8.OffsetXY(-w / 2 - 300 / 2, -h - 100);
                                                            }
                                                            drawDomePipes(vecs.ToGLineSegments(st));
                                                            drawFire(p8);
                                                            textInfos.Add(new DBTextInfo(p7, dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                        }
                                                        else
                                                        {
                                                            if (isLeft) vecs = vecs.GetYAxisMirror();
                                                            drawDomePipes(vecs.ToGLineSegments(st));
                                                            drawFire(p8);
                                                            textInfos.Add(new DBTextInfo(p7, dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (arg.PipeConnectionType == "高位-板下")
                                        {
                                            var ok = false;
                                            if (!hasHalfPlatform && i != 0)
                                            {
                                                if (ringPairInts.Any(x => x.Item2 == i))
                                                {
                                                    var px = bsPt;
                                                    if (isLeft)
                                                    {
                                                        brInfos.Add(new BlockInfo("室内消火栓系统1", "W-FRPT-HYDT", px.OffsetXY(-600, 400)));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-600, 400), px.OffsetXY(-600, -200)), "W-FRPT-HYDT-PIPE"));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-600, -200), px.OffsetXY(-300, -200)), "W-FRPT-HYDT-PIPE"));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-300, -200), px.OffsetXY(-300, -400)), "W-FRPT-HYDT-PIPE"));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-59, 100), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                    }
                                                    else
                                                    {
                                                        brInfos.Add(new BlockInfo("室内消火栓系统1", "W-FRPT-HYDT", px.OffsetXY(600, 400)));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(441, 100), "DN65", "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(600, 400), px.OffsetXY(600, 0)), "W-FRPT-HYDT-PIPE"));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(600, 0), px.OffsetXY(600, -200)), "W-FRPT-HYDT-PIPE"));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(600, -200), px.OffsetXY(300, -200)), "W-FRPT-HYDT-PIPE"));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(300, -200), px.OffsetXY(300, -400)), "W-FRPT-HYDT-PIPE"));
                                                    }
                                                    ok = true;
                                                }
                                            }
                                            if (!ok)
                                            {
                                                var dx1 = isLeft ? -600.0 : 600.0;
                                                var fixY = .0;
                                                var p0 = bsPt;
                                                if (hasHalfPlatform)
                                                {
                                                    p0 = p0.OffsetY(HEIGHT / 2);
                                                    var limit = HEIGHT / 2;
                                                    if (refugeFloors.Contains(storey))
                                                    {
                                                        limit -= 800;
                                                    }
                                                    else if (ringPairFloors.Any(x => x.Item1 == storey || x.Item2 == storey))
                                                    {
                                                        limit -= 400;
                                                    }
                                                    else if (storey == maxNumStorey && vm.IsTopLayerRing)
                                                    {
                                                        limit -= 450;
                                                    }
                                                    if (fireOffsetY + fireHeight >= limit)
                                                    {
                                                        fixY -= fireOffsetY;
                                                    }
                                                }
                                                var p1 = p0.OffsetY(fireOffsetY);
                                                var p2 = p1.OffsetXY(dx1, fixY);
                                                var vecs = new List<Vector2d> { new Vector2d(dx1, 0), new Vector2d(0, 300 + fireOffsetY + fixY) };
                                                var st = p0.OffsetY(-300);
                                                if (refugeFloors.Contains(allStoreys.TryGet(i - 1)))
                                                {
                                                    vecs = new List<Vector2d> { new Vector2d(dx1, 0), new Vector2d(0, 250 + fireOffsetY) };
                                                    st = p0.OffsetY(-250);
                                                }
                                                else if (ringPairFloors.Any(x => x.Item2 == storey))
                                                {
                                                    var seg = new GLineSegment(p0, st);
                                                    drawDomePipe(seg);
                                                    obstacles.Add(seg.Buffer(10));
                                                }
                                                drawFire(p2);
                                                if (ringPairFloors.Any(x => x.Item2 == storey))
                                                {
                                                    vsels.Add(st);
                                                    vkills.Add(st.OffsetY(-.1));
                                                }
                                                var segs = vecs.ToGLineSegments(st);
                                                if (storey == "1F" && !hasHalfPlatform)
                                                {
                                                    segs.RemoveAt(0);
                                                    brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", segs[0].StartPoint.ToPoint3d()) { Rotate = Math.PI / 2 });
                                                }
                                                drawDomePipes(segs);
                                                var text = "DN65";
                                                var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                                                if (hasHalfPlatform)
                                                {
                                                    textInfos.Add(new DBTextInfo(st.OffsetXY(isLeft ? -100 - w : 100, -h - 100), text, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                }
                                                else
                                                {
                                                    textInfos.Add(new DBTextInfo(p0.OffsetXY(h + (dx1 - h) / 2 + (isLeft ? 100 : 0), 100), text, "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                                }
                                                ok = true;
                                            }
                                        }
                                    }
                                    if (hasExtraFire)
                                    {
                                        if (pr.AdditionalFireHydrant is SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.AdditionalFireHydrantEnum.NoNo)
                                        {
                                            var p5 = bsPt.OffsetX(-600);
                                            var p6 = bsPt.OffsetX(600);
                                            {
                                                var px = p5;
                                                var p7 = px.OffsetXY(0, -300);
                                                var p8 = px.OffsetXY(0, 400);
                                                drawDomePipe(new GLineSegment(p7, p8));
                                                drawFire(p8);
                                                brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p7) { Rotate = Math.PI / 2 });
                                                var dn = "DN65";
                                                textInfos.Add(new DBTextInfo(p5.OffsetXY(-150, 50), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                            }
                                            {
                                                var px = p6;
                                                var p7 = px.OffsetXY(0, -300);
                                                var p8 = px.OffsetXY(0, 400);
                                                drawDomePipe(new GLineSegment(p7, p8));
                                                drawFire(p8);
                                                brInfos.Add(new BlockInfo("水管中断", "W-FRPT-HYDT-EQPM", p7) { Rotate = Math.PI / 2 });
                                                var dn = "DN65";
                                                var (w, h) = GetDBTextSize(dn, 350, .7, "TH-STYLE3");
                                                textInfos.Add(new DBTextInfo(p6.OffsetXY(h + 250, 50), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3") { Rotation = Math.PI / 2 });
                                            }
                                        }
                                        else
                                        {
                                            var beLeft = isLeft;
                                            if (getHasHalfPlatform(i, j) || getHasOrdinalFire(i, j))
                                            {
                                                beLeft = !beLeft;
                                            }
                                            drawLow(beLeft, false);
                                        }
                                    }
                                }
                            }
                        }
                        {
                            var obstacle = GeoFac.CreateGeometryEx(obstacles);
                            foreach (var item in airValvePlaceList)
                            {
                                foreach (var pt in item.Item3)
                                {
                                    if (!pt.OffsetY(150).ToNTSPoint().Intersects(obstacle))
                                    {
                                        item.Item2.Value = pt;
                                        break;
                                    }
                                }
                            }
                            foreach (var item in airValvePlaceList)
                            {
                                item.Item4(item.Item2.Value);
                            }
                        }
                        if (vsels.Count > 0 && vkills.Count > 0)
                        {
                            var kill = GeoFac.CreateGeometryEx(vkills.Distinct().Select(x => GRect.Create(x.ToPoint2D(), .01, .01).ToPolygon()).ToList());
                            var lines = GeoFac.CreateIntersectsSelector(vlines)(GeoFac.CreateGeometryEx(vsels.Distinct().Select(x => GRect.Create(x.ToPoint2D(), .01, .01).ToPolygon()).ToList()));
                            vlines = vlines.Except(lines).ToList();
                            lines.AddRange(vsels.Distinct().Select(x => GRect.Create(x.ToPoint2D(), .01, .01)).Select(r => new GLineSegment(r.LeftTop, r.RightButtom).ToLineString()));
                            var lst = GeoFac.ToNodedLineSegments(GeoFac.GetLines(new MultiLineString(lines.ToArray())).ToList()).Where(x => x.Length > 1).ToList();
                            vlines.AddRange(lst.Select(x => x.ToLineString()).Where(x => !x.Intersects(kill)));
                        }
                        if (vdrills.Count > 0)
                        {
                            vlines = GeoFac.GetLines(new MultiLineString(vlines.ToArray()).Difference(GeoFac.CreateGeometryEx(vdrills.Select(x => x.ToPolygon()).ToList()))).Select(x => x.ToLineString()).ToList();
                        }
                    }
                    void drawWSupLines(GLineSegment[] segs)
                    {
                        foreach (var seg in segs.Where(x => x.IsValid))
                        {
                            var line = DrawLineSegmentLazy(seg);
                            line.Layer = "W-WSUP-NOTE";
                            ByLayer(line);
                        }
                    }
                    void drawTestFire(Point3d bsp, double height, double offsetX)
                    {
                        if (!vm.HaveTestFireHydrant) return;
                        var segs = new List<Vector2d> { new Vector2d(offsetX, 0), new Vector2d(0, height), }.ToGLineSegments(bsp).Skip(1).ToList();
                        drawDomePipes(segs);
                        {
                            var px = segs.Last().EndPoint.ToPoint3d();
                            var bi = new BlockInfo("室内消火栓系统1", "W-FRPT-HYDT", px.OffsetXY(-1200, 300));
                            bi.DynaDict["可见性"] = "试验消火栓";
                            brInfos.Add(bi);
                            brInfos.Add(new BlockInfo("自动排气阀系统1", "W-FRPT-HYDT-PIPE", px.OffsetXY(-600, 600)));
                            brInfos.Add(new BlockInfo("蝶阀", "W-FRPT-HYDT-EQPM", px.OffsetXY(-420, 0)));
                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(-180, 0)), "W-FRPT-HYDT-PIPE"));
                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-420, 0), px.OffsetXY(-600, 0)), "W-FRPT-HYDT-PIPE"));
                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-600, 0), px.OffsetXY(-600, 600)), "W-FRPT-HYDT-PIPE"));
                            var p7 = px.OffsetXY(-600, 300);
                            lineInfos.Add(new LineInfo(new GLineSegment(p7, px.OffsetXY(-1200, 300)), "W-FRPT-HYDT-PIPE"));
                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-600, 1332), px.OffsetXY(-388, 1544)), "W-WSUP-NOTE"));
                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-388, 1544), px.OffsetXY(2354, 1544)), "W-WSUP-NOTE"));
                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-1341, 659), px.OffsetXY(-1600, 400)), "W-WSUP-NOTE"));
                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-1600, 400), px.OffsetXY(-2898, 400)), "W-WSUP-NOTE"));
                            textInfos.Add(new DBTextInfo(px.OffsetXY(-388, 1594), "自动排气阀DN25，余同", "W-WSUP-NOTE", "TH-STYLE3"));
                            textInfos.Add(new DBTextInfo(px.OffsetXY(-388, 1144), "该排气阀高度距地1.5m", "W-WSUP-NOTE", "TH-STYLE3"));
                            textInfos.Add(new DBTextInfo(px.OffsetXY(-2898, 400), "试验消火栓", "W-WSUP-NOTE", "TH-STYLE3"));
                            {
                            }
                            {
                                var dn = "DN65";
                                var (w, h) = GetDBTextSize(dn, TEXT_HEIGHT, .7, "TH-STLYE3");
                                textInfos.Add(new DBTextInfo(p7.OffsetXY(-w - 100, -h - 100), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                            }
                        }
                    }
                    for (int i = 0; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        if (storey == "RF")
                        {
                            if (vm.IsRoofRing)
                            {
                                var p1s = new List<Point3d>();
                                var bds = new List<GRect>();
                                var h = HEIGHT / 2 - 300;
                                for (int j = 0; j < _generalCount; j++)
                                {
                                    var bsPt = getGeneralBsPt(i, j);
                                    var p1 = bsPt.OffsetY(h);
                                    p1s.Add(p1);
                                    vlines.Add(new GLineSegment(bsPt, p1).ToLineString());
                                    if (_generalCount > 1)
                                    {
                                        if (j == _generalCount - 1)
                                        {
                                            if (_generalCount == 2)
                                            {
                                                var (w, _) = getValveSize();
                                                var p3 = p1s.First();
                                                var p4 = p1s.Last();
                                                bds.Add(drawValve(GetMidPoint(p3, p4).OffsetXY(vm.HaveTestFireHydrant ? w : 0, 0)));
                                            }
                                            else
                                            {
                                                bds.Add(drawValve(p1.OffsetX(-600)));
                                            }
                                        }
                                        else if (j == _generalCount - 2)
                                        {
                                        }
                                        else
                                        {
                                            Point3d p2;
                                            if (j == 0)
                                            {
                                                p2 = p1.OffsetX(400);
                                            }
                                            else if (_refugeCount > _generalCount && j + 1 == _generalCount / 2)
                                            {
                                                var (w, _) = getValveSize();
                                                p2 = new Point3d((getGeneralBsPt(0, j).X + getGeneralBsPt(0, j + 1).X) / 2 - w / 2, p1.Y, 0);
                                            }
                                            else
                                            {
                                                p2 = p1.OffsetX(700);
                                            }
                                            bds.Add(drawValve(p2));
                                            if (j + 1 == _generalCount / 2)
                                            {
                                                var id = vm.ZoneConfigs.Where(x => x.IsEffective() && x.GetIntStartFloor().HasValue && x.GetIntEndFloor().HasValue).Select(x => x.ZoneID).Max();
                                                var dn = vm.ZoneConfigs.FirstOrDefault(x => x.ZoneID == id)?.DNSelectItem;
                                                if (dn != null)
                                                {
                                                    var px = p2.OffsetY(150);
                                                    var x1 = getGeneralBsPt(i, 0).X;
                                                    if (px.X - x1 < 1500)
                                                    {
                                                        px = px.ReplaceX(x1 + 1500);
                                                    }
                                                    textInfos.Add(new DBTextInfo(px, dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                                }
                                            }
                                        }
                                    }
                                }
                                if (_generalCount == 2)
                                {
                                    var id = vm.ZoneConfigs.Where(x => x.IsEffective() && x.GetIntStartFloor().HasValue && x.GetIntEndFloor().HasValue).Select(x => x.ZoneID).Max();
                                    var dn = vm.ZoneConfigs.FirstOrDefault(x => x.ZoneID == id)?.DNSelectItem;
                                    if (dn != null)
                                    {
                                        var bsPt = getGeneralBsPt(i, 0);
                                        var p1 = bsPt.OffsetY(h);
                                        textInfos.Add(new DBTextInfo(p1.OffsetXY(1200, 200), dn, "W-FRPT-HYDT-DIMS", "TH-STYLE3"));
                                    }
                                }
                                if (_generalCount == 1)
                                {
                                }
                                else if (_generalCount > 1)
                                {
                                    var bsp = p1s.First();
                                    drawTestFire(bsp, 300, 1000);
                                }
                                if (_generalCount > 1)
                                {
                                    var seg = new GLineSegment(p1s.First(), p1s.Last());
                                    if (!vm.HaveTestFireHydrant) brInfos.Add(new BlockInfo("自动排气阀系统1", "W-FRPT-HYDT-PIPE", p1s[0]) { Scale = AIR_VALVE_SCALE });
                                    drawDomePipes(GeoFac.GetLines(seg.ToLineString().Difference(GeoFac.CreateGeometryEx(bds.Select(bd => bd.ToPolygon()).ToList()))));
                                }
                                for (int j = 1; j < _generalCount - 1; j++)
                                {
                                    var p1 = p1s[j];
                                    var isLeft = _refugeCount != _generalCount && (j + 1 > _generalCount / 2);
                                    var vecs = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-600, 0), new Vector2d(0, -600), new Vector2d(600, 0) };
                                    if (isLeft) vecs = vecs.GetYAxisMirror();
                                    var segs = vecs.ToGLineSegments(p1);
                                    {
                                        var s = segs[1];
                                        var p3 = s.StartPoint;
                                        var p4 = segs.Last().EndPoint;
                                        var r3 = GRect.Create(p3, .01, .01);
                                        var r4 = GRect.Create(p4, .01, .01);
                                        var kill = GeoFac.CreateGeometryEx(new GRect[] { GRect.Create(p3.OffsetY(-.1), .01, .01), GRect.Create(p4.OffsetY(.1), .01, .01) }.Select(x => x.ToPolygon()).ToList());
                                        var lines = GeoFac.CreateIntersectsSelector(vlines)(GeoFac.CreateGeometryEx(new GRect[] { r3, r4 }.Select(x => x.ToPolygon()).ToList()));
                                        vlines = vlines.Except(lines).ToList();
                                        lines.Add(new GLineSegment(r3.LeftTop, r3.RightButtom).ToLineString());
                                        lines.Add(new GLineSegment(r4.LeftTop, r4.RightButtom).ToLineString());
                                        var lst = GeoFac.ToNodedLineSegments(GeoFac.GetLines(new MultiLineString(lines.ToArray())).ToList()).Where(x => x.Length > 1).ToList();
                                        vlines.AddRange(lst.Select(x => x.ToLineString()).Where(x => !x.Intersects(kill)));
                                        segs.RemoveAt(1);
                                        var bd = drawValve((isLeft ? s.StartPoint : s.EndPoint).OffsetX(150).ToPoint3d());
                                        drawDomePipes(GeoFac.GetLines(s.ToLineString().Difference(bd.ToPolygon())));
                                    }
                                    drawDomePipes(segs);
                                }
                            }
                            else if (vm.IsTopLayerRing)
                            {
                                if (_generalCount == 1)
                                {
                                    var bsPt = getGeneralBsPt(i, 0);
                                    drawTestFire(bsPt, 1000, 0);
                                }
                                else if (_generalCount > 1)
                                {
                                    var h1 = 300;
                                    var h2 = 300;
                                    {
                                        var p3 = getGeneralBsPt(i - 1, 0).OffsetY(HEIGHT - h1).ToPoint2D();
                                        drawTestFire(p3.ToPoint3d(), 600, 1000);
                                        var p4 = getGeneralBsPt(i - 1, _generalCount - 1).OffsetY(HEIGHT - h1).ToPoint2D();
                                        if (!vm.HaveTestFireHydrant) brInfos.Add(new BlockInfo("自动排气阀系统1", "W-FRPT-HYDT-PIPE", p3.ToPoint3d()) { Scale = AIR_VALVE_SCALE });
                                        {
                                            var bds = new List<GRect>();
                                            for (int j = 0; j < _generalCount; j++)
                                            {
                                                if (j == _generalCount - 1)
                                                {
                                                    if (_generalCount == 2)
                                                    {
                                                        var (w, _) = getValveSize();
                                                        bds.Add(drawValve(GetMidPoint(p3.ToPoint3d(), p4.ToPoint3d()).OffsetXY(vm.HaveTestFireHydrant ? w : 0, 0)));
                                                    }
                                                    else
                                                    {
                                                        bds.Add(drawValve(p4.OffsetX(-650).ToPoint3d()));
                                                    }
                                                }
                                                else if (j == _generalCount - 2)
                                                {
                                                }
                                                else if (j == 0)
                                                {
                                                    var dx = 400;
                                                    bds.Add(drawValve(p3.OffsetX(dx).ToPoint3d()));
                                                }
                                                else
                                                {
                                                    var p = getGeneralBsPt(i - 1, j);
                                                    var dx = 710.0;
                                                    if (_refugeCount != _generalCount && j + 1 == _generalCount / 2)
                                                    {
                                                        dx += 2300;
                                                    }
                                                    bds.Add(drawValve(p.OffsetXY(dx, HEIGHT - h1)));
                                                }
                                            }
                                            drawDomePipes(GeoFac.GetLines(new GLineSegment(p4, p3).ToLineString().Difference(GeoFac.CreateGeometryEx(bds.Select(bd => bd.ToPolygon()).ToList()))));
                                        }
                                        var r3 = GRect.Create(p3, .01, .01);
                                        var r4 = GRect.Create(p4, .01, .01);
                                        var kill = GeoFac.CreateGeometryEx(new GRect[] { GRect.Create(p3.OffsetY(.1), .01, .01), GRect.Create(p4.OffsetY(.1), .01, .01) }.Select(x => x.ToPolygon()).ToList());
                                        var lines = GeoFac.CreateIntersectsSelector(vlines)(GeoFac.CreateGeometryEx(new GRect[] { r3, r4 }.Select(x => x.ToPolygon()).ToList()));
                                        vlines = vlines.Except(lines).ToList();
                                        lines.Add(new GLineSegment(r3.LeftTop, r3.RightButtom).ToLineString());
                                        lines.Add(new GLineSegment(r4.LeftTop, r4.RightButtom).ToLineString());
                                        var lst = GeoFac.ToNodedLineSegments(GeoFac.GetLines(new MultiLineString(lines.ToArray())).ToList()).Where(x => x.Length > 1).ToList();
                                        vlines.AddRange(lst.Select(x => x.ToLineString()).Where(x => !x.Intersects(kill)));
                                    }
                                    for (int j = 1; j < _generalCount - 1; j++)
                                    {
                                        var isLeft = _refugeCount != _generalCount && (j + 1 > _generalCount / 2);
                                        var p = getGeneralBsPt(i - 1, j);
                                        var segs = new List<Vector2d> { new Vector2d(isLeft ? 600 : -600, 0), new Vector2d(0, h2) }.ToGLineSegments(p.OffsetY(HEIGHT - h1 - h2));
                                        if (isLeft)
                                        {
                                            drawDomePipes(GeoFac.GetLines(segs[0].ToLineString().Difference(drawValve(segs[0].StartPoint.OffsetX(210).ToPoint3d()).ToPolygon())));
                                        }
                                        else
                                        {
                                            drawDomePipes(GeoFac.GetLines(segs[0].ToLineString().Difference(drawValve(segs[0].EndPoint.OffsetX(150).ToPoint3d()).ToPolygon())));
                                        }
                                        drawDomePipe(segs[1]);
                                        var p3 = segs[0].StartPoint;
                                        var p4 = segs[0].StartPoint.OffsetY(h2);
                                        var r3 = GRect.Create(p3, .01, .01);
                                        var r4 = GRect.Create(p4, .01, .01);
                                        var kill = GeoFac.CreateGeometryEx(new GRect[] { GRect.Create(p3.OffsetY(.1), .01, .01), GRect.Create(p4.OffsetY(.1), .01, .01) }.Select(x => x.ToPolygon()).ToList());
                                        var lines = GeoFac.CreateIntersectsSelector(vlines)(GeoFac.CreateGeometryEx(new GRect[] { r3, r4 }.Select(x => x.ToPolygon()).ToList()));
                                        vlines = vlines.Except(lines).ToList();
                                        lines.Add(new GLineSegment(r3.LeftTop, r3.RightButtom).ToLineString());
                                        lines.Add(new GLineSegment(r4.LeftTop, r4.RightButtom).ToLineString());
                                        var lst = GeoFac.ToNodedLineSegments(GeoFac.GetLines(new MultiLineString(lines.ToArray())).ToList()).Where(x => x.Length > 1).ToList();
                                        vlines.AddRange(lst.Select(x => x.ToLineString()).Where(x => !x.Intersects(kill)));
                                    }
                                }
                            }
                        }
                    }
                    List<string> names = null;
                    if (basementInfos.Count == 1)
                    {
                        names = new List<string>() { "高区" };
                    }
                    else if (basementInfos.Count == 2)
                    {
                        names = new List<string>() { "中区", "高区" };
                    }
                    else if (basementInfos.Count == 3)
                    {
                        names = new List<string>() { "中1区", "中2区", "高区" };
                    }
                    if (names != null)
                    {
                        for (int i = 0; i < basementInfos.Count; i++)
                        {
                            var item = basementInfos[i];
                            var p3 = item.Item2;
                            var p4 = item.Item3;
                            var p5 = p3.OffsetY(-1200 - i * 600);
                            var p6 = p4.OffsetY(-1200 - i * 600);
                            lineInfos.Add(new LineInfo(new GLineSegment(p3, p5), "W-FRPT-NOTE"));
                            lineInfos.Add(new LineInfo(new GLineSegment(p4, p6), "W-FRPT-NOTE"));
                            var seg = new GLineSegment(p5, p6);
                            lineInfos.Add(new LineInfo(seg, "W-FRPT-NOTE"));
                            var name = names[i];
                            var text = $"接自地库{name}消火栓环管（入口压力X.XXMPa）";
                            var (w, h) = GetDBTextSize(text, 350, .7, "TH-STYLE3");
                            textInfos.Add(new DBTextInfo(seg.StartPoint.OffsetXY((seg.Length - w) / 2, -h - 100).ToPoint3d(), text, "W-FRPT-NOTE", "TH-STYLE3"));
                        }
                    }
                }
                foreach (var g in GeoFac.GroupParallelLines(vlines.SelectMany(ls => GeoFac.GetLines(ls).Where(x => x.IsValid)).ToList(), 1, .01)) ByLayer(DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: 10e5), "W-FRPT-HYDT-PIPE"));
                foreach (var info in lineInfos)
                {
                    var line = DrawLineSegmentLazy(info.Line);
                    if (!string.IsNullOrEmpty(info.LayerName))
                    {
                        line.Layer = info.LayerName;
                    }
                    ByLayer(line);
                }
                foreach (var info in textInfos)
                {
                    var dbt = DrawTextLazy(info.Text, info.BasePoint.ToPoint2d());
                    dbt.Rotation = info.Rotation;
                    dbt.WidthFactor = .7;
                    dbt.Height = TEXT_HEIGHT;
                    if (!string.IsNullOrEmpty(info.LayerName)) dbt.Layer = info.LayerName;
                    if (!string.IsNullOrEmpty(info.TextStyle)) DrawingQueue.Enqueue(adb => { SetTextStyle(dbt, info.TextStyle); });
                    ByLayer(dbt);
                }
                foreach (var info in brInfos)
                {
                    DrawBlockReference(info.BlockName, info.BasePoint, layer: info.LayerName, cb: br =>
                    {
                        ByLayer(br);
                        {
                            if (info.DynaDict != null && br.IsDynamicBlock) br.DynamicBlockReferencePropertyCollection.Cast<DynamicBlockReferenceProperty>().Where(x => !x.ReadOnly).Join(info.DynaDict, x => x.PropertyName, y => y.Key, (x, y) => x.Value = y.Value).Count();
                        }
                    }, props: info.PropDict, scale: info.Scale, rotateDegree: info.Rotate.AngleToDegree());
                }
                FlushDQ(adb);
                static bool IsNumStorey(string storey)
                {
                    return GetStoreyScore(storey) < ushort.MaxValue;
                }
                static int GetStoreyScore(string label)
                {
                    if (label == null) return 0;
                    switch (label)
                    {
                        case "RF": return ushort.MaxValue;
                        case "RF+1": return ushort.MaxValue + 1;
                        case "RF+2": return ushort.MaxValue + 2;
                        default:
                            {
                                int.TryParse(label.Replace("F", ""), out int ret);
                                return ret;
                            }
                    }
                }
            }
        }
    }
    class InputDataConvert
    {
        public static FloorGroupData SplitFloor(FireControlSystemDiagramViewModel _vm)
        {
            int minFloor = int.MaxValue;
            int maxFloor = int.MinValue;
            var refugeFloors = new List<int>();
            var floorInts = new Dictionary<int, int>();
            var floorDNs = new Dictionary<int, string>();
            var listFloor = _vm.ZoneConfigs.ToList();
            for (int i = 0; i < listFloor.Count; i++)
            {
                var floor = listFloor[i];
                if (!floor.IsEffective() || string.IsNullOrEmpty(floor.StartFloor))
                    break;
                int startFloor = floor.GetIntStartFloor().Value;
                int endFloor = floor.GetIntEndFloor().Value;
                floorInts.Add(startFloor, endFloor);
                floorDNs.Add(i, floor.DNSelectItem.ToString());
                minFloor = Math.Min(startFloor, minFloor);
                maxFloor = Math.Max(endFloor, maxFloor);
                bool isRefugeFloor = false;
                if (i != 0)
                {
                    var preFloor = listFloor[i - 1];
                    isRefugeFloor = preFloor.GetIntEndFloor().Value == floor.GetIntStartFloor().Value;
                }
                if (isRefugeFloor)
                    refugeFloors.Add(startFloor);
            }
            var groupData = new FloorGroupData(minFloor, maxFloor);
            foreach (var keyValue in floorInts)
                groupData.FloorGroups.Add(keyValue.Key, keyValue.Value);
            foreach (var keyValue in floorDNs)
                groupData.FloorGroupDN.Add(keyValue.Key, keyValue.Value);
            groupData.RefugeFloors.AddRange(refugeFloors);
            return groupData;
        }
        public static List<FloorDataModel> FloorDataModels(FloorGroupData groupData)
        {
            var floors = new List<FloorDataModel>();
            int i = groupData.MinFloor;
            while (i <= groupData.MaxFloor)
            {
                var floor = new FloorDataModel(i, groupData.RefugeFloors.Any(c => c == i));
                floors.Add(floor);
                i += 1;
            }
            return floors;
        }
    }
    class FloorDataModel
    {
        public int FloorNum { get; }
        public bool IsRefugeFloor { get; set; }
        public FloorDataModel(int floorNum, bool isRefugeFloor)
        {
            this.FloorNum = floorNum;
            this.IsRefugeFloor = isRefugeFloor;
        }
    }
    class FloorGroupData
    {
        public List<int> RefugeFloors { get; }
        public Dictionary<int, int> FloorGroups { get; }
        public Dictionary<int, string> FloorGroupDN { get; }
        public int MinFloor { get; }
        public int MaxFloor { get; }
        public FloorGroupData(int minFloor, int maxFloor)
        {
            this.RefugeFloors = new List<int>();
            this.FloorGroups = new Dictionary<int, int>();
            this.FloorGroupDN = new Dictionary<int, string>();
            this.MinFloor = minFloor;
            this.MaxFloor = maxFloor;
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
            this.Rotate = 0;
            this.Scale = 1;
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
            text ??= "";
            this.LayerName = layerName;
            this.TextStyle = textStyle;
            this.BasePoint = point;
            this.Text = text;
        }
    }
}