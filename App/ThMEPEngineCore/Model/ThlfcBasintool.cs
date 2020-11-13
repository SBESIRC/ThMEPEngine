using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    /// <summary>
    /// 台盆
    /// </summary>
    public class ThlfcBasintool : ThIfcPlumbingFixtures
    {
        public static ThlfcBasintool CreateBasintoolEntity(Entity entity)
        {
            return new ThlfcBasintool()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
