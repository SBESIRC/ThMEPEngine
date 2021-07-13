using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDDimEngine
    {
        public static void positionDimTry(ThDrainageSDTreeNode root)

        {
            var p1 = root.Node;
            var p2 = root.Child.First().Child.First().Child.First().Node;
            var c = new Point3d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5, 0);

            //Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, adb.Database);

            var dim = new RotatedDimension();
            dim.XLine1Point = p1;
            dim.XLine2Point = p2;
            dim.DimLinePoint = new Point3d(p1.X, c.Y, 0);
            dim.Rotation = 135 * Math.PI / 180;

            DrawUtils.ShowGeometry(dim, "l0dimTest", 3);
            DrawUtils.ShowGeometry(dim.DimLinePoint, "l0dimTest", 3, 25, 20);

            var dim2 = new RotatedDimension();
            dim2.XLine1Point = p1;
            dim2.XLine2Point = p2;
            dim2.DimLinePoint = new Point3d(p1.X, c.Y, 0);
            dim2.Rotation = 45 * Math.PI / 180;

            DrawUtils.ShowGeometry(dim2, "l0dimTest", 3);
            DrawUtils.ShowGeometry(dim2.DimLinePoint, "l0dimTest", 3, 25, 20);

        }

        public static List<RotatedDimension> getDim(ThDrainageSDDataExchange dataset)
        {
            var positionDim = new List<RotatedDimension>();

            if (dataset.TerminalList != null && dataset.TerminalList.Count > 0)
            {
                var groupList = dataset.GroupList;
                var alreadyDimArea = new List<Polyline>();

                var allIsolateLine = new List<Line>();
                var roomLine = dataset.roomList.SelectMany(x => x.wallList).ToList();
                var pipes = dataset.Pipes;
                //var toilateLine = dataset.TerminalList.Select(x => new Line(x.Boundary.GetPoint3dAt(0), x.Boundary.GetPoint3dAt(3))).ToList();
                allIsolateLine.AddRange(roomLine);
                allIsolateLine.AddRange(pipes);
                //allIsolateLine.AddRange(toilateLine);

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

                    var dimArea = ThDrainageSDDimService.getDimOptimalArea(possibleDimArea, allIsolateLine, alreadyDimArea);

                    toDimInfo(dimArea, possibleDimArea, out var dir, out var dist);

                    var dimPt = pts[0] + dir * dist;
                    double rotation = dir.GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);
                    var dim = toDim(baseLineDict[possibleDimArea[dimArea]], rotation, dimPt);

                    alreadyDimArea.Add(dimArea);

                    positionDim.AddRange(dim);


                    if (group.Key.Contains(ThDrainageSDCommon.tagIsland))
                    {
                        //岛
                        var orderPtsIsland = horizontalForIslandInfo(possibleDimArea[dimArea], orderPts, baseLineDict[possibleDimArea[dimArea]], out dir, out dist);
                        dimPt = orderPtsIsland.First() + dir * dist;
                        rotation = dir.GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);
                        var hotizontalDim = toDim(orderPtsIsland, rotation, dimPt);
                        positionDim.AddRange(hotizontalDim);

                        hotizontalDim.ForEach(x => DrawUtils.ShowGeometry(x, "l41Dim", 223));

                    }


                    dim.ForEach(x => DrawUtils.ShowGeometry(x, "l41Dim", 223));
                }
            }

            return positionDim;
        }

        //private static List<Point3d> toDimInfo(Polyline dimArea, Dictionary<Polyline, List<Point3d>> possibleDimArea, out Vector3d dir, out double dist)
        //{
        //    var tol = new Tolerance(10, 10);
        //    var dimLine = ThDrainageSDDimService.dimAreaCenterLine(dimArea);
        //    var orderPts = possibleDimArea[dimArea];

        //    dist = 0;
        //    dir = new Vector3d();

        //    var ptsStOnLine = dimLine.GetClosestPointTo(orderPts.First(), true);
        //    var ptsEdOnLine = dimLine.GetClosestPointTo(orderPts.Last(), true);

        //    if (ptsStOnLine.IsEqualTo(dimLine.StartPoint, tol))
        //    {
        //        dir = (dimLine.StartPoint - orderPts.First()).GetNormal();
        //        dist = dimLine.StartPoint.DistanceTo(orderPts.First());
        //        var pt = dimLine.EndPoint - dir * dist;

        //        orderPts.Add(pt);
        //    }
        //    else if (ptsStOnLine.IsEqualTo(dimLine.EndPoint, tol))
        //    {
        //        dir = (dimLine.EndPoint - orderPts.First()).GetNormal();
        //        dist = dimLine.EndPoint.DistanceTo(orderPts.First());
        //        var pt = dimLine.StartPoint - dir * dist;

        //        orderPts.Add(pt);
        //    }
        //    else if (ptsEdOnLine.IsEqualTo(dimLine.StartPoint, tol))
        //    {
        //        dir = (dimLine.StartPoint - orderPts.Last()).GetNormal();
        //        dist = dimLine.StartPoint.DistanceTo(orderPts.Last());
        //        var pt = dimLine.EndPoint - dir * dist;

        //        orderPts.Insert(0, pt);
        //    }
        //    else if (ptsEdOnLine.IsEqualTo(dimLine.EndPoint, tol))
        //    {
        //        dir = (dimLine.EndPoint - orderPts.Last()).GetNormal();
        //        dist = dimLine.EndPoint.DistanceTo(orderPts.Last());
        //        var pt = dimLine.StartPoint - dir * dist;

        //        orderPts.Insert(0, pt);
        //    }


        //    return orderPts;
        //}

        private static void toDimInfo(Polyline dimArea, Dictionary<Polyline, Line> possibleDimArea, out Vector3d dir, out double dist)
        {
            var dimLine = ThDrainageSDDimService.dimAreaCenterLine(dimArea);
            var baseLine = possibleDimArea[dimArea];

            var dirD = dimLine.StartPoint - baseLine.StartPoint;
            dist = dirD.Length;
            dir = dirD.GetNormal();

        }

        private static List<Point3d> horizontalForIslandInfo(Line baseLine, List<Point3d> orderPts,List<Point3d> orderPtsDim, out Vector3d dir, out double dist)
        {
            var tol = new Tolerance(10, 10);
            var horiPts = new List<Point3d>();
            var dirD = new Vector3d();
            if (baseLine.StartPoint.IsEqualTo(orderPts.First(), tol))
            {
                horiPts.Add(orderPts.Last());
                horiPts.Add(orderPtsDim.Last ()) ;
                dirD = baseLine.EndPoint - orderPts.Last();
            }
            else
            {
                horiPts.Add(orderPts.First());
                horiPts.Add(orderPtsDim .First());
                dirD = baseLine.StartPoint - orderPts.First();
            }

            dist = dirD.Length;
            dir = dirD.GetNormal();

            return horiPts;

        }

        //private static List<RotatedDimension> toDim(List<Point3d> orderPts, Vector3d dir, double dist, Point3d basePt)
        //{
        //    var positionDim = new List<RotatedDimension>();

        //    var dimPt = basePt + dir * dist;
        //    double rotation = dir.GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);

        //    for (int i = 0; i < orderPts.Count - 1; i++)
        //    {
        //        var p1 = orderPts[i];
        //        var p2 = orderPts[i + 1];

        //        var dim = new RotatedDimension();
        //        dim.XLine1Point = p1;
        //        dim.XLine2Point = p2;
        //        dim.DimLinePoint = dimPt;
        //        dim.Rotation = rotation;

        //        positionDim.Add(dim);


        //    }

        //    return positionDim;
        //}
        private static List<RotatedDimension> toDim(List<Point3d> orderPts, double rotation, Point3d dimPt)
        {
            var positionDim = new List<RotatedDimension>();

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
