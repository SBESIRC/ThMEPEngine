using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWWell : ThIfcSanitaryTerminal
    {
        public static ThWWell Create(Entity entity)
        {
            return new ThWWell()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
