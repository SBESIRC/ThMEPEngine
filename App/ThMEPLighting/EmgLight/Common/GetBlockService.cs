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

        public void getBlocksData(Polyline bufferFrame, ThMEPOriginTransformer transformer)
        {
            emgLight = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EmgLightBlockName, transformer);
            evacR = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacRBlockName, transformer);
            evacRL = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLRBlockName, transformer);

            exitE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitEBlockName, transformer);
            exitS = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSBlockName, transformer);
            exitECeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitECeilingBlockName, transformer);
            exitSCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSCeilingBlockName, transformer);

            enter = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterBlockName, transformer);
            enterCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterCeilingBlockName, transformer);

            evacCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacCeilingBlockName, transformer);
            evacR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacR2LineCeilingBlockName, transformer);
            evacLR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLR2LineCeilingBlockName, transformer);

            // var ALE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EPowerLayerName, ThMEPLightingCommon.ALEBlockName, transformer);

        }

        public void getBlockList(Dictionary<EmgBlkType.BlockType, List<BlockReference>> blockList)
        {
            blockList.Add(EmgBlkType.BlockType.emgLight, emgLight.Select(x => x.Value).ToList());
            blockList.Add(EmgBlkType.BlockType.evac, evacR.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.evac].AddRange(evacRL.Select(x => x.Value).ToList());

            blockList.Add(EmgBlkType.BlockType.exit, exitE.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.exit].AddRange(exitS.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.exit].AddRange(exitECeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.exit].AddRange(exitSCeiling.Select(x => x.Value).ToList());

            blockList.Add(EmgBlkType.BlockType.enter, enter.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.enter].AddRange(enterCeiling.Select(x => x.Value).ToList());

            blockList.Add(EmgBlkType.BlockType.evacCeiling, evacCeiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.evacCeiling].AddRange(evacR2Ceiling.Select(x => x.Value).ToList());
            blockList[EmgBlkType.BlockType.evacCeiling].AddRange(evacLR2Ceiling.Select(x => x.Value).ToList());

            //blockList.Add(EmgBlkType.BlockType.ale, ALE.Select(x => x.Value).ToList());

        }

        //public static void projectToXY(ref Dictionary<EmgBlkType.BlockType, List<BlockReference>> blockList)
        //{
        //    var typeList = blockList.Select(x => x.Key).ToList();
        //    foreach (var type in typeList)
        //    {
        //        blockList[type].ForEach(x => x.Position = new Point3d(x.Position.X, x.Position.Y, 0));

        //    }
        //}

        public static Dictionary<string, List<Point3d>> blkConnectDict()
        {
            //public static readonly string EmgLightBlockName = "E-BFEL810";      //消防应急灯图块名
            //public static readonly string EvacRBlockName = "E-BFEL200";     //疏散指示灯图块
            //public static readonly string EvacLRBlockName = "E-BFEL210";     //疏散指示灯图块

            //public static readonly string ExitEBlockName = "E-BFEL100";           //紧急出口图块
            //public static readonly string ExitSBlockName = "E-BFEL102";           //紧急出口图块

            //public static readonly string ExitECeilingBlockName = "E-BFEL101";//紧急出口吊装
            //public static readonly string ExitSCeilingBlockName = "E-BFEL103";//紧急出口吊装

            //public static readonly string EnterBlockName = "E-BFEL140";//出口/禁止
            //public static readonly string EnterCeilingBlockName = "E-BFEL141";//出口/禁止吊装

            //public static readonly string EvacCeilingBlockName = "E-BFEL201";//疏散指示灯吊装
            //public static readonly string EvacR2LineCeilingBlockName = "E-BFEL201-1";//疏散指示灯吊装
            //public static readonly string EvacLR2LineCeilingBlockName = "E-BFEL211-1";//疏散指示灯吊装


            Dictionary<string, List<Point3d>> blkConnectDict = new Dictionary<string, List<Point3d>>();

            Point3d rightTopPt1 = new Point3d(2.5, 1.25, 0);

            List<Point3d> type2 = new List<Point3d> { new Point3d(-2.5, 0, 0), new Point3d(0, -1.25, 0), new Point3d(2.5, 0, 0), new Point3d(0, 2.5, 0) };

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

        public static void getBlkOutLine(BlockReference blk, Dictionary<string, List<Point3d>> blkSizeDict,out Polyline blkOutline, out List<Point3d> connectPt, BlockReference groupBlk = null)
        {
            var ptList = blkSizeDict[blk.Name].Select(x => x).ToList();
             connectPt = ptList.Select(x => x.TransformBy(blk.BlockTransform)).ToList();

            if (groupBlk != null)
            {
                var ptListGroup = blkSizeDict[groupBlk.Name];
                var connectPtGroup = ptListGroup.Select(x => x.TransformBy(groupBlk.BlockTransform)).ToList();
                var bottomPt = connectPtGroup[1];

                var inx = connectPt.IndexOf(connectPt.OrderBy(x => x.DistanceTo(bottomPt)).First());

                var ptNew = new Point3d(ptList[inx].X, ptList[inx].Y / Math.Abs(ptList[inx].Y) * (Math.Abs(ptList[inx].Y) + Math.Abs(ptListGroup[1].Y) + Math.Abs(ptListGroup[3].Y)), 0);
                ptList[inx] = ptNew;

                connectPt = ptList.Select(x => x.TransformBy(blk.BlockTransform)).ToList();
            }

             blkOutline = new Polyline();

            blkOutline.AddVertexAt(0, new Point2d(ptList[0].X, ptList[3].Y), 0, 0, 0);
            blkOutline.AddVertexAt(1, new Point2d(ptList[0].X, ptList[1].Y), 0, 0, 0);
            blkOutline.AddVertexAt(2, new Point2d(ptList[2].X, ptList[1].Y), 0, 0, 0);
            blkOutline.AddVertexAt(3, new Point2d(ptList[2].X, ptList[3].Y), 0, 0, 0);
            blkOutline.TransformBy(blk.BlockTransform);
            blkOutline.Closed = true;

           
        }

        public static double getScale(BlockReference blk)
        {
            double scale = 100;

            scale= blk.ScaleFactors.X;

            return scale;
        }

    }
}
