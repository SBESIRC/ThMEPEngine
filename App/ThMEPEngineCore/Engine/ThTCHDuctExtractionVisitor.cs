using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHDuctExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj.IsTCHDuct())
            {
                var objs = new DBObjectCollection();
                dbObj.Explode(objs);
                var collection = objs.OfType<Curve>()
                                     .Where(o => !o.Layer.Contains("DUCT-加压送风中心线"))
                                     .ToCollection()
                                     .Outline();
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = ThOPMTools.GetOPMProperties(dbObj.Id),
                    Geometry = collection[0] as Entity,
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

        public override bool IsDistributionElement(Entity entity)
        {
            return entity.IsTCHDuct();
        }
    }
}
