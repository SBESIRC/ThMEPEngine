using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 重力雨水斗
    /// </summary>
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

