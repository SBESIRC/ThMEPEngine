using System.Collections.Generic;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using DotNetARX;

namespace ThMEPWSS.Pipe.Engine
{
    public  class ThWArchitectureElevationRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public List<DBText> DbTexts;
        public ThWArchitectureElevationRecognitionEngine()
        {
            DbTexts = new List<DBText>();
        }             
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var spaceNameDbExtension = new ThArchitectureElevationRecognition(database))
            {
                spaceNameDbExtension.BuildElementTexts();
                List<DBText> dbTexts = new List<DBText>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    spaceNameDbExtension.Texts.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex columnCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(polygon);
                    foreach (var filterObj in columnCurveSpatialIndex.SelectCrossingPolygon(pline))
                    {
                        dbTexts.Add(filterObj as DBText);
                    }
                }
                else
                {
                    dbTexts = spaceNameDbExtension.Texts;
                }
                DbTexts = dbTexts;
            }
           
        }
    }
}
