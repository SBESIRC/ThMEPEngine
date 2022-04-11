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
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.DrainageADPrivate.Data
{
    public class ThValveExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public List<string> BlockNameList { get; set; } = new List<string>();
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (CheckLayerValid(dbObj) && IsDistributionElement(dbObj))
            {
                elements.AddRange(Handle((dbObj as BlockReference), matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {

        }

        public override bool IsDistributionElement(Entity e)
        {
            var bReturn = false;
            if (e is BlockReference blkref)
            {
                var blkName = blkref.GetEffectiveName();
                bReturn = BlockNameList.Contains(blkName);
            }
            return bReturn;
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

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            var geom = br.GetTransformedCopy(matrix);
            results.Add(new ThRawIfcDistributionElementData()
            {
                Geometry = geom,
                Data = br.GetEffectiveName(),
            });

            return results;
        }
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略动态块
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }
            //忽略外参
            if (blockTableRecord.IsFromExternalReference)
            {
                return false;
            }
            //忽略附着
            if (blockTableRecord.IsFromOverlayReference)
            {
                return false;
            }
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
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
    }

    public class ThValveExtractionEngine : ThDistributionElementExtractionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();
        public List<string> BlockNameList { get; set; } = new List<string>();

        public override void Extract(Database database)
        {

        }
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThValveExtractionVisitor()
            {
                LayerFilter = LayerFilter.ToHashSet(),
                BlockNameList = BlockNameList,
            };

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }
        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }

    }

    public class ThValveRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();
        public List<string> BlockNameList { get; set; } = new List<string>();

        public override void RecognizeEditor(Point3dCollection polygon)
        {

        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            //---提取
            var extractEngine = new ThValveExtractionEngine()
            {
                LayerFilter = LayerFilter,
                BlockNameList = BlockNameList,
            };

            extractEngine.ExtractFromMS(database);

            //--转回原点
            var centerPt = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(centerPt);
            var newFrame = transformer.Transform(polygon);
            extractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            //--识别框内
            Recognize(extractEngine.Results, newFrame);
            //--转回
            Elements.ForEach(x => transformer.Reset(x.Outline));

        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
        }

        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var collection = datas.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);
            var item = spatialIndex.SelectCrossingPolygon(polygon);
            datas.Where(o => item.Contains(o.Geometry)).ForEach(o =>
            {
                Elements.Add(new ThIfcDistributionFlowElement()
                {
                    Outline = (BlockReference)o.Geometry,
                    Name = (string)o.Data,
                });
            });
        }

    }
}
