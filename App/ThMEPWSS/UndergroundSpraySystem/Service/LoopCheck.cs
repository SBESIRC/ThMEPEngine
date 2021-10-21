using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public static class LoopCheck
    {
        public static bool IsSingleLoop(SpraySystem spraySystem, SprayIn sprayIn)
        {
            foreach(var pt in spraySystem.MainLoop)
            {
                if(sprayIn.PtTypeDic[pt].Contains("AlarmValve"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
