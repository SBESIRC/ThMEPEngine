using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThIfcFloorDrain:ThIfcBuildingElement
    {
        public static ThIfcFloorDrain CreateFloorDrainEntity(Entity entity)
        {
            return new ThIfcFloorDrain()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
