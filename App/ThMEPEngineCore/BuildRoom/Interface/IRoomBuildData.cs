using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BuildRoom.Interface
{
    public interface IRoomBuildData
    {
        List<Entity> Columns { get; }
        List<Entity> Walls { get; }
        List<Entity> Doors { get; }
        List<Entity> Windows { get; }
        List<Entity> Railings { get; }
        List<Entity> Cornices { get; }

        void Build(Database db, Point3dCollection pts);
    }
}
