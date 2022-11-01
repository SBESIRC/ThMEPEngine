﻿using AcHelper;
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
using ThMEPEngineCore.Algorithm;
using ThMEPArchitecture.MultiProcess;
using ThCADExtension;

namespace ThMEPArchitecture.ParkingStallArrangement.PreProcess
{
    public  class LayoutData
    {
        // CAD 数据结构
        public List<Polyline> CAD_WallLines = new List<Polyline>();// 提取到的cad边界线
        public  List<Line> CAD_SegLines = new List<Line>();// 提取到的cad分区线
        public  List<Polyline> CAD_Obstacles = new List<Polyline>();//提取到的cad障碍物
        public  List<Polyline> CAD_Ramps = new List<Polyline>();// 提取到的cad坡道
        public List<Circle> CAD_Anchors = new List<Circle>();// 提取到的cad锚点
        public List<int> FixedSegLineIdx = new List<int> ();//迭代范围为0的分区线的index
        // NTS 数据结构
        public  Polygon WallLine;//初始边界线
        public  List<LineSegment> SegLines = new List<LineSegment>();// 初始分区线
        public  List<Polygon> Obstacles; // 初始障碍物,不包含坡道
        public  List<Ramp> Ramps = new List<Ramp>();// 坡道
        public List<Anchor> Anchors = new List<Anchor>();
        // NTS 衍生数据
        public  List<LineSegment> VaildLanes;//分区线等价车道线
        public  Polygon SegLineBoundary;//智能边界，外部障碍物为不可穿障碍物
        public  List<Polygon> InnerBuildings; //不可穿障碍物（中间障碍物）,包含坡道
        public  List<int> OuterBuildingIdxs; //可穿建筑物（外围障碍物）的index,包含坡道
        //public List<Polygon> ObstacleBoundaries = new List<Polygon>();// 建筑物物直角轮廓，外扩合并得到(3000)直角多边形，用于算插入比
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

        public List<List<int>> SeglineIndexList;//分区线连接关系
        public List<(bool, bool)> SeglineConnectToBound;//分区线（负，正）方向是否与边界连接
        public List<(int,int,int,int)> SegLineIntSecNode = new List<(int, int, int, int)>();//四岔节点关系，上下左右的分区线index

        public List<(double, double)> LowerUpperBound; // 基因的下边界和上边界，绝对值
        public  Serilog.Core.Logger Logger;
        private double CloseTol = 5.0;
        public bool Init(BlockReference block, Serilog.Core.Logger logger, bool extractSegLine = true, bool UpdateRelationship = false)
        {
            Logger = logger;

            if (!TryInit(block, extractSegLine)) return false;
            //Show();
            if (extractSegLine)
            {
                return ProcessSegLines(null,true,UpdateRelationship);
            }
            return true;
        }
        public bool ProcessSegLines(List<LineSegment> AutoSegLines = null,bool checkVaild = true,bool UpdateRelationship = false)
        {
            if (AutoSegLines != null) SegLines = AutoSegLines.Select(l => l.Extend(1)).ToList();
            //SegLines = SegLines.RemoveDuplicated(10);
            if (checkVaild)
            {
                if (SegLines.Count < 2)
                {
                    ThMPArrangementCmd.DisplayLogger.Information("分区线个数至少为2，请检查图层以及线的类型（直线）");
                    Active.Editor.WriteLine("分区线个数至少为2，请检查图层以及线的类型（直线）");
                    return false;
                }
                bool Isvaild = SegLineVaild();
                //VaildLanes.ShowInitSegLine();
                if (!Isvaild) return false;
            }
            if(UpdateRelationship)
            {
                var CrossPts = SegLines.GetCrossPoints(WallLine);
                var BreakedSegLines = new List<LineSegment>();
                var newFixedSegLines = SegLines.Slice(FixedSegLineIdx);
                //基于交点打断
                for (int i = 0; i < SegLines.Count; i++)
                {
                    var segLine = SegLines[i];
                    var breakded = segLine.Split(CrossPts.Select(c => c.Coordinate).ToList());
                    BreakedSegLines.AddRange(breakded);
                }
                SegLines = BreakedSegLines.CleanLineWithOneIntSecPt(WallLine);
                //获取连接关系
                SeglineIndexList = SegLines.GetSegLineIntsecList();
                SeglineConnectToBound = SegLines.GetSeglineConnectToBound(WallLine);
                //获取交点关系
                SegLineIntSecNode = SegLines.GetSegLineIntSecNode(CrossPts);
                GetLowerUpperBound(newFixedSegLines);
                //ShowLowerUpperBound();
            }
            return true;
        }
        public  bool TryInit(BlockReference basement, bool extractSegLine )
        {
            Explode(basement);
            if(CAD_WallLines.Count == 0)
            {
                ThMPArrangementCmd.DisplayLogger.Information("地库边界不存在或者不闭合！");
                Active.Editor.WriteMessage("地库边界不存在或者不闭合！");
                return false;
            }
            WallLine = CAD_WallLines.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().OrderBy(plgn => plgn.Area).Last();
            WallLine = WallLine.RemoveHoles();//初始墙线
            if(extractSegLine)UpdateSegLines();
            var RampPolgons = UpdateWallLine();
            UpdateRamps(RampPolgons);
            UpdateObstacles();
            if (Obstacles.Count == 0)
            {
                ThMPArrangementCmd.DisplayLogger.Information("未提取到建筑物！请检查建筑物图层以线型(多段线)，并且确保多段线闭合");
                Active.Editor.WriteMessage("未提取到建筑物！请检查建筑物图层以及线型(多段线)，并且确保多段线闭合");
                return false;
            }
            UpdateAnchors();
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
            var layerName = ent.Layer.ToUpper();
            if (layerName.Contains("地库边界"))
            {
                if (ent is Polyline pline)
                {
                    if (pline.IsVaild(CloseTol))
                    {
                        CAD_WallLines.Add(pline.GetClosed());
                    }
                }
            }
            if (layerName.Contains("障碍物"))
            {
                if (ent is BlockReference br)
                {
                    var dbObjs = new DBObjectCollection();
                    br.Explode(dbObjs);
                    foreach (var obj in dbObjs)
                    {
                        if (obj is Polyline pline)
                        {
                            if (pline.IsVaild(CloseTol))
                            {
                                CAD_Obstacles.Add(pline.GetClosed());
                            }
                        }
                    }
                }
                else if (ent is Polyline pline)
                {
                    if (pline.IsVaild(CloseTol))
                    {
                        CAD_Obstacles.Add(pline.GetClosed());
                    }
                }
            }
            if (layerName.Contains("坡道"))
            {
                if (ent is BlockReference br)
                {
                    var dbObjs = new DBObjectCollection();
                    br.Explode(dbObjs);
                    foreach (var obj in dbObjs)
                    {
                        if (obj is Polyline pline)
                        {
                            if (pline.IsVaild(CloseTol))
                            {
                                CAD_Ramps.Add(pline.GetClosed());
                            }
                        }
                    }
                }
                else if (ent is Polyline pline)
                {
                    if (pline.IsVaild( CloseTol))
                    {
                        CAD_Ramps.Add(pline.GetClosed());
                    }
                }
            }
            if (layerName.Contains("分割线")|| layerName.Contains("分区线"))
            {
                if (ent is Line line)
                {
                    if(line.Length > 1000)
                    {
                        CAD_SegLines.Add(line);
                        if (layerName.Contains("固定")) FixedSegLineIdx.Add(CAD_SegLines.Count - 1);
                    }
                }
            }
            if (layerName.Contains("锚点"))
            {
                if (ent is BlockReference br)
                {
                    var dbObjs = new DBObjectCollection();
                    br.Explode(dbObjs);
                    foreach (var obj in dbObjs)
                    {
                        if (obj is Circle circle)
                        {
                            CAD_Anchors.Add(circle);
                        }
                    }
                }
                else if (ent is Circle circle)
                {
                    CAD_Anchors.Add(circle);
                }
            }
        }
        private void UpdateSegLines()
        {
            SegLines = CAD_SegLines.Select(segLine => segLine.ExtendLine(1)).Select(l => l.ToNTSLineSegment()).ToList();
            //RemoveSortSegLine();
        }
        private void RemoveSortSegLine()
        {
            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];

                if (segLine.Length < 100)
                {
                    SegLines.RemoveAt(i);
                    FixedSegLineIdx.Remove(i);
                }
            }
        }
        private void UpdateObstacles()
        {
            if (CAD_Obstacles.Count == 0) Obstacles = new List<Polygon>();
            var UnionedObstacles = new MultiPolygon(CAD_Obstacles.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
            Obstacles = UnionedObstacles.Get<Polygon>(true);
        }
        private List<Polygon> UpdateWallLine()//墙线合并
        {
            List<Polygon> RampPolgons = new List<Polygon>();
            if (CAD_Ramps.Count > 0)
            {
                var UnionedRamps = new MultiPolygon(CAD_Ramps.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
                RampPolgons = UnionedRamps.Get<Polygon>(true);
            }
            Geometry tempWallLine = WallLine;
            tempWallLine = tempWallLine.Difference(new MultiPolygon(RampPolgons.ToArray()));
            if (tempWallLine is Polygon poly)
                WallLine = poly.RemoveHoles();

            else if (tempWallLine is MultiPolygon mpoly)
                WallLine = mpoly.Geometries.Cast<Polygon>().Select(p => p.RemoveHoles()).OrderBy(p => p.Area).Last();
            WallLine = WallLine.RemoveHoles();
            return RampPolgons;
        }
        private void UpdateRamps(List<Polygon> RampPolgons)
        {
            //移除和内坡道连接的只有一个交点的线
            RampSpatialIndex = new MNTSSpatialIndex(RampPolgons);

            var InnerRampSpatialIndex = new MNTSSpatialIndex(RampPolgons.Where(p =>WallLine.Contains(p.Centroid)));

            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];
                var ramp = InnerRampSpatialIndex.SelectCrossingGeometry(new LineString(new Coordinate[] { segLine.P0, segLine.P1 })).Cast<Polygon>();
                if (ramp.Count() > 0)
                {
                    var insertpt = ramp.First().Shell.GetIntersectPts(segLine).First();
                    Ramps.Add(new Ramp(insertpt, ramp.First()));
                    if (SegLineEx.GetAllIntSecPs(i, SegLines, WallLine).Count < 2) SegLines.RemoveAt(i);//移除仅有一个交点的线
                }
            }
        }
        private void UpdateAnchors()
        {
            CAD_Anchors.ForEach(a => Anchors.Add(new Anchor(a.Center, a.Radius)));
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
                BuildingSpatialIndex = new MNTSSpatialIndex(Buildings);
                UpdateObstacleBoundaries();
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
            //wallBound.Get<Polygon>(false).ForEach(p => p.ToDbMPolygon().AddToCurrentSpace());
            if (wallBound is Polygon wpoly) wallBound = wpoly.RemoveHoles();
            else if (wallBound is MultiPolygon mpoly)
            {
                wallBound = mpoly.Geometries.Cast<Polygon>().Select(p => p.RemoveHoles()).OrderBy(p => p.Area).Last();//取最大的
            }
            var ignorableObstacles = ObstacleSpatialIndex.SelectNOTCrossingGeometry(wallBound).Cast<Polygon>().ToList();
            SegLineBoundary = wallBound as Polygon;
            InnerBuildings = BuildingSpatialIndex.SelectCrossingGeometry(wallBound).Cast<Polygon>().ToList();
            OuterBuildingIdxs = Buildings.Select((v, i) => new { v, i }).Where(x => !wallBound.Intersects(x.v)).Select(x => x.i).ToList();
               
            var outerBounds = RampSpatialIndex.SelectNOTCrossingGeometry(wallBound);//外部坡道
            outerBounds.AddRange(TightBoundaries.Where(b => !wallBound.Contains(b)));//外部障碍物的tight边界
            InnerObs_OutterBoundSPIDX = new MNTSSpatialIndex(InnerBuildings.Concat(outerBounds));
            var BoundaryObjects = new List<Geometry>();
            BoundaryObjects.AddRange(ignorableObstacles);
            BoundaryObjects.AddRange(WallLine.Shell.ToLineStrings());
            BoundaryObjectsSPIDX = new MNTSSpatialIndex(BoundaryObjects);
        }
        private void UpdateObstacleBoundaries()
        {
            var distance = ParameterStock.BuildingTolerance;
            BufferParameters bufferParameters = new BufferParameters(8, EndCapStyle.Square, JoinStyle.Mitre, 5.0);
            var buffered = new MultiPolygon(Buildings.ToArray()).Buffer(distance, bufferParameters);
            Geometry result = new MultiPolygon(buffered.Union().Get<Polygon>(true).ToArray());
            result = result.Buffer(-distance, bufferParameters);
            result = result.Intersection(WallLine);
            //ObstacleBoundaries = result.Get<Polygon>(true);
            var mmtoM = 0.001 * 0.001;
            ParameterStock.TotalArea = WallLine.Buffer(ParameterStock.WallLineThickness,bufferParameters).Area*mmtoM;
            ParkingStallArrangementViewModel.TotalArea= WallLine.Buffer(ParameterStock.WallLineThickness, bufferParameters).Area * mmtoM;
            ParameterStock.BuildingArea = result.Area * mmtoM; 
            Logger?.Information($"地库总面积:"+ string.Format("{0:N1}", ParameterStock.TotalArea) + "m" + Convert.ToChar(0x00b2) );
            Logger?.Information($"地库内部建筑物总面积:" + string.Format("{0:N1}", ParameterStock.BuildingArea) + "m" + Convert.ToChar(0x00b2) );
        }
        #endregion
        #region 分区线检查
        public bool SegLineVaild()
        {
            // 标记圆半径5000
            // 判断正交（中点标记）
            if (!IsOrthogonal()) return false;
            //VaildSegLines.ShowInitSegLine();
            // 判断每根分区线至少有两个交点(端点标记）
            if (!HaveAtLeastTwoIntsecPoints(true)) return false;
            // 先预切割
            SegLines.SeglinePrecut(WallLine);
            SeglineIndexList = SegLines.GetSegLineIntsecList();
            SeglineConnectToBound = SegLines.GetSeglineConnectToBound(WallLine);
            //获取有效分区线
            VaildLanes = SegLines.GetVaildLanes(WallLine,BoundaryObjectsSPIDX);
            // 判断分区线净宽（中点标记）
            if (!LaneWidthSatisfied()) return false;
            // 后预切割
            SegLines.SeglinePrecut(WallLine);
            // 判断每根分区线至少有两个交点(端点标记）
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
                        ThMPArrangementCmd.DisplayLogger.Information("发现非正交分区线 ！");
                        Logger?.Information("发现非正交分区线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分区线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分区线 ！\n");
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
                        Logger?.Information("发现非正交分区线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分区线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分区线 ！\n");
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
        // 判断每根分区线至少有两个交点
        private  bool HaveAtLeastTwoIntsecPoints(bool beforeMove)
        {
            for (int i = 0; i < SegLines.Count; i++)
            {
                var intsecPtCnts = SegLineEx.GetAllIntSecPs(i, SegLines, WallLine).Count;
                if (intsecPtCnts < 2)
                {
                    string message;
                    if (beforeMove) message = "该分区线只有" + intsecPtCnts.ToString() + "个交点" + "\n";
                    else message = "自动调整后，该分区线只有" + intsecPtCnts.ToString() + "个交点" + "\n";
                    Logger?.Information(message);
                    ThMPArrangementCmd.DisplayLogger.Information(message);
                    Active.Editor.WriteMessage(message);
                    SegLines[i].MarkLineSeg(true);
                    return false;
                }
            }
            return true;
        }
        // 判断分区线净宽,如果不够移动一下在判断是否够
        private  bool LaneWidthSatisfied()
        {
            
            double tol = VMStock.RoadWidth -0.1;// 5500 -0.1
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
                        ThMPArrangementCmd.DisplayLogger.Information("分区线范围不够车道净宽！");
                        Logger?.Information("分区线范围不够车道净宽！\n");
                        Active.Editor.WriteMessage("分区线范围不够车道净宽！\n");
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
            segLines_C.ExtendAndIntSect(SeglineIndexList);//延长各分区线使之相交
            segLines_C[idx] = segLines_C[idx].ExtendToBound(WallLine, SeglineConnectToBound[idx]);
            var vaildSeg = SegLineEx.GetVaildLane(idx, segLines_C, SegLineBoundary,BoundaryObjectsSPIDX);// 获取分区线有效部分
            if (vaildSeg == null) return false;//有效分区线无效
            var rect = vaildSeg.GetRect(tol);
            var rst = BoundarySpatialIndex.SelectCrossingGeometry(rect);
            if (rst.Count > 0) return false;
            SegLines.Clear();
            SegLines = segLines_C;//分区线满足需求
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
                    ThMPArrangementCmd.DisplayLogger.Information("自动调整后分区线净宽仍然不够！");
                    Logger?.Information("自动调整后分区线净宽仍然不够！\n");
                    Active.Editor.WriteMessage("自动调整后分区线净宽仍然不够！ \n");
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
                        ThMPArrangementCmd.DisplayLogger.Information("分区线未互相连接 ！");
                        Logger?.Information("分区线未互相连接 ！\n");
                        Active.Editor.WriteMessage("分区线未互相连接！ \n");
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
        public void GetLowerUpperBound(List<LineSegment> newFixedSegLines)
        {
            LowerUpperBound = new List<(double, double)>();
            double HorzSize = WallLine.Coordinates.Max(c => c.X) - WallLine.Coordinates.Min(c => c.X);
            double VertSize = WallLine.Coordinates.Max(c => c.Y) - WallLine.Coordinates.Min(c => c.Y);
            var vaildSegs = SegLines.GetVaildSegLines(WallLine, 0);
            for (int i = 0; i < SegLines.Count; i++)
            {
                if (RampSpatialIndex?.SelectCrossingGeometry(SegLines[i].ToLineString()).Count > 0||
                    Anchors.Any(a => SegLines[i].Distance(a.Center)<=a.Radius) ||
                    newFixedSegLines.Any(l =>l.IsSubSetOf(SegLines[i])))
                {
                    if (SegLines[i].IsVertical())
                        LowerUpperBound.Add((SegLines[i].P0.X, SegLines[i].P0.X));
                    else
                        LowerUpperBound.Add((SegLines[i].P0.Y, SegLines[i].P0.Y));
                    continue;
                }
                var vaildSeg = vaildSegs[i];
                if (vaildSeg == null)
                {
                    if (SegLines[i].IsVertical())
                        LowerUpperBound.Add((SegLines[i].P0.X - (VMStock.RoadWidth / 2), SegLines[i].P0.X + (VMStock.RoadWidth / 2)));
                    else
                        LowerUpperBound.Add((SegLines[i].P0.Y - (VMStock.RoadWidth / 2), SegLines[i].P0.Y + (VMStock.RoadWidth / 2)));
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
                var multiLstr = new MultiLineString(boundLineStrs.ToArray());
                var pts = posBuffer.Intersection(multiLstr).Coordinates;
                if (boundLineStrs.Count() > 0)
                {
                    //maxVal = boundLineStrs.Max(lstr => vaildSegLineStr.Distance(lstr));//返回最大距离 
                    maxVal = pts.Max(pt => vaildSeg.Distance(pt)) + 500;//返回最大距离 
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
                var multiLstr = new MultiLineString(boundLineStrs.ToArray());
                var pts = negBuffer.Intersection(multiLstr).Coordinates;
                if (boundLineStrs.Count() > 0)
                {
                    //minVal = -boundLineStrs.Max(lstr => vaildSegLineStr.Distance(lstr));//返回最大距离
                    minVal = -pts.Max(pt => vaildSeg.Distance(pt)) - 500;//返回最大距离 
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
            var vaildSegs = SegLines.GetVaildSegLines(WallLine, 0);
            for (int i = 0; i < vaildSegs.Count; i++)
            {
                LineSegment SegLine = vaildSegs[i];
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
    public class Anchor
    {
        public Coordinate Center;
        public double Radius;

        public Anchor(Point3d center, double radius)
        {
            Center = center.ToNTSCoordinate();
            Radius = radius;
        }
    }
}
