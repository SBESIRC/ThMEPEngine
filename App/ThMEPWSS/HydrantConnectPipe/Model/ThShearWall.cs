using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThShearWall : ThIfcBuildingElement
    {
        public Polyline ElementObb { get; set; }
        public static ThShearWall Create(Entity data)
        {
            var shearWall = new ThShearWall
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data
            };

            if (data is Polyline)
            {
                shearWall.ElementObb = data as Polyline;
            }

            return shearWall;
        }
    }
}
