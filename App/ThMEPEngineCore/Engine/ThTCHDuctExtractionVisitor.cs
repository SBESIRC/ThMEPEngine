using System;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHDuctExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj.IsTCHDuct())
            {
                var data = ThOPMTools.GetOPMProperties(dbObj.Id);
                var start_x = Convert.ToDouble(data["始端 X 坐标"]);
                var start_y = Convert.ToDouble(data["始端 Y 坐标"]);
                var end_x = Convert.ToDouble(data["末端 X 坐标"]);
                var end_y = Convert.ToDouble(data["末端 Y 坐标"]);
                var geometry = new Line(new Point3d(start_x, start_y, 0), new Point3d(end_x, end_y, 0));
                geometry.TransformBy(matrix);
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Geometry = geometry,
                    Data = data,
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
            return e.IsTCHDuct();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }
    }
}
