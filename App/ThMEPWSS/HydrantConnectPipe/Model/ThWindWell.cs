using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThWindWell : ThIfcBuildingElement
    {
        public static ThWindWell Create(Entity data)
        {
            var windWell = new ThWindWell
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data
            };
            return windWell;
        }
    }
}
