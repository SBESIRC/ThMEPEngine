using System;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPEngineCore.Model.Plumbing
{
    public class ThIfcSideEntryWaterBucket : ThIfcSanitaryTerminal
    {
        public static ThIfcSideEntryWaterBucket Create(Entity entity)
        {
            return new ThIfcSideEntryWaterBucket()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}

