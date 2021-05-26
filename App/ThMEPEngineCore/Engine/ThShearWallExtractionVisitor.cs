using System;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using NetTopologySuite.Geometries;

namespace ThMEPEngineCore.Engine
{
    public class ThShearWallExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        private const double ArcTessellationLength = 100.0;

        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Hatch hatch)
            {
                elements.AddRange(HandleHatch(hatch, matrix));
            }
            else if (dbObj is Solid solid)
            {
                elements.AddRange(HandleSolid(solid, matrix));
            }
        }

        private List<ThRawIfcBuildingElementData> HandleHatch(Hatch hatch, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(hatch) && CheckLayerValid(hatch))
            {
                var polygons = HatchToPolygons(hatch.GetTransformedCopy(matrix) as Hatch);
                foreach (var polygon in polygons)
                {
                    // 把“甜甜圈”式的带洞的Polygon（有且只有洞）转成不带洞的Polygon
                    // 在区域划分时，剪力墙是“不可以用”区域，剪力墙的外部和内部都是“可用区域”
                    // 通过这种处理，将剪力墙的外部和内部区域联通起来，从而获取正确的可用区域
                    ThPolygonToGapPolylineService.ToGapPolyline(polygon).ForEach(o =>
                    {
                        results.Add(new ThRawIfcBuildingElementData()
                        {
                            Geometry = o,
                        });
                    });
                }
            }
            return results;
        }

        private List<Polygon> HatchToPolygons(Hatch hatch)
        {
            using (var ov = new ThCADCoreNTSArcTessellationLength(ArcTessellationLength))
            {
                return hatch.ToPolygons();
            }
        }

        private List<ThRawIfcBuildingElementData> HandleSolid(Solid solid, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(solid) && CheckLayerValid(solid))
            {
                // 可能存在2D Solid不规范的情况
                // 这里将原始2d Solid“清洗”处理
                var clone = solid.WashClone();
                clone.TransformBy(matrix);
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = clone.ToPolyline(),
                });
            }
            return results;
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o =>
                {
                    if (o.Geometry is Polyline polyline)
                    {
                        return !xclip.Contains(polyline);
                    }
                    else if (o.Geometry is MPolygon mPolygon)
                    {
                        return !xclip.Contains(mPolygon);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                });
            }
        }
    }
}
