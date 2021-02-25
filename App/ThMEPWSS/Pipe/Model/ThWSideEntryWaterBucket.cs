using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWSideEntryWaterBucket : ThIfcSanitaryTerminal
    {
        public static ThWSideEntryWaterBucket Create(Entity entity)
        {
            return new ThWSideEntryWaterBucket()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}

