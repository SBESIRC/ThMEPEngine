using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCoolSupplyPt
    {

        public static List<Polyline> hasTerminalRoom(List<Polyline> roomList, List<ThIfcSanitaryTerminalToilate> toilateList)
        {
            var hasTerminalRoom = new List<Polyline>();

            foreach (var room in roomList)
            {
                var bAdd = false;
                foreach (var terminal in toilateList)
                {
                    var outline = terminal.Boundary;
                    if (room.Contains(outline) || room.Intersects(outline))
                    {
                        hasTerminalRoom.Add(room);
                        bAdd = true;
                        break;
                    }
                    if (bAdd == true)
                    { continue; }
                }
            }

            return hasTerminalRoom;
        }

        public static Dictionary<string, Point3d> findCoolSupplyPt(List<Polyline> roomList, List<ThIfcSanitaryTerminalToilate> toilateList)
        {
            Dictionary<string, Point3d> supplyPtCool = null;

            var roomWall = breakRoomWall(roomList);

            supplyPtCool = findCoolSupplyPt(roomWall, toilateList, out var aloneToilate);

            return supplyPtCool;
        }

        private static Dictionary<Polyline, List<Line>> breakRoomWall(List<Polyline> roomList)
        {
            Dictionary<Polyline, List<Line>> roomWall = new Dictionary<Polyline, List<Line>>();

            for (int i = 0; i < roomList.Count; i++)
            {
                var roomBreak = new List<Line>();
                for (int j = 0; j < roomList[i].NumberOfVertices; j++)
                {
                    var pt = roomList[i].GetPoint3dAt(j % roomList[i].NumberOfVertices);
                    var ptNext = roomList[i].GetPoint3dAt((j + 1) % roomList[i].NumberOfVertices);

                    //pt = new Point3d(pt.X, pt.Y, 0);
                    //ptNext = new Point3d(ptNext.X, ptNext.Y, 0);

                    var roomLine = new Line(pt, ptNext);
                    roomBreak.Add(roomLine);
                }
                roomWall.Add(roomList[i], roomBreak);
            }

            return roomWall;
        }

        private static Dictionary<string, Point3d> findCoolSupplyPt(Dictionary<Polyline, List<Line>> roomList, List<ThIfcSanitaryTerminalToilate> toilateList, out List<ThIfcSanitaryTerminalToilate> aloneToilate)
        {
            Dictionary<string, Point3d> supplyPtCool = new Dictionary<string, Point3d>();
            aloneToilate = new List<ThIfcSanitaryTerminalToilate>();

            List<Polyline> roomListPy = roomList.Select(x => x.Key).ToList();

            foreach (var terminal in toilateList)
            {
                Point3d pt = new Point3d();
                Point3d ptSec = new Point3d();

                var room = roomListPy.Where(x => x.Contains(terminal.Boundary) || x.Intersects(terminal.Boundary));
                if (room.Count() == 0)
                {
                    aloneToilate.Add(terminal);
                }
                else
                {
                    var wallList = roomList[room.First()];
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
                        if (terminal.SupplyCool != Point3d.Origin)
                        {
                            pt = closestWall.GetClosestPointTo(terminal.SupplyCool, false);
                        }

                        if (terminal.SupplyCoolSec != Point3d.Origin)
                        {
                            ptSec = closestWall.GetClosestPointTo(terminal.SupplyCoolSec, false);
                        }
                    }
                    else
                    {   //岛
                        if (terminal.SupplyCool != Point3d.Origin)
                        {
                            pt = terminal.SupplyCool;
                        }
                        if (terminal.SupplyCoolSec != Point3d.Origin)
                        {
                            ptSec = terminal.SupplyCoolSec;
                        }
                    }

                    if (pt != Point3d.Origin)
                    {
                        supplyPtCool.Add(terminal.Uuid, pt);
                        terminal.SupplyCoolOnWall = pt;
                    }
                    if (ptSec != Point3d.Origin)
                    {
                        supplyPtCool.Add(terminal.Uuid + DrainageSDCommon .GJSecPtSuffix, ptSec);
                        terminal.SupplyCoolSecOnWall = ptSec;
                    }
                }
            }

            return supplyPtCool;

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
                closeWall = wallList.Where(wall =>
               {
                   var bReturn = false;
                   var ptLeftTop = terminal.BasePt;
                   var ptRightTop = terminal.Boundary.GetPoint3dAt(2);

                   if (wall.GetDistToPoint(ptLeftTop, false) <= DrainageSDCommon.TolToilateToWall ||
                       wall.GetDistToPoint(ptRightTop, false) <= DrainageSDCommon.TolToilateToWall)
                   {
                       bReturn = true;
                   }

                   return bReturn;

               }).ToList();
            }



            return closeWall;
        }
    }
}

