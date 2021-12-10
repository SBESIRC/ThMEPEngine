using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThArchWallBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThArchWallBuilderEngine() { }
        public void Dispose()
        {
        }
        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var archwallExtractor = new ThDB3ArchWallExtractionEngine();
            archwallExtractor.Extract(db);
            archwallExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));
            return res;
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var archwallRecognize = new ThDB3ArchWallRecognitionEngine();
            archwallRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            Elements.AddRange(archwallRecognize.Elements);
        }
    }
}
