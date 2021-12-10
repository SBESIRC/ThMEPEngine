using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThWindowBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThWindowBuilderEngine() { }
        public void Dispose()
        {
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var windowExtractor = new ThDB3WindowExtractionEngine();
            windowExtractor.Extract(db);
            windowExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));
            return res;
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var windowRecognize = new ThDB3WindowRecognitionEngine();
            windowRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            Elements.AddRange(windowRecognize.Elements);
        }
    }
}
