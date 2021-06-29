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
        }

        private List<ThEntityData> HandleCurve(Curve curve)
        {
            var results = new List<ThEntityData>();
            if (IsSpatialElement(curve) && CheckLayerValid(curve))
            {
                results.Add(CreateEntityData(curve, ""));
            }
            return results;
        }

        public override bool IsSpatialElement(Entity entity)
        {
            if (entity is Curve curve)
            {
                return true;
            }
            return false;
        }
        public override bool CheckLayerValid(Entity entity)
        {
            return LayerFilter.Where(o => o.Contains(entity.Layer)).Any();
        }
        private ThEntityData CreateEntityData(Curve curve, string description)
        {
            return new ThEntityData()
            {
                Data = description,
                Geometry = curve
            };
        }

        public override void DoXClip(List<ThEntityData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }
    }
}
