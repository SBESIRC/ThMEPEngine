using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThMEPEngineCore.Engine;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCircuitMarkExtractionVisitor : ThAnnotationElementExtractionVisitor
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
                var clone = entity.Clone() as Entity;
                entities.Add(clone);
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

        public override bool CheckLayerValid(Entity curve)
        {
            var check = false;
            for (int i = 0; i < LayerFilter.Count && !check; i++)
            {
                if (!LayerFilter[i].Contains("*"))
                {
                    check = string.Compare(curve.Layer, LayerFilter[i], true) == 0;
                }
                else
                {
                    var str = LayerFilter[i].Replace("*", "[a-zA-Z]*");
                    check = System.Text.RegularExpressions.Regex.IsMatch(curve.Layer, str);
                }
            }
            return check;
        }
    }
}
