﻿using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThRawBeamExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Curve curve)
            {
                elements.AddRange(Handle(curve, matrix));
            }
            else if (dbObj is Mline mLine)
            {
                elements.AddRange(Handle(mLine, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        private List<ThRawIfcBuildingElementData> Handle(Curve curve, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (CheckLayerValid(curve) && IsBuildElement(curve))
            {
                var clone = curve.GetTransformedCopy(matrix) as Curve;
                if(clone == null)
                {
                    return results;
                }
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = clone,
                });
            }
            return results;
        }

        private List<ThRawIfcBuildingElementData> Handle(Mline mline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (CheckLayerValid(mline) && IsBuildElement(mline))
            {
                var mlineCopy = mline.GetTransformedCopy(matrix) as Mline;
                if (mlineCopy == null)
                {
                    return results;
                }
                var mlineCurves = new DBObjectCollection();
                mlineCopy.Explode(mlineCurves);
                mlineCurves.OfType<Curve>().ForEach(o =>
                {
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = o,
                    });
                });
            }
            return results;
        }
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            return true;
        }
    }
}
