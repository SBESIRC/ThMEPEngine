using System;
using System.Collections.Generic;
using PDSGraph = QuikGraph.AdjacencyGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, 
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphCompareService
    {
        private readonly List<ThPDSProjectGraphComparingUnit> _units;

        public ThPDSProjectGraphCompareService()
        {
            _units = new List<ThPDSProjectGraphComparingUnit>()
            {
                new ThPDSProjectGraphStructuralComparingUnit(),
                new ThPDSProjectGraphQuantitativeComparingUnit(),
                new ThPDSProjectGraphQualitativeComparingUnit(),
            };
        }

        public void Compare(PDSGraph source,PDSGraph target)
        {
            _units.ForEach(o => o.DoCompare(source, target));
        }
    }
}
