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
        public static List<ThDrainageSDADBlkOutput> getAngleValves(List<ThTerminalToilet> terminalList)
        {
            var angleValves = new List<ThDrainageSDADBlkOutput>();

            terminalList.ForEach(t =>
            {
                t.SupplyCoolOnWall.ForEach(pt =>
                {
                    var valve = new ThDrainageSDADBlkOutput(pt);
                    valve.dir = t.Dir;
                    valve.name = ThDrainageSDCommon.Blk_AngleValves;
                    valve.visibility.Add(ThDrainageSDCommon.Visibility_AngleValves_key, ThDrainageSDCommon.Visibility_AngleValves_Value);
                    valve.scale = ThDrainageSDCommon.Blk_scale_AngleValves;
                    valve.blkSize = ThDrainageSDCommon.Blk_size_AngleValves;
                    angleValves.Add(valve);
                });
            });

            return angleValves;
        }
    }
}
