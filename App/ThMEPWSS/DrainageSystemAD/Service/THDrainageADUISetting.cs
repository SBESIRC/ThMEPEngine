using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.DrainageSystemDiagram
{
  public  class THDrainageADUISetting
    {
        public double alpha { get; set; }
        public static THDrainageADUISetting Instance = new THDrainageADUISetting();
        public THDrainageADUISetting()
        {
            alpha = 1.5;
        }
    }
}
