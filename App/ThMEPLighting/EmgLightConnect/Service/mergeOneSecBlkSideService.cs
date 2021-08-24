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
    public class mergeOneSecBlkSideService
    {
        public static void mergeScatterBlockSide(List<ThSingleSideBlocks> singleSideBlocks)
        {
            var tol = new Tolerance(10, 10);

            var scatterSide = singleSideBlocks.Where(x => ifAScatterBlockSide(x) == true);

            foreach (var side in scatterSide)
            {
                foreach (var laneSeg in side.laneSide)
                {
                    var otherSideLaneSeg = singleSideBlocks.SelectMany(x => x.laneSide).Where(x => x.Item1 == laneSeg.Item1 && x.Item2 != laneSeg.Item2).FirstOrDefault();
                    if (otherSideLaneSeg.Item1 != null)
                    {
                        var otherSide = singleSideBlocks.Where(x => x.laneSide.Contains(otherSideLaneSeg)).FirstOrDefault();

                        var segMatrix = GeomUtils.getLineMatrix(laneSeg.Item1.StartPoint, laneSeg.Item1.EndPoint);
                        var blkDict = side.getTotalBlock().ToDictionary(x => x, x => x.TransformBy(segMatrix.Inverse()));
                        var blkInSeg = blkDict.Where(x => 0 <= x.Value.X && x.Value.X <= laneSeg.Item1.Length).Select(x => x.Key).ToList();
                        var blkInSegGroup = side.groupBlock.Where(x => blkInSeg.Contains(x.Key)).ToList();

                        otherSide.secBlk.AddRange(blkInSeg);
                        blkInSegGroup.ForEach(x => otherSide.groupBlock.Add(x.Key, x.Value));
                        side.mainBlk.RemoveAll(x => blkInSeg.Contains(x));
                        side.secBlk.RemoveAll(x => blkInSeg.Contains(x));
                        blkInSegGroup.ForEach(x => side.groupBlock.Remove(x.Key));
                    }

                }
            }
        }

        private static bool ifAScatterBlockSide(ThSingleSideBlocks side)
        {
            var tol = 12000;
            var bReturn = false;
            var pts = side.getTotalBlock();
            //var ptsOrder = pts.OrderBy(x => x.TransformBy(side.Matrix.Inverse()).X).ToList();
            var ptsD = pts.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(x => x.Value.X).ToDictionary(x => x.Key, x => x.Value);

            var dist = new List<double>();
            for (int i = 0; i < ptsD.Count - 1; i++)
            {
                dist.Add(ptsD.ElementAt(i + 1).Value.X - ptsD.ElementAt(i).Value.X);
            }

            var largeDist = dist.Where(x => x > tol);
            if (largeDist.Count() > 0)
            {
                bReturn = true;
            }

            return bReturn;

        }


        public static void mergeOneSecBlockSide(List<ThSingleSideBlocks> singleSideBlocks)
        {
            var tol = new Tolerance(1, 1);
            var oneBlockSide = singleSideBlocks.Where(x => x.secBlk.Count == 1 && x.mainBlk.Count == 0).ToList();
            var allBlk = singleSideBlocks.SelectMany(x => x.getTotalBlock()).ToList();

            for (int i = 0; i < oneBlockSide.Count; i++)
            {
                var secPt = oneBlockSide[i].secBlk[0];
                var allNearest = allBlk.Where(x => x.IsEqualTo(secPt, tol) == false).Select(x => x.DistanceTo(secPt)).Min();

                if (allNearest > 0)
                {
                    var newMainBlk = allBlk.Where(x => x.DistanceTo(secPt) == allNearest).FirstOrDefault();
                    var newSide = singleSideBlocks.Where(x => x.getTotalBlock().Contains(newMainBlk)).First();
                    newSide.secBlk.Add(secPt);
                    oneBlockSide[i].secBlk.Remove(secPt);
                }
            }
        }

        public static void relocateSecBlockSide(List<ThSingleSideBlocks> singleSideBlocks)
        {
            var allTotalMainBlk = singleSideBlocks.SelectMany(x => x.getAllMainAndReMain()).ToList();

            for (int i = 0; i < singleSideBlocks.Count; i++)
            {
                var sideMainBlk = singleSideBlocks[i].getAllMainAndReMain();

                for (int j = singleSideBlocks[i].secBlk.Count - 1; j >= 0; j--)
                {
                    var secPt = singleSideBlocks[i].secBlk[j];
                    double sideNearest = -1;
                    if (sideMainBlk.Count > 0)
                    {
                        //只有sec 没有 main
                        sideNearest = sideMainBlk.Select(x => x.DistanceTo(secPt)).Min();
                    }

                    var allNearest = allTotalMainBlk.Select(x => x.DistanceTo(secPt)).Min();

                    if (sideNearest < 0 || (sideNearest / allNearest > 3) || (sideNearest > 15000 && allNearest < sideNearest))
                    {
                        var newMainBlk = allTotalMainBlk.Where(x => x.DistanceTo(secPt) == allNearest).FirstOrDefault();
                        var newSide = singleSideBlocks.Where(x => x.getTotalBlock().Contains(newMainBlk)).First();

                        if (newSide.laneSideNo != singleSideBlocks[i].laneSideNo)
                        {
                            newSide.secBlk.Add(secPt);
                            singleSideBlocks[i].secBlk.Remove(secPt);
                        }
                    }
                }
            }
        }
    }
}
