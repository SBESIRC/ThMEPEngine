using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.DrainageSystemDiagram.Service
{
    public class ThDrainageSDDimService
    {
        public static Dictionary<Line, List<Point3d>> getDimAreaBaseLine(List<ThTerminalToilet> toiletInGroup, List<ThToiletRoom> roomList, List<Point3d> orderPts)
        {

            Dictionary<Line, List<Point3d>> baseLine = new Dictionary<Line, List<Point3d>>();

            //普通组，小房间细分组，单各组
            var wall = ThDrainageSDCoolPtProcessService.findToiletToWall(toiletInGroup[0], roomList);

            if (wall != null)
            {
                baseLine = getDimAreaBaseLine(orderPts, wall);
            }

            return baseLine;

        }

        public static Dictionary<Line, List<Point3d>> getDimAreaBaseLineIsland(List<ThTerminalToilet> toiletInGroup, List<ThToiletRoom> roomList, List<Point3d> orderPts)
        {
            //岛
            var baseLine = new Dictionary<Line, List<Point3d>>();

            var room = ThDrainageSDCoolPtProcessService.findRoomToiletBelongsTo(toiletInGroup.First(), roomList);
            var wallPt = room.outlinePtList;
            var dir = toiletInGroup.First().Dir;
            var tempLine = new Line(orderPts.Last(), orderPts.First());
            var tol = new Tolerance(10, 10);

            var wallPtInSide = wallPt.Distinct().Where(x =>
              {
                  var b = false;
                  var wallDir = x - orderPts.First();
                  if (Math.Cos(wallDir.GetAngleTo(dir, Vector3d.ZAxis)) > 0)
                  {
                      b = true;
                  }
                  return b;
              });

            var wallOnTempLine = wallPtInSide.ToDictionary(x => x, x => tempLine.GetClosestPointTo(x, true));
            var ptNotOnTempLine = wallOnTempLine.Where(x => tempLine.ToCurve3d().IsOn(x.Value, tol) == false).ToDictionary(x => x.Key, x => x.Value);

            var edgePtToFirst = ptNotOnTempLine.OrderBy(x => x.Key.DistanceTo(orderPts.First())).First();
            var edgePtToLast = ptNotOnTempLine.OrderBy(x => x.Key.DistanceTo(orderPts.Last())).First();

            var baselineCreateFirst = true;
            var baseLineCreateLast = true;
            if (edgePtToFirst.Key.IsEqualTo(edgePtToLast.Key, tol))
            {
                if (edgePtToFirst.Key.DistanceTo(orderPts.First()) <= edgePtToFirst.Key.DistanceTo(orderPts.Last()))
                {
                    baseLineCreateLast = false;
                }
                else
                {
                    baselineCreateFirst = false;
                }

            }

            if (baselineCreateFirst == true)
            {
                var line = new Line(edgePtToFirst.Value, orderPts.Last());
                var orderPtsFirst = new List<Point3d>();
                orderPtsFirst.Add(edgePtToFirst.Key);
                orderPtsFirst.AddRange(orderPts);
                baseLine.Add(line, orderPtsFirst);
            }

            if (baseLineCreateLast == true)
            {
                var line2 = new Line(orderPts.First(), edgePtToLast.Value);
                var orderPtsLast = new List<Point3d>();
                orderPtsLast.AddRange(orderPts);
                orderPtsLast.Add(edgePtToLast.Key);
                baseLine.Add(line2, orderPtsLast);
            }

            return baseLine;

        }

        private static Dictionary<Line, List<Point3d>> getDimAreaBaseLine(List<Point3d> orderPts, Line edge)
        {
            var tol = new Tolerance(10, 10);
            var baseLine = new Dictionary<Line, List<Point3d>>();

            var edgePtToFirst = new Point3d();
            var edgePtToLast = new Point3d();

            if (orderPts.Count > 1)
            {
                var allPts = new List<Point3d>();

                allPts.AddRange(orderPts);
                allPts.Add(edge.StartPoint);
                allPts.Add(edge.EndPoint);

                var matrix = ThDrainageSDCommonService.getGroupMatrix(orderPts);

                var allOrderPts = allPts.OrderBy(x => x.TransformBy(matrix.Inverse()).X).ToList();

                edgePtToFirst = allOrderPts.First();
                edgePtToLast = allOrderPts.Last();

            }
            else
            {
                edgePtToFirst = edge.StartPoint;
                edgePtToLast = edge.EndPoint;
            }


            var line = new Line(edgePtToFirst, orderPts.Last());
            var line2 = new Line(orderPts.First(), edgePtToLast);

            var orderPtsFirst = new List<Point3d>();
            var orderPtsLast = new List<Point3d>();

            orderPtsFirst.Add(edgePtToFirst);
            orderPtsFirst.AddRange(orderPts);

            orderPtsLast.AddRange(orderPts);
            orderPtsLast.Add(edgePtToLast);

            baseLine.Add(line, orderPtsFirst);
            baseLine.Add(line2, orderPtsLast);

            return baseLine;
        }

        public static Dictionary<Polyline, Line> getPossibleDimArea(Dictionary<Line, List<Point3d>> baseLine, string groupName, List<ThTerminalToilet> toiletInGroup)
        {
            var possibleDimArea = new Dictionary<Polyline, Line>();
            var dir = toiletInGroup.First().Dir;

            if (groupName.Contains(ThDrainageSDCommon.tagIsland) == false)
            {
                //普通组两边都有,墙外侧
                var polyOuterBase1 = getDimArea(baseLine.First().Key, -ThDrainageSDCommon.MoveDistDimOutter, ThDrainageSDCommon.DimWidth, dir);
                var polyOuterBase2 = getDimArea(baseLine.Last().Key, -ThDrainageSDCommon.MoveDistDimOutter, ThDrainageSDCommon.DimWidth, dir);
                possibleDimArea.Add(polyOuterBase1, baseLine.First().Key);
                possibleDimArea.Add(polyOuterBase2, baseLine.Last().Key);
            }

            var moveDist = getInnerMoveDist(toiletInGroup, baseLine.First().Key);

            int i = 0;
            while (i < 3)
            {
                var polyInnerBase1 = getDimArea(baseLine.First().Key, moveDist, ThDrainageSDCommon.DimWidth, dir);
                var polyInnerBase2 = getDimArea(baseLine.Last().Key, moveDist, ThDrainageSDCommon.DimWidth, dir);

                possibleDimArea.Add(polyInnerBase1, baseLine.First().Key);
                possibleDimArea.Add(polyInnerBase2, baseLine.Last().Key);

                if (groupName.Contains(ThDrainageSDCommon.tagIsland) == false)
                {
                    moveDist = moveDist + ThDrainageSDCommon.MoveDistDimInner;
                    i = i + 1;
                }
                else
                {
                    i = 3;
                }
            }

            possibleDimArea.ForEach(x => DrawUtils.ShowGeometry(x.Key, "l40DimArea", 135));

            return possibleDimArea;
        }

        private static Polyline getDimArea(Line baseline, double moveDist, double dimWidth, Vector3d dir)
        {
            var pl = new Polyline();
            var pt1 = baseline.StartPoint + dir * (moveDist - dimWidth / 2);
            var pt2 = baseline.EndPoint + dir * (moveDist - dimWidth / 2);
            var pt3 = baseline.EndPoint + dir * (moveDist + dimWidth / 2);
            var pt4 = baseline.StartPoint + dir * (moveDist + dimWidth / 2);

            pl.AddVertexAt(pl.NumberOfVertices, pt1.ToPoint2d(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, pt2.ToPoint2d(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, pt3.ToPoint2d(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, pt4.ToPoint2d(), 0, 0, 0);

            pl.Closed = true;

            return pl;

        }

        private static double getInnerMoveDist(List<ThTerminalToilet> toiletInGroup, Line baseline)
        {
            double dist = ThDrainageSDCommon.LengthSublink * 2;
            double maxY = (toiletInGroup.First().Boundary.GetPoint3dAt(0) - toiletInGroup.First().Boundary.GetPoint3dAt(1)).Length;

            var pts = toiletInGroup.Select(x => x.Boundary.GetPoint3dAt(0)).ToList();

            var ptsMatrix = new List<Point3d>() { baseline.StartPoint, baseline.EndPoint };

            var matrix = ThDrainageSDCommonService.getGroupMatrix(ptsMatrix);
            var ptsDict = pts.ToDictionary(x => x, x => x.TransformBy(matrix.Inverse()));

            maxY = ptsDict.OrderByDescending(x => Math.Abs(x.Value.Y)).First().Value.Y;

            dist = Math.Abs(maxY) + ThDrainageSDCommon.MoveDistDimInner;

            return dist;
        }

        public static Polyline GetDimOptimalArea(List<Polyline> dimAreaList, List<Line> allIsolateLine, List<Polyline> alreadyDimArea)
        {
            var alreadyDimAreaLine = new List<Line>();

            if (alreadyDimArea.Count > 0)
            {
                alreadyDimAreaLine = ThDrainageSDCommonService.GetLines(alreadyDimArea.Last());
            }

            allIsolateLine.AddRange(alreadyDimAreaLine);

            var allIsoObjs = allIsolateLine.ToCollection();
            var allIsoSpatialIndex = new ThCADCoreNTSSpatialIndex(allIsoObjs);

            var dimAreaDict = new Dictionary<Polyline, double>();
            foreach (var area in dimAreaList)
            {
                var result = allIsoSpatialIndex.SelectCrossingPolygon(area);
                double distIso = 0;

                if (result != null && result.Count > 0)
                {
                    var resultList = result.Cast<Line>().ToList();
                    var isoInArea = new List<Curve>();

                    resultList.ForEach(re =>
                    {
                        foreach (var entity in area.Trim(re))
                        {
                            if (entity is Curve pl)
                            {
                                isoInArea.Add(pl);
                            }
                        }
                    });

                    distIso = isoInArea.Sum(x => x.GetLength());
                }
                dimAreaDict.Add(area, distIso);
            }

            dimAreaDict = dimAreaDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            //只在大样图定位标注有用。找area小的（距离墙近的)。轴测图这行其实area都一样。
            var optimalAreaList = dimAreaDict.Where(x => Math.Abs(x.Value - dimAreaDict.First().Value) <= ThDrainageSDCommon.DimWidth).ToDictionary(x => x.Key, x => x.Value);

            var optimal = optimalAreaList.OrderBy(x => x.Value).ThenBy(x => x.Key.Area).First();

            return optimal.Key;
        }


        public static Line dimAreaCenterLine(Polyline pl)
        {
            var pt1 = new Point3d((pl.GetPoint3dAt(0).X + pl.GetPoint3dAt(3).X) / 2, (pl.GetPoint3dAt(0).Y + pl.GetPoint3dAt(3).Y) / 2, 0);
            var pt2 = new Point3d((pl.GetPoint3dAt(1).X + pl.GetPoint3dAt(2).X) / 2, (pl.GetPoint3dAt(1).Y + pl.GetPoint3dAt(2).Y) / 2, 0);
            return new Line(pt1, pt2);
        }
    }
}
