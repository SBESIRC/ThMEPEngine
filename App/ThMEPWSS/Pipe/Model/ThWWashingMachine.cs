using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
   public class ThWWashingMachine : ThIfcElectricAppliance
    {
        public static ThWWashingMachine Create(Entity entity)
        {
            return new ThWWashingMachine()
            {
                Outline = entity,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
