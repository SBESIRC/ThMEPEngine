using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanLayout.Model
{
    public class ThFanFireValveModel
    {
        public Point3d FireValvePosition { set; get; }//防火阀位置
        public double FontHeight { set; get; }//文字高度
        public double FireValveAngle { set; get; }//防火阀角度
        public double FireValveWidth { set; get; }//防火阀宽度
        public string FireValveMark { set; get; }//可见性
        public void InsertFireValve(AcadDatabase acadDatabase)
        {
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-DAPP-ADAMP", "防火阀", FireValvePosition, new Scale3d(1, 1, 1), FireValveAngle);
            var blk = acadDatabase.Element<BlockReference>(blkId);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "宽度或直径")
                    {
                        property.Value = FireValveWidth;
                    }
                    else if (property.PropertyName == "可见性")
                    {
                        property.Value = FireValveMark;
                    }
                    else if(property.PropertyName == "字高")
                    {
                        property.Value = FontHeight;
                    }
                    else if(FireValveAngle > Math.PI && (property.PropertyName == "角度" || property.PropertyName == "角度1"))
                    {
                        property.Value = Math.PI;
                    }

                }
            }
        }
    }
}
