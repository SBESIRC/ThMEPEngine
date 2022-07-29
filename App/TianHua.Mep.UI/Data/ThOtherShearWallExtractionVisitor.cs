using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Mep.UI.Data
{
    public class ThOtherShearWallExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        private const double ArcTessellationLength = 100.0;
        public List<ThBuildingElementExtractionVisitor> BlackVisitors { get; set; }
        public ThOtherShearWallExtractionVisitor()
        {
            BlackVisitors = new List<ThBuildingElementExtractionVisitor>();
        }

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

        private List<ThRawIfcBuildingElementData> HandleHatch(Hatch hatch, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsSolid(hatch) && !IsExistInBlacks(hatch) && IsBuildElement(hatch) &&
                CheckLayerValid(hatch) && hatch.Visible && hatch.Bounds.HasValue)
            {
                var polygons = HatchToPolygons(hatch.GetTransformedCopy(matrix) as Hatch);
                polygons.OfType<Entity>().ForEach(e =>
                {
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = e,
                    });
                });
            }
            return results;
        }



        private List<ThRawIfcBuildingElementData> HandleSolid(Solid solid, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (!IsExistInBlacks(solid) && IsBuildElement(solid) &&
                CheckLayerValid(solid) && solid.Visible && solid.Bounds.HasValue)
            {
                // 可能存在2D Solid不规范的情况
                // 这里将原始2d Solid“清洗”处理
                var clone = solid.WashClone();
                if(clone!=null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = clone.ToPolyline(),
                    });
                }
            }
            return results;
        }

        private bool IsExistInBlacks(Entity entity)
        {
            return BlackVisitors.Where(o => o.IsBuildElement(entity) && o.CheckLayerValid(entity)).Any();
        }

        private DBObjectCollection HatchToPolygons(Hatch hatch)
        {
            using (var ov = new ThCADCoreNTSArcTessellationLength(ArcTessellationLength))
            {
                return hatch.BoundariesEx();
            }
        }

        private bool IsSolid(Hatch hatch)
        {
            return hatch.PatternName.ToUpper() == "SOLID";
        }
    }
}
