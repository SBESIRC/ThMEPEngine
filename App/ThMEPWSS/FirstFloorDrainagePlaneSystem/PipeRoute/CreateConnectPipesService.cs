using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.GlobleAStarAlgorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateConnectPipesService
    {
        double distance = 50;
        double inflectionWeight = 2;
        double moveSpace = 100;
        Dictionary<Vector3d, List<Line>> gridInfo;
        public CreateConnectPipesService(double step, Dictionary<Vector3d, List<Line>> _gridInfo)
        {
            distance = step;
            gridInfo = _gridInfo;
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="closetLane"></param>
        /// <param name="blockPt"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public List<Polyline> CreatePipes(Polyline polyline, Line closetLane, Point3d blockPt, Dictionary<List<Polyline>, double> holes)
        {
            List<Polyline> resLines = new List<Polyline>();
            resLines.AddRange(GetPathByAStar(polyline, closetLane, blockPt, holes));

            return resLines;
        }

        /// <summary>
        /// 使用a*算法寻路
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private List<Polyline> GetPathByAStar(Polyline polyline, Line closetLane, Point3d startPt, Dictionary<List<Polyline>, double> holes)
        {
            List<Polyline> resLines = new List<Polyline>();
            //计算逃生路径(用A*算法)
            //----构建寻路地图框线
            var allHoles = holes.SelectMany(x => x.Key).ToList();
            var mapFrame = CreateMapFrame(closetLane, startPt, allHoles, 500);
            var movePoly = AdjustMapFrame(polyline, closetLane);
            mapFrame = mapFrame.Intersection(new DBObjectCollection() { movePoly }).Cast<Polyline>().OrderByDescending(x => x.Area).First();

            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            GlobleAStarRoutePlanner<Line> aStarRoute = new GlobleAStarRoutePlanner<Line>(mapFrame, dir, closetLane, distance, distance, distance, inflectionWeight);

            //----设置障碍物
            foreach (var weightHole in holes)
            {
                var resHoles = SelelctCrossing(weightHole.Key, mapFrame).Select(x => x.ToNTSPolygon().ToDbMPolygon()).ToList();
                aStarRoute.SetObstacle(resHoles, weightHole.Value);
            }

            //----计算路径
            var path = aStarRoute.Plan(startPt);
            if (path != null)
            {
                resLines.Add(path);
            }

            return resLines;
        }

        /// <summary>
        /// 创建地图
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <param name="expandLength"></param>
        /// <returns></returns>
        private Polyline CreateMapFrame(Line lane, Point3d startPt, List<Polyline> holes, double expandLength)
        {
            Vector3d xDir = (lane.EndPoint - lane.StartPoint).GetNormal();
            List<Point3d> pts = new List<Point3d>() { startPt, lane.StartPoint, lane.EndPoint };
            var polyline = GeometryUtils.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;
            var intersectHoles = SelelctCrossing(holes, polyline);
            foreach (var iHoles in intersectHoles)
            {
                pts.AddRange(GeometryUtils.GetAllPolylinePts(iHoles));
            }
            var resPolyline = GeometryUtils.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;

            return AdjustMapFrame(resPolyline, lane);
        }

        /// <summary>
        /// 移动框线保证连接线是轴网整数倍
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="closetLine"></param>
        /// <returns></returns>
        private Polyline AdjustMapFrame(Polyline frame, Line closetLine)
        {
            var lineDir = Vector3d.ZAxis.CrossProduct(closetLine.EndPoint - closetLine.StartPoint).GetNormal();
            var grids = gridInfo.Where(x => x.Key.IsParallelTo(lineDir, new Tolerance(0.001, 0.001)))
                .SelectMany(x => x.Value)
                .OrderBy(x => x.Distance(closetLine)).ToList();
            if (grids.Count > 0)
            {
                var dir = (closetLine.EndPoint - closetLine.StartPoint).GetNormal();
                var firGrid = grids.First();
                var minPt = GetBoungdingBoxMinPt(dir, frame);
                var closetPt = firGrid.GetClosestPointTo(minPt, true);
                var moveDis = closetPt.DistanceTo(minPt) % moveSpace;
                if (moveDis != 0)
                {
                    moveDis = moveSpace - moveDis;
                }
                var sPt = minPt;
                var moveDir = (minPt - closetPt).GetNormal();
                var toPt = sPt + moveDir * moveDis;
                frame.Move(sPt, toPt);
            }
            return frame;
        }

        /// <summary>
        /// 获取boundingbox
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Point3d GetBoungdingBoxMinPt(Vector3d xDir, Polyline polyline)
        {
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            var matrix = new Matrix3d(new double[] {
                xDir.X, xDir.Y, xDir.Z, 0,
                yDir.X, yDir.Y, yDir.Z, 0,
                zDir.X, zDir.Y, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });
            var clonePoly = polyline.Clone() as Polyline;
            clonePoly.TransformBy(matrix);
            List<Point3d> allPts = new List<Point3d>();
            for (int i = 0; i < clonePoly.NumberOfVertices; i++)
            {
                allPts.Add(clonePoly.GetPoint3dAt(i));
            }

            allPts = allPts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;

            List<Point3d> boundingbox = new List<Point3d>();
            boundingbox.Add(new Point3d(minX, minY, 0));
            boundingbox.Add(new Point3d(maxX, maxY, 0));

            return boundingbox[0].TransformBy(matrix.Inverse());
        }

        /// <summary>
        /// 筛选元素
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Polyline> SelelctCrossing(List<Polyline> polylines, Polyline polyline)
        {
            var objs = polylines.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            return resHoles;
        }
    }
}
