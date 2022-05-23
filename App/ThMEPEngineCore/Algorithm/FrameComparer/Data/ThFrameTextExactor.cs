using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public class ThFrameTextExactor
    {
        public List<ThIfcTextNodeData> curTexts;
        public List<ThIfcTextNodeData> refTexts;
        private Point3dCollection fance;
        private const string roomTextName = "AI-房间名称";
        public ThFrameTextExactor(Point3dCollection fance)
        {
            this.fance = fance;
            Init();
            ReadCurTexts();
            ReadRefTexts();
        }
        private void Init()
        {
            curTexts = new List<ThIfcTextNodeData>();
            refTexts = new List<ThIfcTextNodeData>();
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
                markEngine.RecognizeWithData(visitor.Results, fance);
                var marks = markEngine.Elements.OfType<ThIfcTextNodeData>().ToList();
                refTexts.AddRange(marks);
                //foreach (var t in marks)
                //{
                //    if (IsRoom(t.Text))
                //    {
                //        refTexts.Add(t);
                //    }
                //}
            }
        }

        /// <summary>
        /// 本图不能用提取器。拿不到原始text
        /// </summary>
        private void ReadCurTexts()
        {
            using (var db = AcadDatabase.Active())
            {
                var dbTexts = db.ModelSpace.OfType<DBText>();
                foreach (var t in dbTexts)
                {
                    //if (IsRoom(t.TextString) && t.Layer == roomTextName)
                    //{
                    //    var l = new Line(t.Bounds.Value.MinPoint, t.Bounds.Value.MaxPoint);
                    //    curTexts.Add(ThIfcTextNote.Create(t.TextString, l.Buffer(1)));
                    //}
                    if (t.Layer == roomTextName)
                    {
                        var textObj = new ThIfcTextNodeData(t.TextString, t.TextOBB(), t);
                        curTexts.Add(textObj);
                    }
                }
            }
        }

        //private void ReadCurTexts()
        //{
        //    using (var db = AcadDatabase.Active())
        //    {
        //        // 提取
        //        var visitor = new ThAIRoomMarkExtractionVisitor
        //        {
        //            LayerFilter = new List<string> { roomTextName },
        //        };
        //        var extractor = new ThAnnotationElementExtractor();
        //        extractor.Accept(visitor);
        //        extractor.ExtractFromMS(db.Database);

        //        // 识别
        //        var markEngine = new ThAIRoomMarkRecognitionEngine();
        //        markEngine.RecognizeWithData(visitor.Results, fance);
        //        var marks = markEngine.Elements.OfType<ThIfcTextNodeData>().ToList();
        //        curTexts.AddRange(marks);
        //        //foreach (var t in marks)
        //        //{
        //        //    if (IsRoom(t.Text))
        //        //    {
        //        //        curTexts.Add(t);
        //        //    }
        //        //}
        //    }
        //}


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
