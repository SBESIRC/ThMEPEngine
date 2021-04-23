using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.Duct
{
    public static class ThValveExtension
    {
        public static bool IsCheckValve(this ThValve valve)
        {
            return valve.ValveVisibility == ThDuctUtils.CheckValveModelName();
        }

        public static bool IsFireValve(this ThValve valve)
        {
            return valve.ValveBlockName == ThDuctUtils.FireValveBlockName();
        }
    }
}
