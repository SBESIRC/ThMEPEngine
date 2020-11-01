using System;
using System.Linq;
using System.Collections.Generic;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.Function
{
    public static class ExhaustModelCalculator
    {
        //计算空间净高小于6的场所最小风量
        public static double GetMinVolumeForLess6(ExhaustCalcModel model)
        {
            double textArea = model.CoveredArea.NullToDouble();
            double txtUnitVolume = model.UnitVolume.NullToDouble();
            return Math.Max(textArea * txtUnitVolume, 15000.0);
        }

        //计算空间净高大于6的场所最小风量
        public static double GetMinVolumeForGreater6(ExhaustModelLoader loader, ExhaustCalcModel model)
        {
            if (model.SpatialTypes.IsNullOrEmptyOrWhiteSpace() || model.SpaceHeight.IsNullOrEmptyOrWhiteSpace())
            {
                return 0;
            }
            List<ExhaustSpaceInfo> largerAndLowerModel = new List<ExhaustSpaceInfo>();
            if (model.SpatialTypes.Contains("办公室"))
            {
                largerAndLowerModel = FindLargerAndLowerModel(loader, model, "办公室");
                return 10000 * GetLinearInterpolation(largerAndLowerModel, model.SpaceHeight.NullToDouble());
            }
            else if (model.SpatialTypes.Contains("商店"))
            {
                largerAndLowerModel = FindLargerAndLowerModel(loader, model, "商店");
                return 10000 * GetLinearInterpolation(largerAndLowerModel, model.SpaceHeight.NullToDouble());
            }
            else if (model.SpatialTypes.Contains("仓库"))
            {
                largerAndLowerModel = FindLargerAndLowerModel(loader, model, "仓库");
                return 10000 * GetLinearInterpolation(largerAndLowerModel, model.SpaceHeight.NullToDouble());
            }
            else
            {
                largerAndLowerModel = FindLargerAndLowerModel(loader, model, "厂房");
                return 10000 * GetLinearInterpolation(largerAndLowerModel, model.SpaceHeight.NullToDouble());
            }
        }

        public static List<ExhaustSpaceInfo> FindLargerAndLowerModel(ExhaustModelLoader loader, ExhaustCalcModel model, string spacetype)
        {
            var largerNetHeightItems = loader.Spaces.Where(e => e.SpaceNetHeight >= model.SpaceHeight.NullToDouble() && e.SpaceType.Contains(spacetype) && e.HasSprinkler == model.IsSpray).ToList();
            var lowerNetHeightItems = loader.Spaces.Where(e => e.SpaceNetHeight < model.SpaceHeight.NullToDouble() && e.SpaceType.Contains(spacetype) && e.HasSprinkler == model.IsSpray).ToList();
            if (largerNetHeightItems.Count != 0)
            {
                if (lowerNetHeightItems.Count != 0)
                {
                    var largerModel = largerNetHeightItems.OrderBy(e => e.SpaceNetHeight).First();
                    var lowerModel = lowerNetHeightItems.OrderBy(e => e.SpaceNetHeight).Last();
                    return new List<ExhaustSpaceInfo>() { largerModel, lowerModel };
                }
                else
                {
                    var largerModel = largerNetHeightItems.OrderBy(e => e.SpaceNetHeight).First();
                    return new List<ExhaustSpaceInfo>() { largerModel };
                }
            }
            else
            {
                return new List<ExhaustSpaceInfo>();
            }

        }

        public static double GetLinearInterpolation(List<ExhaustSpaceInfo> models, double interx)
        {
            if (models.Count == 0)
            {
                return 0;
            }
            else
            {
                if (models.Count == 1)
                {
                    return models.First().MinVolume;
                }
                else
                {
                    return Math.Round(((interx - models[0].SpaceNetHeight) * (models[1].MinVolume - models[0].MinVolume) / (models[1].SpaceNetHeight - models[0].SpaceNetHeight)) + models[0].MinVolume, 2);
                }
            }
        }

        //计算汽车库最小风量
        public static double GetMinVolumeForGarage(ExhaustCalcModel model)
        {
            double height = model.SpaceHeight.NullToDouble();
            if (height < 3)
            {
                return 30000;
            }
            else
            {
                if (height < 4)
                {
                    return 31500;
                }
                else
                {
                    if (height < 5)
                    {
                        return 33000;
                    }
                    else
                    {
                        if (height < 6)
                        {
                            return 34500;
                        }
                        else
                        {
                            return 36000;
                        }
                    }
                }
            }
        }

        //计算公共建筑房间内走道或回廊设置排烟时最小风量
        public static double GetMinVolumeForCtlCloistersRooms(ExhaustCalcModel model)
        {
            double calculatevalue = model.CoveredArea.NullToDouble() * model.UnitVolume.NullToDouble();
            return Math.Max(calculatevalue, 13000);
        }

        //计算热释放速率
        //净高小于等于6的场所除外
        public static double GetHeatReleaseRate(HeatReleaseInfoLoader loader, ExhaustCalcModel model)
        {
            bool hassprinkler = model.IsSpray;
            if (model.SpatialTypes.IsNullOrEmptyOrWhiteSpace())
            {
                return 0;
            }
            else if (model.SpaceHeight.NullToDouble() > 8)
            {
                hassprinkler = false;
            }
            var heatmodels = new List<HeatReleaseInfo>();
            if (model.SpatialTypes.Contains("办公室"))
            {
                heatmodels = loader.HeatReleases.Where(h => h.BuildType.Contains("办公室") && h.HasSprinkler == hassprinkler).ToList();
            }
            else if (model.SpatialTypes.Contains("商店"))
            {
                heatmodels = loader.HeatReleases.Where(h => h.BuildType.Contains("商店") && h.HasSprinkler == hassprinkler).ToList();
            }
            else if (model.SpatialTypes.Contains("仓库"))
            {
                heatmodels = loader.HeatReleases.Where(h => h.BuildType.Contains("仓库") && h.HasSprinkler == hassprinkler).ToList();
            }
            else if (model.SpatialTypes.Contains("车库"))
            {
                heatmodels = loader.HeatReleases.Where(h => h.BuildType.Contains("车库") && h.HasSprinkler == hassprinkler).ToList();
            }
            else
            {
                heatmodels = loader.HeatReleases.Where(h => h.BuildType.Contains("厂房") && h.HasSprinkler == hassprinkler).ToList();
            }
            if (heatmodels.Count() == 0)
            {
                return 0;
            }
            return heatmodels.Select(h => h.ReleaseSpeed).First();
        }

        //计算风口-当量直径
        public static double GetSmokeDiameter(ExhaustCalcModel model)
        {
            return Math.Round(2 * model.SmokeWidth.NullToDouble() * model.SmokeLength.NullToDouble() / (model.SmokeLength.NullToDouble() + model.SmokeWidth.NullToDouble()), 0);
        }

        //计算轴对称型Mp值
        public static double GetAxialCalcAirVolum(ExhaustCalcModel model)
        {
            double hsp = model.SpaceHeight.NullToDouble() < 3 ? 0.5 * model.SpaceHeight.NullToDouble() : model.Axial_HighestHeight.NullToDouble();
            double hq = 1.6 + 0.1 * hsp;
            double z = 0;
            if (model.Axial_HangingWallGround.IsNullOrEmptyOrWhiteSpace())
            {
                z = hq;
            }
            else
            {
                double hdd = model.Axial_HangingWallGround.NullToDouble() - model.Axial_FuelFloor.NullToDouble();
                z = Math.Max(hdd, hq);
            }

            double mp = 0;
            double qc = 0.7 * 1000 * model.HeatReleaseRate.NullToDouble();
            double z1 = 0.166 * Math.Pow(qc, 0.4);
            if (z > z1)
            {
                mp = 0.071 * Math.Pow(qc, 1.0 / 3.0) * Math.Pow(z, 5.0 / 3.0) + 0.0018 * qc;
            }
            else
            {
                mp = 0.032 * Math.Pow(qc, 3.0 / 5.0) * z;
            }
            return mp;
        }

        //计算阳台溢出型Mp值
        public static double GetOverfloorCalcAirVolum(ExhaustCalcModel model)
        {
            double W = model.Spill_FireOpening.NullToDouble() + model.Spill_OpenBalcony.NullToDouble();
            double mp = 0.36 * (Math.Pow(1000 * model.HeatReleaseRate.NullToDouble() * Math.Pow(W, 2), 1.0 / 3.0) * (model.Spill_BalconySmokeBottom.NullToDouble() + 0.25 * model.Spill_FuelBalcony.NullToDouble()));
            return mp;
        }

        //计算窗口型Mp值
        public static double GetWindowCalcAirVolum(ExhaustCalcModel model)
        {
            double aw = 2.4 * Math.Pow(model.Window_WindowArea.NullToDouble(), 0.4) * Math.Pow(model.Window_WindowHeight.NullToDouble(), 0.2) - 2.1 * model.Window_WindowHeight.NullToDouble();
            double mp = 0.68 * Math.Pow(model.Window_WindowArea.NullToDouble() * Math.Pow(model.Window_WindowHeight.NullToDouble(), 0.5), 1.0 / 3.0) * Math.Pow(model.Window_SmokeBottom.NullToDouble() + aw, 5.0 / 3.0) + 1.59 * model.Window_WindowArea.NullToDouble() * Math.Pow(model.Window_WindowHeight.NullToDouble(), 0.5);
            return mp;
        }

        //计算排烟量
        public static string GetCalcAirVolum(ExhaustCalcModel model)
        {
            double qc = 0.7 * 1000 * model.HeatReleaseRate.NullToDouble();
            double mp = 0;
            switch (model.PlumeSelection)
            {
                case "轴对称型":
                    mp = GetAxialCalcAirVolum(model);
                    break;
                case "阳台溢出型":
                    mp = GetOverfloorCalcAirVolum(model);
                    break;
                case "窗口型":
                    mp = GetWindowCalcAirVolum(model);
                    break;
                default:
                    break;
            }
            double t = 293.15 + qc / (mp * 1.01);
            double calcairvolum = Math.Round(3600 * mp * t / (1.2 * 293.15), 0);
            return double.IsNaN(calcairvolum) ? "无" : calcairvolum.NullToStr();
        }

        //计算排烟位置系数
        public static double GetSmokeFactor(ExhaustCalcModel model)
        {
            if (model.SmokeFactorOption.Contains("≥2"))
            {
                return 1;
            }
            else
            {
                return 0.5;
            }
        }

        //计算最大允许排烟
        public static string GetMaxSmoke(ExhaustCalcModel model)
        {
            double qc = 0.7 * 1000 * model.HeatReleaseRate.NullToDouble();
            double dt = 0;
            switch (model.PlumeSelection)
            {
                case "轴对称型":
                    dt = qc / (GetAxialCalcAirVolum(model) * 1.01);
                    break;
                case "阳台溢出型":
                    dt = qc / (GetOverfloorCalcAirVolum(model) * 1.01);
                    break;
                case "窗口型":
                    dt = qc / (GetWindowCalcAirVolum(model) * 1.01);
                    break;
                default:
                    return "无";
            }
            double maxsmoke = Math.Round(3600 * 4.16 * model.SmokeFactorValue.NullToDouble() * Math.Pow(model.SmokeThickness.NullToDouble(), 2.5) * Math.Pow(dt / 293.15, 0.5));
            return double.IsNaN(maxsmoke) ? "无" : maxsmoke.NullToStr();
        }

        //判断选型系数
        public static string SelectionFactorCheck(string factor)
        {
            return factor.NullToDouble() < 1.2 ? "1.2" : factor;
        }

        //获取Z值
        public static double GetZValue(ExhaustCalcModel model)
        {
            double hsp = model.SpaceHeight.NullToDouble() < 3 ? 0.5 * model.SpaceHeight.NullToDouble() : model.Axial_HighestHeight.NullToDouble();
            double hq = 1.6 + 0.1 * hsp;
            double z = 0;
            if (model.Axial_HangingWallGround.IsNullOrEmptyOrWhiteSpace())
            {
                z = hq;
            }
            else
            {
                double hdd = model.Axial_HangingWallGround.NullToDouble() - model.Axial_FuelFloor.NullToDouble();
                z = Math.Max(hdd, hq);
            }
            return z;
        }

        //获取Z1值
        public static double GetZ1Value(ExhaustCalcModel model)
        {
            double qc = 0.7 * 1000 * model.HeatReleaseRate.NullToDouble();
            return 0.166 * Math.Pow(qc, 0.4);
        }

        //Z值是否大于Z1
        public static bool IfZBiggerThanZ1(ExhaustCalcModel model)
        {
            return GetZValue(model) > GetZ1Value(model);
        }

        //获取Hq值
        public static double GetHqValue(ExhaustCalcModel model)
        {
            double hsp = model.SpaceHeight.NullToDouble() < 3 ? 0.5 * model.SpaceHeight.NullToDouble() : model.Axial_HighestHeight.NullToDouble();
            return 1.6 + 0.1 * hsp;
        }

        //计算dt
        public static double GetDtValue(ExhaustCalcModel model)
        {
            double qc = 0.7 * 1000 * model.HeatReleaseRate.NullToDouble();
            double mp = 0;
            switch (model.PlumeSelection)
            {
                case "轴对称型":
                    mp = GetAxialCalcAirVolum(model);
                    break;
                case "阳台溢出型":
                    mp = GetOverfloorCalcAirVolum(model);
                    break;
                case "窗口型":
                    mp = GetWindowCalcAirVolum(model);
                    break;
                default:
                    break;
            }
            return qc / (mp * 1.01);
        }

        //计算aw值
        public static double GetawValue(ExhaustCalcModel model)
        {
            double aw = 2.4 * Math.Pow(model.Window_WindowArea.NullToDouble(), 0.4) * Math.Pow(model.Window_WindowHeight.NullToDouble(), 0.2) - 2.1 * model.Window_WindowHeight.NullToDouble();
            return aw;
        }

        //计算T值
        public static double GeTValue(ExhaustCalcModel model)
        {
            return 293.15 + GetDtValue(model);
        }

        //向上圆整50的风量
        public static int RoundUpToFifty(int orgvalue)
        {
            int remaindernumber = orgvalue % 100;
            return remaindernumber > 50 ? orgvalue - remaindernumber + 100 : orgvalue - remaindernumber + 50;
        }

        public static string GetTxtCalcValue(ExhaustCalcModel model)
        {
            if (model.Final_CalcAirVolum == "无" || model.MaxSmokeExtraction == "无")
            {
                return "无";
            }
            else
            {
                if (model.ExhaustCalcType == "空间-净高小于等于6m")
                {
                    return FuncStr.NullToStr(model.MinAirVolume.NullToDouble());
                }
                else
                {
                    return FuncStr.NullToStr(Math.Max(model.Final_CalcAirVolum.NullToDouble(), model.MinAirVolume.NullToDouble()));
                }
            }
        }

    }
}
