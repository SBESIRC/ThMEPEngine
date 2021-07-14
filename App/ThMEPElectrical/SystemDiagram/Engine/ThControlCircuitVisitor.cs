using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThControlCircuitVisitor : ThEntityCommonExtractionVistor
    {
        public override void DoExtract(List<ThEntityData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        public override void DoExtract(List<ThEntityData> elements, Entity dbObj)
        {
            if (dbObj is Curve curve)
            {
                elements.AddRange(HandleCurve(curve));
            }
            else if (dbObj is DBText text)
            {
                elements.AddRange(HandleCurve(text));
            }
        }

        private List<ThEntityData> HandleCurve(Entity entity)
        {
            var results = new List<ThEntityData>();
            if (IsSpatialElement(entity) && CheckLayerValid(entity))
            {
                results.Add(CreateEntityData(entity, ""));
            }
            return results;
        }

        public override bool IsSpatialElement(Entity entity)
        {
            return entity is Curve || entity is DBText;
        }
        public override bool CheckLayerValid(Entity entity)
        {
            if (entity is Curve curve)
            {
                return LayerFilter.Contains(curve.Layer) || curve.Layer.Contains("CMTB");
            }
            else if (entity is DBText text)
            {
                return text.Layer.Equals("E-FAS-NUMB") && text.TextString.Contains("-WFA");
            }
            else
            {
                return false;
            }
        }
        private ThEntityData CreateEntityData(Entity curve, string description)
        {
            return new ThEntityData()
            {
                Data = description,
                Geometry = curve.Clone() as Entity
            };
        }

        public override void DoXClip(List<ThEntityData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }
    }
}
