using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 屋顶雨水管
    /// </summary>
    public class ThWRoofRainPipe : ThWPipe
    {
        public static ThWRoofRainPipe Create(Entity entity)
        {
            return new ThWRoofRainPipe()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
