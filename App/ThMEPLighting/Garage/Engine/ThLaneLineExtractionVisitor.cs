using System.Collections.Generic;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using System;

namespace ThMEPLighting.Garage.Engine
{
    public class ThLaneLineExtractionVisitor : ThSpatialElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public ThLaneLineExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
        }
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if(dbObj is Curve curve)
            {
                HandleCurve(elements, curve, matrix);
            }
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is Curve curve)
            {
                HandleCurve(elements, curve, Matrix3d.Identity);
            }
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }
        private void HandleCurve(List<ThRawIfcSpatialElementData> elements, Curve curve, Matrix3d matrix)
        {
            var clone = curve.WashClone() as Curve;
            if(clone != null)
            {
                clone.TransformBy(matrix);
                elements.Add(new ThRawIfcSpatialElementData()
                {
                    Geometry = clone,
                    Data = curve.Layer,
                });
            }
        }
        public override bool IsSpatialElement(Entity entity)
        {
            return entity is Curve;
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
    }
}
