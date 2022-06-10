using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThFloorHeatingRoomMarkExtractor : ThExtractorBase, ITransformer
    {
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
        public List<ThIfcTextNote> Marks { get; set; } = new List<ThIfcTextNote>();
        public override void Extract(Database database, Point3dCollection pts)
        {
            //获取本地的房间标注
            var roomMarkExtraction = new ThAIRoomMarkExtractionEngine();
            roomMarkExtraction.ExtractFromMS(database);
            roomMarkExtraction.Results.ForEach(o => Transformer.Transform(o.Geometry));

            var newPts = Transformer.Transform(pts);

            var markEngine = new ThAIRoomMarkRecognitionEngine();
            markEngine.Recognize(roomMarkExtraction.Results, newPts);
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
            Marks.AddRange(marks);
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();

            return geos;
        }

        public void Transform()
        {
            Marks.ForEach(o => Transformer.Transform(o.Geometry));
        }

        public void Reset()
        {
            Marks.ForEach(o => Transformer.Reset(o.Geometry));
        }

    }
}
