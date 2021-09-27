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
    public class ThFanAirPortModel
    {
        public string AirPortType { set; get; }//风口类型
        public Point3d AirPortPosition { set; get; }//风口位置
        public double AirPortAngle { set; get; }//风口角度
        public double AirPortLength { set; get; }//风口长度
        public double AirPortDepth { set; get; }//侧回风口深度
        public short AirPortDirection { set; get; }//气流方向
        public void InsertAirPort(AcadDatabase acadDatabase)
        {
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-DAPP-GRIL", "AI-风口", AirPortPosition, new Scale3d(1, 1, 1), AirPortAngle);
            var blk = acadDatabase.Element<BlockReference>(blkId);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "风口长度")
                    {
                        property.Value = AirPortLength;
                    }
                    else if (property.PropertyName == "侧风口深度")
                    {
                        property.Value = AirPortDepth;
                    }
                    else if (property.PropertyName == "风口类型")
                    {
                        property.Value = AirPortType;
                    }
                    else if (property.PropertyName == "气流方向")
                    {
                        property.Value = AirPortDirection;
                    }
                    else if(AirPortType == "外墙防雨百叶" && property.PropertyName == "外墙防雨百叶深度")
                    {
                        property.Value = AirPortDepth;
                    }
                }
            }
        }
    }
}
