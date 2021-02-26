﻿using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThArchitectureWallExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                HandleCurve(polyline, matrix);
            }
        }
        public override void DoXClip(BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                Results.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }
        public override bool IsBuildElement(Entity entity)
        {
            if (entity.Hyperlinks.Count > 0)
            {
                var thPropertySet = ThPropertySet.CreateWithHyperlink(entity.Hyperlinks[0].Description);
                return thPropertySet.IsArchWall;
            }
            return false;
        }
        private void HandleCurve(Polyline polyline, Matrix3d matrix)
        {
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                Results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = polyline.GetTransformedCopy(matrix),
                });
            }
        }
    }
}
