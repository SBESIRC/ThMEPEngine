using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThTTypeSpecAnalysisService : ThAnalysisService
    {
        public int A { get; set; } // 端口宽度
        public int B { get; set; } // 端口宽度
        public int C { get; set; } // 端口宽度

        public int L { get; set; }
        public int H1 { get; set; }
        public int H2 { get; set; }
        public int S1 { get; set; }
        public int S2 { get; set; }
        public override void Analysis(Polyline lType)
        {
            /*              A
             *            ------
             *            |    |
             *         H1 |    | H2
             *    ___S1___|    |___S2___
             *  B |                     | C
             *    |_____________________|
             *               L       
             */
            var lines = lType.ToLines();
            if(lines.Count!=6)
            {
                return;
            }
            var l1l2Edges = lines.FindTTypeMainEdge();
            if(l1l2Edges.Count!=1 || l1l2Edges[0].Item2.Count!=3)
            {
                return;
            }
            var lIndex = l1l2Edges[0].Item1;
            var s1Index = l1l2Edges[0].Item2[0];
            var aIndex = l1l2Edges[0].Item2[1];
            var s2Index = l1l2Edges[0].Item2[2];
            L = lines[lIndex].GetLineDistance().Round(); 
            S1 = lines[s1Index].GetLineDistance().Round(); 
            A = lines[aIndex].GetLineDistance().Round(); //端口
            S2 = lines[s2Index].GetLineDistance().Round();

            var bIndex = lIndex.FindMiddleEdgeIndex(s1Index, lines.Count);
            var cIndex = lIndex.FindMiddleEdgeIndex(s2Index, lines.Count);
            var h1Index = s1Index.FindMiddleEdgeIndex(aIndex, lines.Count);
            var h2Index = s2Index.FindMiddleEdgeIndex(aIndex, lines.Count);
            if(bIndex!=-1)
            {
                B= lines[bIndex].GetLineDistance().Round();
            }
            if (cIndex != -1)
            {
                C = lines[cIndex].GetLineDistance().Round();
            }
            if (h1Index != -1)
            {
                H1 = lines[h1Index].GetLineDistance().Round();
            }
            if (h2Index != -1)
            {
                H2 = lines[h2Index].GetLineDistance().Round();
            }
        }
    }
}
