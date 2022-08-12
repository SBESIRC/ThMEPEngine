using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.MultiProcess;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
using SegLineEx = ThParkingStall.Core.OTools.SegLineEx;

namespace ThMEPArchitecture.ParkingStallArrangement.PreProcess
{
    public class OLayoutData
    {
        public List<Polyline> CAD_WallLines = new List<Polyline>();// 提取到的cad边界线
        public List<Line> CAD_BorderLines = new List<Line>();//提取到的可移动边界线

        public List<Line> CAD_SegLines = new List<Line>();// 提取到的cad分区线
        public List<Polyline> CAD_Obstacles = new List<Polyline>();//提取到的cad障碍物
        public List<Polyline> CAD_Ramps = new List<Polyline>();// 提取到的cad坡道

        // NTS 数据结构
        //public Polygon Basement;//地库，面域部分为可布置区域
        public Polygon WallLine;//初始边界线
        public List<LineSegment> BorderLines;//可动边界线
        public List<SegLine> SegLines = new List<SegLine>();// 初始分区线
        public List<Polygon> Obstacles; // 初始障碍物,不包含坡道
        public List<Polygon> RampPolgons;//坡道polygon

        double MaxArea;//最大地库面积
        // NTS 衍生数据
        public List<ORamp> Ramps = new List<ORamp>();// 坡道
        public List<Polygon> Buildings; // 初始障碍物,包含坡道
        //public Polygon SegLineBoundary;//智能边界，外部障碍物为可穿障碍物
        public Polygon BaseLineBoundary;//基线边界（包含内部孔），基线边界内的分割线的部分用来求基线

        //public List<LineSegment> VaildLanes;//分区线等价车道线
        //public List<Polygon> InnerBuildings; //不可穿障碍物（中间障碍物）,包含坡道
        //public List<int> OuterBuildingIdxs; //可穿建筑物（外围障碍物）的index,包含坡道
        //public List<Polygon> TightBoundaries = new List<Polygon>();//紧密轮廓，外扩合并+内缩2750得到。用于计算最大最小值
        //public List<Polygon> BoundingBoxes = new List<Polygon>();// 障碍物的外包框（矩形）

        public MNTSSpatialIndex InnerBoundSPIndex;//不可穿障碍物外扩半车道宽的边界
        public MNTSSpatialIndex OuterBoundSPIndex;//可穿障碍物外扩半车道宽的边界

        // SpatialIndex
        //public MNTSSpatialIndex ObstacleSpatialIndex; // 初始障碍物,不包含坡道 的spatialindex
        public MNTSSpatialIndex BuildingSpatialIndex; // 初始障碍物,包含坡道 的spatialindex
        public MNTSSpatialIndex RampSpatialIndex; //坡道的spatial index
        public MNTSSpatialIndex BoundarySpatialIndex;//边界打成断线 + 障碍物 + 坡道的spatialindex(所有边界）

        //public MNTSSpatialIndex InnerObs_OutterBoundSPIDX;//中间障碍物（不可穿）+可穿障碍物的tightbound
        //public MNTSSpatialIndex BoundaryObjectsSPIDX;//边界打成断线+可忽略障碍物的spatialindex；
        //public MNTSSpatialIndex BoundLineSpatialIndex;//边界的打成碎线的spindex

        //SegLine Data
        public List<(List<int>, List<int>)> SeglineIndex;//分区线（起始终止点连接关系），数量为0则连到边界，其余为其他分区线的index

        public Serilog.Core.Logger Logger;
        //参数
        private double CloseTol = 5.0;//闭合阈值
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public OLayoutData(BlockReference basement, Serilog.Core.Logger logger,out bool succeed)
        {
            Logger = logger;
            Extract(basement);
            succeed = true;
            if(CAD_WallLines.Count != 0 && CAD_BorderLines.Count != 0)
            {
                ThMPArrangementCmd.DisplayLogger.Information("同时提取到两种地库边界，请保留一种！");
                Active.Editor.WriteMessage("同时提取到两种地库边界，请保留一种！");
                succeed = false;
                return;
            }
            if(CAD_WallLines.Count != 0)
            {
                WallLine = CAD_WallLines.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().OrderBy(plgn => plgn.Area).Last();
            }
            else if(CAD_BorderLines.Count != 0)
            {
                BorderLines = CAD_BorderLines.Select(l => l.ToNTSLineSegment().OExtend(1)).ToList();
                var areas = BorderLines.GetPolygons().OrderBy(plgn => plgn.Area);
                if(areas.Count() == 0)
                {
                    ThMPArrangementCmd.DisplayLogger.Information("可动边界不构成闭合区域！");
                    Active.Editor.WriteMessage("可动边界不构成闭合区域！");
                    succeed = false;
                    return;
                }
                WallLine = areas.Last();
            }
            else
            {
                ThMPArrangementCmd.DisplayLogger.Information("地库边界不存在或者不闭合！");
                Active.Editor.WriteMessage("地库边界不存在或者不闭合！");
                succeed = false;
                return;
            }
            WallLine = WallLine.RemoveHoles();//初始墙线
            MaxArea = WallLine.Buffer(ParameterStock.BorderlineMoveRange,MitreParam).Area *0.001 * 0.001;
            ParameterStock.AreaMax = MaxArea;
            UpdateObstacles();//更新障碍物
            UpdateRampPolgons();//更新坡道polygon
            Buildings = Obstacles.Concat(RampPolgons).ToList();
            //Basement = OverlayNGRobust.Overlay(WallLine, new MultiPolygon(Buildings.ToArray()), SpatialFunction.Difference).
            //    Get<Polygon>(false).OrderBy(plgn => plgn.Area).Last();
            //Basement = WallLine.Difference(new MultiPolygon(Buildings.ToArray())).Get<Polygon>(false).OrderBy(plgn => plgn.Area).Last();
            UpdateSPIndex();//更新空间索引
            UpdateBasementInfo();
            //GetSegLineBoundary();
            UpdateBoundaries();
        }
        #region CAD数据提取+转换为需要的NTS数据结构
        private void Extract(BlockReference basement)
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
                else if (ent is Line line)
                {
                    CAD_BorderLines.Add(line);
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
                    if (pline.IsVaild(CloseTol))
                    {
                        CAD_Ramps.Add(pline.GetClosed());
                    }
                }
            }
            if (layerName.Contains("分割线") || layerName.Contains("分区线"))
            {
                if (ent is Line line)
                {
                    if (line.Length > 1000)//过滤过短的分区线
                    {
                        CAD_SegLines.Add(line);
                        //if (layerName.Contains("固定")) FixedSegLineIdx.Add(CAD_SegLines.Count - 1);
                    }
                }
            }
        }
        private void UpdateObstacles()
        {
            Obstacles = new List<Polygon>();
            if (CAD_Obstacles.Count > 0)
            {
                //输入打成线+求面域+union
                var UnionedObstacles = new MultiPolygon(CAD_Obstacles.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
                //求和地库交集
                Obstacles = UnionedObstacles.Get<Polygon>(true);
            }
        }
        //更新坡道polygon
        private void UpdateRampPolgons()
        {
            RampPolgons = new List<Polygon>();
            if (CAD_Ramps.Count > 0)
            {
                var UnionedRamps = new MultiPolygon(CAD_Ramps.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
                RampPolgons = UnionedRamps.Get<Polygon>(true);
            }
        }
        //更新空间索引
        private void UpdateSPIndex()
        {
            //ObstacleSpatialIndex = new MNTSSpatialIndex(Obstacles);
            BuildingSpatialIndex = new MNTSSpatialIndex(Buildings);
            RampSpatialIndex = new MNTSSpatialIndex(RampPolgons);
            var allObjs = WallLine.Shell.ToLineStrings().Cast<Geometry>().ToList();
            allObjs.AddRange(Buildings);
            BoundarySpatialIndex = new MNTSSpatialIndex(allObjs);
        }
        #endregion
        #region 数据处理
        private void UpdateBasementInfo()
        {
            var distance = ParameterStock.BuildingTolerance;
            var buffered = new MultiPolygon(Buildings.ToArray()).Buffer(distance, MitreParam);
            Geometry result = new MultiPolygon(buffered.Union().Get<Polygon>(true).ToArray());
            result = result.Buffer(-distance, MitreParam);
            result = result.Intersection(WallLine);
            var mmtoM = 0.001 * 0.001;
            ParameterStock.TotalArea = WallLine.Area * mmtoM;
            ParameterStock.BuildingArea = result.Area * mmtoM;
            Logger?.Information($"地库总面积:" + string.Format("{0:N1}", ParameterStock.TotalArea) + "m" + Convert.ToChar(0x00b2));
            Logger?.Information($"建筑物投影总面积:" + string.Format("{0:N1}", ParameterStock.BuildingArea) + "m" + Convert.ToChar(0x00b2));
        }
        //private void GetSegLineBoundary()//以半车道宽为基准外扩
        //{
        //    var bufferDistance = (ParameterStock.RoadWidth / 2) - SegLineEx.SegTol;
        //    var buffered = new MultiPolygon(Buildings.ToArray()).Buffer(bufferDistance);//建筑物外扩
        //    var BuildingBounds = buffered.Union().Get<Polygon>(true);//合并+去除孔
        //    var BuildingBoundsGeo = new MultiPolygon(BuildingBounds.ToArray());
        //    TightBoundaries = BuildingBoundsGeo.Buffer(-bufferDistance).Get<Polygon>(true);//外扩后内缩
        //    BoundingBoxes = BuildingBounds.Select(bound => BuildingSpatialIndex.SelectCrossingGeometry(bound).GetEnvelope()).
        //        Where(envelope => envelope != null).ToList();// 用障碍物轮廓获取外包框
        //    BaseLineBoundary = WallLine.Buffer(-bufferDistance).Difference(BuildingBoundsGeo).
        //        Get<Polygon>(false).OrderBy(p => p.Area).Last();//边界内缩,减掉外扩后的建筑，保留孔
        //    SegLineBoundary = new Polygon(BaseLineBoundary.Shell);
        //    var ignorableBuildings = ObstacleSpatialIndex.SelectNOTCrossingGeometry(SegLineBoundary).Cast<Polygon>().ToList();
        //    InnerBuildings = BuildingSpatialIndex.SelectCrossingGeometry(SegLineBoundary).Cast<Polygon>().ToList();//内部建筑
        //    OuterBuildingIdxs = Buildings.Select((v, i) => new { v, i }).Where(x => !SegLineBoundary.Intersects(x.v)).Select(x => x.i).ToList();//外部建筑的index

        //    var outerTightBounds = TightBoundaries.Where(b => !SegLineBoundary.Contains(b));//外部障碍物的tight边界
        //    InnerObs_OutterBoundSPIDX = new MNTSSpatialIndex(InnerBuildings.Concat(outerTightBounds));
        //    var BoundaryObjects = new List<Geometry>();
        //    BoundaryObjects.AddRange(ignorableBuildings);
        //    BoundaryObjects.AddRange(WallLine.Shell.ToLineStrings());
        //    BoundaryObjectsSPIDX = new MNTSSpatialIndex(BoundaryObjects);
        //}

        private void UpdateBoundaries()
        {
            var bufferDistance = (ParameterStock.RoadWidth / 2) - SegLineEx.SegTol;
            var BuildingBounds = new MultiPolygon(Buildings.ToArray()).Buffer(bufferDistance).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物
            var bufferedWallLine = WallLine.Buffer(-bufferDistance).Get<Polygon>(true).OrderBy(p => p.Area).Last();//边界内缩
            BaseLineBoundary = bufferedWallLine.Difference( new MultiPolygon(BuildingBounds.ToArray())).
                Get<Polygon>(false).OrderBy(p => p.Area).Last();//内缩后的边界 - 外扩后的建筑
            var outerBounds = new List<Polygon>();
            var innerBounds = new List<Polygon>();
            //建筑外轮廓分类
            foreach(var b in BuildingBounds)
            {
                if (bufferedWallLine.Shell.Intersects(b)) outerBounds.Add(b);
                else innerBounds.Add(b);
            }
            OuterBoundSPIndex = new MNTSSpatialIndex(outerBounds);
            InnerBoundSPIndex = new MNTSSpatialIndex(innerBounds);
            //outerBounds.ForEach(b => b.ToDbMPolygon(3, "外边界").AddToCurrentSpace());
            //innerBounds.ForEach(b => b.ToDbMPolygon(4, "内边界").AddToCurrentSpace());
        }

        #endregion
        #region 分割线输入预处理,以及检查
        public bool ProcessSegLines()
        {
            bool isVaild = true;
            // 标记圆半径5000
            //源数据
            var init_segs = CAD_SegLines.Select(l =>l.ToNTSLineSegment()).ToList();
            //1.判断是否有平行且距离小于1的线，若有则合并(需要连续合并）
            var idToMerge = init_segs.GroupSegLines(1);
            var merged = init_segs.MergeSegs(idToMerge);
            //2.获取基线 + 延长1（确保分割线在边界内 且保持连接关系）
            SegLines = merged.Select(l => l.GetBaseLine(WallLine).OExtend(1)).Where(l =>l!=null).
                Select(l => new SegLine(l, false, -1)).ToList();
            //3,处理坡道
            UpdateRamps();
            //4.获取最大全连接车道，若有未连接的，移除+标记+报错
            var groups = SegLines.GroupSegLines(2).OrderBy(g => g.Count).ToList();
            for (int i = 0; i < groups.Count - 1; i++)
            {
                if (isVaild)
                {
                    isVaild = false;
                    //提示该分割线在满足车道宽内不与其他分割线连接
                    //警告存在无效连接，将抛弃部分分割线
                    Logger?.Information("警告存在无效连接，将抛弃部分分割线 ！\n");
                    Active.Editor.WriteMessage("警告存在无效连接，将抛弃部分分割线！\n");
                }
                SegLines.Slice(groups[i]).ForEach(l => l.Splitter?.MidPoint.MarkPoint());
            }
            SegLines = SegLines.Slice(groups.Last());
            //5.判断起始、终结线是否明确 + 更新连接关系
            isVaild = FilteringSegLines(SegLines);
            //6.获取有效车道
            SegLines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex, BaseLineBoundary);
            //7.求迭代范围
            SegLines.ForEach(l => l.UpdateLowerUpperBound(WallLine, BuildingSpatialIndex, OuterBoundSPIndex));
            //ShowLowerUpperBound();

            //SegLines = SegLines.Select(l => l.GetMovedLine()).ToList();
            //SegLines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex, BaseLineBoundary);
            //showVaildLanes();
            return true;
        }
        //public bool _ProcessSegLines()
        //{
        //    bool isVaild = true;
        //    // 标记圆半径5000
        //    //源数据
        //    var init_segs = CAD_SegLines.Select(l => l.ToNTSLineSegment()).ToList();
        //    //1.判断是否有平行且距离小于1的线，若有则合并(需要连续合并）
        //    var idToMerge = init_segs.GroupSegLines(1);
        //    var merged = init_segs.MergeSegs(idToMerge);
        //    //2.获取基线 + 延长1（确保分割线在边界内 且保持连接关系）
        //    SegLines = merged.Select(l => l.GetBaseLine(WallLine).OExtend(1)).Where(l => l != null).
        //        Select(l => new SegLine(l, false, -1)).ToList();
        //    //3,处理坡道
        //    UpdateRamps();
        //    //4.判断起始、终结线是否明确 + 更新连接关系
        //    isVaild = FilteringSegLines(SegLines);
        //    //5.获取最大全连接车道，若有未连接的，移除+标记+报错
        //    //5.1获取有效车道
        //    //SegLines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex, BaseLineBoundary);
        //    var groups = SegLines.GroupSegLines(2).OrderBy(g => g.Count).ToList();
        //    for (int i = 0; i < groups.Count - 1; i++)
        //    {
        //        if (isVaild)
        //        {
        //            isVaild = false;
        //            //提示该分割线在满足车道宽内不与其他分割线连接
        //            //警告存在无效连接，将抛弃部分分割线
        //            Logger?.Information("警告存在无效连接，将抛弃部分分割线 ！\n");
        //            Active.Editor.WriteMessage("警告存在无效连接，将抛弃部分分割线！\n");
        //        }
        //        SegLines.Slice(groups[i]).ForEach(l => l.Splitter?.MidPoint.MarkPoint());
        //    }
        //    SegLines = SegLines.Slice(groups.Last());

        //    SegLines.ForEach(l => l.UpdateLowerUpperBound(WallLine, BuildingSpatialIndex, OuterBoundSPIndex));
        //    ShowLowerUpperBound();

        //    //SegLines = SegLines.Select(l => l.GetMovedLine()).ToList();
        //    SegLines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex, BaseLineBoundary);
        //    showVaildLanes();
        //    return true;
        //}
        //过滤起始、终结点不明确的线,获取线的连接关系
        public bool FilteringSegLines(List<SegLine> segLines)
        {
            bool Isvaild = true;
            for(int j = 0; j < 50;j++)
            {
                var stop = true;
                SeglineIndex = segLines.GetSegLineIndex(WallLine);
                for (int i = segLines.Count - 1; i >= 0; i--)
                {
                    if(SeglineIndex[i].Item1 == null || SeglineIndex[i].Item2 == null)
                    {
                        if (Isvaild)
                        {
                            //提示该分割线在满足车道宽内不与其他分割线连接
                            //警告存在无效连接，将抛弃部分分割线
                            segLines[i].Splitter.MidPoint.MarkPoint();
                            Logger?.Information("警告存在无效连接，将抛弃部分分割线 ！\n");
                            Active.Editor.WriteMessage("警告存在无效连接，将抛弃部分分割线！\n");
                        }
                        segLines.RemoveAt(i);
                        Isvaild = false;
                        stop = false;
                        continue;
                    }
                    //如果当前线不与地库边界相连，且首尾存在连到相同线的情况
                    if(SeglineIndex[i].Item1.Count!=0 && SeglineIndex[i].Item2.Count != 0 
                        && SeglineIndex[i].Item1.Any(id => SeglineIndex[i].Item2.Contains(id)))
                    {
                        if (Isvaild)
                        {
                            //提示该分割线在满足车道宽内不与其他分割线连接
                            //警告存在无效连接，将抛弃部分分割线
                            segLines[i].Splitter.MidPoint.MarkPoint();
                            Logger?.Information("警告存在无效连接，将抛弃部分分割线 ！\n");
                            Active.Editor.WriteMessage("警告存在无效连接，将抛弃部分分割线！\n");
                        }
                        segLines.RemoveAt(i);
                        Isvaild = false;
                        stop = false;
                        continue;
                    }
                }
                if (stop) break;
            }
            return Isvaild;
        }
        //更新坡道
        private void UpdateRamps()
        {
            var lineSegs = SegLines.Select(seg =>seg.Splitter).ToList();
            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];
                var ramp = RampSpatialIndex.SelectCrossingGeometry(segLine.Splitter.ToLineString()).Cast<Polygon>();
                if (ramp.Count() > 0)
                {
                    Ramps.Add(new ORamp(segLine, ramp.First()));
                    if(lineSegs.GetIntersections( WallLine,i).Count < 2) SegLines.RemoveAt(i);//移除仅有一个交点的线
                    else SegLines[i].IsFixed = true;
                }
            }
        }
        #endregion

        public void ShowLowerUpperBound(string layer = "最大最小值")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 3);
            }
            foreach(var segLine in SegLines)
            {
                var normalVec = segLine.Splitter.NormalVector();
                var posBuffer = segLine.Splitter.ShiftBuffer(segLine.MaxValue, normalVec).ToDbMPolygon(2, layer);
                var negBuffer = segLine.Splitter.ShiftBuffer(segLine.MinValue, normalVec).ToDbMPolygon(3, layer);
                var buffers = new List<MPolygon> { posBuffer, negBuffer }.Cast<Entity>().ToList();
                buffers.ShowBlock(layer, layer);
            }
        }
        public void SetInterParam()
        {
            OInterParameter.Init(WallLine,SegLines,Buildings,Ramps,BaseLineBoundary,SeglineIndex,BorderLines);
        }

        private void showVaildLanes()
        {
            var layer = "分区线等价车道";
            //var vaildLanes = SegLines.Where(l => l.VaildLane != null).Select(l =>l.VaildLane).ToList();
            var vaildLanes = SegLines.Where(l => l.Splitter != null).Select(l => l.Splitter).ToList();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 2);
                var outSegLines = vaildLanes.Select(l => l.ToDbLine(2, layer)).Cast<Entity>().ToList();
                outSegLines.ShowBlock(layer, layer);
                //finalSegLines.Select(l => l.ToDbLine(2, layer)).Cast<Entity>().ToList().ShowBlock(layer, layer);
                //MPEX.HideLayer(layer);
            }
        }
    }
}
