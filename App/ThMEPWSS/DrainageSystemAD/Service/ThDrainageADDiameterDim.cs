using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.DrainageSystemDiagram.Model;

namespace ThMEPWSS.DrainageSystemDiagram.Service
{
    public class ThDrainageADDiameterDim
    {
        public static void calculateTreeDiameter(ThDrainageSDTreeNode node, Dictionary<ThDrainageSDTreeNode, ThTerminalToilet> toiDict, Dictionary<ThDrainageSDTreeNode, int> allNodeDiaDict, double alpha)
        {
            calculateEachDiameter(node, toiDict, allNodeDiaDict, alpha);

            foreach (var c in node.Child)
            {
                calculateTreeDiameter(c, toiDict, allNodeDiaDict, alpha);
            }
        }

        private static void calculateEachDiameter(ThDrainageSDTreeNode node, Dictionary<ThDrainageSDTreeNode, ThTerminalToilet> toiDict, Dictionary<ThDrainageSDTreeNode, int> allNodeDiaDict, double alpha)
        {
            //var alpha = 1.5;
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
            if (leafType.Count == 1)
            {
                //支管。直接读数据
                var subPipe = ThDrainageADCommon.cool_supply_leafPipeDiam[leafType[0].Type];
                allNodeDiaDict.Add(node, subPipe);
            }
            else if (leafType.Count > 1)
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

        public static List<ThDrainageSDADBlkOutput> calculatePositionDiaDim(Dictionary<ThDrainageSDTreeNode, int> nodeDia, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertNodeDict, List<Line> allIsolateLine)
        {
            var sDN = ThDrainageADCommon.diameterDN_visi_pre;
            var dimBlkX = ThDrainageADCommon.diameterDim_blk_x;
            var dimBlkY = ThDrainageADCommon.diameterDim_blk_y;
            var blk_name = ThDrainageADCommon.blkName_dim;
            var visiPropertyName = ThDrainageADCommon.visiName_valve;

            var alreadyDimArea = new List<Polyline>();
            var output = new List<ThDrainageSDADBlkOutput>();

            foreach (var node in nodeDia)
            {
                if (node.Key.Parent != null)
                {
                    var s = convertNodeDict[node.Key].Node;
                    var e = convertNodeDict[node.Key.Parent].Node;

                    var dir = (e - s).GetNormal();

                    var angle = dir.GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);
                    if (179 * Math.PI / 180 <= angle && angle <= 359 * Math.PI / 180)
                    {
                        var tempPt = e;
                        e = s;
                        s = tempPt;
                        dir = (e - s).GetNormal();
                    }

                    var dimPtsTemp = PossiblePosition(s, e);
                    var dimOutlineList = dimPtsTemp.Select(x => ToDimOutline(x, dir, dimBlkX, dimBlkY)).ToList();
                    var dimOutline = ThDrainageSDDimService.GetDimOptimalArea(dimOutlineList, allIsolateLine, alreadyDimArea);
                    alreadyDimArea.Add(dimOutline);
                    var dimPt = dimOutline.StartPoint;

                    var thModel = new ThDrainageSDADBlkOutput(dimPt);
                    thModel.Name = blk_name;
                    thModel.Dir = dir;
                    thModel.Visibility.Add(visiPropertyName, sDN + node.Value.ToString());
                    thModel.Scale = ThDrainageADCommon.blk_scale_end;

                    output.Add(thModel);
                }
            }

            if (alreadyDimArea.Count > 0)
            {
                //add last one for end-dim 
                var alreadyDimAreaLine = ThDrainageSDCommonService.GetLines(alreadyDimArea.Last());
                allIsolateLine.AddRange(alreadyDimAreaLine);
            }



            return output;

        }

        public static List<ThDrainageSDADBlkOutput> calculatePositionDiaDimEnd(Dictionary<ThDrainageSDTreeNode, int> allNodeDiaDict, Dictionary<ThDrainageSDTreeNode, List<Line>> endStackPipe, List<Line> allIsolateLine)
        {
            var sDN = ThDrainageADCommon.diameterDN_visi_pre;
            var dimBlkX = ThDrainageADCommon.diameterDim_blk_x;
            var dimBlkY = ThDrainageADCommon.diameterDim_blk_y;
            var blk_name = ThDrainageADCommon.blkName_dim;
            var visiPropertyName = ThDrainageADCommon.visiName_valve;
            var alreadyDimArea = new List<Polyline>();
            var output = new List<ThDrainageSDADBlkOutput>();

            foreach (var end in endStackPipe)
            {
                var s = end.Value.Last().EndPoint;
                var e = end.Value.First().StartPoint;

                var dir = (e - s).GetNormal();

                var dimPtsTemp = PossiblePosition(s, e);
                var dimOutlineList = dimPtsTemp.Select(x => ToDimOutline(x, dir, dimBlkX, dimBlkY)).ToList();
                DrawUtils.ShowGeometry(dimOutlineList, "l0dimOutline", 95);
                var dimOutline = ThDrainageSDDimService.GetDimOptimalArea(dimOutlineList, allIsolateLine, alreadyDimArea);
                alreadyDimArea.Add(dimOutline);
                var dimPt = dimOutline.StartPoint;

                var thModel = new ThDrainageSDADBlkOutput(dimPt);
                thModel.Name = blk_name;
                thModel.Dir = dir;
                thModel.Visibility.Add(visiPropertyName, sDN + allNodeDiaDict[end.Key].ToString());
                thModel.Scale = ThDrainageADCommon.blk_scale_end;

                output.Add(thModel);
            }

            return output;
        }

        /// <summary>
        ///根据给定线和位置返回直径标记块的插入点
        ///0:线左，中点
        ///1:线左，起点
        ///2:线右，中点
        ///3:线右，起点
        ///4:线左，终点
        ///5：线右，终点
        /// </summary>
        /// <param name="baseLineS"></param>
        /// <param name="baseLineE"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static Point3d calculatePositionDia(Point3d baseLineS, Point3d baseLineE, int position)
        {
            var dir = (baseLineE - baseLineS).GetNormal();
            var pt = new Point3d();
            double allMoveX = 0;
            double allMoveY = 0;
            var moveX = ThDrainageADCommon.diameterDim_move_x;
            var moveY = ThDrainageADCommon.diameterDim_move_y;
            var dimBlkX = ThDrainageADCommon.diameterDim_blk_x;
            var dimBlkY = ThDrainageADCommon.diameterDim_blk_y;

            if (position == 0)
            {
                pt = new Point3d((baseLineS.X + baseLineE.X) / 2, (baseLineS.Y + baseLineE.Y) / 2, 0);
                allMoveX = -dimBlkX / 2;
                allMoveY = moveY;
            }
            if (position == 1)
            {
                pt = baseLineS;
                allMoveX = moveX;
                allMoveY = moveY;
            }
            if (position == 2)
            {
                pt = new Point3d((baseLineS.X + baseLineE.X) / 2, (baseLineS.Y + baseLineE.Y) / 2, 0);
                allMoveX = -dimBlkX / 2;
                allMoveY = -dimBlkY - moveY;
            }
            if (position == 3)
            {
                pt = baseLineS;
                allMoveX = moveX;
                allMoveY = -dimBlkY - moveY;
            }
            if (position == 4)
            {
                pt = baseLineS;
                allMoveX = (baseLineE - baseLineS).Length - moveX - dimBlkX;
                allMoveY = moveY;
            }
            if (position == 5)
            {
                pt = baseLineS;
                allMoveX = (baseLineE - baseLineS).Length - moveX - dimBlkX;
                allMoveY = -dimBlkY - moveY;
            }

            var dimPt = pt + dir * allMoveX + allMoveY * dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis);
            return dimPt;
        }

        public static Polyline ToDimOutline(Point3d pt, Vector3d dir, double x, double y)
        {
            var outline = new Polyline();
            var pt0 = pt;
            var pt1 = pt + dir * x;
            var pt2 = pt1 + dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis).GetNormal() * y;
            var pt3 = pt2 - dir * x;

            outline.AddVertexAt(outline.NumberOfVertices, pt0.ToPoint2D(), 0, 0, 0);
            outline.AddVertexAt(outline.NumberOfVertices, pt1.ToPoint2D(), 0, 0, 0);
            outline.AddVertexAt(outline.NumberOfVertices, pt2.ToPoint2D(), 0, 0, 0);
            outline.AddVertexAt(outline.NumberOfVertices, pt3.ToPoint2D(), 0, 0, 0);
            outline.Closed = true;

            return outline;
        }

        private static List<Point3d> PossiblePosition(Point3d baseLineS, Point3d baseLineE)
        {
            var dimPts = new List<Point3d>();

            //左边中点位
            dimPts.Add(calculatePositionDia(baseLineS, baseLineE, 0));
            //右边中点位
            dimPts.Add(calculatePositionDia(baseLineS, baseLineE, 3));
            //左边起点位
            dimPts.Add(calculatePositionDia(baseLineS, baseLineE, 1));
            //右边起点位
            dimPts.Add(calculatePositionDia(baseLineS, baseLineE, 2));
            //左边终点
            dimPts.Add(calculatePositionDia(baseLineS, baseLineE, 4));
            //右边终点
            dimPts.Add(calculatePositionDia(baseLineS, baseLineE, 5));

            return dimPts;
        }
    }
}
