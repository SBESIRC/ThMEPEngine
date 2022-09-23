using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThAIRoomOutlineExtractionVisitor : ThSpatialElementExtractionVisitor
    {
        public bool IsSupportMPolygon { get; set; } = false;

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline,Matrix3d.Identity));
            }
            else if(dbObj is MPolygon mpolygon)
            {
                if(IsSupportMPolygon)
                {
                    elements.AddRange(Handle(mpolygon, Matrix3d.Identity));
                }
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

        private List<ThRawIfcSpatialElementData> Handle(MPolygon mPolygon, Matrix3d matrix)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (IsSpatialElement(mPolygon) && CheckLayerValid(mPolygon))
            {
                var clone = mPolygon.GetTransformedCopy(matrix);
                if (clone != null)
                {
                    results.Add(CreateSpatialElementData(clone, ""));
                }
            }
            return results;
        }

        private ThRawIfcSpatialElementData CreateSpatialElementData(Entity curve, string description)
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
