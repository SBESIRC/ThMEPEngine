using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWExternalTag : ThIfcSanitaryTerminal
    {
        public static ThWExternalTag Create(Entity entity)
        {
            return new ThWExternalTag()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
