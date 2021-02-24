using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWGravityWaterBucket : ThIfcSanitaryTerminal
    {
        public static ThWGravityWaterBucket Create(Entity entity)
        {
            return new ThWGravityWaterBucket()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}

