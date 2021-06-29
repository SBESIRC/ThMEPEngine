using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using ThCADExtension;
using ThCADCore.NTS;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDFindColdPtService
    {
        

        public static void findCoolSupplyPt(List<ThToilateRoom> roomList, List<ThIfcSanitaryTerminalToilate> toilateList, out List<ThIfcSanitaryTerminalToilate> aloneToilate)
        {
            aloneToilate = new List<ThIfcSanitaryTerminalToilate>();

            foreach (var terminal in toilateList)
            {
                if (terminal.SupplyCool.Count == 0)
                {
                    continue;
                }

                List<Point3d> ptOnWall = new List<Point3d>();

                var room = roomList.Where(x => x.toilate.Contains(terminal));
                if (room.Count() == 0)
                {
                    aloneToilate.Add(terminal);
                }
                else
                {
                    var wallList = room.First().wallList;
                    var closeWall = findNearbyWall(wallList, terminal);
                    var parallelWall = findParallelWall(closeWall, terminal);

                    Line closestWall = null;
                    if (parallelWall.Count > 1)
                    {
                        var parallelWallDistDict = parallelWall.ToDictionary(x => x, x => x.GetDistToPoint(terminal.BasePt, false));
                        parallelWallDistDict = parallelWallDistDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                        closestWall = parallelWallDistDict.First().Key;
                    }
                    else if (parallelWall.Count == 1)
                    {
                        closestWall = parallelWall[0];
                    }

                    if (closestWall != null)
                    {
                        //靠墙
                        terminal.SupplyCool.ForEach(x => ptOnWall.Add(closestWall.GetClosestPointTo(x, false)));
                    }
                    else
                    {   //岛
                        terminal.SupplyCool.ForEach(x => ptOnWall.Add(x));
                    }

                    terminal.SupplyCoolOnWall = ptOnWall;
                }
            }
        }

        private static List<Line> findParallelWall(List<Line> wallList, ThIfcSanitaryTerminalToilate terminal)
        {
            List<Line> parallelWall = new List<Line>();
            if (wallList.Count > 0)
            {
                parallelWall = wallList.Where(wall =>
               {
                   var wallDir = (wall.EndPoint - wall.StartPoint).GetNormal();
                   var angle = wallDir.GetAngleTo(terminal.Dir);
                   var bReturn = false;
                   if (Math.Abs(Math.Cos(angle)) <= Math.Cos(70 * Math.PI / 180))
                   {
                       bReturn = true;
                   }
                   return bReturn;
               }).ToList();
            }
            return parallelWall;
        }


        private static List<Line> findNearbyWall(List<Line> wallList, ThIfcSanitaryTerminalToilate terminal)
        {
            List<Line> closeWall = new List<Line>();
            if (wallList.Count > 0)
            {
                var ptLeftTop = terminal.BasePt;
                var ptRightTop = terminal.Boundary.GetPoint3dAt(2);
                var tol = new Tolerance(10, 10);

                closeWall = wallList.Where(wall =>
               {
                   var bReturn = false;

                   var ptLeftWall = wall.GetClosestPointTo(ptLeftTop, true);
                   var ptRightWall = wall.GetClosestPointTo(ptRightTop, true);

                   if (ptLeftTop.DistanceTo(ptLeftWall) <= DrainageSDCommon.TolToilateToWall ||
                       ptRightTop.DistanceTo(ptRightWall) <= DrainageSDCommon.TolToilateToWall)
                   {

                       var ptSupplyWall = wall.GetClosestPointTo(terminal.SupplyCool[0], true);
                       if (wall.ToCurve3d().IsOn(ptSupplyWall, tol))
                       {
                           bReturn = true;
                       }


                   }

                   return bReturn;

               }).ToList();
            }

            return closeWall;
        }

    }
}

