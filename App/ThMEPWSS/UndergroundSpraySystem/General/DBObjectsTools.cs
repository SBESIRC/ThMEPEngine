using Autodesk.AutoCAD.DatabaseServices;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class DBObjectsTools
    {
        public static void AddList(this DBObjectCollection DBObjs, List<Line> objs)
        {
            foreach(var obj in objs)
            {
                DBObjs.Add((DBObject)obj);
            }
        }

        public static void AddObjs(this DBObjectCollection DBObjs, DBObjectCollection objs)
        {
            foreach (var obj in objs)
            {
                DBObjs.Add((DBObject)obj);
            }
        }
    }
}
