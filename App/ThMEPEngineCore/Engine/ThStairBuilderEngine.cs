using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThStairBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThStairBuilderEngine() 
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {
            //
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var stairExtractor = new ThDB3StairExtractionEngine();
            stairExtractor.Extract(db);
            stairExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));
            return res;
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var stairRecognize = new ThDB3StairRecognitionEngine();
            stairRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            Elements.AddRange(stairRecognize.Elements);
        }
    }
}
