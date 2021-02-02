using System.Collections.Generic;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWindowRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var windowDbExtension = new ThWindowDbExtension(database))
            {
                windowDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    windowDbExtension.Windows.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex basintoolSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in basintoolSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = windowDbExtension.Windows;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThIfcWindow.Create(o));
                });
            }
        }
    }
}

   

