using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;

using ThMEPWSS.DrainageSystemDiagram;


namespace ThMEPWSS.SprinklerConnect.Service
{
    internal class ThSprinklerPipeService
    {
        /// <summary>
        /// 打散干管和支干管。
        /// </summary>
        /// <param name="mainPipe">干管原数据</param>
        /// <param name="subMainPipe">支干管原数据</param>
        /// <param name="mainLine">打断后的干管</param>
        /// <param name="subMainLine">打断后的支干管</param>
        /// <param name="allLine">全部管线</param>
        public static void ThSprinklerPipeToLine(List<Polyline> mainPipe, List<Polyline> subMainPipe, out List<Line> mainLine, out List<Line> subMainLine, out List<Line> allLine)
        {
            mainLine = new List<Line>();
            subMainLine = new List<Line>();
            var allTemp = new List<Line>();
            var mainTemp = new List<Line>();
            var subMainTemp = new List<Line>();

            mainTemp = ThSprinklerLineService.PolylineToLine(mainPipe);
            subMainTemp = ThSprinklerLineService.PolylineToLine(subMainPipe);
            allTemp.AddRange(mainTemp);
            allTemp.AddRange(subMainTemp);

            allLine = ThDrainageSDCleanLineService.simplifyLine(allTemp);

            foreach (var l in allLine)
            {
                var mainP = mainTemp.Where(x => IsWithIn(l, x));
                if (mainP.Count() > 0)
                {
                    mainLine.Add(l);
                }
                else
                {
                    var subP = subMainTemp.Where(x => IsWithIn(l, x));
                    if (subP.Count() > 0)
                    {
                        subMainLine.Add(l.ExtendLine(10.0));
                    }
                }
            }

        }


        public static void ThSprinklerPipeToLine2(List<Polyline> mainPipe, List<Polyline> subMainPipe, out List<Line> mainLine, out List<Line> subMainLine, out List<Line> allLine)
        {
            mainLine = new List<Line>();
            subMainLine = new List<Line>();
            var allTemp = new List<Line>();
            var mainTemp = new List<Line>();
            var subMainTemp = new List<Line>();

            mainTemp = ThSprinklerLineService.PolylineToLine(mainPipe);
            subMainTemp = ThSprinklerLineService.PolylineToLine(subMainPipe);
            allTemp.AddRange(mainTemp);
            allTemp.AddRange(subMainTemp);

            allLine = ThDrainageSDCleanLineService.simplifyLine(allTemp);

            foreach (var l in allLine)
            {
                var bMainP = false;
                var bSub = false;
                foreach (var main in mainTemp)
                {
                    if (IsWithIn(l, main) == true)
                    {
                        bMainP = true;
                        bSub = true;
                        mainLine.Add(l);
                        break;
                    }
                }

                if (bMainP == false)
                {
                    foreach (var sub in subMainTemp)
                    {
                        if (IsWithIn(l, sub) == true)
                        {
                            bSub = true;

                            subMainLine.Add(l);
                            break;
                        }
                    }
                }
                if (bSub == false)
                {
                    subMainLine.Add(l);
                }
            }
        }

        private static bool IsWithIn(Line A, Line B)
        {
            var bReturn = false;
            var angelTol = 1;
            var tol = new Tolerance(30, 30);
            if (ThSprinklerLineService.IsParallelAngle(A.Angle, B.Angle, angelTol))
            {
                var i = 0;

                i = i + Convert.ToInt16(B.IsPointOnCurve(A.StartPoint, tol));
                i = i + Convert.ToInt16(B.IsPointOnCurve(A.EndPoint, tol));

                if (i == 2)
                {
                    bReturn = true;
                }
            }

            return bReturn;
        }


    }
}
