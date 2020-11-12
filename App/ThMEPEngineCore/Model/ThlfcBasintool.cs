using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;



namespace ThMEPEngineCore.Model
{//台盆
   public class ThlfcBasintool : ThIfcBuildingElement
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
