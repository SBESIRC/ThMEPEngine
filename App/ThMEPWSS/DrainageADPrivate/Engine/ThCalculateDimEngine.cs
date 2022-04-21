using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThMEPWSS.DrainageADPrivate.Model;
using ThMEPWSS.DrainageADPrivate.Service;


namespace ThMEPWSS.DrainageADPrivate.Engine
{
    internal class ThCalculateDimEngine
    {
        private ThDrainageADPDataPass DataPass { get; set; }
        private ThDraingeTreeEngine TreeEngine { get; set; }
        public List<ThDrainageTreeNode> RootList { get; set; }

        public ThCalculateDimEngine(ThDrainageADPDataPass dataPass, ThDraingeTreeEngine treeEngine)
        {
            this.DataPass = dataPass;
            this.TreeEngine = treeEngine;
            RootList = new List<ThDrainageTreeNode>();
        }

        public void CalculateDim()
        {
            var ptTerminal = TreeEngine.PtTerminal;
            var terminalPairDict = TreeEngine.TerminalPairDict;
            var mergedRootList = TreeEngine.MergedRootList;
            var oriRootList = TreeEngine.OriRootList;

            var NG = ThCalculateDimService.CalculateWholeFixtureUnit(ptTerminal, terminalPairDict);

            var U0 = ThCalculateDimService.CalculateMaxFlowProbability(NG, DataPass);

            var alpha = ThCalculateDimService.CalculateAlpha(U0);

            foreach (var root in mergedRootList)
            {
                ThCalculateDimService.CalculateDimTree(root, alpha);
            }

            RootList = ThCalculateDimService.RemoveDuplicateHotTree(oriRootList);
            ThCalculateDimService.SelectMaxDim(mergedRootList, RootList);

        }

    }
}
