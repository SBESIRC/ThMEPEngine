using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDAngleValvesEngine
    {
        public static List<KeyValuePair<Point3d, Vector3d>> getAngleValves(List<ThTerminalToilate> terminalList)
        {
            var angleValves = new List<KeyValuePair<Point3d, Vector3d>>();

            terminalList.ForEach(t =>
               {
                   t.SupplyCoolOnWall.ForEach(pt => angleValves.Add(new KeyValuePair<Point3d, Vector3d>(pt, t.Dir)));
               });



            return angleValves;
        }
    }
}
