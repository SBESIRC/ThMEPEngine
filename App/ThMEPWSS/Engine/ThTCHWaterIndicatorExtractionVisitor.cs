using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Engine
{
    public class ThTCHWaterIndicatorExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj.IsTCHPipeValue())
            {
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = dbObj,
                    Geometry = dbObj.GeometricExtents.ToRectangle(),
                });
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid && elements.Count != 0)
            {
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override bool IsDistributionElement(Entity e)
        {
            return e.IsTCHPipeValue();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }
    }
}
