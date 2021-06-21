using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Interface
{
    public interface IArchitectureOutlineData
    {
        List<Entity> Query(Database db, Point3dCollection pts);
    }
}
