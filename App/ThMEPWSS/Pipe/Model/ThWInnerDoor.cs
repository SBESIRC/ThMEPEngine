using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
   public class ThWInnerDoor : ThIfcSanitaryTerminal
    {
        public static ThWInnerDoor Create(Entity entity)
        {
            return new ThWInnerDoor()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
