using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Plumbing
{
    /// <summary>
    /// 台盆
    /// </summary>
    public class ThIfcBasin : ThIfcSanitaryTerminal
    {
        public static ThIfcBasin Create(Entity entity)
        {
            return new ThIfcBasin()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
