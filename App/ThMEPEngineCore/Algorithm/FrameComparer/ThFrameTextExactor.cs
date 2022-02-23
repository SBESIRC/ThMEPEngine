using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public class ThFrameTextExactor
    {
        public List<ThIfcTextNote> curTexts;
        public List<ThIfcTextNote> refTexts;
        private const string roomTextName = "AI-房间名称";
        public ThFrameTextExactor()
        {
            Init();
            ReadCurTexts();
            ReadRefTexts();
        }
        private void Init()
        {
            curTexts = new List<ThIfcTextNote>();
            refTexts = new List<ThIfcTextNote>();
        }

        private void ReadRefTexts()
        {
            using (var db = AcadDatabase.Active())
            {
                // 提取
                var visitor = new ThAIRoomMarkExtractionVisitor
                {
                    LayerFilter = ThRoomMarkLayerManager.AIRoomMarkXRefLayers(db.Database),
                };
                var extractor = new ThAnnotationElementExtractor();
                extractor.Accept(visitor);
                extractor.Extract(db.Database);

                // 识别
                var markEngine = new ThAIRoomMarkRecognitionEngine();
                markEngine.Recognize(visitor.Results, new Autodesk.AutoCAD.Geometry.Point3dCollection());
                var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
                foreach (var t in marks)
                    if (IsRoom(t.Text))
                        refTexts.Add(t);
            }
        }

        private void ReadCurTexts()
        {
            using (var db = AcadDatabase.Active())
            {
                var dbTexts = db.ModelSpace.OfType<DBText>();
                foreach (var t in dbTexts)
                {
                    if (IsRoom(t.TextString) && t.Layer == roomTextName)
                    {
                        var l = new Line(t.Bounds.Value.MinPoint, t.Bounds.Value.MaxPoint);
                        curTexts.Add(ThIfcTextNote.Create(t.TextString, l.Buffer(1)));
                    }
                }
            }
        }
        private bool IsRoom(string s)
        {
            return s.Contains("房") ||
                   s.Contains("道") ||
                   s.Contains("室") ||
                   s.Contains("间") ||
                   s.Contains("梯") ||
                   s.Contains("堂") ||
                   s.Contains("电") ||
                   s.Contains("厅") ||
                   s == "车库" ||
                   s == "水";
        }
    }
}
