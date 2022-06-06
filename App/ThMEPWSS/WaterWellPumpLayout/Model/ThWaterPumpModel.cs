﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetARX;

using ThMEPWSS.Common;

namespace ThMEPWSS.WaterWellPumpLayout.Model
{
    public class ThWaterPumpModel
    {
        public double Angle { set; get; }//泵角度
        public Point3d Position { set; get; }//水泵位置
        public BlockReference Geometry { set; get; }//水泵图块数据
        public Polyline OBB { get; set; }
        public string VisibilityValue { get; set; }
        public string AttriValue { get; set; }
        public static ThWaterPumpModel Create(Entity ent)
        {
            ThWaterPumpModel pumpModel = null;
            if (ent is BlockReference blk)
            {
                pumpModel = new ThWaterPumpModel();
                pumpModel.Angle = 0.0;
                pumpModel.Geometry = blk;
                pumpModel.Position = blk.Position;
                pumpModel.OBB = ThMEPWSSUtils.GetVisibleOBB(blk);
                pumpModel.VisibilityValue = GetPumpCount(blk);
                pumpModel.AttriValue = blk.Id.GetAttributeInBlockReference("编号");
            }
            return pumpModel;
        }
        public void SetPumpSpace(double space)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                if (Geometry.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in Geometry.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "距离")
                        {
                            property.Value = space;
                        }
                        else if (property.PropertyName == "距离1")
                        {
                            property.Value = space * 2;
                        }
                        else if (property.PropertyName == "距离2")
                        {
                            property.Value = space * 3;
                        }
                    }
                }
            }
        }
        public void SetFontHeight(double fontHeight)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                if (Geometry.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in Geometry.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "字高")
                        {
                            property.Value = fontHeight;
                        }
                    }
                }
            }
        }
        public void SetPumpCount(int count)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                if (Geometry.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in Geometry.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "可见性")
                        {
                            string strCount = "单台";
                            switch (count)
                            {
                                case 1:
                                    strCount = "单台";
                                    break;
                                case 2:
                                    strCount = "两台";
                                    break;
                                case 3:
                                    strCount = "三台";
                                    break;
                                case 4:
                                    strCount = "四台";
                                    break;
                                default:
                                    break;
                            }
                            property.Value = strCount;
                        }
                    }
                }
            }
        }

        private static string GetPumpCount(BlockReference blk)
        {
            var visi = blk.Id.GetDynBlockValue("可见性");
            var strCount = "0";
            switch (visi)
            {
                case "单台":
                    strCount = "1";
                    break;
                case "两台":
                    strCount = "2";
                    break;
                case "三台":
                    strCount = "3";
                    break;
                case "四台":
                    strCount = "4";
                    break;
                default:
                    break;
            }

            return strCount;
        }
    }
}
