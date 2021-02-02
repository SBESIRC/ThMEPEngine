using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public static class CheckService
    {
        static readonly double tol = 3500;   //3.5米以内我们认为不相交
        /// <summary>
        /// 检查连接线
        /// </summary>
        /// <returns></returns>
        public static bool CheckConnectLines(KeyValuePair<Polyline, List<Polyline>> holeInfo, Polyline usePoly, List<Polyline> endingPolys)
        {
            return CheckUsefulLine(usePoly, endingPolys) && CheckFrameLine(holeInfo.Key, usePoly) && CheckHolesLine(holeInfo.Value, usePoly);
        }

        /// <summary>
        /// 检查副车道连接线
        /// </summary>
        /// <returns></returns>
        public static bool CheckOtherConnectLines(KeyValuePair<Polyline, List<Polyline>> holeInfo, Polyline usePoly, List<Polyline> endingPolys)
        {
            return CheckUsefulLine(usePoly, endingPolys) && CheckFrameLine(holeInfo.Key, usePoly) &&
                CheckHolesLine(holeInfo.Value, usePoly) && CheckConnectPtNum(usePoly.EndPoint, endingPolys);
        }

        /// <summary>
        /// 判断该线是否与洞口相交
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="usePoly"></param>
        /// <returns></returns>
        public static bool CheckHolesLine(List<Polyline> polyline, Polyline usePoly)
        {
            foreach (var poly in polyline)
            {
                if (usePoly.IsIntersects(poly))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 判断该线是否与外框线相交
        /// </summary>
        /// <param name="usePoly"></param>
        /// <param name="endingPolys"></param>
        /// <returns></returns>
        public static bool CheckFrameLine(Polyline polyline, Polyline usePoly)
        {
            return !usePoly.IsIntersects(polyline);
        }

        /// <summary>
        /// 判断该线是否是可用线(是否和其他连接线相交)
        /// </summary>
        /// <param name="usePoly"></param>
        /// <param name="endingPolys"></param>
        /// <returns></returns>
        public static bool CheckUsefulLine(Polyline usePoly, List<Polyline> endingPolys)
        {
            ///Line connectLine = new Line(usePoly.StartPoint, usePoly.EndPoint);
            foreach (var poly in endingPolys)
            {
                var intersectPt = usePoly.IntersectWithEx(poly);
                foreach (Point3d pt in intersectPt)
                {
                    if (pt.DistanceTo(usePoly.StartPoint) > tol &&
                        pt.DistanceTo(usePoly.EndPoint) > tol)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 检查一个连接点最多只能有四个连接线
        /// </summary>
        /// <param name="connectPt"></param>
        /// <param name="endingPolys"></param>
        /// <returns></returns>
        public static bool CheckConnectPtNum(Point3d connectPt, List<Polyline> endingPolys)
        {
            var checkPolys = endingPolys.Where(x => x.StartPoint.IsEqualTo(connectPt, new Tolerance(1, 1)) ||
                 x.EndPoint.IsEqualTo(connectPt, new Tolerance(1, 1)))
                .ToList();
            return checkPolys.Count < 4;
        }
    }
}
