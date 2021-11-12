using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPHVAC.FanConnect.Model
{
    public class ThFanShearWallModel : ThIfcBuildingElement
    {
        public static ThFanShearWallModel Create(Entity data)
        {
            var shearWall = new ThFanShearWallModel
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data
            };

            return shearWall;
        }
    }
}
