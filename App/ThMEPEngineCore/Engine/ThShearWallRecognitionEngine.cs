using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NetTopologySuite.Operation.Linemerge;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Simplify;
using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThCADCore.Geometry;

namespace ThMEPEngineCore.Engine
{
    public class ThShearWallRecognitionEngine : ThBuildingElementRecognitionEngine, IDisposable
    {
        public ThShearWallRecognitionEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var shearWallDbExtension = new ThStructureShearWallDbExtension(database))
            {
                shearWallDbExtension.BuildElementCurves();
                shearWallDbExtension.ShearWallCurves.ForEach(o =>
                {
                    if(o is Polyline polyline && polyline.Length>0.0)
                    {
                        Elements.Add(ThIfcWall.CreateWallEntity(polyline.Clone() as Polyline));
                    }
                });
            }
        }
    }
}
