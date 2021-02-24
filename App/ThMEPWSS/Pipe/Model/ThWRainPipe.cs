using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 雨水管
    /// </summary>
    public class ThWRainPipe : ThWPipe
    {
        public static ThWRainPipe Create(Entity entity)
        {
            return new ThWRainPipe()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
