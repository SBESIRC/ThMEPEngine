using System;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineExtractionVisitor : ThSpatialElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline));
            }
            else if (dbObj is Ellipse ellipse)
            {
                elements.AddRange(Handle(ellipse));
            }
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotSupportedException();
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            throw new NotSupportedException();
        }

        private List<ThRawIfcSpatialElementData> Handle(Curve polyline)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (IsSpatialElement(polyline) && CheckLayerValid(polyline))
            {
                Curve clone = null;
                if (polyline is Polyline)
                {
                    clone = polyline.WashClone() as Polyline;
                }
                else if (polyline is Ellipse)
                {
                    clone = polyline.WashClone() as Ellipse;
                }

                if (clone != null)
                {
                    results.Add(CreateSpatialElementData(clone, ""));
                }
            }
            return results;
        }


        private ThRawIfcSpatialElementData CreateSpatialElementData(Curve curve, string description)
        {
            return new ThRawIfcSpatialElementData()
            {
                Geometry = curve,
                Data = description
            };
        }
        public override bool IsSpatialElement(Entity entity)
        {
            return entity.ObjectId.IsValid;
        }
    }
}
