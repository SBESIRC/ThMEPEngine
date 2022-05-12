using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.PressureDrainageSystem.Model;
using static DotNetARX.UCSTools;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;

namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public static class DrwaingServiesAssistant
    {
        public static List<BlockReference> SimplifyUnitsByRemovingUnusedTexts(List<Entity> entities, List<BlockReference> blocks)
        {
            var dnblocks = blocks.Where(e => e.GetEffectiveName().Contains("排水管径") || e.Name.Contains("排水管径")).ToList();
            blocks = blocks.Except(dnblocks).ToList();
            var lines = entities.Where(e => e is Line).Select(e => (Line)e).ToList();
            //去空
            dnblocks.ForEach(e => e.Visible = false);
            dnblocks = dnblocks.Where(t => ClosestPointInCurves(t.Position, lines) < 1000)
                .OrderBy(t => t.Position.X)
                .ToList();
            //去重
            if (dnblocks.Count >= 2)
            {
                for (int i = 0; i < dnblocks.Count - 1; i++)
                {
                    for (int j = i + 1; j < dnblocks.Count; j++)
                    {
                        var cond_vert = Math.Abs(dnblocks[i].Position.Y - dnblocks[j].Position.Y) < 1;
                        var cond_hor = dnblocks[j].Position.X - dnblocks[i].Position.X < 1000;
                        var cond = dnblocks[i].Id.GetDynBlockValue("可见性").Equals(dnblocks[j].Id.GetDynBlockValue("可见性"));
                        if (cond_vert && cond_hor && cond)
                        {
                            dnblocks[i].Visible = false;
                            dnblocks.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
            blocks.AddRange(dnblocks);
            return blocks; 
        }
    }
}
