using System;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPEngineCore.Model.Plumbing
{
    public class ThIfcRainPipe : ThIfcSanitaryTerminal
    {
        public static ThIfcRainPipe Create(Entity entity)
        {
            return new ThIfcRainPipe()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
