using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.Model
{
    public class ThTCHWall : ThIfcWall
    {
        public static new ThTCHWall Create(Entity solid3d)
        {
            return new ThTCHWall()
            {
                Outline = solid3d,
                Uuid = Guid.NewGuid().ToString()
            };
        }
    }
}
