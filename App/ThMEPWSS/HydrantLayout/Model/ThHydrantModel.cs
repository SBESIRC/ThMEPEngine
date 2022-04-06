using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.HydrantLayout.Model
{
    public class ThHydrantModel
    {
        //input 
        public int Type { get; set; }//消火栓（0）灭火器（1)
        public Polyline Outline { get; set; }//下面的长方形
        public Point3d Center { get; set; }//长方形的中心点
        public int OpenDir { get; set; }//左开（0）右开（1）灭火器没有（-1）
        public Vector3d BlkDir { get; set; }//原始块方向
        public Entity Data { get; set; }//原始块（新生成的没有）

        public ThHydrantModel()
        {

        }
        public void Transform(ThMEPOriginTransformer transformer)
        {
            transformer.Transform(Outline);
            Center = transformer.Transform(Center);
        }
        public void ProjectOntoXYPlane()
        {
            Outline.ProjectOntoXYPlane();
            var cDBP = new DBPoint(Center);
            cDBP.ProjectOntoXYPlane();
            Center = cDBP.Position;
        }
        public void Reset(ThMEPOriginTransformer transformer)
        {
            transformer.Reset(Outline);
            Center = transformer.Reset(Center);
        }
    }
}
