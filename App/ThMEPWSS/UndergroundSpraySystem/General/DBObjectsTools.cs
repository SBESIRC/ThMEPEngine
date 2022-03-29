using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class DBObjectsTools
    {
        public static void AddList(this DBObjectCollection DBObjs, List<Line> objs)
        {
            foreach(var obj in objs)
            {
                DBObjs.Add(obj);
            }
        }
    }
}
