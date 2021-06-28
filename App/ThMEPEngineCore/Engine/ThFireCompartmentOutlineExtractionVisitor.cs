using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    class ThFireCompartmentOutlineExtractionVisitor : ThSpatialElementExtractionVisitor
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
            if (entity is Polyline polyline)
            {
                // 过滤掉面积不大于5平方米的区域
                return polyline.Area > 5E+6;
            }
            return false;
        }

        private List<ThRawIfcSpatialElementData> Handle(Polyline polyline)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (IsSpatialElement(polyline) && CheckLayerValid(polyline))
            {
                var newFrame = ThMEPFrameService.NormalizeEx(polyline, 500.0);
                if(IsSpatialElement(newFrame))
                {
                    results.Add(CreateSpatialElementData(newFrame, ""));
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
    }
}
