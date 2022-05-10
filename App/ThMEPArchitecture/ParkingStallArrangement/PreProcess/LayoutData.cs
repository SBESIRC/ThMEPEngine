using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.Tools;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using NetTopologySuite.Operation.Buffer;
using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
namespace ThMEPArchitecture.ParkingStallArrangement.PreProcess
{
    public  class LayoutData
    {
        // CAD 数据结构
        public List<Polyline> CAD_WallLines = new List<Polyline>();// 提取到的cad边界线
        public  List<Line> CAD_SegLines = new List<Line>();// 提取到的cad分割线
        public  List<Polyline> CAD_Obstacles = new List<Polyline>();//提取到的cad障碍物
        public  List<Polyline> CAD_Ramps = new List<Polyline>();// 提取到的cad坡道

        // NTS 数据结构
        public  Polygon WallLine;//初始边界线
        public  List<LineSegment> SegLines;// 初始分割线
        public  List<Polygon> Obstacles; // 初始障碍物,不包含坡道
        public  List<Ramp> Ramps = new List<Ramp>();// 坡道

        // NTS 衍生数据
        public  List<LineSegment> VaildLanes;//分割线等价车道线
        public  Polygon SegLineBoundary;//智能边界，外部障碍物为不可穿障碍物
        public  List<Polygon> InnerBuildings; //不可穿障碍物（中间障碍物）,包含坡道
        public  List<int> OuterBuildingIdxs; //可穿建筑物（外围障碍物）的index,包含坡道
        //public  List<Polygon> ObstacleBoundaries =new List<Polygon>();// 建筑物物直角轮廓，外扩合并得到(3000)直角多边形，用于算插入比
        public  List<Polygon> TightBoundaries = new List<Polygon>();//紧密轮廓，外扩合并+内缩2750得到。用于计算最大最小值
        public  List<Polygon> BoundingBoxes = new List<Polygon>();// 障碍物的外包框（矩形）
        public  List<Polygon> Buildings; // 初始障碍物,包含坡道

        // SpatialIndex
        public  MNTSSpatialIndex ObstacleSpatialIndex; // 初始障碍物,不包含坡道 的spatialindex
        public  MNTSSpatialIndex BuildingSpatialIndex; // 初始障碍物,包含坡道 的spatialindex
        public  MNTSSpatialIndex RampSpatialIndex; //坡道的spatial index
        public  MNTSSpatialIndex BoundarySpatialIndex;//边界 + 障碍物 + 坡道的spatialindex(所有边界）。用于判断车道宽
        public  MNTSSpatialIndex InnerObs_OutterBoundSPIDX;//中间障碍物（不可穿）+可穿障碍物的tightbound
        public  MNTSSpatialIndex BoundaryObjectsSPIDX;//边界打成断线+可忽略障碍物的spatialindex；
        public  MNTSSpatialIndex BoundLineSpatialIndex;//边界的打成碎线的spindex

        public  Dictionary<int, List<int>> SeglineIndexDic;//分割线连接关系
        public List<(double, double)> LowerUpperBound; // 基因的下边界和上边界，绝对值
        public  Serilog.Core.Logger Logger;
        public bool Init(AcadDatabase acadDatabase, Serilog.Core.Logger logger)
        {
            var block = InputData.SelectBlock(acadDatabase);//提取地库对象
            if (block is null)
            {
                return false;
            }
            Logger = logger;
            Logger?.Information("块名：" + block.GetEffectiveName());
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            string drawingName = Path.GetFileName(doc.Name);
            Logger?.Information("文件名：" + drawingName);
            if (!TryInit(block)) return false;
            //Show();
            if (SegLines.Count != 0)
            {
                bool Isvaild = SegLineVaild();
                //VaildLanes.ShowInitSegLine();
                if (!Isvaild) return false;
            }
            GetLowerUpperBound();
            //ShowLowerUpperBound();
            return true;
        }
        public  bool TryInit(BlockReference basement)
        {
            Explode(basement);
            if(CAD_WallLines.Count == 0)
            {
                Active.Editor.WriteMessage("地库边界不存在或者不闭合！");
                return false;
            }
            WallLine = CAD_WallLines.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().OrderBy(plgn => plgn.Area).Last();
            WallLine = WallLine.RemoveHoles();//初始墙线
            UpdateSegLines();
            var RampPolgons = GetRamps();
            UpdateWallLine(RampPolgons);
            UpdateObstacles();
            Buildings = RampPolgons;
            Buildings.AddRange(Obstacles);
            var boundaries = new List<Geometry> { WallLine.Shell };
            boundaries.AddRange(Buildings);
            BoundarySpatialIndex = new MNTSSpatialIndex(boundaries);
            PreProcess();
            //Active.Editor.WriteMessage("提取到" + Ramps.Count.ToString() + "个坡道");
            return true;
        }
        #region CAD数据提取
        private void Explode(BlockReference basement)
        {
            var dbObjs = new DBObjectCollection();
            basement.Explode(dbObjs);
            foreach (var obj in dbObjs)
            {
                var ent = obj as Entity;
                AddObj(ent);
            }
        }
        private void AddObj(Entity ent)
        {
            if (ent.Layer.ToUpper().Contains("地库边界"))
            {
                if (ent is Polyline pline)
                {
                    if (pline.Closed) CAD_WallLines.Add(pline);
                }
            }
            if (ent.Layer.ToUpper().Contains("障碍物"))
            {
                if (ent is BlockReference br)
                {
                    var dbObjs = new DBObjectCollection();
                    br.Explode(dbObjs);
                    foreach (var obj in dbObjs)
                    {
                        if (obj is Polyline pline)
                        {
                            if (pline.Closed) CAD_Obstacles.Add(pline);
                        }
                    }
                }
                else if (ent is Polyline pline) CAD_Obstacles.Add(pline);
            }
            if (ent.Layer.ToUpper().Contains("坡道"))
            {
                if (ent is BlockReference br)
                {
                    var dbObjs = new DBObjectCollection();
                    br.Explode(dbObjs);
                    foreach (var obj in dbObjs)
                    {
                        if (obj is Polyline pline)
                        {
                            if (pline.Closed) CAD_Ramps.Add(pline);
                        }
                    }
                }
                else if (ent is Polyline pline) CAD_Ramps.Add(pline);
            }
            if (ent.Layer.ToUpper().Contains("分割线"))
            {
                if (ent is Line line)
                {
                    CAD_SegLines.Add(line);
                }
            }
        }
        private void UpdateSegLines()
        {
            SegLines = CAD_SegLines.Select(segLine => segLine.ExtendLineEx(1, 3)).Select(l => l.ToNTSLineSegment()).ToList();
            RemoveSortSegLine();
        }
        private void RemoveSortSegLine()
        {
            //移除和内坡道连接的线
            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];

                if (segLine.Length < 100)
                {
                    SegLines.RemoveAt(i);
                }
            }
        }
        private void UpdateObstacles()
        {
            if (CAD_Obstacles.Count == 0) Obstacles = new List<Polygon>();
            var UnionedObstacles = new MultiPolygon(CAD_Obstacles.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
            Obstacles = UnionedObstacles.Get<Polygon>(true);
        }

        private void UpdateWallLine(List<Polygon> RampPolgons)//墙线合并
        {
            Geometry tempWallLine = WallLine;
            tempWallLine = tempWallLine.Difference(new MultiPolygon(RampPolgons.ToArray()));
            if (tempWallLine is Polygon poly)
                WallLine = poly.RemoveHoles();

            else if (tempWallLine is MultiPolygon mpoly)
                WallLine = mpoly.Geometries.Cast<Polygon>().Select(p => p.RemoveHoles()).OrderBy(p => p.Area).Last();
            WallLine = WallLine.RemoveHoles();
        }
        private List<Polygon> GetRamps()
        {
            if (CAD_Ramps.Count == 0) return new List<Polygon>();
            //移除和内坡道连接的只有一个交点的线
            var UnionedRamps = new MultiPolygon(CAD_Ramps.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
            var RampPolgons = UnionedRamps.Get<Polygon>(true);

            RampSpatialIndex = new MNTSSpatialIndex(RampPolgons);
            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];
                var ramp = RampSpatialIndex.SelectCrossingGeometry(new LineString(new Coordinate[] { segLine.P0, segLine.P1 })).Cast<Polygon>();
                if (ramp.Count() > 0)
                {
                    var insertpt = ramp.First().Shell.GetIntersectPts(segLine).First();
                    Ramps.Add(new Ramp(insertpt, ramp.First()));
                    if (SegLineEx.GetAllIntSecPs(i, SegLines, WallLine).Count < 2) SegLines.RemoveAt(i);//移除仅有一个交点的线
                }
            }
            return RampPolgons.ToList();
        }
        #endregion
        #region 预处理
        private void PreProcess()
        {
            if(Obstacles.Count == 0)
            {
                SegLineBoundary = WallLine;
            }
            else
            {
                ObstacleSpatialIndex = new MNTSSpatialIndex(Obstacles);
                if (Ramps.Count != 0) BuildingSpatialIndex = new MNTSSpatialIndex(Buildings);
                else BuildingSpatialIndex = ObstacleSpatialIndex;
                //UpdateObstacleBoundaries();
                GetSegLineBoundary();
                BoundLineSpatialIndex = new MNTSSpatialIndex(WallLine.Shell.ToLineStrings());
            }
        }
        private  void GetSegLineBoundary()//以半车道宽为基准外扩
        {
            var buffered = new MultiPolygon(Obstacles.ToArray()).Buffer((ParameterStock.RoadWidth / 2));//外扩
            var ObstacleBounds = buffered.Union().Get<Polygon>(true);
            var ObstacleBoundsGeo = new MultiPolygon(ObstacleBounds.ToArray());
            TightBoundaries = ObstacleBoundsGeo.Buffer(-(ParameterStock.RoadWidth / 2)).Get<Polygon>(true);
            BoundingBoxes = ObstacleBounds.Select(bound => ObstacleSpatialIndex.SelectCrossingGeometry(bound).GetEnvelope()).
                Where(envelope => envelope != null).ToList();// 用障碍物轮廓获取外包框
            var wallBound = WallLine.Buffer(-(ParameterStock.RoadWidth / 2));//边界内缩
            wallBound = wallBound.Difference(ObstacleBoundsGeo);//取差值

            if (wallBound is Polygon wpoly) wallBound = wpoly.RemoveHoles();
            else if (wallBound is MultiPolygon mpoly)
            {
                wallBound = mpoly.Geometries.Cast<Polygon>().Select(p => p.RemoveHoles()).OrderBy(p => p.Area).Last();//取最大的
            }
            var ignorableObstacles = ObstacleSpatialIndex.SelectNOTCrossingGeometry(wallBound).Cast<Polygon>().ToList();
            SegLineBoundary = wallBound as Polygon;
            InnerBuildings = BuildingSpatialIndex.SelectCrossingGeometry(wallBound).Cast<Polygon>().ToList();
            OuterBuildingIdxs = Buildings.Select((v, i) => new { v, i }).Where(x => !wallBound.Intersects(x.v)).Select(x => x.i).ToList();
               
            var OuterTightBounds = TightBoundaries.Where(b => !wallBound.Contains(b));
            InnerObs_OutterBoundSPIDX = new MNTSSpatialIndex(InnerBuildings.Concat(OuterTightBounds));
            var BoundaryObjects = new List<Geometry>();
            BoundaryObjects.AddRange(ignorableObstacles);
            BoundaryObjects.AddRange(WallLine.Shell.ToLineStrings());
            BoundaryObjectsSPIDX = new MNTSSpatialIndex(BoundaryObjects);
        }
        //private  void UpdateObstacleBoundaries()
        //{
        //    var distance = ParameterStock.BuildingTolerance;
        //    BufferParameters bufferParameters = new BufferParameters(8, EndCapStyle.Square, JoinStyle.Mitre, 5.0);
        //    var buffered = new MultiPolygon(Obstacles.ToArray()).Buffer(distance, bufferParameters);
        //    Geometry result = new MultiPolygon(buffered.Union().Get<Polygon>(true).ToArray());
        //    result = result.Buffer(-distance, bufferParameters);
        //    result = result.Intersection(WallLine);
        //    ObstacleBoundaries = result.Get<Polygon>(true);
        //}
        #endregion
        #region 分割线检查
        public bool SegLineVaild()
        {
            // 标记圆半径5000
            // 判断正交（中点标记）
            if (!IsOrthogonal()) return false;
            //VaildSegLines.ShowInitSegLine();
            // 判断每根分割线至少有两个交点(端点标记）
            if (!HaveAtLeastTwoIntsecPoints(true)) return false;
            // 先预切割
            SegLines.SeglinePrecut(WallLine);
            //获取有效分割线
            VaildLanes = SegLines.GetVaildLanes(WallLine,BoundaryObjectsSPIDX);
            // 判断分割线净宽（中点标记）
            if (!LaneWidthSatisfied()) return false;
            // 后预切割
            SegLines.SeglinePrecut(WallLine);
            // 判断每根分割线至少有两个交点(端点标记）
            if (!HaveAtLeastTwoIntsecPoints(false)) return false;
            // 判断车道是否全部相连（两个以上标记剩余中点，以下标记自己）
            if (!Allconnected()) return false;
            return true;
        }
        // 判断正交
        private  bool IsOrthogonal()
        {
            double tol = 0.02;// arctan 0.02 （1.146°）以下的交会自动归正
            for (int i = 0; i < SegLines.Count; i++)
            {

                var line = SegLines[i];
                var spt = line.P0;

                var ept = line.P1;
                //1. check parallel, perpendicular
                var X_dif = Math.Abs(spt.X - ept.X);
                var Y_dif = Math.Abs(spt.Y - ept.Y);
                if (Y_dif > X_dif)// 垂直线
                {
                    if (X_dif / Y_dif > tol)
                    {
                        Logger?.Information("发现非正交分割线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分割线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分割线 ！\n");
                        //Active.Editor.WriteMessage("起始点：" + spt.ToString() + "\n");
                        //Active.Editor.WriteMessage("终点：" + ept.ToString() + "\n");
                        line.MarkLineSeg(true);
                        return false;
                    }
                    var newX = (spt.X + ept.X) / 2;
                    spt = new Coordinate(newX, spt.Y);
                    ept = new Coordinate(newX, ept.Y);
                }
                if (X_dif > Y_dif)// 水平线
                {
                    if (Y_dif / X_dif > tol)
                    {
                        Logger?.Information("发现非正交分割线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分割线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分割线 ！\n");
                        //Active.Editor.WriteMessage("起始点：" + spt.ToString() + "\n");
                        //Active.Editor.WriteMessage("终点：" + ept.ToString() + "\n");
                        line.MarkLineSeg(true);
                        return false;
                    }
                    var newY = (spt.Y + ept.Y) / 2;
                    spt = new Coordinate(spt.X, newY);
                    ept = new Coordinate(ept.X, newY);
                }
                SegLines[i] = new LineSegment(spt, ept);
            }
            return true;
        }
        // 判断每根分割线至少有两个交点
        private  bool HaveAtLeastTwoIntsecPoints(bool beforeMove)
        {
            for (int i = 0; i < SegLines.Count; i++)
            {
                var intsecPtCnts = SegLineEx.GetAllIntSecPs(i, SegLines, WallLine).Count;
                if (intsecPtCnts < 2)
                {
                    string message;
                    if (beforeMove) message = "该分割线只有" + intsecPtCnts.ToString() + "个交点" + "\n";
                    else message = "移动后，该分割线只有" + intsecPtCnts.ToString() + "个交点" + "\n";
                    Logger?.Information(message);
                    Active.Editor.WriteMessage(message);
                    SegLines[i].MarkLineSeg(true);
                    return false;
                }
            }
            return true;
        }
        // 判断分割线净宽,如果不够移动一下在判断是否够
        private  bool LaneWidthSatisfied()
        {
            SeglineIndexDic = SegLines.GetSegLineIntsecDic();
            double tol = VMStock.RoadWidth ;// 5500 -0.1
            for (int i = 0; i < VaildLanes.Count; i++)
            {
                var vaildseg = VaildLanes[i];
                if (vaildseg == null) continue;
                var rect = vaildseg.GetRect(tol);
                var rst = BoundarySpatialIndex.SelectCrossingGeometry(rect);
                if (rst.Count > 0)//净宽不足
                {
                    // 移动两次上和下
                    var seglstr = vaildseg.ToLineString();
                    var initDist = rst.Min(geo => geo.Distance(seglstr));
                    if (!ExistWidthSatisfied(i, initDist))
                    {
                        Logger?.Information("分割线范围不够车道净宽！\n");
                        Active.Editor.WriteMessage("分割线范围不够车道净宽！\n");
                        SegLines[i].MarkLineSeg(true);
                        return false;
                    }
                }
            }
            VaildLanes = SegLines.GetVaildLanes(WallLine,BoundaryObjectsSPIDX);
            return VaildLaneWidthSatisfied();//判断移动后是否合理
        }
        private  bool ExistWidthSatisfied(int idx, double initDist)
        {
            var segLines_C = new List<LineSegment>();
            SegLines.ForEach(l => segLines_C.Add(l.Clone()));
            var segline = SegLines[idx];
            double moveSize = (ParameterStock.RoadWidth / 2) - initDist;
            segLines_C[idx] = SegLines[idx].Move(moveSize);
            if (SatisfiedAfterMove(idx, ref segLines_C)) return true;
            // 不满足，尝试另一个方向
            segLines_C[idx] = SegLines[idx].Move(-moveSize);
            if (SatisfiedAfterMove(idx, ref segLines_C)) return true;
            return false;

        }
        private  bool SatisfiedAfterMove(int idx, ref List<LineSegment> segLines_C)
        {
            double tol = ParameterStock.RoadWidth - 0.1;// 5500-0.1
            segLines_C.ExtendAndIntSect(SeglineIndexDic);//延长各分割线使之相交
            var vaildSeg = SegLineEx.GetVaildLane(idx, segLines_C, SegLineBoundary,BoundaryObjectsSPIDX);// 获取分割线有效部分
            if (vaildSeg == null) return false;//有效分割线无效
            var rect = vaildSeg.GetRect(tol);
            var rst = BoundarySpatialIndex.SelectCrossingGeometry(rect);
            if (rst.Count > 0) return false;
            SegLines.Clear();
            SegLines = segLines_C;//分割线满足需求
            return true;
        }
        private  bool VaildLaneWidthSatisfied()
        {
            double tol = ParameterStock.RoadWidth  - 0.1;// 5500-0.1
            bool flag = true;
            for (int i = 0; i < VaildLanes.Count; i++)
            {
                var segline = VaildLanes[i];
                if (segline== null) continue;
                var rect = segline.GetRect(tol);
                var rst = BoundarySpatialIndex.SelectCrossingGeometry(rect);
                if (rst.Count > 0)
                {
                    Logger?.Information("自动调整后分割线净宽仍然不够！\n");
                    Active.Editor.WriteMessage("自动调整后分割线净宽仍然不够！ \n");
                    SegLines[i].MarkLineSeg(true);
                    flag = false;
                }
            }
            return flag;
        }
        public  bool Allconnected()
        {
            var CheckedLines = new List<LineSegment>();
            CheckedLines.Add(SegLines[0]);
            var rest_idx = new List<int>();
            for (int i = 1; i < SegLines.Count; ++i) rest_idx.Add(i);

            while (rest_idx.Count != 0)
            {
                var curCount = rest_idx.Count;// 记录列表个数
                for (int j = 0; j < curCount; ++j)
                {
                    var idx = rest_idx[j];
                    var line = SegLines[idx];
                    if (line.ConnectWithAny(CheckedLines))
                    {
                        CheckedLines.Add(line);
                        rest_idx.RemoveAt(j);
                        break;
                    }
                    if (j == curCount - 1)
                    {
                        Logger?.Information("分割线未互相连接 ！\n");
                        Active.Editor.WriteMessage("分割线未互相连接！ \n");
                        if (CheckedLines.Count < 3)
                        {
                            foreach (var linetomark in CheckedLines)
                            {
                                linetomark.MarkLineSeg(true);
                            }
                        }
                        else
                        {
                            foreach (int idxtomark in rest_idx)
                            {
                                SegLines[idxtomark].MarkLineSeg(true);
                            }
                        }
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region 计算最大最小值
        public void GetLowerUpperBound()
        {
            LowerUpperBound = new List<(double, double)>();
            double HorzSize = WallLine.Coordinates.Max(c => c.X) - WallLine.Coordinates.Min(c => c.X);
            double VertSize = WallLine.Coordinates.Max(c => c.Y) - WallLine.Coordinates.Min(c => c.Y);
            var vaildSegs = SegLines.GetVaildSegLines(WallLine, 0);
            for (int i = 0; i < SegLines.Count; i++)
            {
                var vaildSeg = vaildSegs[i];
                if(vaildSeg == null)
                {
                    if (SegLines[i].IsVertical())
                        LowerUpperBound.Add((SegLines[i].P0.X - (VMStock.RoadWidth / 2), SegLines[i].P0.X + (VMStock.RoadWidth / 2)));
                    else
                        LowerUpperBound.Add((SegLines[i].P0.Y - (VMStock.RoadWidth / 2), SegLines[i].P0.Y + (VMStock.RoadWidth / 2)));
                    continue;
                }
                if (RampSpatialIndex?.SelectCrossingGeometry(SegLines[i].ToLineString()).Count > 0)
                {
                    if (SegLines[i].IsVertical())
                        LowerUpperBound.Add((SegLines[i].P0.X, SegLines[i].P0.X));
                    else
                        LowerUpperBound.Add((SegLines[i].P0.Y, SegLines[i].P0.Y));
                    continue;
                }
                if (vaildSeg.IsVertical())
                {
                    var MinMaxValue = GetMinMaxValue(vaildSegs[i], HorzSize);
                    var value = vaildSeg.P0.X;
                    LowerUpperBound.Add((MinMaxValue.Item1 + value, MinMaxValue.Item2 + value));
                }
                else
                {
                    var MinMaxValue = GetMinMaxValue(vaildSegs[i], VertSize);
                    var value = vaildSeg.P0.Y;
                    LowerUpperBound.Add((MinMaxValue.Item1 + value, MinMaxValue.Item2 + value));
                }
            }
        }
        public (double, double) GetMinMaxValue(LineSegment vaildSeg, double bufferSize)
        {
            double maxVal = 0;
            double minVal = 0;
            var ignorableObstacles = InnerObs_OutterBoundSPIDX.SelectCrossingGeometry(vaildSeg.GetRect(VMStock.RoadWidth-1)).Cast<Polygon>();
            var vaildSegLineStr = vaildSeg.ToLineString();
            var posBuffer = vaildSeg.GetHalfBuffer(bufferSize, true);
            var posObstacles = InnerObs_OutterBoundSPIDX.SelectCrossingGeometry(posBuffer).Cast<Polygon>().Except(ignorableObstacles);
            if (posObstacles.Count() > 0)
            {
                var multiPolygon = new MultiPolygon(posObstacles.ToArray());
                maxVal = vaildSegLineStr.Distance(multiPolygon) - (VMStock.RoadWidth / 2);//返回最近距离- 半车道宽
            }
            else
            {
                var boundLineStrs = BoundLineSpatialIndex.SelectCrossingGeometry(posBuffer).Cast<LineString>();
                if (boundLineStrs.Count() > 0)
                {
                    maxVal = boundLineStrs.Max(lstr => vaildSegLineStr.Distance(lstr));//返回最大距离 
                }
            }
            var negBuffer = vaildSeg.GetHalfBuffer(bufferSize, false);
            var negObstacles = InnerObs_OutterBoundSPIDX.SelectCrossingGeometry(negBuffer).Cast<Polygon>().Except(ignorableObstacles);
            if (negObstacles.Count() > 0)
            {
                var multiPolygon = new MultiPolygon(negObstacles.ToArray());
                minVal = -vaildSegLineStr.Distance(multiPolygon) + (VMStock.RoadWidth / 2);//返回最近距离- 半车道宽
            }
            else
            {
                var boundLineStrs = BoundLineSpatialIndex.SelectCrossingGeometry(negBuffer).Cast<LineString>();
                if (boundLineStrs.Count() > 0)
                {
                    minVal = -boundLineStrs.Max(lstr => vaildSegLineStr.Distance(lstr));//返回最大距离
                }
            }
            return (Math.Min(0, minVal), Math.Max(0, maxVal));
        }
        #endregion
        private void Show()
        {
            SegLineBoundary.ToDbMPolygon().AddToCurrentSpace();
            Buildings.ForEach(x => x.ToDbMPolygon().AddToCurrentSpace());
            TightBoundaries.ForEach(x => x.ToDbMPolygon().AddToCurrentSpace());
            BoundingBoxes.ForEach(x => x.ToDbMPolygon().AddToCurrentSpace());
        }
        public void ShowLowerUpperBound( string layer = "最大最小值")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 3);
            }

            for (int i = 0; i < VaildLanes.Count; i++)
            {
                LineSegment SegLine = VaildLanes[i];
                var lb = LowerUpperBound[i].Item1;
                var ub = LowerUpperBound[i].Item2;
                LinearRing shell;
                if (SegLine == null) continue;
                if (SegLine.IsVertical())
                {
                    var origion = new Coordinate(lb, SegLine.P0.Y);
                    var coors = new Coordinate[] { origion,
                                                    new Coordinate(lb, SegLine.P1.Y),
                                                    new Coordinate(ub, SegLine.P1.Y),
                                                    new Coordinate(ub, SegLine.P0.Y),origion};
                    shell = new LinearRing(coors);

                }
                else
                {
                    var origion = new Coordinate(SegLine.P0.X, lb);
                    var coors = new Coordinate[] { origion,
                                                    new Coordinate(SegLine.P1.X, lb),
                                                    new Coordinate(SegLine.P1.X, ub),
                                                    new Coordinate(SegLine.P0.X, ub),origion};
                    shell = new LinearRing(coors);

                }
                var poly = shell.ToDbPolyline();
                poly.Layer = layer;
                poly.ColorIndex = 3;
                poly.AddToCurrentSpace();
            }
        }
    }

    public static class NTSDrawEx
    {
        public static void MarkLineSeg(this LineSegment lineSeg,bool MarkMid = false,double Radius = 5000, string LayerName = "AI-提示")
        {
            if (MarkMid)
            {
                lineSeg.MidPoint.MarkPoint(Radius, LayerName);
            }
            else
            {
                lineSeg.P0.MarkPoint(Radius, LayerName);
                lineSeg.P1.MarkPoint(Radius, LayerName);
            }
        }
        public static void MarkPoint(this Coordinate coor, double Radius = 5000, string LayerName = "AI-提示")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(LayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, LayerName, 1);
                var circle = GetCADCircle(coor.GetCADPoint3d(), Radius);
                circle.Layer = LayerName;
                circle.ColorIndex = 2;
                circle.AddToCurrentSpace();
            }
        }
        public static Point3d GetCADPoint3d(this Coordinate coor)
        {
            return new Point3d(coor.X, coor.Y, 0);
        }

        public static Circle GetCADCircle(Point3d Center, double Radius)
        {
            var circle = new Circle();
            circle.Center = Center;
            circle.Radius = Radius;
            return circle;
        }

        public static void ShowInitSegLine(this List<LineSegment> seglines, string LayerName = "AI-初始车道线")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(LayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, LayerName, 4);
                foreach (var lseg in seglines)
                {
                    if (lseg == null) continue;
                    var l = lseg.ToDbLine();
                    l.Layer = LayerName;
                    l.ColorIndex = 6;
                    l.AddToCurrentSpace();
                }
            }
        }
    }
}
