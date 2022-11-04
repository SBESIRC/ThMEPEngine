using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class StrTools
    {
        public static bool IsContainsIn(this string str1, List<string> strs)
        {
            foreach(var str in strs)
            {
                if(str1.Contains(str))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
