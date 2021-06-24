using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Engine
{
    public class ThFireCompartmentMarkExtractionVisitor : ThAnnotationElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj)
        {
            elements.AddRange(Handle(dbObj));
        }
        public override bool IsAnnotationElement(Entity entity)
        {
            if (entity is DBText dBText)
            {
                return dBText.TextString.Contains('-');
            }
            else if (entity is MText mText)
            {
                return mText.Contents.Contains('-');
            }
            return false;
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotSupportedException();
        }

        public override void DoXClip(List<ThRawIfcAnnotationElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            throw new NotSupportedException();
        }

        private List<ThRawIfcAnnotationElementData> Handle(Entity dbObj)
        {
            var results = new List<ThRawIfcAnnotationElementData>();
            if (IsAnnotationElement(dbObj) && CheckLayerValid(dbObj))
            {
                if (dbObj is DBText dBText)
                {
                    results.Add(CreateAnnotationElementData(dBText));
                }
                else if (dbObj is MText mText)
                {
                    results.Add(CreateAnnotationElementData(mText));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return results;
        }

        private ThRawIfcAnnotationElementData CreateAnnotationElementData(DBText dbText)
        {
            return new ThRawIfcAnnotationElementData()
            {
                Data = dbText,
                Geometry = dbText.TextOBB(),
            };
        }

        private ThRawIfcAnnotationElementData CreateAnnotationElementData(MText mText)
        {
            return new ThRawIfcAnnotationElementData()
            {
                Data = mText,
                Geometry = mText.TextOBB(),
            };
        }
    }
}
