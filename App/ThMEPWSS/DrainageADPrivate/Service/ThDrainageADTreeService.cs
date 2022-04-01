using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThDrainageADTreeService
    {
        public static Dictionary<Point3d, List<Line>> GetPtDict(List<Line> lines)
        {
            var ptDict = new Dictionary<Point3d, List<Line>>();
            foreach (var line in lines)
            {
                var pt = line.StartPoint;

                var key = IsInDict(pt, ptDict);
                if (key == Point3d.Origin)
                {
                    ptDict.Add(pt, new List<Line> { line });
                }
                else
                {
                    ptDict[key].Add(line);
                }

                pt = line.EndPoint;

                key = IsInDict(pt, ptDict);
                if (key == Point3d.Origin)
                {
                    ptDict.Add(pt, new List<Line> { line });
                }
                else
                {
                    ptDict[key].Add(line);
                }
            }

            return ptDict;
        }
        public static void GetEndTerminal(Dictionary<Point3d, List<Line>> ptDict, List<ThSaniterayTerminal> terminalList, out Dictionary<Point3d, ThSaniterayTerminal> ptTerminal, out List<Point3d> ptStart)
        {
            ptTerminal = new Dictionary<Point3d, ThSaniterayTerminal>();
            ptStart = new List<Point3d>();

            var endPtList = ptDict.Where(x => x.Value.Count == 1).ToList();
            foreach (var endPt in endPtList)
            {
                var t = FindTerminal(endPt.Key, terminalList);
                if (t != null)
                {
                    ptTerminal.Add(endPt.Key, t);
                }
                else
                {
                    ptStart.Add(endPt.Key);
                }
            }
        }
        private static ThSaniterayTerminal FindTerminal(Point3d pt, List<ThSaniterayTerminal> terminal)
        {
            ThSaniterayTerminal endTerminal = null;
            var tol = 500;
            var projPt = new Point3d(pt.X, pt.Y, 0);
            var orderTerminal = terminal.OrderBy(x => x.Boundary.DistanceTo(projPt, false)).First();

            if (orderTerminal.Boundary.ContainsOrOnBoundary(projPt))
            {
                endTerminal = orderTerminal;
            }
            else
            {
                if (orderTerminal.Boundary.DistanceTo(projPt, false) < tol)
                {
                    endTerminal = orderTerminal;
                }
            }

            return endTerminal;

        }

        private static Point3d IsInDict(Point3d pt, Dictionary<Point3d, List<Line>> ptDict)
        {
            var key = new Point3d();
            var tol = new Tolerance(1, 1);
            var dict = ptDict.Where(x => x.Key.IsEqualTo(pt, tol));
            if (dict.Count() > 0)
            {
                key = dict.First().Key;
            }
            return key;
        }

        public static Dictionary<Point3d, bool> CheckCoolHotPt(List<Point3d> ptStart, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal, Dictionary<Point3d, List<Line>> ptDict, ThDrainageADPDataPass datapass)
        {
            var ptCoolHotDict = new Dictionary<Point3d, bool>();

            foreach (var pt in ptStart)
            {
                var isCool = CheckCoolHotPt(pt, ptDict, datapass);
                ptCoolHotDict.Add(pt, isCool);
            }
            foreach (var pt in ptTerminal)
            {
                if (pt.Value.Type == ThDrainageADCommon.TerminalType.WaterHeater)
                {
                    var isCool = CheckCoolHotPt(pt.Key, ptDict, datapass);
                    ptCoolHotDict.Add(pt.Key, isCool);
                }
            }

            return ptCoolHotDict;
        }
        private static bool CheckCoolHotPt(Point3d pt, Dictionary<Point3d, List<Line>> ptDict, ThDrainageADPDataPass datapass)
        {
            var isCool = false;
            var tol = new Tolerance(1, 1);
            ptDict.TryGetValue(pt, out var lines);

            if (lines != null && lines.Count > 0)
            {
                if (lines.Where(x => datapass.CoolPipeTopView.Contains(x)).Any())
                {
                    isCool = true;
                }
                else if (lines.Where(x => datapass.HotPipeTopView.Contains(x)).Any())
                {
                    isCool = false;
                }
                else if (lines.Where(x => datapass.VerticalPipe.Contains(x)).Any())
                {
                    var ptOther = lines[0].EndPoint;
                    if (pt.IsEqualTo(ptOther, tol))
                    {
                        ptOther = lines[0].StartPoint;
                    }
                    var ptKey = IsInDict(ptOther, ptDict);
                    if (ptKey != Point3d.Origin)
                    {
                        var connLine = ptDict[ptKey];
                        if (connLine.Where(x => datapass.CoolPipeTopView.Contains(x)).Any())
                        {
                            isCool = true;
                        }
                        else if (connLine.Where(x => datapass.HotPipeTopView.Contains(x)).Any())
                        {
                            isCool = false;
                        }
                    }
                }
            }

            return isCool;

        }

        public static List<ThDrainageTreeNode> BuildTree(List<Point3d> startPtList, Dictionary<Point3d, List<Line>> ptDict)
        {
            var treeList = new List<ThDrainageTreeNode>();
            var traversed = ptDict.SelectMany(x => x.Value).Distinct().ToDictionary(x => x, x => 0);

            foreach (var startPt in startPtList)
            {
                var rootNode = new ThDrainageTreeNode(startPt);
                FindNextLeaf(rootNode, ptDict, traversed);

                treeList.Add(rootNode);
            }

            return treeList;
        }

        private static void FindNextLeaf(ThDrainageTreeNode thisNode, Dictionary<Point3d, List<Line>> ptDict, Dictionary<Line, int> traversed)
        {
            var tol = new Tolerance(1, 1);
            var thisNodePt = thisNode.Node;
            var linkPt = IsInDict(thisNodePt, ptDict);

            if (linkPt != Point3d.Origin)
            {
                var toLine = ptDict[linkPt].Where(x => traversed[x] == 0).ToList();

                foreach (var l in toLine)
                {
                    traversed[l] = 1;
                    var theOtherEnd = l.EndPoint;
                    if (theOtherEnd.IsEqualTo(thisNodePt, tol))
                    {
                        var startPt = l.StartPoint;
                        l.StartPoint = thisNodePt;
                        l.EndPoint = startPt;
                        theOtherEnd = l.EndPoint;
                    }
                    var otherKey = IsInDict(theOtherEnd, ptDict);
                    if (otherKey != Point3d.Origin)
                    {
                        var child = new ThDrainageTreeNode(otherKey);
                        thisNode.Child.Add(child);
                        child.Parent = thisNode;
                        FindNextLeaf(child, ptDict, traversed);
                    }

                }
            }
        }

        public static void SetTerminal(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal)
        {
            var allLeafStart = rootList.SelectMany(x => x.GetLeaf()).ToList();
            allLeafStart.AddRange(rootList);//热水起点热水器不是leaf
            foreach (var l in allLeafStart)
            {
                if (ptTerminal.TryGetValue(l.Node, out var t))
                {
                    l.Terminal = t;
                }
            }
        }
        public static void SetTerminalPair(List<ThDrainageTreeNode> rootList)
        {
            var disTol = 250;
            var allLeafStart = rootList.SelectMany(x => x.GetLeaf()).ToList();
            allLeafStart.AddRange(rootList);//热水起点热水器不是leaf

            foreach (var l in allLeafStart)
            {
                if (l.Terminal != null && l.TerminalPair == null)
                {
                    var pair = allLeafStart.Where(x => x.Terminal == l.Terminal && x != l);
                    if (pair.Count() > 0)
                    {
                        l.TerminalPair = pair.First();
                        pair.First().TerminalPair = l;
                    }
                    else
                    {
                        //有些不是一个块。直接用distance找
                        pair = allLeafStart.Where(x => x.Node.DistanceTo(l.Node) <= disTol && x != l && x.Terminal.Type == l.Terminal.Type).OrderBy(x => x.Node.DistanceTo(l.Node));
                        if (pair.Count() > 0)
                        {
                            l.TerminalPair = pair.First();
                            pair.First().TerminalPair = l;
                        }
                    }
                }
            }
        }
        public static void SetCoolHot(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, bool> ptCoolHotDict)
        {
            foreach (var tree in rootList)
            {
                if (ptCoolHotDict.TryGetValue(tree.Node, out var isCool))
                {
                    tree.IsCool = isCool;
                }
                SetCoolHot(tree);
            }
        }
        private static void SetCoolHot(ThDrainageTreeNode node)
        {
            node.Child.ForEach(x => x.IsCool = x.Parent.IsCool);
            node.Child.ForEach(x => SetCoolHot(x));
        }

        public static void PrintTree(ThDrainageTreeNode root, string layer)
        {
            int cs = root.GetLeafCount();
            int dp = root.GetDepth();
            DrawUtils.ShowGeometry(new Point3d(root.Node.X + 20, root.Node.Y, 0), string.Format("{0}_{1}_{2}", dp, cs, root.IsCool), layer, (short)(dp % 7), 25, 50);

            root.Child.ForEach(x => PrintTree(x, layer));
        }
    }
}
