using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class CreateStartExtendLineService
    {
        public void CreateStartLines(List<Line> lanes, Point3d startPt, Vector3d dir, List<Polyline> holes)
        {

        }

        private void CreateExtendLine(List<Line> lanes, Point3d startPt, Vector3d dir)
        {
            var lanePtInfo = GetClosetLane(lanes, startPt);
            Line lane = lanePtInfo.Value;
            Point3d closetPt = lanePtInfo.Key;


        }

        /// <summary>
        /// 获取最近车道线
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private KeyValuePair<Point3d, Line> GetClosetLane(List<Line> lanes, Point3d startPt)
        {
            var lanePtInfo = lanes.ToDictionary(x => x.GetClosestPointTo(startPt, false), y => y)
                .OrderBy(x => x.Key.DistanceTo(startPt))
                .First();

            return lanePtInfo;
        }
    }
}
