using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
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
