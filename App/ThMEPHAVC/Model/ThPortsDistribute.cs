using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;

namespace ThMEPHVAC.Model
{
    public class ThPortsDistribute
    {
        public List<Point2d> dirAlignPoints;
        public List<Point2d> verAlignPoints;
        private Point3d startPos;
        private double alignLowLimit;
        private double alignHeighLimit;
        private PortParam portParam;
        private List<Line> vGridSet;
        private List<Line> hGridSet;
        public ThPortsDistribute(PortParam portParam, List<EndlineInfo> endlines)
        {
            // 确定墙点后设置风口位置
            Init(portParam);
            bool needAdjustPort = (portParam.genStyle == GenerationStyle.Auto);
            if (needAdjustPort)
            {
                if (Math.Abs(portParam.portInterval) < 1e-3)
                    SetPortPosition(endlines, true);
                else
                    SetPortPosition(endlines, false);
                var grids = GetGridLines();
                if (grids.Count > 0)
                {
                    MoveToOrg(grids);
                    var gridLines = FilterHVGridLine(grids);
                    grids.Clear();
                    SeperateVAndHGrid(gridLines, vGridSet, hGridSet);
                    DetectAlignGrid(endlines);
                }
            }
        }
        private void DetectAlignGrid(List<EndlineInfo> endlines)
        {
            var XCoor = CollectXCoor();
            var YCoor = CollectYCoor();
            foreach (var endline in endlines)
            {
                foreach (var seg in endline.endlines.Values)
                {
                    var dirVec = ThMEPHVACService.GetEdgeDirection(seg.seg.l);
                    Vector3d disOft;
                    if (XCoor.Count > 0 &&(ThMEPHVACService.IsCollinear(dirVec, Vector3d.XAxis) || ThMEPHVACService.IsCollinear(dirVec, -Vector3d.XAxis)))
                    {
                        disOft = GetXAlignOft(XCoor, seg);
                    }
                    else if (YCoor.Count > 0 && (ThMEPHVACService.IsCollinear(dirVec, Vector3d.YAxis) || ThMEPHVACService.IsCollinear(dirVec, -Vector3d.YAxis)))
                    {
                        disOft = GetYAlignOft(YCoor, seg);
                    }
                    else
                    {
                        // 非水平垂直不处理
                        disOft = Point3d.Origin.GetAsVector();
                    }
                    foreach (var port in seg.portsInfo)
                        port.position -= disOft;
                }
            }
        }
        private Vector3d GetYAlignOft(Dictionary<int, Line> YCoor, EndlineSegInfo seg)
        {
            var Ys = CollectPortYCoor(seg.portsInfo, out Dictionary<int, Point3d> dicYtoPoint);
            Ys.AddRange(YCoor.Keys);
            Ys.Sort();
            var dicDiff = CoorDiff(Ys);
            var tup = SearchCloestAlignLine(YCoor, dicDiff, dicYtoPoint);
            if (tup.Item2.IsEqualTo(Point3d.Origin))
                return Point3d.Origin.GetAsVector();
            return GetPointOft(tup.Item1, tup.Item2, seg);
        }
        private Vector3d GetXAlignOft(Dictionary<int, Line> XCoor, EndlineSegInfo seg)
        {
            var Xs = CollectPortXCoor(seg.portsInfo, out Dictionary<int, Point3d> dicXtoPoint);
            Xs.AddRange(XCoor.Keys);
            Xs.Sort();
            var dicDiff = CoorDiff(Xs);
            var tup = SearchCloestAlignLine(XCoor, dicDiff, dicXtoPoint);
            if (tup.Item2.IsEqualTo(Point3d.Origin))
                return Point3d.Origin.GetAsVector();
            return GetPointOft(tup.Item1, tup.Item2, seg);
        }
        private Vector3d GetPointOft(Line l, Point3d p, EndlineSegInfo seg)
        {
            var p2 = ThMEPHVACService.GetVerticalPoint(p.ToPoint2D(), l);
            var p3 = new Point3d(p2.X, p2.Y, 0);
            seg.dirAlignPoint = p3;// 更新对齐点

            var dis = p.DistanceTo(p3);
            var roundDis = ((int)(Math.Ceiling(dis / 100)) * 100);
            var diffDis = dis - roundDis;
            var dirVec = (p - p3).GetNormal();
            return dirVec * diffDis;
        }
        private Tuple<Line, Point3d> SearchCloestAlignLine(Dictionary<int, Line> coors, Dictionary<int, Tuple<int, int>> dicDiff, Dictionary<int, Point3d> dicXtoPoint)
        {
            var sortedDiff = dicDiff.Keys.ToList();
            sortedDiff.Sort();
            // 从最小的坐标差值遍历，直到找到一个包含在对其坐标中的值
            foreach (var key in sortedDiff)
            {
                var diff = dicDiff[key];
                if (key < alignLowLimit || key > alignHeighLimit)
                    continue;
                var num1 = diff.Item1;
                var num2 = diff.Item2;
                if (coors.ContainsKey(num1) && dicXtoPoint.ContainsKey(num2))
                    return new Tuple<Line, Point3d>(coors[num1], dicXtoPoint[num2]);
                if (coors.ContainsKey(num2) && dicXtoPoint.ContainsKey(num1))
                    return new Tuple<Line, Point3d>(coors[num2], dicXtoPoint[num1]);
            }
            return new Tuple<Line, Point3d>(new Line(), Point3d.Origin);
        }
        private Dictionary<int, Tuple<int, int>> CoorDiff(List<int> coors)
        {
            var Dis = new Dictionary<int, Tuple<int, int>>();
            for (int i = 1; i < coors.Count; ++i)
            {
                int diff = Math.Abs(coors[i - 1] - coors[i]);
                if (!Dis.ContainsKey(diff))//过滤掉相同间距的点
                {
                    Dis.Add(diff, new Tuple<int, int>(coors[i - 1], coors[i]));
                }
            }
            return Dis;
        }
        private List<int> CollectPortYCoor(List<PortInfo> portsInfo, out Dictionary<int, Point3d> dicYtoPoint)
        {
            var Ys = new List<int>();
            dicYtoPoint = new Dictionary<int, Point3d>();
            foreach (var port in portsInfo)
            {
                Ys.Add((int)port.position.Y);
                dicYtoPoint.Add((int)port.position.Y, port.position);
            }
            return Ys;
        }
        private List<int> CollectPortXCoor(List<PortInfo> portsInfo, out Dictionary<int, Point3d> dicXtoPoint)
        {
            var Xs = new List<int>();
            dicXtoPoint = new Dictionary<int, Point3d>();
            foreach (var port in portsInfo)
            {
                Xs.Add((int)port.position.X);
                dicXtoPoint.Add((int)port.position.X, port.position);
            }
            return Xs;
        }
        private Dictionary<int, Line> CollectYCoor()
        {
            var dic = new Dictionary<int, Line>();
            foreach (Line l in hGridSet)
            {
                int Y = (int)l.StartPoint.Y;
                if (!dic.ContainsKey(Y))// 轴网间距不可能小于一毫米
                    dic.Add(Y, l);
            }
            return dic;
        }
        private Dictionary<int, Line> CollectXCoor()
        {
            var dic = new Dictionary<int, Line>();
            foreach (Line l in vGridSet)
            {
                int X = (int)l.StartPoint.X;
                if (!dic.ContainsKey(X))// 轴网间距不可能小于一毫米
                    dic.Add(X, l);
            }
            return dic;
        }
        private void SetPortPosition(List<EndlineInfo> endlines, bool isAuto)
        {
            // 最末端的管段从末端-200处开始分配风口，其他管段居中分配风口
            // 注意末端管的最后一段管段一定有风口
            ThMEPHVACService.GetWidthAndHeight(portParam.param.portSize, out double w, out double h);
            var x = Math.Max(w, h);
            var endInterval = 200;
            var firstOftDis = portParam.verticalPipeEnable ? 
                endInterval + (x + 200) * 0.5 : endInterval + x * 0.5;// 有立管时风口宽+200
            foreach (var endline in endlines)
            {
                var endEndline = endline.endlines.Values.First();// 最后一段管段
                var firstEndline = endline.endlines.Values.LastOrDefault();// 最后一段管段
                foreach (var seg in endline.endlines.Values)
                {
                    if (seg.portNum == 1)
                    {
                        if (!endEndline.Equals(seg))
                            seg.portsInfo.FirstOrDefault().position = ThMEPHVACService.GetMidPoint(seg.seg.l.EndPoint, seg.seg.l.StartPoint);
                        else
                        {
                            var dirVec = ThMEPHVACService.GetEdgeDirection(seg.seg.l);
                            seg.portsInfo.FirstOrDefault().position = seg.seg.l.EndPoint - dirVec * firstOftDis;
                        }
                    }
                    else
                    {
                        var shrinkedLine = seg.seg.GetShrinkedLine();
                        var len = firstEndline.Equals(seg) ? 1300 : 0;
                        var portStep = isAuto ? ThMEPHVACService.RoundNum((shrinkedLine.Length - (x + 400 + 340 + len)) / (seg.portNum - 1), 100) : portParam.portInterval;
                        var dirVec = ThMEPHVACService.GetEdgeDirection(shrinkedLine);
                        //var firstOftDis = endEndline.Equals(seg) ? 200 : portStep;末端偏移200，中间端居中
                        var p = shrinkedLine.EndPoint - firstOftDis * dirVec;
                        foreach (var port in seg.portsInfo)
                        {
                            port.position = p;
                            p -= (dirVec * portStep);
                        }
                        var lastPosition = (p + (dirVec * portStep));// 恢复到前一个点
                        AdjustLastPoint(seg.portsInfo, dirVec, lastPosition, shrinkedLine.StartPoint);
                    }
                }
            }
        }

        private void AdjustLastPoint(List<PortInfo> portsInfo, Vector3d dirVec, Point3d p, Point3d startPoint)
        {
            if (portsInfo.Count > 0)
            {
                var tor = new Tolerance(1.5, 1.5);
                var otherVec = (p - startPoint).GetNormal();
                if (!dirVec.IsEqualTo(otherVec, tor))
                {
                    // 若反向，p在line之外
                    var port = portsInfo.LastOrDefault();
                    var dis = p.DistanceTo(startPoint);
                    dis = ThMEPHVACService.RoundNum(dis, 100);
                    if (dis < portParam.portInterval)
                    {
                        // 根据startPoint对称后可以放下
                        port.position = p + 2 * dis * dirVec;
                    }
                    else
                    {
                        // 对称后放不下
                        dis = ThMEPHVACService.RoundNum(portParam.portInterval / 4, 10);
                        port.position = p + dis * dirVec;
                    }
                }
            }
            
        }

        private void Init(PortParam portParam)
        {   
            alignLowLimit = 500;
            alignHeighLimit = 1e5;
            this.portParam = portParam;
            startPos = portParam.srtPoint;
            dirAlignPoints = new List<Point2d>();
            verAlignPoints = new List<Point2d>();
            vGridSet = new List<Line>();
            hGridSet = new List<Line>();
        }
        private DBObjectCollection FilterHVGridLine(DBObjectCollection gridLines)
        {
            var lins = new DBObjectCollection();
            for (int i = 0; i < gridLines.Count; ++i)
            {
                var l = gridLines[i] as Line;
                if (ThMEPHVACService.IsVertical(l) || ThMEPHVACService.IsHorizontal(l))
                    lins.Add(l);
            }
            return lins;
        }
        private void MoveToOrg(DBObjectCollection gridLines)
        {
            var mat = Matrix3d.Displacement(-startPos.GetAsVector());
            foreach (Line l in gridLines)
            {
                l.TransformBy(mat);
            }
        }
        private void SeperateVAndHGrid(DBObjectCollection crossingLines, List<Line> vSet, List<Line> hSet)
        {
            foreach (Line l in crossingLines)
            {
                if (ThMEPHVACService.IsVertical(l))
                    vSet.Add(l);
                else if (ThMEPHVACService.IsHorizontal(l))
                    hSet.Add(l);
            }
        }
        private DBObjectCollection GetGridLines()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThAXISLineRecognitionEngine();
                engine.Recognize(acadDatabase.Database, new Point3dCollection());
                return engine.Elements.Where(o=>o.Outline is Line).Select(o => o.Outline).ToCollection();
            }
        }
    }
}