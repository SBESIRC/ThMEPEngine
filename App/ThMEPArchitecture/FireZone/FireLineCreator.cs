using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using ThParkingStall.Core.LineCleaner;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.Tools;
using ThParkingStall.Core.OTools;
using NetTopologySuite.Operation.Buffer;
using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
using Dreambuild.AutoCAD;
using ThMEPArchitecture.MultiProcess;
using Linq2Acad;
using ThMEPEngineCore;

namespace ThMEPArchitecture.FireZone
{
    public class FireLineCreator
    {
        #region CAD 输入
        public List<Polyline> CAD_WallLines = new List<Polyline>();
        public List<Polyline> CAD_CarSpots = new List<Polyline>();
        public List<Polyline> CAD_Obstacles = new List<Polyline>();
        public List<Line> CAD_Lanes = new List<Line>();
        #endregion

        #region 转译后数据
        public Polygon Basement;
        public List<InfoCar> Cars = new List<InfoCar>();
        public List<LineSegment> Lanes = new List<LineSegment>();
        #endregion

        #region 防火线生成
        private FireLineGenerator Generator; 
        #endregion
        #region 辅助变量
        private Stopwatch _stopwatch = new Stopwatch();
        private double t_pre = 0;
        public Serilog.Core.Logger Logger = null;
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        #endregion
        public FireLineCreator(BlockReference block, Serilog.Core.Logger logger)
        {
            _stopwatch.Start();
            t_pre = _stopwatch.Elapsed.TotalSeconds;
            Logger = logger;
            Extract(block);
            UpdateBasement();
            UpdateLanes();
            Logger?.Information($"数据提取用时{_stopwatch.Elapsed.TotalSeconds - t_pre}s");
        }
        #region 输入处理
        private void UpdateBasement()//提取地库
        {
            var obstacles = new List<Polygon>();
            if (CAD_Obstacles.Count > 0)
            {
                //输入打成线+求面域+union
                var UnionedObstacles = new MultiPolygon(CAD_Obstacles.Select(pl => 
                    pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Buffer(0.1, MitreParam).Union();
                //求和地库交集
                obstacles = UnionedObstacles.Get<Polygon>(true);
            }
            Basement = CAD_WallLines.OrderBy(wl => wl.Area).Last().ToNTSPolygon();
            var holes = new MultiPolygon(obstacles.ToArray());
            Basement = Basement.Difference(holes).Get<Polygon>(false).OrderBy(p => p.Area).Last();

        }
        private void UpdateLanes()//车道转换
        {
            //暂无处理，直接转译
            Lanes = CAD_Lanes.Select(l=>l.ToNTSLineSegment()).ToList();
        }
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
                    if (pline.Closed)
                    {
                        CAD_WallLines.Add(pline);
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
                            if (pline.IsVaild(5))
                            {
                                CAD_Obstacles.Add(pline.GetClosed());
                            }
                        }
                    }
                }
                else if (ent is Polyline pline)
                {
                    if (pline.IsVaild(5))
                    {
                        CAD_Obstacles.Add(pline.GetClosed());
                    }
                }
            }
            if (layerName.Contains("停车位"))
            {
                if (ent is BlockReference br)
                {
                    var insertPt = br.Position.ToNTSCoordinate();
                    var dbObjs = new DBObjectCollection();
                    br.Explode(dbObjs);
                    var Plines = new List<Polyline>();
                    foreach (var obj in dbObjs)
                    {
                        if (obj is Polyline pline)
                        {
                            //if(pline.NumberOfVertices != 4)
                            //{
                            //    pline.GetCentroidPoint().ToNTSCoordinate().MarkPoint(5000,"非标准车位");
                            //    throw new NotSupportedException("发现非标准车位块");
                            //}
                            if (!pline.Closed)
                            {
                                pline.GetCentroidPoint().ToNTSCoordinate().MarkPoint(5000, "非标准车位");
                                throw new NotSupportedException("发现非标准车位块");
                            }
                            Plines.Add(pline);
                        }
                    }
                    if(Plines.Count == 0)
                    {
                        insertPt.MarkPoint(5000,"非标准车位");
                        throw new NotSupportedException("发现非标准车位块");
                    }
                    var coors = Plines.OrderBy(p => p.Area).Last().ToNTSLineString().Coordinates;
                    var shell = new LinearRing(new Coordinate[] { coors[4], coors[1], coors[2], coors[3],coors[4] });
                    var linesgs = shell.ToLineSegments();
                    if(Math.Abs(linesgs[0].Length - linesgs[2].Length) > 1 ||
                        Math.Abs(linesgs[1].Length - linesgs[3].Length) > 1)
                    {
                        shell.Centroid.Coordinate.MarkPoint(5000, "非标准车位");
                    }
                    var polygon = new Polygon(shell);
                    
                    var line = polygon.Shell.ToLineSegments().OrderBy(l =>l.Distance(insertPt)).First();
                    var vector = line.NormalVector();
                    if(polygon.Contains(vector.Translate(insertPt).ToPoint()))
                    {
                        Cars.Add(new InfoCar(polygon,insertPt,vector));
                    }
                    else
                    {
                        Cars.Add(new InfoCar(polygon, insertPt, vector.RotateByQuarterCircle(2)));
                    }
                }
            }
            if (layerName.Contains("车道"))
            {
                if (ent is Line line)
                {
                    CAD_Lanes.Add(line);
                }
            }
        }
        #endregion

        #region 防火线生成
        public (List<LineSegment>,List<LineSegment>) Generate()
        {
            var fireWalls = new List<LineSegment>();
            var shutters = new List<LineSegment>();
            Generator = new FireLineGenerator(Basement, Lanes, Cars);
            Generator.Generate();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains("防火墙"))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "防火墙", 0);
                if (!acad.Layers.Contains("射线防火墙"))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "射线防火墙", 0);
                if (!acad.Layers.Contains("卷帘门"))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "卷帘门", 0);
                if (!acad.Layers.Contains("发射点0"))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "发射点0", 0);
                if (!acad.Layers.Contains("发射点1"))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "发射点1", 0);
                if (!acad.Layers.Contains("发射点2"))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "发射点2", 0);
                if (!acad.Layers.Contains("发射点3"))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "发射点3", 0);
            }
            Generator.CarFireLines.ForEach(l => l.ToDbLine(1, "防火墙").AddToCurrentSpace());
            Generator.BuildingFireLines.ForEach(l => l.ToDbLine(0, "防火墙").AddToCurrentSpace());
            Generator.RayFireLines.ForEach(l => l.ToDbLine(2, "射线防火墙").AddToCurrentSpace());
            //Generator.FireWalls.ForEach(w => w.ToDbLine(1,"防火墙").AddToCurrentSpace());
            Generator.Shutters.ForEach(w => w.ToDbLine(3, "卷帘门").AddToCurrentSpace());

            //Generator.StartPoints.Where(pt => pt.Directions.Count() == 0).
            //    ForEach(pt => pt.StartPoint.MarkPoint(2000, "发射点0", 0));
            //Generator.StartPoints.Where(pt => pt.Directions.Count() == 1).
            //    ForEach(pt => pt.StartPoint.MarkPoint(2000, "发射点1", 1));
            //Generator.StartPoints.Where(pt => pt.Directions.Count() == 2).
            //    ForEach(pt => pt.StartPoint.MarkPoint(2000, "发射点2", 2));
            //Generator.StartPoints.Where(pt => pt.Directions.Count() == 3).
            //    ForEach(pt => pt.StartPoint.MarkPoint(2000, "发射点3", 3));
            return (fireWalls, shutters);
        }
        #endregion
    }
}
