using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThElectricWell : ThIfcBuildingElement
    {
        public static ThElectricWell Create(Entity data)
        {
            var electricWell = new ThElectricWell
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data
            };
            return electricWell;
        }
    }
}
