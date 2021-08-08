using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem.LayoutService
{
    public class LayoutOtherGTService
    {
        double angle = 30;
        double blockWidth = 300;
        public List<KeyValuePair<Polyline, (Point3d, Vector3d)>> Layout(List<ThIfcRoom> stairRooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            List<KeyValuePair<Polyline, (Point3d, Vector3d)>> layoutPts = new List<KeyValuePair<Polyline, (Point3d, Vector3d)>>();
            foreach (var sRoom in stairRooms)
            {
                var thSRoom = getLayoutStructureService.GetUseRoomBoundary(sRoom);
                var bufferSRoom = thSRoom.Buffer(5)[0] as Polyline;
                var stairDoors = getLayoutStructureService.GetNeedDoors(doors, bufferSRoom);

                foreach (var sDoor in stairDoors)
                {
                    var doorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(thSRoom, sDoor);
                    var doorCenterPt = getLayoutStructureService.GetDoorCenterPt(sDoor);
                    var otherDoorPt = doorCenterPt - doorInfo.Item3 * (doorInfo.Item5 / 2);
                    var structs = getLayoutStructureService.CalLayoutStruc(sDoor, columns, walls);
                    //using (Linq2Acad.AcadDatabase db= Linq2Acad.AcadDatabase.Active())
                    //{
                    //    foreach (var item in structs)
                    //    {
                    //        db.ModelSpace.Add(item);
                    //    }
                    //}
                    var layoutPt = CalControllerLayoutPt(structs, -doorInfo.Item3, otherDoorPt);
                    var pt = layoutPt.Item1 + layoutPt.Item2 * (blockWidth / 2);
                    layoutPts.Add(new KeyValuePair<Polyline, (Point3d, Vector3d)>(sDoor, (pt, layoutPt.Item2)));
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
        private (Point3d, Vector3d) CalControllerLayoutPt(List<Polyline> structs, Vector3d dir, Point3d doorPt)
        {
            List<(Point3d, Vector3d)> resLayoutInfo = new List<(Point3d, Vector3d)>();
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
                        var layoutDir = Vector3d.ZAxis.CrossProduct(lineDir);
                        if (dir.DotProduct(layoutDir) < 0)
                        {
                            layoutDir = -layoutDir;
                        }
                        
                        resLayoutInfo.Add((layoutPt, layoutDir));
                    }
                }
            }

            return resLayoutInfo.OrderBy(x => x.Item1.DistanceTo(doorPt)).FirstOrDefault();
        }
    }
}
