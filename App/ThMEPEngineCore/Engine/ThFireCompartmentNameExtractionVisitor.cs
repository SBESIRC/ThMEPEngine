using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Engine
{
    public class ThFireCompartmentNameExtractionVisitor : ThSpatialElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is DBText dBText)
            {
                elements.AddRange(Handle(dBText));
            }
            if (dbObj is MText mText)
            {
                elements.AddRange(Handle(mText));
            }
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //处理外参DoXClip的
            throw new NotImplementedException();
        }
        public override bool IsSpatialElement(Entity entity)
        {
            //认为防火分区一定是[楼层-编号]的格式
            if (entity is DBText dBText)
            {
                if (dBText.TextString.Contains('-'))
                    return true;
            }
            if (entity is MText mText)
            {
                if (mText.Contents.Contains('-'))
                    return true;
            }
            return false;
        }

        private List<ThRawIfcSpatialElementData> Handle(Entity dBText)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (IsSpatialElement(dBText) && CheckLayerValid(dBText))
            {
                results.Add(CreateSpatialElementData(dBText, ""));
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
    }
}
