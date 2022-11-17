using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThParkingStall.Core.LineCleaner;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.FireZone
{
    public class FireLineCreator
    {
        public List<Polyline> CAD_WallLines = new List<Polyline>();
        public List<Polyline> CAD_CarSpots = new List<Polyline>();
        public List<Polyline> CAD_Obstacles = new List<Polyline>();

        public List<Polygon> Obstacles;
        public List<LineSegment> Skeletons;
        public LinearRing Boundary;
        public List<LineString> FireLines;
        public List<LinearRing> Holes;
        private Stopwatch _stopwatch = new Stopwatch();
        private double t_pre = 0;
        public Serilog.Core.Logger Logger = null;
        public FireLineCreator(BlockReference block, Serilog.Core.Logger logger)
        {
            _stopwatch.Start();
            t_pre = _stopwatch.Elapsed.TotalSeconds;
            Logger = logger;
            Extract(block);
            UpdateObstacles();
            //ExtractSkeleton();
            GenerateLines();
            Logger?.Information($"数据提取用时{_stopwatch.Elapsed.TotalSeconds - t_pre}s");
        }
        #region 图形数据处理
        private void ExtractSkeleton()
        {
            Skeletons = new List<LineSegment>();
            double tol = 1000;
            foreach (var obs in Obstacles)
            {
                var lines = obs.Shell.ToLineSegments();
                var cleaner = new LineService(lines, tol);
                lines = cleaner.MergeParalle(lines).Where(l =>l.Length > tol).ToList();
                lines = cleaner.Noding(lines).Where(l =>l.Length > tol).ToList();
                lines = cleaner.MergePoints(lines).Where(l =>l.Length > tol).ToList();
                Skeletons.AddRange(lines);
            }
        }
        private void GenerateLines()
        {
            var generator = new ShearWallLineCreator(Obstacles, 9000);
            Skeletons = generator.GenerateLines();
        }
        private void UpdateObstacles()
        {
            Obstacles = new List<Polygon>();
            if (CAD_Obstacles.Count > 0)
            {
                //输入打成线+求面域+union
                var UnionedObstacles = new MultiPolygon(CAD_Obstacles.Select(pl => 
                    pl.ToNTSLineString()).ToList().GetPolygons().ToArray()).Union();
                //求和地库交集
                Obstacles = UnionedObstacles.Get<Polygon>(true);
            }
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
        }
        #endregion
    }
}
