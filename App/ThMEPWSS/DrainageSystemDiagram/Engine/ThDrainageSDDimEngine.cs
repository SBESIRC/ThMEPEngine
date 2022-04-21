using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDDimEngine
    {
        public static List<RotatedDimension> getDim(ThDrainageSDDataExchange dataset)
        {
            var positionDim = new List<RotatedDimension>();

            if (dataset.TerminalList != null && dataset.TerminalList.Count > 0)
            {
                var groupList = dataset.GroupList;
                var alreadyDimArea = new List<Polyline>();

                //设置躲避的线
                var allIsolateLine = new List<Line>();
                var roomLine = dataset.roomList.SelectMany(x => x.wallList).ToList();
                var pipes = dataset.Pipes;
                //var toiletLine = dataset.TerminalList.Select(x => new Line(x.Boundary.GetPoint3dAt(0), x.Boundary.GetPoint3dAt(3))).ToList();
                allIsolateLine.AddRange(roomLine);
                allIsolateLine.AddRange(pipes);
                //allIsolateLine.AddRange(toiletLine);

                foreach (var group in groupList)
                {
                    Dictionary<Line, List<Point3d>> baseLineDict = new Dictionary<Line, List<Point3d>>();

                    var pts = group.Value.SelectMany(x => x.SupplyCoolOnWall).ToList();
                    var orderPts = pts;
                    if (pts.Count > 1)
                    {
                        orderPts = ThDrainageSDCommonService.orderPtInStrightLine(pts);
                    }

                    if (group.Key.Contains(ThDrainageSDCommon.tagIsland))
                    {
                        //岛
                        baseLineDict = ThDrainageSDDimService.getDimAreaBaseLineIsland(group.Value, dataset.roomList, orderPts);
                    }
                    else
                    {
                        //普通组 小房间细分组
                        baseLineDict = ThDrainageSDDimService.getDimAreaBaseLine(group.Value, dataset.roomList, orderPts);
                    }

                    if (baseLineDict.Count == 0)
                    {
                        continue;
                    }

                    var possibleDimArea = ThDrainageSDDimService.getPossibleDimArea(baseLineDict, group.Key, group.Value);

                    var possibleDimAreaList = possibleDimArea.Select(x => x.Key).ToList();
                    var dimArea = ThDrainageSDDimService.GetDimOptimalArea(possibleDimAreaList, allIsolateLine, alreadyDimArea);

                    toDimInfo(dimArea, possibleDimArea, out var dir, out var dist);

                    var dim = toDim(baseLineDict[possibleDimArea[dimArea]], dir, dist, pts[0]);

                    alreadyDimArea.Add(dimArea);

                    positionDim.AddRange(dim);


                    if (group.Key.Contains(ThDrainageSDCommon.tagIsland))
                    {
                        //岛独有的垂直标注
                        var orderPtsIsland = verticalForIslandInfo(possibleDimArea[dimArea], orderPts, baseLineDict[possibleDimArea[dimArea]], out dir, out dist);
                        var verticalDim = toDim(orderPtsIsland, dir, dist, orderPtsIsland.First());

                        positionDim.AddRange(verticalDim);
                    }
                }
            }

            return positionDim;
        }

        private static void toDimInfo(Polyline dimArea, Dictionary<Polyline, Line> possibleDimArea, out Vector3d dir, out double dist)
        {
            var dimLine = ThDrainageSDDimService.dimAreaCenterLine(dimArea);
            var baseLine = possibleDimArea[dimArea];

            var dirD = dimLine.StartPoint - baseLine.StartPoint;
            dist = dirD.Length;
            dir = dirD.GetNormal();

        }

        private static List<Point3d> verticalForIslandInfo(Line baseLine, List<Point3d> orderPts, List<Point3d> orderPtsDim, out Vector3d dir, out double dist)
        {
            var tol = new Tolerance(10, 10);
            var horiPts = new List<Point3d>();
            var dirD = new Vector3d();
            if (baseLine.StartPoint.IsEqualTo(orderPts.First(), tol))
            {
                horiPts.Add(orderPts.Last());
                horiPts.Add(orderPtsDim.Last());
                dirD = baseLine.EndPoint - orderPts.Last();
            }
            else
            {
                horiPts.Add(orderPts.First());
                horiPts.Add(orderPtsDim.First());
                dirD = baseLine.StartPoint - orderPts.First();
            }
            dist = dirD.Length;
            dir = dirD.GetNormal();

            return horiPts;

        }

        private static List<RotatedDimension> toDim(List<Point3d> orderPts, Vector3d dir, double dist, Point3d basePt)
        {
            var positionDim = new List<RotatedDimension>();

            var dimPt = basePt + dir * dist;
            double rotation = dir.GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);

            for (int i = 0; i < orderPts.Count - 1; i++)
            {
                var p1 = orderPts[i];
                var p2 = orderPts[i + 1];

                var dim = new RotatedDimension();
                dim.XLine1Point = p1;
                dim.XLine2Point = p2;
                dim.DimLinePoint = dimPt;
                dim.Rotation = rotation;

                positionDim.Add(dim);
            }

            return positionDim;
        }

    }
}
