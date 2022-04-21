using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;

using ThMEPWSS.DrainageSystemDiagram.Model;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageADConvertValveService
    {
        public static List<ThDrainageSDADBlkOutput> convertValveInPipe(List<ThDrainageSDADValve> valveList, List<Line> convertPipeList, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertNode)
        {
            var tol = new Tolerance(10, 10);
            var valveCon = new List<ThDrainageSDADBlkOutput>();

            for (int i = 0; i < valveList.Count; i++)
            {
                //找到新插入点，新插入方向
                var startPt = valveList[i].centerLine.StartPoint;
                var endPt = valveList[i].centerLine.EndPoint;
                var valveNodeStart = convertNode.Where(x => x.Key.Node.IsEqualTo(startPt, tol)).FirstOrDefault();
                var valveNodeEnd = convertNode.Where(x => x.Key.Node.IsEqualTo(endPt, tol)).FirstOrDefault();

                if (valveNodeStart.Key != null && valveNodeStart.Key.Child.Count() > 0)
                {
                    var pt = valveNodeStart.Value.Node;
                    var dir = (valveNodeEnd.Value.Node - valveNodeStart.Value.Node).GetNormal();
                    var outputPt = valveNodeStart.Value.Node + (valveList[i].blk.Position - valveNodeStart.Key.Node).Length * dir;
                    var valveOutput = new ThDrainageSDADBlkOutput(outputPt);
                    valveOutput.Name = valveList[i].type;
                    valveOutput.Dir = dir;
                    valveOutput.Visibility.Add(ThDrainageADCommon.visiName1_valve, valveList[i].visibility);
                    valveOutput.Scale = valveList[i].scale;
                    valveOutput.BlkSize = valveList[i].centerLine.Length;
                    valveCon.Add(valveOutput);

                    //删除pipe
                    var centerPipe = convertPipeList.Where(x => (x.StartPoint.IsEqualTo(valveNodeStart.Value.Node, tol) && x.EndPoint.IsEqualTo(valveNodeEnd.Value.Node, tol)) ||
                                                                (x.EndPoint.IsEqualTo(valveNodeStart.Value.Node, tol) && x.StartPoint.IsEqualTo(valveNodeEnd.Value.Node, tol))).FirstOrDefault();
                    convertPipeList.Remove(centerPipe);
                }
            }

            return valveCon;
        }

        public static Dictionary<ThDrainageSDTreeNode, ThTerminalToilet> findToiletType(List<ThDrainageSDTreeNode> leaf, List<ThTerminalToilet> toiletList)
        {
            var toiDict = new Dictionary<ThDrainageSDTreeNode, ThTerminalToilet>();
            var allSupplyPt = toiletList.SelectMany(x => x.SupplyCool).ToList();

            foreach (var node in leaf)
            {
                //最近和同方向
                var ptTemp = allSupplyPt.Where(x => x.DistanceTo(node.Node) <= ThDrainageSDCommon.TolToiletToWall);
                var dirNode = new Vector3d();
                if (node.Parent != null)
                {
                    dirNode = (node.Parent.Node - node.Node).GetNormal();
                }

                var ptTempSameDir = ptTemp.Where(x =>
                {
                    var bReturn = false;
                    var dirSupply = x - node.Node;
                    if (dirSupply.Length < 1)
                    {
                        bReturn = true;
                    }
                    else
                    {
                        //dirSupply = dirSupply.GetNormal();
                        var toi = toiletList.Where(t => t.SupplyCool.Contains(x)).First();
                        dirSupply = toi.Dir.GetNormal();
                        var cos = dirSupply.DotProduct(dirNode) / (dirNode.Length * dirSupply.Length);
                        if (cos >= Math.Cos(5 * Math.PI / 180))
                        {
                            bReturn = true;
                        }
                    }
                    return bReturn;
                });

                var ptTempClose = ptTempSameDir.OrderBy(x => x.DistanceTo(node.Node)).FirstOrDefault();

                var toilet = toiletList.Where(x => x.SupplyCool.Contains(ptTempClose)).FirstOrDefault();

                if (node != null && toilet != null)
                {
                    toiDict.Add(node, toilet);
                }
            }

            return toiDict;
        }

        public static List<ThDrainageSDADBlkOutput> convertEndValve(Dictionary<ThDrainageSDTreeNode, List<Line>> endStackPipe, Dictionary<ThDrainageSDTreeNode, ThTerminalToilet> toiDict, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertNode)
        {
            var valveEndCon = new List<ThDrainageSDADBlkOutput>();

            foreach (var end in endStackPipe)
            {
                var endValveOutput = new ThDrainageSDADBlkOutput(end.Value.Last().EndPoint);
                endValveOutput.Dir = new Vector3d(1, 0, 0);

                var hasToi = toiDict.TryGetValue(end.Key, out var toi);

                if (hasToi == true)
                {
                    endValveOutput.Name = ThDrainageADCommon.toi_end_name[toi.Type];
                    var visi = getEndVisivility(end.Key, endValveOutput.Name, convertNode, out var visiInx);
                    endValveOutput.Visibility.Add(ThDrainageADCommon.visiName_valve, visi);
                    endValveOutput.Scale = ThDrainageADCommon.blk_scale_end;
                    valveEndCon.Add(endValveOutput);
                }
            }

            return valveEndCon;
        }

        private static string getEndVisivility(ThDrainageSDTreeNode node, string name, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertNode, out int visiInx)
        {
            var nodeP = node.Parent;
            var dir = (convertNode[nodeP].Node - convertNode[node].Node).GetNormal();

            var angle = dir.GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);
            var dirInx = 0;
            var visibility = ThDrainageADCommon.endValve_dir_name[name][0];

            if (359 * Math.PI / 180 <= angle || angle <= 1 * Math.PI / 180)
            {
                dirInx = 0;
            }
            else if (44 * Math.PI / 180 <= angle && angle <= 46 * Math.PI / 180)
            {
                dirInx = 1;
            }
            else if (179 * Math.PI / 180 <= angle && angle <= 181 * Math.PI / 180)
            {
                dirInx = 2;
            }
            else if (224 * Math.PI / 180 <= angle && angle <= 226 * Math.PI / 180)
            {
                dirInx = 3;
            }

            if (dirInx != -1)
            {
                visibility = ThDrainageADCommon.endValve_dir_name[name][dirInx];
            }

            visiInx = dirInx;

            return visibility;
        }

        public static List<Line> toAllIsolateLine(ThDrainageSDADBlkOutput endValve)
        {
            var visiIdx = ThDrainageADCommon.endValve_dir_name[endValve.Name].IndexOf(endValve.Visibility.ElementAt(0).Value);
            var isolateLine = new List<Line>();
            var yLeftRight = 200;
            var xLeftRight = 400;
            var yFrontBack = 210;
            var xFrontBack = 210;

            var pt0 = new Point3d();
            var pt1 = new Point3d();
            var pt2 = new Point3d();
            var pt3 = new Point3d();

            if (visiIdx == 0)
            {
                pt0 = new Point3d(endValve.Position.X, endValve.Position.Y + yLeftRight / 2, 0);
                pt1 = new Point3d(endValve.Position.X + xLeftRight, endValve.Position.Y + yLeftRight / 2, 0);
                pt2 = new Point3d(endValve.Position.X + xLeftRight, endValve.Position.Y - yLeftRight/2, 0);
                pt3 = new Point3d(endValve.Position.X, endValve.Position.Y - yLeftRight/2, 0);
                var pt4= new Point3d(endValve.Position.X, endValve.Position.Y, 0);
                var pt5 = new Point3d(endValve.Position.X + xLeftRight, endValve.Position.Y, 0);
                isolateLine.Add(new Line(pt4, pt5));
            }
            if (visiIdx == 2)
            {
                pt0 = new Point3d(endValve.Position.X, endValve.Position.Y + yLeftRight / 2, 0);
                pt1 = new Point3d(endValve.Position.X - xLeftRight, endValve.Position.Y + yLeftRight / 2, 0);
                pt2 = new Point3d(endValve.Position.X - xLeftRight, endValve.Position.Y - yLeftRight/2, 0);
                pt3 = new Point3d(endValve.Position.X, endValve.Position.Y - yLeftRight/2, 0);
                var pt4 = new Point3d(endValve.Position.X, endValve.Position.Y, 0);
                var pt5 = new Point3d(endValve.Position.X - xLeftRight, endValve.Position.Y, 0);
                isolateLine.Add(new Line(pt4, pt5));
            }
            if (visiIdx == 1)
            {
                pt0 = new Point3d(endValve.Position.X, endValve.Position.Y, 0);
                pt1 = new Point3d(endValve.Position.X, endValve.Position.Y + yFrontBack, 0);
                pt2 = new Point3d(endValve.Position.X + xFrontBack, endValve.Position.Y + yFrontBack, 0);
                pt3 = new Point3d(endValve.Position.X + xFrontBack, endValve.Position.Y, 0);
                isolateLine.Add(new Line(pt0, pt2));
            }
            if (visiIdx == 3)
            {
                pt0 = new Point3d(endValve.Position.X, endValve.Position.Y, 0);
                pt1 = new Point3d(endValve.Position.X - xFrontBack, endValve.Position.Y, 0);
                pt2 = new Point3d(endValve.Position.X - xFrontBack, endValve.Position.Y - yFrontBack, 0);
                pt3 = new Point3d(endValve.Position.X, endValve.Position.Y - yFrontBack, 0);
                isolateLine.Add(new Line(pt0, pt2));
            }

            isolateLine.Add(new Line(pt0, pt1));
            isolateLine.Add(new Line(pt1, pt2));
            isolateLine.Add(new Line(pt2, pt3));
            isolateLine.Add(new Line(pt3, pt0));

            return isolateLine;
        }
    }
}
