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

        /// <summary>
        /// 立管：T 角阀：T 末端：T 结果：末端
        /// 立管：T 角阀：F 末端：F 结果：起点
        /// 立管：T 角阀：T 末端：F 结果：虚拟末端
        /// 立管：T 角阀：F 末端：T 结果：末端,默认角阀
        /// 立管：F 角阀：T 末端：T 结果：末端
        /// 立管：F 角阀：F 末端：F 结果：起点
        /// 立管：F 角阀：T 末端：F 结果：虚拟末端
        /// 立管：F 角阀：F 末端：T 结果：起点
        /// </summary>
        /// <param name="ptDict"></param>
        /// <param name="terminalList"></param>
        /// <param name="angleValve"></param>
        /// <param name="ptTerminal"></param>
        /// <param name="ptValve"></param>
        /// <param name="ptStart"></param>
        public static void GetEndTerminal(Dictionary<Point3d, List<Line>> ptDict, List<ThSaniterayTerminal> terminalList, List<ThValve> angleValve, out Dictionary<Point3d, ThSaniterayTerminal> ptTerminal, out Dictionary<Point3d, ThValve> ptValve, out List<Point3d> ptStart)
        {
            var r = 50.0;
            ptTerminal = new Dictionary<Point3d, ThSaniterayTerminal>();
            ptValve = new Dictionary<Point3d, ThValve>();
            ptStart = new List<Point3d>();

            var endPtList = ptDict.Where(x => x.Value.Count == 1).ToList();

            foreach (var endPt in endPtList)
            {
                var isVertical = IsVertical(endPt.Value[0]);
                var hasAngleValve = HasAngleValve(endPt.Key, angleValve, out var valve);

                if (isVertical == false && hasAngleValve == false)
                {
                    ptStart.Add(endPt.Key);
                    continue;
                }

                if (hasAngleValve == true)
                {
                    ptValve.Add(endPt.Key, valve);
                }

                var t = FindTerminal(endPt.Key, terminalList);
                if (t != null)
                {
                    ptTerminal.Add(endPt.Key, t);
                }
                else
                {
                    if (isVertical == true && hasAngleValve == false)
                    {
                        ptStart.Add(endPt.Key);
                        continue;
                    }
                    //找不到末端洁具的，造个虚拟洁具 type： unknow
                    Polyline pl;
                    if (valve != null)
                    {
                        //有角阀用角阀boundary
                        pl = valve.Boundary.Buffer(1).OfType<Polyline>().OrderByDescending(x => x.Area).First();
                    }
                    else
                    {
                        //没有角阀用点位前后100直径的框（无视方向）
                        pl = CreateSquare(endPt.Key, r);
                    }

                    var virtualTerminal = new ThSaniterayTerminal()
                    {
                        Boundary = pl,
                        Name = "VirtualTerminal",
                        Type = ThDrainageADCommon.TerminalType.Unknow,
                    };

                    ptTerminal.Add(endPt.Key, virtualTerminal);
                    terminalList.Add(virtualTerminal);
                }
            }
        }


        /// <summary>
        /// 是否立管。管线长度小于Tol_SamePoint的水平管也会被判定为true
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public static bool IsVertical(Line l)
        {
            var bReturn = false;
            var tol = ThDrainageADCommon.Tol_SamePoint;
            var zDelta = Math.Abs(l.StartPoint.Z - l.EndPoint.Z);
            var vertical = Math.Abs(zDelta - l.Length);

            if (vertical <= tol)
            {
                bReturn = true;
            }

            return bReturn;

        }

        /// <summary>
        /// 是否立管
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsVertical(ThDrainageTreeNode node)
        {
            var bReturn = false;
            var tol = ThDrainageADCommon.Tol_SamePoint;

            if (node.Parent != null && Math.Abs(node.Pt.Z - node.Parent.Pt.Z) > tol)
            {
                bReturn = true;
            }

            return bReturn;

        }


        private static bool HasAngleValve(Point3d pt, List<ThValve> angleValveList, out ThValve valve)
        {
            var tol = new Tolerance(ThDrainageADCommon.Tol_AngleValveToPipe, ThDrainageADCommon.Tol_AngleValveToPipe);
            var hasAngleValve = false;
            valve = null;
            var projPt = new Point3d(pt.X, pt.Y, 0);

            var endValve = angleValveList.Where(x => x.InsertPt.IsEqualTo(projPt, tol)).FirstOrDefault();
            if (endValve != null)
            {
                valve = endValve;
                hasAngleValve = true;
            }

            return hasAngleValve;
        }

        private static Polyline CreateSquare(Point3d pt, double r)
        {
            var sq = new Polyline();

            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X - r, pt.Y + r), 0, 0, 0);
            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X + r, pt.Y + r), 0, 0, 0);
            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X + r, pt.Y - r), 0, 0, 0);
            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X - r, pt.Y - r), 0, 0, 0);
            sq.Closed = true;

            return sq;
        }
        public static Dictionary<Point3d, Point3d> GetTerminalPairDict(Dictionary<Point3d, ThSaniterayTerminal> ptTerminal)
        {
            var disTol = ThDrainageADCommon.Tol_PipeEndPair;
            var terminalPairDict = new Dictionary<Point3d, Point3d>();

            var allPointDict = ptTerminal.ToDictionary(x => x.Key, x => 0);

            for (int i = 0; i < allPointDict.Count(); i++)
            {
                var pt = allPointDict.ElementAt(i);
                if (pt.Value == 1)
                {
                    continue;
                }

                var pair = allPointDict.Where(x => pt.Key != x.Key && x.Value == 0 && ptTerminal[pt.Key] == ptTerminal[x.Key]);
                if (pair.Count() > 0)
                {
                    terminalPairDict.Add(pt.Key, pair.First().Key);
                    allPointDict[pt.Key] = 1;
                    allPointDict[pair.First().Key] = 1;
                }
                else
                {
                    //有些不是一个块。直接用distance找
                    pair = allPointDict.Where(x => pt.Key != x.Key && x.Value == 0 &&
                                                    ptTerminal[pt.Key].Type == ptTerminal[x.Key].Type &&
                                                    x.Key.DistanceTo(pt.Key) <= disTol)
                                        .OrderBy(x => x.Key.DistanceTo(pt.Key));

                    if (pair.Count() > 0)
                    {
                        terminalPairDict.Add(pt.Key, pair.First().Key);
                        allPointDict[pt.Key] = 1;
                        allPointDict[pair.First().Key] = 1;
                    }
                }
            }

            return terminalPairDict;
        }

        private static ThSaniterayTerminal FindTerminal(Point3d pt, List<ThSaniterayTerminal> terminal)
        {
            ThSaniterayTerminal endTerminal = null;
            var tol = ThDrainageADCommon.Tol_PipeEndToTerminal;
            var projPt = new Point3d(pt.X, pt.Y, 0);

            //先找是否有被包含的 热水器优先度高
            var containedTerminal = terminal.Where(x => x.Boundary.ContainsOrOnBoundary(projPt)).OrderBy(x => x.Boundary.DistanceTo(projPt, false)).ToList();
            if (containedTerminal.Count() > 0)
            {
                var waterHeater = containedTerminal.Where(x => x.Type == ThDrainageADCommon.TerminalType.WaterHeater);
                if (waterHeater.Count() > 0)
                {
                    endTerminal = waterHeater.First();
                }
                else
                {
                    endTerminal = containedTerminal.First();
                }
            }
            if (endTerminal == null)
            {
                //没有被包含的找500内距离最近的,且不是热水器
                var orderTerminal = terminal.Where(x => x.Boundary.DistanceTo(projPt, false) < tol).OrderBy(x => x.Boundary.DistanceTo(projPt, false)).ToList();
                if (orderTerminal.Count() > 0 && orderTerminal.First().Type != ThDrainageADCommon.TerminalType.WaterHeater)
                {
                    endTerminal = orderTerminal.First();
                }
            }

            return endTerminal;

        }

        private static Point3d IsInDict(Point3d pt, Dictionary<Point3d, List<Line>> ptDict)
        {
            var key = new Point3d();
            var tol = new Tolerance(ThDrainageADCommon.Tol_SamePoint, ThDrainageADCommon.Tol_SamePoint);
            var dict = ptDict.Where(x => x.Key.IsEqualTo(pt, tol));
            if (dict.Count() > 0)
            {
                key = dict.First().Key;
            }
            return key;
        }

        /// <summary>
        /// 返回冷热起点
        /// </summary>
        /// <param name="ptStart"></param>
        /// <param name="ptTerminal"></param>
        /// <param name="PtCoolHotDict"></param>
        /// <returns></returns>
        public static Dictionary<Point3d, bool> CheckCoolHotStartPt(List<Point3d> ptStart, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal, Dictionary<Point3d, bool> PtCoolHotDict)
        {
            var ptCoolHotDict = new Dictionary<Point3d, bool>();

            foreach (var pt in ptStart)
            {
                if (PtCoolHotDict.TryGetValue(pt, out var isCool))
                {
                    ptCoolHotDict.Add(pt, isCool);
                }
            }
            foreach (var pt in ptTerminal)
            {
                if (pt.Value.Type == ThDrainageADCommon.TerminalType.WaterHeater)
                {
                    if (PtCoolHotDict.TryGetValue(pt.Key, out var isCool))
                    {
                        if (isCool == false)
                        {
                            ptCoolHotDict.Add(pt.Key, isCool);
                        }
                    }
                }
            }

            return ptCoolHotDict;
        }

        public static ThDrainageTreeNode BuildTree(Point3d startPt, Dictionary<Point3d, List<Line>> ptDict)
        {
            var traversed = ptDict.SelectMany(x => x.Value).Distinct().ToDictionary(x => x, x => 0);

            var rootNode = new ThDrainageTreeNode(startPt);
            FindNextLeaf(rootNode, ptDict, traversed);

            return rootNode;
        }

        private static void FindNextLeaf(ThDrainageTreeNode thisNode, Dictionary<Point3d, List<Line>> ptDict, Dictionary<Line, int> traversed)
        {
            var tol = new Tolerance(ThDrainageADCommon.Tol_SamePoint, ThDrainageADCommon.Tol_SamePoint);
            var thisNodePt = thisNode.Pt;

            var toLine = ptDict[thisNodePt].Where(x => traversed[x] == 0).ToList();
            foreach (var l in toLine)
            {
                traversed[l] = 1;
                var theOtherEnd = l.EndPoint;
                if (theOtherEnd.IsEqualTo(thisNodePt, tol))
                {
                    theOtherEnd = l.StartPoint;
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

        /// <summary>
        /// 拼接前。设定冷热
        /// </summary>
        /// <param name="rootList"></param>
        /// <param name="ptCoolHotDict"></param>
        public static void SetCoolHot(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, bool> ptCoolHotDict)
        {
            foreach (var tree in rootList)
            {
                if (ptCoolHotDict.TryGetValue(tree.Pt, out var isCool))
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

        public static void SetTerminal(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal)
        {
            foreach (var tree in rootList)
            {
                var leaf = tree.GetLeaf();
                foreach (var l in leaf)
                {
                    if (ptTerminal.TryGetValue(l.Pt, out var t))
                    {
                        l.Terminal = t;
                    }
                }
                if (tree.IsCool == false)
                {
                    //热水起点 热水器
                    if (ptTerminal.TryGetValue(tree.Pt, out var t))
                    {
                        tree.Terminal = t;
                    }
                }
            }
        }

        public static List<ThDrainageTreeNode> MergeCoolHotTree(List<ThDrainageTreeNode> rootList)
        {
            var rootListMerged = new List<ThDrainageTreeNode>();
            var rootListDict = rootList.ToDictionary(x => x, x => x.GetLeaf());
            var rootListHasTerminal = rootListDict.Where(x => x.Value.Where(o => o.Terminal != null).Any()).Select(x => x.Key).ToList();

            //拼接冷热树
            var hotRoot = rootListHasTerminal.Where(x => x.IsCool == false).ToList();
            for (int i = 0; i < hotRoot.Count(); i++)
            {
                var hotTree = hotRoot[i];

                var pairCool = rootListDict.Where(x => x.Value.Where(o => o.Terminal == hotTree.Terminal && o.Terminal != null && o.Terminal.Type == ThDrainageADCommon.TerminalType.WaterHeater).Any());
                if (pairCool.Count() > 0)
                {
                    var coolClone = CloneTree(pairCool.First().Key);
                    var hotClone = CloneTree(hotTree);
                    var coolPair = coolClone.GetLeaf().Where(x => x.Terminal == hotClone.Terminal).First();
                    coolPair.Child.Add(hotClone);
                    hotClone.Parent = coolPair;
                    rootListMerged.Add(coolClone);
                }
            }

            //插入没加入的树
            foreach (var tree in rootListHasTerminal)
            {
                var added = rootListMerged.Where(x => x.Pt.IsEqualTo(tree.Pt));
                if (added.Count() == 0)
                {
                    rootListMerged.Add(tree);
                }
            }

            return rootListMerged;
        }


        public static void RemoveNotWaterHeaterHotTree(List<ThDrainageTreeNode> rootList)
        {
            var rootListRemove = new List<ThDrainageTreeNode>();
            var rootListDict = rootList.ToDictionary(x => x, x => x.GetLeaf());

            var removeHotTree = rootListDict.Where(x => x.Key.IsCool == false &&
                                                    (x.Key.Terminal == null || x.Key.Terminal.Type != ThDrainageADCommon.TerminalType.WaterHeater) &&
                                                    x.Value.Where(l => l.Terminal != null && l.Terminal.Type == ThDrainageADCommon.TerminalType.WaterHeater).Any())
                                .Select(x => x.Key);

            rootList.RemoveAll(x => removeHotTree.Contains(x));
        }

        private static ThDrainageTreeNode CloneTree(ThDrainageTreeNode node)
        {
            var cloneNode = new ThDrainageTreeNode(node.Pt);
            cloneNode.Terminal = node.Terminal;
            cloneNode.IsCool = node.IsCool;

            CloneChild(cloneNode, node);

            return cloneNode;
        }

        private static void CloneChild(ThDrainageTreeNode cloneNode, ThDrainageTreeNode oriNode)
        {
            foreach (var oriChild in oriNode.Child)
            {
                var cloneChildNode = new ThDrainageTreeNode(oriChild.Pt);
                cloneChildNode.Terminal = oriChild.Terminal;
                cloneChildNode.IsCool = oriChild.IsCool;

                cloneChildNode.Parent = cloneNode;
                cloneNode.Child.Add(cloneChildNode);

                CloneChild(cloneChildNode, oriChild);
            }
        }

        public static void SetTerminalPairSingleTree(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, Point3d> terminalPairDict)
        {
            foreach (var tree in rootList)
            {
                var leafs = tree.GetLeaf();
                foreach (var l in leafs)
                {
                    if (terminalPairDict.TryGetValue(l.Pt, out var pairPt))
                    {
                        var pairNode = leafs.Where(x => x.Pt.IsEqualTo(pairPt));
                        if (pairNode.Count() > 0)
                        {
                            pairNode.First().TerminalPair = l;
                            l.TerminalPair = pairNode.First();
                        }
                    }
                }
            }
        }

        public static void SetTerminalPairMultipleTree(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, Point3d> terminalPairDict)
        {
            var allLeafs = rootList.SelectMany(x => x.GetLeaf()).ToList();
            allLeafs.AddRange(rootList);

            foreach (var l in allLeafs)
            {
                if (terminalPairDict.TryGetValue(l.Pt, out var pairPt))
                {
                    var pairNode = allLeafs.Where(x => x.Pt.IsEqualTo(pairPt));
                    if (pairNode.Count() > 0)
                    {
                        pairNode.First().TerminalPair = l;
                        l.TerminalPair = pairNode.First();
                    }
                }
            }

        }

    }
}
