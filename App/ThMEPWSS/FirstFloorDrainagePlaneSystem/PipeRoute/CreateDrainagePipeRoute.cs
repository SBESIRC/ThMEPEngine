using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateDrainagePipeRoute
    {
        Polyline frame;                                     //外框线
        List<Polyline> mainPipes;                           //排水主管
        List<VerticalPipeModel> verticalPipes;              //排水立管
        List<DrainingEquipmentModel> drainingEquipment;     //洁具立管
        List<Polyline> wallPolys;                           //墙线
        readonly double step = 100;                         //步长
        public CreateDrainagePipeRoute(Polyline polyline, List<Polyline> mainPolys, List<VerticalPipeModel> verticalPipesModel, List<DrainingEquipmentModel> drainingEquipmentModel, 
            List<Polyline> walls)
        {
            frame = polyline;
            mainPipes = mainPolys;
            verticalPipes = verticalPipesModel;
            drainingEquipment = drainingEquipmentModel;
            wallPolys = walls;
        }

        /// <summary>
        /// 计算路由
        /// </summary>
        /// <returns></returns>
        public List<Polyline> Routing()
        {
            var resLines = new List<Polyline>();
            var allLines = mainPipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            foreach (var pipe in verticalPipes)
            {
                var closetLine = GetClosetLane(allLines, pipe.Position, frame);
                CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step);
                var connectLine = connectPipesService.CreatePipes(frame, closetLine.Key, pipe.Position, wallPolys);
                resLines.AddRange(connectLine);
            }

            foreach (var pipe in drainingEquipment)
            {
                var closetLine = GetClosetLane(allLines, pipe.DiranPoint, frame);
                CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step);
                var connectLine = connectPipesService.CreatePipes(frame, closetLine.Key, pipe.DiranPoint, wallPolys);
                resLines.AddRange(connectLine);
            }

            return resLines;
        }

        /// <summary>
        /// 获取最近的线信息
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lines, Point3d startPt, Polyline polyline)
        {
            var closeInfo = GeometryUtils.GetClosetLine(lines, startPt);
            Line checkLine = new Line(startPt, closeInfo.Value);
            if (!CheckService.CheckIntersectWithFrame(checkLine, polyline))
            {
                var checkDir = (closeInfo.Value - startPt).GetNormal();
                var lineDir = Vector3d.ZAxis.CrossProduct((closeInfo.Key.EndPoint - closeInfo.Key.StartPoint).GetNormal());
                if (checkDir.IsEqualTo(lineDir, new Tolerance(0.001, 0.001)))
                {
                    return closeInfo;
                }
            }

            BFSPathPlaner pathPlaner = new BFSPathPlaner(step);
            var closetLine = pathPlaner.FindingClosetLine(startPt, lines, polyline);
            var closetPt = closetLine.GetClosestPointTo(startPt, false);

            return new KeyValuePair<Line, Point3d>(closetLine, closetPt);
        }
    }
}
