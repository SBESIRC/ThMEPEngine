using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.WaterWellPumpLayout.Interface
{
    public interface IWallEdgeData
    {
        List<Line> GetWallEdges(Database db, Point3dCollection pts);
    }
}
