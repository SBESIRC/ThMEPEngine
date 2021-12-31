using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
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

            DrawUtils.ShowGeometry(connectMainLine, EmgConnectCommon.LayerConnectLine, 130);
            DrawUtils.ShowGeometry(connectSecLine, EmgConnectCommon.LayerConnectLine, 70);

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

                    line.groupBlock.ForEach(x => DrawUtils.ShowGeometry(x.Value, EmgConnectCommon.LayerOptimalSingleSideGroup,  130));


                    count = count + line.Count;
                }
                groupLine.Add(mLine);

                if (mLine.NumberOfVertices > 0)
                {
                    DrawUtils.ShowGeometry(mLine.StartPoint, count.ToString(), EmgConnectCommon.LayerOptimalSingleSideGroup, 130,30);
                }
            }

            DrawUtils.ShowGeometry(groupLine, EmgConnectCommon.LayerOptimalSingleSideGroup, 130);

        }

        public static void forDebugConnectLine(List<(Point3d, Point3d)> connectList)
        {
            var connectLine = new List<Line>();
            foreach (var connectPair in connectList)
            {
                connectLine.Add(new Line(connectPair.Item1, connectPair.Item2));

            }

            DrawUtils.ShowGeometry(connectLine, EmgConnectCommon.LayerConnectLine, 30);
        }

        public static void forDebugLaneSideNo(List<ThSingleSideBlocks> singleSideBlocks)
        {
            for (int i = 0; i < singleSideBlocks.Count; i++)
            {
                var side = singleSideBlocks[i];
                var lanePoly = new Polyline();


                for (int j = 0; j < side.laneSide.Count; j++)
                {
                    lanePoly.AddVertexAt(lanePoly.NumberOfVertices, side.laneSide[j].Item1.StartPoint.ToPoint2d(), 0, 0, 0);

                    if (j == side.laneSide.Count - 1)
                    {
                        lanePoly.AddVertexAt(lanePoly.NumberOfVertices, side.laneSide[j].Item1.EndPoint.ToPoint2d(), 0, 0, 0);
                    }
                }

                var midPoint = lanePoly.GetPointAtDist(lanePoly.Length / 2);

                var zDir = side.laneSide[0].Item2 == 0 ? 1 : -1;
                var newPt = midPoint + 100 * (lanePoly.EndPoint - lanePoly.StartPoint).GetNormal().RotateBy(Math.PI / 2, Vector3d.ZAxis * zDir);

                DrawUtils.ShowGeometry(newPt, string.Format("sideNo: {0}", singleSideBlocks[i].laneSideNo), "l5laneSide",  30, 25, 200);
            }

        }

        public static void forDebugPrintBlkLable(List<Point3d> pts, string layer)
        {
            for (int j = 0; j < pts.Count; j++)
            {
                DrawUtils.ShowGeometry(new Point3d(pts[j].X + 100, pts[j].Y, 0), j.ToString(), layer, 40, 25, 200);
            }
        }

        public static void forDebugBlkOutline(List<ThBlock> blks)
        {
            blks.ForEach(x => DrawUtils.ShowGeometry(x.outline, EmgConnectCommon.LayerBlkOutline, 40));
        }
    }

}
