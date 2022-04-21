using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

using ThMEPWSS.DrainageSystemDiagram.Model;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDAngleValvesEngine
    {
        public static List<ThDrainageSDADBlkOutput> getAngleValves(List<ThTerminalToilet> terminalList)
        {
            var angleValves = new List<ThDrainageSDADBlkOutput>();
            if (terminalList != null && terminalList.Count > 0)
            {
                terminalList.ForEach(t =>
                {
                    t.SupplyCoolOnWall.ForEach(pt =>
                    {
                        var valve = new ThDrainageSDADBlkOutput(pt);
                        valve.Dir = t.Dir;
                        valve.Name = ThDrainageSDCommon.Blk_AngleValves;
                        valve.Visibility.Add(ThDrainageSDCommon.Visibility_AngleValves_key, ThDrainageSDCommon.Visibility_AngleValves_Value);
                        valve.Scale = ThDrainageSDCommon.Blk_scale_AngleValves;
                        valve.BlkSize = ThDrainageSDCommon.Blk_size_AngleValves;
                        angleValves.Add(valve);
                    });
                });
            }
            return angleValves;
        }
    }
}
