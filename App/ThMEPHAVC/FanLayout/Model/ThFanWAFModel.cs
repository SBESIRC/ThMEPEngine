﻿using Autodesk.AutoCAD.DatabaseServices;
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
    /// <summary>
    /// 壁式轴流风机
    /// </summary>
    public class ThFanWAFModel
    {
        public Point3d FanPosition { set; get; }//风机位置
        public double FontHeight { set; get; }//字体高度
        public double FanAngle { set; get; }//风机角度
        public string FanNumber { set; get; }//设备编号
        public string FanVolume { set; get; }//风量
        public string FanPower { set; get; }//电量=功率
        public string FanWeight { set; get; }//重量
        public string FanNoise { set; get; } //噪音
        public double FanDepth { set; get; }//深度
        public double FanWidth { set; get; }//宽度
        public double FanLength { set; get; }//长度
        public string FanMark { set; get; }//标高
        public void InsertWAFFan(AcadDatabase acadDatabase)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", FanNumber);
            attNameValues.Add("风量", FanVolume);
            attNameValues.Add("电量", FanPower);
            attNameValues.Add("标高", FanMark);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-EQUP-FANS", "AI-壁式轴流风机", FanPosition, new Scale3d(1, 1, 1), 0, attNameValues);
            var blk = acadDatabase.Element<BlockReference>(blkId);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "风机深度")
                    {
                        property.Value = FanDepth;
                    }
                    if (property.PropertyName == "风机宽度")
                    {
                        property.Value = FanWidth;
                    }
                    if (property.PropertyName == "风机角度")
                    {
                        property.Value = FanAngle;
                    }
                    if(property.PropertyName == "文字高度")
                    {
                        property.Value = FontHeight;
                    }
                }
            }
        }
    }
}
