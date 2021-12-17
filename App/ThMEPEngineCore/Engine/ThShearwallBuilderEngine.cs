using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThShearWallBuilderEngine : ThBuildingElementBuilder ,IDisposable
    {
        public ThShearWallBuilderEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {
            //
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            // 获取数据
            //var walls = new DBObjectCollection();
            var res = new List<ThRawIfcBuildingElementData>();
            var shearwallExtractor = new ThShearWallExtractionEngine();
            shearwallExtractor.Extract(db);
            var db3ShearwallExtractor = new ThDB3ShearWallExtractionEngine();
            db3ShearwallExtractor.Extract(db);
            shearwallExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData() 
                                                {
                                                    Geometry=e.Geometry,
                                                    Source=DataSource.Raw
                                                }));
            db3ShearwallExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
                                                 {
                                                     Geometry = e.Geometry,
                                                     Source = DataSource.Raw
                                                 }));
            return res;
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var shearwallRecognize = new ThShearWallRecognitionEngine();
            var db3shearwallRecognize = new ThDB3ShearWallRecognitionEngine();
            shearwallRecognize.Recognize(datas.Where(o => o.Source == DataSource.Raw).ToList(), pts);
            db3shearwallRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            Elements.AddRange(shearwallRecognize.Elements);
            Elements.AddRange(db3shearwallRecognize.Elements);
        }
    }
}
