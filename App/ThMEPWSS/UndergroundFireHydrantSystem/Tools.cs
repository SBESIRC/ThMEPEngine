using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore;

namespace ThMEPWSS.UndergroundFireHydrantSystem
{
    public static class Tools
    {
        public static void DrawLines(List<Line> lineList, string layerNames)
        {
#if DEBUG
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                foreach (var line in lineList)
                {
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }
    }
}
