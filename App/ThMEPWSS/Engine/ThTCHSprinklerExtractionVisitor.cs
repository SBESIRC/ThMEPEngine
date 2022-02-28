using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Engine
{
    public class ThTCHSprinklerExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj.IsTCHSprinkler())
            {
                var objs = new DBObjectCollection();
                dbObj.Explode(objs);
                var entities = objs.OfType<BlockReference>();
                if (entities.Count() == 0)
                {
                    return;
                }
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = ThOPMTools.GetOPMProperties(dbObj.Id),
                    Geometry = entities.First(),
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
            return e.IsTCHSprinkler();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }
    }
}
