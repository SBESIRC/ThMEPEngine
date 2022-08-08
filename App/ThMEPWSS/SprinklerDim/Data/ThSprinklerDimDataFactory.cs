using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.Sprinkler.Service;
using ThMEPWSS.Engine;

namespace ThMEPWSS.SprinklerDim.Data
{
    public class ThSprinklerDimDataFactory
    {
        //---input
        public ThMEPOriginTransformer Transformer { get; set; }

        //----output
        public List<Point3d> SprinklerPtData { get; set; }
        public List<ThIfcFlowSegment> TchPipeData { get; set; }
        public List<ThExtractorBase> Extractors { get; set; }
        public List<Curve> AxisCurves { get; set; }

        public ThSprinklerDimDataFactory()
        {
            SprinklerPtData = new List<Point3d>();
            TchPipeData = new List<ThIfcFlowSegment>();
            AxisCurves = new List<Curve>();
        }

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractBasicArchitechObject(database, framePts);
            GetAllAxisCurves(database, framePts);
            GetSprinklerPtData(database, framePts);
            GetTCHPipeData(database, framePts);
        }

        private void ExtractBasicArchitechObject(Database database, Point3dCollection framePts)
        {
            var manger = Extract(database); // visitor manager,提取的是原始数据
            manger.MoveToOrigin(Transformer); // 移动到原点

            Extractors = new List<ThExtractorBase>()
            {
                new ThSprinklerArchitectureWallExtractor()
                {
                    ElementLayer = "AI-墙",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ArchWallVisitor.Results,
                 },
                new ThSprinklerShearWallExtractor()
                {
                    ElementLayer = "AI-剪力墙",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ShearWallVisitor.Results,
                    NonDb3ExtractResults = manger.ShearWallVisitor.Results,
                },
                new ThSprinklerColumnExtractor()
                {
                    ElementLayer = "AI-柱",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ColumnVisitor.Results,
                    NonDb3ExtractResults = manger.ColumnVisitor.Results,
                },
                new ThSprinklerRoomExtractor()
                {
                    IsWithHole=false,
                    UseDb3Engine=true,
                    Transformer = Transformer,
                },
            };
            Extractors.ForEach(o => o.Extract(database, framePts));

            // 移回原位
            Extractors.ForEach(o =>
            {
                if (o is ITransformer iTransformer)
                {
                    iTransformer.Reset();
                }
            });
        }

        private ThBuildingElementVisitorManager Extract(Database database)
        {
            var visitors = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.DB3ArchWallVisitor);
            extractor.Accept(visitors.DB3ShearWallVisitor);
            extractor.Accept(visitors.DB3ColumnVisitor);
            //extractor.Accept(visitors.DB3BeamVisitor);
            //extractor.Accept(visitors.DB3DoorMarkVisitor);
            //extractor.Accept(visitors.DB3DoorStoneVisitor);
            //extractor.Accept(visitors.DB3WindowVisitor);
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Extract(database);
            return visitors;
        }

        private void GetSprinklerPtData(Database database, Point3dCollection framePts)
        {
            var recognizeAllEngine = new ThTCHSprinklerRecognitionEngine();
            recognizeAllEngine.RecognizeMS(database, framePts);
            var sprinklersData = recognizeAllEngine.Elements
                .OfType<ThSprinkler>()
                .Select(o => o.Position)
                .ToList();

            SprinklerPtData.AddRange(sprinklersData);
        }

        private void GetTCHPipeData(Database database, Point3dCollection framePts)
        {
            var TCHPipeRecognize = new ThTCHPipeRecognitionEngine()
            {
                LayerFilter = new List<string> { "W-FRPT-SPRL-PIPE" },
            };
            TCHPipeRecognize.RecognizeMS(database, framePts);
            TchPipeData.AddRange(TCHPipeRecognize.Elements.OfType<ThIfcFlowSegment>().ToList());
        }

        private void GetAllAxisCurves(Database database, Point3dCollection framePts)
        {
            var axisEngine = new ThAXISLineRecognitionEngine();
            axisEngine.Recognize(database, framePts);
            foreach (var item in axisEngine.Elements)
            {
                if (item == null || item.Outline == null)
                    continue;
                if (item.Outline is Curve curve)
                {
                    var copy = (Curve)curve.Clone();
                    AxisCurves.Add(copy);
                }
            }

        }

    }
}
