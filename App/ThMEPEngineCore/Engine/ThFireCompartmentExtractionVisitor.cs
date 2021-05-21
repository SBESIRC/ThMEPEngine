using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    class ThFireCompartmentExtractionVisitor : ThSpatialElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline));
            }
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //处理外参DoXClip的
            throw new NotImplementedException();
        }
        public override bool IsSpatialElement(Entity entity)
        {
            //认为长度>10 才可能是防火分区
            if(entity is Polyline polyline)
            {
                if (polyline.Length > 10)
                    return true;
            }
            return false;
        }

        private List<ThRawIfcSpatialElementData> Handle(Polyline polyline)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (IsSpatialElement(polyline) && CheckLayerValid(polyline))
            {
                var newFrame = ThMEPFrameService.NormalizeEx(polyline);
                results.Add(CreateSpatialElementData(newFrame, ""));
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
    }
}
