using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.DrainageSystemDiagram.Model;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDShutValveEngine
    {
        public static List<ThDrainageSDADBlkOutput> getShutValvePoint(ThDrainageSDDataExchange dataset)
        {
            var shutValveList = new List<ThDrainageSDADBlkOutput>();

            if (dataset.PipeTreeRoot != null)
            {
                ThDrainageSDTreeNode root = dataset.PipeTreeRoot;

                List<ThDrainageSDTreeNode> cutList = new List<ThDrainageSDTreeNode>();

                cutTree(root, cutList, dataset);

                //repair tree
                cutList.ForEach(x => x.Parent.Child.Add(x));
                cutList.ForEach(x => DrawUtils.ShowGeometry(x.Node, "l30shutValvePt", 50, 35, 50, "C"));

                shutValveList = cutList.Select(x => ToShutValve(x)).ToList();

            }
            return shutValveList;
        }

        private static ThDrainageSDADBlkOutput ToShutValve(ThDrainageSDTreeNode node)
        {
            int moveLength = 200;
            Vector3d dir = (node.Parent.Node - node.Node).GetNormal();
            Point3d pt = node.Node + moveLength * dir;

            var valve = new ThDrainageSDADBlkOutput(pt);
            valve.Dir = dir;
            valve.Name = ThDrainageSDCommon.Blk_ShutValves;
            valve.BlkSize = ThDrainageSDCommon.Blk_size_ShutValves;
            valve.Scale = ThDrainageSDCommon.Blk_scale_ShutValves;

            return valve;
        }

        private static void cutTree(ThDrainageSDTreeNode node, List<ThDrainageSDTreeNode> cutList, ThDrainageSDDataExchange dataset)
        {
            int leafNum = 10;
            var bEnd = false;
            var currNode = node;

            var islandGroupList = getIslandGroupList(dataset.GroupList, dataset.IslandPair);

            if (dataset.TerminalList.Count < leafNum)
            {
                while (bEnd == false)
                {
                    var childNum = currNode.Child.Count();
                    if (childNum == 1)
                    {
                        currNode = currNode.Child.First();
                    }
                    else
                    {
                        bEnd = true;
                    }
                }

                currNode = ifNodeLineTooShort(currNode, cutList);
                cutList.Add(currNode);
            }

            while (bEnd == false)
            {
                var chDict = currNode.Child.ToDictionary(x => x, x => getLeafCountWithCondition(x, cutList));
                var childCount = chDict.Where(x => x.Value >= leafNum).ToDictionary(x => x.Key, x => x.Value);

                if (childCount.Count == 0 && getLeafCountWithCondition(currNode, cutList) >= leafNum)
                {
                    currNode = ifInIsland(currNode, dataset.TerminalList, islandGroupList);
                    currNode = ifChildHasAllHandWashSink(currNode, dataset.TerminalList);
                    currNode = ifNodeLineTooShort(currNode, cutList);

                    if (currNode.Parent != null)
                    {
                        cutList.Add(currNode);
                        currNode.Parent.Child.Remove(currNode);
                        var allRoot = findRoot(currNode);
                        currNode = allRoot;
                    }
                }
                else
                {
                    currNode = childCount.First().Key;
                }

                if (childCount.Count == 0 && getLeafCountWithCondition(currNode, cutList) < leafNum)
                {
                    bEnd = true;
                }
            }
        }

        private static ThDrainageSDTreeNode findRoot(ThDrainageSDTreeNode node)
        {
            ThDrainageSDTreeNode allroot = null;

            var curr = node;
            while (curr.Parent != null)
            {
                curr = curr.Parent;
            }
            allroot = curr;
            return allroot;
        }

        private static ThDrainageSDTreeNode ifNodeLineTooShort(ThDrainageSDTreeNode node, List<ThDrainageSDTreeNode> cutList)
        {
            int moveLength = 200;
            int leafNum = 10;

            var currNode = node;
            var bEnd = false;
            while (bEnd == false)
            {
                if (currNode.Parent != null)
                {
                    var length = (currNode.Parent.Node - node.Node).Length;
                    var sibling = node.getSibling();
                    var other = sibling.Sum(x => getLeafCountWithCondition(x, cutList));

                    if (length < moveLength * 2 && other < leafNum && currNode.Parent.Parent  != null)
                    {
                        currNode = currNode.Parent;
                    }
                    else
                    {
                        bEnd = true;
                    }
                }
                else
                {
                    bEnd = true;
                }
            }

            return currNode;
        }

        private static ThDrainageSDTreeNode ifInIsland(ThDrainageSDTreeNode node, List<ThTerminalToilet> terminalList, List<List<ThTerminalToilet>> islandGroupList)
        {
            ThDrainageSDTreeNode newNode = node;

            if (islandGroupList.Count > 0)
            {
                var leaf = node.getLeaf();
                var toiletLeaf = leaf.Select(x => matchPipeEndTerminal(x, terminalList)).ToList();

                foreach (var toi in toiletLeaf)
                {
                    var group = islandGroupList.Where(x => x.Contains(toi)).FirstOrDefault();
                    if (group != null && group.Count > 0)
                    {
                        var notInLeaf = group.Where(x => toiletLeaf.Contains(x) == false).ToList();
                        if (notInLeaf.Count > 0)
                        {
                            newNode = node.Parent;
                            break;
                        }
                    }
                }
            }

            return newNode;
        }

        private static ThDrainageSDTreeNode ifChildHasAllHandWashSink(ThDrainageSDTreeNode node, List<ThTerminalToilet> terminalList)
        {
            ThDrainageSDTreeNode newNode = node;
            List<string> HandWashSinkType = new List<string>() { "A-Toilet-1", "A-Toilet-2", "A-Toilet-3", "A-Toilet-4" };

            if (node.Parent != null)
            {
                var siblingLeaf = node.getSibling().SelectMany(x => x.getLeaf());
                var toiletSibling = siblingLeaf.Select(x => matchPipeEndTerminal(x, terminalList)).ToList();

                if (toiletSibling.Count > 0)
                {
                    var toiletNotHandWashSink = toiletSibling.Where(x => x!=null && HandWashSinkType.Contains(x.Type) == false);
                    if (toiletNotHandWashSink.Count() == 0)
                    {
                        newNode = node.Parent;
                    }
                }
            }

            return newNode;
        }

        private static List<List<ThTerminalToilet>> getIslandGroupList(Dictionary<string, List<ThTerminalToilet>> GroupList, Dictionary<string, (string, string)> IslandPair)
        {
            var islandGroupList = new List<List<ThTerminalToilet>>();

            List<string> islandTraversal = new List<string>();

            foreach (var island in IslandPair)
            {
                if (islandTraversal.Contains(island.Key) == false)
                {

                    var group = new List<ThTerminalToilet>();
                    group.AddRange(GroupList[island.Value.Item1]);
                    group.AddRange(GroupList[island.Value.Item2]);
                    islandGroupList.Add(group);

                    islandTraversal.Add(island.Value.Item1);
                    islandTraversal.Add(island.Value.Item2);
                }
            }
            return islandGroupList;
        }

        private static int getLeafCountWithCondition(ThDrainageSDTreeNode node, List<ThDrainageSDTreeNode> cutList)
        {
            int i = 0;
            i = node.getLeafCount();
            i = i - DescendantCount(node, cutList);

            return i;
        }

        private static int DescendantCount(ThDrainageSDTreeNode node, List<ThDrainageSDTreeNode> cutList)
        {
            int i = 0;

            if (cutList.Contains(node))
            {
                i = node.getLeafCount();
            }
            else
            {
                foreach (var cut in cutList)
                {
                    if (cut.isDescendant(node))
                    {
                        i = i - cut.getLeafCount();
                    }
                }
            }
            return i;

        }

        private static ThTerminalToilet matchPipeEndTerminal(ThDrainageSDTreeNode node, List<ThTerminalToilet> terminalList)
        {
            var tol = new Tolerance(10, 10);

            var terminal = terminalList.Where(x => x.SupplyCoolOnWall.Where(pt => pt.IsEqualTo(node.Node, tol)).Count() > 0).FirstOrDefault();
            return terminal;
        }

        public static List<Line> cutPipe(List<ThDrainageSDADBlkOutput> valveList, List<Line> pipes)
        {
            var tol = new Tolerance(1, 1);
            var finalLine = new List<Line>();

            if (valveList != null && valveList.Count > 0)
            {
                foreach (var valve in valveList)
                {
                    var line = pipes.Where(x => x.ToCurve3d().IsOn(valve.Position, tol)).ToList();
                    if (line.Count > 0)
                    {
                        var l = line.First();
                        var dir = valve.Dir;
                        double scale = valve.Scale;
                        double blkSize = valve.BlkSize;
                        var blkS = valve.Position - dir * scale * blkSize / 2;
                        var blkE = valve.Position + dir * scale * blkSize / 2;
                        var newE = l.StartPoint.DistanceTo(blkS) < l.StartPoint.DistanceTo(blkE) ? blkS : blkE;
                        var newE2 = blkS == newE ? blkE : blkS;


                        var cutpart1 = new Line(l.StartPoint, newE);
                        var cutpart2 = new Line(newE2, l.EndPoint);
                        finalLine.Add(cutpart1);
                        finalLine.Add(cutpart2);
                        pipes.Remove(l);
                    }
                }
            }

            finalLine.AddRange(pipes);

            return finalLine;
        }
    }
}

