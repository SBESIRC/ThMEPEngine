using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDShutValveEngine
    {
        public static List<KeyValuePair<Point3d, Vector3d>> getShutValvePoint(ThDrainageSDDataExchange dataset)
        {
            var shutValveList = new List<KeyValuePair<Point3d, Vector3d>>();

            if (dataset.PipeTreeRoot != null)
            {
                ThDrainageSDTreeNode root = dataset.PipeTreeRoot;

                //ThDrainageSDTreeService.travelTree(root, "l30tree");

                List<ThDrainageSDTreeNode> cutList = new List<ThDrainageSDTreeNode>();

                cutTree(root, cutList, dataset);

                //repair tree
                cutList.ForEach(x => x.Parent.Child.Add(x));
                cutList.ForEach(x => DrawUtils.ShowGeometry(x.Node, "l30shutValvePt", 50, 35, 50, "C"));

                shutValveList = cutList.Select(x => ToShutValve(x)).ToList();

            }
            return shutValveList;
        }

        private static KeyValuePair<Point3d, Vector3d> ToShutValve(ThDrainageSDTreeNode node)
        {
            int moveLength = 200;
            Vector3d dir = (node.Parent.Node - node.Node).GetNormal();

            Point3d pt = node.Node + moveLength * dir;

            var valve = new KeyValuePair<Point3d, Vector3d>(pt, dir);

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

                    cutList.Add(currNode);
                    currNode.Parent.Child.Remove(currNode);
                    var allRoot = findRoot(currNode);
                    currNode = allRoot;
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

                    if (length < moveLength * 2 && other < leafNum)
                    {
                        currNode = currNode.Parent;
                    }
                    else
                    {
                        bEnd = true;
                    }
                }
            }

            return currNode;
        }

        private static ThDrainageSDTreeNode ifInIsland(ThDrainageSDTreeNode node, List<ThIfcSanitaryTerminalToilate> terminalList, List<List<ThIfcSanitaryTerminalToilate>> islandGroupList)
        {
            ThDrainageSDTreeNode newNode = node;

            if (islandGroupList.Count > 0)
            {
                var leaf = node.getLeaf();
                var toilateLeaf = leaf.Select(x => matchPipeEndTerminal(x, terminalList)).ToList();

                foreach (var toi in toilateLeaf)
                {
                    var group = islandGroupList.Where(x => x.Contains(toi)).FirstOrDefault();
                    if (group != null && group.Count > 0)
                    {
                        var notInLeaf = group.Where(x => toilateLeaf.Contains(x) == false).ToList();
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

        private static ThDrainageSDTreeNode ifChildHasAllHandWashSink(ThDrainageSDTreeNode node, List<ThIfcSanitaryTerminalToilate> terminalList)
        {
            ThDrainageSDTreeNode newNode = node;
            List<string> HandWashSinkType = new List<string>() { "A-Toilet-1", "A-Toilet-2", "A-Toilet-3", "A-Toilet-4" };

            if (node.Parent != null)
            {
                var siblingLeaf = node.getSibling().SelectMany(x => x.getLeaf());
                var toilateSibling = siblingLeaf.Select(x => matchPipeEndTerminal(x, terminalList)).ToList();

                if (toilateSibling.Count > 0)
                {
                    var toilateNotHandWashSink = toilateSibling.Where(x => HandWashSinkType.Contains(x.Type) == false);
                    if (toilateNotHandWashSink.Count() == 0)
                    {
                        newNode = node.Parent;
                    }
                }
            }

            return newNode;
        }

        private static List<List<ThIfcSanitaryTerminalToilate>> getIslandGroupList(Dictionary<string, List<ThIfcSanitaryTerminalToilate>> GroupList, Dictionary<string, (string, string)> IslandPair)
        {
            var islandGroupList = new List<List<ThIfcSanitaryTerminalToilate>>();

            List<string> islandTraversal = new List<string>();

            foreach (var island in IslandPair)
            {
                if (islandTraversal.Contains(island.Key) == false)
                {

                    var group = new List<ThIfcSanitaryTerminalToilate>();
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

        private static ThIfcSanitaryTerminalToilate matchPipeEndTerminal(ThDrainageSDTreeNode node, List<ThIfcSanitaryTerminalToilate> terminalList)
        {
            var tol = new Tolerance(10, 10);

            var terminal = terminalList.Where(x => x.SupplyCoolOnWall.Where(pt => pt.IsEqualTo(node.Node, tol)).Count() > 0).FirstOrDefault();
            return terminal;
        }
    }
}

