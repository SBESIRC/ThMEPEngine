using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Service;
using ThMEPElectrical.SecurityPlaneSystem.Utils;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public class SystemConnectPipeService
    {
        public List<Polyline> Conenct(Polyline polyline, List<Polyline> columns, List<Line> trunkings, List<Point3d> pts, List<Polyline> holes)
        {
            List<Polyline> resLines = new List<Polyline>();
            if (trunkings.Count <= 0)
            {
                return resLines;
            }
            List<Polyline> allHoles = new List<Polyline>(holes);
            holes.AddRange(columns);
            foreach (var pt in pts)
            {
                var closetLine = GetClosetLane(trunkings, pt, polyline);
                PipePathService pipePathService = new PipePathService();
                resLines.Add(pipePathService.CreatePipePath(polyline, closetLine.Key, pt, allHoles));
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
        private KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lanes, Point3d startPt, Polyline polyline)
        {
            var closeInfo = lanes.ToDictionary(x => x, y => y.GetClosestPointTo(startPt, false))
                .OrderBy(x => x.Value.DistanceTo(startPt))
                .First();
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

            BFSPathPlaner pathPlaner = new BFSPathPlaner(400);
            var closetLine = pathPlaner.FindingClosetLine(startPt, lanes, polyline);
            var closetPt = closetLine.GetClosestPointTo(startPt, false);

            return new KeyValuePair<Line, Point3d>(closetLine, closetPt);
        }
    }
}
