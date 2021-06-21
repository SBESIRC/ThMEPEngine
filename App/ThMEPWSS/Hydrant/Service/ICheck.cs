using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Hydrant.Service
{
    public interface ICheck
    {
        List<ThIfcRoom> Rooms { get; set; }
        List<Entity> Covers { get; set; }
        void Check(Database db,Point3dCollection pts);
        List<ThExtractorBase> Extract(Database db, Point3dCollection pts);
        string OutPutGeojson(List<ThExtractorBase> extractors);
    }
}
