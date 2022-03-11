using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThShrinkDuct
    {
        private Tolerance tor;
        private HashSet<Point3d> connectorPSet;
        public List<EndlineInfo> endLinesInfos;
        public List<ReducingInfo> reducingInfos;
        public List<EntityModifyParam> connectors; 
        public Dictionary<int, SegInfo> mainLinesInfos;
        public ThShrinkDuct(List<EndlineInfo> endLinesInfos,
                            List<ReducingInfo> reducingInfos,
                            Dictionary<int, SegInfo> mainLinesInfos)
        {
            tor = new Tolerance(1.5, 1.5);
            connectorPSet = new HashSet<Point3d>();
            connectors = new List<EntityModifyParam>();
            this.reducingInfos = reducingInfos;
            this.endLinesInfos = endLinesInfos;
            this.mainLinesInfos = mainLinesInfos;
        }
        public void SetLinesShrink(DBObjectCollection centerlines, 
                                   Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var index = new ThCADCoreNTSSpatialIndex(centerlines);
            // shrink main line
            foreach (Line l in centerlines)
            {
                DoShrinkDuct(l, l.StartPoint, index, dic);
                DoShrinkDuct(l, l.EndPoint, index, dic);
            }
        }
        private void DoShrinkDuct(Line currLine,
                                  Point3d p,
                                  ThCADCoreNTSSpatialIndex index,
                                  Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var srtPl = ThMEPHVACService.CreateDetector(p);
            var crossLines = index.SelectCrossingPolygon(srtPl);
            if (connectorPSet.Add(p) && crossLines.Count > 1)
                CreateConnector(p, crossLines, dic);
            crossLines.Remove(currLine);
            switch (crossLines.Count)
            {
                case 0: break;
                case 1: ShrinkElbow(currLine, p, crossLines, dic); break;
                case 2: ShrinkTee(currLine, p, crossLines, dic); break;
                case 3: ShrinkCross(currLine, p, crossLines, dic); break;
                default: throw new NotImplementedException("[CheckError]: Just support connector less than 4!");
            }
        }
        private void ShrinkCross(Line currLine,
                                 Point3d detectP,
                                 DBObjectCollection crossLines,
                                 Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var otherLine1 = crossLines[0] as Line;
            var otherLine2 = crossLines[1] as Line;
            var otherLine3 = crossLines[2] as Line;
            var curInfo = dic[currLine.GetHashCode()][detectP];
            var otherInfo1 = dic[otherLine1.GetHashCode()][detectP];
            var otherInfo2 = dic[otherLine2.GetHashCode()][detectP];
            var otherInfo3 = dic[otherLine3.GetHashCode()][detectP];
            var dicShrink = ThDuctPortsShapeService.GetCrossShrink(currLine, otherLine1, otherLine2, otherLine3, curInfo, otherInfo1, otherInfo2, otherInfo3);
            var shrinkLen = dicShrink[currLine.GetHashCode()];
            var isStart = detectP.IsEqualTo(currLine.StartPoint, tor);
            UpdateMainLineShrink(currLine, shrinkLen, isStart);
            UpdateEndLineShrink(currLine, shrinkLen, isStart);
            shrinkLen = dicShrink[otherLine1.GetHashCode()];
            isStart = detectP.IsEqualTo(otherLine1.StartPoint, tor);
            UpdateMainLineShrink(otherLine1, shrinkLen, isStart);
            UpdateEndLineShrink(otherLine1, shrinkLen, isStart);
            shrinkLen = dicShrink[otherLine2.GetHashCode()];
            isStart = detectP.IsEqualTo(otherLine2.StartPoint, tor);
            UpdateMainLineShrink(otherLine2, shrinkLen, isStart);
            UpdateEndLineShrink(otherLine2, shrinkLen, isStart);
            shrinkLen = dicShrink[otherLine3.GetHashCode()];
            isStart = detectP.IsEqualTo(otherLine3.StartPoint, tor);
            UpdateMainLineShrink(otherLine3, shrinkLen, isStart);
            UpdateEndLineShrink(otherLine3, shrinkLen, isStart);
        }
        private void ShrinkTee(Line currLine,
                               Point3d detectP,
                               DBObjectCollection crossLines,
                               Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var otherLine1 = crossLines[0] as Line;
            var otherLine2 = crossLines[1] as Line;
            var curInfo = dic[currLine.GetHashCode()][detectP];
            var otherInfo1 = dic[otherLine1.GetHashCode()][detectP];
            var otherInfo2 = dic[otherLine2.GetHashCode()][detectP];
            var dicShrink = ThDuctPortsShapeService.GetTeeShrink(currLine, otherLine1, otherLine2, curInfo, otherInfo1, otherInfo2);
            var shrinkLen = dicShrink[currLine.GetHashCode()];
            var isStart = detectP.IsEqualTo(currLine.StartPoint, tor);
            UpdateMainLineShrink(currLine, shrinkLen, isStart);
            UpdateEndLineShrink(currLine, shrinkLen, isStart);
            shrinkLen = dicShrink[otherLine1.GetHashCode()];
            isStart = detectP.IsEqualTo(otherLine1.StartPoint, tor);
            UpdateMainLineShrink(otherLine1, shrinkLen, isStart);
            UpdateEndLineShrink(otherLine1, shrinkLen, isStart);
            shrinkLen = dicShrink[otherLine2.GetHashCode()];
            isStart = detectP.IsEqualTo(otherLine2.StartPoint, tor);
            UpdateMainLineShrink(otherLine2, shrinkLen, isStart);
            UpdateEndLineShrink(otherLine2, shrinkLen, isStart);
        }
        private void CreateConnector(Point3d centerP, DBObjectCollection lines, Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var portWidths = new Dictionary<Point3d, string>();
            double maxAirVolume = 0.0;
            var inLine = new Line();
            // 通过管道宽度判入口只适用于三通和四通
            foreach (Line l in lines)
            {
                var airVolume = dic[l.GetHashCode()][centerP].Item1;
                if (maxAirVolume <= airVolume)
                {
                    maxAirVolume = airVolume;
                    inLine = l;
                }
            }
            Record(portWidths, centerP, inLine, dic);
            foreach (Line l in lines)
            {
                if (l.Equals(inLine))
                    continue;
                Record(portWidths, centerP, l, dic);
            }
            connectors.Add(new EntityModifyParam() { centerP = centerP, portWidths = portWidths });
        }
        private void Record(Dictionary<Point3d, string> portWidths, Point3d centerP, Line inLine,
                            Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var otherP = ThMEPHVACService.GetOtherPoint(inLine, centerP, tor);
            var ductSize = dic[inLine.GetHashCode()][centerP].Item2;
            if (!portWidths.ContainsKey(otherP))
                portWidths.Add(otherP, ductSize);
        }
        private void ShrinkElbow(Line currLine,
                                 Point3d detectP,
                                 DBObjectCollection crossLines,
                                 Dictionary<int, Dictionary<Point3d, Tuple<double, string>>> dic)
        {
            var otherLine = crossLines[0] as Line;
            // detectP为currLine的startPoint则一定为otherLine的endPoint
            var isStart = detectP.IsEqualTo(currLine.StartPoint, tor);
            var openAngle = ThDuctPortsShapeService.GetElbowOpenAngle(detectP, currLine, otherLine);
            var curInfo = dic[currLine.GetHashCode()];
            var otherInfo = dic[otherLine.GetHashCode()];
            var curDuctSize = curInfo[detectP].Item2;
            var otherDuctSize = otherInfo[detectP].Item2;
            var curW = ThMEPHVACService.GetWidth(curDuctSize);
            var otherW = ThMEPHVACService.GetWidth(otherDuctSize);
            if (curDuctSize == otherDuctSize)
            {
                // 两边大小一样直接缩弯头长度
                var shrinkLen = ThDuctPortsShapeService.GetElbowShrink(openAngle, curW);
                UpdateMainLineShrink(currLine, shrinkLen, isStart);
                UpdateMainLineShrink(otherLine, shrinkLen, !isStart);
                UpdateEndLineShrink(currLine, shrinkLen, isStart);
                UpdateEndLineShrink(otherLine, shrinkLen, !isStart);
            }
            else
            {
                // 两边大小不一样，小的一边缩弯头，大的一边缩弯头加变径
                if (curW <= otherW)
                {
                    var shrinkLen = ThDuctPortsShapeService.GetElbowShrink(openAngle, curW);
                    UpdateMainLineShrink(currLine, shrinkLen, isStart);
                    UpdateEndLineShrink(currLine, shrinkLen, isStart);
                }
                else
                {
                    var elbowShrink = ThDuctPortsShapeService.GetElbowShrink(openAngle, otherW);
                    UpdateMainLineShrink(otherLine, elbowShrink, !isStart);
                    UpdateEndLineShrink(otherLine, elbowShrink, !isStart);
                    var reducingLen = GetMainElbowReducingShrink(currLine, elbowShrink);
                    if (reducingLen < 0)
                        reducingLen = GetEndLineElbowReducingShrink(otherLine, elbowShrink);
                    var totalShrink = elbowShrink + reducingLen;
                    RecordReducingWithElbow(currLine, totalShrink, elbowShrink, curDuctSize, otherDuctSize);
                    UpdateMainLineShrink(currLine, totalShrink, isStart);
                    UpdateEndLineShrink(currLine, totalShrink, isStart);
                }
            }
        }
        private void RecordReducingWithElbow(Line l, double totalShrink, double elbowShrink, string bigSize, string smallSize)
        {
            var dirVec = ThMEPHVACService.GetEdgeDirection(l);
            var newl = new Line(l.EndPoint - dirVec * totalShrink, l.EndPoint - dirVec * elbowShrink);
            reducingInfos.Add(new ReducingInfo() { l = newl, bigSize = bigSize, smallSize = smallSize });
        }
        private void UpdateMainLineShrink(Line l, double shrinkLen, bool isStart)
        {
            var key = l.GetHashCode();
            if (mainLinesInfos.ContainsKey(key))
            {
                if (isStart)
                    mainLinesInfos[key].srcShrink = shrinkLen;
                else
                    mainLinesInfos[key].dstShrink = shrinkLen;
            }
        }
        private void UpdateEndLineShrink(Line l, double shrinkLen, bool isStart)
        {
            var key = l.GetHashCode();
            foreach (var endlines in endLinesInfos)
            {
                if (endlines.endlines.ContainsKey(key))
                {
                    if (isStart)
                        endlines.endlines[key].seg.srcShrink = shrinkLen;
                    else
                        endlines.endlines[key].seg.dstShrink = shrinkLen;
                    break;
                }
            }
        }
        private double GetEndLineElbowReducingShrink(Line l, double elbowShrink)
        {
            var key = l.GetHashCode();
            foreach (var endlines in endLinesInfos)
            {
                if (endlines.endlines.ContainsKey(key))
                {
                    var endlineInfo = endlines.endlines[key].seg;
                    var haveShrinkedLen = endlineInfo.srcShrink + endlineInfo.dstShrink;
                    var diff1 = l.Length - (haveShrinkedLen + elbowShrink);
                    var diff2 = diff1 - 1000;
                    if (diff2 > 0)
                        return 1000;// 够放 1000 的变径
                    else if (diff1 >= 0)
                        return diff1;// 够放 <1000 的变径
                    else
                        throw new NotImplementedException("[CheckError]: Reducing len distribute error!");// 够放 <1000 的变径
                }
            }
            return 0;
        }
        private double GetMainElbowReducingShrink(Line l, double elbowShrink)
        {
            // 弯头后接变径，判断变径长度是否够1000
            var key = l.GetHashCode();
            if (mainLinesInfos.ContainsKey(key))
            {
                var haveShrinkedLen = mainLinesInfos[key].srcShrink + mainLinesInfos[key].dstShrink;
                var diff1 = l.Length - (haveShrinkedLen + elbowShrink);
                var diff2 = diff1 - 1000;
                if (diff2 > 0)
                    return 1000;// 够放 1000 的变径
                else if (diff1 >= 0)
                    return diff1;// 够放 <1000 的变径
                else
                    throw new NotImplementedException("[CheckError]: Reducing len distribute error!");// 够放 <1000 的变径
            }
            return -1;
        }
    }
}
