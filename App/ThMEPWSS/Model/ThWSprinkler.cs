using System;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Model
{
    public class ThWSprinkler : ThIfcFireSuppressionTerminal
    {
        public static ThWSprinkler Create(Entity entity)
        {
            return new ThWSprinkler()
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = entity.GeometricExtents.ToRectangle(),
            };
        }
    }
}
