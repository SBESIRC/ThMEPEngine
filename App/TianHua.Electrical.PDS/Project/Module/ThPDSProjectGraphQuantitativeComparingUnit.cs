﻿using System;
using PDSGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 定量分析比较器
    /// </summary>
    public class ThPDSProjectGraphQuantitativeComparingUnit : ThPDSProjectGraphComparingUnit
    {
        public override void DoCompare(PDSGraph source, PDSGraph target)
        {
            throw new NotImplementedException();
        }
    }
}
