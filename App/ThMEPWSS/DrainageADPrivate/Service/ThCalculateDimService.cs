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
    internal class ThCalculateDimService
    {
        /// <summary>
        /// 计算Ng
        /// </summary>
        /// <param name="ptTerminal"></param>
        /// <param name="terminalPairDict"></param>
        /// <returns></returns>
        public static double CalculateWholeFixtureUnit(Dictionary<Point3d, ThSaniterayTerminal> ptTerminal, Dictionary<Point3d, Point3d> terminalPairDict)
        {
            double Ng = 0;

            var pairPt = terminalPairDict.Select(x => x.Value).ToList();
            var ptTerminalSingle = ptTerminal.Where(x => terminalPairDict.ContainsKey(x.Key) == false && pairPt.Contains(x.Key) == false).ToList();

            foreach (var pt in ptTerminalSingle)
            {
                var typeint = (int)pt.Value.Type;
                var value = ThDrainageADCommon.TerminalFixtureUnitCool[(int)(pt.Value.Type)];
                Ng = Ng + value;
            }
            foreach (var pt in terminalPairDict)
            {
                var type = (int)ptTerminal[pt.Key].Type;
                var value = ThDrainageADCommon.TerminalFixtureUnitCoolHot[type];
                Ng = Ng + value;
            }

            return Ng;

        }

        /// <summary>
        /// 计算U0
        /// </summary>
        /// <param name="NG"></param>
        /// <param name="dataPass"></param>
        /// <returns></returns>
        public static double CalculateMaxFlowProbability(double NG, ThDrainageADPDataPass dataPass)
        {
            double U0 = 0;

            var qL = dataPass.qL;
            var m = dataPass.m;
            var Kh = dataPass.Kh;
            var Th = ThDrainageADCommon.Th;

            U0 = 100 * qL * m * Kh / (0.2 * NG * Th * 3600);


            return U0;
        }

        public static double CalculateAlpha(double U0)
        {
            double alpha = 0;

            var alphaList = ThDrainageADCommon.AlphaList;

            if (U0 <= alphaList.First().Item1)
            {
                alpha = alphaList.First().Item2;
            }
            else if (U0 >= alphaList.Last().Item1)
            {
                alpha = alphaList.Last().Item2;
            }
            else
            {
                for (int i = 0; i < alphaList.Count - 1; i++)
                {
                    if (alphaList[i].Item1 <= U0 && U0 <= alphaList[i + 1].Item1)
                    {
                        var a = (alphaList[i].Item2 - alphaList[i + 1].Item2) / (alphaList[i].Item1 - alphaList[i + 1].Item1);
                        var b = alphaList[i].Item2 - a * alphaList[i].Item1;
                        alpha = a * U0 + b;
                        break;
                    }
                }
            }

            return alpha;

        }
        public static void CalculateDimTree(ThDrainageTreeNode node, double alpha)
        {
            CalculateNodeDim(node, alpha);

            foreach (var c in node.Child)
            {
                CalculateDimTree(c, alpha);
            }
        }
        private static void CalculateNodeDim(ThDrainageTreeNode node, double alpha)
        {
            var leaf = node.GetLeaf();
            if (leaf.Count <= 1)
            {
                node.Dim = 15;
                return;
            }

            var Ng = CalculateNodeNg(leaf);
            if (Ng == 0)
            {
                node.Dim = 15;
            }
            else if (Ng < 1)
            {
                node.Dim = 15;
            }
            else
            {
                double U = (1 + alpha * (System.Math.Pow(Ng - 1, 0.49))) / Math.Sqrt(Ng);
                double qg = 0.2 * U * Ng;

                var miuDict = ThDrainageADCommon.FlowDiam.ToDictionary(x => x.Key, x => qg / (1.0 / 4 * Math.PI * Math.Pow(x.Value, 2) * 1000));

                foreach (var miu in miuDict)
                {
                    //检查管径
                    ThDrainageADCommon.DiamFlowRange.TryGetValue(miu.Key, out var minFlow);

                    if (minFlow.Item1 < miu.Value && miu.Value <= minFlow.Item2)
                    {
                        node.Dim = miu.Key;
                        break;
                    }
                }
            }
        }
        private static double CalculateNodeNg(List<ThDrainageTreeNode> leaf)
        {
            var pair = leaf.Where(x => x.TerminalPair != null && leaf.Contains(x.TerminalPair)).ToList();
            double Ng = 0.0;
            foreach (var l in leaf)
            {
                if (pair.Contains(l) == false)
                {
                    double value = 0.0;
                    if (l.IsCool == true)
                    {
                        value = ThDrainageADCommon.TerminalFixtureUnitCool[(int)l.Terminal.Type];
                    }
                    else
                    {
                        value = ThDrainageADCommon.TerminalFixtureUnitHot[(int)l.Terminal.Type];
                    }
                    Ng = Ng + value;
                }
            }
            double NgPair = 0.0;
            foreach (var l in pair)
            {
                double value = 0.0;
                value = ThDrainageADCommon.TerminalFixtureUnitCoolHot[(int)l.Terminal.Type];
                NgPair = NgPair + value;
            }
            Ng = Ng + NgPair / 2;

            return Ng;
        }

        public static List<ThDrainageTreeNode> RemoveDuplicateHotTree(List<ThDrainageTreeNode> oriRootList)
        {
            var rootList = new List<ThDrainageTreeNode>();
            rootList.AddRange(oriRootList);
            for (int i = rootList.Count - 1; i >= 0; i--)
            {
                var root = rootList[i];

                var pt = root.Pt;
                var otherTreeLeaf = rootList.Where(x => x != root).SelectMany(x => x.GetLeaf()).Select(x => x.Pt).ToList();
                if (otherTreeLeaf.Contains(pt))
                {
                    rootList.RemoveAt(i);
                }

            }

            return rootList;

        }

        public static void SelectMaxDim(List<ThDrainageTreeNode> mergedRootList, List<ThDrainageTreeNode> rootList)
        {
            var allNode = mergedRootList.SelectMany(x => x.GetDescendant()).ToList();
            allNode.AddRange(mergedRootList);
            var ptDimDict = allNode.GroupBy(x => x.Pt).ToDictionary(x => x.Key, x => x.OrderByDescending(o => o.Dim).ToList());

            foreach (var root in rootList)
            {
                FindMaxDimNode(root, ptDimDict);
            }
        }

        private static void FindMaxDimNode(ThDrainageTreeNode node, Dictionary<Point3d, List<ThDrainageTreeNode>> ptDimDict)
        {
            if (ptDimDict.TryGetValue(node.Pt, out var dimValues))
            {
                var dim = dimValues.First().Dim;
                node.Dim = dim;
            }

            foreach (var c in node.Child)
            {
                FindMaxDimNode(c, ptDimDict);
            }
        }

    }
}
