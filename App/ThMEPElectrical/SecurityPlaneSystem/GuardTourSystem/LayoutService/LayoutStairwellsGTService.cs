using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem.LayoutService
{
    public class LayoutStairwellsGTService
    {
        double angle = 30;
        double blockWidth = 300;
        public List<Point3d> Layout(ThIfcRoom room, List<ThIfcRoom> stairRooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls) 
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            List<Point3d> layoutPts = new List<Point3d>();
            foreach (var sRoom in stairRooms)
            {
                var thSRoom = sRoom.Boundary as Polyline;
                var bufferSRoom = thSRoom.Buffer(5)[0] as Polyline;
                var stairDoors = getLayoutStructureService.GetNeedDoors(doors, bufferSRoom);

                foreach (var sDoor in stairDoors)
                {
                    var doorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room.Boundary as Polyline, sDoor);
                    var structs = getLayoutStructureService.CalLayoutStruc(sDoor, columns, walls);
                    var layoutPt = CalControllerLayoutPt(structs, doorInfo.Item2, doorInfo.Item1);
                    layoutPts.Add(layoutPt);
                }
            }

            return layoutPts;
        }

        /// <summary>
        /// 计算控制器布置点位
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="dir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private Point3d CalControllerLayoutPt(List<Polyline> structs, Vector3d dir, Point3d doorPt)
        {
            List<Point3d> resLayoutInfo = new List<Point3d>();
            foreach (var str in structs)
            {
                var allLines = str.GetAllLinesInPolyline();
                foreach (var line in allLines)
                {
                    var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                    if (!dir.IsParallelWithTolerance(lineDir, angle) && line.Length > blockWidth)
                    {
                        var pt = line.StartPoint.DistanceTo(doorPt) < line.EndPoint.DistanceTo(doorPt) ? line.StartPoint : line.EndPoint;
                        var checkDir = (pt - doorPt).GetNormal();
                        if (checkDir.DotProduct(lineDir) < 0)
                        {
                            lineDir = -lineDir;
                        }

                        var layoutPt = pt + lineDir * (blockWidth / 2);
                        resLayoutInfo.Add(layoutPt);
                    }
                }
            }

            return resLayoutInfo.OrderBy(x => x.DistanceTo(doorPt)).FirstOrDefault();
        }
    }
}
