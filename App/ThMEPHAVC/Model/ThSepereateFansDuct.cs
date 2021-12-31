using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPHVAC.CAD;

namespace ThMEPHVAC.Model
{
    public class ThSepereateFansDuct
    {
        public DBObjectCollection mainDucts;
        public DBObjectCollection centerlines;
        public List<TextAlignLine> textAlignment;
        public DBObjectCollection connNotRoomLines;// 筛选后只剩下不与风机相连的非服务侧主管段
        public Dictionary<int, FanParam> dicLineParam;
        private Stack<int> lineCode;
        private Point3d roomStartPoint;
        private ThCADCoreNTSSpatialIndex index;
        private Dictionary<string, FanParam> dicFans;
        
        public ThSepereateFansDuct(Point3d roomStartPoint, DBObjectCollection connNotRoomLines, Dictionary<string, FanParam> dicFans)
        {
            Init(roomStartPoint, connNotRoomLines, dicFans);
            MoveToZero();
            CreateFanParamDic();
            ExcludeConnLine();
            CollectMainDuct();
            AddMainDuctToDic();
            SplitNotRoomLines();
            MoveToOrg();
        }

        private void Init(Point3d roomStartPoint, DBObjectCollection connNotRoomLines, Dictionary<string, FanParam> dicFans)
        {
            this.dicFans = dicFans;
            this.roomStartPoint = roomStartPoint;
            this.connNotRoomLines = connNotRoomLines;
            lineCode = new Stack<int>();
            mainDucts = new DBObjectCollection();
            centerlines = new DBObjectCollection();
            textAlignment = new List<TextAlignLine>();
            dicLineParam = new Dictionary<int, FanParam>();
        }
        private void AddMainDuctToDic()
        {
            if (dicFans.Keys.Count > 0)
            {
                // 有风机
                FanMergeNotRoomLine();
            }
            else
            {
                WithoutFanMergeNotRoomLine();
            }
        }
        private void WithoutFanMergeNotRoomLine()
        {
            var l = connNotRoomLines[0] as Line;
            var line = SearchLastLine(l);
            dicLineParam.Add(l.GetHashCode(), dicLineParam[line.GetHashCode()]);
            TransNotRoomLineToCenterLine(line);
            dicLineParam.Remove(line.GetHashCode());
        }
        private void FanMergeNotRoomLine()
        {
            int idx = 0;
            if (dicFans.Count == 1)
            {
                ProcWithSingleFan();
            }
            while (connNotRoomLines.Count != 0)
            {
                // 循环探测直到把所有线加到参数字典中
                var l = connNotRoomLines[idx] as Line;
                var shadow = SearchCenterLine(l);
                var f1 = TogetherDetect(l.StartPoint, l, shadow, out int srtCrossNum);
                var f2 = TogetherDetect(l.EndPoint, l, shadow, out int endCrossNum);
                if (!f1 && !f2)
                {
                    if (lineCode.Count > 0 && srtCrossNum + endCrossNum == 1)
                    {
                        // 主管段一端相连一端不相连
                        dicLineParam.Add(l.GetHashCode(), dicLineParam[lineCode.Pop()]);
                        TransNotRoomLineToCenterLine(l);
                    }
                    else if (srtCrossNum + endCrossNum == 0)
                    {
                        // 只有一条线
                        if (shadow.Length > 0)
                        {
                            dicLineParam.Add(l.GetHashCode(), dicLineParam[shadow.GetHashCode()]);
                            dicLineParam.Remove(shadow.GetHashCode());
                            TransNotRoomLineToCenterLine(l);
                        }
                    }
                    else
                        idx++;
                }
                else
                    idx = 0;
            }
        }
        private void ProcWithSingleFan()
        {
            // 只有一台风机时无法通过支路合并风量
            var fanParam = dicFans.Values.FirstOrDefault();
            while (connNotRoomLines.Count != 0)
            {
                foreach (Line l in connNotRoomLines)
                {
                    var shadow = SearchCenterLine(l);
                    if (dicLineParam.ContainsKey(shadow.GetHashCode()))
                    {
                        dicLineParam.Add(l.GetHashCode(), dicLineParam[shadow.GetHashCode()]);
                        connNotRoomLines.Remove(l);
                        centerlines.Remove(shadow);
                        centerlines.Add(l);
                        dicLineParam.Remove(shadow.GetHashCode());
                    }
                    else
                    {
                        // 有问题
                        dicLineParam.Add(l.GetHashCode(), fanParam);
                        fanParam.centerLines.Add(l);
                        connNotRoomLines.Remove(l);
                        centerlines.Add(l);
                    }
                    break;
                }
            }
            index = new ThCADCoreNTSSpatialIndex(centerlines);
        }
        // shadow 是 detectLine 在centerline中的线
        private bool TogetherDetect(Point3d detectPoint, Line detectLine, Line shadow, out int crossNum)
        {
            crossNum = 0;
            if (dicLineParam.ContainsKey(detectLine.GetHashCode()))
                return false;// 检测srt点的时候已经检测到了，此时crossNum可设任意值
            var pl = new Polyline();
            pl.CreatePolygon(detectPoint.ToPoint2D(), 4, 1);
            var res = index.SelectCrossingPolygon(pl);
            if (shadow.Length > 0)
                res.Remove(shadow);// 当搜索线包含在索引中
            crossNum = res.Count;
            if (crossNum == 2)
            {
                // 三通
                var param1 = dicLineParam[(res[0] as Line).GetHashCode()];
                var param2 = dicLineParam[(res[1] as Line).GetHashCode()]; 
                var fans = new List<FanParam>() { param1, param2 };
                dicLineParam.Add(detectLine.GetHashCode(), CreateParam(fans));
                TransNotRoomLineToCenterLine(detectLine);
                return true;
            }
            else if (crossNum == 3)
            {
                // 四通
                var param1 = dicLineParam[(res[0] as Line).GetHashCode()];
                var param2 = dicLineParam[(res[1] as Line).GetHashCode()];
                var param3 = dicLineParam[(res[2] as Line).GetHashCode()];
                var fans = new List<FanParam>() { param1, param2, param3};
                dicLineParam.Add(detectLine.GetHashCode(), CreateParam(fans));
                TransNotRoomLineToCenterLine(detectLine);
                return true;
            }
            return false;
        }

        private Line SearchCenterLine(Line detectLine)
        {
            foreach (Line l in centerlines)
                if (ThMEPHVACService.IsSameLine(l, detectLine))
                    return l;
            return new Line();
        }
        private void TransNotRoomLineToCenterLine(Line l)
        {
            lineCode.Push(l.GetHashCode());
            connNotRoomLines.Remove(l);
            centerlines.Add(l);
            index = new ThCADCoreNTSSpatialIndex(centerlines);
        }
        private void ExcludeConnLine()
        {
            // 将与风机相连的线与整个非服务区的线分开
            if (dicFans.Count > 1)
            {
                foreach (Line l in centerlines)
                {
                    foreach (Line shadow in connNotRoomLines)
                    {
                        if (ThMEPHVACService.IsSameLine(l, shadow))
                        {
                            connNotRoomLines.Remove(shadow);
                            break;
                        }
                    }
                }
            }
        }

        private void CollectMainDuct()
        {
            foreach (Line l in connNotRoomLines)
                mainDucts.Add(l);
        }

        private void CreateFanParamDic()
        {
            foreach (var param in dicFans.Values)
            {
                foreach (Line l in param.centerLines)
                {
                    if (!dicLineParam.ContainsKey(l.GetHashCode()))
                    {
                        dicLineParam.Add(l.GetHashCode(), param);
                        centerlines.Add(l);
                    }
                }
            }
            index = new ThCADCoreNTSSpatialIndex(centerlines);
        }
        private FanParam CreateParam(List<FanParam> fans)
        {
            var airSpeed = fans[0].airSpeed;
            double maxE = 0.0;
            double maxW = 0.0;
            double maxH = 0.0;
            string maxElevation = string.Empty;
            var firstFan = fans.FirstOrDefault();
            var selectMaxFlag = (firstFan.scenario.Contains("排烟") && !firstFan.scenario.Contains("兼")) ||
                                 firstFan.scenario.Contains("消防加压送风");
            double airVolume = selectMaxFlag ? firstFan.airVolume : 0;
            foreach (var fan in fans)
            {
                if (selectMaxFlag && fan.airVolume > airVolume)
                    airVolume = fan.airVolume;
                else
                    airVolume += fan.airVolume;
                ThMEPHVACService.GetWidthAndHeight(fan.notRoomDuctSize, out double w, out double h);
                if (maxW < w)
                    maxW = w;
                if (maxH < h)
                    maxH = h;
                var e = Double.Parse(fan.notRoomElevation);
                if (maxE < e)
                {
                    maxH = h;
                    maxElevation = fan.notRoomElevation;
                }
            }
            var ductSize = SelectDuctSize(airVolume, maxH, maxW, firstFan.scenario);
            return new FanParam()
            {
                airSpeed = airSpeed,
                airVolume = selectMaxFlag ? airVolume + 1 : airVolume,// +1是为了区分当风量相同时的主管段
                notRoomDuctSize = ductSize,
                notRoomElevation = maxElevation
            };
        }
        private string SelectDuctSize(double airVolume, double maxH, double maxW, string scenario)
        {
            var ductParam = new ThDuctParameter(airVolume, scenario);
            var ductSize = String.Empty;
            string tmpSize = String.Empty;
            var size = ductParam.DuctSizeInfor.DefaultDuctsSizeString.Count - 1;
            for (int i = size; i >= 0; --i)
            {
                var s = ductParam.DuctSizeInfor.DefaultDuctsSizeString[i];
                ThMEPHVACService.GetWidthAndHeight(s, out double w, out double h);
                if (w >= maxW && h >= maxH)
                    return s;
            }
            if (String.IsNullOrEmpty(ductSize))
                ductSize = tmpSize;
            return ductSize;
        }
        private void MoveToZero()
        {
            var mat = Matrix3d.Displacement(-roomStartPoint.GetAsVector());
            foreach (var param in dicFans.Values)
            {
                foreach (Line l in param.centerLines)
                {
                    l.TransformBy(mat);
                }
            }
        }
        private void MoveToOrg()
        {
            var mat = Matrix3d.Displacement(roomStartPoint.GetAsVector());
            foreach (var param in dicFans.Values)
            {
                foreach (Line l in param.centerLines)
                {
                    l.TransformBy(mat);
                }
            }
        }
        private void SplitNotRoomLines()
        {
            if (dicFans.Count > 1)
            {
                // 将管段和三通连接处根据要缩短的距离划分为两段
                foreach (var param in dicFans.Values)
                {
                    // 找到最后一条线的划分方法
                    SplitLastLine(param, out Line newFanLine, out Line mainDuctLine);
                    mainDucts.Add(mainDuctLine);
                    dicLineParam.Add(mainDuctLine.GetHashCode(), param);
                    // 将划分的线更新到风机的中心线
                    UpdateCenterLine(param, newFanLine);
                }
            }
        }

        private void UpdateCenterLine(FanParam param, Line newFanLine)
        {
            foreach (Line l in param.centerLines)
            {
                if (ThMEPHVACService.IsSameLine(l, param.lastNotRoomLine))
                {
                    param.centerLines.Remove(l);
                    break;
                }
            }
            param.centerLines.Add(newFanLine);
        }

        private void SplitLastLine(FanParam param, out Line newFanLine, out Line mainDuctLine)
        {
            Line l = SearchLastLine(param.lastNotRoomLine);
            textAlignment.Add(new TextAlignLine() { l = l, ductSize = param.notRoomDuctSize, isRoom = false });
            var spl = new Polyline();
            spl.CreatePolygon(l.StartPoint.ToPoint2D(), 4, 1);
            var sRes = index.SelectCrossingPolygon(spl);
            var epl = new Polyline();
            epl.CreatePolygon(l.EndPoint.ToPoint2D(), 4, 1);
            var eRes = index.SelectCrossingPolygon(epl);
            int sCrossNum = sRes.Count;
            int eCrossNum = eRes.Count;
            if (eCrossNum == 2 && sCrossNum == 1)
            {
                foreach (Line tl in eRes)
                    if (ThMEPHVACService.IsSameLine(tl, l))
                        eRes.Remove(tl);
                ProcWithElbow(eCrossNum, param, l, eRes[0] as Line, out newFanLine, out mainDuctLine);
                return;
            }
            if (eCrossNum == 3 && (sCrossNum == 1 || sCrossNum == 2))
            {
                foreach (Line tl in eRes)
                    if (ThMEPHVACService.IsSameLine(tl, l))
                        eRes.Remove(tl);
                ProcWithTee(eCrossNum, l, eRes, out newFanLine, out mainDuctLine);
                return;
            }
            if (eCrossNum == 4 && (sCrossNum == 1 || sCrossNum == 2))
            {
                foreach (Line tl in eRes)
                    if (ThMEPHVACService.IsSameLine(tl, l))
                        eRes.Remove(tl);
                ProcWithCross(eCrossNum, l, eRes, out newFanLine, out mainDuctLine);
                return;
            }
            throw new NotImplementedException("[Check Error]: Main duct is not a collector!");
        }
        private Line SearchLastLine(Line lastLine)
        {
            foreach (Line line in centerlines)
                if (ThMEPHVACService.IsSameLine(lastLine, line))
                    return line;
            throw new NotImplementedException();
        }
        
        private void ProcWithCross(int eCrossNum, Line Victim, DBObjectCollection otherLines, out Line newFanLine, out Line mainDuctLine)
        {
            // Cross只需要缩出口段
            var flag = (eCrossNum == 4);
            var sp = flag ? Victim.StartPoint : Victim.EndPoint;
            var ep = flag ? Victim.EndPoint : Victim.StartPoint;
            var dirVec = (ep - sp).GetNormal();
            var l1 = (otherLines[0] as Line);
            var l2 = (otherLines[1] as Line);
            var l3 = (otherLines[2] as Line);
            var param1 = dicLineParam[l1.GetHashCode()];
            var param2 = dicLineParam[l2.GetHashCode()];
            var param3 = dicLineParam[l3.GetHashCode()];
            var w1 = ThMEPHVACService.GetWidth(param1.notRoomDuctSize);
            var w2 = ThMEPHVACService.GetWidth(param2.notRoomDuctSize);
            var w3 = ThMEPHVACService.GetWidth(param3.notRoomDuctSize);
            double oCollinearShrink = 0;
            if (param1.airVolume > param2.airVolume && param1.airVolume > param3.airVolume)
                ThDuctPortsShapeService.GetCrossShrink(w1, w2, w3, out double _, out double _, out double _, out oCollinearShrink);
            if (param2.airVolume > param1.airVolume && param2.airVolume > param3.airVolume)
                ThDuctPortsShapeService.GetCrossShrink(w2, w1, w3, out double _, out double _, out double _, out oCollinearShrink);
            if (param3.airVolume > param1.airVolume && param3.airVolume > param2.airVolume)
                ThDuctPortsShapeService.GetCrossShrink(w3, w1, w2, out double _, out double _, out double _, out oCollinearShrink);
            var splitPoint = ep - dirVec * oCollinearShrink;
            newFanLine = new Line(sp, splitPoint);
            mainDuctLine = new Line(splitPoint, ep);
        }
        private void ProcWithTee(int eCrossNum, Line Victim, DBObjectCollection res, out Line newFanLine, out Line mainDuctLine)
        {
            var flag = (eCrossNum == 3);
            var sp = flag ? Victim.StartPoint : Victim.EndPoint;
            var ep = flag ? Victim.EndPoint : Victim.StartPoint;
            var dirVec = (ep - sp).GetNormal();
            var otherLine1 = (res[0] as Line);
            var otherLine2 = (res[1] as Line);
            var curParam = dicLineParam[Victim.GetHashCode()];
            var param1 = dicLineParam[otherLine1.GetHashCode()];
            var param2 = dicLineParam[otherLine2.GetHashCode()];
            double shrinkLen = ThDuctPortsShapeService.GetTeeShrink(Victim, otherLine1, otherLine2, curParam, param1, param2)[Victim.GetHashCode()];
            var splitPoint = ep - dirVec * shrinkLen;
            newFanLine = new Line(sp, splitPoint);
            mainDuctLine = new Line(splitPoint, ep);
        }
        private void ProcWithElbow(int eCrossNum,
                                   FanParam param,
                                   Line Victim,
                                   Line otherLine,
                                   out Line newFanLine,
                                   out Line mainDuctLine)
        {
            var w = ThMEPHVACService.GetWidth(param.notRoomDuctSize);
            var angle = ThMEPHVACService.GetElbowOpenAngle(Victim, otherLine, Victim.EndPoint);
            var len = ThDuctPortsShapeService.GetElbowShrink(angle, w);
            var flag = (eCrossNum == 2);
            var sp = flag ? Victim.EndPoint : Victim.StartPoint;
            var ep = flag ? Victim.StartPoint : Victim.EndPoint;
            var dirVec = (ep - sp).GetNormal();
            var splitPoint = sp + dirVec * len;
            newFanLine = new Line(sp, splitPoint);
            mainDuctLine = new Line(splitPoint, ep);
        }
    }
}