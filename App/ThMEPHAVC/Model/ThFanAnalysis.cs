using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;    
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Algorithm;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.CAD
{
    class LineCompare : IEqualityComparer<Line>
    {
        public Tolerance tor;
        public LineCompare(Tolerance tor)
        {
            this.tor = tor;
        }
        public bool Equals(Line x, Line y)
        {
            return (x.StartPoint.IsEqualTo(y.StartPoint, tor) && x.EndPoint.IsEqualTo(y.EndPoint, tor)) ||
                   (x.StartPoint.IsEqualTo(y.EndPoint, tor) && x.EndPoint.IsEqualTo(y.StartPoint, tor));
        }
        public int GetHashCode(Line obj)
        {
            return 0;
        }
    }
    
    public class BypassTee
    {
        public Line inLine;
        public Line otherLine;
        public Line bypass;
        public Point3d crossP;
    }
    public class ReducingWithType
    {
        public bool isRoom;
        public LineGeoInfo bounds;
    }
    public class EntityWithType
    {
        public bool isRoom;
        public EntityModifyParam entity;
    }
    public class ThFanAnalysis
    {
        public Tolerance tor;
        public Point3d moveSrtP;
        public Point3d fanBreakP;
        public FanParam fanParam;
        public ThDbModelFan fan;
        public PortParam portParam;
        public List<SegInfo> UpDownVertivalPipe;          // 上下翻立管
        public List<Line> auxLines;                       // 风机进出口到起始搜索点的线段
        public HashSet<Line> roomLines;
        public HashSet<Line> notRoomLines;
        public Dictionary<int, SegInfo> centerLines;      // 不直接用Line是因为src和dst的shrink是不同时间获得的
        public List<ReducingWithType> reducings;
        public List<TextAlignLine> textRoomAlignment;
        public List<TextAlignLine> textNotRoomAlignment;
        public List<EntityWithType> specialShapesInfo;
        public DBObjectCollection bypass;
        public DBObjectCollection outCenterLine;
        public ThVTee vt;
        public Point3d roomVtPos;
        public Point3d notRoomVtPos;
        private bool isExhaust;
        private double ioBypassSepDis;
        private List<BypassTee> bypassTees;
        private ThCADCoreNTSSpatialIndex spatialIndex;
        private ThDuctPortsDrawService service;
        public ThShrinkDuct shrinkService;
        public ThFanAnalysis(double ioBypassSepDis,
                             ThDbModelFan fan,
                             FanParam param,
                             PortParam portParam,
                             DBObjectCollection bypass,
                             DBObjectCollection wallLines,
                             bool haveMultiFan,
                             ThDuctPortsDrawService service)
        {
            Init(ioBypassSepDis, fan, param, bypass, portParam, service);
            MoveToZero(fan.FanInletBasePoint, fan.FanOutletBasePoint, param.centerLines, wallLines, out Point3d roomP, out Point3d notRoomP);
            MergeBypassCenterLine(ref param.centerLines, bypass);
            UpdateSearchPoint(roomP, notRoomP, param, ref param.centerLines, out Point3d iRoomP, out Point3d iNotRoomP, out Line roomLine, out Line notRoomLine);
            auxLines = new List<Line>() { new Line(roomP, iRoomP) , new Line(notRoomP, iNotRoomP) };
            spatialIndex = new ThCADCoreNTSSpatialIndex(param.centerLines);
            PreSearchConnLines(iRoomP, roomLine, iNotRoomP, notRoomLine, param.roomEnable, param.notRoomEnable);
            var buffer = BufferCenterLine(roomLine, notRoomLine);
            SeperateFanInsideAndOutSide(wallLines, iRoomP, roomP);
            ClearRoomLines(wallLines.Count);
            SearchConnectors(iRoomP, roomLine, iNotRoomP, notRoomLine, param.roomEnable, param.notRoomEnable);
            CollectLines();
            ShrinkDuct();
            MergeBrokenBypass();
            MoveToOrg();
            if (!haveMultiFan)
                AddNotRoomEndComp();
            AddTextAlignLine(haveMultiFan);
            if (bypass.Count == 0 && param.bypassSize != null)
            {
                GetVtElbowPos(iRoomP, iNotRoomP, buffer);
                vt = new ThVTee(roomVtPos, notRoomVtPos, param, fan.installStyle);
            }
            param.centerLines.Clear();
            param.centerLines = outCenterLine;
            portParam.srtPoint = fanBreakP;
        }

        private DBObjectCollection BufferCenterLine(Line roomLine, Line notRoomLine)
        {
            // 将更新后的线也加进来，会多两条线，但是保证找
            var lines = new DBObjectCollection
            {
                roomLine,
                notRoomLine
            };
            return lines;
        }

        private void ClearRoomLines(int wallCount)
        {
            if (bypass.Count == 0 && wallCount == 0)
            {
                roomLines.Clear();// 无旁通和墙线时风机房内服务侧的管段用风平面工具生成(否则风机房生成管段重叠)
            }
        }
        private void AddTextAlignLine(bool haveMultiFan)
        {
            foreach (Line l in roomLines)
            {
                if (IsBypass(l))
                    continue;// 旁通单独标
                textRoomAlignment.Add(new TextAlignLine() { l = l, ductSize = fanParam.roomDuctSize, isRoom = true });
            }
            if (outCenterLine.Count == 0 || !haveMultiFan)
            {
                foreach (Line l in notRoomLines)
                {
                    if (IsBypass(l))
                        continue;// 旁通单独标
                    textNotRoomAlignment.Add(new TextAlignLine() { l = l, ductSize = fanParam.notRoomDuctSize, isRoom = false });
                }
            }
        }
        private void Init(double ioBypassSepDis, 
                          ThDbModelFan fan, 
                          FanParam param, 
                          DBObjectCollection bypass, 
                          PortParam portParam,
                          ThDuctPortsDrawService service)
        {
            this.service = service;
            this.fan = fan;
            this.fanParam = param;
            this.bypass = bypass;
            this.portParam = portParam;
            this.ioBypassSepDis = ioBypassSepDis;
            isExhaust = fan.isExhaust;
            moveSrtP = isExhaust ? fan.FanInletBasePoint : fan.FanOutletBasePoint;
            bypassTees = new List<BypassTee>();
            tor = new Tolerance(1.5, 1.5);
            var comp = new LineCompare(tor);
            roomLines = new HashSet<Line>(comp);
            notRoomLines = new HashSet<Line>(comp);
            textRoomAlignment = new List<TextAlignLine>();
            textNotRoomAlignment = new List<TextAlignLine>();
            reducings = new List<ReducingWithType>();
            centerLines = new Dictionary<int, SegInfo>();
            outCenterLine = new DBObjectCollection();
            UpDownVertivalPipe = new List<SegInfo>();
            specialShapesInfo =  new List<EntityWithType>();
        }
        private void AddNotRoomEndComp()
        {
            var rootDuct = centerLines.Values.ToList().LastOrDefault();
            var insertP = rootDuct.l.EndPoint;
            var dirVec = (rootDuct.l.EndPoint - rootDuct.l.StartPoint).GetNormal();
            if (portParam.genStyle == GenerationStyle.Auto)
                ThNotRoomStartComp.InsertComp(rootDuct, dirVec, moveSrtP, insertP, portParam, service);
        }

        private void SearchConnectors(Point3d iRoomP,
                                      Line roomLine,
                                      Point3d iNotRoomP,
                                      Line notRoomLine,
                                      bool roomEnable,
                                      bool notRoomEnable)
        {
            // 加压送风需要添加旁通后重新搜索连接件
            if (fanParam.scenario == "消防加压送风")
            {
                if (roomEnable && notRoomEnable)
                {
                    GetSpecialShapeInfo(iRoomP, roomLine, roomLines, fanParam.roomDuctSize);
                    GetSpecialShapeInfo(iNotRoomP, notRoomLine, notRoomLines, fanParam.notRoomDuctSize);
                }
                else if (roomEnable && !notRoomEnable)
                {
                    GetSpecialShapeInfo(iRoomP, roomLine, roomLines, fanParam.roomDuctSize);
                }
                else if (!roomEnable && notRoomEnable)
                {
                    GetSpecialShapeInfo(iNotRoomP, notRoomLine, notRoomLines, fanParam.notRoomDuctSize);
                }
                else
                    throw new NotImplementedException("未选择要生成室外侧或服务侧！！！");
            }
        }

        private void PreSearchConnLines(Point3d iRoomP, 
                                        Line roomLine, 
                                        Point3d iNotRoomP, 
                                        Line notRoomLine,
                                        bool roomEnable,
                                        bool notRoomEnable)
        {
            var flag = fanParam.scenario == "消防加压送风";
            // 加压送风需要添加旁通后重新搜索路由
            if (roomEnable && notRoomEnable)
            {
                if (flag)
                {
                    SearchDuctInfo(iRoomP, roomLine, roomLines);
                    SearchDuctInfo(iNotRoomP, notRoomLine, notRoomLines);
                }
                else
                {
                    GetDuctInfo(fanParam.roomLines, roomLines, roomLine, iRoomP);
                    GetDuctInfo(fanParam.notRoomLines, notRoomLines, notRoomLine, iNotRoomP);
                }
            }
            else if (roomEnable && !notRoomEnable)
            {
                if (flag)
                    SearchDuctInfo(iRoomP, roomLine, roomLines);
                else
                    GetDuctInfo(fanParam.roomLines, roomLines, roomLine, iRoomP);
            }
            else if (!roomEnable && notRoomEnable)
            {
                if (flag)
                    SearchDuctInfo(iNotRoomP, notRoomLine, notRoomLines);
                else
                    GetDuctInfo(fanParam.notRoomLines, notRoomLines, notRoomLine, iNotRoomP);
            }
            else
                throw new NotImplementedException("未选择要生成室外侧或服务侧！！！");
        }
        private void GetDuctInfo(DBObjectCollection lines, HashSet<Line> curSet, Line replaceLine, Point3d detectorP)
        {
            // 需要将起始线换成replaceLine
            var detector = ThMEPHVACService.CreateDetector(detectorP, 5);
            var index = new ThCADCoreNTSSpatialIndex(lines);
            var res = index.SelectCrossingPolygon(detector);
            if (res.Count == 0)
                throw new NotImplementedException("检查风机进出口线段太短不足放置连接件");
            if (res.Count > 1)
                throw new NotImplementedException("有多条同色路由线连接在风机进出口");
            var orgStartLine = res[0] as Line;
            foreach (Line l in lines)
                if (!l.Equals(orgStartLine))
                    curSet.Add(l);
            curSet.Add(replaceLine);
        }
        private void SeperateFanInsideAndOutSide(DBObjectCollection wallLines, Point3d iRoomP, Point3d roomP)
        {
            if (wallLines.Count == 0)
                SetRoomInfo(iRoomP, roomP);
            else
            {
                BreakByWall(wallLines);
                if (fanBreakP.IsEqualTo(Point3d.Origin))
                {
                    // 有墙线但未相交
                    SetRoomInfo(iRoomP, roomP);
                }
            }
        }
        private void MergeBypassCenterLine(ref DBObjectCollection centerLine, DBObjectCollection bypass)
        {
            if (bypass.Count > 0)
            {
                foreach (Line l in bypass)
                    centerLine.Add(l.Clone() as Line);
                centerLine = ThMEPHVACLineProc.PreProc(centerLine);
            }
        }
        private void MergeBrokenBypass()
        {
            if (fanParam.bypassPattern == "RBType3")
            {
                var sepInfo1 = new SegInfo();
                var sepInfo2 = new SegInfo();
                foreach (var info1 in centerLines)
                {
                    foreach (var info2 in centerLines)
                    {
                        if (info1.Equals(info2))
                            continue;
                        var dis = ThMEPHVACService.GetLineDis(info1.Value.l.StartPoint, info1.Value.l.EndPoint, info2.Value.l.StartPoint, info2.Value.l.EndPoint);
                        if (Math.Abs(dis - ioBypassSepDis) < 1e-3)
                        {
                            sepInfo1 = info1.Value;
                            sepInfo2 = info2.Value;
                            break;
                        }
                    }
                    if (!String.IsNullOrEmpty(sepInfo1.ductSize))
                        break;
                }
                if (String.IsNullOrEmpty(sepInfo1.ductSize) || String.IsNullOrEmpty(sepInfo2.ductSize))
                    throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
                UpdateCenterLine(sepInfo1, sepInfo2);
            }
        }
        private void UpdateCenterLine(SegInfo info1, SegInfo info2)
        {
            ThMEPHVACService.GetLongestDis(info1.l, info2.l, out Point3d p1, out Point3d p2);
            var l = new Line(p1, p2);
            var newDuct = new SegInfo() { l = l, ductSize = fanParam.bypassSize, 
                                          srcShrink = info1.srcShrink, dstShrink = info2.srcShrink, 
                                          airVolume = fanParam .airVolume };
            centerLines.Remove(info1.l.GetHashCode());
            centerLines.Remove(info2.l.GetHashCode());
            centerLines.Add(l.GetHashCode(), newDuct);
        }
        private void DoAddInnerDuct(Line startLine, Point3d srtP, string ductSize)
        {
            var dirVec = ThMEPHVACService.GetEdgeDirection(startLine);
            var h = ThMEPHVACService.GetHeight(ductSize);
            var mmElevation = portParam.param.elevation * 1000;
            var cp = srtP - (dirVec * 0.5 * h);
            var ep = cp + new Vector3d(0, 0, h + mmElevation);
            var sp = ep - new Vector3d(0, 0, h + 1000);
            var l = new Line(sp, ep);
            UpDownVertivalPipe.Add(new SegInfo() 
            { 
                l = l, 
                horizontalVec = dirVec, 
                airVolume = portParam.param.airVolume, 
                ductSize = ductSize 
            });
        }
        private void MoveToOrg()
        {
            var lines = new DBObjectCollection();
            foreach (Line l in outCenterLine)
                if (l.Length > 10)
                    lines.Add(l);
            outCenterLine.Clear();
            outCenterLine = lines;

            var disMat = Matrix3d.Displacement(moveSrtP.GetAsVector());
            foreach (Line l in outCenterLine)
                l.TransformBy(disMat);
            fanBreakP = fanBreakP.TransformBy(disMat);
            // Move for FPM
            disMat = Matrix3d.Displacement(-fanBreakP.GetAsVector());
            foreach (Line l in outCenterLine)
                l.TransformBy(disMat);
        }
        private void MoveToZero(Point3d fanInletP,
                                Point3d fanOutletP,
                                DBObjectCollection centerLine,
                                DBObjectCollection wallLines,
                                out Point3d roomP,
                                out Point3d notRoomP)
        {
            var disMat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            foreach (Line l in bypass)
                l.TransformBy(disMat);
            foreach (Line l in centerLine)
                l.TransformBy(disMat);
            if (!isExhaust)
            {
                roomP = fanOutletP.TransformBy(disMat);
                notRoomP = fanInletP.TransformBy(disMat);
            }
            else
            {
                roomP = fanInletP.TransformBy(disMat);
                notRoomP = fanOutletP.TransformBy(disMat);
            }
        }
        private void GetVtElbowPos(Point3d inSearchPoint, Point3d outSearchPoint, DBObjectCollection lines)
        {
            roomVtPos = RecordVtElbowPos(inSearchPoint, lines);
            notRoomVtPos = RecordVtElbowPos(outSearchPoint, lines);
        }
        private Point3d RecordVtElbowPos(Point3d searchPoint, DBObjectCollection lines)
        {
            foreach (Line l in lines)
            {
                if (l.StartPoint.IsEqualTo(searchPoint, tor))
                {
                    //var line = seg.GetShrinkedLine();
                    var midP = ThMEPHVACService.GetMidPoint(l);
                    var dis = searchPoint.DistanceTo(midP);
                    if (dis > 2000)
                        dis = 2000;
                    var dirVec = ThMEPHVACService.GetEdgeDirection(l);
                    return searchPoint + (dirVec * dis);
                }
            }
            throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
        }
        private void CollectLines()
        {
            // 越像末尾越接近风机入口
            double inc = 0;
            centerLines.Clear();
            foreach (var l in roomLines)
            {
                if (IsBypass(l))
                    centerLines.Add(l.GetHashCode(), new SegInfo() { l = l, ductSize = fanParam.bypassSize, airVolume = inc, elevation = fanParam.roomElevation});
                else
                    centerLines.Add(l.GetHashCode(), new SegInfo() { l = l, ductSize = fanParam.roomDuctSize, airVolume = fanParam.airVolume + inc, elevation = fanParam.roomElevation });
                inc++;
            }
            inc = 0;
            foreach (var l in notRoomLines)
            {
                if (IsBypass(l))
                    centerLines.Add(l.GetHashCode(), new SegInfo() { l = l, ductSize = fanParam.bypassSize, airVolume = inc, elevation = fanParam.notRoomElevation });
                else
                    centerLines.Add(l.GetHashCode(), new SegInfo() { l = l, ductSize = fanParam.notRoomDuctSize, airVolume = fanParam.airVolume + inc, elevation = fanParam.notRoomElevation });
                inc++;
            }
        }
        private void ShrinkDuct()
        {
            CreateMainEndlineIndex(out Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic);
            var endLinesInfos = new List<EndlineInfo>();
            var reducingInfos = new List<ReducingInfo>();
            shrinkService = new ThShrinkDuct(endLinesInfos, reducingInfos, centerLines);
            var tlines = new DBObjectCollection();
            foreach (var l in roomLines)
                tlines.Add(l);
            shrinkService.SetLinesShrink(tlines, dic);
            foreach (var e in shrinkService.connectors)
                specialShapesInfo.Add(new EntityWithType() { entity = e, isRoom = true });
            tlines.Clear();
            foreach (var l in notRoomLines)
                tlines.Add(l);
            shrinkService.SetLinesShrink(tlines, dic);
            foreach (var e in shrinkService.connectors)
                specialShapesInfo.Add(new EntityWithType() { entity = e, isRoom = false });
        }
        private DBObjectCollection CreateMainEndlineIndex(out Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var lines = new DBObjectCollection();
            dic = new Dictionary<int, Dictionary<Point3d, Tuple<double, string>>>();
            // 添加主管段
            foreach (var seg in centerLines.Values)
            {
                var l = seg.l;
                lines.Add(l);
                dic.Add(l.GetHashCode(), new Dictionary<Point3d, Tuple<double, string>>());
                dic[l.GetHashCode()].Add(l.StartPoint, new Tuple<double, string>(seg.airVolume, seg.ductSize));
                dic[l.GetHashCode()].Add(l.EndPoint, new Tuple<double, string>(seg.airVolume, seg.ductSize));
            }
            return lines;
        }
        private void GetSpecialShapeInfo(Point3d startPoint, Line startLine, HashSet<Line> lineSet, string ductSize)
        {
            if (lineSet.Count == 0)
                return;
            var lines = new DBObjectCollection();
            foreach (var l in lineSet)
                lines.Add(l);
            spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            var search_p = startPoint.IsEqualTo(startLine.StartPoint, tor) ? startLine.EndPoint : startLine.StartPoint;
            UpdateStartLine(ref startLine, lines);
            DoSearchSpecialShape(search_p, startLine, ductSize);
        }
        private void UpdateStartLine(ref Line startLine, DBObjectCollection lines)
        {
            foreach (Line l in lines)
            {
                if (startLine.StartPoint.IsEqualTo(l.StartPoint, tor))
                {
                    startLine = l;
                    return;
                }
            }
        }
        private void DoSearchSpecialShape(Point3d searchPoint, Line currentLine, string ductSize)
        {
            var res = DetectCrossLine(searchPoint, currentLine);
            if (res.Count == 0)
            {
                return;
            }
            foreach (Line l in res)
            {
                var step_p = searchPoint.IsEqualTo(l.StartPoint, tor) ? l.EndPoint : l.StartPoint;
                DoSearchSpecialShape(step_p, l, ductSize);
            }
            if (res.Count >= 1)
            {
                RecordShapeParameter(searchPoint, currentLine, res, ductSize);
            }
        }
        private void RecordShapeParameter(Point3d centerP, Line inLine, DBObjectCollection outLines, string ductSize)
        {
            var portWidths = new Dictionary<Point3d, string>();
            var shapePortWidths = new List<string>();
            var otherP = ThMEPHVACService.GetOtherPoint(inLine, centerP, tor);
            var w = IsBypass(inLine) ? fanParam.bypassSize : ductSize;
            portWidths.Add(otherP, w);

            foreach (Line l in outLines)
            {
                w = IsBypass(l) ? fanParam.bypassSize : ductSize;
                shapePortWidths.Add(w);
                otherP = centerP.IsEqualTo(l.StartPoint, tor) ? l.EndPoint : l.StartPoint;
                portWidths.Add(otherP, w);
            }
            specialShapesInfo.Add(new EntityWithType() { entity = new EntityModifyParam() { centerP = centerP, portWidths = portWidths }, isRoom = (ductSize == fanParam.roomDuctSize) } );
        }
        private void SetRoomInfo(Point3d iRoomP, Point3d roomP)
        {
            if (bypass.Count == 0)
            {
                fanBreakP = iRoomP;
                outCenterLine = new DBObjectCollection();
                foreach (var line in roomLines)
                    outCenterLine.Add(line);
                roomLines.Clear();
            }
            else
            {
                var t = GetClosestTee(iRoomP);
                GetTeeInfo(t, iRoomP, out TeeType teeType, out Point3d p);
                UpdateBreakPoint(t, teeType, p);
                UpdateCenterLine(t, p);
                ClassifyRoomLine(p);
            }
        }
        private BypassTee GetClosestTee(Point3d iRoomP)
        {
            var min = Double.MaxValue;
            var t = new BypassTee();
            foreach (var tee in bypassTees)
            {
                var dis = tee.crossP.DistanceTo(iRoomP);
                if (dis < min)
                {
                    t = tee;
                    min = dis;
                }
            }
            return t;
        }
        private void ClassifyRoomLine(Point3d p)
        {
            var lines = new DBObjectCollection();
            foreach (Line l in roomLines)
                lines.Add(l);
            var detector = new ThFanCenterLineDetector(false);
            detector.SearchCenterLine(lines, ref p, SearchBreakType.breakWithEndline);
            foreach (Line l in detector.connectLines)
            {
                outCenterLine.Add(l);
                roomLines.Remove(l);
            }
        }
        private void BreakByWall(DBObjectCollection wallLines)
        {
            if (wallLines.Count == 0)
                return;
            var lines = ThMEPHVACService.CastMPolygon2Lines(wallLines[0] as MPolygon);
            var wallIndex = new ThCADCoreNTSSpatialIndex(lines);
            fanBreakP = Point3d.Origin;
            outCenterLine = new DBObjectCollection();
            var crossLine = new Line();
            foreach (var l in roomLines)
            {
                foreach (MPolygon wall in wallLines)
                {
                    if (!wall.Contains(l.StartPoint) && !wall.Contains(l.EndPoint))
                        outCenterLine.Add(l);
                    else
                    {
                        var e_l = ThMEPHVACService.ExtendLine(l, 1);
                        var pl = ThMEPHVACService.GetLineExtend(e_l, 1);
                        var res = wallIndex.SelectCrossingPolygon(pl);
                        if (res.Count > 0)
                        {
                            if (!IsBypass(l))
                            {
                                // 不是旁通再更新风机与墙的打断点
                                var line = res[0] as Line;
                                fanBreakP = ThMEPHVACService.IntersectPoint(e_l, line);
                                if (fanBreakP.IsEqualTo(Point3d.Origin))
                                    continue;
                                crossLine = l;
                                break;
                            }
                        }
                    }
                }
            }
            if (fanBreakP.IsEqualTo(Point3d.Origin))
                return;
            UpdateWallBreakPoint(wallLines, crossLine);
        }
        // p->出与旁通相交的三通后第一条线的末端点
        private void GetTeeInfo(BypassTee t, Point3d iRoomP, out TeeType teeType, out Point3d p)
        {
            teeType = ThDuctPortsShapeService.GetTeeType(t.bypass, t.otherLine);
            var p1 = t.crossP.IsEqualTo(t.inLine.StartPoint, tor) ? t.inLine.EndPoint : t.inLine.StartPoint;
            var p2 = t.crossP.IsEqualTo(t.otherLine.StartPoint, tor) ? t.otherLine.EndPoint : t.otherLine.StartPoint;
            p = p1.DistanceTo(iRoomP) > p2.DistanceTo(iRoomP) ? p1 : p2; // Direction
        }
        private void UpdateCenterLine(BypassTee t, Point3d p)
        {
            // 将crossPoint和p组成的线从fanBreakPoint处打断，分到roomline和outcenterline中
            var judgerLine = new Line(t.crossP, p);
            foreach (var l in roomLines)
            {
                if (ThMEPHVACService.IsSameLine(judgerLine, l, tor))
                {
                    roomLines.Remove(l);
                    break;
                }
            }
            var line = new Line(t.crossP, fanBreakP);
            roomLines.Add(line);
            line = new Line(fanBreakP, p);
            outCenterLine.Add(line);
        }
        private void UpdateBreakPoint(BypassTee t, TeeType teeType, Point3d p)
        {
            var dir_vec = (p - t.crossP).GetNormal();
            var ductWidth = ThMEPHVACService.GetWidth(fanParam.roomDuctSize);
            var bypassWidth = ThMEPHVACService.GetWidth(fanParam.bypassSize);
            var dis = (teeType == TeeType.BRANCH_COLLINEAR_WITH_OTTER) ? ductWidth + 50 : bypassWidth * 0.5 + 100;
            fanBreakP = t.crossP + (dir_vec * dis);
        }
        private void UpdateWallBreakPoint(DBObjectCollection wallLines, Line crossLine)
        {
            if (wallLines.Count == 0)
                return;
            var wall = wallLines[0] as MPolygon;
            //fanBreakP -> 中心线和墙
            var p = wall.Contains(crossLine.StartPoint)? crossLine.StartPoint : crossLine.EndPoint;
            foreach (Line l in outCenterLine)
                roomLines.Remove(l);
            roomLines.Remove(crossLine);
            roomLines.Add(new Line(p, fanBreakP));
            var otherP = ThMEPHVACService.GetOtherPoint(crossLine, p, tor);
            var line = new Line(fanBreakP, otherP);
            if (line.Length > 0)
                outCenterLine.Add(new Line(fanBreakP, otherP));
        }
        private void SearchDuctInfo(Point3d startPoint, Line startLine, HashSet<Line> lines)
        {
            if (startLine.Length < 1)
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
            var searchPoint = startPoint.IsEqualTo(startLine.StartPoint, tor) ? startLine.EndPoint : startLine.StartPoint;
            DoSearchDuct(searchPoint, startLine, lines);
            lines.Add(new Line(startPoint, searchPoint));
        }
        private void DoSearchDuct(Point3d searchPoint, Line currentLine, HashSet<Line> lines)
        {
            var res = DetectCrossLine(searchPoint, currentLine);
            if (res.Count == 0)
            {
                var otherP = ThMEPHVACService.GetOtherPoint(currentLine, searchPoint, tor);
                var line = new Line(otherP, searchPoint); // 空间索引出来的线可能不是风的流动方向
                lines.Add(line);
                return;
            }
            if (res.Count == 2)
            {
                // 三通
                if (IsBypass(res[0] as Line))
                {
                    var bypass = res[0] as Line;
                    var other = res[1] as Line;
                    bypassTees.Add(new BypassTee() { crossP = searchPoint, bypass = bypass, inLine = currentLine, otherLine = other });
                }
                else if (IsBypass(res[1] as Line))
                {
                    var bypass = res[1] as Line;
                    var other = res[0] as Line;
                    bypassTees.Add(new BypassTee() { crossP = searchPoint, bypass = bypass, inLine = currentLine, otherLine = other });
                }
            }
            foreach (Line l in res)
            {
                var step_p = searchPoint.IsEqualTo(l.StartPoint, tor) ? l.EndPoint : l.StartPoint;
                DoSearchDuct(step_p, l, lines);
                var line = new Line(searchPoint, step_p); // 空间索引出来的线可能不是风的流动方向
                lines.Add(line);
            }
        }
        private DBObjectCollection DetectCrossLine(Point3d search_point, Line current_line)
        {
            var poly = new Polyline();
            poly.CreatePolygon(search_point.ToPoint2D(), 4, 10);
            var res = spatialIndex.SelectCrossingPolygon(poly);
            res.Remove(current_line);
            return res;
        }
        private void UpdateSearchPoint(Point3d roomP,
                                       Point3d notRoomP,
                                       FanParam info, 
                                       ref DBObjectCollection centerLine,
                                       out Point3d iRoomP, 
                                       out Point3d iNotRoomP,
                                       out Line roomLine,
                                       out Line notRoomLine)
        {
            iRoomP = ThMEPHVACService.RoundPoint(roomP, 6);
            iNotRoomP = ThMEPHVACService.RoundPoint(notRoomP, 6);
            centerLine = GetStartLine(iRoomP, iNotRoomP, centerLine, out roomLine, out notRoomLine);
            if (isExhaust) 
            {
                // 排风时room_p -> fan_intlet_p not_room_p -> fan_outlet_p
                if (fan.IntakeForm.Contains("上进") || fan.IntakeForm.Contains("下进"))
                {
                    // 进风口加上翻，出风口变径
                    UpdateInStartInfo(centerLine, fanParam.roomDuctSize, ref roomLine, ref iRoomP);
                    DoAddInnerDuct(roomLine, iRoomP, fanParam.roomDuctSize);
                    UpdateOutStartInfo(false, fan.fanOutWidth, info, centerLine, ref notRoomLine, ref iNotRoomP);

                }
                else if (fan.IntakeForm.Contains("上出") || fan.IntakeForm.Contains("下出"))
                {
                    // 出风口加上翻，进风口变径
                    UpdateInStartInfo(centerLine, fanParam.notRoomDuctSize, ref notRoomLine, ref iNotRoomP);
                    DoAddInnerDuct(notRoomLine, iNotRoomP, fanParam.notRoomDuctSize);
                    UpdateOutStartInfo(true, fan.fanInWidth, info, centerLine, ref roomLine, ref iRoomP);
                }
                else
                {
                    // 两边变径
                    UpdateOutStartInfo(true, fan.fanInWidth, info, centerLine, ref roomLine, ref iRoomP);
                    UpdateOutStartInfo(false, fan.fanOutWidth, info, centerLine, ref notRoomLine, ref iNotRoomP);
                }
            }
            else
            {
                //非排风时room_p->fan_outlet_p not_room_p->fan_inlet_p
                if (fan.IntakeForm.Contains("上进") || fan.IntakeForm.Contains("下进"))
                {
                    // 进风口加上翻，出风口变径
                    UpdateInStartInfo(centerLine, fanParam.notRoomDuctSize, ref notRoomLine, ref iNotRoomP);
                    DoAddInnerDuct(notRoomLine, iNotRoomP, fanParam.notRoomDuctSize);
                    UpdateOutStartInfo(true, fan.fanOutWidth, info, centerLine, ref roomLine, ref iRoomP);
                }
                else if (fan.IntakeForm.Contains("上出") || fan.IntakeForm.Contains("下出"))
                {
                    // 出风口加上翻，进风口变径
                    UpdateInStartInfo(centerLine, fanParam.roomDuctSize, ref roomLine, ref iRoomP);
                    DoAddInnerDuct(roomLine, iRoomP, fanParam.roomDuctSize);
                    UpdateOutStartInfo(false, fan.fanInWidth, info, centerLine, ref notRoomLine, ref iNotRoomP);
                }
                else
                {
                    // 两边变径
                    UpdateOutStartInfo(true, fan.fanOutWidth, info, centerLine, ref roomLine, ref iRoomP);
                    UpdateOutStartInfo(false, fan.fanInWidth, info, centerLine, ref notRoomLine, ref iNotRoomP);
                }
            }
        }
        private void UpdateInStartInfo(DBObjectCollection lines, string ductSize, ref Line startLine, ref Point3d srtP)
        {
            var shrinkLen = ThMEPHVACService.GetHeight(ductSize);
            var dirVec = ThMEPHVACService.GetEdgeDirection(startLine);
            srtP += (shrinkLen * dirVec * 0.5);
            lines.Remove(startLine);
            startLine = new Line(srtP, startLine.EndPoint);
            lines.Add(startLine);
        }
        private void UpdateOutStartInfo(bool isRoom,
                                        double fanWidth,
                                        FanParam info, 
                                        DBObjectCollection lines,
                                        ref Line startLine,
                                        ref Point3d srtP)
        {
            double shrinkDis = (isRoom) ? GetShrinkDis(info.roomDuctSize, fanWidth, out double ductWidth, out double ductHeight) :
                                          GetShrinkDis(info.notRoomDuctSize, fanWidth, out ductWidth, out ductHeight);
            var highDisVec = new Vector3d(0, 0, ductHeight);
            if (shrinkDis < 0)
                return;
            var dirVec = ThMEPHVACService.GetEdgeDirection(startLine);
            srtP = startLine.StartPoint + dirVec * shrinkDis;// reducing长度一定大于软接长度
            var hoseLen = (fan.scenario == "消防补风" || fan.scenario == "消防排烟" || fan.scenario == "消防加压送风") ? 0 : 200;
            srtP = startLine.StartPoint + dirVec * (shrinkDis + hoseLen);// ThDuctPortsAnaylysis.cs Line:253
            var reducing = new Line(startLine.StartPoint + (dirVec * hoseLen), srtP);
            var isAxis = (fan.Name.Contains("轴流风机"));
            reducing.StartPoint += highDisVec;
            reducing.EndPoint += highDisVec;
            reducings.Add(new ReducingWithType() { bounds = ThDuctPortsFactory.CreateReducing(reducing, fanWidth, ductWidth, isAxis) , isRoom = isRoom} );
            lines.Remove(startLine);
            startLine = new Line(srtP, startLine.EndPoint);
            lines.Add(startLine);
        }
        private double GetShrinkDis(string ductSize, double fanWidth, out double ductWidth, out double ductHeight)
        {
            ThMEPHVACService.GetWidthAndHeight(ductSize, out ductWidth, out ductHeight);
            var bigWidth = Math.Max(ductWidth, fanWidth);
            var smallWidth = Math.Min(ductWidth, fanWidth);
            return ThDuctPortsShapeService.GetReducingLen(bigWidth, smallWidth);
        }
        private DBObjectCollection GetStartLine(Point3d iRoomP, 
                                                Point3d iNotRoomP, 
                                                DBObjectCollection lines,
                                                out Line roomLine,
                                                out Line notRoomLine)
        {
            roomLine = new Line();
            notRoomLine = new Line();
            var newLines = new DBObjectCollection();
            var startPTor = new Tolerance(1.5, 1.5);
            foreach (Line l in lines)
            {
                if (iRoomP.IsEqualTo(l.StartPoint, startPTor) || iRoomP.IsEqualTo(l.EndPoint, startPTor))
                {
                    var otherP = iRoomP.IsEqualTo(l.StartPoint, startPTor) ? l.EndPoint : l.StartPoint;
                    roomLine = new Line(iRoomP, otherP);
                    newLines.Add(roomLine);
                }
                else if (iNotRoomP.IsEqualTo(l.StartPoint, startPTor) || iNotRoomP.IsEqualTo(l.EndPoint, startPTor))
                {
                    var otherP = iNotRoomP.IsEqualTo(l.StartPoint, startPTor) ? l.EndPoint : l.StartPoint;
                    notRoomLine = new Line(iNotRoomP, otherP);
                    newLines.Add(notRoomLine);
                }
                else
                    newLines.Add(l);
            }
            return newLines;
        }
        private bool IsBypass(Line line)
        {
            foreach (Line l in bypass)
            {
                if (ThMEPHVACService.IsSameLine(line, l, tor))
                    return true;
            }
            return false;
        }
    }
}
