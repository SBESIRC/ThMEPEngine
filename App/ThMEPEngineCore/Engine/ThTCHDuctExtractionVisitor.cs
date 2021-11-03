using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
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
                var results = objs
                    .OfType<Curve>()
                    .Where(o => !o.Layer.Contains("DUCT-加压送风中心线"))
                    .ToCollection();
                // 这里需要获取一个外轮廓, 通常情况下用NTS Polygonizer来获取Outline
                // 由于天正图元有精度的问题，NTS Polygonizer不能获取Outline
                // 这里借用NTS Buffer的功能，它能把图元“合并”
                var outline = results
                    .Buffer(0.01)
                    .OfType<Curve>()
                    .OrderByDescending(o => o.Area)
                    .FirstOrDefault();
                if (outline != null)
                {
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = outline,
                        Data = ThOPMTools.GetOPMProperties(dbObj.Id),
                    });
                }
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
            return e.IsTCHDuct();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }
    }
}
