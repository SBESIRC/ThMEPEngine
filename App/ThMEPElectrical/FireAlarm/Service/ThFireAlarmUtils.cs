using System.Linq;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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
        public static List<string> FindCurveGroupIds(Dictionary<Entity, string> groupId, Entity curve)
        {
            var ids = new List<string>();
            var groups = groupId.Select(g => g.Key).ToList().Where(g => g.IsContains(curve)).ToList();
            groups.ForEach(g => ids.Add(groupId[g]));
            return ids;
        }
    }
}
