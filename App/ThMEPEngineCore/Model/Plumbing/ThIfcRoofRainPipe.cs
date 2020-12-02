using System;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPEngineCore.Model.Plumbing
{
    public class ThIfcRoofRainPipe : ThIfcSanitaryTerminal
    {
        public static ThIfcRoofRainPipe Create(Entity entity)
        {
            return new ThIfcRoofRainPipe()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
