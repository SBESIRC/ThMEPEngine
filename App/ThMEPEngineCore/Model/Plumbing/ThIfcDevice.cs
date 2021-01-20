using System;
using Autodesk.AutoCAD.DatabaseServices;
namespace ThMEPEngineCore.Model.Plumbing
{
   public class ThIfcDevice : ThIfcSanitaryTerminal
    {
        public static ThIfcDevice Create(Entity entity)
        {
            return new ThIfcDevice()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
