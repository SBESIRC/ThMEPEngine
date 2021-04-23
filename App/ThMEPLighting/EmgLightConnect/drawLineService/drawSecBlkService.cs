using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawSecBlkService
    {
        public static void drawSecToMain(List<ThSingleSideBlocks> singleSideBlocks, List<BlockReference> blkSource, Polyline frame, List<ThBlock> blkList)
        {
            List<Polyline> connList = new List<Polyline>();
            for (int sideIndex = 0; sideIndex < singleSideBlocks.Count; sideIndex++)
            {
                var side = singleSideBlocks[sideIndex];

                for (int i = 0; i < side.ptLink.Count; i++)
                {
                    var ptL = side.ptLink[i];

                    if (side.reSecBlk.Contains(ptL.Item1) || side.reSecBlk.Contains(ptL.Item2))
                    {
                        var poly = linkSecToMain(ptL, blkList);
                        connList.Add(poly);
                    }
                }
            }

            DrawUtils.ShowGeometry(connList, EmgConnectCommon.LayerConnectLineFinal, Color.FromColorIndex(ColorMethod.ByColor, 70));
        }

        public static void drawGroupToGroup(List<(Point3d, Point3d)> connPts, List<BlockReference> blkSource, Polyline frame, List<ThBlock> blkList)
        {
            List<Polyline> connList = new List<Polyline>();


            for (int i = 0; i < connPts.Count; i++)
            {

                var poly = linkSecToMain(connPts[i], blkList);
                connList.Add(poly);
            }

            DrawUtils.ShowGeometry(connList, EmgConnectCommon.LayerConnectLineFinal, Color.FromColorIndex(ColorMethod.ByColor, 30));
        }

        private static Polyline linkSecToMain((Point3d,Point3d) ptL, List<ThBlock> blkList)
        {
            var blkS = GetBlockService.getBlock(ptL.Item1, blkList);
            var blkE = GetBlockService.getBlock(ptL.Item2, blkList);

            var lineTemp = new Line(blkS.cenPt , blkE.cenPt);
            var ptS = drawEmgPipeService.getConnectPt(blkS, lineTemp, blkList);
            var ptE = drawEmgPipeService.getConnectPt(blkE, lineTemp, blkList);

            var poly = new Polyline();
            poly.AddVertexAt(poly.NumberOfVertices, ptS.ToPoint2d(), 0, 0, 0);
            poly.AddVertexAt(poly.NumberOfVertices, ptE.ToPoint2d(), 0, 0, 0);


            return poly;

        }


    }
}
