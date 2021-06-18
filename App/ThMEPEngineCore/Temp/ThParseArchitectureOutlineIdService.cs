using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThParseArchitectureOutlineIdService
    {
        public static List<string> ParseBelongedArchitectureIds(Entity entity)
        {
            var ids = new List<string>();
            var colorIndex = entity.ColorIndex;
            if (colorIndex >= 0 && colorIndex <= 9)
            {
                ids.Add(colorIndex.ToString());
            }
            else if (colorIndex >= 10 && colorIndex <= 99)
            {
                ids.Add((colorIndex / 10).ToString());
                ids.Add((colorIndex % 10).ToString());
            }
            else if (colorIndex >= 100 && colorIndex <= 255)
            {
                ids.Add((colorIndex / 10).ToString());
                ids.Add((colorIndex % 10).ToString());
            }
            return ids;
        }
    }
}
