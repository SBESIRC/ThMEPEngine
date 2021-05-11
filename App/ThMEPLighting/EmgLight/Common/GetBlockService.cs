using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.EmgLight.Common
{
    public class GetBlockService
    {
        public Dictionary<BlockReference, BlockReference> emgLight = new Dictionary<BlockReference, BlockReference>();
        public Dictionary<BlockReference, BlockReference> evacR = new Dictionary<BlockReference, BlockReference>();
        public Dictionary<BlockReference, BlockReference> evacRL = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitE = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitS = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitECeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitSCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> enter = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> enterCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacR2Ceiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacLR2Ceiling = new Dictionary<BlockReference, BlockReference>();

        Dictionary<BlockReference, BlockReference> enterN = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> enterNCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> floor = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> floorCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> floorEvac = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> floorEvacCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacUpCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacPost = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacSq = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacSqD = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacCir = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacCirD = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> refuge = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> refugeCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> refugeE = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> refugeECeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacLR = new Dictionary<BlockReference, BlockReference>();

        public void getBlocksData(Polyline bufferFrame, ThMEPOriginTransformer transformer, List<Polyline> holes = null)
        {
            emgLight = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EmgLightBlockName, transformer, holes);
            evacR = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacRBlockName, transformer, holes);
            evacRL = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLRBlockName, transformer, holes);

            exitE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitEBlockName, transformer, holes);
            exitS = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSBlockName, transformer, holes);
            exitECeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitECeilingBlockName, transformer, holes);
            exitSCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSCeilingBlockName, transformer, holes);

            enter = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterBlockName, transformer, holes);
            enterCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterCeilingBlockName, transformer, holes);

            evacCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacCeilingBlockName, transformer, holes);
            evacR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacR2LineCeilingBlockName, transformer, holes);
            evacLR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLR2LineCeilingBlockName, transformer, holes);


            emgLight = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EmgLightBlockName, transformer, holes);
            evacR = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacRBlockName, transformer, holes);
            evacRL = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLRBlockName, transformer, holes);

            exitE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitEBlockName, transformer, holes);
            exitS = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSBlockName, transformer, holes);
            exitECeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitECeilingBlockName, transformer, holes);
            exitSCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSCeilingBlockName, transformer, holes);

            enter = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterBlockName, transformer, holes);
            enterCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterCeilingBlockName, transformer, holes);

            evacCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacCeilingBlockName, transformer, holes);
            evacR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacR2LineCeilingBlockName, transformer, holes);
            evacLR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLR2LineCeilingBlockName, transformer, holes);

            enterN = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterNBlockName, transformer, holes);
            enterNCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterNCeilingBlockName, transformer, holes);
            floor = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.FloorBlockName, transformer, holes);
            floorCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.FloorCeilingBlockName, transformer, holes);
            floorEvac = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.FloorEvacBlockName, transformer, holes);
            floorEvacCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.FloorEvacCeilingBlockName, transformer, holes);
            evacUpCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacUpCeilingBlockName, transformer, holes);
            evacPost = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacPostBlockName, transformer, holes);
            evacSq = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacSqBlockName, transformer, holes);
            evacSqD = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacSqDBlockName, transformer, holes);
            evacCir = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacCirBlockName, transformer, holes);
            evacCirD = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacCirDBlockName, transformer, holes);
            refuge = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.RefugeBlockName, transformer, holes);
            refugeCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.RefugeCeilingBlockName, transformer, holes);
            refugeE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.RefugeEBlockName, transformer, holes);
            refugeECeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.RefugeECeilingBlockName, transformer, holes);
            evacLR = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLRCeilingBlockName, transformer, holes);

            //var ALE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EPowerLayerName, ThMEPLightingCommon.ALEBlockName, transformer);

        }

        public void getBlockList(Dictionary<EmgBlkType.BlockType, List<BlockReference>> blockList)
        {
            blockList.Add(EmgBlkType.BlockType.emgLight, emgLight.Select(x => x.Value).ToList());
            blockList.Add(EmgBlkType.BlockType.evac, evacR.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.evac].AddRange(evacRL.Select(x => x.Value).ToList());

            blockList.Add(EmgBlkType.BlockType.otherSecBlk, exitE.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(exitS.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(exitECeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(exitSCeiling.Select(x => x.Value).ToList());

            //blockList.Add(EmgBlkType.BlockType.otherSecBlk, enter.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(enter.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(enterCeiling.Select(x => x.Value).ToList());

            //blockList.Add(EmgBlkType.BlockType.otherSecBlk, evacCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacR2Ceiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacLR2Ceiling.Select(x => x.Value).ToList());


            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(enterN.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(enterNCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(floor.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(floorCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(floorEvac.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(floorEvacCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacUpCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacPost.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacSq.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacSqD.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacCir.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacCirD.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(refuge.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(refugeCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(refugeE.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(refugeECeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.otherSecBlk].AddRange(evacLR.Select(x => x.Value).ToList());

            //blockList.Add(EmgBlkType.BlockType.ale, ALE.Select(x => x.Value).ToList());

        }

        public static Dictionary<string, List<Point3d>> blkConnectDict()
        {
            Dictionary<string, List<Point3d>> blkConnectDict = new Dictionary<string, List<Point3d>>();//左，下，右，上

            Point3d rightTopPt1 = new Point3d(2.5, 1.25, 0);//普通出口

            List<Point3d> type2 = new List<Point3d> { new Point3d(-2.5, 0, 0), new Point3d(0, -1.25, 0), new Point3d(2.5, 0, 0), new Point3d(0, 2.5, 0) }; //普通吊装
            List<Point3d> type3 = new List<Point3d> { new Point3d(-1.25, 2.5, 0), new Point3d(0, 0, 0), new Point3d(1.25, 2.5, 0), new Point3d(0, 6.25, 0) };//落地灯柱

            Point3d rightTopPt2 = new Point3d(2, 2, 0);//埋地

            blkConnectDict.Add("E-BFEL810", new List<Point3d> { new Point3d (-1.25,0,0),
                                                                new Point3d (0,-2.25,0),
                                                                new Point3d (1.25,0,0),
                                                                new Point3d (0,1.25,0)});

            addBlkConnectDict(blkConnectDict, "E-BFEL200", rightTopPt1);
            addBlkConnectDict(blkConnectDict, "E-BFEL210", rightTopPt1);
            addBlkConnectDict(blkConnectDict, "E-BFEL100", rightTopPt1);
            addBlkConnectDict(blkConnectDict, "E-BFEL102", rightTopPt1);
            blkConnectDict.Add("E-BFEL101", type2);
            blkConnectDict.Add("E-BFEL103", type2);
            addBlkConnectDict(blkConnectDict, "E-BFEL140", rightTopPt1);
            blkConnectDict.Add("E-BFEL141", type2);
            blkConnectDict.Add("E-BFEL201", type2);
            blkConnectDict.Add("E-BFEL201-1", type2);
            blkConnectDict.Add("E-BFEL211-1", type2);

            addBlkConnectDict(blkConnectDict, "E-BFEL130", rightTopPt1);
            blkConnectDict.Add("E-BFEL131", type2);
            addBlkConnectDict(blkConnectDict, "E-BFEL110", rightTopPt1);
            blkConnectDict.Add("E-BFEL111", type2);
            blkConnectDict.Add("E-BFEL161", type2);
            blkConnectDict.Add("E-BFEL161-1", type2);
            blkConnectDict.Add("E-BFEL221", type2);
            blkConnectDict.Add("E-BFEL223", type3);
            addBlkConnectDict(blkConnectDict, "E-BFEL240", rightTopPt2);
            addBlkConnectDict(blkConnectDict, "E-BFEL241", rightTopPt2);
            addBlkConnectDict(blkConnectDict, "E-BFEL250", rightTopPt2);
            addBlkConnectDict(blkConnectDict, "E-BFEL251", rightTopPt2);
            addBlkConnectDict(blkConnectDict, "E-BFEL120", rightTopPt1);
            blkConnectDict.Add("E-BFEL121", type2);
            addBlkConnectDict(blkConnectDict, "E-BFEL122", rightTopPt1);
            blkConnectDict.Add("E-BFEL123", type2);
            blkConnectDict.Add("E-BFEL211", type2);

            return blkConnectDict;

        }

        private static void addBlkConnectDict(Dictionary<string, List<Point3d>> blkConnectDict, string key, Point3d rightTopPt)
        {
            Point3d leftPt = new Point3d(-rightTopPt.X, 0, 0);
            Point3d bottomPt = new Point3d(0, -rightTopPt.Y, 0);
            Point3d rightPt = new Point3d(rightTopPt.X, 0, 0);
            Point3d topPt = new Point3d(0, rightTopPt.Y, 0);

            blkConnectDict.Add(key, new List<Point3d> { leftPt, bottomPt, rightPt, topPt });
        }

        public static void getBlkOutLine(BlockReference blk, Dictionary<string, List<Point3d>> blkSizeDict, out Polyline blkOutline, out List<Point3d> connectPt, BlockReference groupBlk = null)
        {
            var ptList = blkSizeDict[blk.Name].Select(x => x).ToList();
            connectPt = ptList.Select(x => x.TransformBy(blk.BlockTransform)).ToList();

            if (groupBlk != null)
            {
                var ptListGroup = blkSizeDict[groupBlk.Name];
                var connectPtGroup = ptListGroup.Select(x => x.TransformBy(groupBlk.BlockTransform)).ToList();
                var bottomPt = connectPtGroup[1];

                var inx = connectPt.IndexOf(connectPt.OrderBy(x => x.DistanceTo(bottomPt)).First());

                if (inx == 1 || inx == 3)
                {
                    var ptNew = new Point3d(ptList[inx].X, ptList[inx].Y / Math.Abs(ptList[inx].Y) * (Math.Abs(ptList[inx].Y) + Math.Abs(ptListGroup[1].Y) + Math.Abs(ptListGroup[3].Y)), 0);
                    ptList[inx] = ptNew;

                    connectPt = ptList.Select(x => x.TransformBy(blk.BlockTransform)).ToList();
                }
            }

            blkOutline = new Polyline();

            blkOutline.AddVertexAt(0, new Point2d(ptList[0].X, ptList[3].Y), 0, 0, 0);
            blkOutline.AddVertexAt(1, new Point2d(ptList[0].X, ptList[1].Y), 0, 0, 0);
            blkOutline.AddVertexAt(2, new Point2d(ptList[2].X, ptList[1].Y), 0, 0, 0);
            blkOutline.AddVertexAt(3, new Point2d(ptList[2].X, ptList[3].Y), 0, 0, 0);
            blkOutline.TransformBy(blk.BlockTransform);
            blkOutline.Closed = true;


        }


    }
}
