using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Common;


namespace ThMEPLighting.EmgLightConnect.Service
{
  public  class BlockListService
    {
        public static void getBlkList(List<ThSingleSideBlocks> SingleSide, List<BlockReference> blkSource, ref List<ThBlock> blkList)
        {
            var blkConnectDict = GetBlockService.blkConnectDict();
            for (int i = 0; i < SingleSide.Count; i++)
            {
                getSideBlkList(SingleSide[i], blkSource, blkConnectDict, ref blkList);
            }
        }

        private static void getSideBlkList(ThSingleSideBlocks side, List<BlockReference> blkSource, Dictionary<string, List<Point3d>> blkConnectDict, ref List<ThBlock> blkList)
        {
            Tolerance tol = new Tolerance(10, 10);
            var allBlockList = side.getTotalBlock();

            for (int i = 0; i < allBlockList.Count; i++)
            {
                Point3d pt = allBlockList[i];
                BlockReference groupBlk = null;

                var blk = blkSource.Where(x => x.Position.IsEqualTo(pt, tol)).FirstOrDefault();

                if (side.groupBlock.ContainsKey(pt) == true)
                {
                    var groupPt = side.groupBlock[pt];
                    groupBlk = blkSource.Where(x => x.Position.IsEqualTo(groupPt, tol)).FirstOrDefault();
                }

                var blkModel = new ThBlock(blk);
                blkModel.setBlkInfo(blkConnectDict, groupBlk);

                blkList.Add(blkModel);
            }
        }

        public static ThBlock getBlockByCenter(Point3d pt, List<ThBlock> blkList)
        {
            ThBlock blk = null;

            blk = blkList.Where(x => x.blkCenPt.IsEqualTo(pt, new Tolerance(1, 1))).FirstOrDefault();

            return blk;
        }

        public static ThBlock getBlockByConnect(Point3d pt, List<ThBlock> blkList)
        {
            ThBlock blk = null;
            var tol = new Tolerance(1, 1);

            blk = blkList.Where(x => x.outline.ToCurve3d().IsOn(pt, tol) || x.outline.Contains(pt)).FirstOrDefault();

            return blk;
        }
    
    }
}
