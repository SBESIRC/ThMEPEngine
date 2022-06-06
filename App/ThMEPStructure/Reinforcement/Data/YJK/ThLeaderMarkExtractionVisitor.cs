using System.Collections.Generic;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.Reinforcement.Data.YJK
{
    public class ThLeaderMarkExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间
            if (blockTableRecord.IsLayout)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is DBText text)
            {
                elements.AddRange(Handle(text, matrix));
            }
            else if(dbObj is Line line)
            {
                elements.AddRange(Handle(line, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o =>
                {
                    if(o.Geometry is Curve curve)
                    {
                        return !xclip.Contains(o.Geometry as Curve);
                    }
                    else if(o.Geometry is DBText text)
                    {
                        return !xclip.Contains(text.Position);
                    }
                    else
                    {
                        return false;
                    }
                });
            }
        }

        private List<ThRawIfcBuildingElementData> Handle(Curve curve, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(curve) && CheckLayerValid(curve))
            {
                var clone = curve.WashClone();
                if(clone!=null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = clone,
                    });
                }
            }
            return results;
        }

        private List<ThRawIfcBuildingElementData> Handle(DBText text, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(text) && CheckLayerValid(text))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = text.GetTransformedCopy(matrix),
                });
            }
            return results;
        }
    }
}
