using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Plumbing
{
    public class ThIfcCondensePipe : ThIfcSanitaryTerminal
    {
        public static ThIfcCondensePipe Create(Entity entity)
        {
            return new ThIfcCondensePipe()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}

