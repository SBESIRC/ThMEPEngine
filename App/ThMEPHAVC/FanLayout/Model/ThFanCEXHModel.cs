using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanLayout.Service;

namespace ThMEPHVAC.FanLayout.Model
{
    /// <summary>
    /// 吊顶式排气扇
    /// </summary>
    public class ThFanCEXHModel
    {
        public Point3d FanPosition { set; get; }//风机位置
        public double FanAngle { set; get; }//设备角度
        public double FontHeight { set; get; }//文字高度
        public string FanNumber { set; get; }//设备编号
        public string FanVolume { set; get; }//风量
        public string FanPower { set; get; }//电量=功率
        public string FanWeight { set; get; }//重量
        public string FanNoise { set; get; } //噪音
        public double FanDepth { set; get; }//深度
        public double FanWidth { set; get; }//宽度
        public double FanLength { set; get; }//长度

        public void InsertCEXHFan(AcadDatabase acadDatabase)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", FanNumber);
            attNameValues.Add("风量", FanVolume);
            attNameValues.Add("电量", FanPower);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-EQUP-FANS", "AI-吊顶式排风扇", FanPosition, new Scale3d(1, 1, 1), 0, attNameValues);
            var blk = acadDatabase.Element<BlockReference>(blkId);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "设备长度")
                    {
                        property.Value = FanLength;
                    }
                    else if (property.PropertyName == "设备宽度")
                    {
                        property.Value = FanWidth;
                    }
                    else if(property.PropertyName == "设备角度")
                    {
                        property.Value = FanAngle;
                    }
                    else if(property.PropertyName == "文字高度")
                    {
                        property.Value = FontHeight;
                    }
                }
            }
        }
    }
}
