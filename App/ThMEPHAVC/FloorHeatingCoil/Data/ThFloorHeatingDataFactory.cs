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
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model.Hvac;


namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThFloorHeatingDataFactory
    {
        //input
        public ThMEPOriginTransformer Transformer { get; set; }
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        //output
        public List<ThExtractorBase> Extractors { get; set; }
        public List<ThIfcDistributionFlowElement> SanitaryTerminal { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Polyline> SenitaryTerminalOBBTemp { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();
        public List<DBText> RoomSuggestDist { get; set; } = new List<DBText>();
        public List<BlockReference> WaterSeparator { get; set; } = new List<BlockReference>();

        public ThFloorHeatingDataFactory()
        { }
        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractBasicArchitechObject(database, framePts);
            ExtractFurnitureObstacle(database, framePts);
            ExtractFurnitureObstacleTemp(database, framePts);
            ExtractRoomSeparateLine(database, framePts);
            ExtractRoomSuggestDist(database, framePts);
            ExtractWaterSeparator(database, framePts);
        }

        private void ExtractBasicArchitechObject(Database database, Point3dCollection framePts)
        {
            //var manger = Extract(database); // visitor manager,提取的是原始数据
            //MoveToOrigin(manger, Transformer); // 移动到原点

            Extractors = new List<ThExtractorBase>()
            {
                new ThFloorHeatingDoorExtractor()
                {
                    ElementLayer = "AI-门",
                    Transformer = Transformer,
                    //VisitorManager = manger,
                },
                new ThFloorHeatingRoomExtractor ()
                {
                    IsWithHole = true,
                    UseDb3Engine = true,
                    Transformer = Transformer,
                },
                new ThFloorHeatingRoomMarkExtractor ()
                {
                    Transformer = Transformer,
                }
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
        private void ExtractFurnitureObstacle(Database database, Point3dCollection framePts)
        {
            var extractBlkList = new List<string>();
            foreach (var blkType in ThFloorHeatingCommon.ObstacleTypeList)
            {
                var list = QueryBlkNames(blkType);
                if (list != null)
                {
                    extractBlkList.AddRange(list);
                }

            }
            extractBlkList = extractBlkList.Distinct().ToList();

            if (extractBlkList.Count == 0)
            {
                return;
            }

            var sanitaryTerminalExtractor = new ThSanitaryTerminalRecognitionEngine()
            {
                BlockNameList = extractBlkList,
                LayerFilter = new List<string>(),
            };

            sanitaryTerminalExtractor.Recognize(database, framePts);
            //sanitaryTerminalExtractor.Elements.ForEach(x => SanitaryTerminal.Add(x.Outline as BlockReference));
            SanitaryTerminal.AddRange(sanitaryTerminalExtractor.Elements);
        }

        private void ExtractFurnitureObstacleTemp(Database database, Point3dCollection framePts)
        {
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = ThFloorHeatingCommon.Layer_Obstacle,
            };
            extractService.Extract(database, framePts);

            foreach (var poly in extractService.Polys)
            {
                poly.Closed = true;
                SenitaryTerminalOBBTemp.Add(poly);
            }
        }

        private void ExtractRoomSeparateLine(Database database, Point3dCollection framePts)
        {
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = ThFloorHeatingCommon.Layer_RoomSeparate,
            };
            extractService.Extract(database, framePts);

            var extractServiceLine = new ThExtractLineService()
            {
                ElementLayer = ThFloorHeatingCommon.Layer_RoomSeparate,
            };
            extractServiceLine.Extract(database, framePts);

            var obj = new DBObjectCollection();
            extractService.Polys.ForEach(x => obj.Add(x));
            var lines = ThDrawTool.GetLines(obj);

            RoomSeparateLine.AddRange(lines);
            RoomSeparateLine.AddRange(extractServiceLine.Lines);
        }



        private void ExtractWaterSeparator(Database database, Point3dCollection framePts)
        {
            var extractService = new ThExtractBlockReferenceService()
            {
                BlockName = ThFloorHeatingCommon.BlkName_WaterSeparator,
            };
            extractService.Extract(database, framePts);
            WaterSeparator.AddRange(extractService.Blocks);
        }

        private void ExtractRoomSuggestDist(Database database, Point3dCollection framePts)
        {
            var extractService = new ThExtractTextService()
            {
                ElementLayer = ThFloorHeatingCommon.Layer_RoomSuggestDist,
            };
            extractService.Extract(database, framePts);
            RoomSuggestDist.AddRange(extractService.Texts.OfType<DBText>());
        }


        private void MoveToOrigin(ThBuildingElementVisitorManager vm, ThMEPOriginTransformer transformer)
        {
            vm.DB3ArchWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3WindowVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3CurtainWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorStoneVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorMarkVisitor.Results.ForEach(o =>
            {
                if (o is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Data as Entity);
                }
                transformer.Transform(o.Geometry);
            });
        }

        private ThBuildingElementVisitorManager Extract(Database database)
        {
            //识别门需要柱墙窗
            var visitors = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.DB3ArchWallVisitor);
            extractor.Accept(visitors.DB3ShearWallVisitor);
            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Accept(visitors.DB3ColumnVisitor);
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.DB3WindowVisitor);
            extractor.Accept(visitors.DB3CurtainWallVisitor);
            extractor.Accept(visitors.DB3DoorStoneVisitor);
            extractor.Accept(visitors.DB3DoorMarkVisitor);
            extractor.Extract(database);
            return visitors;
        }
        private List<string> QueryBlkNames(string category)
        {
            var blkName = new List<string>();

            BlockNameDict.TryGetValue(category, out blkName);
            return blkName;
        }
    }
}
