using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageADDiameterDim
    {
        public static void calculateTreeDiameter(ThDrainageSDTreeNode node, Dictionary<ThDrainageSDTreeNode, ThTerminalToilet> toiDict, Dictionary<ThDrainageSDTreeNode, int> allNodeDiaDict)
        {
            calculateEachDiameter(node, toiDict, allNodeDiaDict);

            foreach (var c in node.Child)
            {
                calculateTreeDiameter(c, toiDict, allNodeDiaDict);
            }
        }
        private static void calculateEachDiameter(ThDrainageSDTreeNode node, Dictionary<ThDrainageSDTreeNode, ThTerminalToilet> toiDict, Dictionary<ThDrainageSDTreeNode, int> allNodeDiaDict)
        {
            var alpha = 1.5;
            var specialToi = "蹲便器";
            var specialToiFlow = 1.2;

            var leaf = node.getLeaf();
            var leafType = new List<ThTerminalToilet>();
            leaf.ForEach(x =>
            {
                var bToi = toiDict.TryGetValue(x, out var toi);
                if (bToi == true)
                {
                    leafType.Add(toi);
                }
            });

            //计算流量
            double q = 0;
            if (leafType.Count > 0)
            {
                //var sum = leaf.Select(x =>  ThDrainageADCommon.cool_supply_equivalent[toiDict[x].Type]).Sum();
                var sum = leafType.Select(x => ThDrainageADCommon.cool_supply_equivalent[x.Type]).Sum();

                q = 0.2 * alpha * Math.Sqrt(sum);

                //修正流量
                ////设计流量
                var qDesign = leafType.Select(x => ThDrainageADCommon.cool_supply_flow[x.Type]).ToList();
                var leafToi = leafType.Where(x => x.Type == specialToi);
                var toiCount = leafToi.Count() > 0 ? 1 : 0;
                q = q + toiCount * specialToiFlow;
                double adjustQ = -1;

                if (q < qDesign.Max())
                {
                    adjustQ = qDesign.Max();
                }
                if (q > qDesign.Sum())
                {
                    adjustQ = qDesign.Sum();
                }
                if (adjustQ == -1)
                {
                    adjustQ = q;
                }
            }
            var miuCalculateList = ThDrainageADCommon.cool_supply_flow_diam.ToDictionary(x => x.Key, x => q / (1.0 / 4 * Math.PI * Math.Pow(x.Value, 2) * 1000));

            foreach (var miuCalculatePair in miuCalculateList)
            {
                //检查管径
                var minFlow = ThDrainageADCommon.cool_supply_diamFlowRange[miuCalculatePair.Key];
                if (minFlow.Item1 < miuCalculatePair.Value && miuCalculatePair.Value <= minFlow.Item2)
                {
                    allNodeDiaDict.Add(node, miuCalculatePair.Key);
                    break;
                }
            }

        }

        public static void selectChangeNode(ThDrainageSDTreeNode node, Dictionary<ThDrainageSDTreeNode, int> allNodeDiaDict, Dictionary<ThDrainageSDTreeNode, int> nodeDia)
        {
            if (node.Child.Count > 0)
            {
                if (node.Child.Count > 1 || node.Child.First().Child.Count != 0)
                {
                    var childDia = node.Child.Where(x =>
                    {
                        var bReturn = false;
                        if (allNodeDiaDict.TryGetValue(x, out var childNum) && allNodeDiaDict.TryGetValue(node, out var nodeNum))
                        {
                            if (childNum != nodeNum)
                            {
                                bReturn = true;
                            }
                        }
                        return bReturn;
                    });
                    if (childDia.Count() > 0)
                    {
                        var curr = node;

                        while (curr.Parent != null && curr.Parent.Node.DistanceTo(curr.Node) < ThDrainageADCommon.tol_diaDim)
                        {
                            curr = curr.Parent;
                        }
                        addNodeDim(nodeDia, curr, allNodeDiaDict[curr]);
                    }
                }
            }
            foreach (var c in node.Child)
            {
                selectChangeNode(c, allNodeDiaDict, nodeDia);
            }
        }

        private static void addNodeDim(Dictionary<ThDrainageSDTreeNode, int> nodeDia, ThDrainageSDTreeNode node, int dia)
        {
            if (nodeDia.ContainsKey(node) == true)
            {
                if (nodeDia[node] < dia)
                {
                    nodeDia[node] = dia;
                }
            }
            else
            {
                nodeDia.Add(node, dia);
            }
        }

        public static List<ThDrainageSDADBlkOutput> calculatePositionDiaDim(Dictionary<ThDrainageSDTreeNode, int> nodeDia, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertNodeDict)
        {
            var sDN = ThDrainageADCommon.diameterDN_visi_pre;
            var moveX = ThDrainageADCommon.diameterDim_move_x;
            var moveY = ThDrainageADCommon.diameterDim_move_y;
            var blk_name = ThDrainageADCommon.blkName_dim;
            var visiPropertyName = ThDrainageADCommon.visiName_valve;

            var output = new List<ThDrainageSDADBlkOutput>();

            foreach (var node in nodeDia)
            {
                if (node.Key.Parent != null)
                {
                    var s = convertNodeDict[node.Key].Node;
                    var e = convertNodeDict[node.Key.Parent].Node;

                    var dir = (e - s).GetNormal();
                    //var pt = new Point3d((s.X + e.X) / 2, (s.Y + e.Y) / 2, 0);
                    var angle = dir.GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);
                    if (179 * Math.PI / 180 <= angle && angle <= 359 * Math.PI / 180)
                    {
                        var tempPt = e;
                        e = s;
                        s = tempPt;
                    }
                    dir = (e - s).GetNormal();
                    var pt = s;

                    var dimPt = pt + (dir * moveX) + moveY * dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis);

                    var thModel = new ThDrainageSDADBlkOutput(dimPt);
                    thModel.name = blk_name;
                    thModel.dir = dir;
                    thModel.visibility.Add(visiPropertyName, sDN + node.Value.ToString());
                    thModel.scale = ThDrainageADCommon.blk_scale_end;

                    output.Add(thModel);
                }
            }

            return output;

        }

        public static List<ThDrainageSDADBlkOutput> calculatePositionDiaDimEnd(Dictionary<ThDrainageSDTreeNode, int> allNodeDiaDict, Dictionary<ThDrainageSDTreeNode, List<Line>> endStackPipe)
        {
            var sDN = ThDrainageADCommon.diameterDN_visi_pre;
            var moveX = ThDrainageADCommon.diameterDim_move_x;
            var moveY = ThDrainageADCommon.diameterDim_move_y;
            var blk_name = ThDrainageADCommon.blkName_dim;
            var visiPropertyName = ThDrainageADCommon.visiName_valve;

            var output = new List<ThDrainageSDADBlkOutput>();

            foreach (var end in endStackPipe)
            {
                var s = end.Value.Last().EndPoint;
                var e = end.Value.First().StartPoint;

                var dir = (e - s).GetNormal();
                var pt = s;

                var dimPt = pt + (dir * moveX) + moveY * dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis);

                var thModel = new ThDrainageSDADBlkOutput(dimPt);
                thModel.name = blk_name;
                thModel.dir = dir;
                thModel.visibility.Add(visiPropertyName, sDN + allNodeDiaDict[end.Key].ToString());
                thModel.scale = ThDrainageADCommon.blk_scale_end;

                output.Add(thModel);
            }

            return output;
        }
    }
}
