using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
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
        //----input
        public ThMEPOriginTransformer Transformer { get; set; }
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        //----output
        public List<ThExtractorBase> Extractors { get; set; }
        public List<ThIfcDistributionFlowElement> SanitaryTerminal { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Polyline> SenitaryTerminalOBBTemp { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();
        //public List<DBText> RoomSuggestDist { get; set; } = new List<DBText>();
        public List<BlockReference> WaterSeparator { get; set; } = new List<BlockReference>();
        public List<BlockReference> BathRadiator { get; set; } = new List<BlockReference>();
        //public List<BlockReference> RoomRouteSuggestBlk { get; set; } = new List<BlockReference>();
        //   public List<Polyline> RoomSetFrame { get; set; } = new List<Polyline>();

        public ThFloorHeatingDataFactory()
        {
        }
        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractBasicArchitechObject(database, framePts);
            ExtractFurnitureObstacle(database, framePts);
            ExtractFurnitureObstacleTemp(database, framePts);
            ExtractRoomSeparateLine(database, framePts);
            ExtractWaterSeparator(database, framePts);
            ExtractBathRadiator(database, framePts);
        }



        private void ExtractBasicArchitechObject(Database database, Point3dCollection framePts)
        {
            Extractors = new List<ThExtractorBase>()
            {
                new ThFloorHeatingDoorExtractor()
                {
                    ElementLayer = "AI-门",
                    Transformer = Transformer,
                    //VisitorManager = manger,
                },
                new ThFloorHeatingRoomExtractor()
                {
                    IsWithHole = true,
                    UseDb3Engine = true,
                    Transformer = Transformer,
                },
                new ThFloorHeatingRoomMarkExtractor()
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
            //var extractService = new ThBlockReferenceExtractor()
            var extractService = new ThExtractBlockReferenceService()
            {
                BlockName = ThFloorHeatingCommon.BlkName_WaterSeparator,
            };
            extractService.Extract(database, framePts);
            WaterSeparator.AddRange(extractService.Blocks);
        }

        private void ExtractBathRadiator(Database database, Point3dCollection framePts)
        {
            //var extractService = new ThBlockReferenceExtractor()
            var extractService = new ThExtractBlockReferenceService()
            {
                BlockName = ThFloorHeatingCommon.BlkName_BathRadiator,
            };
            extractService.Extract(database, framePts);
            BathRadiator.AddRange(extractService.Blocks);
        }

        private List<string> QueryBlkNames(string category)
        {
            var blkName = new List<string>();

            BlockNameDict.TryGetValue(category, out blkName);
            return blkName;
        }

        public static List<Polyline> ExtractPolylineMsNotClone(Database database, List<string> layers)
        {
            var polys = new List<Polyline>();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Polys = acadDatabase.ModelSpace
                  .OfType<Polyline>()
                  .Where(o => IsElementLayer(o.Layer, layers))
                  .OfType<Polyline>()
                  .ToList();

                polys.AddRange(Polys);
            }

            return polys;
        }

        private static bool IsElementLayer(string layer, List<string> layers)
        {
            var breturn = false;
            if (layers == null || layers.Count == 0)
            {
                //不考虑图层
                breturn = true;
            }
            else
            {
                foreach (var layerContainer in layers)
                {
                    if (layerContainer.ToUpper() == layer.ToUpper())
                    {
                        breturn = true;
                        break;
                    }
                }
            }
            return breturn;
        }
    }
}
