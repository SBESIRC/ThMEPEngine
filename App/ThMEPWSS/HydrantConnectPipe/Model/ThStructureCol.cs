using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThStructureCol : ThIfcBuildingElement
    {
        public static ThStructureCol Create(Entity data)
        {
            var shearCol = new ThStructureCol
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data
            };
            return shearCol;
        }
    }
}
