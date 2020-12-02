using System;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPEngineCore.Model.Plumbing
{
    public class ThIfcGravityWaterBucket : ThIfcSanitaryTerminal
    {
        public static ThIfcGravityWaterBucket Create(Entity entity)
        {
            return new ThIfcGravityWaterBucket()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}

