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
            //20220907:发现有图纸将紧急出口和应急指示灯一组，此时这两个灯不会成组在groupPt里，如果刚好一组只有一个点位，则后面会因为只有一个点位切不出偏移车道线而报错。
            //目前在后面硬加了“检测切不出来线当成当前组织有一个主点位处理”不报错的逻辑，且能连出线，单看图纸看不出来哪里有问题，但其实这里逻辑不太对。
            //需要后期加“应急指示灯和副图块也能成组但当做副图块看的”逻辑优化。

            mainPt = new List<BlockReference>();
            groupPt = new Dictionary<Point3d, Point3d>();

            if (evacList.Count > 0 || emgLightList.Count > 0)
            {
                double groupDist = -1;
                double scaleEvac = 100;
                double scaleEmg = 100;
                if (evacList.Count > 0)
                {
                    scaleEvac = Math.Abs(evacList[0].ScaleFactors.X);
                }
                if (emgLightList.Count > 0)
                {
                    scaleEmg = Math.Abs(emgLightList[0].ScaleFactors.X);
                }

                groupDist = scaleEvac * 1.25 + scaleEmg * 2.25 + EmgConnectCommon.TolGroupEmgLightEvac;

                foreach (var emgPt in emgLightList)
                {
                    bool bGroup = false;

                    var evac = evacList.Where(e => e.Position.DistanceTo(emgPt.Position) <= groupDist).ToList();
                    if (evac.Count > 0)
                    {
                        //check direction

                        var evacRA = evac.First().Rotation;
                        var evacR = Vector3d.YAxis.RotateBy(evacRA, Vector3d.ZAxis).GetNormal().GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);
                        var evacEmgR = (emgPt.Position - evac.First().Position).GetNormal().GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);

                        var CosAngle = Math.Cos(evacR - evacEmgR);

                        ////角度在20 以内
                        if (Math.Abs(CosAngle) > Math.Cos(20 * Math.PI / 180))
                        {

                            mainPt.Add(evac.First());
                            groupPt.Add(evac.First().Position, emgPt.Position);
                            evacList.Remove(evac.First());
                            bGroup = true;
                        }
                    }

                    if (bGroup == false)
                    {
                        mainPt.Add(emgPt);
                    }

                }
            }
            mainPt.AddRange(evacList);

        }

        private static Vector3d getDirectionBlock(BlockReference block)
        {
            //may have bug, make sure the UCS coordinate is coorect. may be need to change to use blockReference matrix(????
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

                    var sideLeft = new ThSingleSideBlocks(mainBlockGroup[0], new List<(Line, int)> { (mergedOrderedLane[i][j], 0) });
                    var sideRight = new ThSingleSideBlocks(mainBlockGroup[1], new List<(Line, int)> { (mergedOrderedLane[i][j], 1) });

                    sideLeft.secBlk.AddRange(secBlockGroup[0]);
                    sideRight.secBlk.AddRange(secBlockGroup[1]);

                    var sideLeftEmgGroup = getEmgEvacGroup(mainBlockGroup[0], emgEvacGroup);
                    var sideRightEmgGroup = getEmgEvacGroup(mainBlockGroup[1], emgEvacGroup);

                    sideLeft.setEmgGroup(sideLeftEmgGroup);
                    sideRight.setEmgGroup(sideRightEmgGroup);

                    singleSideBlocks.Add(sideLeft);
                    singleSideBlocks.Add(sideRight);

                    RemoveBlockFromList(sideLeft, blockList);
                    RemoveBlockFromList(sideRight, blockList);
                }
            }

            return singleSideBlocks;
        }

        public static List<ThSingleSideBlocks> classifyMainBlocks(List<List<Line>> mergedOrderedLane, Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockList, Dictionary<Point3d, Point3d> emgEvacGroup)
        {
            List<ThSingleSideBlocks> singleSideBlocks = new List<ThSingleSideBlocks>();

            for (var i = 0; i < mergedOrderedLane.Count; i++)
            {
                for (var j = 0; j < mergedOrderedLane[i].Count; j++)
                {

                    var mainBlockGroup = separateMainBlocksByLine(mergedOrderedLane[i][j], blockList[EmgConnectCommon.BlockGroupType.mainBlock], EmgConnectCommon.TolGroupBlkLane, EmgConnectCommon.TolGroupBlkLaneHead);
                    //var secBlockGroup = separateSecBlocksByLine(mergedOrderedLane[i][j], blockList[EmgConnectCommon.BlockGroupType.secBlock], EmgConnectCommon.TolGroupBlkLane, EmgConnectCommon.TolGroupBlkLaneHead);

                    var sideLeft = new ThSingleSideBlocks(mainBlockGroup[0], new List<(Line, int)> { (mergedOrderedLane[i][j], 0) });
                    var sideRight = new ThSingleSideBlocks(mainBlockGroup[1], new List<(Line, int)> { (mergedOrderedLane[i][j], 1) });

                    //sideLeft.secBlk.AddRange(secBlockGroup[0]);
                    //sideRight.secBlk.AddRange(secBlockGroup[1]);

                    var sideLeftEmgGroup = getEmgEvacGroup(mainBlockGroup[0], emgEvacGroup);
                    var sideRightEmgGroup = getEmgEvacGroup(mainBlockGroup[1], emgEvacGroup);

                    sideLeft.setEmgGroup(sideLeftEmgGroup);
                    sideRight.setEmgGroup(sideRightEmgGroup);

                    singleSideBlocks.Add(sideLeft);
                    singleSideBlocks.Add(sideRight);

                    RemoveBlockFromList(sideLeft, blockList);
                    RemoveBlockFromList(sideRight, blockList);
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
            var closeTol = 12000;

            var farBlock = blockList.SelectMany(x => x.Value).Select(x => x.Position).ToList();
            var farBlkLink = new Dictionary<int, List<int>>();
            var mergeBlock = new List<List<int>>();

            for (int i = 0; i < farBlock.Count; i++)
            {
                var closeI = farBlock.Where(x => x != farBlock[i] && x.DistanceTo(farBlock[i]) < closeTol).ToList()
                                        .Select(x => farBlock.IndexOf(x))
                                        .Where(x => x >= 0).ToList();

                farBlkLink.Add(i, closeI);
            }

            foreach (var blkLink in farBlkLink)
            {
                var already = mergeBlock.SelectMany(x => x);
                if (already.Contains(blkLink.Key) == false)
                {
                    var thisLink = new List<int>() { blkLink.Key };
                    getDescendant(farBlkLink[blkLink.Key], farBlkLink, thisLink);
                    mergeBlock.Add(thisLink);
                }
            }

            var allSide = singleSideBlocks.SelectMany(x => x.getTotalBlock()).ToList();

            foreach (var list in mergeBlock)
            {
                var listPts = list.Select(x => farBlock[x]).ToList();
                var closedDists = returnValueCalculation.getDistMatrix(listPts, allSide);
                var closedPtIndex = closedDists.OrderBy(x => x.Item3).FirstOrDefault();
                var closedSidePt = allSide[closedPtIndex.Item2];
                var group = singleSideBlocks.Where(x => x.getTotalBlock().Contains(closedSidePt)).FirstOrDefault();

                if (group != null)
                {
                    group.secBlk.AddRange(listPts);

                    foreach (var pt in listPts)
                    {
                        var groupPt = emgEvacGroup.Where(x => x.Key.IsEqualTo(pt, new Tolerance(1, 1))).ToList();
                        if (groupPt.Count > 0)
                        {
                            group.groupBlock.Add(groupPt[0].Key, groupPt[0].Value);
                        }
                    }
                }
            }
        }

        private static void getDescendant(List<int> Child, Dictionary<int, List<int>> farBlkLink, List<int> thisLink)
        {
            var nextChild = Child.Where(x => thisLink.Contains(x) == false).ToList();
            if (nextChild.Count != 0)
            {
                thisLink.AddRange(nextChild);
                foreach (var c in nextChild)
                {
                    getDescendant(farBlkLink[c], farBlkLink, thisLink);
                }
            }
        }


        //public static void restBlockToSingleSideBlocks(Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockList, List<ThSingleSideBlocks> singleSideBlocks, Dictionary<Point3d, Point3d> emgEvacGroup)
        //{
        //    //有可能主要块放到不正确的组里
        //    foreach (var pair in blockList)
        //    {
        //        foreach (var block in pair.Value)
        //        {
        //            var closePoint = singleSideBlocks.SelectMany(x => x.getTotalBlock()).OrderBy(y => y.DistanceTo(block.Position)).FirstOrDefault();
        //            var group = singleSideBlocks.Where(x => x.getTotalBlock().Contains(closePoint)).FirstOrDefault();
        //            if (group != null)
        //            {
        //                group.secBlk.Add(block.Position);

        //                if (pair.Key == EmgConnectCommon.BlockGroupType.mainBlock)
        //                {
        //                    var groupPt = emgEvacGroup.Where(x => x.Key.IsEqualTo(block.Position, new Tolerance(1, 1))).ToList();
        //                    if (groupPt.Count > 0)
        //                    {
        //                        group.groupBlock.Add(groupPt[0].Key, groupPt[0].Value);
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    singleSideBlocks.ForEach(x => RemoveBlockFromList(x, blockList));

        //}



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


        //public static void classifySecBlocks(List<ThSingleSideBlocks> singleSideBlocks, Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>> blockList, Point3d ALE)
        //{

        //    var allMainPt = singleSideBlocks.SelectMany(x => x.mainBlk).ToList();


        //    foreach (var block in blockList[EmgConnectCommon.BlockGroupType.secBlock])
        //    {
        //        var secPt = block.Position;

        //        var closePoint = new Point3d();
        //        var closePointList = allMainPt.Where(x => x.DistanceTo(secPt) < EmgConnectCommon.TolSaperateGroupMaxDistance).ToList();

        //        if (closePointList.Count > 0)
        //        {
        //            var mainDist = closePointList.Select(x => (closePointList.IndexOf(x), 0, x.DistanceTo(secPt))).ToList();

        //            Dictionary<int, double> returnValueDict = returnValueCalculation.getReturnValueClassify(ALE, closePointList, secPt);//key:blockListIndex value:returnValue

        //            closePoint = returnValueCalculation.findOptimalConnectionClassifySec(returnValueDict, mainDist, closePointList);

        //        }
        //        else
        //        {
        //            closePoint = allMainPt.ToDictionary(x => x, x => x.DistanceTo(secPt)).OrderBy(x => x.Value).First().Key;
        //        }



        //        var group = singleSideBlocks.Where(x => x.getTotalBlock().Contains(closePoint)).FirstOrDefault();
        //        group.secBlk.Add(secPt);
        //    }

        //    singleSideBlocks.ForEach(x => RemoveBlockFromList(x, blockList));

        //}
    }
}
