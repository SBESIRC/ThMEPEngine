using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.SprinklerConnect.Data;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThHydrantLayoutDataFactory
    {
        //input
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        public ThMEPOriginTransformer Transformer { get; set; }
        //output
        public List<ThExtractorBase> Extractors { get; set; }

        public List<ThIfcVirticalPipe> THCVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcVirticalPipe> BlkVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcVirticalPipe> CVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();

        public List<ThIfcDistributionFlowElement> Hydrant { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Entity> Car { get; set; } = new List<Entity>();
        public ThHydrantLayoutDataFactory()
        {

        }

        public void GetElements(Database database, Point3dCollection framePts)
        {
            var thcVertical = new ThTchVerticalPipeExtractService();
            thcVertical.Extract(database, framePts);
            THCVerticalPipe = thcVertical.TCHVerticalPipe;

            var blkVertical = new ThBlkVerticalPipeExtractService();
            blkVertical.Extract(database, framePts);
            BlkVerticalPipe = blkVertical.VerticalPipe;

            var cVertical = new ThCircleVerticalPipeExtractService();
            cVertical.Extract(database, framePts);
            CVerticalPipe = cVertical.VerticalPipe;


            var hydrantVisitor = new ThHydrantExtractionVisitor()
            {
                BlkNames = new List<string> { ThHydrantCommon.BlkName_Hydrant, ThHydrantCommon.BlkName_Hydrant_Extinguisher },
            };
            var hydrantRecog = new ThHydrantRecognitionEngine(hydrantVisitor);
            hydrantRecog.RecognizeMS(database, framePts);
            Hydrant.AddRange(hydrantRecog.Elements);

            var manger = Extract(database); // visitor manager,提取的是原始数据
            MoveToOrigin(manger, Transformer); // 移动到原点

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
                new ThHydrantDoorExtractor()
                {
                    ElementLayer = "AI-门",
                    Transformer = Transformer,
                    VisitorManager = manger,
                },
                new ThSprinklerFireproofshutterExtractor()
                {
                    ElementLayer = "AI-防火卷帘",
                    Transformer = Transformer,
                },

                new ThSprinklerRoomExtractor()
                {
                    IsWithHole = true,
                    UseDb3Engine = true,
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


            // 提取车位
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(framePts);
            var parkingStallService = new ThSprinklerConnectParkingStallService
            {
                BlockNameDict = BlockNameDict
            };
            parkingStallService.ParkingStallExtractor(database, pline);

            Car.AddRange(parkingStallService.ParkingStalls.Cast<Entity>().ToList());





        }

        private void MoveToOrigin(ThBuildingElementVisitorManager vm, ThMEPOriginTransformer transformer)
        {
            vm.DB3ArchWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorStoneVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
        }

        private ThBuildingElementVisitorManager Extract(Database database)
        {
            var visitors = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.DB3ArchWallVisitor);
            extractor.Accept(visitors.DB3ShearWallVisitor);
            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Accept(visitors.DB3ColumnVisitor);
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.DB3DoorStoneVisitor);
            extractor.Accept(visitors.DB3DoorMarkVisitor);

            extractor.Extract(database);
            return visitors;
        }

    }
}

