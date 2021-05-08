using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Common;


namespace ThMEPLighting.EmgLightConnect.Service
{
    class SingleSideBlockService
    {
        /// <summary>
        /// 应急灯疏散灯打组。之后可能用的到
        /// </summary>
        /// <param name="emgLightList"></param>
        /// <param name="evacList"></param>
        /// <returns></returns>
        private static void GroupEmgLightEvac(List<BlockReference> emgLightList, List<BlockReference> evacList, out List<BlockReference> mainPt, out Dictionary<Point3d, Point3d> groupPt)
        {
            //mainPt:如果应急指示灯和疏散灯一组，则存疏散灯坐标.否则存应急灯
            //groupPt:如果应急指示灯和疏散灯一组，则存应急指示灯

            mainPt = new List<BlockReference>();
            groupPt = new Dictionary<Point3d, Point3d>();

            if (evacList.Count > 0 && emgLightList.Count > 0)
            {


                double groupDist = -1;
                double scaleEvac = evacList[0].ScaleFactors.X;
                double scaleEmg = emgLightList[0].ScaleFactors.X;
                groupDist = scaleEvac * 1.25 + scaleEmg * 2.25 + EmgConnectCommon.TolGroupEmgLightEvac;

                //for debug
                //groupDist = 400;

                foreach (var emgPt in emgLightList)
                {
                    //bug:不要用固定值
                    var evac = evacList.Where(e => e.Position.DistanceTo(emgPt.Position) <= groupDist).ToList();

                    if (evac.Count > 0)
                    {
                        mainPt.Add(evac.First());
                        groupPt.Add(evac.First().Position, emgPt.Position);
                        evacList.Remove(evac.First());

                    }
                    else
                    {
                        mainPt.Add(emgPt);
                    }

                }
            }
            mainPt.AddRange(evacList);

        }

        private static Vector3d getDirectionBlock(BlockReference block)
        {
            //may has bug, make sure the UCS coordinate is coorect. may be changed to use blockReference matrix(????
            var dir = Vector3d.YAxis.RotateBy(block.Rotation, Vector3d.ZAxis).GetNormal();

            return dir;
        }

        public static List<ThSingleSideBlocks> groupSingleSideBlocks(List<List<Line>> mergedOrderedLane, Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockList, Dictionary<Point3d, Point3d> emgEvacGroup)
        {
            List<ThSingleSideBlocks> singleSideBlocks = new List<ThSingleSideBlocks>();

            for (var i = 0; i < mergedOrderedLane.Count; i++)
            {
                for (var j = 0; j < mergedOrderedLane[i].Count; j++)
                {

                    var mainBlockGroup = separateMainBlocksByLine(mergedOrderedLane[i][j], blockList[EmgConnectCommon.BlockGroupType.mainBlock], EmgConnectCommon.TolGroupBlkLane, EmgConnectCommon.TolGroupBlkLaneHead);
                    var secBlockGroup = separateSecBlocksByLine(mergedOrderedLane[i][j], blockList[EmgConnectCommon.BlockGroupType.secBlock], EmgConnectCommon.TolGroupBlkLane, EmgConnectCommon.TolGroupBlkLaneHead);

                    var groupLeft = new ThSingleSideBlocks(mainBlockGroup[0], new List<(Line, int)> { (mergedOrderedLane[i][j], 0) });
                    var groupRight = new ThSingleSideBlocks(mainBlockGroup[1], new List<(Line, int)> { (mergedOrderedLane[i][j], 1) });

                    groupLeft.secBlk.AddRange(secBlockGroup[0]);
                    groupRight.secBlk.AddRange(secBlockGroup[1]);

                    var groupBlockGroupLeft = getEmgEvacGroup(mainBlockGroup[0], emgEvacGroup);
                    var groupBlockGroupRight = getEmgEvacGroup(mainBlockGroup[1], emgEvacGroup);

                    groupLeft.setGroupGroup(groupBlockGroupLeft);
                    groupRight.setGroupGroup(groupBlockGroupRight);

                    singleSideBlocks.Add(groupLeft);
                    singleSideBlocks.Add(groupRight);

                    RemoveBlockFromList(groupLeft, blockList);
                    RemoveBlockFromList(groupRight, blockList);
                }
            }

            return singleSideBlocks;
        }

        private static Dictionary<Point3d, Point3d> getEmgEvacGroup(List<Point3d> pt, Dictionary<Point3d, Point3d> emgEvacGroup)
        {
            var emgEvacDict = new Dictionary<Point3d, Point3d>();

            foreach (var p in pt)
            {
                foreach (var item in emgEvacGroup)
                {
                    if (item.Key.IsEqualTo(p, new Tolerance(1, 1)))
                    {
                        emgEvacDict.Add(item.Key, item.Value);
                        break;
                    }
                }
            }

            return emgEvacDict;
        }

        public static void restBlockToSingleSideBlocks(Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockList, List<ThSingleSideBlocks> singleSideBlocks, Dictionary<Point3d, Point3d> emgEvacGroup)
        {
            //有可能主要块放到不正确的组里
            foreach (var pair in blockList)
            {
                foreach (var block in pair.Value)
                {
                    var closePoint = singleSideBlocks.SelectMany(x => x.getTotalBlock()).OrderBy(y => y.DistanceTo(block.Position)).FirstOrDefault();
                    var group = singleSideBlocks.Where(x => x.getTotalBlock().Contains(closePoint)).FirstOrDefault();

                    group.secBlk.Add(block.Position);

                    if (pair.Key == EmgConnectCommon.BlockGroupType.mainBlock)
                    {
                        var groupPt = emgEvacGroup.Where(x => x.Key.IsEqualTo(block.Position, new Tolerance(1, 1))).ToList();
                        if (groupPt.Count > 0)
                        {
                            group.groupBlock.Add(groupPt[0].Key, groupPt[0].Value);
                        }
                    }
                }
            }

            singleSideBlocks.ForEach(x => RemoveBlockFromList(x, blockList));

        }

        private static void RemoveBlockFromList(ThSingleSideBlocks blockGroup, Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockList)
        {
            blockGroup.mainBlk.ForEach(x => blockList[EmgConnectCommon.BlockGroupType.mainBlock].RemoveAll(y => y.Position == x));
            blockGroup.mainBlk.ForEach(x => blockList[EmgConnectCommon.BlockGroupType.secBlock].RemoveAll(y => y.Position == x));

            blockGroup.secBlk.ForEach(x => blockList[EmgConnectCommon.BlockGroupType.mainBlock].RemoveAll(y => y.Position == x));
            blockGroup.secBlk.ForEach(x => blockList[EmgConnectCommon.BlockGroupType.secBlock].RemoveAll(y => y.Position == x));

        }

        private static List<List<Point3d>> separateMainBlocksByLine(Line lane, List<BlockReference> blocks, double tolLeftRight, double tolUpDown)
        {

            var linePoly = GeomUtils.ExpandLine(lane, tolLeftRight, tolUpDown, 0, tolUpDown);

            var leftPolyline = blocks.Where(y =>
            {
                var bContain = linePoly.Contains(y.Position);
                var prjPt = lane.GetClosestPointTo(y.Position, false);
                var compareDir = (prjPt - y.Position).GetNormal();
                var bAngle = Math.Abs(compareDir.DotProduct(getDirectionBlock(y))) / (compareDir.Length * getDirectionBlock(y).Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                return bContain && bAngle;

            }).Select(x => x.Position).ToList();


            linePoly = GeomUtils.ExpandLine(lane, 0, tolUpDown, tolLeftRight, tolUpDown);
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

        private static List<List<Point3d>> separateSecBlocksByLine(Line lane, List<BlockReference> blocks, double tolLeftRight, double tolUpDown)
        {

            var linePoly = GeomUtils.ExpandLine(lane, tolLeftRight, tolUpDown, 0, tolUpDown);

            var leftPolyline = blocks.Where(y =>
            {
                var bContain = linePoly.Contains(y.Position);
                return bContain;

            }).Select(x => x.Position).ToList();


            linePoly = GeomUtils.ExpandLine(lane, 0, tolUpDown, tolLeftRight, tolUpDown);
            var rightPolyline = blocks.Where(y =>
            {
                var bContain = linePoly.Contains(y.Position);
                return bContain;

            }).Select(x => x.Position).ToList();


            var usefulStruct = new List<List<Point3d>>() { leftPolyline, rightPolyline };

            return usefulStruct;
        }

        public static void addSecBlockList(Dictionary<EmgBlkType.BlockType, List<BlockReference>> blockSourceList, ref Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockDict)
        {
            var otherSecBlk = blockSourceList[EmgBlkType.BlockType.otherSecBlk];
            blockDict.Add(EmgConnectCommon.BlockGroupType.secBlock, otherSecBlk);
        }

        public static void addMainGroupBlockList(Dictionary<EmgBlkType.BlockType, List<BlockReference>> blockSourceList, ref Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockDict, out Dictionary<Point3d, Point3d> groupBlock)
        {
            GroupEmgLightEvac(blockSourceList[EmgBlkType.BlockType.emgLight], blockSourceList[EmgBlkType.BlockType.evac], out var mainPt, out groupBlock);
            blockDict.Add(EmgConnectCommon.BlockGroupType.mainBlock, mainPt);

        }

    }
}
