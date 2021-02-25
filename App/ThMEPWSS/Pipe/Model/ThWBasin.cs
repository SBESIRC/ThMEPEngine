using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 台盆
    /// </summary>
    public class ThWBasin : ThIfcSanitaryTerminal
    {
        public static ThWBasin Create(Entity entity)
        {
            return new ThWBasin()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
