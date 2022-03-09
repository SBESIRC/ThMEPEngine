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
using ThMEPHVAC.FanLayout.ViewModel;

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
            attNameValues.Add("安装属性", airPortMark.AirPortHeightMark);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-DIMS-DUCT", "AI-风口标注1", airPortMark.AirPortMarkPosition, new Scale3d(airPortMark.FontHeight, airPortMark.FontHeight, airPortMark.FontHeight), 0, attNameValues);
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
        public void InsertCEXHFan(AcadDatabase acadDatabase , ThFanCEXHModel cexh, ThFanConfigInfo info)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", cexh.FanNumber);
            //attNameValues.Add("风量", cexh.FanVolume);
            //attNameValues.Add("电量", cexh.FanPower);
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
            var tvs = new TypedValueList();
            tvs.Add(new TypedValue((int) DxfCode.ExtendedDataReal, info.FanPressure));
            tvs.Add(new TypedValue((int) DxfCode.ExtendedDataReal, info.FanNoise));
            tvs.Add(new TypedValue((int) DxfCode.ExtendedDataReal, info.FanWeight));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanVolume));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanPower));
            blkId.AddXData("FanProperty", tvs);
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
        public void InsertWAFFan(AcadDatabase acadDatabase,ThFanWAFModel waf, ThFanConfigInfo info)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", waf.FanNumber);
            //attNameValues.Add("风量", waf.FanVolume);
            //attNameValues.Add("电量", waf.FanPower);
            //attNameValues.Add("标高", waf.FanMark);
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
            var tvs = new TypedValueList();
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanPressure));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanNoise));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanWeight));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanVolume));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanPower));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, waf.FanMark));
            blkId.AddXData("FanProperty", tvs);
        }
        public void InsertWEXHFan(AcadDatabase acadDatabase , ThFanWEXHModel wexh, ThFanConfigInfo info)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("设备编号", wexh.FanNumber);
            //attNameValues.Add("风量", wexh.FanVolume);
            //attNameValues.Add("电量", wexh.FanPower);
            //attNameValues.Add("标高", wexh.FanMark);
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
            var tvs = new TypedValueList();
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanPressure));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanNoise));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanWeight));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanVolume));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataReal, info.FanPower));
            tvs.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, wexh.FanMark));
            blkId.AddXData("FanProperty", tvs);
        }

        public void InsertEntity(Entity entity,string layer)
        {
            using (var database = AcadDatabase.Active())
            {
                database.ModelSpace.Add(entity);
                entity.Layer = layer;
                entity.Linetype = "ByLayer";
                entity.LineWeight = LineWeight.ByLayer;
                entity.ColorIndex = (int)ColorIndex.BYLAYER;
            }
        }
        public void InsertEntity(Entity entity, string layer,int colorIndex)
        {
            using (var database = AcadDatabase.Active())
            {
                database.ModelSpace.Add(entity);
                entity.Layer = layer;
                entity.Linetype = "ByLayer";
                entity.LineWeight = LineWeight.ByLayer;
                entity.ColorIndex = colorIndex;
            }
        }
        public void InsertPipeMark(string layer, string blockName, Point3d position,double angle,List<string> properties)
        {
            using (var database = AcadDatabase.Active())
            {
                Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                for(int i = 0;i < properties.Count;i++)
                {
                    string strKey = "水管标注" + (i + 1).ToString();
                    attNameValues.Add(strKey, properties[i]);
                }
                var blkID = InsertBlockReference(database.ModelSpace.ObjectId, layer, blockName, position, new Scale3d(1, 1, 1), angle, attNameValues);
                foreach (DynamicBlockReferenceProperty property in blkID.GetDynProperties())
                {
                    if (property.PropertyName.Contains("管间距"))
                    {
                        property.Value = 300.0;
                    }
                }
            }
        }
        public void InsertBlockReference(string layer, string blockName, Point3d position, double angle, Scale3d scale)
        {
            using (var database = AcadDatabase.Active())
            {
                Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                InsertBlockReference(database.ModelSpace.ObjectId, layer, blockName, position, scale, angle, attNameValues);
            }
        }
        public void InsertText(string layer, string strText, Point3d position, double angle)
        {
            using (var database = AcadDatabase.Active())
            {
                var dbText = new DBText();
                dbText.Layer = layer;
                dbText.TextString = strText;
                dbText.Position = position;
                dbText.Rotation = angle;
                dbText.Height = 300.0;
                dbText.WidthFactor = 0.7;
                dbText.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                dbText.HorizontalMode = TextHorizontalMode.TextCenter;
                dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
                dbText.AlignmentPoint = position;
                database.ModelSpace.Add(dbText);
            }
        }
        public void InsertValve(string layer, string blockName,string strType, Point3d position, double angle, Scale3d scale)
        {
            using (var database = AcadDatabase.Active())
            {
                Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                var blkId = InsertBlockReference(database.ModelSpace.ObjectId, layer, blockName, position, scale, angle, attNameValues);
                var data = new ThBlockReferenceData(blkId);
                if (data.CustomProperties.Contains("可见性"))
                {
                    data.CustomProperties.SetValue("可见性", strType);
                }
            }
        }
        public ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blockName, Point3d position, Scale3d scale, double rotateAngle, Dictionary<string, string> attNameValues)
        {
            Database db = spaceId.Database;//获取数据库对象
            //以读的方式打开块表
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            //如果没有blockName表示的块，则程序返回
            if (!bt.Has(blockName)) return ObjectId.Null;
            //以写的方式打开空间（模型空间或图纸空间）
            BlockTableRecord space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            ObjectId btrId = bt[blockName];//获取块表记录的Id
            //打开块表记录
            BlockTableRecord record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
            //创建一个块参照并设置插入点
            BlockReference br = new BlockReference(position, bt[blockName]);
            br.ScaleFactors = scale;//设置块参照的缩放比例
            br.Layer = layer;//设置块参照的层名
            br.Rotation = rotateAngle;//设置块参照的旋转角度
            space.AppendEntity(br);//为了安全，将块表状态改为读 
            //判断块表记录是否包含属性定义
            if (record.HasAttributeDefinitions)
            {
                //若包含属性定义，则遍历属性定义
                foreach (ObjectId id in record)
                {
                    //检查是否是属性定义
                    AttributeDefinition attDef = id.GetObject(OpenMode.ForRead) as AttributeDefinition;
                    if (attDef != null)
                    {
                        //创建一个新的属性对象
                        AttributeReference attribute = new AttributeReference();
                        //从属性定义获得属性对象的对象特性
                        attribute.SetAttributeFromBlock(attDef, br.BlockTransform);
                        //判断是否包含指定的属性名称
                        if (attNameValues.ContainsKey(attDef.Tag.ToUpper()))
                        {
                            //设置属性值
                            attribute.TextString = attNameValues[attDef.Tag.ToUpper()].ToString();
                        }
                        //向块参照添加属性对象
                        br.AttributeCollection.AppendAttribute(attribute);
                        db.TransactionManager.AddNewlyCreatedDBObject(attribute, true);
                    }
                }
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            return br.ObjectId;//返回添加的块参照的Id
        }

    }
}