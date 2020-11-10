using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThIfcClosestool:ThIfcBuildingElement
    {
        public static ThIfcClosestool CreateClosestoolEntity(Entity entity)
        {
            return new ThIfcClosestool()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
