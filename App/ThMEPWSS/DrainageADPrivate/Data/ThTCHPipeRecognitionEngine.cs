using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageADPrivate.Data
{
    public class ThTCHPipeExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            if (CheckLayerValid(dbObj) && dbObj.IsTCHPipe() && IsHorizontalPipe(dbObj))
            {
                var geom = HandleTCHPipe(dbObj);

                elements.Add(new ThRawIfcFlowSegmentData()
                {
                    Data = dbObj,
                    Geometry = geom
                });
            }
        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (CheckLayerValid(dbObj) && dbObj.IsTCHPipe() && IsHorizontalPipe(dbObj))
            {
                var geom = HandleTCHPipe(dbObj);
                geom.TransformBy(matrix);

                elements.Add(new ThRawIfcFlowSegmentData()
                {
                    Data = dbObj,
                    Geometry = geom
                });
            }
        }

        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {

        }

        public override bool IsFlowSegment(Entity e)
        {
            return e.IsTCHPipe();
        }
        public override bool CheckLayerValid(Entity e)
        {
            var bReturn = false;
            if (LayerFilter.Count > 0)
            {
                bReturn = LayerFilter.Contains(e.Layer);
            }
            else
            {
                bReturn = true;
            }
            return bReturn;
        }

        public override bool IsFlowSegmentBlock(BlockTableRecord blockTableRecord)
        {
            //忽略外参
            if (blockTableRecord.IsFromExternalReference)
            {
                return false;
            }

            // 忽略动态块
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }


        private bool IsHorizontalPipe(Entity dbObj)
        {
            var bIsVP = false;
            var pipeParameters = ThOPMTools.GetOPMProperties(dbObj.Id);

            if (pipeParameters.ContainsKey("起点标高") && pipeParameters.ContainsKey("终点标高") && pipeParameters.ContainsKey("管长"))
            {
                var start = Convert.ToDouble(pipeParameters["起点标高"]);
                var end = Convert.ToDouble(pipeParameters["终点标高"]);

                //没有坡度的管子
                var verticalDiff = Math.Abs(end - start);
                if (Math.Abs(verticalDiff - 0) <= 0.1)
                {
                    //（起点-终点） =0 
                    bIsVP = true;
                }
            }
            return bIsVP;
        }

        /// <summary>
        /// 抽象天正管线
        /// </summary>
        /// <param name="pipe"></param>
        private Line HandleTCHPipe(Entity pipe)
        {
            var extent = pipe.GeometricExtents;
            var pl = new Line(extent.MinPoint, extent.MaxPoint);
            pl.Layer = pipe.Layer;

            return pl;
        }

    }

    public class ThTCHPipeExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();
        public override void Extract(Database database)
        {
            throw new NotSupportedException();
        }
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThTCHPipeExtractionVisitor()
            {
                LayerFilter = LayerFilter,
            };
            var extractor = new ThFlowSegmentExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }

    public class ThTCHPipeRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();

        public override void Recognize(Database database, Point3dCollection polygon)
        { }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            var collection = datas.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);
            var pipes = spatialIndex.SelectCrossingPolygon(polygon);
            datas.Where(o => pipes.Contains(o.Geometry)).ForEach(o =>
            {
                Elements.Add(new ThIfcFlowSegment()
                {
                    Outline = o.Geometry,
                });
            });
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            //---提取
            var pipeExtractEngine = new ThTCHPipeExtractionEngine()
            {
                LayerFilter = LayerFilter,
            };

            pipeExtractEngine.ExtractFromMS(database);
            //--转回原点
            var centerPt = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(centerPt);
            var newFrame = transformer.Transform(polygon);
            pipeExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            //--识别框内
            Recognize(pipeExtractEngine.Results, newFrame);
            //--转回原位置
            Elements.ForEach(x => transformer.Reset(x.Outline));
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {

        }
    }
}

