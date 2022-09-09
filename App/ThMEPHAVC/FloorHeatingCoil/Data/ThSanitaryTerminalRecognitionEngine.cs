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
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    internal class ThSanitaryTerminalExtractionVisitor : ThDistributionElementExtractionVisitor
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
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }
        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                return false;
            }
        }
        public override bool IsDistributionElement(Entity e)
        {
            var bReturn = false;
            if (e is BlockReference blkref)
            {
                var blkName = blkref.GetEffectiveName().ToUpper();
                bReturn = BlockNameList.Where(o => blkName.EndsWith(o.ToUpper())).Any();
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

            //var geom = br.GetTransformedCopy(matrix);
            //var data = new ThBlockReferenceData(br.ObjectId, matrix);
            //results.Add(new ThRawIfcDistributionElementData()
            //            {
            //                Geometry = geom,
            //                Data = data,
            //            });

            return results;
        }
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
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

    internal class ThSanitaryTerminalExtractionEngine : ThDistributionElementExtractionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();
        public List<string> BlockNameList { get; set; } = new List<string>();

        public override void Extract(Database database)
        {
            var visitor = new ThSanitaryTerminalExtractionVisitor()
            {
                LayerFilter = LayerFilter.ToHashSet(),
                BlockNameList = BlockNameList,
            };

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }
        public override void ExtractFromMS(Database database)
        {

        }
        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }


    }

    internal class ThSanitaryTerminalRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();
        public List<string> BlockNameList { get; set; } = new List<string>();

        public override void RecognizeEditor(Point3dCollection polygon)
        {

        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            //---提取
            var sanitaryTerminal = new ThSanitaryTerminalExtractionEngine()
            {
                LayerFilter = LayerFilter,
                BlockNameList = BlockNameList,
            };

            sanitaryTerminal.Extract(database);

            //--转回原点
            var centerPt = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(centerPt);
            var newFrame = transformer.Transform(polygon);
            sanitaryTerminal.Results.ForEach(x => transformer.Transform(x.Geometry));
            //--识别框内
            Recognize(sanitaryTerminal.Results, newFrame);
            //--转回
            Elements.ForEach(x => transformer.Reset(x.Outline));
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
        }

        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var collection = datas.Select(o => o.Geometry).ToCollection();
            var pipes = new DBObjectCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);
                pipes = spatialIndex.SelectCrossingPolygon(polygon);
            }
            else
            {
                pipes = collection;
            }

            datas.Where(o => pipes.Contains(o.Geometry)).ForEach(o =>
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
