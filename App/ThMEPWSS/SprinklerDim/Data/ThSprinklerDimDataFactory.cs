using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
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
using ThMEPEngineCore.Config;

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
        public List<ThIfcRoom> RoomsData { get; set; }
        public List<Curve> LinePipeData { get; set; }
        public List<Entity> LinePipeTextData { get; set; }
        public List<Entity> PreviousData { get; set; }
        public ThSprinklerDimDataFactory()
        {
            SprinklerPtData = new List<Point3d>();
            TchPipeData = new List<ThIfcFlowSegment>();
            LinePipeData = new List<Curve>();
            LinePipeTextData = new List<Entity>();
            AxisCurves = new List<Curve>();
            RoomsData = new List<ThIfcRoom>();
            PreviousData = new List<Entity>();

        }

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractBasicArchitechObject(database, framePts);
            ExtractRoom(database, framePts);
            GetAllAxisCurves(database, framePts);
            GetSprinklerPtData(database, framePts);
            GetSrpinklerPtBlkData(database, framePts);
            GetTCHPipeData(database, framePts);
            GetLinePipeData(database, framePts);
            GetPreviousData(database, framePts);
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
                //new ThSprinklerRoomExtractor()
                //{
                //    IsWithHole=true,
                //    UseDb3Engine=true,
                //    Transformer = Transformer,
                //},
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
            visitors.ShearWallVisitor.LayerFilter = ThExtractShearWallConfig.Instance.LayerInfos.Select(x => x.Layer).ToHashSet();

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

        private void GetSrpinklerPtBlkData(Database database, Point3dCollection framePts)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var blkNameFilter = ThSprinklerDimCommon.BlkFilter_Sprinkler;
                var Blocks = acadDatabase.ModelSpace
                      .OfType<BlockReference>()
                      .Where(b => !b.BlockTableRecord.IsNull)
                      .Where(b => IsBlockName(b.GetEffectiveName(), blkNameFilter))
                      .ToList();

                if (framePts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Blocks.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(framePts);
                    Blocks = objs.Cast<BlockReference>().ToList();
                }

                SprinklerPtData.AddRange(Blocks.Select(x => x.Position));
            }
        }

        private static bool IsBlockName(string blkName, string blkNameFilter)
        {
            var ismatch = blkName.ToUpper().Contains(blkNameFilter.ToUpper());
            return ismatch;
        }

        private void GetTCHPipeData(Database database, Point3dCollection framePts)
        {
            var TCHPipeRecognize = new ThTCHPipeRecognitionEngine()
            {
                LayerFilter = new List<string> { ThSprinklerDimCommon.Layer_Pipe },
            };
            TCHPipeRecognize.RecognizeMS(database, framePts);
            TchPipeData.AddRange(TCHPipeRecognize.Elements.OfType<ThIfcFlowSegment>().ToList());
        }

        private void GetLinePipeData(Database database, Point3dCollection framePts)
        {

            var itemExtractPl = new ExtractPipeMSPolyline()
            {
                ElementLayer = ThSprinklerDimCommon.LayerFilter_SPRL,
            };

            itemExtractPl.Extract(database, framePts);
            LinePipeData.AddRange(itemExtractPl.Polys);


            var itemExtractLine = new ExtractPipeMSLine()
            {
                ElementLayer = ThSprinklerDimCommon.LayerFilter_SPRL,
            };

            itemExtractLine.Extract(database, framePts);
            LinePipeData.AddRange(itemExtractLine.Lines);


            var itemExtractText = new ExtractPipeMSText();
            itemExtractText.Extract(database, framePts);
            LinePipeTextData.AddRange(itemExtractText.Texts);

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

        private void ExtractRoom(Database database, Point3dCollection framePts)
        {
            var isSupportMpolygon = true;
            var isWithHole = true;

            var roomBuilder = new ThRoomBuilderEngine()
            { IsSupportMPolygon = isSupportMpolygon, };

            var rooms = roomBuilder.BuildFromMS(database, framePts, isWithHole);
            RoomsData.AddRange(rooms);
        }

        private void GetPreviousData(Database database, Point3dCollection framePts)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                //之前的dim
                var dimsTemp = acadDatabase.ModelSpace
                    .OfType<Dimension>()
                    .Where(o => o.Layer == ThSprinklerDimCommon.Layer_Dim)
                    .ToList();

                if (framePts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(dimsTemp.ToCollection());
                    var items = spatialIndex.SelectCrossingPolygon(framePts).OfType<Dimension>().ToList();

                    PreviousData.AddRange(items);
                }

                //之前的没标注的圆
                var circleTemp = acadDatabase.ModelSpace
                        .OfType<Circle>()
                        .Where(o => o.Layer == ThSprinklerDimCommon.Layer_UnTagX || o.Layer == ThSprinklerDimCommon.Layer_UnTagY)
                        .ToList();

                if (framePts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(circleTemp.ToCollection());
                    var items = spatialIndex.SelectCrossingPolygon(framePts).OfType<Circle>().ToList();

                    PreviousData.AddRange(items);
                }
            }
        }

    }

    class ExtractPipeMSPolyline : ThExtractPolylineService
    {
        public override bool IsElementLayer(string layer)
        {
            var ismatch = false;
            foreach (var layerP in SplitLayers)
            {
                ismatch = layer.ToUpper().Contains(layerP.ToUpper());
                if (ismatch == true)
                {
                    break;
                }
            }

            return ismatch;
        }
    }

    class ExtractPipeMSLine : ThExtractLineService
    {
        public override bool IsElementLayer(string layer)
        {
            var ismatch = false;
            foreach (var layerP in SplitLayers)
            {
                ismatch = layer.ToUpper().Contains(layerP.ToUpper());
                if (ismatch == true)
                {
                    break;
                }
            }

            return ismatch;
        }
    }

    class ExtractPipeMSText : ThExtractTextService
    {
        public override bool IsElementLayer(string layer)
        {
            var ismatch = (layer.ToUpper().Contains(ThSprinklerDimCommon.LayerFilter_W) && layer.ToUpper().Contains(ThSprinklerDimCommon.LayerFilter_NOTE)) ||
                        (layer.ToUpper().Contains(ThSprinklerDimCommon.LayerFilter_W) && layer.ToUpper().Contains(ThSprinklerDimCommon.LayerFilter_DIMS));

            return ismatch;
        }
    }


}
