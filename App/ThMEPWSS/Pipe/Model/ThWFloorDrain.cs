using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 地漏
    /// </summary>
    public class ThWFloorDrain : ThIfcSanitaryTerminal
    {
        public UseKind Use { get; set; }
        public static ThWFloorDrain Create(Entity entity)
        {
            return new ThWFloorDrain()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
    public enum UseKind
    {
        None,
        Toilet,
        Balcony
    }
}
