using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPHVAC.FanConnect.Model
{
    public class ThFanColumnModel : ThIfcBuildingElement
    {
        public static ThFanColumnModel Create(Entity data)
        {
            var shearCol = new ThFanColumnModel
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data
            };
            return shearCol;
        }
    }
}
