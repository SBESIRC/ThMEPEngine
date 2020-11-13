using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcClosestool: ThIfcPlumbingFixtures
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
