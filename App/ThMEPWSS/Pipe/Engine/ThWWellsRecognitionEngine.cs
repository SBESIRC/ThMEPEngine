﻿using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Engine
{
  public class ThWWellsRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var ThWWellsRecognitionEngine = new ThWellsDbExtension(database))
            {
                ThWWellsRecognitionEngine.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    ThWWellsRecognitionEngine.Wells.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex basintoolSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in basintoolSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = ThWWellsRecognitionEngine.Wells;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThIfcInnerDoor.Create(o));
                });
            }
        }
    }
}
