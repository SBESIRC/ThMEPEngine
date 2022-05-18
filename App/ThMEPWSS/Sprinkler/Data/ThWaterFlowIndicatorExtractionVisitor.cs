using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThWaterFlowIndicatorExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public List<string> NameFilter { get; set; }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid && elements.Count != 0)
            {
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is Curve curve)
            {
                return xclip.Contains(curve);
            }
            else
            {
                return false;
            }
        }

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
                if (matrix == Matrix3d.Identity)
                {
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = br,
                        Geometry = br.GeometricExtents.ToRectangle(),
                    });
                }
                else
                {
                    using (var acad = AcadDatabase.Use(br.Database))
                    {
                        var brCopy = br.GetTransformedCopy(matrix);
                        acad.ModelSpace.Add(brCopy);
                        results.Add(new ThRawIfcDistributionElementData()
                        {
                            Data = brCopy,
                            Geometry = brCopy.GeometricExtents.ToRectangle(),
                        });
                        brCopy.Erase();
                    }
                }
            }
            return results;
        }

        public override bool IsDistributionElement(Entity entity)
        {
            try
            {
                if (entity is BlockReference br)
                {
                    var effectiveName = ThMEPXRefService.OriginalFromXref(br.GetEffectiveName());
                    return NameFilter.Contains(effectiveName.Replace(" ", ""));
                }
                return false;
            }
            catch
            {
                // BlockReference.IsDynamicBlock可能会抛出异常
                // 这里可以忽略掉这些有异常情况的动态块
                return false;
            }
        }

        public override bool CheckLayerValid(Entity entity)
        {
            if (!entity.LayerId.IsValid)
            {
                return false;
            }
            var layer = entity.LayerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
            return !layer.IsFrozen && !layer.IsOff && !layer.IsHidden;
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsFromExternalReference || blockTableRecord.IsFromOverlayReference)
            {
                return false;
            }
            // 不支持图纸空间
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
}
