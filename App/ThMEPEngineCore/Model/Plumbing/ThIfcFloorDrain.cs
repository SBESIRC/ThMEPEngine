using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model.Plumbing
{
    public class ThIfcFloorDrain : ThIfcSanitaryTerminal
    {
        public UseKind Use { get; set; }
        public static ThIfcFloorDrain Create(Entity entity)
        {
            return new ThIfcFloorDrain()
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
