﻿using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineExtractionVisitor : ThSpatialElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline,Matrix3d.Identity));
            }
            else if (dbObj is Ellipse ellipse)
            {
                elements.AddRange(Handle(ellipse, Matrix3d.Identity));
            }
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline, matrix));
            }
            else if (dbObj is Ellipse ellipse)
            {
                elements.AddRange(Handle(ellipse, matrix));
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

        private List<ThRawIfcSpatialElementData> Handle(Curve polyline,Matrix3d matrix)
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
                    clone.TransformBy(matrix);
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
