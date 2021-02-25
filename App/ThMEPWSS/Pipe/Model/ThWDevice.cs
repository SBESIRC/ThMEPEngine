using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
   public class ThWDevice : ThIfcSanitaryTerminal
    {
        public static ThWDevice Create(Entity entity)
        {
            return new ThWDevice()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
