using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class ConnectSingleSideService
    {

        /// <summary>
        /// not useful now, just save it
        /// </summary>
        /// <param name=""></param>
        public static void moveLane(List<List<Line>> mergedOrderedLane, List<BlockReference> lightPt)
        {

            var LaneList = LaneToPolyline(mergedOrderedLane);

            for (int i = 0; i < LaneList.Count; i++)
            {

                var blockByLane = separateBlocksByLine(LaneList[i], lightPt, EmgConnectCommon.TolGroupBlkLane);

                var displacementValue = getLaneDisplacement(LaneList[i], blockByLane[0]);
                if (displacementValue != 0)
                {
                    //GetOffsetCurves 负值：左 正值：右
                    var movedline = LaneList[i].GetOffsetCurves(-displacementValue)[0] as Polyline;
                    DrawUtils.ShowGeometry(movedline, EmgConnectCommon.LayerMovedLane, Color.FromColorIndex(ColorMethod.ByColor, 10));
                }

                displacementValue = getLaneDisplacement(LaneList[i], blockByLane[1]);
                if (displacementValue != 0)
                {
                    var movedline = LaneList[i].GetOffsetCurves(displacementValue)[0] as Polyline;
                    DrawUtils.ShowGeometry(movedline, EmgConnectCommon.LayerMovedLane, Color.FromColorIndex(ColorMethod.ByColor, 50));
                }
            }
        }

        private static List<Polyline> LaneToPolyline(List<List<Line>> mergedOrderedLane)
        {

            var lanePolyList = new List<Polyline>();

            for (var i = 0; i < mergedOrderedLane.Count; i++)
            {
                var lanePoly = new Polyline();
                lanePoly.AddVertexAt(0, mergedOrderedLane[i][0].StartPoint.ToPoint2D(), 0, 0, 0);
                foreach (var Lane in mergedOrderedLane[i])
                {
                    lanePoly.AddVertexAt(lanePoly.NumberOfVertices, Lane.EndPoint.ToPoint2D(), 0, 0, 0);
                }
                lanePolyList.Add(lanePoly);
            }

            return lanePolyList;

        }

        private static double getLaneDisplacement(Polyline lanes, List<Point3d> blocks)
        {
            var displacementList = blocks.Select(x => lanes.GetDistToPoint(x, false)).ToList();

            var distance = displacementList
             .OrderBy(x => x)
             .GroupBy(x => Math.Floor(x / 10))
             .OrderByDescending(x => x.Count())
             .First()
             .ToList()
             .First();

            return distance;
        }

        private static List<List<Point3d>> separateBlocksByLine(Polyline lane, List<BlockReference> blocks, double tol)
        {
            //SingleSidedBuffer 正值：左 负值：右
            var linePoly = new DBObjectCollection() { lane }.SingleSidedBuffer(tol).Cast<Polyline>().First();
            DrawUtils.ShowGeometry(linePoly, EmgConnectCommon.LayerLaneSape, Color.FromColorIndex(ColorMethod.ByColor, 130));
            var leftPolyline = blocks.Where(y =>
            {
                var bContain = linePoly.Contains(y.Position);
                var prjPt = lane.GetClosestPointTo(y.Position, false);
                var compareDir = (prjPt - y.Position).GetNormal();
                var bAngle = Math.Abs(compareDir.DotProduct(getDirectionBlock(y))) / (compareDir.Length * getDirectionBlock(y).Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                return bContain && bAngle;

            }).Select(x => x.Position).ToList();


            linePoly = new DBObjectCollection() { lane }.SingleSidedBuffer(-tol).Cast<Polyline>().First();
            DrawUtils.ShowGeometry(linePoly, EmgConnectCommon.LayerLaneSape, Color.FromColorIndex(ColorMethod.ByColor, 220));
            var rightPolyline = blocks.Where(y =>
            {
                var bContain = linePoly.Contains(y.Position);
                var prjPt = lane.GetClosestPointTo(y.Position, false);
                var compareDir = (prjPt - y.Position).GetNormal();
                var bAngle = Math.Abs(compareDir.DotProduct(getDirectionBlock(y))) / (compareDir.Length * getDirectionBlock(y).Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                return bContain && bAngle;

            }).Select(x => x.Position).ToList();


            var usefulStruct = new List<List<Point3d>>() { leftPolyline, rightPolyline };

            return usefulStruct;
        }

        private static Vector3d getDirectionBlock(BlockReference block)
        {
            //may has bug, make sure the UCS coordinate is coorect. may be changed to use blockReference matrix(????
            var dir = Vector3d.YAxis.RotateBy(block.Rotation, Vector3d.ZAxis).GetNormal();

            return dir;
        }

        public static void forDebugSingleSideBlocks(List<ThSingleSideBlocks> blockGroup)
        {
            var listMain = new List<Polyline>();
            var listSec = new List<Polyline>();
            var listAddM = new List<Line>();
            var allMain = blockGroup.SelectMany(x => x.getTotalMainBlock()).ToList(); //所有主块

            foreach (var side in blockGroup)
            {
                var mainLine = new Polyline();
                if (side.mainBlk.Count > 0)
                {
                    side.mainBlk.ForEach(x => mainLine.AddVertexAt(mainLine.NumberOfVertices, x.ToPoint2D(), 0, 0, 0));

                    if (side.mainBlk.Count == 1)
                    {
                        side.mainBlk.ForEach(x => mainLine.AddVertexAt(mainLine.NumberOfVertices, new Point2d(x.ToPoint2D().X + 2000, x.ToPoint2D().Y), 0, 0, 0));
                    }
                }


                var secLine = createSecLink(side, allMain);
                var addMainLine = createAddMainLink(side);

                side.groupBlock.ForEach(x => DrawUtils.ShowGeometry(x.Value, EmgConnectCommon.LayerGroupConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 130)));

                listMain.Add(mainLine);
                listSec.AddRange(secLine);
                //listAddM.AddRange(side.addMainBlkLine);
                listAddM.AddRange(addMainLine);
            }

            DrawUtils.ShowGeometry(listMain, EmgConnectCommon.LayerGroupConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 130));
            DrawUtils.ShowGeometry(listSec, EmgConnectCommon.LayerGroupConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 130));
            DrawUtils.ShowGeometry(listAddM, EmgConnectCommon.LayerGroupConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 130));

        }

        public static void forDebugSingleSideBlocks2(List<ThSingleSideBlocks> sides)
        {
            var connectMainLine = new List<Line>();
            var connectSecLine = new List<Line>();

            sides.ForEach(side =>
               {
                   side.ptLink.ForEach(pt =>
                   {
                       if (side.reSecBlk.Contains(pt.Item1) || side.reSecBlk.Contains(pt.Item2))
                       {
                           connectSecLine.Add(new Line(pt.Item1, pt.Item2));
                       }
                       else
                       {
                           connectMainLine.Add(new Line(pt.Item1, pt.Item2));
                       }
                   });
               });

            DrawUtils.ShowGeometry(connectMainLine, EmgConnectCommon.LayerGroupConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 130));
            DrawUtils.ShowGeometry(connectSecLine, EmgConnectCommon.LayerGroupConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 70));

        }

        private static List<Polyline> createSecLink(ThSingleSideBlocks side, List<Point3d> allMain)
        {
            var lineList = new List<Polyline>();

            List<Point3d> tempMain = null;
            if (side.getTotalMainBlock().Count > 0)
            {
                tempMain = side.getTotalMainBlock();
            }
            else
            {
                tempMain = allMain;
            }

            foreach (var secBlk in side.secBlk)
            {
                var sLine = new Polyline();
                var mainBlk = tempMain.Where(x => x.DistanceTo(secBlk) == tempMain.Select(dist => dist.DistanceTo(secBlk)).Min()).First();

                sLine.AddVertexAt(sLine.NumberOfVertices, secBlk.ToPoint2D(), 0, 0, 0);
                sLine.AddVertexAt(sLine.NumberOfVertices, mainBlk.ToPoint2D(), 0, 0, 0);

                lineList.Add(sLine);
            }
            return lineList;
        }

        private static List<Line> createAddMainLink(ThSingleSideBlocks side)
        {
            var lineList = new List<Line>();
            var addMLink = new Point3d();

            var mainBlkTrans = side.mainBlk.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(item => item.Value.X).ToList();
            var addMBlkTrans = side.addMainBlock.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(item => item.Value.X).ToList();

            for (int i = 0; i < addMBlkTrans.Count; i++)
            {
                if (i == 0)
                {
                    addMLink = side.mainBlk.OrderBy(dist => dist.DistanceTo(addMBlkTrans[i].Key)).First();
                }
                else
                {
                    var distM = side.mainBlk.Select(dist => dist.DistanceTo(addMBlkTrans[i].Key)).Min();

                    var distAM = addMBlkTrans[i].Key.DistanceTo(addMBlkTrans[i - 1].Key);

                    if (distM <= distAM)
                    {
                        addMLink = side.mainBlk.OrderBy(dist => dist.DistanceTo(addMBlkTrans[i].Key)).First();
                    }
                    else
                    {
                        addMLink = addMBlkTrans[i - 1].Key;

                    }
                }

                var sLine = new Line(addMBlkTrans[i].Key, addMLink);
                lineList.Add(sLine);
            }

            return lineList;
        }

        public static void forDebugOptimalGroup(List<List<ThSingleSideBlocks>> OptimalGroupBlocks)
        {
            List<Polyline> groupLine = new List<Polyline>();

            foreach (var group in OptimalGroupBlocks)
            {
                var mLine = new Polyline();
                var count = 0;
                foreach (var line in group)
                {
                    line.mainBlk.ForEach(x => mLine.AddVertexAt(mLine.NumberOfVertices, x.ToPoint2D(), 0, 0, 0));
                    line.secBlk.ForEach(x => mLine.AddVertexAt(mLine.NumberOfVertices, x.ToPoint2D(), 0, 0, 0));
                    line.addMainBlock.ForEach(x => mLine.AddVertexAt(mLine.NumberOfVertices, x.ToPoint2D(), 0, 0, 0));

                    line.groupBlock.ForEach(x => DrawUtils.ShowGeometry(x.Value, EmgConnectCommon.LayerSingleSideGroup, Color.FromColorIndex(ColorMethod.ByColor, 130)));


                    count = count + line.Count;
                }
                groupLine.Add(mLine);

                DrawUtils.ShowGeometry(mLine.StartPoint, count.ToString(), EmgConnectCommon.LayerSingleSideGroup, Color.FromColorIndex(ColorMethod.ByColor, 130), LineWeight.LineWeight030);

            }

            DrawUtils.ShowGeometry(groupLine, EmgConnectCommon.LayerSingleSideGroup, Color.FromColorIndex(ColorMethod.ByColor, 130));

        }

        public static void forDebugConnectLine(List<(Point3d, Point3d)> connectList)
        {
            var connectLine = new List<Line>();
            foreach (var connectPair in connectList)
            {
                connectLine.Add(new Line(connectPair.Item1, connectPair.Item2));

            }

            DrawUtils.ShowGeometry(connectLine, EmgConnectCommon.LayerGroupConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 30));


        }

    }

}
