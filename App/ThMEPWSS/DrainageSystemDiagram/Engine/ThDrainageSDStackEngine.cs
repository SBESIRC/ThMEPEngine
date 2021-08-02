using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDStackEngine
    {
        public static List<Point3d> getStackPoint(List<ThTerminalToilet> terminalList)
        {
            var stackPt = new List<Point3d>();
            if (terminalList!=null && terminalList.Count > 0)
            {
                stackPt = terminalList.SelectMany(x => x.SupplyCoolOnWall).ToList();
            }

            return stackPt;
        }
    }
}
