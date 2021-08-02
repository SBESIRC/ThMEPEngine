using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCoolPtService
    {
        public static void findCoolSupplyPt(List<ThToiletRoom> roomList, List<ThTerminalToilet> toiletList, out List<ThTerminalToilet> aloneToilet)
        {
            aloneToilet = new List<ThTerminalToilet>();

            foreach (var terminal in toiletList)
            {
                var room = roomList.Where(x => x.toilet.Contains(terminal));
                if (room.Count() == 0)
                {
                    aloneToilet.Add(terminal);
                }
                else
                {
                    var wallList = room.First().wallList;
                    List<Point3d> ptOnWall = findPtOnWall(wallList, terminal, ThDrainageSDCommon.TolToiletToWall, false);
                    terminal.SupplyCoolOnWall = ptOnWall;
                }
            }
        }

        public static List<Point3d> findPtOnWall(List<Line> wallList, ThTerminalToilet terminal, int TolClosedWall, bool toiletFaceSide)
        {
            List<Point3d> ptOnWall = new List<Point3d>();
            var closeWall = findNearbyWall(wallList, terminal, TolClosedWall);
            var parallelWall = findParallelWall(closeWall, terminal, toiletFaceSide);

            Line closestWall = null;
            if (parallelWall.Count > 0)
            {
                var parallelWallDistDict = parallelWall.ToDictionary(x => x, x => x.GetDistToPoint(terminal.Boundary.GetPoint3dAt(1), false));
                parallelWallDistDict = parallelWallDistDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                closestWall = parallelWallDistDict.First().Key;
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

            return ptOnWall;
        }

        private static List<Line> findParallelWall(List<Line> wallList, ThTerminalToilet terminal, bool toiletFaceSide)
        {
            List<Line> parallelWall = new List<Line>();
            if (wallList.Count > 0)
            {
                parallelWall = wallList.Where(wall =>
                {
                    var ptOnWall = wall.GetClosestPointTo(terminal.SupplyCool[0], true);
                    var ptToWallDir = (terminal.SupplyCool[0] - ptOnWall).GetNormal();
                    var angle = ptToWallDir.GetAngleTo(terminal.Dir);

                    var bReturn = false;
                    if (toiletFaceSide == false)
                    {
                        if (Math.Abs(Math.Cos(angle)) >= Math.Cos(10 * Math.PI / 180))
                        {
                            bReturn = true;
                        }
                    }
                    else
                    {
                        if (Math.Cos(angle) >= Math.Cos(10 * Math.PI / 180))
                        {
                            bReturn = true;
                        }
                    }
                    return bReturn;

                }).ToList();
            }
            return parallelWall;
        }


        private static List<Line> findNearbyWall(List<Line> wallList, ThTerminalToilet terminal, int TolClosedWall)
        {
            List<Line> closeWall = new List<Line>();
            if (wallList.Count > 0)
            {
                var ptLeftTop = terminal.Boundary.GetPoint3dAt(1);
                var ptRightTop = terminal.Boundary.GetPoint3dAt(2);
                var tol = new Tolerance(10, 10);

                closeWall = wallList.Where(wall =>
               {
                   var bReturn = false;

                   var ptLeftWall = wall.GetClosestPointTo(ptLeftTop, true);
                   var ptRightWall = wall.GetClosestPointTo(ptRightTop, true);

                   if (ptLeftTop.DistanceTo(ptLeftWall) <= TolClosedWall ||
                       ptRightTop.DistanceTo(ptRightWall) <= TolClosedWall)
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

