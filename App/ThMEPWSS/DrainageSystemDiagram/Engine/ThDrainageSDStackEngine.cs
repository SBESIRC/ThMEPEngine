using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;


namespace ThMEPWSS.DrainageSystemDiagram
{
   public  class ThDrainageSDStackEngine
    {
        public static List<Point3d > getStackPoint(List<ThIfcSanitaryTerminalToilate > terminalList)
        {
            var stackPt = terminalList.SelectMany(x => x.SupplyCoolOnWall).ToList();

            return stackPt;
        }

    }
}
