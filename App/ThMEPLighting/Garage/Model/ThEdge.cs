using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.Garage.Model
{
    public class ThEdge<TVertex> :Edge<TVertex> where TVertex:ThVertex
    {
        public double Length { get; set; }
        public ThEdge(TVertex source, TVertex target) : base(source, target)
        {
            Length = source.Position.DistanceTo(target.Position);
        }
    }
}
