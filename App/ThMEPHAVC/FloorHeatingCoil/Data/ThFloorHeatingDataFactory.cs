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
        public List<Polyline> ObstacleObb { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();
        public List<BlockReference> WaterSeparator { get; set; } = new List<BlockReference>();
        public List<BlockReference> BathRadiator { get; set; } = new List<BlockReference>();

        public ThFloorHeatingDataFactory()
        {
        }
        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractBasicArchitechObject(database, framePts);
            ExtractFurnitureObstacle(database, framePts);
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
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = ThFloorHeatingCommon.Layer_Obstacle,
            };
            extractService.Extract(database, framePts);

            foreach (var poly in extractService.Polys)
            {
                poly.Closed = true;
                ObstacleObb.Add(poly);
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

        private void ExtractBathRadiator(Database database, Point3dCollection framePts)
        {
            var extractService = new ThExtractBlockReferenceService()
            {
                BlockName = ThFloorHeatingCommon.BlkName_BathRadiator,
            };
            extractService.Extract(database, framePts);
            BathRadiator.AddRange(extractService.Blocks);
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

        public static List<Hatch> ExtractHatch(List<string> layerName)
        {
            using (var docLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var hatchList = acadDatabase.ModelSpace
                      .OfType<Hatch>()
                      .Where(o => IsElementLayer(o.Layer, layerName))
                      .ToList();

                return hatchList;
            }
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
