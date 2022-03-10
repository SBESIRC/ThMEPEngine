using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWDeepWellPumpVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
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

        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference reference)
            {
                var name = reference.GetEffectiveName();
                if (name.Contains("潜水泵"))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            elements.Add(new ThRawIfcDistributionElementData()
            {
                Data = blkref.GetEffectiveName(),
                Geometry = blkref,
            });
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 不支持外部参照
            if (blockTableRecord.IsFromExternalReference)
            {
                return false;
            }
            // 不支持覆盖块
            if (blockTableRecord.IsFromOverlayReference)
            {
                return false;
            }
            // 不支持图纸空间和匿名块
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
    class ThWDeepWellPumpEngine : ThDistributionElementRecognitionEngine
    {
        public List<ThRawIfcDistributionElementData> Datas { get; set; }
        public override void Recognize(Database database, Point3dCollection polygon)
        {

        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var extractor = new ThDistributionElementExtractor();
            var deepWellPumpVisitor = new ThWDeepWellPumpVisitor();
            extractor.Accept(deepWellPumpVisitor);

            extractor.ExtractFromMS(database);

            var dbObjs = deepWellPumpVisitor.Results.Select(o => o.Geometry).ToCollection(); 

            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Datas = deepWellPumpVisitor.Results.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            Elements.AddRange(Datas.Select(o => o.Geometry).Cast<Entity>().Select(x => new ThIfcDistributionFlowElement() { Outline = x }));
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
    }
}
