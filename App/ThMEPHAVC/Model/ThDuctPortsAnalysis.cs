using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using ThCADCore.NTS;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Algorithm;
using NFox.Cad;
using ThMEPEngineCore.Model.Hvac;
using NetTopologySuite.Geometries;
using ThCADExtension;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsAnalysis
    {
        public Line startLine;
        public Dictionary<Point3d, Point3d> endPoints;// 输入风口为0时用于插入风口断线
        public List<SegInfo> breakedDucts;
        public List<LineGeoInfo> reducings;
        public List<EndlineInfo> endLinesInfos;
        public List<ReducingInfo> reducingInfos;
        public Dictionary<int, SegInfo> mainLinesInfos;
        public List<TextAlignLine> textAlignment;
        public ThShrinkDuct shrinkService;// connector 通过shrink获得
        public List<int> connPort;
        public Dictionary<int, PortInfo> dicPlToAirVolume;
        private bool smokeFlag;
        private Tolerance tor;
        private PortParam portParam;
        // mainLines的第一条线是最远离主路的主路线(最先被累计风量)，最后一条是根线
        private DBObjectCollection mainLines;
        // 存放每一条endline，Collection的第一条线是最末端线，最后一条是最靠近主路的线
        private List<DBObjectCollection> endLines;
        private DBObjectCollection excludeLines;
        private Dictionary<Polyline, ObjectId> allFansDic;
        private ThCADCoreNTSSpatialIndex portIndex;
        // start_point_ == (0,0,0) -> center_lines_ is near (0,0,0)
        // start_point_ != (0,0,0) -> center_lines_ need to move
        public ThDuctPortsAnalysis() { }
        public ThDuctPortsAnalysis(PortParam portParam, DBObjectCollection excludeLines, Dictionary<Polyline, ObjectId> allFansDic)
        {
            Init(portParam, excludeLines, allFansDic);
            GetMainLineAndEndLine();
            if (endLines.Count() == 0)
                throw new NotImplementedException("未搜索到任何中心线");
            if (portParam.genStyle == GenerationStyle.Auto)
                AutoDistributePort();
            else if (portParam.genStyle == GenerationStyle.GenerationWithPortVolume)
            {
                //var conns = DeleteOrgGraph();
                // 1.风口上带风量
                CountEndlinePortAirVolume();
                AccumMainDuctAirVolume();
                SetFirstDuctSize();
                SetMainDuctSize();
                SetEndlinePortDuctSize();
                //ThDuctPortsDrawService.ClearGraphs(conns);
            }
            else if (portParam.genStyle == GenerationStyle.GenerationByPort)
            {
                // 2.风口上是平均风量
                var conns = DeleteOrgGraph();
                CountWithAveragePortAirVolume(conns);
                AccumMainDuctAirVolume();
                SetFirstDuctSize();
                SetMainDuctSize();
                SetEndlinePortDuctSize();
                ThDuctPortsDrawService.ClearGraphs(conns);
            }
            SetLinesShrink();
            SetNoPortDuctInfo();
            // a.分配风口，b.根据风口计算风量，c.根据风量计算风速，d.用风量和风速计算管径
            // 分配或不分配
            // 两种分配方式 1.根据管长比例(位置平均分) 2.根据步长从末端分配(位置从末端管步进)
            // 不分配(位置固定) 1.风口上带风量2.风口上是平均风量
        }
        public double CalcAirVolume(PortParam portParam)
        {
            // 用于UI上读带风量的风口
            Init(portParam, new DBObjectCollection(), new Dictionary<Polyline, ObjectId>());
            GetMainLineAndEndLine();
            CountEndlinePortAirVolume();
            AccumMainDuctAirVolume();
            if (mainLinesInfos.Count > 0)
            {
                var firstDuct = mainLinesInfos.Values.LastOrDefault();
                return firstDuct.airVolume;
            }
            else
            {
                var firstDuct = endLinesInfos.FirstOrDefault();
                return firstDuct.totalAirVolume;
            }
        }
        private List<Handle> DeleteOrgGraph()
        {
            var bounds2IdDic = ThDuctPortsReadComponent.ReadAllTCHComponent(portParam);
            var groupBounds = bounds2IdDic.Keys.ToCollection();
            var index = new ThCADCoreNTSSpatialIndex(groupBounds);
            var conns = new List<Handle>();
            var centerLines = new DBObjectCollection();
            foreach (var lines in endLines)
                foreach (Line l in lines)
                    centerLines.Add(l);
            foreach (Line l in mainLines)
                centerLines.Add(l);
            foreach (Line l in centerLines)
            {
                var pl = CreateLineBound(l);
                var res = index.SelectCrossingPolygon(pl);
                foreach (Polyline p in res)
                    conns.Add(bounds2IdDic[p]);
            }
            var detector = ThMEPHVACService.CreateDetector(Point3d.Origin);
            var detectorRes = index.SelectCrossingPolygon(detector);
            if (detectorRes.Count > 0)
                conns.Add(bounds2IdDic[detectorRes[0] as Polyline]);
            ThModifyPortClear.DeleteTextDimValve(portParam.srtPoint, centerLines);
            return conns;
        }
        public void AutoDistributePort()
        {
            if (portParam.param.portNum > 0)
            {
                DistributePortNum();
                AccumEndlineAirVolume();
                AccumMainDuctAirVolume();
                SetFirstDuctSize();
                SetMainDuctSize();
                SetEndlinePortDuctSize();
            }
            else
            {
                AddInfoToEndline();
                AddInfoToMainDuct();
            }
        }
        public void CreateReducing()
        {
            bool isAxis = false;
            foreach (var r in reducingInfos)
            {
                ThMEPHVACService.GetWidthAndHeight(r.bigSize, out double bWidth, out double bHeight);
                ThMEPHVACService.GetWidthAndHeight(r.smallSize, out double sWidth, out double sHeight);
                var disVec = new Vector3d(0, 0, bHeight);
                var sp = r.l.StartPoint + disVec;
                disVec = new Vector3d(0, 0, sHeight);
                var ep = r.l.EndPoint + disVec;
                reducings.Add(ThDuctPortsFactory.CreateReducing(new Line(sp, ep), bWidth, sWidth, isAxis));
            }
        }

        public void CreatePortDuctGeo()
        {
            foreach (var info in endLinesInfos)
            {
                foreach (var seg in info.endlines.Values)
                {
                    if (seg.portNum < 2)
                    {
                        var shrinkedLine = seg.seg.GetShrinkedLine();
                        var newSeg = new SegInfo() { l = shrinkedLine, ductSize = seg.seg.ductSize, airVolume = seg.seg.airVolume, elevation = seg.seg.elevation };
                        textAlignment.Add(new TextAlignLine() { l = shrinkedLine, ductSize = seg.seg.ductSize, isRoom = true });
                        breakedDucts.Add(newSeg);
                    }
                    else
                        BreakDuctByPort(seg);
                }
            }
        }
        private void BreakDuctByPort(EndlineSegInfo seg)
        {
            var dirVec = ThMEPHVACService.GetEdgeDirection(seg.seg.l);
            var shrinkedLine = seg.seg.GetShrinkedLine();
            // 有布置风口产生的变径
            var nextEndP = shrinkedLine.EndPoint;
            var ductSize = seg.portsInfo[0].ductSize;
            var lastIdx = seg.portNum - 1;
            var mainHeight = ThMEPHVACService.GetHeight(portParam.param.inDuctSize);
            for (int i = 1; i < seg.portNum; ++i)
            {
                if (ductSize != seg.portsInfo[i].ductSize)
                {
                    var curInfo = seg.portsInfo[i];
                    var preInfo = seg.portsInfo[i - 1];
                    var dis = curInfo.position.DistanceTo(preInfo.position);
                    var reducingLen = ThDuctPortsShapeService.GetReducingLen(ThMEPHVACService.GetWidth(ductSize), ThMEPHVACService.GetWidth(seg.portsInfo[i].ductSize));
                    if (dis >= reducingLen)
                    {
                        // 够放1000的变径
                        var midP = ThMEPHVACService.GetMidPoint(curInfo.position, preInfo.position);
                        var curSrtP = midP + dirVec * reducingLen * 0.5;//缩一半
                        var l = new Line(curSrtP, nextEndP);
                        double ductHeight = ThMEPHVACService.GetHeight(preInfo.ductSize);
                        double num = (portParam.param.elevation * 1000 + mainHeight - ductHeight) / 1000;
                        var info = new SegInfo() { l = l, ductSize = preInfo.ductSize, airVolume = preInfo.portAirVolume, elevation = num.ToString() };
                        textAlignment.Add(new TextAlignLine() { l =  l, ductSize = ductSize, isRoom = true });
                        breakedDucts.Add(info);
                        nextEndP = midP - dirVec * reducingLen * 0.5;
                        var reduingLine = new Line(nextEndP, curSrtP);
                        reducingInfos.Add(new ReducingInfo() { l = reduingLine, bigSize = curInfo.ductSize, smallSize = preInfo.ductSize });
                        ductSize = curInfo.ductSize;
                    }
                }
            }
            if (!nextEndP.IsEqualTo(shrinkedLine.StartPoint, tor))
            {
                var curInfo = seg.portsInfo[lastIdx];
                var l = new Line(shrinkedLine.StartPoint, nextEndP);
                var elevation = seg.seg.elevation;
                var info = new SegInfo() { l = l, ductSize = curInfo.ductSize, airVolume = curInfo.portAirVolume, elevation = elevation };
                textAlignment.Add(new TextAlignLine() { l = l, ductSize = curInfo.ductSize, isRoom = true });
                breakedDucts.Add(info);
            }
        }
        private void SetNoPortDuctInfo()
        {
            string ductSize = String.Empty;
            foreach (var endlines in endLinesInfos)
            {
                for (int i = endlines.endlines.Count - 1; i >= 0; --i)//倒着取(从根到末端)
                {
                    var key = endlines.endlines.Keys.ToList()[i];
                    var endline = endlines.endlines[key];
                    for (int j = endline.portNum - 1; j >= 0; --j)//倒着取(从根风口到末端)
                    {
                        var port = endline.portsInfo[j];
                        if (!String.IsNullOrEmpty(port.ductSize))
                            continue;
                        port.ductSize = SelectASize(port.portAirVolume, ductSize);
                        ductSize = port.ductSize;
                    }
                }
            }
        }

        private void SetLinesShrink()
        {
            var centerlines = CreateMainEndlineIndex(out Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic);
            // shrinkService必须到此处再初始化
            shrinkService = new ThShrinkDuct(endLinesInfos, reducingInfos, mainLinesInfos);
            shrinkService.SetLinesShrink(centerlines, dic);
            ShrinkLineWithFan(centerlines);
        }

        private void ShrinkLineWithFan(DBObjectCollection centerlines)
        {
            if (portParam.genStyle == GenerationStyle.GenerationByPort)
            {
                var pl = ThMEPHVACService.CreateDetector(Point3d.Origin);
                var fanIndex = new ThCADCoreNTSSpatialIndex(allFansDic.Keys.ToCollection());
                var res = fanIndex.SelectCrossingPolygon(pl);
                if (res.Count == 1)
                {
                    // 起点与风机相交
                    var centerLineIndex = new ThCADCoreNTSSpatialIndex(centerlines);
                    var crossLines = centerLineIndex.SelectCrossingPolygon(pl);
                    if (crossLines.Count == 1)
                    {
                        var fan = new ThDbModelFan(allFansDic[res[0] as Polyline]);
                        var hoseLen = (fan.scenario == "消防补风" || fan.scenario == "消防排烟" || fan.scenario == "消防加压送风") ? 0 : 200;
                        var l = crossLines[0] as Line;
                        var fanWidth = GetFanConnWidth(fan);
                        var ductWidth = ThMEPHVACService.GetWidth(portParam.param.inDuctSize);
                        var reducingLen = ThDuctPortsShapeService.GetReducingLen(ductWidth, fanWidth);
                        var shrinkLen = reducingLen;// ThFanAnalysis.cs Line: 704(要改一起改)
                        var isAxis = (fan.Name.Contains("轴流风机"));
                        if (mainLinesInfos.ContainsKey(l.GetHashCode()))
                        {
                            mainLinesInfos[l.GetHashCode()].srcShrink = shrinkLen;
                            AddFanReducing(mainLinesInfos[l.GetHashCode()], hoseLen, reducingLen, ductWidth, fanWidth, isAxis);
                        }
                        else
                        {
                            foreach (var endlines in endLinesInfos)
                                if (endlines.endlines.ContainsKey(l.GetHashCode()))
                                {
                                    endlines.endlines[l.GetHashCode()].seg.srcShrink = shrinkLen;
                                    AddFanReducing(endlines.endlines[l.GetHashCode()].seg, hoseLen, reducingLen, ductWidth, fanWidth, isAxis);
                                }
                        }
                    }
                    else
                        throw new NotImplementedException("[CheckError]: centerline doesn't contain Point3d.Origin!");
                }
            }
        }
        private void AddFanReducing(SegInfo firstSeg, double hoseLen, double reducingLen, double ductWidth, double fanWidth, bool isAxis)
        {
            var dirVec = ThMEPHVACService.GetEdgeDirection(firstSeg.l);
            var srtP = firstSeg.l.StartPoint + dirVec * hoseLen;
            var endP = firstSeg.l.StartPoint + dirVec * reducingLen;
            var reducingLine = new Line(endP, srtP);
            reducings.Add(ThDuctPortsFactory.CreateReducing(reducingLine, ductWidth, fanWidth, isAxis));
        }
        private double GetFanConnWidth(ThDbModelFan fan)
        {
            var mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
            var inP = fan.FanInletBasePoint.TransformBy(mat);
            var outP = fan.FanOutletBasePoint.TransformBy(mat);
            var inLen = inP.DistanceTo(Point3d.Origin);
            var outLen = outP.DistanceTo(Point3d.Origin);
            return (inLen < outLen) ? fan.fanInWidth : fan.fanOutWidth;
        }
        private DBObjectCollection CreateMainEndlineIndex(out Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var lines = new DBObjectCollection();
            dic = new Dictionary<int, Dictionary<Point3d, Tuple<double, string>>>();
            // 添加主管段
            foreach (var seg in mainLinesInfos.Values)
            {
                var l = seg.l;
                lines.Add(l);
                dic.Add(l.GetHashCode(), new Dictionary<Point3d, Tuple<double, string>>());
                dic[l.GetHashCode()].Add(l.StartPoint, new Tuple<double, string>(seg.airVolume, seg.ductSize));
                dic[l.GetHashCode()].Add(l.EndPoint, new Tuple<double, string>(seg.airVolume, seg.ductSize));
            }
            foreach (var endlines in endLinesInfos)
            {
                foreach (var endline in endlines.endlines.Values)
                {
                    var l = endline.seg.l;
                    lines.Add(l);
                    dic.Add(l.GetHashCode(), new Dictionary<Point3d, Tuple<double, string>>());
                    var havePort = endline.portNum > 0;
                    var srtVolume = havePort ? endline.portsInfo[index: endline.portNum - 1].portAirVolume : endline.seg.airVolume;
                    var srtSize = havePort? endline.portsInfo[index: endline.portNum - 1].ductSize : endline.seg.ductSize;
                    var endVolume = havePort ? endline.portsInfo[0].portAirVolume : endline.seg.airVolume;
                    var endSize = havePort ? endline.portsInfo[0].ductSize : endline.seg.ductSize;
                    dic[l.GetHashCode()].Add(l.StartPoint, new Tuple<double, string>(srtVolume, srtSize));
                    dic[l.GetHashCode()].Add(l.EndPoint, new Tuple<double, string>(endVolume, endSize));
                }
            }
            return lines;
        }
        private void SetEndlinePortDuctSize()
        {
            if (mainLinesInfos.Count == 0)
            {
                // 只有一条endline,start ductSize = portParam.param.inDuctSize;
                var firstSeg = endLinesInfos[0].endlines.Values.LastOrDefault();
                firstSeg.seg.ductSize = portParam.param.inDuctSize;
                if (firstSeg.portsInfo.Count > 0)
                    firstSeg.portsInfo[firstSeg.portNum - 1].ductSize = portParam.param.inDuctSize;
            }
            foreach (var endlines in endLinesInfos)
            {
                var ductSize = CalcEndlineStartDuctSize(endlines);
                for (int i = endlines.endlines.Count - 1; i >= 0; --i)//倒着取(从根到末端)
                {
                    var key = endlines.endlines.Keys.ToList()[i];
                    var endline = endlines.endlines[key];
                    endline.seg.ductSize = ductSize;//管段没有风口时需要用到
                    endline.seg.elevation = CalcElevation(ductSize);
                    for (int j = endline.portNum - 1; j >= 0; --j)//倒着取(从根风口到末端)
                    {
                        var port = endline.portsInfo[j];
                        if (!String.IsNullOrEmpty(port.ductSize))
                            continue;
                        port.ductSize = SelectASize(port.portAirVolume, ductSize);
                        ductSize = port.ductSize;
                    }
                    if (endline.portNum == 1)
                    {
                        endline.seg.ductSize = ductSize;//有一个风口时可能会对管段产生影响
                        endline.seg.elevation = CalcElevation(ductSize);
                    }
                }
            }
        }
        private string CalcEndlineStartDuctSize(EndlineInfo endlines)
        {
            if (mainLinesInfos.Keys.Count > 0)
            {
                // 有主管段时用主管段限制末管段起始管段
                var mainLines = new DBObjectCollection();
                foreach (var mainInfo in mainLinesInfos.Values)
                    mainLines.Add(mainInfo.l);
                var index = new ThCADCoreNTSSpatialIndex(mainLines);
                var key = endlines.endlines.Keys.ToList()[endlines.endlines.Count - 1];
                var firstEndline = endlines.endlines[key];
                var pl = new Polyline();
                pl.CreatePolygon(firstEndline.seg.l.StartPoint.ToPoint2D(), 4, 10);
                var res = index.SelectCrossingPolygon(pl);
                var crossLine = res[0] as Line;
                var info = mainLinesInfos[crossLine.GetHashCode()];
                return SelectASize(endlines.totalAirVolume, info.ductSize);
            }
            else
            {
                var rootSeg = endlines.endlines.Values.LastOrDefault();
                return rootSeg.seg.ductSize;
            }
        }
        private string CalcElevation(string ductSize)
        {
            var elevation = portParam.param.elevation;
            var mainHeight = ThMEPHVACService.GetHeight(portParam.param.inDuctSize);
            var ductHeight = ThMEPHVACService.GetHeight(ductSize);
            double num = (elevation * 1000 + mainHeight - ductHeight) / 1000;
            return num.ToString();
        }
        private void SetMainDuctSize()
        {
            var infos = mainLinesInfos.Values.ToList();
            int idx = infos.Count - 1;
            string limit = portParam.param.inDuctSize;
            // 倒着遍历，从根到末端设置管段
            for (int i = idx; i >= 0; --i)
            {
                var info = infos[i];
                if (!String.IsNullOrEmpty(info.ductSize))
                    continue;//根管段
                info.ductSize = SelectAMainSize(info.airVolume, limit);
                limit = info.ductSize;
            }
        }
        private string SelectAMainSize(double airVolume, string favorite)
        {
            var ductInfo = new ThDuctParameter(airVolume, portParam.param.scenario);
            foreach (var size in ductInfo.DuctSizeInfor.DefaultDuctsSizeString)
                if (size == favorite)
                    return size;
            ThMEPHVACService.GetWidthAndHeight(favorite, out double inW, out double inH);
            ductInfo = (!ThDuctParameter.ductMods.Contains(inW) || 
                        !ThDuctParameter.ductHeights.Contains(inH)) ?
                new ThDuctParameter(airVolume, portParam.param.scenario, inW, inH) :
                new ThDuctParameter(airVolume, portParam.param.scenario);
            return SelectClostRatioSize(ductInfo, favorite, airVolume);
        }
        private string SelectASize(double airVolume, string favorite)
        {
            var ductInfo = new ThDuctParameter(airVolume, portParam.param.scenario);
            foreach (var size in ductInfo.DuctSizeInfor.DefaultDuctsSizeString)
                if (size == favorite)
                    return size;
            return SelectClostRatioSize(ductInfo, favorite, airVolume);
        }
        private string SelectClostRatioSize(ThDuctParameter ductInfo, string favorite, double airVolume)
        {
            ThMEPHVACService.GetWidthAndHeight(favorite, out double inW, out double inH);
            string s = String.Empty;
            double minRatio = Double.MaxValue;
            var r = GetWHRatio(airVolume, portParam.param.scenario);
            foreach (var size in ductInfo.DuctSizeInfor.DefaultDuctsSizeString)
            {
                ThMEPHVACService.GetWidthAndHeight(size, out double w, out double h);
                var ratio = w / h;
                if (h <= inH && w <= inW)
                {
                    var t = Math.Abs(ratio - r);
                    if (t < minRatio)
                    {
                        minRatio = t;
                        s = size;
                    }
                }
            }
            return (!s.IsNullOrEmpty()) ? s : favorite;
        }

        public double GetWHRatio(double airVolume, string scenario)
        {
            if ((scenario.Contains("排烟") && !scenario.Contains("兼")) || scenario == "消防加压送风" || scenario == "消防补风")
            {
                if (airVolume >= 100000) { return 6; }
                else if (airVolume >= 80000) { return 5; }
                else if (airVolume >= 50000) { return 3.5; }
                else if (airVolume >= 8000) { return 3.5; }
                else { return 2.5; }
            }
            else if (scenario == "厨房排油烟")
            {
                if (airVolume >= 60000) { return 6; }
                else if (airVolume >= 50000) { return 5; }
                else if (airVolume >= 40000) { return 4; }
                else if (airVolume >= 30000) { return 3.5; }
                else if (airVolume >= 5000) { return 3.5; }
                else { return 2.5; }
            }
            else
            {
                if (airVolume >= 40000) { return 6; }
                else if (airVolume >= 35000) { return 5; }
                else if (airVolume >= 30000) { return 4; }
                else if (airVolume >= 12000) { return 3.5; }
                else if (airVolume >= 2000) { return 3.5; }
                else { return 2.5; }
            }
        }
        private void SetFirstDuctSize()
        {
            if (mainLines.Count > 0)
            {
                var rootSeg = mainLinesInfos.Values.LastOrDefault();
                rootSeg.ductSize = portParam.param.inDuctSize;
            }
            else
            {
                // 只有一条末端管
                var rootSeg = endLinesInfos[0].endlines.Values.LastOrDefault();
                rootSeg.seg.ductSize = portParam.param.inDuctSize;
            }
        }
        private void AddInfoToMainDuct()
        {
            if (mainLines.Count > 0)
            {
                var airVolume = portParam.param.airVolume;
                var ductSize = portParam.param.inDuctSize;
                foreach (Line l in mainLines)
                    mainLinesInfos.Add(l.GetHashCode(), new SegInfo() { airVolume = airVolume, l = l, ductSize = ductSize });
            }
        }
        private void AccumMainDuctAirVolume()
        {
            if (mainLines.Count > 0)
            {
                if (smokeFlag && portParam.genStyle == GenerationStyle.GenerationWithPortVolume)
                    AccMainDuctWithExhaust();
                else
                    AccMainDuctWithNotExhaust();
            }
        }
        private void AccMainDuctWithExhaust()
        {
            var endSegs = new DBObjectCollection();
            foreach (var lines in endLines)
                foreach (Line l in lines)
                    endSegs.Add(l);
            foreach (Line l in mainLines)
                endSegs.Add(l);
            var dic = CountSmokeZoneAirVolume(endSegs, out DBObjectCollection smokeMpBounds);
            var smokeIndex = new ThCADCoreNTSSpatialIndex(smokeMpBounds);
            double k = 1;
            foreach (Line l in mainLines)
            {
                endSegs.Remove(l);
                double airVolume = GetSucceedSmokeZoneAirVolume(endSegs, l, smokeIndex, dic, out MPolygon curSmokeZone);
                double preAirVolume = GetSameSmokeZonePreAirVolume(endSegs, l, curSmokeZone);
                airVolume -= preAirVolume;
                mainLinesInfos.Add(l.GetHashCode(), new SegInfo() { airVolume = airVolume + (k++), l = l });
                endSegs.Add(l);
            }
        }
        private double GetSameSmokeZonePreAirVolume(DBObjectCollection lines, 
                                                    Line l, 
                                                    MPolygon curSmokeZone)
        {
            if (curSmokeZone.Area < 10)
                return 0;
            var detector = new ThFanCenterLineDetector(true);
            detector.SearchCenterLine(lines, l.StartPoint, l);
            var crossPort = portIndex.SelectCrossingPolygon(curSmokeZone);
            var linesIndex = new ThCADCoreNTSSpatialIndex(detector.connectLines);
            double airVolume = 0;
            foreach (MPolygon portBound in crossPort)
            {
                var portSet = linesIndex.SelectCrossingPolygon(portBound);
                if (portSet.Count > 0)
                    airVolume += dicPlToAirVolume[portBound.GetHashCode()].portAirVolume;
            }
            return airVolume;
        }

        private double GetSucceedSmokeZoneAirVolume(DBObjectCollection lines, 
                                        Line l, 
                                        ThCADCoreNTSSpatialIndex smokeIndex, 
                                        Dictionary<int, double> dicZone2AirVolume,
                                        out MPolygon curSmokeZone)
        {
            var detector = new ThFanCenterLineDetector(true);
            detector.SearchCenterLine(lines, l.EndPoint, l);
            var airVolumes = new List<double>();
            var set = new HashSet<int>();
            curSmokeZone = new MPolygon();
            bool flag = true;
            foreach (Line connLine in detector.connectLines)
            {
                var polyline = connLine.Buffer(1);
                var crossPort = portIndex.SelectCrossingPolygon(polyline);
                if (crossPort.Count > 0)
                {
                    var portBounds = crossPort[0] as MPolygon;
                    var res = smokeIndex.SelectCrossingPolygon(portBounds);
                    foreach (MPolygon pl in res)
                    {
                        if (flag)
                        {
                            curSmokeZone = pl;
                            flag = false;// 第一个交到风口的防烟分区
                        }
                        if (set.Add(pl.GetHashCode()))
                        {
                            airVolumes.Add(dicZone2AirVolume[pl.GetHashCode()]);
                        }
                    }
                }
            }
            if (airVolumes.Count > 0)
            {
                airVolumes.Sort();
                return airVolumes.Count > 1 ? (airVolumes[airVolumes.Count - 1] + airVolumes[airVolumes.Count - 2]) : airVolumes[0];
            }
            else
                return 0;
        }

        private void AccMainDuctWithNotExhaust()
        {
            var lines = CreateEndlineIndex(out Dictionary<int, double> dicLineToParam);
            var index = new ThCADCoreNTSSpatialIndex(lines);
            var selectMaxFlag = portParam.param.scenario.Contains("消防加压送风");
            foreach (Line l in mainLines)
            {
                var pl = new Polyline();
                pl.CreatePolygon(l.EndPoint.ToPoint2D(), 4, 10);
                var res = index.SelectCrossingPolygon(pl);
                var airVolume = selectMaxFlag ? GetMaxAirVolume(res, dicLineToParam) : AccumAirVolume(res, dicLineToParam);
                mainLinesInfos.Add(l.GetHashCode(), new SegInfo() { airVolume = airVolume, l = l });
                lines.Add(l);
                index = new ThCADCoreNTSSpatialIndex(lines);
                dicLineToParam.Add(l.GetHashCode(), airVolume);
            }
        }
        private DBObjectCollection GetSmokeZone(DBObjectCollection smokeLines)
        {
            var t = new DBObjectCollection();
            foreach (Line l in smokeLines)
            {
                var dirVec = ThMEPHVACService.GetEdgeDirection(l) * 5;
                var exL = new Line(l.StartPoint - dirVec, l.EndPoint + dirVec);
                t.Add(exL);
            }
            var zone = t.Polygonize();
            var bounds = new DBObjectCollection();
            foreach (Polygon pl in zone)
            {
                if (pl.Area > 100)
                {
                    var e = pl.ToDbEntity();
                    if (e is MPolygon)
                    {
                        var mpl = e as MPolygon;
                        CovertMPolygon2Polyline(mpl, bounds);
                    }
                    else
                    {
                        bounds.Add(e);
                    }
                }
            }
            return bounds;
        }
        private void CovertMPolygon2Polyline(MPolygon mpl, DBObjectCollection bounds)
        {
            bounds.Add(mpl.Shell());
            var holes = mpl.Holes();
            foreach (Polyline hole in holes)
                bounds.Add(hole);
        }
        private double AccumAirVolume(DBObjectCollection lines, Dictionary<int, double> dicLineToParam)
        {
            double airVolume = 0;
            foreach (Line c in lines)
                airVolume += dicLineToParam[c.GetHashCode()];
            return airVolume;
        }

        private double GetMaxAirVolume(DBObjectCollection lines, Dictionary<int, double> dicLineToParam)
        {
            double maxAirVolume = 0;
            foreach (Line c in lines)
            {
                var vol = dicLineToParam[c.GetHashCode()];
                if (maxAirVolume < vol)
                    maxAirVolume = vol;
            }
            return maxAirVolume + 1;// +1是为了区分当风量相同时的主管段
        }

        private DBObjectCollection CreateEndlineIndex(out Dictionary<int, double> dicLineToParam)
        {
            var lines = new DBObjectCollection();
            dicLineToParam = new Dictionary<int, double>();
            foreach (var endline in endLinesInfos)
            {
                var srtInfo = endline.endlines.Values.LastOrDefault<EndlineSegInfo>();
                lines.Add(srtInfo.seg.l);
                dicLineToParam.Add(srtInfo.seg.l.GetHashCode(), srtInfo.seg.airVolume);
            }
            return lines;
        }
        private void AccumEndlineAirVolume()
        {
            foreach (var endline in endLinesInfos)
            {
                double accAirVolume = 0;
                foreach (var endlineSeg in endline.endlines.Values)
                {
                    foreach (var port in endlineSeg.portsInfo)
                    {
                        port.portAirVolume += accAirVolume;
                        accAirVolume = port.portAirVolume;
                    }
                    endlineSeg.seg.airVolume = accAirVolume;
                }
                endline.totalAirVolume = accAirVolume;
            }
        }
          
        private void DistributePortNum()
        {
            if (Math.Abs(portParam.portInterval) < 1e-3)
            {
                //1.根据管长比例(位置平均分)
                DistributePortByDuctRatio();
            }
            else
            {
                //2.根据步长从末端分配(位置从末端管步进)
                DistributePortByStep();
            }
        }
        private void RecordPortInfo(int portNum,
                                    List<Handle> deleteList,
                                    List<Polyline> crossPortBounds,
                                    Dictionary<Polyline, PortModifyParam> dicPlToPort,
                                    Dictionary<Point3d, List<Handle>> sidePortHandle)
        {
            portParam.param.portNum = portNum;
            double avgAirVolume = portParam.param.airVolume / portNum;
            avgAirVolume = (Math.Ceiling(avgAirVolume / 10)) * 10;
            foreach (Polyline crossPorts in crossPortBounds)
            {
                var port = dicPlToPort[crossPorts];
                deleteList.Add(port.handle);
                port.portAirVolume = avgAirVolume;
                portParam.param.portRange = port.portRange;
                var p = ThMEPHVACService.RoundPoint(crossPorts.GetCentroidPoint(), 6);
                if (sidePortHandle.ContainsKey(p))
                    deleteList.AddRange(sidePortHandle[p]);
            }
        }
        private void CountWithAveragePortAirVolume(List<Handle> deleteList)
        {
            ThDuctPortsReadComponent.GetCenterPortBounds(portParam, 
                                                        out Dictionary <Polyline, PortModifyParam> dicPlToPort,
                                                        out Dictionary<Point3d, List<Handle>> sidePortHandle);
            var portIndex = new ThCADCoreNTSSpatialIndex(dicPlToPort.Keys.ToCollection());
            int portNum = CountAirVolume(portIndex, out List<Polyline> crossPortBounds);
            RecordPortInfo(portNum, deleteList, crossPortBounds, dicPlToPort, sidePortHandle);
            foreach (DBObjectCollection lines in endLines)
            {
                var endline = new Dictionary<int, EndlineSegInfo>();
                double totalAirVolume = 0;
                foreach (Line l in lines)
                {
                    var pl = CreateLineBound(l);
                    var res = portIndex.SelectCrossingPolygon(pl);
                    var p = new EndlineSegInfo() { portNum = res.Count };
                    p.seg = new SegInfo() { l = l };
                    p.portsInfo = new List<PortInfo>();
                    // 最末端的风口放在第一个，方便累加风量
                    var sortedDis = SortByDistance(l.EndPoint, res, dicPlToPort, out Dictionary<string, PortModifyParam> dic);
                    foreach (var dis in sortedDis)
                    {
                        var info = dic[dis.ToString()];
                        totalAirVolume += info.portAirVolume;
                        p.portsInfo.Add(new PortInfo() { portAirVolume = totalAirVolume, position = info.pos });
                    }
                    p.seg.airVolume = totalAirVolume;
                    endline.Add(l.GetHashCode(), p);
                }
                endLinesInfos.Add(new EndlineInfo() { endlines = endline, totalAirVolume = totalAirVolume });
            }

        }
        private int CountAirVolume(ThCADCoreNTSSpatialIndex portIndex, out List<Polyline> crossPortBounds)
        {
            int count = 0;
            crossPortBounds = new List<Polyline>();
            foreach (DBObjectCollection lines in endLines)
            {
                foreach (Line l in lines)
                {
                    var pl = CreateLineBound(l);
                    var res = portIndex.SelectCrossingPolygon(pl);
                    crossPortBounds.AddRange(res.Cast<Polyline>());
                    count += res.Count;
                }
            }
            return count;
        }

        private Dictionary<int, double> CountSmokeZoneAirVolume(DBObjectCollection centerlines, out DBObjectCollection smokeMpBounds)
        {
            var index = new ThCADCoreNTSSpatialIndex(centerlines);
            smokeMpBounds = new DBObjectCollection();
            var smokeLines = ThDuctPortsReadComponent.ReadSmokeLine();
            var mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
            foreach (Curve c in smokeLines)
                c.TransformBy(mat);
            var smokeBounds = GetSmokeZone(smokeLines);
            var dicSmokeZone = new Dictionary<int, double>();
            foreach (Polyline bounds in smokeBounds)
            {
                var t = new DBObjectCollection() { bounds };
                var mp = t.BuildMPolygon();
                smokeMpBounds.Add(mp);
                var res = portIndex.SelectCrossingPolygon(mp);
                double zoneAirVolume = 0;
                foreach (MPolygon p in res)
                {
                    var crossLine = index.SelectCrossingPolygon(p);
                    if (crossLine.Count > 0)
                        zoneAirVolume += dicPlToAirVolume[p.GetHashCode()].portAirVolume;
                }
                dicSmokeZone.Add(mp.GetHashCode(), zoneAirVolume);
            }
            return dicSmokeZone;
        }
        private void CountEndlinePortAirVolume()
        {
            connPort = new List<int>();
            foreach (DBObjectCollection lines in endLines)
            {
                var endline = new Dictionary<int, EndlineSegInfo>();
                double totalAirVolume = 0;
                foreach (Line l in lines)
                {
                    var pl = CreateLineBound(l);
                    var res = portIndex.SelectCrossingPolygon(pl);
                    var p = new EndlineSegInfo() { portNum = res.Count };
                    p.seg = new SegInfo() { l = l };
                    p.portsInfo = new List<PortInfo>();
                    // 最末端的风口放在第一个，方便累加风量
                    var sortedDis = SortByDistance(l.EndPoint, res, out Dictionary<string, PortInfo> dic);
                    foreach (var dis in sortedDis)
                    {
                        var info = dic[dis.ToString()];
                        totalAirVolume += info.portAirVolume;
                        p.portsInfo.Add(new PortInfo() { portAirVolume = totalAirVolume });
                    }
                    p.seg.airVolume = totalAirVolume;
                    endline.Add(l.GetHashCode(), p);
                }
                endLinesInfos.Add(new EndlineInfo() { endlines = endline, totalAirVolume = totalAirVolume });
            }
        }

        private Polyline CreateLineBound(Line l)
        {
            if (portParam.param.portName.Contains("侧"))
            {
                var dirVec = ThMEPHVACService.GetEdgeDirection(l);
                var extLine = new Line(l.StartPoint - dirVec, l.EndPoint + dirVec);
                var w = ThMEPHVACService.GetWidth(portParam.param.inDuctSize) * 0.5;
                return extLine.Buffer(w);
            }
            else
            {
                return l.Buffer(1);
            }
        }
        private List<double> SortByDistance(Point3d baseP,
                                            DBObjectCollection res,
                                            out Dictionary<string, PortInfo> dic)
        {
            var distances = new List<double>();
            dic = new Dictionary<string, PortInfo>();
            foreach (MPolygon pl in res)
            {
                connPort.Add(pl.GetHashCode());
                var info = dicPlToAirVolume[pl.GetHashCode()];
                var dis = info.position.DistanceTo(baseP);
                dic.Add(dis.ToString(), info);
                distances.Add(dis);
            }
            distances.Sort();
            return distances;
        }
        private List<double> SortByDistance(Point3d baseP,
                                            DBObjectCollection res, 
                                            Dictionary<Polyline, PortModifyParam> dicPlToAirVolume,
                                            out Dictionary<string, PortModifyParam> dic)
        {
            var distances = new List<double>();
            dic = new Dictionary<string, PortModifyParam>();
            foreach (Polyline pl in res)
            {
                var info = dicPlToAirVolume[pl];
                var dis = info.pos.DistanceTo(baseP);
                dic.Add(dis.ToString(), info);
                distances.Add(dis);
            }
            distances.Sort();
            return distances;
        }
        private void DistributePortByStep()
        {
            if (endLines.Count != 1)
                throw new NotImplementedException("[CheckError]: Step generation with multi branch!");
            double avgAirVolume = portParam.param.airVolume / portParam.param.portNum;
            var lines = endLines[0];
            var endline = new Dictionary<int, EndlineSegInfo>();
            var set = new HashSet<int>();
            DistributePort(endline, lines, set);
            DistributeRemainLine(endline, lines, set);
            foreach (var endlineInfo in endline.Values)
            {
                endlineInfo.portsInfo = new List<PortInfo>();
                for (int i = 0; i < endlineInfo.portNum; ++i)
                    endlineInfo.portsInfo.Add(new PortInfo() { portAirVolume = avgAirVolume });
            }
            endLinesInfos.Add(new EndlineInfo() { endlines = endline } );
        }
        private void DistributeRemainLine(Dictionary<int, EndlineSegInfo> info, DBObjectCollection lines, HashSet<int> set)
        {
            if (info.Count < lines.Count)
            {
                foreach (Line l in lines)
                {
                    if (set.Contains(l.GetHashCode()))
                        continue;
                    var p = new EndlineSegInfo() { portNum = 0 };
                    p.seg = new SegInfo() { l = l };
                    info.Add(l.GetHashCode(), p);
                }
            }
        }
        private void DistributePort(Dictionary<int, EndlineSegInfo> info, DBObjectCollection lines, HashSet<int> set)
        {
            int distributePortNum = 0;
            foreach (Line l in lines)
            {
                set.Add(l.GetHashCode());
                if (IsExcludeLine(l))
                {
                    var p = new EndlineSegInfo() { portNum = 0 };
                    p.seg = new SegInfo() { l = l };
                    info.Add(l.GetHashCode(), p);
                    continue;
                }
                int portNum = (int)Math.Ceiling(l.Length / portParam.portInterval);
                distributePortNum += portNum;
                if (distributePortNum >= portParam.param.portNum)
                {
                    portNum -= (distributePortNum - portParam.param.portNum);
                    var p = new EndlineSegInfo() { portNum = portNum};
                    p.seg = new SegInfo() { l = l };
                    info.Add(l.GetHashCode(), p);
                    break;
                }
                var param = new EndlineSegInfo() { portNum = portNum};
                param.seg = new SegInfo() { l = l };
                info.Add(l.GetHashCode(), param);
            }
        }
        private void DistributePortByDuctRatio()
        {
            double totalLen = GetTotalLen();
            if (Math.Abs(totalLen) < 1e-3)
                return;
            DistributePort(totalLen, out int distributePortNum);
            DistributeRemainPort(distributePortNum);
        }
        private double GetTotalLen()
        {
            double totalLen = 0;
            foreach (DBObjectCollection lines in endLines)
                foreach (Line l in lines)
                    totalLen += l.Length;
            return totalLen;
        }
        private void AddInfoToEndline()
        {
            foreach (DBObjectCollection lines in endLines)
            {
                var endline = new Dictionary<int, EndlineSegInfo>();
                var airVolume = portParam.param.airVolume;
                foreach (Line l in lines)
                {
                    var p = new EndlineSegInfo() { portNum = 0 };
                    p.seg = new SegInfo() { l = l };
                    p.portsInfo = new List<PortInfo>();
                    p.seg.ductSize = portParam.param.inDuctSize;
                    p.seg.airVolume = airVolume;
                    endline.Add(l.GetHashCode(), p);
                }
                endLinesInfos.Add(new EndlineInfo() { endlines = endline, totalAirVolume = airVolume });
            }
        }
        private void DistributePort(double totalLen, out int distributePortNum)
        {
            distributePortNum = 0;
            double avgAirVolume = portParam.param.airVolume / portParam.param.portNum;
            avgAirVolume = (Math.Ceiling(avgAirVolume / 10)) * 10;
            foreach (DBObjectCollection lines in endLines)
            {
                var endline = new Dictionary<int, EndlineSegInfo>();
                foreach (Line l in lines)
                {
                    if (IsExcludeLine(l))
                    {
                        var p = new EndlineSegInfo() { portNum = 0};
                        p.seg = new SegInfo() { l = l };
                        p.portsInfo = new List<PortInfo>();
                        endline.Add(l.GetHashCode(), p);
                        continue;
                    }
                    int portNum = (int)(portParam.param.portNum * l.Length / totalLen);
                    portNum = (portNum == 0) ? 1 : portNum;
                    distributePortNum += portNum;
                    var param = new EndlineSegInfo() { portNum = portNum};
                    param.seg = new SegInfo() { l = l };
                    param.portsInfo = new List<PortInfo>();
                    for (int i = 0; i < portNum; ++i)
                        param.portsInfo.Add(new PortInfo() { portAirVolume = avgAirVolume});
                    endline.Add(l.GetHashCode(), param);
                }
                endLinesInfos.Add(new EndlineInfo() { endlines = endline });
            }
        }
        private void DistributeRemainPort(int distributePortNum)
        {
            int remainPortNum = portParam.param.portNum - distributePortNum;
            double avgAirVolume = portParam.param.airVolume / portParam.param.portNum;
            avgAirVolume = (Math.Ceiling(avgAirVolume / 10)) * 10;
            while (remainPortNum > 0)
            {
                foreach (var endline in endLinesInfos)
                {
                    foreach (var seg in endline.endlines.Values)
                    {
                        if (IsExcludeLine(seg.seg.l))
                            continue;
                        // 将剩余的风口平均分到每一段的末端管
                        seg.portNum++;
                        seg.portsInfo.Add(new PortInfo() { portAirVolume = avgAirVolume });
                        remainPortNum--;
                        break;
                    }
                    if (remainPortNum == 0)
                        break;
                }
            }
        }
        private bool IsExcludeLine(Line shadow)
        {
            foreach (Line l in excludeLines)
                if (ThMEPHVACService.IsSameLine(l, shadow))
                    return true;
            return false;
        }
        private void GetMainLineAndEndLine()
        {
            endLines = new List<DBObjectCollection>();
            mainLines = new DBObjectCollection();
            endPoints = new Dictionary<Point3d, Point3d>();
            // GetAllEndPoint
            var pointDetector = new ThFanCenterLineDetector(false);
            var tmpEndLines = new DBObjectCollection();
            // portParam.srtDisVec ThHvacCmdService.cs Line : 239更新
            var srtP = Point3d.Origin + portParam.srtDisVec;// 有上下翻时需要更新起始点
            pointDetector.SearchCenterLine(portParam.centerLines, ref srtP, SearchBreakType.breakWithEndline);
            startLine = pointDetector.srtLine;
            foreach (var p in pointDetector.endPoints.Keys)
            {
                var endLineDetector = new ThFanCenterLineDetector(true);// 保持原线的走向
                // 搜索末端点到三通四通的所有点
                var pp = p;
                endLineDetector.SearchCenterLine(pointDetector.connectLines, ref pp, SearchBreakType.breakWithTeeAndCross);
                var set = new DBObjectCollection();
                var orgLines = new DBObjectCollection();
                for (int i = endLineDetector.connectLines.Count - 1; i >= 0; --i)
                {
                    var l = endLineDetector.connectLines[i];
                    orgLines.Add(l);
                    tmpEndLines.Add(l);
                    set.Add(l.Clone() as Line);
                }
                if (portParam.genStyle == GenerationStyle.Auto)
                    endLines.Add(set);
                else
                {
                    // 如果末端线与风口有交则添加，否则从搜索的中心线中删除所有到三通的线，主管段上共线的线也可以被过滤掉
                    FilterNoPortCenterLine(orgLines, set, pointDetector.connectLines, p, pointDetector.endPoints[p]);
                }
            }
            mainLines = pointDetector.connectLines;
            foreach (Line l in tmpEndLines)
                mainLines.Remove(l);
            if (mainLines.Count == 0 && endLines.Count == 0)
                endLines.Add(new DBObjectCollection() { startLine });
        }
        private void FilterNoPortCenterLine(DBObjectCollection orgLines, 
                                            DBObjectCollection addLines,
                                            DBObjectCollection hasDetectConnLines,
                                            Point3d p,
                                            Point3d otherP)
        {
            // endLines 已经被初始化过了
            if (addLines.Count > 0)
            {
                var l = addLines[0] as Line;// 0是最末端线
                var pl = l.Buffer(1);
                var res = portIndex.SelectCrossingPolygon(pl);
                if (res.Count > 0)
                {
                    endLines.Add(addLines);
                    endPoints.Add(p, otherP);
                }
                else
                {
                    foreach (Line line in orgLines)
                        hasDetectConnLines.Remove(line);
                    endPoints.Remove(p);
                }
            }
        }
        private void Init(PortParam portParam, DBObjectCollection excludeLines, Dictionary<Polyline, ObjectId> allFansDic)
        {
            tor = new Tolerance(1.5, 1.5);
            this.portParam = portParam;
            this.allFansDic = allFansDic;
            this.excludeLines = excludeLines;
            breakedDucts = new List<SegInfo>();
            reducings = new List<LineGeoInfo>();
            endLinesInfos = new List<EndlineInfo>();
            reducingInfos = new List<ReducingInfo>();
            mainLinesInfos = new Dictionary<int, SegInfo>();
            textAlignment = new List<TextAlignLine>();
            smokeFlag = portParam.param.scenario.Contains("排烟") && !portParam.param.scenario.Contains("兼");
            var portBounds = ThDuctPortsReadComponent.GetPortBoundsByPortAirVolume(portParam, out dicPlToAirVolume);
            portIndex = new ThCADCoreNTSSpatialIndex(portBounds);
        }
    }
}