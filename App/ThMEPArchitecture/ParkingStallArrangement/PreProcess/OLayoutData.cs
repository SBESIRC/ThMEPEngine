using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NetTopologySuite.Algorithm;
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
using ThMEPArchitecture.ParkingStallArrangement.General;
using NetTopologySuite.Mathematics;
using System.Diagnostics;

namespace ThMEPArchitecture.ParkingStallArrangement.PreProcess
{
    public class OLayoutData
    {
        public List<Polyline> CAD_WallLines = new List<Polyline>();// 提取到的cad边界线
        public List<Polyline> CAD_MaxWallLines = new List<Polyline>();//提取到的cad最大范围
        public List<Line> CAD_BorderLines = new List<Line>();//提取到的可移动边界线

        public List<Line> CAD_SegLines = new List<Line>();// 提取到的cad分区线
        public List<Polyline> CAD_Obstacles = new List<Polyline>();//提取到的cad障碍物
        public List<Polyline> CAD_Ramps = new List<Polyline>();// 提取到的cad坡道
        public List<Line> CAD_RampLines = new List<Line>();
        public List<Polyline> CAD_MovingBounds = new List<Polyline>();//提取到的cad可动建筑框线
        // NTS 数据结构
        //public Polygon Basement;//地库，面域部分为可布置区域
        public Polygon WallLine;//初始边界线(输入边界）,输入用地红线则为最大可建范围
        //public Polygon MaxWallLine;//边界最大范围
        public List<LineSegment> BorderLines;//可动边界线
        public List<SegLine> SegLines = new List<SegLine>();// 初始分区线
        public List<Polygon> Obstacles; // 初始障碍物,不包含坡道
        public List<Polygon> RampPolygons;//坡道polygon
        public List<Polygon> MovingBounds;//可动建筑框线

        double MaxArea;//最大地库面积
        public Coordinate Center;//点集移动中心
        public double[] MaxMoveDistances;//扇形移动最大距离
        // NTS 衍生数据
        public List<ORamp> Ramps = new List<ORamp>();// 坡道
        public List<Polygon> Buildings; // 初始障碍物,包含坡道
        //public Polygon SegLineBoundary;//智能边界，外部障碍物为可穿障碍物
        //public Polygon BaseLineBoundary;//基线边界（包含内部孔），基线边界内的分割线的部分用来求基线

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
                //ThMPArrangementCmd.DisplayLogger.Information("同时提取到两种地库边界，请保留一种！");
                Active.Editor.WriteMessage("同时提取到两种地库边界，请保留一种！");
                succeed = false;
                return;
            }
            if (CAD_WallLines.Count != 0)//固定边界
            {
                WallLine = CAD_WallLines.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().OrderBy(plgn => plgn.Area).Last();
            }
            else if(CAD_BorderLines.Count != 0)//输入线+边界迭代
            {
                BorderLines = CAD_BorderLines.Select(l => l.ToNTSLineSegment().OExtend(1)).ToList();
                var areas = BorderLines.GetPolygons().OrderBy(plgn => plgn.Area);
                if(areas.Count() == 0)
                {
                    //ThMPArrangementCmd.DisplayLogger.Information("可动边界不构成闭合区域！");
                    Active.Editor.WriteMessage("可动边界不构成闭合区域！");
                    succeed = false;
                    return;
                }
                WallLine = areas.Last();
            }
            else if(CAD_MaxWallLines.Count != 0)//无输入，边界迭代
            {
                WallLine = CAD_MaxWallLines.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().OrderBy(plgn => plgn.Area).Last();
                UpdateBoundPoints(ParameterStock.BoundPointCnt);
            }
            else
            {
                //ThMPArrangementCmd.DisplayLogger.Information("地库边界不存在或者不闭合！");
                Active.Editor.WriteMessage("地库边界不存在或者不闭合！");
                succeed = false;
                return;
            }
            WallLine = WallLine.RemoveHoles();//初始墙线
            MaxArea = WallLine.Buffer(ParameterStock.BorderlineMoveRange,MitreParam).Area *0.001 * 0.001;
            ParameterStock.AreaMax = MaxArea;
            UpdateObstacles();//更新障碍物
            UpdateRampPolgons();//更新坡道polygon
            UpdateRamps();
            UpdateMovingBounds();//更新可动建筑框线
            Buildings = Obstacles.Concat(RampPolygons).ToList();
            //Basement = OverlayNGRobust.Overlay(WallLine, new MultiPolygon(Buildings.ToArray()), SpatialFunction.Difference).
            //    Get<Polygon>(false).OrderBy(plgn => plgn.Area).Last();
            //Basement = WallLine.Difference(new MultiPolygon(Buildings.ToArray())).Get<Polygon>(false).OrderBy(plgn => plgn.Area).Last();
            UpdateSPIndex();//更新空间索引
            UpdateBoundaries();
            UpdateBasementInfo();
            WallLine.ToDbMPolygon().AddToCurrentSpace();
            //GetSegLineBoundary();

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
            if (layerName.Contains("最大可建范围"))
            {
                if(ent is Polyline pline)
                {
                    if (pline.IsVaild(CloseTol))
                    {
                        CAD_MaxWallLines.Add(pline.GetClosed());
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

            if (layerName.Contains("可动建筑框线"))
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
                                CAD_MovingBounds.Add(pline.GetClosed());
                            }
                        }
                    }
                }
                else if (ent is Polyline pline)
                {
                    if (pline.IsVaild(CloseTol))
                    {
                        CAD_MovingBounds.Add(pline.GetClosed());
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
            if (layerName.Contains("坡道出口标记"))
            {
                if (ent is Line line)
                {
                     CAD_RampLines.Add(line);
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
        private void UpdateMovingBounds()
        {
            MovingBounds = new List<Polygon>();
            if(CAD_MovingBounds.Count > 0)
            {
                //输入打成线+求面域+union
                var UnionedBounds = new MultiPolygon(CAD_MovingBounds.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
                MovingBounds = UnionedBounds.Get<Polygon>(true);
            }
        }
        //更新坡道polygon
        private void UpdateRampPolgons()
        {
            RampPolygons = new List<Polygon>();
            if (CAD_Ramps.Count > 0)
            {
                var UnionedRamps = new MultiPolygon(CAD_Ramps.Select(pl => pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
                RampPolygons = UnionedRamps.Get<Polygon>(true);
            }
        }
        private void UpdateRamps()
        {
            var minLength = 4000;
            var maxLength = 9000;
            var rampLines = CAD_RampLines.Select(l => l.ToNTSLineSegment()).ToList();
            foreach (var rampLine in rampLines)
            {
                var rampPolys = RampSpatialIndex.SelectCrossingGeometry(rampLine.ToLineString()).Cast<Polygon>();
                
                foreach(var rampPoly in rampPolys)
                {
                    var shell = rampPoly.Shell;
                    bool IsCCW =shell.IsCCW;
                    var exitLines = shell.ToLineSegments();
                    foreach(var exitLine in exitLines)
                    {
                        if (exitLine.Length < minLength || exitLine.Length > maxLength) continue;
                        var intSection = rampLine.Intersection(exitLine);
                        if (intSection == null) continue;
                        Vector2D direction;
                        if (IsCCW) direction = exitLine.DirVector().RotateByQuarterCircle(-1);
                        else direction = exitLine.DirVector().RotateByQuarterCircle(1);
                        Ramps.Add(new ORamp(intSection, direction));
                    }
                }
            }
        }
        //更新空间索引
        private void UpdateSPIndex()
        {
            //ObstacleSpatialIndex = new MNTSSpatialIndex(Obstacles);
            BuildingSpatialIndex = new MNTSSpatialIndex(Buildings);
            RampSpatialIndex = new MNTSSpatialIndex(RampPolygons);
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
            var buildingtol = ParameterStock.BuildingTolerance;

            var bufferDistance = (ParameterStock.RoadWidth / 2) - SegLineEx.SegTol;
            //var BuildingBounds = new MultiPolygon(Buildings.ToArray()).Buffer(bufferDistance).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物c

            var BuildingBounds = new MultiPolygon(Buildings.ToArray()).Buffer(ParameterStock.BuildingTolerance, MitreParam).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物


            var unbuffered = new MultiPolygon(BuildingBounds.ToArray()).Buffer(-buildingtol, MitreParam).Get<Polygon>(true);
            WallLine = WallLine.Union(new MultiPolygon(unbuffered.ToArray())).Get<Polygon>(true).OrderBy(p => p.Area).Last();
            //#################用建筑物外包框和边界求新地库边界#############
            //var newWallLines =  WallLine.Union(new MultiPolygon( BuildingBounds.ToArray()).Buffer(-ParameterStock.BuildingTolerance,MitreParam)).Get<Polygon>(true);
            //var newWallLine = newWallLines.OrderBy(x => x.Area).Last();
            //newWallLine.ToDbMPolygon().AddToCurrentSpace();

            //#################建筑物外接矩形求边界####################
            //Geometry obbs =new MultiPolygon(BuildingBounds.Select(b=>b.GetObb()).ToArray());
            //var convexHull =(Polygon) obbs.ConvexHull();
            //var borders = obbs.Get<Polygon>(true);
            //while(borders.Count!=1)
            //{
            //    obbs = obbs.Buffer(bufferDistance, MitreParam);
            //    borders = obbs.Get<Polygon>(true);
            //}
            //var maxMindist = obbs.Max(b_this =>obbs.Min(b_other => { if (b_other.Disjoint(b_this)) 
            //        return b_other.Distance(b_this); else return double.MaxValue; } ))+10;
            //var init_border = new MultiPolygon(obbs.ToArray()).Buffer(maxMindist,MitreParam).Union().Get<Polygon>(true).OrderBy(p =>p.Area).Last();
            //var init_border = borders.First().Union(convexHull).Get<Polygon>(true).OrderBy(p =>p.Area).Last();
            //var init_border = GetInitBound(BuildingBounds);
            //init_border.ToDbMPolygon().AddToCurrentSpace();
            //convexHull.ToDbMPolygon().AddToCurrentSpace();
            //BuildingBounds.ForEach(p => p.GetObb().ToDbMPolygon().AddToCurrentSpace());
            //BuildingBounds.ForEach(p => p.ToDbMPolygon().AddToCurrentSpace());
            var bufferedWallLine = WallLine.Buffer(-bufferDistance).Get<Polygon>(true).OrderBy(p => p.Area).Last();//边界内缩
            //BaseLineBoundary = bufferedWallLine.Difference( new MultiPolygon(BuildingBounds.ToArray())).
            //    Get<Polygon>(false).OrderBy(p => p.Area).Last();//内缩后的边界 - 外扩后的建筑

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
        private void UpdateBoundPoints(int Counts = 8,int chunks = 100)//获取边界点集
        {
            ////求最小外接圆
            //var boundingCircle = new MinimumBoundingCircle(MaxWallLine);
            //var center = boundingCircle.GetCentre();
            //var radius = boundingCircle.GetRadius();
            //var center = MaxWallLine.Centroid.Coordinate;
            //var radius = MaxWallLine.Coordinates.Max(c =>c.Distance(center));
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            double radius;
            //(center, radius) = GetCenter2(chunks,Counts);
            (Center, radius) = GetCenter(chunks);

            stopWatch.Stop();
            Active.Editor.WriteLine("找中心时间"+stopWatch.Elapsed.TotalSeconds);
            Center.MarkPoint(radius);
            var angleStepSize = AngleUtility.PiTimes2 / Counts;
            MaxMoveDistances = new double[Counts];
            for (int i = 0; i < Counts; i++)
            {
                var StartAngle = angleStepSize * i;
                var MidAngle = angleStepSize * (i+0.5);
                var EndAngle = angleStepSize*(i+1);
                var StartVector = new Vector2D(Math.Cos(StartAngle), Math.Sin(StartAngle));
                var MidVector = new Vector2D(Math.Cos(MidAngle), Math.Sin(MidAngle));
                var EndVector = new Vector2D(Math.Cos(EndAngle), Math.Sin(EndAngle));
                var StartPt = StartVector.Multiply(radius).Translate(Center);//旋转的第一个点
                var MidPt = MidVector.Multiply(radius/Math.Cos(MidAngle-StartAngle)).Translate(Center);//旋转的中点
                var EndPt = EndVector.Multiply(radius).Translate(Center);//旋转的终点
                var sector =new Polygon( new LinearRing(new Coordinate[] { Center, StartPt, MidPt, EndPt, Center }));
                sector.ToDbMPolygon().AddToCurrentSpace();
                var intSection = sector.Intersection(WallLine.Shell);
                MaxMoveDistances[i] = intSection.Coordinates.Max(c => c.Distance(Center));
                intSection.Coordinates.OrderBy(c => c.Distance(Center)).Last().MarkPoint();

            }
        }

        private (Coordinate,double) GetCenter(int chunks)
        {
            //var boundingCircle = new MinimumBoundingCircle(WallLine);
            //var Mcenter = boundingCircle.GetCentre();
            //var Mradius = boundingCircle.GetRadius();
            var Mcenter = WallLine.Centroid.Coordinate;
            var Mradius = WallLine.Coordinates.Max(c =>c.Distance(Mcenter));
            if(WallLine.Contains(Mcenter.ToPoint())) return(Mcenter,Mradius);
            var envelop = WallLine.EnvelopeInternal;
            var X_StepSize = (envelop.MaxX - envelop.MinX)/chunks;
            var Y_StepSize = (envelop.MaxY - envelop.MinY)/chunks;
            var min_radius = double.MaxValue;
            var min_center = envelop.Centre;
            for (var i = 0; i < chunks; i++)
            {
                for(var j = 0; j < chunks; j++)
                {
                    var X_start = envelop.MinX + i*X_StepSize;
                    var X_end = envelop.MinX + (i+1)*X_StepSize;
                    var Y_start = envelop.MinY + j*Y_StepSize;
                    var Y_end = envelop.MinY + (j+1)*Y_StepSize;
                    var center = new Coordinate((X_start+X_end)/2, (Y_start+Y_end)/2);
                    if (WallLine.Contains(center.ToPoint()))
                    {
                        double radius = 0;
                        foreach(var coordinate in WallLine.Coordinates)
                        {
                            radius = Math.Max(center.Distance(coordinate), radius);
                            if (radius > min_radius) break;
                        }
                        if(radius < min_radius)
                        {
                            min_radius = radius;
                            min_center = center;
                        }
                    }
                }
            }
            return (min_center,min_radius);
        }
        //private (Coordinate, double) GetCenter2(int chunks,int Counts)
        //{
        //    var boundingCircle = new MinimumBoundingCircle(MaxWallLine);
        //    var Mcenter = boundingCircle.GetCentre();
        //    var Mradius = boundingCircle.GetRadius();
        //    if (MaxWallLine.Contains(Mcenter.ToPoint())) return (Mcenter, Mradius);
        //    var envelop = MaxWallLine.EnvelopeInternal;
        //    var X_StepSize = (envelop.MaxX - envelop.MinX) / chunks;
        //    var Y_StepSize = (envelop.MaxY - envelop.MinY) / chunks;
        //    var min_radius = double.MaxValue;
        //    var min_center = envelop.Centre;
        //    var min_Area = double.MaxValue;
        //    for (var i = 0; i < chunks; i++)
        //    {
        //        for (var j = 0; j < chunks; j++)
        //        {
        //            var X_start = envelop.MinX + i * X_StepSize;
        //            var X_end = envelop.MinX + (i + 1) * X_StepSize;
        //            var Y_start = envelop.MinY + j * Y_StepSize;
        //            var Y_end = envelop.MinY + (j + 1) * Y_StepSize;
        //            var center = new Coordinate((X_start + X_end) / 2, (Y_start + Y_end) / 2);
        //            if (MaxWallLine.Contains(center.ToPoint()))
        //            {
        //                var radius = MaxWallLine.Coordinates.Max(c => c.Distance(center));
        //                var area = GetSectorArea(center, MaxWallLine.Coordinates.Max(c => c.Distance(center)), Counts);
        //                if (area < min_Area)
        //                {
        //                    min_radius = radius;
        //                    min_center = center;
        //                    min_Area = area;
        //                }
        //            }
        //        }
        //    }
        //    return (min_center, min_radius);
        //}
        //private double GetSectorArea(Coordinate center,double radius,int Counts)
        //{
        //    var angleStepSize = AngleUtility.PiTimes2 / Counts;
        //    //var MaxMoveDistances = new List<double>();
        //    var totalArea = 0.0;
        //    for (int i = 0; i < Counts; i++)
        //    {
        //        var StartAngle = angleStepSize * i;
        //        var MidAngle = angleStepSize * (i + 0.5);
        //        var EndAngle = angleStepSize * (i + 1);
        //        var StartVector = new Vector2D(Math.Cos(StartAngle), Math.Sin(StartAngle));
        //        var MidVector = new Vector2D(Math.Cos(MidAngle), Math.Sin(MidAngle));
        //        var EndVector = new Vector2D(Math.Cos(EndAngle), Math.Sin(EndAngle));
        //        var StartPt = StartVector.Multiply(radius).Translate(center);//旋转的第一个点
        //        var MidPt = MidVector.Multiply(radius / Math.Cos(MidAngle - StartAngle)).Translate(center);//旋转的中点
        //        var EndPt = EndVector.Multiply(radius).Translate(center);//旋转的终点
        //        var sector = new Polygon(new LinearRing(new Coordinate[] { center, StartPt, MidPt, EndPt, center }));
        //        //sector.ToDbMPolygon().AddToCurrentSpace();
        //        var intSection = sector.Intersection(MaxWallLine.Shell);
        //        var moveDist = intSection.Coordinates.Max(c => c.Distance(center));
        //        totalArea += Math.PI * moveDist * moveDist / Counts;
        //        //intSection.Coordinates.OrderBy(c => c.Distance(center)).Last().MarkPoint();

        //    }
        //    return totalArea;
        //}
        private Polygon _GetInitBound(List<Polygon> BuildingBounds)
        {
            var obbs = BuildingBounds.Select(b => b.GetObb());
            var convexHull = (Polygon)(new MultiPolygon( obbs.ToArray()).ConvexHull());
            var obbIndex = new MNTSSpatialIndex(obbs);
            var outerObbs = new List<Polygon>();
            var outerSets = new HashSet<Polygon>();
            foreach (var coor in convexHull.Coordinates)
            {
                var pt = coor.ToPoint();
                var buffered = pt.Buffer(0.1);
                var selectedobbs = obbIndex.SelectCrossingGeometry(buffered).Cast<Polygon>();
                var tempObj = selectedobbs.OrderBy(b => b.Distance(pt)).First();
                if (!outerSets.Contains(tempObj))
                {
                    outerSets.Add(tempObj);
                    outerObbs.Add(tempObj);
                }
                //selectedobbs.ForEach
                //    (obb => { if (!outerSets.Contains(obb)) { outerSets.Add(obb); outerObbs.Add(obb); } });
            }
            //var bufferdistances = new List<double>();
            //for(int i = 0; i < outerObbs.Count; i++)
            //{
            //    var lastIndex = i - 1 + outerObbs.Count;
            //    var nextIndex = i + 1;
            //    var current = outerObbs[i];
            //    var next = outerObbs[nextIndex % outerObbs.Count];
            //    var last = outerObbs[lastIndex % outerObbs.Count];
            //    bufferdistances.Add(1+Math.Max(current.Distance(last), current.Distance(next))/2);
            //}
            //for(int i = 0; i < outerObbs.Count; i++)
            //{
            //    outerObbs[i] = outerObbs[i].Buffer(bufferdistances[i], MitreParam).Get<Polygon>(true).First();
            //}

            for (int i = 0; i < outerObbs.Count; i++)
            {
                var nextIndex = i + 1;
                var current = outerObbs[i];
                var next = outerObbs[nextIndex % outerObbs.Count];
                var nearestPts = next.Coordinates.Select(c => c.ToPoint()).OrderBy(p => p.Distance(current)).Take(2).Cast<Geometry>().ToList();
                nearestPts.Add(current);
                outerObbs[i] = new GeometryCollection(nearestPts.ToArray()).GetObb().Buffer(0.1, MitreParam).Get<Polygon>(true).OrderBy(p => p.Area).Last();
            }

            //for (int i = 0; i < outerObbs.Count; i++)
            //{
            //    var nextIndex = i + 1;
            //    var current = outerObbs[i];
            //    var next = outerObbs[nextIndex % outerObbs.Count];
            //    outerObbs[i] =current.Union(next).GetObb();
            //}
            var unioned = new MultiPolygon(outerObbs.ToArray()).Union().Get<Polygon>(true).OrderBy(p=>p.Area).Last();
            return unioned;
        }

        private Polygon GetInitBound(List<Polygon> BuildingBounds)
        {
            var convexHull = new MultiPolygon(BuildingBounds.Select(b =>b.GetObb()).ToArray()).ConvexHull() as Polygon;
            var coors = convexHull.Coordinates.Take(convexHull.Coordinates.Count()-1).ToList();
            for(int i = 0; i < coors.Count; i++)
            {
                var lastIndex = i - 1 + coors.Count;
                var nextIndex = i + 1;
                var current = coors[i];
                var next = coors[nextIndex % coors.Count];
                var last = coors[lastIndex % coors.Count];
                if (current.Distance(last) < 100 || current.Distance(next) < 100) continue;
                var pts = new List<Coordinate> { current,new Coordinate((current.X+next.X)/2,(current.Y+next.Y)/2),
                    new Coordinate((current.X + last.X) / 2, (current.Y + last.Y) / 2) };
                var envelop = new MultiPoint(pts.Select(coor => coor.ToPoint()).ToArray()).Envelope as Polygon;
                envelop.ToDbMPolygon().AddToCurrentSpace();
            }

            var new_coors = new List<Coordinate>();
            for (int i = 0; i < coors.Count; i++)
            {
                new_coors.Add(RandomCoor());
            }
            var lines = new List<LineSegment>();
            for(int i = 0; i < new_coors.Count; i++)
            {
                var nextIndex = i + 1;
                var current = new_coors[i];
                var next = new_coors[nextIndex % new_coors.Count];
                lines.Add(new LineSegment(current, next));  
                current.ToPoint().ToDbPoint().AddToCurrentSpace();
            }
            lines.GetPolygons().OrderBy(p => p.Area).Last().ToDbMPolygon().AddToCurrentSpace();


            return convexHull;
        }
        private Coordinate RandomCoor()
        {
            var lb = -10000;
            var ub = 10000;
            var x = General.Utils.RandDouble() *(ub-lb) + lb;
            var y = General.Utils.RandDouble() *(ub-lb) + lb;
            return new Coordinate(x, y);
        }

        #endregion
        #region 分割线输入预处理,以及检查
        public bool ProcessSegLines()
        {
            bool isVaild = true;
            // 标记圆半径5000
            //源数据
            var init_segs = CAD_SegLines.Select(l =>ToSegLine(l)).ToList();
            //1.判断是否有平行且距离小于1的线，若有则合并(需要连续合并）
            var idToMerge = init_segs.GroupSegLines(1);
            var merged = init_segs.MergeSegs(idToMerge);
            //2.获取基线 + 延长1（确保分割线在边界内 且保持连接关系）
            SegLines = merged.Select(l => l.GetBaseLine(WallLine)).Where(l =>l!=null).ToList();
            SegLines.ForEach(l => { l.Splitter = l.Splitter.OExtend(1);l.IsInitLine = true; });
            //3,处理坡道(逻辑更新，使用特殊的坡道线作为出入口标记)
            //UpdateRamps();
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
            SegLines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex);
            //7.求迭代范围
            SegLines.ForEach(l => l.UpdateLowerUpperBound(WallLine, BuildingSpatialIndex, OuterBoundSPIndex));
            
            //ShowLowerUpperBound();
            //SegLines = SegLines.Select(l => l.GetMovedLine()).ToList();
            //SegLines.UpdateSegLines(SeglineIndex, WallLine, BoundarySpatialIndex, BaseLineBoundary);
            //showVaildLanes();
            return true;
        }
        //可动建筑预处理
        public void MovingBuildingPreProcess()
        {
            if(MovingBounds.Count == 0) return;
            //var movingBuildings = new List<Polygon>();
            //MovingBounds.ForEach(b => movingBuildings.AddRange(BuildingSpatialIndex.SelectCrossingGeometry(b).Cast<Polygon>()));
            
            for (int i = 0; i < MovingBounds.Count; i++)
            {
                var bound = MovingBounds[i];
                bound = new GeometryCollection(BuildingSpatialIndex.SelectCrossingGeometry(bound).ToArray()).ConvexHull() as Polygon;
                //var dist = obb.Centroid.Distance(obb.Shell) - 1;
                //obb = (Polygon)obb.Buffer(-dist, MitreParam);
                MovingBounds[i] = bound;
                //bound.ToDbMPolygon().AddToCurrentSpace();
            }
            //var MovingBoundSPindex = new MNTSSpatialIndex(MovingBounds);

            //var halfRoadWidth = ParameterStock.RoadWidth / 2;

            //foreach (var segLine in SegLines)
            //{ 
            //    var laneRect = segLine.Splitter.OGetRect(halfRoadWidth);
            //    var selected = MovingBoundSPindex.SelectCrossingGeometry(laneRect).Cast<Polygon>();
            //    if(selected.Count() == 0) continue;
            //    var splitter = segLine.Splitter.ToLineString();
            //    var selectedGeo = new MultiPolygon(selected.ToArray());
            //    var IntSecCenter = splitter.Intersection(selectedGeo).Centroid;
            //    splitter = splitter.Difference(selectedGeo).Get<LineString>().OrderBy(l => l.Length).Last();
            //    var coors = splitter.Coordinates.ToList();
            //    if(!IntSecCenter.IsEmpty)coors.Add(IntSecCenter.Coordinate);
            //    coors = coors.PositiveOrder();
            //    segLine.Splitter = new LineSegment(coors.First(), coors.Last());
            //}
            //showVaildLanes();
            
        }

        public SegLine ToSegLine(Line line,double extendTol = 1)
        {
            var segLine = new SegLine(line.ToNTSLineSegment().OExtend(extendTol),line.Layer.Contains("固定"), -1);
            return segLine;
        }
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
        //private void _UpdateRamps()//旧逻辑，弃用
        //{
        //    var lineSegs = SegLines.Select(seg =>seg.Splitter).ToList();
        //    for (int i = SegLines.Count - 1; i >= 0; i--)
        //    {
        //        var segLine = SegLines[i];
        //        var ramp = RampSpatialIndex.SelectCrossingGeometry(segLine.Splitter.ToLineString()).Cast<Polygon>();
        //        if (ramp.Count() > 0)
        //        {
        //            Ramps.Add(new ORamp(segLine, ramp.First()));
        //            if(lineSegs.GetIntersections( WallLine,i).Count < 2) SegLines.RemoveAt(i);//移除仅有一个交点的线
        //            else SegLines[i].IsFixed = true;
        //        }
        //    }
        //}

        
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
            OInterParameter.Init(WallLine,SegLines,Buildings,Ramps,SeglineIndex,BorderLines);
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
