using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 冷凝管
    /// </summary>
    public class ThWCondensePipe : ThWPipe
    {
        public static ThWCondensePipe Create(Entity entity)
        {
            return new ThWCondensePipe()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}

