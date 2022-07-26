using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.MultiProcess;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ViewModel;
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
        public List<Line> CAD_SegLines = new List<Line>();// 提取到的cad分区线
        public List<Polyline> CAD_Obstacles = new List<Polyline>();//提取到的cad障碍物
        public List<Polyline> CAD_Ramps = new List<Polyline>();// 提取到的cad坡道

        // NTS 数据结构
        public Polygon Basement;//地库，面域部分为可布置区域
        public Polygon WallLine;//初始边界线
        public List<SegLine> SegLines = new List<SegLine>();// 初始分区线
        public List<Polygon> Obstacles; // 初始障碍物,不包含坡道
        public List<Polygon> RampPolgons;//坡道polygon

        // NTS 衍生数据
        public List<ORamp> Ramps = new List<ORamp>();// 坡道
        public List<Polygon> Buildings; // 初始障碍物,包含坡道
        public Polygon SegLineBoundary;//智能边界，外部障碍物为可穿障碍物
        public Polygon BaseLineBoundary;//基线边界（包含内部孔），基线边界内的分割线的部分用来求基线

        public List<LineSegment> VaildLanes;//分区线等价车道线
        public List<Polygon> InnerBuildings; //不可穿障碍物（中间障碍物）,包含坡道
        public List<int> OuterBuildingIdxs; //可穿建筑物（外围障碍物）的index,包含坡道
        public List<Polygon> TightBoundaries = new List<Polygon>();//紧密轮廓，外扩合并+内缩2750得到。用于计算最大最小值
        public List<Polygon> BoundingBoxes = new List<Polygon>();// 障碍物的外包框（矩形）
        
        // SpatialIndex
        public MNTSSpatialIndex ObstacleSpatialIndex; // 初始障碍物,不包含坡道 的spatialindex
        public MNTSSpatialIndex BuildingSpatialIndex; // 初始障碍物,包含坡道 的spatialindex
        public MNTSSpatialIndex RampSpatialIndex; //坡道的spatial index
        public MNTSSpatialIndex BoundarySpatialIndex;//边界打成断线 + 障碍物 + 坡道的spatialindex(所有边界）

        public MNTSSpatialIndex InnerObs_OutterBoundSPIDX;//中间障碍物（不可穿）+可穿障碍物的tightbound
        public MNTSSpatialIndex BoundaryObjectsSPIDX;//边界打成断线+可忽略障碍物的spatialindex；
        public MNTSSpatialIndex BoundLineSpatialIndex;//边界的打成碎线的spindex

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
            if (CAD_WallLines.Count == 0)
            {
                ThMPArrangementCmd.DisplayLogger.Information("地库边界不存在或者不闭合！");
                Active.Editor.WriteMessage("地库边界不存在或者不闭合！");
                succeed = false;
            }
            WallLine = CAD_WallLines.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().OrderBy(plgn => plgn.Area).Last();
            WallLine = WallLine.RemoveHoles();//初始墙线
            UpdateObstacles();//更新障碍物
            UpdateRampPolgons();//更新坡道polygon
            Buildings = Obstacles.Concat(RampPolgons).ToList();
            Basement = WallLine.Difference(new MultiPolygon(Buildings.ToArray())).Get<Polygon>(false).OrderBy(plgn => plgn.Area).Last();
            UpdateSPIndex();//更新空间索引
            UpdateBasementInfo();
            GetSegLineBoundary();
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
                Obstacles = UnionedObstacles.Intersection(WallLine).Get<Polygon>(true);
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
            ObstacleSpatialIndex = new MNTSSpatialIndex(Obstacles);
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
        private void GetSegLineBoundary()//以半车道宽为基准外扩
        {
            var bufferDistance = (ParameterStock.RoadWidth / 2) - SegLineEx.SegTol;
            var buffered = new MultiPolygon(Buildings.ToArray()).Buffer(bufferDistance);//建筑物外扩
            var BuildingBounds = buffered.Union().Get<Polygon>(true);//合并+去除孔
            var BuildingBoundsGeo = new MultiPolygon(BuildingBounds.ToArray());
            TightBoundaries = BuildingBoundsGeo.Buffer(-bufferDistance).Get<Polygon>(true);//外扩后内缩
            BoundingBoxes = BuildingBounds.Select(bound => BuildingSpatialIndex.SelectCrossingGeometry(bound).GetEnvelope()).
                Where(envelope => envelope != null).ToList();// 用障碍物轮廓获取外包框
            BaseLineBoundary = WallLine.Buffer(-bufferDistance).Difference(BuildingBoundsGeo).
                Get<Polygon>(false).OrderBy(p => p.Area).Last();//边界内缩,减掉外扩后的建筑，保留孔
            SegLineBoundary = new Polygon(BaseLineBoundary.Shell);
            var ignorableBuildings = ObstacleSpatialIndex.SelectNOTCrossingGeometry(SegLineBoundary).Cast<Polygon>().ToList();
            InnerBuildings = BuildingSpatialIndex.SelectCrossingGeometry(SegLineBoundary).Cast<Polygon>().ToList();//内部建筑
            OuterBuildingIdxs = Buildings.Select((v, i) => new { v, i }).Where(x => !SegLineBoundary.Intersects(x.v)).Select(x => x.i).ToList();//外部建筑的index

            var outerTightBounds = TightBoundaries.Where(b => !SegLineBoundary.Contains(b));//外部障碍物的tight边界
            InnerObs_OutterBoundSPIDX = new MNTSSpatialIndex(InnerBuildings.Concat(outerTightBounds));
            var BoundaryObjects = new List<Geometry>();
            BoundaryObjects.AddRange(ignorableBuildings);
            BoundaryObjects.AddRange(WallLine.Shell.ToLineStrings());
            BoundaryObjectsSPIDX = new MNTSSpatialIndex(BoundaryObjects);
        }
        #endregion
        public void _ProcessSegLines()
        {
            var segLines = new List<SegLine>();
            foreach(var line in CAD_SegLines)
            {
                var segLine = new SegLine(line.ToNTSLineSegment().OExtend(1), false, -1, 0, 0);
                segLines.Add(segLine);  
            }
            //SeglineIndex = segLines.GetSegLineIndex(WallLine);
            segLines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex,BaseLineBoundary);
            BaseLineBoundary.ToDbMPolygon().AddToCurrentSpace();
            segLines.Select(seg => seg.Splitter?.ToDbLine()).ForEach(seg => { seg.ColorIndex = 0; seg.AddToCurrentSpace(); });
            segLines.Select(seg => seg.VaildLane?.ToDbLine()).ForEach(seg => { seg.ColorIndex = 1; seg.AddToCurrentSpace(); });
        }
        public bool ProcessSegLines()
        {
            var init_segs = CAD_SegLines.Select(l =>l.ToNTSLineSegment());
            //1.获取基线 + 延长1（确保分割线在边界内 且保持连接关系）
            var baselines = init_segs.Select(l => l.GetBaseLine(WallLine).OExtend(1)).Where(l =>l!=null).
                Select(l => new SegLine(l, false, -1, 0, 0)).ToList();
            //2.判断起始、终结线是否明确 + 更新连接关系
            var isVaild = FilteringSegLines(baselines);
            //2.获取最大全连接组，若有未连接的，移除+标记+报错
            baselines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex, BaseLineBoundary);

            //2.1获取有效车道

            //3.处理坡道，跟坡道连接 且与其他分割线仅有一个交点的线，直接移除

            //4.判断剩余分割线是否有仅有一个交点的线，若有移除+标记+报错

            // 标记圆半径5000
            // 判断每根分区线至少有两个交点(端点标记）
            //if (!HaveAtLeastTwoIntsecPoints(true)) return false;
            //// 先预切割
            //SegLines.SeglinePrecut(WallLine);
            //SeglineIndexList = SegLines.GetSegLineIntsecList();
            //SeglineConnectToBound = SegLines.GetSeglineConnectToBound(WallLine);
            ////获取有效分区线
            //VaildLanes = SegLines.GetVaildLanes(WallLine, BoundaryObjectsSPIDX);
            //// 判断分区线净宽（中点标记）
            //if (!LaneWidthSatisfied()) return false;
            //// 后预切割
            //SegLines.SeglinePrecut(WallLine);
            //// 判断每根分区线至少有两个交点(端点标记）
            //if (!HaveAtLeastTwoIntsecPoints(false)) return false;
            //// 判断车道是否全部相连（两个以上标记剩余中点，以下标记自己）
            //if (!Allconnected()) return false;
            return true;
        }
        //过滤起始、终结点不明确的线,获取线的连接关系
        public bool FilteringSegLines(List<SegLine> segLines)
        {
            bool Isvaild = true;
            for(int j = 0; j < 50;j++)
            {
                var stop = true;
                SeglineIndex = segLines.GetSegLineIndex(WallLine.Shell, BaseLineBoundary);
                for (int i = segLines.Count - 1; i >= 0; i--)
                {
                    if(SeglineIndex[i].Item1 == null || SeglineIndex[i].Item2 == null)
                    {
                        //提示该分割线在有效范围内不与其他分割线连接
                        segLines.RemoveAt(i);
                        Isvaild = false;
                        stop = false;
                        continue;
                    }
                    //如果当前线不与地库边界相连，且首尾存在连到相同线的情况
                    if(SeglineIndex[i].Item1.Count!=0 && SeglineIndex[i].Item2.Count != 0 
                        && SeglineIndex[i].Item1.Any(id => SeglineIndex[i].Item2.Contains(id)))
                    {
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

        public bool FilteringLanes(List<SegLine> segLines)
        {
            return true;
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
                }
            }
        }
    }
}
