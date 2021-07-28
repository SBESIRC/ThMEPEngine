﻿using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using System.Linq;
using Dreambuild.AutoCAD;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertElementExtractionVisitor : ThDistributionElementExtractionVisitor
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
                results.Add(new ThRawIfcDistributionElementData()
                {
                    Data = new ThBlockReferenceData(br.ObjectId, matrix),
                    Geometry = br.GetTransformedCopy(matrix).GeometricExtents.ToRectangle(),
                });
            }
            return results;
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference br)
            {
                return NameFilter.Contains(ThMEPXRefService.OriginalFromXref(br.GetEffectiveName()));
            }
            return false;
        }

        public override bool CheckLayerValid(Entity curve)
        {
            var layer = curve.LayerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
            return !layer.IsFrozen && !layer.IsOff && !layer.IsHidden;
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
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
