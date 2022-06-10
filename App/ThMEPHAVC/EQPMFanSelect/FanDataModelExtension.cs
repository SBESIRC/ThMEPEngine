using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.IO.JSON;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.EQPMFanModelEnums;

namespace ThMEPHVAC.EQPMFanSelect
{
    public static class FanDataModelExtension
    {
        public static Dictionary<string, string> Attributes(this FanDataModel model)
        {
            var strScenario = CommonUtil.GetEnumDescription(model.Scenario);
            var strType = CommonUtil.GetEnumDescription(model.MountType);
            var attributes = new Dictionary<string, string>
            {
                // 设备符号
                [EQPMFanCommon.BLOCK_ATTRIBUTE_EQUIPMENT_SYMBOL] = EQPMFanCommon.Symbol(model.Name, model.InstallSpace),
                // 风机功能
                [EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_USAGE] = model.ServiceArea == null ? "":model.ServiceArea,
                // 风量
                [EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_VOLUME] = EQPMFanCommon.AirVolume(model.AirVolumeDescribe),
                // 全压
                [EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_PRESSURE] = EQPMFanCommon.WindResis(model.WindResisDescribe),
                // 电量
                [EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_CHARGE] = EQPMFanCommon.MotorPower(model.FanModelPowerDescribe),
                // 定频
                [EQPMFanCommon.BLOCK_ATTRIBUTE_FIXED_FREQUENCY] = EQPMFanCommon.FixedFrequency(model.Control),
                // 消防电源
                [EQPMFanCommon.BLOCK_ATTRIBUTE_FIRE_POWER_SUPPLY] = EQPMFanCommon.FirePower(model.FanPowerType),
                // 备注
                // 业务需求，暂时忽略备注
                [EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_REMARK] = "",
                //[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_REMARK] = model.Remark,
                // 安装方式
                [EQPMFanCommon.BLOCK_ATTRIBUTE_MOUNT_TYPE] = EQPMFanCommon.Mount(strType),
            };
            return attributes;
        }
        public static TypedValueList XDataValueList(this FanDataModel model, int number)
        {
            var strScenario = CommonUtil.GetEnumDescription(model.Scenario);
            var strType = CommonUtil.GetEnumDescription(model.VentStyle);
            var strControl = CommonUtil.GetEnumDescription(model.Control);
            var strFanEng = CommonUtil.GetEnumDescription(model.VentLev);
            var strElvEng = CommonUtil.GetEnumDescription(model.EleLev);
            var strVirType = CommonUtil.GetEnumDescription(model.VibrationMode);
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString,     model.ID },//风机ID - Guid
                { (int)DxfCode.ExtendedDataInteger32,       number },
                { (int)DxfCode.ExtendedDataAsciiString,     strType },
                { (int)DxfCode.ExtendedDataAsciiString,     strScenario},
                { (int)DxfCode.ExtendedDataAsciiString,     strControl},
                { (int)DxfCode.ExtendedDataAsciiString,     model.VolumeCalcModel.AirCalcValue},
                { (int)DxfCode.ExtendedDataAsciiString,     model.VolumeCalcModel.AirCalcFactor},
                { (int)DxfCode.ExtendedDataAsciiString,     model.DragModel.DuctLength},
                { (int)DxfCode.ExtendedDataAsciiString,     model.DragModel.Friction},
                { (int)DxfCode.ExtendedDataAsciiString,     model.DragModel.LocRes},
                { (int)DxfCode.ExtendedDataAsciiString,     model.DragModel.Damper},
                { (int)DxfCode.ExtendedDataAsciiString,     model.DragModel.EndReservedAirPressure},
                { (int)DxfCode.ExtendedDataAsciiString,     model.DragModel.DynPress},
                { (int)DxfCode.ExtendedDataAsciiString,     model.DragModel.SelectionFactor},
                { (int)DxfCode.ExtendedDataAsciiString,     strFanEng},
                { (int)DxfCode.ExtendedDataAsciiString,     strElvEng},
                { (int)DxfCode.ExtendedDataAsciiString,     model.FanModelCCCF},
                { (int)DxfCode.ExtendedDataAsciiString,     strVirType},
                { (int)DxfCode.ExtendedDataAsciiString,     model.Remark},
                { (int)DxfCode.ExtendedDataAsciiString,     model.FanModelTypeCalcModel.FanModelFanSpeed},
                { (int)DxfCode.ExtendedDataAsciiString,     model.FanModelTypeCalcModel.FanModelNoise},
                { (int)DxfCode.ExtendedDataAsciiString,     model.FanModelTypeCalcModel.FanModelInputMotorPower},
            };
            return valueList;
        }
        public static TypedValueList XDataValueList(this FanDataModel model, int number, FanDataModel childFan,string handleStr) 
        {
            var strScenario = CommonUtil.GetEnumDescription(model.Scenario);
            var strType = CommonUtil.GetEnumDescription(model.VentStyle);
            var strControl = CommonUtil.GetEnumDescription(model.Control);
            var xDataModel = FanXData(model, childFan);
            var strData = JsonHelper.SerializeObject(xDataModel);
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString,     model.ID },//风机ID - Guid
                { (int)DxfCode.ExtendedDataInteger32,       number },
                { (int)DxfCode.ExtendedDataAsciiString,     strType },
                { (int)DxfCode.ExtendedDataAsciiString,     strScenario},
                { (int)DxfCode.ExtendedDataAsciiString,     strControl},
                { (int)DxfCode.ExtendedDataAsciiString,     strData},
                { (int)DxfCode.ExtendedDataAsciiString,     handleStr},
                { (int)DxfCode.ExtendedDataAsciiString,     model.Remark},
            };
            return valueList;
        }
        public static FanBlockXDataModel FanXData(this FanDataModel model,FanDataModel childFan) 
        {
            var strFanEng = CommonUtil.GetEnumDescription(model.VentLev);
            var strElvEng = CommonUtil.GetEnumDescription(model.EleLev);
            var strVirType = CommonUtil.GetEnumDescription(model.VibrationMode);
            var strSource = CommonUtil.GetEnumDescription(model.FanModelTypeCalcModel.ValueSource);
            var retModel = new FanBlockXDataModel();
            retModel.ElvLev = strElvEng;
            retModel.VentLev = strFanEng;
            retModel.Remark = model.Remark;
            retModel.VibrationMode = strVirType;
            retModel.FanModelCCCF = model.FanModelCCCF;
            retModel.FanModelMotorPowerSource = strSource;
            if (null != childFan)
            {
                retModel.AirCalcValue = string.Format("{0}/{1}", model.VolumeCalcModel.AirCalcValue, childFan.VolumeCalcModel.AirCalcValue);
                retModel.AirCalcFactor = string.Format("{0}/{1}", model.VolumeCalcModel.AirCalcFactor, childFan.VolumeCalcModel.AirCalcFactor);
                retModel.DuctLength = string.Format("{0}/{1}", model.DragModel.DuctLength, childFan.DragModel.DuctLength);
                retModel.Friction = string.Format("{0}/{1}", model.DragModel.Friction, childFan.DragModel.Friction);
                retModel.LocRes = string.Format("{0}/{1}", model.DragModel.LocRes, childFan.DragModel.LocRes);
                retModel.Damper = string.Format("{0}/{1}", model.DragModel.Damper, childFan.DragModel.Damper);
                retModel.EndReservedAirPressure = string.Format("{0}/{1}", model.DragModel.EndReservedAirPressure, childFan.DragModel.EndReservedAirPressure);
                retModel.DynPress = string.Format("{0}/{1}", model.DragModel.DynPress, childFan.DragModel.DynPress);
                retModel.SelectionFactor = string.Format("{0}/{1}", model.DragModel.SelectionFactor, childFan.DragModel.SelectionFactor);
                retModel.FanModelInputMotorPower = string.Format("{0}/{1}", model.FanModelTypeCalcModel.FanModelInputMotorPower, childFan.FanModelTypeCalcModel.FanModelInputMotorPower);
            }
            else 
            {
                retModel.AirCalcValue = string.Format("{0}", model.VolumeCalcModel.AirCalcValue);
                retModel.AirCalcFactor = string.Format("{0}", model.VolumeCalcModel.AirCalcFactor);
                retModel.DuctLength = string.Format("{0}", model.DragModel.DuctLength);
                retModel.Friction = string.Format("{0}", model.DragModel.Friction);
                retModel.LocRes = string.Format("{0}", model.DragModel.LocRes);
                retModel.Damper = string.Format("{0}", model.DragModel.Damper);
                retModel.EndReservedAirPressure = string.Format("{0}", model.DragModel.EndReservedAirPressure);
                retModel.DynPress = string.Format("{0}", model.DragModel.DynPress);
                retModel.SelectionFactor = string.Format("{0}", model.DragModel.SelectionFactor);
                retModel.FanModelInputMotorPower = string.Format("{0}", model.FanModelTypeCalcModel.FanModelInputMotorPower);
            }
            return retModel;
        }
        public static FanBlockXDataModel ReadBlockFanXData(this ObjectId blockId,out FanBlockXDataBase fanBlockXData,string regAppName = ThHvacCommon.RegAppName_FanSelectionEx) 
        {
            fanBlockXData = null;
            FanBlockXDataModel retModel = null;
            var valueList = blockId.GetXData(regAppName);
            if (valueList == null)
            {
                return retModel;
            }
            try 
            {
                fanBlockXData = new FanBlockXDataBase();
                //读取顺序要和上面的写入顺序一致
                for (int i = 1; i < valueList.Count; i++)
                {
                    var strData = valueList.ElementAt(i).Value.ToString();
                    switch (i)
                    {
                        case 1:
                            fanBlockXData.Id = strData;
                            break;
                        case 2:
                            fanBlockXData.Number = (int)valueList.ElementAt(i).Value;
                            break;
                        case 3:
                            fanBlockXData.VentStyleString = strData;
                            break;
                        case 4:
                            fanBlockXData.ScenarioString = strData;
                            break;
                        case 5:
                            fanBlockXData.FanControlString = strData;
                            break;
                        case 6:
                            retModel = JsonHelper.DeserializeJsonToObject<FanBlockXDataModel>(strData);
                            break;
                        case 7:
                            fanBlockXData.HandleString = strData;
                            break;
                        case 8:
                            fanBlockXData.Remark = strData;
                            break;
                    }
                }
            }
            catch 
            {
                return null;
            }
            return retModel;
        }
        public static FanDataModel ReadBlockAllFanData(BlockReference fanBlock,out FanDataModel cModel,out bool isCopy) 
        {
            var reg = new Regex("[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var pModel = ReadFanBlockModel(fanBlock.Id, out cModel, out isCopy);
            if (null == pModel)
                return pModel;
            bool haveValue = false;
            if (pModel.VentStyle == EnumFanModelType.AxialFlow)
            {
                pModel.IntakeForm = EnumFanAirflowDirection.StraightInAndStraightOut;
                pModel.FanModelCCCF = fanBlock.Id.GetDynBlockValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_SPECIFICATION_MODEL);
                haveValue = true;
            }
            else 
            {
               
                var trx = fanBlock.Database.TransactionManager.TopTransaction;
                var blockName = fanBlock.DynamicBlockTableRecord.GetDBObject<BlockTableRecord>(trx, OpenMode.ForRead, false).Name;
                var items = CommonUtil.EnumDescriptionToList(typeof(EnumFanAirflowDirection));
                foreach (var item in items)
                {
                    if (blockName.Contains(item.Name))
                    {
                        pModel.IntakeForm = (EnumFanAirflowDirection)item.Value;
                        haveValue = true;
                        break;
                    }
                }
            }
            if (!haveValue)
                pModel.IntakeForm = EnumFanAirflowDirection.StraightInAndStraightOut;
            var visAttrs = BlockTools.GetAttributesInBlockReference(fanBlock.Id, true);
            foreach (var attr in visAttrs)
            {
                var strValue = attr.Value;
                if (attr.Key.Equals(EQPMFanCommon.BLOCK_ATTRIBUTE_EQUIPMENT_SYMBOL))
                {
                    //设备符号
                    var spliteStr = strValue.Split('-');
                    if (spliteStr.Count() == 2)
                    {
                        pModel.InstallSpace = spliteStr[1];
                    }
                }
                else if (attr.Key.Equals(EQPMFanCommon.BLOCK_ATTRIBUTE_STOREY_AND_NUMBER))
                {
                    //楼层编号
                    var spliteStr = strValue.Split('-');
                    pModel.InstallFloor = spliteStr[0];
                    if (spliteStr.Count() == 2)
                    {
                        pModel.VentNum = spliteStr[1];
                    }
                }
                else if (attr.Key.Equals(EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_USAGE))
                {
                    //风机功能
                    pModel.ServiceArea = strValue;
                }
                else if (attr.Key.Equals(EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_VOLUME))
                {
                    //风机风量

                    var matches = reg.Matches(strValue);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        int.TryParse(matches[i].Value, out int intValue);
                        if (i == 0)
                        {
                            pModel.AirVolume = intValue;
                        }
                        else if (i == 1 && cModel != null)
                        {
                            cModel.AirVolume = intValue;
                        }
                    }
                }
                else if (attr.Key.Equals(EQPMFanCommon.BLOCK_ATTRIBUTE_FAN_PRESSURE))
                {
                    //风机全压
                    var matches = reg.Matches(strValue);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        int.TryParse(matches[i].Value, out int intValue);
                        if (i == 0)
                        {
                            pModel.WindResis = intValue;
                        }
                        else if (i == 1 && cModel != null)
                        {
                            cModel.WindResis = intValue;
                        }
                    }
                }
                else if (attr.Key.Equals(EQPMFanCommon.BLOCK_ATTRIBUTE_MOUNT_TYPE))
                {
                    //安装方式
                    if (strValue.Contains("吊装"))
                    {
                        pModel.MountType = EnumMountingType.Hoisting;
                    }
                    else if (strValue.Contains("条形"))
                    {
                        pModel.MountType = EnumMountingType.FloorBar;
                    }
                    else
                    {
                        pModel.MountType = EnumMountingType.FloorSquare;
                    }
                }
            }
            return pModel;
        }
        public static FanDataModel ReadFanBlockModel(ObjectId fanBlockId, out FanDataModel cModel, out bool isCopy) 
        {
            FanDataModel pModel = null;
            cModel = null;
            isCopy = false;
            var xData = fanBlockId.ReadBlockFanXData(out FanBlockXDataBase xDataBase);
            if (null == xData || xDataBase == null)
                return pModel;
            isCopy = fanBlockId.Handle.ToString() != xDataBase.HandleString;
            string temp = xData.AirCalcValue;
            var enumItem = CommonUtil.GetEnumItemByDescription<EnumScenario>(xDataBase.ScenarioString);
            pModel = new FanDataModel(enumItem);
            pModel.ID = xDataBase.Id;
            pModel.VentStyle = CommonUtil.GetEnumItemByDescription<EnumFanModelType>(xDataBase.VentStyleString);
            pModel.Control = CommonUtil.GetEnumItemByDescription<EnumFanControl>( xDataBase.FanControlString);
            pModel.EleLev = CommonUtil.GetEnumItemByDescription<EnumFanEnergyConsumption>( xData.ElvLev);
            pModel.VentLev = CommonUtil.GetEnumItemByDescription<EnumFanEnergyConsumption>( xData.VentLev);
            pModel.VibrationMode = CommonUtil.GetEnumItemByDescription<EnumDampingType>(xData.VibrationMode);
            pModel.FanModelTypeCalcModel.ValueSource = CommonUtil.GetEnumItemByDescription<EnumValueSource>( xData.FanModelMotorPowerSource);
            pModel.Remark = xDataBase.Remark;
            bool haveChild = !string.IsNullOrEmpty(temp) && temp.Contains("/");
            if (haveChild)
            {
                cModel = new FanDataModel(enumItem);
                cModel.PID = pModel.ID;
                cModel.IsChildFan = true;
            }
            var spliteAirCalcValue = xData.AirCalcValue.Split('/');
            int.TryParse(spliteAirCalcValue[0], out int airCalcValue1);
            pModel.VolumeCalcModel.AirCalcValue = airCalcValue1;
            if (haveChild)
            {
                int.TryParse(spliteAirCalcValue[1], out int airCalcValue2);
                cModel.VolumeCalcModel.AirCalcValue = airCalcValue2;
            }
            var spliteAirCalcFactor = xData.AirCalcFactor.Split('/');
            double.TryParse(spliteAirCalcFactor[0], out double airCalcFactor1);
            pModel.VolumeCalcModel.AirCalcFactor = airCalcFactor1;
            if (haveChild)
            {
                double.TryParse(spliteAirCalcFactor[1], out double airCalcFactor2);
                cModel.VolumeCalcModel.AirCalcFactor = airCalcFactor2;
            }

            var spliteDuctLength = xData.DuctLength.Split('/');
            double.TryParse(spliteDuctLength[0], out double airDuctLength1);
            pModel.DragModel.DuctLength = airDuctLength1;
            if (haveChild)
            {
                double.TryParse(spliteDuctLength[1], out double airDuctLength2);
                cModel.DragModel.DuctLength = airDuctLength2;
            }

            var spliteFriction = xData.Friction.Split('/');
            double.TryParse(spliteFriction[0], out double Friction1);
            pModel.DragModel.Friction = Friction1;
            if (haveChild)
            {
                double.TryParse(spliteFriction[1], out double Friction2);
                cModel.DragModel.Friction = Friction2;
            }

            var spliteLocRes = xData.LocRes.Split('/');
            double.TryParse(spliteLocRes[0], out double LocRes1);
            pModel.DragModel.LocRes = LocRes1;
            if (haveChild)
            {
                double.TryParse(spliteLocRes[1], out double LocRes2);
                cModel.DragModel.LocRes = LocRes2;
            }

            var spliteDamper = xData.Damper.Split('/');
            int.TryParse(spliteDamper[0], out int Damper1);
            pModel.DragModel.Damper = Damper1;
            if (haveChild)
            {
                int.TryParse(spliteDamper[1], out int Damper2);
                cModel.DragModel.Damper = Damper2;
            }
            var spliteEnd = xData.EndReservedAirPressure.Split('/');
            int.TryParse(spliteEnd[0], out int End1);
            pModel.DragModel.EndReservedAirPressure = End1;
            if (haveChild)
            {
                int.TryParse(spliteEnd[1], out int End2);
                cModel.DragModel.EndReservedAirPressure = End2;
            }

            var spliteDynPress = xData.DynPress.Split('/');
            int.TryParse(spliteDynPress[0], out int DynPress1);
            pModel.DragModel.DynPress = DynPress1;
            if (haveChild)
            {
                int.TryParse(spliteEnd[1], out int DynPress2);
                cModel.DragModel.DynPress = DynPress2;
            }
            var spliteSelectionFactor = xData.SelectionFactor.Split('/');
            double.TryParse(spliteSelectionFactor[0], out double SelectionFactor1);
            pModel.DragModel.SelectionFactor = SelectionFactor1;
            if (haveChild)
            {
                double.TryParse(spliteSelectionFactor[1], out double SelectionFactor2);
                cModel.DragModel.SelectionFactor = SelectionFactor2;
            }
            var spliteInputMotorPower = xData.FanModelInputMotorPower.Split('/');
            pModel.FanModelTypeCalcModel.FanModelInputMotorPower = spliteInputMotorPower[0];
            if (haveChild)
            {
                cModel.FanModelTypeCalcModel.FanModelInputMotorPower = spliteInputMotorPower[1];
            }
            pModel.FanModelCCCF = xData.FanModelCCCF;
            return pModel;
        }
    }
    public class FanBlockXDataBase 
    {
        public string Id { get; set; }
        /// <summary>
        /// 风机编号
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 风机形式
        /// </summary>
        public string VentStyleString { get; set; }
        /// <summary>
        /// 风机频率
        /// </summary>
        public string FanControlString { get; set; }
        /// <summary>
        /// 应用场景
        /// </summary>
        public string ScenarioString { get; set; }
        /// <summary>
        /// 块HandStr,(用来判断块是否是复制块，复制后没有修改信息)
        /// </summary>
        public string HandleString { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
    public class FanBlockXDataModel 
    {
        /// <summary>
        /// 风机输入风量（双频时数据格式为 12000/10000）
        /// 前面为高速，后面为低速
        /// </summary>
        public string AirCalcValue { get; set; }
        /// <summary>
        /// 风量计算系数（双频时数据格式为 1.2/1.1）
        /// 前面为高速，后面为低速
        /// </summary>
        public string AirCalcFactor { get; set; }
        /// <summary>
        /// 风机阻力计算中的 风管长度（双频时数据格式为 80/60）
        /// 前面为高速，后面为低速
        /// </summary>
        public string DuctLength { get; set; }
        /// <summary>
        /// 风机阻力计算中的 比摩阻（双频时数据格式为 2/1）
        /// 前面为高速，后面为低速
        /// </summary>
        public string Friction { get; set; }
        /// <summary>
        /// 风机阻力计算中的局部阻力倍数（双频时数据格式为 2/1）
        /// 前面为高速，后面为低速
        /// </summary>
        public string LocRes { get; set; }
        /// <summary>
        /// 风机阻力计算中的消音器阻力（双频时数据格式为 100/80）
        /// 前面为高速，后面为低速
        /// </summary>
        public string Damper { get; set; }
        /// <summary>
        /// 风机阻力计算中的末端预留风压（双频时数据格式为 100/80）
        /// 前面为高速，后面为低速
        /// </summary>
        public string EndReservedAirPressure { get; set; }
        /// <summary>
        /// 风机阻力计算中的选择系数（双频时数据格式为 1.2/1.1）
        /// 前面为高速，后面为低速
        /// </summary>
        public string SelectionFactor { get; set; }
        /// <summary>
        /// 风机阻力计算中的动压（双频时数据格式为 100/80）
        /// 前面为高速，后面为低速
        /// </summary>
        public string DynPress { get; set; }
        /// <summary>
        /// 电机能耗等级
        /// </summary>
        public string ElvLev { get; set; }
        /// <summary>
        /// 风机能耗等级
        /// </summary>
        public string VentLev { get; set; }
        /// <summary>
        /// 减震方式
        /// </summary>
        public string VibrationMode { get; set; }
        /// <summary>
        /// 风机型号
        /// </summary>
        public string FanModelCCCF { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 风机选型中标识功率来运的信息
        /// </summary>
        public string FanModelMotorPowerSource { get; set; }
        /// <summary>
        /// 风机选型中的输入功率（双频时数据格式为 5/2.4）
        /// 前面为高速，后面为低速
        /// </summary>
        public string FanModelInputMotorPower { get; set; }
    }
}
