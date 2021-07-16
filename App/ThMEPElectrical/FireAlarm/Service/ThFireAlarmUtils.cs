using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.FireAlarm.Service
{
    public class ThFireAlarmUtils
    {
        public static string BuildString(Dictionary<Entity, List<string>> owners, Entity curve, string linkChar = ";")
        {
            if (owners.ContainsKey(curve))
            {
                return string.Join(linkChar, owners[curve]);
            }
            return "";
        }
    }
}
