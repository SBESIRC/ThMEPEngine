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
        public static void forDebugSingleSideBlocks(List<ThSingleSideBlocks> sides)
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

            DrawUtils.ShowGeometry(connectMainLine, EmgConnectCommon.LayerConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 130));
            DrawUtils.ShowGeometry(connectSecLine, EmgConnectCommon.LayerConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 70));

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

                    line.groupBlock.ForEach(x => DrawUtils.ShowGeometry(x.Value, EmgConnectCommon.LayerOptimalSingleSideGroup, Color.FromColorIndex(ColorMethod.ByColor, 130)));


                    count = count + line.Count;
                }
                groupLine.Add(mLine);

                DrawUtils.ShowGeometry(mLine.StartPoint, count.ToString(), EmgConnectCommon.LayerOptimalSingleSideGroup, Color.FromColorIndex(ColorMethod.ByColor, 130), LineWeight.LineWeight030);

            }

            DrawUtils.ShowGeometry(groupLine, EmgConnectCommon.LayerOptimalSingleSideGroup, Color.FromColorIndex(ColorMethod.ByColor, 130));

        }

        public static void forDebugConnectLine(List<(Point3d, Point3d)> connectList)
        {
            var connectLine = new List<Line>();
            foreach (var connectPair in connectList)
            {
                connectLine.Add(new Line(connectPair.Item1, connectPair.Item2));

            }

            DrawUtils.ShowGeometry(connectLine, EmgConnectCommon.LayerConnectLine, Color.FromColorIndex(ColorMethod.ByColor, 30));


        }

    }

}
