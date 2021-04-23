using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 马桶
    /// </summary>
    public class ThWClosestool: ThIfcSanitaryTerminal
    {
        public static ThWClosestool Create(Entity entity)
        {
            return new ThWClosestool()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
