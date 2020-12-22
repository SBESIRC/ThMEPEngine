using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.Garage.Model
{
    public class ThEntityParameter
    {
        public string Layer { get; set; }
        public short ColorIndex { get; set; }
        public string LineType { get; set; }
        public ThEntityParameter()
        {
            Layer = "";
            LineType = "";
        }
    }
}
