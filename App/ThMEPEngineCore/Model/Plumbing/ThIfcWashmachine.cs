using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Plumbing
{
   public class ThIfcWashMachine : ThIfcSanitaryTerminal
    {
        public static ThIfcWashMachine Create(Entity entity)
        {
            return new ThIfcWashMachine()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
