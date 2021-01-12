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
        /// <summary>
        /// 检查连接线
        /// </summary>
        /// <returns></returns>
        public static bool CheckConnectLines(KeyValuePair<Polyline, List<Polyline>> holeInfo, Polyline usePoly, List<Polyline> endingPolys)
        {
            return CheckUsefulLine(usePoly, endingPolys) && CheckFrameLine(holeInfo.Key, usePoly) && CheckHolesLine(holeInfo.Value, usePoly);
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
            Line connectLine = new Line(usePoly.StartPoint, usePoly.EndPoint);
            foreach (var poly in endingPolys)
            {
                var intersectPt = connectLine.IntersectWithEx(poly);
                if (intersectPt.Count > 0 &&
                    !(intersectPt[0].IsEqualTo(poly.StartPoint, new Tolerance(1, 1)) || intersectPt[0].IsEqualTo(poly.EndPoint, new Tolerance(1, 1))))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
