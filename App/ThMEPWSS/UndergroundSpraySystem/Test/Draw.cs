using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dreambuild.AutoCAD;
using ThMEPEngineCore;

namespace ThMEPWSS.UndergroundSpraySystem.Test
{
    public static class Draw
    {
        public static void DrawLine(List<Line> lines, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (!acadDatabase.Layers.Contains(layer))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layer, 30);
                }
                foreach(var line in lines)
                {
                    var clone = new Line(line.StartPoint,line.EndPoint);
                    clone.LayerId = DbHelper.GetLayerId(layer);
                    clone.ColorIndex = (int)ColorIndex.Red;
                    acadDatabase.CurrentSpace.Add(clone);
                }
            }
        }
    }
}
