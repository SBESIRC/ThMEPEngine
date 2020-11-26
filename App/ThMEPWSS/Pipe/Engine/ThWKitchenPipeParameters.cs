using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWKitchenPipeParameters
    {
        public double Diameter { get; set; }
        public string Identifier { get; set; }
        public ThWKitchenPipeParameters(int number, double diameter)
        {
            Diameter = diameter;
            Identifier = string.Format("废水FLx{0}", number);
        }
    }
}
