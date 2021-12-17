using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
namespace ThMEPWSS.Hydrant.Engine
{
    public class ThFireHydrantValveEngine : ThSpatialElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDB3RoomOutlineExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromMS(Database database, ObjectIdCollection dbObjs)
        {
            var visitor = new ThDB3RoomOutlineExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database, dbObjs);
            Results.AddRange(visitor.Results);
        }
    }

}