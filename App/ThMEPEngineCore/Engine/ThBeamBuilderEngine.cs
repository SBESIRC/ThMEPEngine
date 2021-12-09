using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThBeamBuilderEngine()
        {
        }
        public void Dispose()
        {
        }    
        
        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                return ExtractDB3Beam(db);
            }
            else
            {
                return ExtractRawBeam(db);
            }
        }

        private List<ThRawIfcBuildingElementData> ExtractDB3Beam(Database db)
        {
            var extraction = new ThDB3BeamExtractionEngine();
            extraction.Extract(db);
            return extraction.Results;
        }

        private List<ThRawIfcBuildingElementData> ExtractRawBeam(Database db)
        {
            var extraction = new ThRawBeamExtractionEngine();
            extraction.Extract(db);
            return extraction.Results;
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            ThBuildingElementRecognitionEngine recognitionEngine;
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                recognitionEngine = new ThDB3BeamRecognitionEngine();
            }
            else
            {
                recognitionEngine = new ThRawBeamRecognitionEngine();
            }
            recognitionEngine.Recognize(datas, pts);
            return recognitionEngine.Elements;
        }

        public override void Build(Database db, Point3dCollection pts)
        {
            // 提取
            var beams = Extract(db);
            Build(beams, pts);
        }

        public void Build(List<ThRawIfcBuildingElementData> beams, Point3dCollection pts)
        {
            // 移动到近原点处
            var objs = beams.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            var newPts = transformer.Transform(pts);

            // 识别
            var buildingElements = Recognize(beams, newPts);

            // 恢复到原位置
            buildingElements.ForEach(o => transformer.Reset(o.Outline));

            // 保存结果
            Elements = buildingElements
                .OfType<ThIfcBuildingElement>()
                .ToList();
        }
    }
}
