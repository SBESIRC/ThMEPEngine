using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSComponentGraphEdge<T> : Edge<T> where T : ThPDSComponentGraphNode
    {
        public ThPDSComponentGraphEdge(T source, T target) : base(source, target)
        {
        }
    }
}
