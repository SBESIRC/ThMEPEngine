using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Plumbing
{
    public class ThIfcClosestool: ThIfcSanitaryTerminal
    {
        public static ThIfcClosestool Create(Entity entity)
        {
            return new ThIfcClosestool()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
