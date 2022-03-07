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
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThHydrantLayoutDataFactory
    {
        //input
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        public ThMEPOriginTransformer Transformer { get; set; }
        //output
        public List<ThExtractorBase> Extractors { get; set; }
        public List<ThIfcVirticalPipe> VerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();

        //public List<ThIfcVirticalPipe> THCVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        //public List<ThIfcVirticalPipe> BlkVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        //public List<ThIfcVirticalPipe> CVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();

        public List<ThIfcDistributionFlowElement> Hydrant { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Polyline> Car { get; set; } = new List<Polyline>();
        public List<Polyline> Well { get; set; } = new List<Polyline>();

        public ThHydrantLayoutDataFactory()
        { }

        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractVerticalPipe(database, framePts);
            ExtractHydrant(database, framePts);
            ExtractBasicArchitechObject(database, framePts);
            ExtractCar(database, framePts);
            ExtractWells(database, framePts);
        }

        private void ExtractBasicArchitechObject(Database database, Point3dCollection framePts)
        {
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

        }
        private void ExtractCar(Database database, Point3dCollection framePts)
        {
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
            Car.AddRange(parkingStallService.ParkingStalls.OfType<Polyline>().ToList());

        }

        private void ExtractWells(Database database, Point3dCollection framePts)
        {

            //创建集水井提取白名单
            WaterWellIdentifyConfigInfo info = new WaterWellIdentifyConfigInfo();
            info.WhiteList.Clear();
            BlockNameDict["集水井"].ForEach(e => info.WhiteList.Add(e));

            using (var waterwellEngine = new ThWWaterWellRecognitionEngine(info))
            {
                waterwellEngine.Recognize(database, framePts);
                waterwellEngine.RecognizeMS(database, framePts);
                foreach (var element in waterwellEngine.Datas)
                {
                    ThWWaterWell waterWell = ThWWaterWell.Create(element);
                    waterWell.Init();
                    var obb = waterWell.OBB;
                    Well.Add(obb);
                }
            }

        }

        private void ExtractVerticalPipe(Database database, Point3dCollection framePts)
        {
            var vertical = new ThVerticalPipeExtractService();
            vertical.Extract(database, framePts);
            VerticalPipe = vertical.VerticalPipe;

            //var blkVertical = new ThBlkVerticalPipeExtractService();
            //blkVertical.Extract(database, framePts);
            //BlkVerticalPipe = blkVertical.VerticalPipe;

            //var cVertical = new ThCircleVerticalPipeExtractService();
            //cVertical.Extract(database, framePts);
            //CVerticalPipe = cVertical.VerticalPipe;

        }

        private void ExtractHydrant(Database database, Point3dCollection framePts)
        {
            var hydrantVisitor = new ThHydrantExtractionVisitor()
            {
                BlkNames = new List<string> { ThHydrantCommon.BlkName_Hydrant, ThHydrantCommon.BlkName_Hydrant_Extinguisher },
            };
            var hydrantRecog = new ThHydrantRecognitionEngine(hydrantVisitor);
            hydrantRecog.RecognizeMS(database, framePts);
            Hydrant.AddRange(hydrantRecog.Elements);

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

