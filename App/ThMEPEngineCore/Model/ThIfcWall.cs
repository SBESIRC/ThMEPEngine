using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThIfcWall : ThIfcBuildingElement
    {
        public static ThIfcWall Create(Entity curve)
        {
            return new ThIfcWall()
            {
                Outline = curve,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
