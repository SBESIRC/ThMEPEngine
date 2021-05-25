using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    class MergeSideService
    {
        public static void mergeSide(Dictionary<int, List<List<(Line, int)>>> orderedAllLaneSideList, out Dictionary<int, List<(Line, int)>> sideDict)
        {
            sideDict = new Dictionary<int, List<(Line, int)>>();
            int mergeIndex = 0;
            int check = 0;


            foreach (var pair in orderedAllLaneSideList)
            {
                var orderedLaneSideList = pair.Value;

                for (int i = 0; i < orderedLaneSideList.Count; i++)
                {

                    sideDict.Add(sideDict.Count, new List<(Line, int)>() { orderedLaneSideList[i][0] });

                    for (int j = 1; j < orderedLaneSideList[i].Count; j++)
                    {
                        for (mergeIndex = j; mergeIndex < orderedLaneSideList[i].Count; mergeIndex++)
                        {
                            check = mergeIndex - 1;
                            if (mergeSameLaneSide(orderedLaneSideList[i][mergeIndex], orderedLaneSideList[i][check]))
                            {
                                var keyIndex = sideDict.Where(x => x.Value.Contains(orderedLaneSideList[i][check])).FirstOrDefault().Key;
                                sideDict[keyIndex].Add(orderedLaneSideList[i][mergeIndex]);
                            }
                            else
                            {
                                break;
                            }
                        }

                        j = mergeIndex;
                        if (j < orderedLaneSideList[i].Count)
                        {
                            sideDict.Add(sideDict.Count, new List<(Line, int)>() { orderedLaneSideList[i][j] });
                        }
                    }

                    //check if 0 and -1 are same lane
                    check = orderedLaneSideList[i].Count - 1;
                    if (mergeSameLaneSide(orderedLaneSideList[i][0], orderedLaneSideList[i][check]))
                    {
                        var lastIndex = sideDict.Where(x => x.Value.Contains(orderedLaneSideList[i][check])).First();
                        var firstIndex = sideDict.Where(x => x.Value.Contains(orderedLaneSideList[i][0])).First();
                        sideDict[firstIndex.Key].AddRange(sideDict[lastIndex.Key]);
                        sideDict.Remove(lastIndex.Key);
                    }
                }
            }
        }

        private static bool mergeSameLaneSide((Line, int) lane, (Line, int) laneNext)
        {
            bool bAngle = false;

            if (lane.Item1 != laneNext.Item1)
            {
                var laneDir = (lane.Item1.EndPoint - lane.Item1.StartPoint).GetNormal();
                var laneNextDir = (laneNext.Item1.EndPoint - laneNext.Item1.StartPoint).GetNormal();

                bAngle = Math.Abs(laneDir.DotProduct(laneNextDir)) / (laneDir.Length * laneNextDir.Length) > Math.Abs(Math.Cos(20 * Math.PI / 180));

            }

            return bAngle;
        }

        public static void mergeSigleSideBlocks(Dictionary<int, List<(Line, int)>> sideDict, List<ThSingleSideBlocks> singleSideBlocks)
        {
            foreach (var sidePair in sideDict)
            {
                var side = sidePair.Value;
                var singleSideBlocksInSameLane = singleSideBlocks.Where(y => side.Contains(y.laneSide[0])).ToList();

                singleSideBlocksInSameLane[0].laneSideNo = sidePair.Key;

                for (int i = 1; i < singleSideBlocksInSameLane.Count; i++)
                {
                    singleSideBlocksInSameLane[0].mainBlk.AddRange(singleSideBlocksInSameLane[i].mainBlk);
                    singleSideBlocksInSameLane[0].secBlk.AddRange(singleSideBlocksInSameLane[i].secBlk);
                    singleSideBlocksInSameLane[i].groupBlock.ToList().ForEach(x => singleSideBlocksInSameLane[0].groupBlock.Add(x.Key, x.Value));

                    singleSideBlocksInSameLane[0].laneSide.AddRange(singleSideBlocksInSameLane[i].laneSide);
                    singleSideBlocks.Remove(singleSideBlocksInSameLane[i]);

                }
            }

        }

      

       

    }
}
