using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThTCHWireDim2ExtractionVisitor : ThAnnotationElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj)
        {
            elements.AddRange(Handle(dbObj));
        }

        public override void DoXClip(List<ThRawIfcAnnotationElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        private List<ThRawIfcAnnotationElementData> Handle(Entity entity)
        {
            var entities = new List<Entity>();
            if (IsAnnotationElement(entity) && CheckLayerValid(entity))
            {
                entities.Add(entity);
            }
            return entities.Select(o => CreateAnnotationElementData(o)).ToList();
        }

        private ThRawIfcAnnotationElementData CreateAnnotationElementData(Entity entity)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                return new ThRawIfcAnnotationElementData()
                {
                    Data = entity,
                };
            }
        }

        public override bool IsAnnotationElement(Entity entity)
        {
            return ThMEPTCHService.IsTCHWireDim2(entity) || ThMEPTCHService.IsTCHMULTILEADER(entity);
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }
    }
}
