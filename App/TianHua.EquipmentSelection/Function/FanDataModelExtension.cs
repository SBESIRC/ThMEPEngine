using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.Function
{
    public static class FanDataModelExtension
    {
        public static Dictionary<string, string> Attributes(this FanDataModel model)
        {
            var attributes = new Dictionary<string, string>
            {
                // 设备符号
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_EQUIPMENT_SYMBOL] = ThFanSelectionUtils.Symbol(model.Scenario, model.InstallSpace),

                // 风机功能
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_USAGE] = model.Name,

                // 风量
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_VOLUME] = ThFanSelectionUtils.AirVolume(model.AirVolumeDescribe),

                // 全压
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_PRESSURE] = ThFanSelectionUtils.WindResis(model.WindResisDescribe),

                // 电量
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_CHARGE] = ThFanSelectionUtils.MotorPower(model.FanModelPowerDescribe),

                // 定频
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_FIXED_FREQUENCY] = ThFanSelectionUtils.FixedFrequency(model.Control, model.IsFre),

                // 消防电源
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_FIRE_POWER_SUPPLY] = ThFanSelectionUtils.FirePower(model.PowerType),

                // 备注
                // 业务需求，暂时忽略备注
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_REMARK] = "",
                //[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_REMARK] = model.Remark,

                // 安装方式
                [ThFanSelectionCommon.BLOCK_ATTRIBUTE_MOUNT_TYPE] = ThFanSelectionUtils.Mount(model.MountType),
            };
            return attributes;
        }

        public static bool IsValid(this FanDataModel model)
        {
            return !string.IsNullOrEmpty(model.FanModelName) && (model.FanModelName != "无此风机");
        }

        public static bool IsAXIALModel(this FanDataModel model)
        {
            return FuncStr.NullToStr(model.VentStyle) == "轴流";
        }

        public static bool IsHighSpeedModel(this FanDataModel model)
        {
            return model.PID == "0";
        }

        public static FanDataModel ParentModel(this List<FanDataModel> models, FanDataModel model)
        {
            if (model.IsHighSpeedModel())
            {
                return null;
            }

            return models.Find(p => p.ID == model.PID);
        }

        public static FanDataModel ChildModel(this List<FanDataModel> models, FanDataModel model)
        {
            if (!model.IsHighSpeedModel())
            {
                return null;
            }

            return models.Find(p => p.PID == model.ID);
        }

        public static bool HasChildModel(this List<FanDataModel> models, FanDataModel model)
        {
            if (!model.IsHighSpeedModel())
            {
                return false;
            }

            return models.Any(m=>m.PID == model.ID);
        }

        public static bool IsAttributeModified(this FanDataModel model, Dictionary<string, string> attributes)
        {
            // 设备符号
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_EQUIPMENT_SYMBOL))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_EQUIPMENT_SYMBOL]
                    != ThFanSelectionUtils.Symbol(model.Scenario, model.InstallSpace))
                {
                    return true;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            // 设备编号（“楼层-编号”）
            // 暂时只比较楼层是否变化
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_STOREY_AND_NUMBER))
            {
                if (!attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_STOREY_AND_NUMBER].StartsWith(model.InstallFloor))
                {
                    return true;
                }
            }

            // 风机功能
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_USAGE))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_USAGE] != model.Name)
                {
                    return true;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            // 风量
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_VOLUME))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_VOLUME]
                    != ThFanSelectionUtils.AirVolume(model.AirVolumeDescribe))
                {
                    return true;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            // 全压
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_PRESSURE))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_PRESSURE]
                    != ThFanSelectionUtils.WindResis(model.WindResisDescribe))
                {
                    return true;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            // 电量
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_CHARGE))
            {

                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_CHARGE]
                    != ThFanSelectionUtils.MotorPower(model.FanModelPowerDescribe))
                {
                    return true;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            // 定频
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_FIXED_FREQUENCY))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FIXED_FREQUENCY]
                    != ThFanSelectionUtils.FixedFrequency(model.Control, model.IsFre))
                {
                    return true;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            // 消防电源
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_FIRE_POWER_SUPPLY))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FIRE_POWER_SUPPLY]
                    != ThFanSelectionUtils.FirePower(model.PowerType))
                {
                    return true;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            // 备注
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_REMARK))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_REMARK] != model.Remark)
                {
                    return true;
                }
            }

            // 安装方式
            if (attributes.ContainsKey(ThFanSelectionCommon.BLOCK_ATTRIBUTE_MOUNT_TYPE))
            {
                if (attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_MOUNT_TYPE] != ThFanSelectionUtils.Mount(model.MountType))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetAirVolume(this FanDataModel model)
        {
            if (model.Scenario == "消防加压送风")
            {
                return model.SplitAirVolume;
            }
            return model.AirVolume;
        }

        public static FanDataModel CreateAuxiliaryModel(this FanDataModel model, string scenario)
        {
            FanDataModel _FanDataModel = new FanDataModel();
            _FanDataModel.ID = Guid.NewGuid().ToString();
            _FanDataModel.Scenario = scenario;
            _FanDataModel.PID = model.ID;
            _FanDataModel.AirVolume = 0;

            _FanDataModel.InstallSpace = "-";
            _FanDataModel.InstallFloor = "-";
            _FanDataModel.VentQuan = 0;
            _FanDataModel.VentNum = "-";

            _FanDataModel.VentStyle = "-";
            _FanDataModel.VentConnect = "-";
            _FanDataModel.VentLev = "-";
            _FanDataModel.EleLev = "-";
            _FanDataModel.FanModelName = "-";
            _FanDataModel.MountType = "-";
            _FanDataModel.VibrationMode = "-";
            switch(scenario)
            {
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                    {
                        _FanDataModel.Name = "平时";
                        _FanDataModel.Use = "平时排风";
                    }
                    break;
                case "平时送风兼事故补风":
                case "平时排风兼事故排风":
                    {
                        _FanDataModel.Name = "兼用";
                        _FanDataModel.Use = "平时排风";
                    }
                    break;
                default:
                    break;
            }
            return _FanDataModel;
        }
    }
}