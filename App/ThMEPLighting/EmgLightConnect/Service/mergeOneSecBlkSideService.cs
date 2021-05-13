using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class mergeOneSecBlkSideService
    {
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

        public static void relocateSecBlockSideOri(List<ThSingleSideBlocks> singleSideBlocks)
        {
            var allTotalMainBlk = singleSideBlocks.SelectMany(x => x.getAllMainAndReMain()).ToList();

            for (int i = 0; i < singleSideBlocks.Count; i++)
            {
                var sideMainBlk = singleSideBlocks[i].getAllMainAndReMain();

                for (int j = singleSideBlocks[i].secBlk.Count - 1; j >= 0; j--)
                {
                    var secPt = singleSideBlocks[i].secBlk[j];
                    var sideNearest = sideMainBlk.Select(x => x.DistanceTo(secPt)).Min();
                    var allNearest = allTotalMainBlk.Select(x => x.DistanceTo(secPt)).Min();

                    if (sideNearest / allNearest > 3)
                    {
                        var newMainBlk = allTotalMainBlk.Where(x => x.DistanceTo(secPt) == allNearest).FirstOrDefault();
                        var newSide = singleSideBlocks.Where(x => x.getTotalBlock().Contains(newMainBlk)).First();
                        newSide.secBlk.Add(secPt);
                        singleSideBlocks[i].secBlk.Remove(secPt);
                    }

                }
            }
        }

        public static void relocateSecBlockSideNotUse(List<ThSingleSideBlocks> singleSideBlocks)
        {
            var allTotalMainBlk = singleSideBlocks.SelectMany(x => x.getAllMainAndReMain()).ToList();
            //var allTotalMainBlk = singleSideBlocks.SelectMany(x => x.getTotalBlock()).ToList();

            for (int i = 0; i < singleSideBlocks.Count; i++)
            {
                var sideMainBlk = singleSideBlocks[i].getAllMainAndReMain();
                //var sideAllBlk = singleSideBlocks[i].getTotalBlock();

                for (int j = singleSideBlocks[i].secBlk.Count - 1; j >= 0; j--)
                {
                    var secPt = singleSideBlocks[i].secBlk[j];
                    var sideNearest = sideMainBlk.Select(x => x.DistanceTo(secPt)).Min();
                    var allNearest = allTotalMainBlk.Select(x => x.DistanceTo(secPt)).Min();
                    //var allNearest = allTotalMainBlk.Where(x => sideAllBlk.Contains(x) == false).Select(x => x.DistanceTo(secPt)).Min();

                    if ((sideNearest / allNearest > 3) || (sideNearest > 15000 && allNearest < sideNearest))
                    //if (sideNearest / allNearest > 3)
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

                    if (sideNearest < 0  || (sideNearest / allNearest > 3) || (sideNearest > 15000 && allNearest < sideNearest))
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
