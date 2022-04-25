using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPWSS.DrainageADPrivate.Model;
using ThMEPWSS.DrainageADPrivate.Service;

namespace ThMEPWSS.DrainageADPrivate.Engine
{
    internal class ThCalculateDimEngine
    {
        private double qL { get; set; }
        private double m { get; set; }
        private double Kh { get; set; }
        private Dictionary<Point3d, ThSaniterayTerminal> PtTerminal { get; set; }
        private Dictionary<Point3d, Point3d> TerminalPairDict { get; set; }
        private List<ThDrainageTreeNode> OriRootList { get; set; }
        private List<ThDrainageTreeNode> MergedRootList { get; set; }

        public List<ThDrainageTreeNode> RootList { get; set; }

        public ThCalculateDimEngine(ThDrainageADPDataPass dataPass, ThDraingeTreeEngine treeEngine)
        {
            qL = dataPass.qL;
            m = dataPass.m;
            Kh = dataPass.Kh;
            PtTerminal = treeEngine.PtTerminal;
            TerminalPairDict = treeEngine.TerminalPairDict;
            OriRootList = treeEngine.OriRootList;
            MergedRootList = treeEngine.MergedRootList;
            RootList = new List<ThDrainageTreeNode>();
        }

        public void CalculateDim()
        {
            var NG = ThCalculateDimService.CalculateWholeFixtureUnit(PtTerminal, TerminalPairDict);

            var U0 = ThCalculateDimService.CalculateMaxFlowProbability(NG, qL, m, Kh);

            var alpha = ThCalculateDimService.CalculateAlpha(U0);

            foreach (var root in MergedRootList)
            {
                ThCalculateDimService.CalculateDimTree(root, alpha);
            }

            RootList = ThCalculateDimService.RemoveDuplicateHotTree(OriRootList);
            ThCalculateDimService.SelectMaxDim(MergedRootList, RootList);
            ThDrainageADTreeService.SetTerminalPairMultipleTree(RootList, TerminalPairDict);
        }

    }
}
