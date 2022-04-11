using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3ColumnExtractionVisitor : ThBuildingElementExtractionVisitor
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
            if (dbObj is Hatch hatch)
            {
                elements.AddRange(HandleHatch(hatch, matrix));
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

        public override bool IsBuildElement(Entity entity)
        {
            if (entity.Hyperlinks.Count > 0)
            {
                return ThStructureUtils.OriginalFromXref(entity.Layer) == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_STRU_COLUMN;
            }
            return false;
        }

        private List<ThRawIfcBuildingElementData> HandleHatch(Hatch hatch, Matrix3d matrix)
        {
            List<Curve> curves = new List<Curve>();
            if (IsBuildElement(hatch) && CheckLayerValid(hatch))
            {
                hatch.Boundaries().ForEach(o =>
                {
                    if (o is Circle circle)
                    {
                        // 圆形柱
                        // 细化成封闭多段线
                        var poly = circle.TessellateCircleWithArc(ThMEPEngineCoreCommon.CircularColumnTessellateArcLength);
                        poly.TransformBy(matrix);
                        curves.Add(poly);
                    }
                    else
                    {
                        o.TransformBy(matrix);
                        curves.Add(o);
                    }
                });
            }
            return curves.Select(o => CreateBuildingElementData(o)).ToList();
        }

        private ThRawIfcBuildingElementData CreateBuildingElementData(Curve curve)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = curve,
            };
        }
    }
}
