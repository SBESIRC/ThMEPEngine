﻿using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;


namespace ThMEPEngineCore.Engine
{
    public class ThCondensePipeRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var condensePipeDbExtension = new ThCondensePipeDbExtension(database))
            {
                condensePipeDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    condensePipeDbExtension.CondensePipe.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex condensePipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in condensePipeSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = condensePipeDbExtension.CondensePipe;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThIfcCondensePipe.Create(o));
                });
            }
        }
    }
}



