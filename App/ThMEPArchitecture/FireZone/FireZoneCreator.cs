using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThParkingStall.Core.FireZone;
using ThParkingStall.Core.Tools;
using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;

namespace ThMEPArchitecture.FireZone
{
    public class FireZoneCreator
    {
        public List<Polyline> CAD_WallLines = new List<Polyline>();
        public List<Polyline> CAD_Obstacles = new List<Polyline>();
        public List<Line> CAD_FireLines = new List<Line>();

        public Polygon Basement;
        public List<Polygon> Obstacles;
        public List<LineSegment> FireLines;
        public List<Polygon> BuildingBounds;

        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public Serilog.Core.Logger Logger = null;
        private Stopwatch _stopwatch = new Stopwatch();
        private double t_pre;
        public FireZoneCreator(BlockReference block, Serilog.Core.Logger logger)
        {
            _stopwatch.Start();
            t_pre = _stopwatch.Elapsed.TotalSeconds;
            Logger = logger;
            Extract(block);
            UpdateObstacles();
            FireLines = CAD_FireLines.Select(l => l.ToNTSLineSegment()).ToList();
            UpdateBounds();
            UpdateBasement();
            Logger?.Information($"数据提取用时{_stopwatch.Elapsed.TotalSeconds - t_pre}s");
        }
        #region 图形数据处理
        private void UpdateBasement()
        {
            var bounds = new MultiPolygon(BuildingBounds.ToArray());
            Basement = CAD_WallLines.OrderBy(wl => wl.Area).Last().ToNTSPolygon();
            Basement = Basement.Difference(bounds).Get<Polygon>(false).OrderBy(p => p.Area).Last();
        }
        private void UpdateBounds()
        {
            var buildingtol = 3500;
            var buffered = new MultiPolygon(Obstacles.ToArray()).Buffer(buildingtol, MitreParam).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物
            BuildingBounds = new MultiPolygon(buffered.ToArray()).Buffer(-buildingtol + 100, MitreParam).Get<Polygon>(true);
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
            if (layerName.Contains("防火分区线"))
            {
                if (ent is Line line)
                {
                    CAD_FireLines.Add(line);
                }
            }
        }
        #endregion
        #region 数据转译
        public FireZoneMap GetFireZoneMap()
        {
            var translator =new FireZoneTranslator(Basement,FireLines,Logger);
            //translator.Clean();
            var map = translator.Map;
            map.Root.polygon.ToDbMPolygon().AddToCurrentSpace();

            //translator.Paths.ForEach(l => l.ToDbPolyline().AddToCurrentSpace());
            map.Edges.ForEach(e => e.Path.ToDbPolyline().AddToCurrentSpace());
            var poly = map.FindBestFireZone(3800, 4000);
            poly.ToDbMPolygon().AddToCurrentSpace();
            return null;
        }
        #endregion
    }
}
