using System;
using Autodesk.AutoCAD.DatabaseServices;
namespace ThMEPEngineCore.Model.Plumbing
{
   public class ThIfcInnerDoor : ThIfcSanitaryTerminal
    {
        public static ThIfcInnerDoor Create(Entity entity)
        {
            return new ThIfcInnerDoor()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
