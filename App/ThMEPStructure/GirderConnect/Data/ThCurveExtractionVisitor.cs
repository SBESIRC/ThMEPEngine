using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.GirderConnect.Data
{
    public class ThCurveExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public Func<Entity, bool> CheckLayerQualified { get; set; }
        public ThCurveExtractionVisitor()
        {
            CheckLayerQualified = base.CheckLayerValid;
        }
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(HandlePolyline(polyline, matrix));
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

        private List<ThRawIfcBuildingElementData> HandlePolyline(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                var clone = polyline.WashClone();
                if(clone!=null && clone is Polyline poly)
                {
                    clone.TransformBy(matrix);
                    results.Add(CreateBuildingElementData(poly));
                }
            }
            return results;
        }

        private ThRawIfcBuildingElementData CreateBuildingElementData(Curve curve)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = curve,
            };
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckLayerQualified(curve);
        }
    }
}
