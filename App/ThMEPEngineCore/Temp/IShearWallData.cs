using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public interface IShearWallData
    {
        List<Entity> OuterShearWalls { get; set; }
        List<Entity> OtherShearWalls { get; set; }
    }
}
