using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Pipe
{
    public class ThWPipeParameters
    {
        public double Diameter { get; set; }
        public string Identifier { get; set; }
        public ThWPipeParameters(int number, double diameter)
        {
            Diameter = diameter;
            Identifier = string.Format("废水FLx{0}", number);
        }
    }
}
