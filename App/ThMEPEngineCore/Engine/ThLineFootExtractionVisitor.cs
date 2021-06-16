using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThLineFootExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline, matrix));
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
        private List<ThRawIfcBuildingElementData> Handle(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = polyline.GetTransformedCopy(matrix),
                });
            }
            return results;
        }

        public override bool IsBuildElement(Entity entity)
        {
            if (entity.Hyperlinks.Count > 0)
            {
                var thPropertySet = ThPropertySet.CreateWithHyperlink(entity.Hyperlinks[0].Description);
                return thPropertySet.IsLineFoot;
            }
            return false;
        }
    }
}
