using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPHVAC.FanLayout.Model;

namespace ThMEPHVAC.FanLayout.Service
{
    public class ThFanToDBServiece
    {
        public void InsertAirPortMark(AcadDatabase acadDatabase,ThFanAirPortMarkModel airPortMark)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("风口名称", airPortMark.AirPortMarkName);
            attNameValues.Add("尺寸", airPortMark.AirPortMarkSize);
            attNameValues.Add("数量", airPortMark.AirPortMarkCount);
            attNameValues.Add("风量", airPortMark.AirPortMarkVolume);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-DIMS-DUCT", "风口标注", airPortMark.AirPortMarkPosition, new Scale3d(airPortMark.FontHeight, airPortMark.FontHeight, airPortMark.FontHeight), 0, attNameValues);
            var markLine = new Line(airPortMark.FanPosition, airPortMark.AirPortMarkPosition);
            markLine.LayerId = DbHelper.GetLayerId("H-DIMS-DUCT");
            acadDatabase.CurrentSpace.Add(markLine);
        }
        public void InsertAirPort(AcadDatabase acadDatabase,ThFanAirPortModel airPort)
        {
            ObjectId blkId;
            using (AcadDatabase tmpDataBase = AcadDatabase.Active())
            {
                blkId = tmpDataBase.ModelSpace.ObjectId.InsertBlockReference(
                    "H-DAPP-GRIL", 
                    "AI-风口", 
                    airPort.AirPortPosition, 
                    new Scale3d(1, 1, 1), 
                    airPort.AirPortAngle);
            }
            using (AcadDatabase tmpDataBase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(blkId);
                if (data.CustomProperties.Contains("风口类型"))
                {
                    data.CustomProperties.SetValue("风口类型", airPort.AirPortType);
                }
            }
            using (AcadDatabase tmpDataBase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(blkId);
                if (data.CustomProperties.Contains("风口长度"))
                {
                    data.CustomProperties.SetValue("风口长度", airPort.AirPortLength);
                }
                if (data.CustomProperties.Contains("气流方向"))
                {
                    data.CustomProperties.SetValue("气流方向", airPort.AirPortDirection);
                }
                if (data.CustomProperties.Contains("风口类型"))
                {
                    if ((string)data.CustomProperties.GetValue("风口类型") == "外墙防雨百叶")
                    {
                        if (data.CustomProperties.Contains("外墙防雨百叶深度"))
                        {
                            data.CustomProperties.SetValue("外墙防雨百叶深度", airPort.AirPortDepth);
                        }
                    }
                    else
                    {
                        if (data.CustomProperties.Contains("侧风口深度"))
                        {
                            data.CustomProperties.SetValue("侧风口深度", airPort.AirPortDepth);
                        }
                    }
                }
            }
        }
        public void InsertCEXHFan(AcadDatabase acadDatabase , ThFanCEXHModel cexh)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", cexh.FanNumber);
            attNameValues.Add("风量", cexh.FanVolume);
            attNameValues.Add("电量", cexh.FanPower);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-EQUP-FANS", "AI-吊顶式排风扇", cexh.FanPosition, new Scale3d(1, 1, 1), 0, attNameValues);
            var blk = acadDatabase.Element<BlockReference>(blkId, true);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "设备长度")
                    {
                        property.Value = cexh.FanLength;
                    }
                    else if (property.PropertyName == "设备宽度")
                    {
                        property.Value = cexh.FanWidth;
                    }
                    else if (property.PropertyName == "设备角度")
                    {
                        property.Value = cexh.FanAngle;
                    }
                    else if (property.PropertyName == "文字高度")
                    {
                        property.Value = cexh.FontHeight;
                    }
                }
            }
        }
        public void InsertFireValve(AcadDatabase acadDatabase,ThFanFireValveModel valve)
        {
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-DAPP-ADAMP", "防火阀", valve.FireValvePosition, new Scale3d(1, 1, 1), valve.FireValveAngle);
            var blk = acadDatabase.Element<BlockReference>(blkId, true);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "宽度或直径")
                    {
                        property.Value = valve.FireValveWidth;
                    }
                    else if (property.PropertyName == "可见性")
                    {
                        property.Value = valve.FireValveMark;
                    }
                    else if (property.PropertyName == "字高")
                    {
                        property.Value = valve.FontHeight;
                    }
                    else if (valve.FireValveAngle > Math.PI && (property.PropertyName == "角度" || property.PropertyName == "角度1"))
                    {
                        property.Value = Math.PI;
                    }

                }
            }
        }
        public void InsertFanHole(AcadDatabase acadDatabase,ThFanHoleModel hole)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("标高", hole.FanHoleMark);
            attNameValues.Add("洞口尺寸", hole.FanHoleSize);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-HOLE", "AI-洞口", hole.FanHolePosition, new Scale3d(1, 1, 1), 0, attNameValues);
            var blk = acadDatabase.Element<BlockReference>(blkId, true);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "洞口宽度")
                    {
                        property.Value = hole.FanHoleWidth;
                    }
                    if (property.PropertyName == "洞口角度")
                    {
                        property.Value = hole.FanHoleAngle;
                    }
                    if (property.PropertyName == "文字高度")
                    {
                        property.Value = hole.FontHeight;
                    }
                }
            }
        }
        public void InsertWAFFan(AcadDatabase acadDatabase,ThFanWAFModel waf)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", waf.FanNumber);
            attNameValues.Add("风量", waf.FanVolume);
            attNameValues.Add("电量", waf.FanPower);
            attNameValues.Add("标高", waf.FanMark);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-EQUP-FANS", "AI-壁式轴流风机", waf.FanPosition, new Scale3d(1, 1, 1), 0, attNameValues);
            var blk = acadDatabase.Element<BlockReference>(blkId, true);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "风机深度")
                    {
                        property.Value = waf.FanDepth;
                    }
                    if (property.PropertyName == "风机宽度")
                    {
                        property.Value = waf.FanWidth;
                    }
                    if (property.PropertyName == "风机角度")
                    {
                        property.Value = waf.FanAngle;
                    }
                    if (property.PropertyName == "文字高度")
                    {
                        property.Value = waf.FontHeight;
                    }
                }
            }
        }
        public void InsertWEXHFan(AcadDatabase acadDatabase , ThFanWEXHModel wexh)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", wexh.FanNumber);
            attNameValues.Add("风量", wexh.FanVolume);
            attNameValues.Add("电量", wexh.FanPower);
            attNameValues.Add("标高", wexh.FanMark);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-EQUP-FANS", "AI-壁式排风扇", wexh.FanPosition, new Scale3d(1, 1, 1), 0, attNameValues);
            var blk = acadDatabase.Element<BlockReference>(blkId, true);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "风机深度")
                    {
                        property.Value = wexh.FanDepth;
                    }
                    if (property.PropertyName == "风机宽度")
                    {
                        property.Value = wexh.FanWidth;
                    }
                    if (property.PropertyName == "风机角度")
                    {
                        property.Value = wexh.FanAngle;
                    }
                    if (property.PropertyName == "文字高度")
                    {
                        property.Value = wexh.FontHeight;
                    }
                }
            }
        }
    }
}