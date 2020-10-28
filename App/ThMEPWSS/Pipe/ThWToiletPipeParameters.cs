using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Pipe
{
    public class ThWToiletPipeParameters
    {
        public List<double> Diameter { get; set; }
        public string Identifier { get; set; }
        public int Number { get; set; }
        public ThWToiletPipeParameters(int index, int index1, int floor)
        {
            if (index == 0)
            {
                if (index1 == 0)
                {
                    Number = 2;
                    Identifier = string.Format("通气TLx1,污废PLx1");
                }
                else
                {
                    Number = 3;
                    Identifier = string.Format("沉箱DLx1，通气TLx1,污废PLx1");
                }
            }
            else
            {
                if (index1 == 0)
                {
                    Number = 3;
                    Identifier = string.Format("污水WLx1,通气TLx1,水FLx1");
                }
                else
                {
                    Number = 4;
                    Identifier = string.Format("沉箱DLx1，污水WLx1,通气TLx1,水FLx1");
                }
            }
            Diameter = new List<double>();
            for (int i = 0; i < Number; i++)
            {
                if (floor >= 150)
                {
                    Diameter.Add(150.00);
                }
                else
                {
                    Diameter.Add(100.00);
                }
            }
            Diameter[Number - 2] = 100;
        }
    }
}


