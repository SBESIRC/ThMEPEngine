using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWRoofRainPipeRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var rainPipeDbExtension = new ThRoofRainPipeDbExtension(database))
            {
                rainPipeDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    rainPipeDbExtension.RainPipes.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex roofrainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in roofrainPipeSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = rainPipeDbExtension.RainPipes;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThWRoofRainPipe.Create(o));
                });
            }
        }
    }
}
