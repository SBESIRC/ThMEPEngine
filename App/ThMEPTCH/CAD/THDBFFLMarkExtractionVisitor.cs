using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.CAD
{
    public class THDBFFLMarkExtractionVisitor : ThAnnotationElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(Handle(dbObj, matrix));
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj)
        {
            elements.AddRange(Handle(dbObj, Matrix3d.Identity));
        }

        public override void DoXClip(List<ThRawIfcAnnotationElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //throw new NotImplementedException();
        }

        private List<ThRawIfcAnnotationElementData> Handle(Entity e, Matrix3d matrix)
        {
            var results = new List<ThRawIfcAnnotationElementData>();
            if (IsAnnotationElement(e) && CheckLayerValid(e))
            {
                results.Add(new ThRawIfcAnnotationElementData()
                {
                    Geometry = e.GetTransformedCopy(matrix),
                });
            }
            else if(e.IsTCHText() && CheckLayerValid(e))
            {
                results.Add(new ThRawIfcAnnotationElementData()
                {
                    Geometry = e,
                });
            }
            return results;
        }

        public override bool IsAnnotationElement(Entity e)
        {
            return (e is DBText) || (e is MText) ;
        }
    }
}
